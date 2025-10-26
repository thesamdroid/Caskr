# Dashboard Redesign - Integration Guide

## Overview
This guide will walk you through integrating the new premium dashboard into your existing Caskr application.

---

## File Structure

```
Caskr/
├── caskr.client/src/
│   ├── pages/
│   │   └── DashboardPage.tsx          [REPLACE]
│   ├── features/
│   │   └── ordersSlice.ts             [REPLACE]
│   ├── styles/
│   │   └── Dashboard.css              [NEW]
│   └── index.css                      [Already exists - no changes needed]
│
└── Caskr.Server/
    ├── Controllers/
    │   └── TasksController.cs         [NEW]
    ├── Services/
    │   └── TaskService.cs             [NEW]
    └── Models/
        └── OrderTask.cs               [NEW]
```

---

## Step 1: Frontend Integration

### 1.1 Replace DashboardPage Component

**Location:** `caskr.client/src/pages/DashboardPage.tsx`

**Action:** Replace the existing file with the new `DashboardPage.tsx`

**What Changed:**
- Premium card-based layout instead of tables
- Interactive task management with hover effects
- Real-time progress tracking
- Statistics overview cards
- Expandable order details

### 1.2 Add Dashboard CSS

**Location:** `caskr.client/src/styles/Dashboard.css` (create this folder if needed)

**Action:** Add the new `Dashboard.css` file

**Alternative Location:** You can also put it in the same directory as DashboardPage:
- `caskr.client/src/pages/Dashboard.css`

**Important:** Make sure to import it in DashboardPage.tsx:
```typescript
import './Dashboard.css';  // or '../styles/Dashboard.css' depending on location
```

### 1.3 Update Redux Slice

**Location:** `caskr.client/src/features/ordersSlice.ts`

**Action:** Replace the existing file with the new `ordersSlice.ts`

**What Changed:**
- Added `assignTask` thunk for assigning tasks to users
- Added `completeTask` thunk for marking tasks complete/incomplete
- Updated Task interface with proper types
- Enhanced state management for task operations

---

## Step 2: Backend Integration

### 2.1 Add Task Controller

**Location:** `Caskr.Server/Controllers/TasksController.cs`

**Action:** Add the new controller file

**Endpoints Created:**
- `GET /api/tasks/order/{orderId}` - Get all tasks for an order
- `PUT /api/tasks/{taskId}/assign` - Assign a task to a user
- `PUT /api/tasks/{taskId}/complete` - Mark task as complete/incomplete
- `POST /api/tasks` - Create a new task
- `DELETE /api/tasks/{taskId}` - Delete a task

### 2.2 Add Task Service

**Location:** `Caskr.Server/Services/TaskService.cs`

**Action:** Add the new service file

**What It Does:**
- Handles all task business logic
- Validates task operations
- Manages task assignment and completion
- Provides comprehensive error handling

### 2.3 Add Task Model

**Location:** `Caskr.Server/Models/OrderTask.cs`

**Action:** Add the new model file

**Database Fields:**
- `Id` - Primary key
- `Name` - Task name
- `OrderId` - Foreign key to Order
- `AssigneeId` - Foreign key to User (nullable)
- `IsComplete` - Completion status
- `DueDate` - Optional due date
- `CreatedAt`, `UpdatedAt`, `CompletedAt` - Audit timestamps

### 2.4 Register Services

**Location:** `Caskr.Server/Program.cs`

**Action:** Add service registration

```csharp
// Add this line with your other service registrations
builder.Services.AddScoped<ITaskService, TaskService>();
```

### 2.5 Update Database Context

**Location:** `Caskr.Server/Models/CaskrDbContext.cs`

**Action:** Add DbSet for tasks

```csharp
public DbSet<OrderTask> Tasks { get; set; }
```

### 2.6 Create Database Migration

**Run these commands in the Server project directory:**

```bash
# Create migration
dotnet ef migrations add AddTasksTable

# Update database
dotnet ef database update
```

---

## Step 3: Update Existing API Endpoint

### 3.1 Update Orders Controller

**Location:** `Caskr.Server/Controllers/OrdersController.cs`

**Action:** Add endpoint to fetch tasks for an order

```csharp
[HttpGet("{orderId}/tasks")]
public async Task<ActionResult<IEnumerable<OrderTask>>> GetOrderTasks(int orderId)
{
    var tasks = await _taskService.GetTasksByOrderIdAsync(orderId);
    return Ok(tasks);
}
```

**Important:** Inject ITaskService into the OrdersController constructor:

```csharp
private readonly ITaskService _taskService;

public OrdersController(
    IOrderService orderService, 
    ITaskService taskService,  // Add this
    ILogger<OrdersController> logger)
{
    _orderService = orderService;
    _taskService = taskService;  // Add this
    _logger = logger;
}
```

---

## Step 4: Verify CSS Integration

### 4.1 Check index.css

The new Dashboard.css is designed to work with your existing `index.css` file. It uses the CSS variables already defined:

- `--primary-blue`
- `--primary-blue-light`
- `--secondary-orange`
- `--success-green`
- `--gray-*` colors
- `--white`

**No changes needed to index.css** - Dashboard.css extends it seamlessly.

### 4.2 Import Order

Make sure CSS files are imported in this order:

```typescript
// In DashboardPage.tsx
import './Dashboard.css';  // Dashboard-specific styles
```

Your root `index.css` is already imported globally in `main.tsx`, so you're good!

---

## Step 5: Test the Integration

### 5.1 Start the Application

```bash
# Start backend
cd Caskr.Server
dotnet run

# Start frontend (in a new terminal)
cd caskr.client
npm run dev
```

### 5.2 Verify Features

Test each feature to ensure everything works:

1. **Dashboard Loads** ✓
   - Visit http://localhost:5173/
   - Should see stat cards at the top
   - Should see order cards below

2. **Order Cards Display** ✓
   - Each order should show as a card
   - Status badge should be colored correctly
   - Progress bar should show completion percentage

3. **Expand/Collapse Orders** ✓
   - Click the › button on any order card
   - Tasks list should slide down
   - Icon should rotate 90 degrees

4. **Task Completion** ✓
   - Check/uncheck task checkboxes
   - Task should update immediately
   - Progress bar should update

5. **Task Assignment** ✓
   - Hover over an unassigned task
   - Dropdown should appear
   - Select a user
   - User name should display below task
   - Hover again to see "Unassign" button

---

## Troubleshooting

### Issue: CSS not loading

**Solution:** Make sure you imported Dashboard.css in DashboardPage.tsx:
```typescript
import './Dashboard.css';
```

### Issue: Tasks not showing

**Solution:** Check that:
1. `fetchOutstandingTasks` is being called for each order
2. Backend endpoint `/api/orders/{orderId}/tasks` is working
3. Database has the Tasks table

### Issue: Assignment not working

**Solution:** Verify:
1. Users are loaded (`fetchUsers` is called)
2. Backend `/api/tasks/{taskId}/assign` endpoint exists
3. TaskService is registered in Program.cs

### Issue: TypeScript errors

**Solution:** Ensure all interfaces are properly exported in ordersSlice.ts:
```typescript
export interface Task {
  id: number;
  name: string;
  orderId: number;
  assigneeId: number | null;
  isComplete: boolean;
  dueDate?: string;
}
```

---

## Database Schema

### Tasks Table

```sql
CREATE TABLE Tasks (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    OrderId INT NOT NULL,
    AssigneeId INT NULL,
    IsComplete BIT NOT NULL DEFAULT 0,
    DueDate DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    FOREIGN KEY (AssigneeId) REFERENCES Users(Id)
);
```

---

## API Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/orders/{orderId}/tasks` | Get tasks for an order |
| PUT | `/api/tasks/{taskId}/assign` | Assign task to user |
| PUT | `/api/tasks/{taskId}/complete` | Mark complete/incomplete |
| POST | `/api/tasks` | Create new task |
| DELETE | `/api/tasks/{taskId}` | Delete task |

---

## Features Summary

✅ **Statistics Dashboard** - 4 overview cards with trend indicators
✅ **Order Cards** - Beautiful card layout instead of tables
✅ **Progress Tracking** - Visual progress bars for each order
✅ **Task Management** - Inline task completion and assignment
✅ **Hover Interactions** - Assign tasks without extra clicks
✅ **Real-time Updates** - Instant feedback on all actions
✅ **Responsive Design** - Works on all screen sizes
✅ **Professional Styling** - Gradients, shadows, smooth animations
✅ **Accessibility** - Proper ARIA labels and keyboard navigation

---

## Next Steps

After successful integration, consider:

1. **Add Task Creation UI** - Allow users to add tasks to orders
2. **Due Date Reminders** - Highlight overdue tasks
3. **Task Filtering** - Filter by assignee, status, or date
4. **Bulk Operations** - Select multiple tasks for bulk actions
5. **Task Comments** - Add discussion threads to tasks
6. **Activity Log** - Track who completed/assigned tasks and when

---

## Need Help?

If you encounter any issues:
1. Check the browser console for errors
2. Check the server logs for API errors
3. Verify all database migrations ran successfully
4. Ensure all required files are in the correct locations

---

## Checklist

Use this checklist to track your integration progress:

Frontend:
- [ ] Replaced `DashboardPage.tsx`
- [ ] Added `Dashboard.css`
- [ ] Imported CSS in component
- [ ] Replaced `ordersSlice.ts`

Backend:
- [ ] Added `TasksController.cs`
- [ ] Added `TaskService.cs`
- [ ] Added `OrderTask.cs`
- [ ] Registered TaskService in Program.cs
- [ ] Added DbSet to DbContext
- [ ] Created database migration
- [ ] Updated database
- [ ] Updated OrdersController with tasks endpoint

Testing:
- [ ] Dashboard loads without errors
- [ ] Order cards display correctly
- [ ] Tasks expand/collapse
- [ ] Task completion works
- [ ] Task assignment works
- [ ] Progress bars update
- [ ] All hover effects work

You're done! 🎉
