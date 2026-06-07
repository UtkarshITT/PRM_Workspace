# Requirements Snapshot — Project & Resource Management (PRM) Tool
## Quick Reference for Development

> **Source of Truth:** BRD V4 (Final)
> **Tech Stack:** .NET C# · MSSQL · REST API · Console Client
> **Document Purpose:** Long-term development reference — concise and scannable

---

## Table of Contents

1. [Project Vision](#1-project-vision)
2. [User Roles](#2-user-roles)
3. [Core Modules](#3-core-modules)
4. [Main Workflows](#4-main-workflows)
5. [Functional Requirements](#5-functional-requirements)
6. [Non-Functional Requirements](#6-non-functional-requirements)
7. [Business Rules](#7-business-rules)
8. [AI Integration Summary](#8-ai-integration-summary)
9. [Reports & Exports](#9-reports--exports)
10. [Future Scope / Out of Scope](#10-future-scope--out-of-scope)
11. [Open Questions](#11-open-questions)

---

## 1. Project Vision

**What:** A console-based, client-server Project & Resource Management tool for TechServe Solutions — a mid-sized IT services company.

**Why:** Replace manual Excel/chat-based resource planning with a single, intelligent system that tracks people, projects, allocations, and timesheets in real time.

**The system answers:**
- "Who is on bench right now and what can they do?"
- "Who is the best match for this new project requirement?"
- "Is Project Alpha on track or at risk?"
- "Did everyone on my team submit their timesheets this week?"

**Key value adds:**
- Prevents over-allocation automatically (100% utilization cap)
- Captures real skill evidence through timesheet activity tags
- AI-powered resource matching and project risk summaries
- Background automation of health flags and missed timesheet detection

---

## 2. User Roles

| Role | Who | Key Capability | Access |
|------|-----|---------------|--------|
| **Admin** | HR / Ops team | Master data management | Admin menu only |
| **Manager** | Delivery manager | Project operations + AI resourcing | Manager menu only |
| **Employee** | Developer, tester, etc. | Timesheet submission + own view | Employee menu only |

### Role Creation Rules

- **No self-registration** for any role
- All accounts created by Admin only (Manage Users → Create User Account)
- First Admin seeded directly into DB: `admin` / `Admin@1234` (forced change on first login)
- All subsequent Admin, Manager, and Employee accounts created by existing Admin

### Scope Boundaries

| Role | Cannot Do |
|------|----------|
| Admin | Allocate resources, view timesheets, approve/reject |
| Manager | Modify employee profiles, system settings, view other teams |
| Employee | View other employees, projects, or allocation data |

> **Director role is OUT OF SCOPE in V4.** Do not build it.

---

## 3. Core Modules

| Module | Owner Role | Key Screens |
|--------|-----------|------------|
| Authentication | All | Login, Force Password Change |
| Employee Management | Admin | View/Update/Deactivate Employees, Skills, Assign Manager |
| Project Management | Admin | Create/Update Projects, Milestones (with Story Points) |
| User Management | Admin | Create/View/Reset/Deactivate/Reactivate Accounts |
| System Configuration | Admin | LLM provider, API key, scheduler interval, max hours |
| Resource Dashboard | Manager | Bench + active employees, drill-down (team-scoped) |
| Resource Allocation | Manager | AI-assisted + direct allocation, end allocation |
| My Projects | Manager | Project health (R/A/G), milestones, resources |
| Timesheet View | Manager | Team timesheet status by week (read-only) |
| AI Assistant | Manager | Skill Match + Risk Summary |
| Submit Timesheet | Employee | Hours + activity tags per allocated project |
| View My Timesheets | Employee | History with SUBMITTED/MISSED status |
| View My Allocations | Employee | Current + past allocations with utilization % |
| Background Scheduler | System | Utilization recompute, health flagging, missed timesheets |

---

## 4. Main Workflows

### 4.1 New User Onboarding

```
Admin → Manage Users → Create User Account
     → (For Employee/Manager) Manage Employees → Assign Manager
     ↓
User logs in → Force Password Change → Role menu
```

### 4.2 Resource Allocation (AI-Assisted)

```
Manager → Allocate Resource → AI Search
        → Describe requirement in plain English
        → Server filters by capacity → calls LLM
        → LLM returns ranked candidates with reasons
        → Manager selects → Server validates (≤100%) → Allocation saved
```

### 4.3 Resource Allocation (Direct)

```
Manager → Allocate Resource → Direct
        → Enter Employee ID + Project
        → Server validates (≤100%) → Allocation saved
```

### 4.4 Weekly Timesheet Submission

```
Employee → Submit Timesheet → Select week (default: last Monday)
         → For each allocated project: enter hours + select activity tags
         → Server validates hours per project + total hours
         → Submit → Status: SUBMITTED
```

### 4.5 Project Health Flow

```
Background Scheduler (every N hours):
  → Recompute employee utilization from active allocations
  → Evaluate each active project:
      Overdue milestones?      → Flag
      Low logged hours?        → Flag
      Approaching deadline?    → Flag
  → 0 flags = GREEN, 1 flag = AMBER, 2+ flags = RED
  → Mark past-week unsupported employees as MISSED
```

### 4.6 AI Risk Summary

```
Manager → My Projects → Select project → Get AI Risk Summary
       → Server collects: milestones, allocations, timesheet hours vs expected
       → Sends to LLM → Returns plain-English paragraph
       → Displayed with AI-generated disclaimer
```

### 4.7 Employee Deactivation

```
Admin → Manage Employees → Deactivate Employee
      → Server: is_active=false on Employee + User
               to_date=today on all active Allocations
               All historical data preserved
```

---

## 5. Functional Requirements

### 5.1 Authentication

| # | Requirement |
|---|-------------|
| F-AUTH-01 | Login validates username and password via server |
| F-AUTH-02 | On first login for Admin-created accounts: force password change before any menu access |
| F-AUTH-03 | Password rules: 8+ characters, at least 1 uppercase, 1 number |
| F-AUTH-04 | Passwords stored as bcrypt hashes — never plain text |
| F-AUTH-05 | JWT or session token returned on successful login; stored in memory on client |
| F-AUTH-06 | No self-registration. Admin creates all accounts |

### 5.2 Admin — Employee Management

| # | Requirement |
|---|-------------|
| F-EMP-01 | View all employees with ID, name, department, status |
| F-EMP-02 | Filter employees by status or department |
| F-EMP-03 | Update employee: name, department, designation |
| F-EMP-04 | Deactivate employee: is_active=false, end all active allocations (to_date=today), block login |
| F-EMP-05 | All historical data (timesheets, past allocations) preserved on deactivation |
| F-EMP-06 | Add/update/remove skills per employee with proficiency (Beginner/Intermediate/Advanced) |
| F-EMP-07 | Skill category is mandatory: Backend, Frontend, DevOps, QA, or Other |
| F-EMP-08 | Assign a Manager to an Employee (updates manager_id field) |

### 5.3 Admin — Project Management

| # | Requirement |
|---|-------------|
| F-PRJ-01 | Create project: name, description, start date, end date, status, manager, total story points |
| F-PRJ-02 | View all projects with SP Done/Total column |
| F-PRJ-03 | Update all project fields; status can be set to COMPLETED |
| F-PRJ-04 | Project statuses: PLANNED, ACTIVE, ON_HOLD, COMPLETED |
| F-PRJ-05 | Add milestones to projects: title, due date, story points |
| F-PRJ-06 | Update milestone status: NOT_STARTED, IN_PROGRESS, DONE |
| F-PRJ-07 | Milestone list shows total SP, completed SP, remaining SP |

### 5.4 Admin — User Management

| # | Requirement |
|---|-------------|
| F-USR-01 | Create user account for any role (Admin/Manager/Employee) |
| F-USR-02 | Username and email must be unique |
| F-USR-03 | New account auto-sets force_password_change=true |
| F-USR-04 | View all users with role and active status |
| F-USR-05 | Reset any user's password (re-sets force_password_change=true) |
| F-USR-06 | Deactivate user (blocks login, preserves all data) |
| F-USR-07 | Reactivate user (does NOT restore past allocations) |

### 5.5 Admin — System Configuration

| # | Requirement |
|---|-------------|
| F-CFG-01 | Set LLM provider: Gemini or Groq |
| F-CFG-02 | Set LLM API key (stored encrypted) |
| F-CFG-03 | Set scheduler interval (hours) |
| F-CFG-04 | Set max weekly hours (default: 40) |

### 5.6 Manager — Resource Dashboard

| # | Requirement |
|---|-------------|
| F-DASH-01 | Show bench employees (0% allocated) with skills — team-scoped |
| F-DASH-02 | Show active employees with utilization % and remaining availability — team-scoped |
| F-DASH-03 | Drill into any team employee to see skills, active allocations, recent activity tags (last 4 weeks) |

### 5.7 Manager — Allocation

| # | Requirement |
|---|-------------|
| F-ALLOC-01 | Allocate employee to project with utilization % and date range |
| F-ALLOC-02 | Server must prevent over-allocation: total utilization across overlapping allocations ≤ 100% |
| F-ALLOC-03 | From date must be before to date |
| F-ALLOC-04 | Project must be ACTIVE or PLANNED for new allocations |
| F-ALLOC-05 | AI-assisted search: manager types natural language → AI ranks available employees |
| F-ALLOC-06 | Direct allocation: manager enters employee ID directly |
| F-ALLOC-07 | End an allocation: sets to_date=today, immediately recomputes employee status |
| F-ALLOC-08 | Manager can only allocate employees from their own team |
| F-ALLOC-09 | Only the manager who owns a project can end allocations on it |

### 5.8 Manager — My Projects

| # | Requirement |
|---|-------------|
| F-PRJ-MGR-01 | View own projects with health indicator (🔴 AT RISK / 🟡 ATTENTION / 🟢 ON TRACK) |
| F-PRJ-MGR-02 | Drill into project: risk flags, milestones, allocated resources |
| F-PRJ-MGR-03 | Request AI risk summary for any project (plain-English paragraph) |

### 5.9 Manager — Timesheets

| # | Requirement |
|---|-------------|
| F-TS-MGR-01 | View team timesheets filtered by week |
| F-TS-MGR-02 | See SUBMITTED and MISSED status per employee per project |
| F-TS-MGR-03 | Drill into individual timesheet for detail |
| F-TS-MGR-04 | No approve/reject — read-only view only |

### 5.10 Employee — Timesheet

| # | Requirement |
|---|-------------|
| F-TS-EMP-01 | Submit timesheet for a given week (default: last Monday) |
| F-TS-EMP-02 | Log hours per project for projects allocated during that week |
| F-TS-EMP-03 | Select activity tags per project (fixed list + "Other" manual) |
| F-TS-EMP-04 | Hours per project ≤ (allocation% × max_weekly_hours) |
| F-TS-EMP-05 | Total hours across all projects ≤ max_weekly_hours |
| F-TS-EMP-06 | Cannot submit twice for same week |
| F-TS-EMP-07 | Cannot submit for a future week |
| F-TS-EMP-08 | View timesheet history with SUBMITTED/MISSED status |
| F-TS-EMP-09 | Reminder shown on Employee menu if most recent completed week not submitted |

### 5.11 Employee — Allocations

| # | Requirement |
|---|-------------|
| F-ALLOC-EMP-01 | View own current and past allocations with %, date range, and status |
| F-ALLOC-EMP-02 | Total utilization % shown |

---

## 6. Non-Functional Requirements

| # | Category | Requirement |
|---|----------|-------------|
| NF-01 | Architecture | Two separate programs: Console Client + REST API Server |
| NF-02 | Architecture | Client never accesses DB directly; all data via REST API |
| NF-03 | Architecture | All business logic and validation on server only |
| NF-04 | Architecture | Layered server: Controller → Service → Repository → DB |
| NF-05 | Architecture | No SQL in Service layer classes |
| NF-06 | Concurrency | Background scheduler runs on a separate thread; must not block main API |
| NF-07 | Security | Passwords stored as bcrypt hashes |
| NF-08 | Security | JWT-based authentication; role enforced per endpoint |
| NF-09 | Security | LLM API key stored encrypted; never logged |
| NF-10 | Reliability | LLM failures must not crash the application; show user-friendly fallback |
| NF-11 | Testing | Minimum 60% unit test coverage on the Service layer |
| NF-12 | Documentation | Swagger/OpenAPI or Postman collection required |
| NF-13 | Documentation | README with setup guide, design decisions, LLM setup |
| NF-14 | Code Quality | SOLID principles demonstrable with examples in README |
| NF-15 | Code Quality | At least one design pattern used and documented |
| NF-16 | Code Quality | At least two design principles applied and documented |
| NF-17 | Code Quality | Clean code: meaningful names, small focused methods, no magic numbers, no dead code |

---

## 7. Business Rules

### Allocation Rules

| Rule | Detail |
|------|--------|
| BR-A1 | Total utilization across all overlapping allocations for an employee cannot exceed 100% |
| BR-A2 | Only the project's assigned manager can end allocations on that project |
| BR-A3 | New allocations only allowed on ACTIVE or PLANNED projects |
| BR-A4 | `from_date` must be before `to_date` |
| BR-A5 | Ending an allocation sets `to_date` to today and immediately recomputes employee status |
| BR-A6 | Manager can only allocate employees from their own team (filtered by `manager_id`) |

### Timesheet Rules

| Rule | Detail |
|------|--------|
| BR-T1 | Employee can only log hours for projects allocated to them during that week |
| BR-T2 | Hours per project ≤ `allocation% × max_weekly_hours` |
| BR-T3 | Total hours across all projects ≤ `max_weekly_hours` (configurable, default 40) |
| BR-T4 | Cannot submit a timesheet for the same week twice |
| BR-T5 | Cannot submit a timesheet for a future week |
| BR-T6 | No approve/reject workflow — submitted timesheets are final |

### Employee / User Rules

| Rule | Detail |
|------|--------|
| BR-E1 | Deactivating an employee: sets `is_active=false`, ends all active allocations, blocks login |
| BR-E2 | All historical data (timesheets, past allocations) preserved on deactivation |
| BR-E3 | Reactivating a user does NOT restore past allocations |
| BR-E4 | Username and email must be unique across all users |
| BR-E5 | First Admin account seeded via script. All subsequent accounts created by Admin |

### Project Health Rules

| Rule | Detail |
|------|--------|
| BR-H1 | GREEN: 0 health flags |
| BR-H2 | AMBER: 1 health flag |
| BR-H3 | RED: 2+ health flags |
| BR-H4 | Health flags: overdue milestone, low logged hours vs expected, approaching deadline with pending milestones |

### Scheduler Rules

| Rule | Detail |
|------|--------|
| BR-S1 | Scheduler runs periodically at configurable interval (default: 4 hours) |
| BR-S2 | Missed timesheet: marked for employees with no submission for a past completed week |
| BR-S3 | Scheduler runs must be idempotent (no double-marking) |

### AI Rules

| Rule | Detail |
|------|--------|
| BR-AI1 | LLM never accesses the database. Server always prepares data first |
| BR-AI2 | LLM failure must not crash the application. Show fallback message |
| BR-AI3 | Both Gemini and Groq must be supported. Active provider configured by Admin |
| BR-AI4 | All AI output displayed with disclaimer that it is AI-generated and should be verified |
| BR-AI5 | Employees at 100% utilization are excluded from skill match candidates before LLM is called |

### Password Rules

| Rule | Detail |
|------|--------|
| BR-P1 | Minimum 8 characters |
| BR-P2 | At least 1 uppercase letter |
| BR-P3 | At least 1 number |
| BR-P4 | Admin-created accounts have `force_password_change=true`. Must change on first login |

---

## 8. AI Integration Summary

### Skill Match

| Aspect | Detail |
|--------|--------|
| Trigger | Manager types natural language requirement |
| Input | Manager query + team employees (skills, allocations, activity tags) |
| Pre-filter | Exclude employees at 100% utilization before calling LLM |
| Output | Ranked list with plain-English reasons per candidate |
| Two modes | Full-time (capacity-based filter) + Part-time (hours-based filter, allocation % suggested) |
| Disclaimer | "AI-generated. Verify before confirming allocation." |

### Risk Summary

| Aspect | Detail |
|--------|--------|
| Trigger | Manager requests AI summary for a project |
| Input | Milestone titles/dates/status, allocated resources, timesheet hours vs expected (last weeks) |
| Output | Short plain-English paragraph highlighting risks and concerns |
| Disclaimer | "AI-generated from milestone and timesheet data." |

### Fallback Behavior

| Situation | Response |
|-----------|----------|
| LLM unavailable | Show user-friendly message; no crash |
| No eligible candidates | Tell manager before calling LLM |
| Invalid LLM response | Show fallback; log error server-side |

---

## 9. Reports & Exports

| Report | Available To | Format | Notes |
|--------|-------------|--------|-------|
| All Allocations Matrix | Admin | Console table (filterable) | No export |
| All Employees List | Admin | Console table | No export |
| All Projects List | Admin | Console table with SP Done/Total | No export |
| Resource Dashboard | Manager | Console table | Team-scoped only |
| My Projects Health | Manager | Console view with R/A/G indicators | No export |
| Team Timesheets | Manager | Console table by week | No export |
| My Timesheets | Employee | Console list with status | No export |
| My Allocations | Employee | Console table | No export |

> **No file export or download is required in V4.** (Director's company-wide report was removed with Director role.)

---

## 10. Future Scope / Out of Scope

### Explicitly Out of Scope in V4

| Feature | Notes |
|---------|-------|
| Director role and dashboard | Removed in V4 |
| Company-wide health report (txt export) | Was Director feature, removed |
| Web or desktop UI | Console client only. Optional bonus, never required |
| Timesheet approve/reject workflow | Confirmed not in scope in V4 |
| Self-registration | Never allowed |
| Cross-team employee visibility for Managers | Manager sees own team only |

### Potential Future Enhancements (Not in BRD)

| Feature | Notes |
|---------|-------|
| Director dashboard | Can be added as a 4th role in a future milestone |
| Push notifications | Replace in-app pull model with real-time alerts (e.g., SignalR) |
| Web UI | Angular / React frontend consuming the same REST API |
| Timesheet approval workflow | Could be added as a Manager capability later |
| Export reports | PDF/Excel export of allocations, health reports |
| Multi-company support | Partition by company_id in SystemConfig and all tables |
| Skill gap analysis | AI-powered identification of missing skills across the team |

---

## 11. Open Questions

| # | Question | Impact | Priority |
|---|----------|--------|----------|
| OQ-01 | Where is employee department/designation captured now that Add Employee screen is removed? | DB schema, Create User Account screen | 🔴 High |
| OQ-02 | Can an Employee record exist without a manager assigned (manager_id = NULL)? | Manager visibility scope queries | 🟡 Medium |
| OQ-03 | Is `MISSED` the only additional timesheet status in V4, or can a timesheet be in a pending/draft state? | DB status enum, UI display | 🟡 Medium |
| OQ-04 | What is the exact definition of "significantly fewer hours" for the low-hours health flag? | Scheduler logic | 🟡 Medium |
| OQ-05 | Can Admin view all timesheets, or only Manager can? | Admin capabilities / API endpoints | 🟢 Low |
| OQ-06 | Is there a maximum number of activity tags per timesheet entry, or is it unlimited? | UI validation | 🟢 Low |

---

*End of Requirements Snapshot — PRM Tool V4*

> **Last updated:** Based on BRD V4 (Final)
> **Raise all open questions (Section 11) with the product owner before starting implementation of affected features.**
