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

> Setup commands will be added after Phase 0 completes.

```bash
dotnet build PRM_Workspace.sln
dotnet ef database update --project Server/PRM.Server.csproj
dotnet run --project Server/PRM.Server.csproj
dotnet run --project Client/PRM.Client.csproj
dotnet test Tests/PRM.Tests.csproj
```

**Folder layout:** `Client/` = PRM.Client, `Server/` = PRM.Server, `Tests/` = PRM.Tests (csproj files live directly in each folder).

Default admin (seeded): `admin` / `Admin@1234` (forced password change on first login).

## Development Progress

| Phase | Scope | Status |
|-------|-------|--------|
| 0 | Solution, EF schema, seed data | In Progress |
| 1 | Auth, JWT, navigation shell | Not Started |
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

- `PRM_Workspace.sln` with `Client/`, `Server/`, `Tests/` projects targeting **.NET 10**
- EF Core schema + `InitialCreate` migration (milestone `due_date` as SQL DATE)
- Database seed: admin user, 11 activity tags, 4 system config keys
- Server health endpoint: `GET /health`
- Client placeholder menu

## Documentation

| Document | Purpose |
|----------|---------|
| [Docs/PRM_BRD_V4.md](Docs/PRM_BRD_V4.md) | Business requirements |
| [Docs/PRM_Developer_Guide_V3.md](Docs/PRM_Developer_Guide_V3.md) | Implementation guide |
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
