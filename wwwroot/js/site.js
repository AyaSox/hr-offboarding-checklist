// Initialize page
$(document).ready(function() {
    // Initialize toasts
    $('.toast').each(function() {
        new bootstrap.Toast(this).show();
    });
    
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
let lastNotificationCount = 0;
let lastNotificationCheck = new Date();

function initializeRealTimeFeatures() {
    // Check for new notifications immediately
    setTimeout(checkForNewNotifications, 5000);
}

function loadNotificationCount() {
    $.ajax({
        url: '/Notifications/GetUnreadCount',
        type: 'GET',
        success: function(data) {
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
        error: function() {
            console.warn('Could not load notification count');
        }
    });
}

function checkForNewNotifications() {
    $.ajax({
        url: '/Notifications/GetLatest',
        type: 'GET',
        data: { since: lastNotificationCheck.toISOString() },
        success: function(data) {
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
        error: function() {
            console.warn('Could not check for new notifications');
        }
    });
}

function showNotificationToast(count) {
    const toastHtml = `
        <div class="toast" role="alert" aria-live="polite" aria-atomic="true" data-bs-delay="5000">
            <div class="toast-header bg-info text-white">
                <i class="fas fa-bell me-2"></i>
                <strong class="me-auto">New Notification${count > 1 ? 's' : ''}</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                You have ${count} new notification${count > 1 ? 's' : ''}. Click the bell icon to view.
            </div>
        </div>`;
    
    const toastContainer = $('.toast-container');
    const toastElement = $(toastHtml);
    toastContainer.append(toastElement);
    
    const toast = new bootstrap.Toast(toastElement[0]);
    toast.show();
    
    // Remove toast element after it hides
    toastElement.on('hidden.bs.toast', function() {
        $(this).remove();
    });
}

function showHighPriorityNotificationToast(notification) {
    const priorityText = notification.priority === 4 ? 'Critical' : 'High Priority';
    const bgClass = notification.priority === 4 ? 'bg-danger' : 'bg-warning';
    
    const toastHtml = `
        <div class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="8000">
            <div class="toast-header ${bgClass} text-white">
                <i class="fas fa-exclamation-triangle me-2"></i>
                <strong class="me-auto">${priorityText}</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                <strong>${notification.title}</strong><br>
                ${notification.message}
                ${notification.actionUrl ? `<br><a href="${notification.actionUrl}" class="text-decoration-none">View Details</a>` : ''}
            </div>
        </div>`;
    
    const toastContainer = $('.toast-container');
    const toastElement = $(toastHtml);
    toastContainer.append(toastElement);
    
    const toast = new bootstrap.Toast(toastElement[0]);
    toast.show();
    
    // Remove toast element after it hides
    toastElement.on('hidden.bs.toast', function() {
        $(this).remove();
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
    
    $.ajax({
        url: '/Notifications/GetLatest',
        type: 'GET',
        success: function(data) {
            displayNotifications(data);
        },
        error: function(xhr, status, error) {
            console.error('Failed to load notifications:', error);
            const errorHtml = `
                <div class="text-center p-3">
                    <i class="fas fa-exclamation-circle text-danger mb-2"></i>
                    <p class="mb-0 small">Could not load notifications</p>
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
        data: JSON.stringify({ id: id }),
        headers: {
            'RequestVerificationToken': $('input:hidden[name="__RequestVerificationToken"]').val()
        },
        success: function(response) {
            if(response.success) {
                // Update the notification count
                loadNotificationCount();
                
                // Navigate to the action URL if provided
                if (event && event.currentTarget && event.currentTarget.href) {
                    window.location.href = event.currentTarget.href;
                }
            }
        },
        error: function(xhr, status, error) {
            console.error('Failed to mark notification as read:', error);
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
        data: JSON.stringify({ processIds: processIds }),
        headers: {
            'RequestVerificationToken': $('input:hidden[name="__RequestVerificationToken"]').val()
        },
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
