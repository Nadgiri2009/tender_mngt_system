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
| GET | `/api/dashboard/public-stats` | anonymous public summary |
| GET | `/api/dashboard/public-tenders` | anonymous; published notices only |
| GET | `/api/dashboard/public-departments` | anonymous; active department names only |
| GET | `/api/dashboard/admin-stats` | Admin only; status/category summaries and deadlines |
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

DepartmentOfficer tender lists, dashboard summaries, and unpublished detail access are restricted to tenders created by the signed-in officer. Published tender notices remain publicly viewable. Full request/response shapes are visible in Swagger (`/swagger`) once running, generated directly
from the DTOs in `TenderManagement.Application/DTOs`.

## Configuration

For local development, configure the API to use Windows Authentication with the SQL Express
instance `localhost\SQLEXPRESS02` and database `TenderManagementDB`. The API process must run as a
Windows account that has access to that database. Store configuration in .NET user-secrets rather
than committing local credentials:

```powershell
$project = "Backend\src\TenderManagement.API\TenderManagement.API.csproj"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS02;Database=TenderManagementDB;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true" --project $project
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 64
$rng.GetBytes($bytes)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes)) --project $project
$rng.Dispose()
```

`Jwt:Key` must be at least 32 characters. It may also be supplied as the `Jwt__Key` environment
variable in deployed environments. The structural example configuration is in
`src/TenderManagement.API/appsettings.Example.json`; keep real keys and passwords out of that file.
`Cors:AllowedOrigins` should include the deployed frontend's origin.

## Running tests / sanity-checking

From the repository root, build the API with
`dotnet build Backend\src\TenderManagement.API\TenderManagement.API.csproj`. Run it with
`dotnet run --project Backend\src\TenderManagement.API\TenderManagement.API.csproj`; in
Development, Swagger is available at `http://localhost:5050/swagger`.