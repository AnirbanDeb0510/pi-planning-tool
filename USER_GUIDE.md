# PI Planning Tool - User Guide

**Version:** 1.0  
**Last Updated:** March 7, 2026  
**Audience:** Product Owners, Scrum Masters, Development Teams

---

## 📋 Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Creating a Board](#creating-a-board)
4. [Importing Features from Azure DevOps](#importing-features-from-azure-devops)
5. [Planning User Stories](#planning-user-stories)
6. [Managing Team Capacity](#managing-team-capacity)
7. [Real-Time Collaboration](#real-time-collaboration)
8. [Board Lock & Unlock](#board-lock--unlock)
9. [Finalizing the Plan](#finalizing-the-plan)
10. [Common Workflows](#common-workflows)
11. [Tips & Best Practices](#tips--best-practices)
12. [Troubleshooting](#troubleshooting)

---

## Introduction

### What is the PI Planning Tool?

The PI Planning Tool is a web-based application designed to help Agile teams plan Program Increments (PIs) collaboratively. Think of it as a digital planning board that integrates directly with Azure DevOps, allowing teams to:

- **Import Features and User Stories** from Azure Boards
- **Drag and drop stories** across sprints
- **Track team capacity** and load per sprint
- **Work together in real-time** with live cursor tracking
- **Lock plans** to prevent accidental changes
- **Finalize plans** with clear indicators for post-planning changes

### Who Should Use This Tool?

- **Product Owners**: Prioritize features and manage PI scope
- **Scrum Masters**: Facilitate planning sessions and track capacity
- **Development Teams**: Assign stories to sprints and estimate effort
- **Stakeholders**: Review planned features and timelines

### Key Features at a Glance

| Feature               | Description                                           |
| --------------------- | ----------------------------------------------------- |
| **Azure Integration** | Fetch Features and Stories directly from Azure Boards |
| **Interactive Board** | Drag-and-drop interface for sprint planning           |
| **Team Capacity**     | Track Dev/Test capacity vs. load per sprint           |
| **Real-Time Sync**    | See other users' cursors and edits live               |
| **Board Locking**     | Password-protect boards to prevent changes            |
| **Finalization**      | Mark boards as finalized with change tracking         |
| **Search & Filter**   | Find boards by organization, project, or status       |

---

## Getting Started

### Accessing the Tool

**Local Development:**

- Frontend: `http://localhost:4200`
- Backend API: `http://localhost:5262`
- Swagger UI: `http://localhost:5262/swagger`

**Docker Deployment:**

- Frontend: `http://localhost:4200`
- Backend API: `http://localhost:8080`

**IIS Deployment (Windows):**

- Frontend: `http://localhost/PIPlanningUI`
- Backend API: `http://localhost/PIPlanningBackend`

### System Requirements

- **Browser**: Chrome 90+, Firefox 88+, Edge 90+, Safari 14+
- **Network**: WebSocket support required for real-time features
- **Azure Access**: Personal Access Token (PAT) with `vso.work` scope

### First-Time Setup

1. **Navigate to the application URL**
2. **Enter a name** when prompted (stored in browser session)
3. **Obtain an Azure DevOps PAT** (required to import features)

---

## Creating a Board

### Step 1: Start a New Board

1. Click the **"Create New Board"** button on the home page
2. Fill out the board creation form:

**Board Details:**

```
Name: PI 2024 Q2
Organization: your-azure-org
Project: YourProject
```

**Sprint Configuration:**

```
Number of Sprints: 5
Sprint Duration: 2 weeks
Start Date: 2024-04-01
```

**Options:**

```
☐ Dev/Test Toggle: OFF (show total story points)
☐ Lock on Creation: OFF (optional password protection)
```

3. Click **"Create Board"**

### What Happens Next?

The system automatically:

- Creates **5 sprints** (Sprint 1 through Sprint 5)
- Calculates sprint dates based on start date and duration
- Generates a **Placeholder column** for unassigned stories
- Assigns a unique board ID

### Understanding the Board Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Board Header: PI 2024 Q2                      [Lock] [Menu] │
├──────────────────────────────────────────────────────────────┤
│  Feature     │ Placeholder │ Sprint 1 │ Sprint 2 │ Sprint 3 │
├──────────────┼─────────────┼──────────┼──────────┼──────────┤
│  Auth        │   Story A   │ Story B  │          │          │
│  (Feature 1) │             │          │          │          │
├──────────────┼─────────────┼──────────┼──────────┼──────────┤
│  Dashboard   │   Story C   │          │ Story D  │          │
│  (Feature 2) │   Story E   │          │          │          │
└──────────────┴─────────────┴──────────┴──────────┴──────────┘
```

- **Rows**: Features (horizontal)
- **Columns**: Placeholder + Sprints (vertical)
- **Cards**: User Stories (draggable)

---

## Importing Features from Azure DevOps

### Step 1: Obtain an Azure DevOps PAT

1. Go to **Azure DevOps** → User Settings → **Personal Access Tokens**
2. Click **"New Token"**
3. Set scopes: **Work Items (Read)** (`vso.work`)
4. Copy the generated token (required in the next step)

**Note:** PATs are stored temporarily (10 minutes) and never persisted to the database.

### Step 2: Fetch a Feature

1. On the board, click **"Import from Azure"** button
2. Enter the Feature ID from Azure Boards (e.g., `12345`)
3. Paste the **PAT** in the token field
4. Click **"Fetch Feature"**

**Example:**

```
Organization: my-org
Project: MyProject
Feature ID: 12345
PAT: abcdef1234567890abcdef1234567890
```

### Step 3: Preview and Import

The tool will display:

- Feature title and description
- All child User Stories
- Story points (total or Dev/Test split)

Review the data and click **"Import to Board"**.

### What Gets Imported?

| Azure Field      | PI Tool Mapping                |
| ---------------- | ------------------------------ |
| Feature Title    | Feature name (row header)      |
| User Story Title | Story card title               |
| Story Points     | Total points or Dev/Test split |
| Work Item ID     | Azure ID (linked for tracking) |

**Result:** Feature appears as a new row, stories land in the **Placeholder** column.

### Importing Multiple Features

Repeat the process for each feature. Features are automatically:

- Prioritized sequentially (Feature 1, Feature 2, etc.)
- Positioned at the bottom of the board
- Reorderable via drag-and-drop or priority menu

---

## Planning User Stories

### Drag-and-Drop Stories

**How to Move Stories:**

1. Click and hold a **story card** in any column
2. Drag it to the target **sprint column**
3. Release to drop

### Understanding Story Cards

```
┌────────────────────────────────────┐
│ Login Page                         │  ← Story Title
│ #12346                             │  ← Azure ID
│ ● 5 pts  │  Dev: 3  │  Test: 2   │  ← Story Points
└────────────────────────────────────┘
```

**Card Colors:**

- **White**: Unassigned (in Placeholder)
- **Blue**: Assigned to a sprint
- **Yellow Border**: Moved after finalization (warning indicator)

### Viewing Sprint Load

Each sprint column header shows:

```
Sprint 1
Apr 1 - Apr 14
─────────────
Load:  24 pts
Capacity: 80 pts
─────────────
Utilization: 30%
```

**Color Indicators:**

- **Green**: Under capacity (<80%)
- **Yellow**: Near capacity (80-100%)
- **Red**: Over capacity (>100%)

---

## Managing Team Capacity

### Adding Team Members

1. Click **"Manage Team"** in the board header
2. Enter team member details:

```
Name: Alice Smith
☑ Developer (Dev)
☐ Tester (Test)
```

3. Click **"Add Member"**

**Auto-Capacity Calculation:**

- **Dev capacity**: `Sprint Duration × 40 hours × 1.0` (if Dev checked)
- **Test capacity**: `Sprint Duration × 40 hours × 1.0` (if Test checked)

**Example:**

- 2-week sprint → 80 hours Dev capacity
- Alice (Dev only) → 80 hrs Dev, 0 hrs Test per sprint

### Editing Team Member Roles

1. Find the team member in the **Team Panel**
2. Click **"Edit"**
3. Toggle Dev/Test checkboxes as needed
4. Click **"Save"**

**Note:** Changing roles recalculates default capacities for all sprints.

### Adjusting Sprint-Specific Capacity

**Scenario:** Alice is on vacation during Sprint 2.

1. Go to **Team Panel** → Find **Alice**
2. Find **Sprint 2** row
3. Click capacity values to edit:

```
Sprint 2
Dev: 80 → 0 hours    (vacation)
Test: 0 → 0 hours
```

4. Press Enter to save

### Capacity vs. Load

**Capacity:** Total hours available from team members  
**Load:** Sum of story points assigned to the sprint

**Formula:**

```
Utilization = (Load / Capacity) × 100%
```

**Healthy Range:** 70-90% utilization per sprint

---

## Real-Time Collaboration

### How It Works

When multiple users open the same board:

- **Live Presence**: See who else is viewing the board
- **Cursor Tracking**: View other users' cursor positions in real-time
- **Instant Updates**: Changes broadcast immediately (story moves, capacity edits, feature imports)

### Active Users Panel

Located in the top-right corner:

```
👤 Alice Smith
👤 Bob Johnson (You)
👤 Carol Lee
```

**Cursor Labels:**

- Hover over a remote cursor to see the user's name
- Cursors fade after 3 seconds of inactivity
- Reappear on movement

### Sync Events

All mutation operations sync automatically:

| Action            | What Syncs                          |
| ----------------- | ----------------------------------- |
| Story Move        | Card position updates for all users |
| Feature Import    | New feature row appears instantly   |
| Team Member Added | Team panel updates everywhere       |
| Capacity Change   | Sprint load recalculates live       |
| Board Locked      | Editing disabled for all users      |

### Connection Status

**Indicator:** Bottom-right corner

- 🟢 **Connected**: Real-time sync active
- 🟡 **Reconnecting**: Network interruption, retrying...
- 🔴 **Disconnected**: Refresh page or check network

**Auto-Reconnect:** System automatically reconnects after 5 seconds with exponential backoff.

---

## Board Lock & Unlock

### When to Lock a Board

Use board locking to:

- **Prevent accidental changes** after initial planning
- **Freeze the plan** during executive reviews
- **Enforce approval workflows** (only authorized users can unlock)

### Locking a Board

1. Click **"Lock Board"** in the board header
2. Enter a password (6-100 characters):

```
Password: my-secure-password-123
```

3. Click **"Lock"**

**What Gets Locked:**

- ❌ Story moves (drag-and-drop disabled)
- ❌ Feature imports
- ❌ Team capacity edits
- ❌ Feature reordering
- ❌ Story/feature deletion

**What Still Works:**

- ✅ Viewing board (read-only)
- ✅ Searching boards
- ✅ Exporting data (future feature)

### Unlocking a Board

1. Click **"Unlock Board"** in the header
2. Enter the **same password** used to lock
3. Click **"Unlock"**

**Error:** "Invalid password" → Password mismatch, try again or contact the person who locked it

### Password Security

**Storage:** Passwords are hashed using **PBKDF2-HMAC-SHA256**:

- 10,000 iterations
- 32-byte random salt per board
- 32-byte hash output
- Never stored in plaintext

**Recovery:** No password recovery mechanism. If lost, contact the system administrator to reset via database.

### Lock Status Indicator

```
🔒 Board Locked   [Unlock]
```

**Color:**

- Red background when locked
- All edit buttons disabled and grayed out

---

## Finalizing the Plan

### What is Finalization?

**Finalization** marks the board as a "committed plan" without preventing edits. It's a soft milestone that:

- Tracks when the plan was locked down
- Highlights stories moved **after finalization** with warning badges
- Allows continued iteration while maintaining transparency

**Key Difference from Locking:**

- **Lock**: Prevents all edits (password-protected)
- **Finalize**: Allows edits but tracks changes visually

### When to Finalize

Finalize a board when:

- ✅ All features are imported
- ✅ Stories are assigned to sprints
- ✅ Team agrees on the plan
- ✅ Capacity is balanced (no major over-allocations)

### Step 1: Validate Before Finalizing

1. Click **"Validate Finalization"** in the board menu
2. Review warnings:

```
⚠ 3 user stories are not assigned to any sprint
⚠ Sprint 2 has 0 team members assigned
⚠ Sprint 4 is over capacity by 15 hours
```

**Note:** Warnings are informational, not blocking. The board can be finalized with warnings.

### Step 2: Finalize the Board

1. Click **"Finalize Board"**
2. Confirm in the dialog: **"Yes, Finalize"**
3. Success message appears with timestamp

**Result:**

- Board status changes to **Finalized**
- Green checkmark badge appears in header
- Timestamp recorded: `Finalized on Mar 7, 2026 at 2:30 PM`

### What Happens After Finalization?

**Stories Moved After Finalization:**

```
┌────────────────────────────────────┐
│ Login Page                         │
│ ⚠ Moved after finalization         │  ← Yellow warning badge
│ #12346  │  5 pts                   │
└────────────────────────────────────┘
```

**Why This Matters:**

- Tracks scope changes post-commitment
- Provides accountability for late adjustments
- Helps with retrospectives and metrics

### Restoring (Unfinalizing) a Board

**Scenario:** Need to make major changes after finalization.

1. Click **"Restore Board"** in the menu
2. Confirm: **"Yes, Restore"**

**Effect:**

- Finalized status removed
- All "moved after finalization" badges cleared
- Board returns to editable state (if not locked)

---

## Common Workflows

### Workflow 1: New PI Planning Session

```
1. Create Board
   ↓
2. Import Features from Azure (repeat for each feature)
   ↓
3. Add Team Members
   ↓
4. Drag Stories to Sprints (balance capacity)
   ↓
5. Validate Finalization (check warnings)
   ↓
6. Finalize Board
   ↓
7. (Optional) Lock Board to prevent changes
```

**Time Estimate:** 1-2 hours for 5 features, 10-15 stories

---

### Workflow 2: Mid-PI Replanning

**Scenario:** New high-priority feature needs to be added to Sprint 3.

```
1. Unlock Board (if locked)
   ↓
2. Import New Feature from Azure
   ↓
3. Prioritize Feature (move to top via reorder)
   ↓
4. Drag Stories from Placeholder to Sprint 3
   ↓
5. Check capacity (is Sprint 3 over-allocated?)
   ↓
6. Move lower-priority stories out if needed
   ↓
7. Re-Finalize Board (optional)
```

---

### Workflow 3: Refreshing Data from Azure

**Scenario:** Story points updated in Azure Boards after import.

```
1. Find the story card on the board
   ↓
2. Click story menu (three dots)
   ↓
3. Select "Refresh from Azure"
   ↓
4. Enter the Azure PAT when prompted
   ↓
5. Story data syncs (title, points)
   ↓
6. Verify capacity recalculations
```

**Use Cases:**

- Story points changed in Azure
- Story title or description updated
- Need latest Azure metadata

---

### Workflow 4: Exporting Capacity Report

**Coming Soon:** Currently, capacity data is viewed in-app only.

**Workaround:**

1. Take screenshots of sprint columns showing load/capacity
2. Or use browser DevTools → Network tab → Export board JSON response

---

## Tips & Best Practices

### Planning Tips

**1. Start with High-Priority Features**

- Import and plan P0/P1 features first
- Leave lower-priority features in Placeholder initially

**2. Balance Capacity Across Sprints**

- Aim for 70-90% utilization per sprint
- Avoid overloading early sprints (team velocity ramps up)

**3. Account for Holidays and PTO**

- Adjust team member capacity in affected sprints
- Flag these sprints with notes (e.g., "Thanksgiving week")

**4. Use Placeholder Column**

- Keep all unplanned stories in Placeholder
- Review Placeholder during replanning sessions

**5. Lock After Commitment**

- Lock board once stakeholders approve the plan
- Share password only with authorized users (SM, PO)

---

### Collaboration Tips

**1. Name Boards Descriptively**

- Good: `"PI 2024 Q2 - Team Alpha"`
- Bad: `"Board 1"`

**2. Check Active Users**

- Before making bulk changes, see who else is online
- Use team chat to coordinate major edits

**3. Communicate Lock/Unlock**

- Announce in team chat when locking/unlocking
- Document password in secure location (e.g., 1Password)

**4. Finalize Early, Adjust Later**

- Don't wait for perfection to finalize
- Finalization is reversible and tracks changes

---

### Capacity Planning Tips

**1. Factor in Non-Coding Time**

- Ceremonies (15-20% overhead): Standups, Retros, Planning
- Meetings, training, support work

**2. Dev vs. Test Split**

- Enable Dev/Test Toggle if teams track separately
- Typical split: 60% Dev, 40% Test (varies by team)

**3. Validate Totals**

- Total story points across all sprints should align with PI capacity
- Formula: `Total Capacity = (Team Size × Sprint Duration × Num Sprints × 40 hours)`

**4. Plan for Slack**

- Leave 10-20% buffer in each sprint for:
  - Bug fixes
  - Tech debt
  - Production support

---

## Troubleshooting

### Issue: "Board is locked and cannot be modified"

**Symptom:** All edit buttons are disabled, red lock icon in header.

**Solution:**

1. Click **"Unlock Board"**
2. Enter the password used to lock the board
3. If the password is unavailable, contact the person who locked it

**Prevention:** Document lock passwords in a secure shared location.

---

### Issue: Stories Not Syncing Across Users

**Symptom:** Story moved by User A doesn't appear moved for User B.

**Check:**

1. Verify **connection status** (bottom-right corner)
   - Should show 🟢 **Connected**
2. Refresh the page (Ctrl+F5 or Cmd+Shift+R)
3. Check browser console for WebSocket errors

**Common Cause:** Network firewall blocking WebSocket connections

**Solution:**

- Ensure WebSocket protocol is allowed (port 80/443)
- Check reverse proxy configuration (Nginx, IIS URL Rewrite)

---

### Issue: Azure PAT Returns Error

**Symptom:** "Invalid PAT" or "403 Forbidden" when fetching features.

**Check:**

1. PAT has **Work Items (Read)** scope (`vso.work`)
2. PAT is not expired (check Azure DevOps → User Settings → PATs)
3. Organization and Project names are correct (case-sensitive)
4. Feature ID exists in Azure Boards

**Test PAT:**

```bash
curl -u :{YOUR_PAT} https://dev.azure.com/{org}/{project}/_apis/wit/workitems/12345?api-version=7.0
```

**Solution:** Generate a new PAT with correct scopes and expiration date.

---

### Issue: Capacity Not Updating After Team Changes

**Symptom:** Added a team member but sprint capacity still shows 0.

**Check:**

1. Team member has **Dev** or **Test** checkbox enabled
2. Refresh the page to trigger recalculation
3. Verify sprint assignments are created (Team Panel → Expand sprints)

**Solution:**

- Edit team member → Toggle Dev/Test → Save
- System recalculates capacities for all sprints automatically

---

### Issue: Board Finalized But Can Still Edit

**Expected Behavior:** Finalization does NOT prevent edits.

**Clarification:**

- **Finalize** = Soft milestone with change tracking
- **Lock** = Hard block with password protection

**Use Lock when preventing edits entirely is required.**

---

### Issue: Stories Assigned to Wrong Sprint

**Quick Fix:**

1. Drag story back to Placeholder column
2. Double-check target sprint dates
3. Re-assign to correct sprint

**Prevention:**

- Verify sprint dates before planning
- Use sprint date labels in column headers

---

### Issue: Browser Freezes During Large Imports

**Symptom:** Importing a feature with 50+ child stories causes lag.

**Workaround:**

1. Break large features into smaller features in Azure
2. Import in batches of 10-15 stories per feature
3. Close unused browser tabs to free memory

**System Limits:**

- Recommended: 10 features, 100 stories per board
- Maximum tested: 20 features, 500 stories (may experience lag)

---

### Issue: Can't Find My Board

**Search Tips:**

1. Use the **Search Boards** feature
2. Filter by:
   - Organization: `my-org`
   - Project: `MyProject`
   - Board Name: `Q2` (partial match works)
3. Check **"Show Locked"** or **"Show Finalized"** filters

**Verify:**

- Board was created under the correct Organization/Project
- The search is targeting the correct environment (Dev vs. Production)

---

## Additional Resources

### Related Documentation

- **[API Reference](API_REFERENCE.md)**: Complete API documentation for developers
- **[Architecture Guide](ARCHITECTURE.md)**: System design and technical architecture
- **[Configuration Guide](CONFIGURATION.md)**: Runtime configuration and environment variables
- **[Docker Deployment](DOCKER_DEPLOYMENT_GUIDE.md)**: Deploy with Docker Compose
- **[IIS Deployment](IIS_DEPLOYMENT_GUIDE.md)**: Deploy on Windows IIS with SQL Server
- **[Security Guide](SECURITY.md)**: Security best practices and authentication

### Getting Help

**Report Issues:**

- GitHub Issues: `[Repository URL]/issues`
- Team Chat: `#pi-planning-tool` channel
- Email: `pipl-support@yourcompany.com`

**Feature Requests:**

- Submit via GitHub Issues with `enhancement` label
- Vote on existing requests

**Training:**

- Recorded demo sessions: `[Internal Wiki URL]`
- Live training schedule: `[Training Portal URL]`

## Version History

### Version 1.0 (March 2026)

- Initial user guide documentation
- Covers all features through Phase 7 (Board Lock/Unlock)
- Real-time collaboration documented
- Comprehensive troubleshooting section

---

**Questions or feedback?** Contact the PI Planning Tool team or submit an issue on GitHub.

**Happy Planning! 🚀**
