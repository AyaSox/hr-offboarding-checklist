// Theme Toggle Functionality
(function() {
    // Load saved theme or default to light
    const savedTheme = localStorage.getItem('theme') || 'light';
    
    // Apply theme on page load (before DOM ready for no flash)
    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        const themeIcon = document.getElementById('theme-icon');
        if (themeIcon) {
            if (theme === 'dark') {
                themeIcon.classList.remove('fa-moon');
                themeIcon.classList.add('fa-sun');
            } else {
                themeIcon.classList.remove('fa-sun');
                themeIcon.classList.add('fa-moon');
            }
        }
    }
    
    // Apply saved theme immediately
    applyTheme(savedTheme);
    
    // Wait for DOM to be ready
    document.addEventListener('DOMContentLoaded', function() {
        const themeToggle = document.getElementById('theme-toggle');
        
        if (themeToggle) {
            themeToggle.addEventListener('click', function() {
                const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
                const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
                
                // Save preference
                localStorage.setItem('theme', newTheme);
                
                // Apply theme
                applyTheme(newTheme);
            });
        }
    });
})();

// Initialize page
$(document).ready(function() {
    // Initialize toasts
    $('.toast').each(function() {
        new bootstrap.Toast(this).show();
    });
    
    // Initialize notification settings if not already set
    if (typeof window.notificationSettings === 'undefined') {
        window.notificationSettings = {
            lastCount: 0,
            lastCheck: new Date(),
            userId: ''
        };
    }
    
    // Set up notification event handlers
    setupNotificationHandlers();
    
    // Load notification count on page load
    loadNotificationCount();
    
    // Refresh notification count and check for new notifications every 30 seconds
    setInterval(function() {
        loadNotificationCount();
        checkForNewNotifications();
    }, 30000);
    
    // Initialize real-time features
    initializeRealTimeFeatures();
});

// Global variables for notification tracking
let lastNotificationCount = window.notificationSettings ? window.notificationSettings.lastCount : 0;
let lastNotificationCheck = new Date();

function setupNotificationHandlers() {
    // Ensure the dropdown event is properly bound
    $(document).off('show.bs.dropdown', '#notificationsDropdown').on('show.bs.dropdown', '#notificationsDropdown', function () {
        console.log('Notification dropdown opened, loading notifications...');
        loadNotifications();
    });
}

function initializeRealTimeFeatures() {
    // Check for new notifications immediately
    setTimeout(checkForNewNotifications, 5000);
}

function loadNotificationCount() {
    // Skip if user is not logged in
    if (!window.notificationSettings || !window.notificationSettings.userId) {
        return;
    }
    
    $.ajax({
        url: '/Notifications/GetUnreadCount',
        type: 'GET',
        headers: { 'Accept': 'application/json' },
        cache: false,
        success: function(data) {
            if (typeof data !== 'object' || data === null || typeof data.count === 'undefined') {
                console.warn('Unexpected response for GetUnreadCount', data);
                return;
            }
            const badge = $('#notificationCount');
            const currentCount = data.count || 0;
            
            if(currentCount > 0) {
                badge.text(currentCount).show();
                
                // Show toast for new notifications
                if (currentCount > lastNotificationCount && lastNotificationCount >= 0) {
                    const newCount = currentCount - lastNotificationCount;
                    if (newCount > 0) {
                        showNotificationToast(newCount);
                    }
                }
            } else {
                badge.hide();
            }
            
            lastNotificationCount = currentCount;
        },
        error: function(xhr, status, error) {
            console.warn('Could not load notification count:', status, error);
        }
    });
}

function checkForNewNotifications() {
    // Skip if user is not logged in
    if (!window.notificationSettings || !window.notificationSettings.userId) {
        return;
    }
    
    $.ajax({
        url: '/Notifications/GetLatest',
        type: 'GET',
        headers: { 'Accept': 'application/json' },
        data: { since: lastNotificationCheck.toISOString() },
        cache: false,
        success: function(data) {
            if (!Array.isArray(data)) {
                console.warn('Unexpected response for GetLatest', data);
                return;
            }
            if (data && data.length > 0) {
                // Update last check time
                lastNotificationCheck = new Date();
                
                // Show toast for high priority notifications
                data.forEach(function(notification) {
                    if (!notification.isRead && (notification.priority >= 3)) {
                        showHighPriorityNotificationToast(notification);
                    }
                });
            }
        },
        error: function(xhr, status, error) {
            console.warn('Could not check for new notifications:', status, error);
        }
    });
}

// Load notifications when dropdown is opened
$('#notificationsDropdown').on('show.bs.dropdown', function () {
    loadNotifications();
});

function loadNotifications() {
    const loadingHtml = `
        <div class="text-center p-3">
            <div class="spinner-border spinner-border-sm text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mb-0 mt-2 small">Loading notifications...</p>
        </div>`;
    
    $('#notificationContent').html(loadingHtml);
    
    // Skip if user is not logged in
    if (!window.notificationSettings || !window.notificationSettings.userId) {
        $('#notificationContent').html(`
            <div class="text-center p-3">
                <i class="fas fa-user-slash text-muted mb-2"></i>
                <p class="mb-0 small">Please log in to view notifications</p>
            </div>`);
        return;
    }
    
    $.ajax({
        url: '/Notifications/GetLatest',
        type: 'GET',
        headers: { 'Accept': 'application/json' },
        cache: false,
        success: function(data) {
            if (!Array.isArray(data)) {
                console.error('Failed to load notifications: non-JSON response received');
                const errorHtml = `
                    <div class="text-center p-3">
                        <i class="fas fa-exclamation-circle text-danger mb-2"></i>
                        <p class="mb-0 small">Could not load notifications</p>
                        <p class="mb-0 small text-muted">Unexpected response</p>
                        <button class="btn btn-sm btn-outline-primary mt-2" onclick="loadNotifications()">
                            <i class="fas fa-sync me-1"></i>Retry
                        </button>
                    </div>`;
                $('#notificationContent').html(errorHtml);
                return;
            }
            console.log('Notifications loaded:', data);
            displayNotifications(data);
        },
        error: function(xhr, status, error) {
            console.error('Failed to load notifications:', status, error, xhr);
            const errorHtml = `
                <div class="text-center p-3">
                    <i class="fas fa-exclamation-circle text-danger mb-2"></i>
                    <p class="mb-0 small">Could not load notifications</p>
                    <p class="mb-0 small text-muted">${error || 'Network error'}</p>
                    <button class="btn btn-sm btn-outline-primary mt-2" onclick="loadNotifications()">
                        <i class="fas fa-sync me-1"></i>Retry
                    </button>
                </div>`;
            $('#notificationContent').html(errorHtml);
        }
    });
}

function displayNotifications(data) {
    if(data && data.length > 0) {
        let html = '';
        $.each(data, function(i, notification) {
            const iconClass = getNotificationIcon(notification.type);
            const readClass = notification.isRead ? 'text-muted' : 'fw-bold';
            const priorityIndicator = notification.priority >= 3 ? 
                `<span class="badge ${notification.priority === 4 ? 'bg-danger' : 'bg-warning'} ms-1">!</span>` : '';
            
            html += `
                <a class="dropdown-item notification-item ${readClass}" 
                   href="${notification.actionUrl || '/Notifications'}" 
                   onclick="markAsRead(${notification.id}, event)"
                   style="white-space: normal;">
                    <div class="d-flex align-items-start">
                        <div class="flex-shrink-0 me-2 mt-1">
                            <i class="${iconClass}"></i>
                        </div>
                        <div class="flex-grow-1">
                            <div class="notification-title d-flex align-items-center">
                                ${notification.title}
                                ${priorityIndicator}
                                ${!notification.isRead ? '<span class="badge bg-primary ms-1">New</span>' : ''}
                            </div>
                            <div class="notification-message small text-muted">${notification.message}</div>
                            <div class="notification-time small text-muted">
                                <i class="fas fa-clock me-1"></i>${notification.timeAgo}
                            </div>
                        </div>
                    </div>
                </a>`;
                
            if(i < data.length - 1) {
                html += '<div class="dropdown-divider"></div>';
            }
        });
        $('#notificationContent').html(html);
    } else {
        const emptyHtml = `
            <div class="text-center p-3">
                <i class="fas fa-check-circle text-success mb-2"></i>
                <p class="mb-0 small">No new notifications</p>
                <p class="mb-0 small text-muted">You're all caught up!</p>
            </div>`;
        $('#notificationContent').html(emptyHtml);
    }
}

function getNotificationIcon(type) {
    switch(type) {
        case 0: return 'fas fa-play-circle text-success';
        case 1: return 'fas fa-check-circle text-primary';
        case 2: return 'fas fa-exclamation-triangle text-danger';
        case 3: return 'fas fa-check text-success';
        case 4: return 'fas fa-bell text-warning';
        case 5: return 'fas fa-clock text-info';
        default: return 'fas fa-info-circle text-secondary';
    }
}

function markAsRead(id, event) {
    // Prevent the default action initially
    if (event) {
        event.preventDefault();
    }
    
    // Send AJAX request to mark as read
    $.ajax({
        url: '/Notifications/MarkAsRead',
        type: 'POST',
        contentType: 'application/json',
        headers: {
            'Accept': 'application/json',
            'RequestVerificationToken': $('input:hidden[name="__RequestVerificationToken"]').val()
        },
        data: JSON.stringify({ id: id }),
        cache: false,
        success: function(response) {
            if(response && response.success) {
                // Update the notification count
                loadNotificationCount();
                
                // Navigate to the action URL if provided
                if (event && event.currentTarget && event.currentTarget.href) {
                    window.location.href = event.currentTarget.href;
                }
            }
        },
        error: function(xhr, status, error) {
            console.error('Failed to mark notification as read:', status, error);
            // Still navigate even if marking read fails
            if (event && event.currentTarget && event.currentTarget.href) {
                window.location.href = event.currentTarget.href;
            }
        }
    });
}

// Bulk task completion functionality
function openBulkCompleteModal() {
    const selectedProcesses = [];
    $('input[name="selectedProcesses"]:checked').each(function() {
        selectedProcesses.push(parseInt($(this).val()));
    });
    
    if (selectedProcesses.length === 0) {
        Swal.fire({
            title: 'No Processes Selected',
            text: 'Please select at least one process to bulk complete tasks.',
            icon: 'warning',
            confirmButtonText: 'OK'
        });
        return;
    }
    
    // Show modal and load tasks
    $('#bulkCompleteModal').modal('show');
    loadTasksForBulkComplete(selectedProcesses);
}

function loadTasksForBulkComplete(processIds) {
    const loadingHtml = `
        <div class="text-center p-3">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading tasks...</span>
            </div>
            <p class="mt-2">Loading tasks for selected processes...</p>
        </div>`;
    
    $('#tasksList').html(loadingHtml);
    
    $.ajax({
        url: '/OffboardingProcesses/GetTasksForProcesses',
        type: 'POST',
        contentType: 'application/json',
        headers: {
            'Accept': 'application/json',
            'RequestVerificationToken': $('input:hidden[name="__RequestVerificationToken"]').val()
        },
        data: JSON.stringify({ processIds: processIds }),
        success: function(response) {
            if (response.success && response.tasks) {
                displayTasksForBulkComplete(response.tasks);
            } else {
                $('#tasksList').html('<div class="alert alert-warning">No incomplete tasks found for selected processes.</div>');
            }
        },
        error: function() {
            $('#tasksList').html('<div class="alert alert-danger">Error loading tasks. Please try again.</div>');
        }
    });
}

function displayTasksForBulkComplete(tasks) {
    if (tasks.length === 0) {
        $('#tasksList').html('<div class="alert alert-info">No incomplete tasks found for the selected processes.</div>');
        return;
    }
    
    let html = '<div class="mb-3"><button type="button" class="btn btn-sm btn-outline-primary" onclick="toggleAllTasks()">Toggle All</button></div>';
    
    tasks.forEach(function(task) {
        const overdueClass = task.isOverdue ? 'text-danger' : '';
        const dueText = task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'No due date';
        
        html += `
            <div class="form-check mb-2">
                <input class="form-check-input task-checkbox" type="checkbox" value="${task.id}" id="task-${task.id}">
                <label class="form-check-label ${overdueClass}" for="task-${task.id}">
                    <strong>${task.employeeName}</strong> - ${task.taskName}
                    <br><small class="text-muted">Department: ${task.department} | Due: ${dueText}</small>
                </label>
            </div>`;
    });
    
    $('#tasksList').html(html);
}

function toggleAllTasks() {
    const checkboxes = $('.task-checkbox');
    const allChecked = checkboxes.length === checkboxes.filter(':checked').length;
    checkboxes.prop('checked', !allChecked);
}

function showNotificationToast(count) {
    // Create and show toast notification for new items
    console.log(`${count} new notification(s) received`);
}

function showHighPriorityNotificationToast(notification) {
    // Show toast for high priority notifications
    console.log('High priority notification:', notification.title);
}
