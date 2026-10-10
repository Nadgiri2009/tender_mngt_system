# Tender Management — Backend (ASP.NET Core 8)

## Architecture

```
TenderManagement.Domain          Entities, enums, no external dependencies
TenderManagement.Application     DTOs, service interfaces + implementations, WorkflowEngine
TenderManagement.Infrastructure  EF Core DbContext, Repository + UnitOfWork, JWT/file/audit services
TenderManagement.API             Controllers, JWT auth wiring, Swagger, global exception middleware
```

Dependency direction: `API → Infrastructure → Application → Domain` (Application never references
Infrastructure or API; Domain has zero dependencies). This is what lets `WorkflowEngine` (the
business rule for "what status comes next") be unit-testable without a database.

## The Workflow Engine

`TenderManagement.Application/Services/WorkflowEngine.cs` is a static dictionary keyed by
`Tender.CurrentStatus`, where each entry declares:
- the **Submit** transition (a plain hand-off with no decision — e.g. Department Officer moving a
  tender from `Draft` to `Requirement Submitted`), and/or
- the **Approve/Reject** transition (a decision made by a specific role).

`WorkflowService` looks up the tender's current status, checks the caller's role against the
allowed list, and — if permitted — calls `IUnitOfWork.ExecuteWorkflowTransitionAsync(...)`, which
executes `sp_UpdateTenderStatus` (in `/Database/02_StoredProcedures.sql`). That stored procedure
updates `Tender.CurrentStatus` and inserts the `ApprovalHistory` row in **one SQL transaction**, so
the live status and the audit trail can never drift apart.

`WorkflowStages` (DB table) additionally lets Admin see/reassign which Role owns each stage without
a code deploy — the dictionary above is what's authoritative for *status sequencing*, the table is
what's authoritative for *who currently approves what* (read by `GetPendingApprovalsForRoleAsync`).

## Auth

JWT bearer tokens, `BCrypt.Net-Next` password hashing. Claims include `sub` (user id), `role`
(role name — used by `[Authorize(Roles = "...")]`), and a custom `roleId` claim (used by the
pending-approvals/dashboard endpoints to avoid an extra DB round-trip per request).

## Endpoints

| Method | Path | Notes |
|---|---|---|
| POST | `/api/auth/login` | |
| POST | `/api/auth/register` | |
| GET/POST | `/api/tenders` | paginated/search/filter on GET |
| GET/PUT/DELETE | `/api/tenders/{id}` | PUT/DELETE only while `Draft` |
| POST | `/api/tenders/{id}/submit` | |
| POST | `/api/tenders/{id}/approve` | |
| POST | `/api/tenders/{id}/reject` | |
| GET | `/api/tenders/{id}/history` | full audit trail |
| GET | `/api/approvals/pending` | tenders awaiting the caller's role |
| GET | `/api/dashboard/stats` | |
| POST | `/api/documents/upload` | multipart |
| GET | `/api/documents/{tenderId}` | |
| POST | `/api/boq` | |
| GET | `/api/boq/{tenderId}` | |
| POST | `/api/bids/technical` | |
| PUT | `/api/bids/technical/evaluate` | Qualify/Disqualify |
| POST | `/api/bids/financial` | |
| POST | `/api/bids/financial/select` | recommend the winning contractor |
| GET | `/api/bids/{tenderId}` / `/api/bids/{tenderId}/financial` | |
| POST | `/api/agreements` | |
| POST | `/api/workorder/create` | |
| POST | `/api/work/update` | progress % |
| POST | `/api/mb/create` | e-MB entry |
| POST | `/api/bills/create` | |
| PUT | `/api/bills/{id}/approve` | |
| GET | `/api/bills/{tenderId}` | |
| POST | `/api/payment/process` | |
| GET | `/api/departments`, `/api/roles`, `/api/contractors` | dropdown data |
| GET | `/api/users`, `PUT /api/users/{id}/toggle-active` | Admin only |

Full request/response shapes are visible in Swagger (`/swagger`) once running, generated directly
from the DTOs in `TenderManagement.Application/DTOs`.

## Configuration

`appsettings.json` ships with **no real credentials or secrets** — only structural defaults:
- `ConnectionStrings:DefaultConnection` defaults to Windows Authentication against
  `localhost\SQLEXPRESS`. For SQL Authentication instead, replace it with:
  `Server=<host>,<port>;Database=TenderManagementDB;User Id=<user>;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true`
- `Jwt:Key` ships **empty on purpose**. `Program.cs` checks this at startup and throws a clear error
  (rather than a cryptic crypto exception) if it's missing or under 32 characters. Set a real one via
  user-secrets (recommended for local dev — never gets committed):
  ```bash
  dotnet user-secrets init
  dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"
  ```
  or as an environment variable (`Jwt__Key=...`) in any deployed environment.
- `Cors:AllowedOrigins` — add your deployed frontend's origin.

## Running tests / sanity-checking

This bundle was authored without a live .NET SDK + NuGet feed in the build environment, so it
hasn't been through `dotnet build` here — it has been written and reviewed carefully for
correctness (consistent namespaces, EF Core attribute usage, nullable-reference compliance), but
please run `dotnet restore && dotnet build` as your first step and treat any compiler warnings as
the place to start if something doesn't compile in your toolchain version.
