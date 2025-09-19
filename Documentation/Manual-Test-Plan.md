Manual Test Plan - OffboardingChecklist

Notes
- Use the seeded accounts (from DbInitializer):
  - Admin: admin@company.co.za / Admin123!
  - HR: hr@company.co.za / Hr123!
  - User: user@company.co.za / User123!
- Place screenshots under Documentation/Screenshots following suggested names.
- For each step, capture the suggested screenshot.

1) Environment & Health
- Navigate to /health and verify status Healthy (200).
  - Screenshot: health-endpoint.png
- Open /api-docs Swagger UI is available.
  - Screenshot: swagger-ui.png

2) Authentication & Roles
- Login as User; verify no Management menu.
  - Screenshot: login-user-dashboard.png
- Logout; login as HR, then as Admin; verify Management menu visible and Analytics link.
  - Screenshot: login-hr-dashboard.png, login-admin-dashboard.png

3) Home Dashboard
- Verify Quick Actions and System Status cards.
  - Screenshot: home-quick-actions.png

4) Offboarding Process - Create (User)
- As User, go to OffboardingProcesses > Create.
- Fill EmployeeName, JobTitle, EmploymentStartDate, LastWorkingDay; submit.
- Expect success toast and redirect to list with Pending Approval (visible to HR/Admin).
  - Screenshot: create-form.png, create-success-toast.png

5) Approvals (HR/Admin)
- As HR, open Processes list. Pending approval badge and alert should be visible.
  - Screenshot: pending-approval-list.png
- Open Details of the pending process. Approve via Approve button; confirm SweetAlert.
- Expect success toast, tasks created from templates or defaults.
  - Screenshot: details-before-approve.png, approve-confirm.png, details-after-approve.png
- Negative: As User, POST ApproveProcess (e.g., via button if visible). Should be forbidden/not allowed.
  - Screenshot: approve-forbidden.png (optional)

6) Tasks - Complete / Undo / Dependencies
- In Details (active process):
  - Complete a task with a comment; observe toast and the task row turns Completed.
    - Screenshot: task-complete-form.png, task-completed.png
  - Try completing a task that depends on another (if configured). Expect error toast.
    - Screenshot: dependency-error.png
  - As same user who completed, undo completion; or as HR, undo. Expect warning toast.
    - Screenshot: task-undo.png

7) Bulk Complete
- From Processes list, select multiple active processes with incomplete tasks.
- Open Bulk Complete modal, load tasks, select a subset, add bulk comment, submit.
- Expect success toast; verify tasks now completed.
  - Screenshot: bulk-modal-loaded-tasks.png, bulk-complete-success.png

8) Filters, Sorting, Date Range, Pagination
- Use search, status, department filters; adjust sort by Date/Progress.
- Use Start From/Start To to limit by StartDate.
- Change page size to 20; navigate with pager.
  - Screenshot: filters-in-use.png, pagination-controls.png

9) Export CSV
- With filters applied, click Export CSV.
- Open the CSV and verify rows match current filters.
  - Screenshot: export-csv-download.png

10) Documents
- In Details (active process): upload a document (e.g., PDF); expect success toast.
  - Screenshot: document-upload-form.png, document-list.png
- Download a document; delete a document; verify list updates.
  - Screenshot: document-download.png, document-delete-confirm.png
- Hit /Documents/DownloadAll?processId={id} to download ZIP of all docs.
  - Screenshot: documents-zip-download.png

11) Close Process
- Complete all tasks for a process.
- Click Close Process (HR/Admin only); confirm SweetAlert.
- Expect success toast; status becomes Closed; verify emails/notifications (see below).
  - Screenshot: close-confirm.png, closed-status.png
- Negative: Try closing with pending tasks; should show warning with pending count.
  - Screenshot: close-denied-pending.png

12) Notifications
- Trigger events (approve, task complete, close) and verify unread count badge updates.
  - Screenshot: navbar-notification-badge.png
- Open notifications dropdown; verify latest items, icons, priorities.
  - Screenshot: notifications-dropdown.png
- Click an item (marks as read) and verify it no longer shows as unread or count decreases.
  - Screenshot: notification-marked-read.png
- Go to Notifications page; Mark All as Read and Delete a notification.
  - Screenshot: notifications-index.png

13) Analytics (HR/Admin only)
- As HR/Admin, open Analytics; verify cards and charts render.
  - Screenshot: analytics-dashboard.png
- Negative: As User, accessing /Analytics should be denied.
  - Screenshot: analytics-unauthorized.png

14) Background Service (Reminders)
- Create an active process with a task having DueDate in the past to simulate overdue.
- Wait for service interval or trigger manually (optional); verify overdue notifications/emails logged.
  - Screenshot: overdue-task-indicator.png (in UI), logs if available.

15) API Spot Checks (Swagger)
- Test GET /Notifications/GetUnreadCount while logged in.
  - Screenshot: swagger-unreadcount.png
- Test POST /OffboardingProcesses/GetTasksForProcesses with sample IDs; expect JSON list.
  - Screenshot: swagger-gettasks.png

16) Authorization Matrix Quick Checks
- User cannot Approve/Reject/Close (403 or redirected with error toast).
- HR/Admin can Approve/Reject/Close and see Management/Analytics.
  - Screenshot: role-based-ui.png

17) UI/UX Polishing
- Verify toast auto-dismiss behavior for Success/Error/Warning/Info.
  - Screenshot: toasts-examples.png
- Check responsiveness on a narrow viewport (navbar collapses, tables scroll).
  - Screenshot: responsive-mobile.png

18) Error Handling
- Trigger a concurrency error (e.g., complete same task from two sessions) and verify message.
  - Screenshot: concurrency-error.png
- Try downloading a non-existent document; expect "File not found" and redirect.
  - Screenshot: file-not-found-toast.png

19) Final Demo Flow (recommended order)
- Login as User -> Create request.
- Login as HR -> Approve -> Tasks created.
- Complete a few tasks and bulk complete.
- Upload documents; download all ZIP.
- View Analytics; export CSV from Processes.
- Close process after tasks done.
- Show Notifications.
  - Screenshot: demo-collage.png

Screenshot storage
- Create folders under Documentation/Screenshots per section, e.g.:
  - Documentation/Screenshots/01-Health
  - Documentation/Screenshots/02-Auth
  - Documentation/Screenshots/03-Home
  - ... (continue for each numbered section)
- Use simple names as suggested above.