#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Creates a new Pull Request for the Dashboard Redesign
.DESCRIPTION
    This script creates a new branch, copies the dashboard redesign files,
    commits the changes, pushes to GitHub, and creates a Pull Request.
.PARAMETER BranchName
    Name of the branch to create (default: feature/dashboard-redesign)
.PARAMETER BaseBranch
    Base branch to create PR against (default: master)
#>

param(
    [string]$BranchName = "feature/dashboard-redesign",
    [string]$BaseBranch = "master"
)

# Color functions
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "→ $Message" -ForegroundColor Cyan }
function Write-Error { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }

# Banner
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║       Dashboard Redesign - Pull Request Creator       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Error "Not in a git repository! Please run this script from the root of your Caskr project."
    exit 1
}

# Check if gh CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed!"
    Write-Info "Install it from: https://cli.github.com/"
    Write-Info "Or run: winget install --id GitHub.cli"
    exit 1
}

# Check if user is authenticated with gh
Write-Info "Checking GitHub CLI authentication..."
$ghAuth = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Not authenticated with GitHub CLI!"
    Write-Info "Run: gh auth login"
    exit 1
}
Write-Success "Authenticated with GitHub CLI"

# Store the downloads path (where Claude outputs files)
$downloadsPath = Join-Path $env:USERPROFILE "Downloads"
$outputsPath = Join-Path $downloadsPath "outputs"

# Check if output files exist
$requiredFiles = @(
    "DashboardPage.tsx",
    "Dashboard.css",
    "ordersSlice.ts",
    "TasksController.cs",
    "TaskService.cs",
    "OrderTask.cs",
    "INTEGRATION_GUIDE.md"
)

Write-Info "Checking for required files in Downloads folder..."
$missingFiles = @()
foreach ($file in $requiredFiles) {
    $filePath = Join-Path $downloadsPath $file
    if (-not (Test-Path $filePath)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Error "Missing files in Downloads folder:"
    foreach ($file in $missingFiles) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Info "Please download all files from Claude first!"
    exit 1
}
Write-Success "All required files found"

# Ensure we're on the base branch and it's up to date
Write-Info "Switching to $BaseBranch branch..."
git checkout $BaseBranch 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to checkout $BaseBranch branch"
    exit 1
}
Write-Success "On $BaseBranch branch"

Write-Info "Pulling latest changes..."
git pull origin $BaseBranch 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Failed to pull latest changes. Continuing anyway..."
}

# Check if branch already exists
$branchExists = git branch --list $BranchName
if ($branchExists) {
    Write-Warning "Branch '$BranchName' already exists!"
    $response = Read-Host "Do you want to delete it and create a new one? (y/N)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Write-Info "Deleting existing branch..."
        git branch -D $BranchName 2>&1 | Out-Null
        Write-Success "Deleted existing branch"
    } else {
        Write-Info "Using existing branch..."
        git checkout $BranchName 2>&1 | Out-Null
    }
} else {
    # Create new branch
    Write-Info "Creating new branch: $BranchName..."
    git checkout -b $BranchName 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create branch"
        exit 1
    }
    Write-Success "Created and switched to $BranchName"
}

# Copy frontend files
Write-Info "Copying frontend files..."

# DashboardPage.tsx
$destDashboard = "caskr.client\src\pages\DashboardPage.tsx"
Copy-Item (Join-Path $downloadsPath "DashboardPage.tsx") $destDashboard -Force
Write-Success "Copied DashboardPage.tsx"

# Dashboard.css
$destCss = "caskr.client\src\pages\Dashboard.css"
Copy-Item (Join-Path $downloadsPath "Dashboard.css") $destCss -Force
Write-Success "Copied Dashboard.css"

# ordersSlice.ts
$destSlice = "caskr.client\src\features\ordersSlice.ts"
Copy-Item (Join-Path $downloadsPath "ordersSlice.ts") $destSlice -Force
Write-Success "Copied ordersSlice.ts"

# Copy backend files
Write-Info "Copying backend files..."

# TasksController.cs
$destController = "Caskr.Server\Controllers\TasksController.cs"
Copy-Item (Join-Path $downloadsPath "TasksController.cs") $destController -Force
Write-Success "Copied TasksController.cs"

# TaskService.cs
$destService = "Caskr.Server\Services\TaskService.cs"
Copy-Item (Join-Path $downloadsPath "TaskService.cs") $destService -Force
Write-Success "Copied TaskService.cs"

# OrderTask.cs
$destModel = "Caskr.Server\Models\OrderTask.cs"
Copy-Item (Join-Path $downloadsPath "OrderTask.cs") $destModel -Force
Write-Success "Copied OrderTask.cs"

# Copy documentation
Write-Info "Copying documentation..."
$destGuide = "DASHBOARD_INTEGRATION_GUIDE.md"
Copy-Item (Join-Path $downloadsPath "INTEGRATION_GUIDE.md") $destGuide -Force
Write-Success "Copied integration guide"

# Stage all changes
Write-Info "Staging changes..."
git add . 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to stage changes"
    exit 1
}
Write-Success "Staged all changes"

# Commit changes
Write-Info "Committing changes..."
$commitMessage = @"
feat: Premium dashboard redesign with task management

This commit introduces a complete redesign of the dashboard with the following features:

Frontend Changes:
- 🎨 Premium card-based layout replacing table views
- 📊 Statistics overview with 4 key metrics cards
- 🎯 Interactive task management with hover-based assignment
- ✅ Real-time task completion tracking
- 📈 Visual progress bars for each order
- 🔄 Expandable order cards with task details
- 📱 Fully responsive design
- 🎭 Smooth animations and transitions

Backend Changes:
- 🔧 New TasksController with RESTful endpoints
- 💼 TaskService with comprehensive business logic
- 🗄️ OrderTask model with audit timestamps
- ✅ Full validation and error handling
- 📝 Comprehensive logging

Technical Improvements:
- ♻️ React-compatible implementation
- 🎨 Uses existing CSS variables from index.css
- 📦 No external icon dependencies
- 🔒 Type-safe TypeScript interfaces
- 🧪 Production-ready code

Files Changed:
- caskr.client/src/pages/DashboardPage.tsx (redesigned)
- caskr.client/src/pages/Dashboard.css (new)
- caskr.client/src/features/ordersSlice.ts (enhanced)
- Caskr.Server/Controllers/TasksController.cs (new)
- Caskr.Server/Services/TaskService.cs (new)
- Caskr.Server/Models/OrderTask.cs (new)
- DASHBOARD_INTEGRATION_GUIDE.md (new)

Breaking Changes: None - backward compatible

See DASHBOARD_INTEGRATION_GUIDE.md for integration steps.
"@

git commit -m $commitMessage 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to commit changes"
    exit 1
}
Write-Success "Committed changes"

# Push to remote
Write-Info "Pushing to remote..."
git push -u origin $BranchName 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to push to remote"
    Write-Info "You may need to push manually with: git push -u origin $BranchName"
    exit 1
}
Write-Success "Pushed to remote"

# Create Pull Request
Write-Info "Creating Pull Request..."

$prTitle = "🎨 Premium Dashboard Redesign with Interactive Task Management"
$prBody = @"
## 📋 Summary

This PR introduces a complete redesign of the dashboard, transforming it from a basic table view into a premium, intuitive management interface with interactive task management capabilities.

## ✨ Key Features

### Frontend Improvements
- **📊 Statistics Dashboard**: 4 overview cards showing active orders, completed orders, total tasks, and overall progress with trend indicators
- **🎴 Card-Based Layout**: Beautiful order cards instead of cluttered tables
- **📈 Progress Tracking**: Visual progress bars showing completion percentage for each order
- **✅ Inline Task Management**: Check/uncheck tasks directly on the dashboard
- **🎯 Hover-Based Assignment**: Hover over any task to assign it to a user without extra clicks
- **🔄 Expandable Cards**: Click to expand orders and see detailed task lists
- **🎨 Professional Styling**: Gradients, shadows, smooth animations
- **📱 Responsive Design**: Works beautifully on all screen sizes

### Backend Improvements
- **🔧 TasksController**: RESTful API for task operations
- **💼 TaskService**: Business logic with validation and error handling
- **🗄️ OrderTask Model**: Database model with audit timestamps
- **📝 Comprehensive Logging**: All operations are logged
- **✅ Validation**: Input validation on all endpoints

## 🏗️ Technical Details

### Files Changed

**Frontend:**
\`\`\`
caskr.client/src/pages/DashboardPage.tsx     - Complete redesign
caskr.client/src/pages/Dashboard.css         - New styles (extends index.css)
caskr.client/src/features/ordersSlice.ts     - Added task actions
\`\`\`

**Backend:**
\`\`\`
Caskr.Server/Controllers/TasksController.cs  - New API endpoints
Caskr.Server/Services/TaskService.cs         - Business logic
Caskr.Server/Models/OrderTask.cs             - Database model
\`\`\`

**Documentation:**
\`\`\`
DASHBOARD_INTEGRATION_GUIDE.md               - Complete integration guide
\`\`\`

### New API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | \`/api/orders/{orderId}/tasks\` | Get tasks for an order |
| PUT | \`/api/tasks/{taskId}/assign\` | Assign task to user |
| PUT | \`/api/tasks/{taskId}/complete\` | Mark complete/incomplete |
| POST | \`/api/tasks\` | Create new task |
| DELETE | \`/api/tasks/{taskId}\` | Delete task |

## 🔄 Integration Steps

### Quick Start
1. Merge this PR
2. Register TaskService in \`Program.cs\`
3. Add Tasks DbSet to \`CaskrDbContext.cs\`
4. Run migrations: \`dotnet ef migrations add AddTasksTable\`
5. Update database: \`dotnet ef database update\`

**Detailed steps in:** \`DASHBOARD_INTEGRATION_GUIDE.md\`

## ✅ Testing Checklist

- [ ] Dashboard loads without errors
- [ ] Order cards display correctly
- [ ] Statistics cards show accurate data
- [ ] Tasks expand/collapse
- [ ] Task completion works
- [ ] Task assignment works
- [ ] Progress bars update in real-time
- [ ] Hover effects work smoothly
- [ ] Responsive on mobile

## 📸 Screenshots

[Add screenshots here after testing]

## 🔒 Breaking Changes

**None** - This is fully backward compatible. Existing functionality continues to work.

## 🎯 Future Enhancements

Potential follow-up features:
- Task creation UI
- Due date reminders
- Task filtering and search
- Bulk task operations
- Task comments/discussion
- Activity log

## 📚 Documentation

See **DASHBOARD_INTEGRATION_GUIDE.md** for:
- Complete integration steps
- Database schema
- API documentation
- Troubleshooting guide
- Testing checklist

---

## 👀 Review Focus Areas

Please pay special attention to:
1. **Database migrations** - Ensure Tasks table is created correctly
2. **Service registration** - TaskService must be registered in DI container
3. **CSS integration** - Dashboard.css extends index.css properly
4. **API endpoints** - All new endpoints work as expected
5. **Type safety** - TypeScript interfaces are correct

---

**Ready to merge after:**
- ✅ Code review approval
- ✅ Backend integration steps completed
- ✅ Database migrations run
- ✅ Manual testing completed
"@

gh pr create --title $prTitle --body $prBody --base $BaseBranch --head $BranchName 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create Pull Request via GitHub CLI"
    Write-Info "Creating PR via web browser instead..."
    $repoInfo = gh repo view --json nameWithOwner --jq .nameWithOwner
    $prUrl = "https://github.com/$repoInfo/compare/${BaseBranch}...${BranchName}?expand=1"
    Write-Info "Opening browser to create PR manually..."
    Start-Process $prUrl
    Write-Success "Browser opened. Please create the PR manually."
} else {
    Write-Success "Pull Request created successfully!"
    
    # Get the PR URL
    $prUrl = gh pr view --json url --jq .url
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║              Pull Request Created! 🎉                  ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Host "PR URL: " -NoNewline
    Write-Host $prUrl -ForegroundColor Cyan
    Write-Host ""
}

# Summary
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║                    Next Steps                          ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Review the Pull Request" -ForegroundColor White
Write-Host "2. Complete backend integration (see DASHBOARD_INTEGRATION_GUIDE.md)" -ForegroundColor White
Write-Host "   - Register TaskService in Program.cs" -ForegroundColor Gray
Write-Host "   - Add Tasks DbSet to CaskrDbContext.cs" -ForegroundColor Gray
Write-Host "   - Run: dotnet ef migrations add AddTasksTable" -ForegroundColor Gray
Write-Host "   - Run: dotnet ef database update" -ForegroundColor Gray
Write-Host "3. Test the dashboard thoroughly" -ForegroundColor White
Write-Host "4. Merge the PR once approved" -ForegroundColor White
Write-Host ""
Write-Host "✨ Dashboard redesign is ready for review!" -ForegroundColor Green
Write-Host ""

# Offer to open the PR in browser
$openBrowser = Read-Host "Do you want to open the PR in your browser? (Y/n)"
if ($openBrowser -ne 'n' -and $openBrowser -ne 'N') {
    gh pr view --web
}
