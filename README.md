# Project & Resource Management (PRM) Tool

A console-based client-server system for TechServe Solutions that centralizes employee, project, allocation, and timesheet data with AI-assisted resource matching and automated project health monitoring.

## Tech Stack

- **.NET 10** (C#) — `net10.0`, SDK 10.0.300
- **MSSQL** (LocalDB / SQL Server) + **EF Core**
- **ASP.NET Web API** (server)
- **Console client** (presentation only)
- **JWT** authentication
- **LLM integration** (Gemini / Groq) — server-side only

## Architecture

```
Console Client  ←—— HTTP/REST ——→  ASP.NET Web API  ←—— EF Core ——→  MSSQL
                                         ↓
                                    LLM API (Gemini/Groq)
```

Diagrams: [Docs/Diagrams/Diagram_per_v4/](Docs/Diagrams/Diagram_per_v4/)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server or LocalDB
- (Optional) LLM API key for AI features — set `PRM_LLM_API_KEY` environment variable

## Quick Start

```bash
dotnet build PRM_Workspace.sln
dotnet run --project Server/PRM.Server.csproj
dotnet run --project Client/PRM.Client.csproj
```

In a second terminal, run the client after the server is listening on `https://localhost:5001`.

## Development Progress

| Phase | Scope | Status |
|-------|-------|--------|
| 0 | Solution, EF schema, seed data | Done |
| 1 | Auth, JWT, navigation shell | Done |
| 2 | Admin users & employees | Not Started |
| 3 | Admin projects & milestones | Not Started |
| 4 | Manager dashboard & allocations | Not Started |
| 5 | Employee timesheets | Not Started |
| 6 | Manager projects & team timesheets | Not Started |
| 7 | Background scheduler | Not Started |
| 8 | AI skill match & risk summary | Not Started |
| 9 | System configuration | Not Started |
| 10 | Audit, logging, security | Not Started |
| 11 | 80%+ test coverage & CI | Not Started |
| 12 | Portfolio docs & release | Not Started |

### What Works Now

- `PRM_Workspace.sln` with `Client/`, `Server/`, `Tests/` targeting **.NET 10**
- EF Core schema + migrations (`InitialCreate`, `SeedReferenceData`), admin bootstrap via `DatabaseSeeder`
- **Phase 1:** JWT login, forced password change, Admin/Manager/Employee menu shells
- Default admin: `admin` / `Admin@1234` (change password on first login)

## Documentation

| Document | Purpose |
|----------|---------|
| [Docs/PRM_BRD_V4.md](Docs/PRM_BRD_V4.md) | Business requirements |
| [Docs/Developer_Guide_V2 1.md](Docs/Developer_Guide_V2%201.md) | Implementation guide (schema, seed, API) |
| [Docs/Requirements_Snapshot.md](Docs/Requirements_Snapshot.md) | Quick reference |
| [Docs/prm_phase_development_roadmap_6163a1a3.plan.md](Docs/prm_phase_development_roadmap_6163a1a3.plan.md) | Phase roadmap |

## Known Limitations (V4)

- No JWT refresh tokens (8-hour expiry)
- JWT remains valid until expiry after account deactivation
- Single-instance background scheduler
- Manager timesheets are read-only (no approve/reject)
- Director role not in scope

## License

TBD — portfolio / learning project.
