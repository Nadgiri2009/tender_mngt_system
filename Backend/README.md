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
updates `Tender.CurrentStatus` and inserts the `ApprovalHistory` row in **one SQL transaction**.
The authoritative schema and stored-procedure definitions are in the repository-root
`SMC_TENDER_DATABASE.sql`; do not apply historical SQL files individually.

`WorkflowStages` stores stage ownership while the dictionary above is authoritative for status
sequencing. Fresh database installation leaves this table empty; stage rows must be provisioned
through an approved process before role-specific approvals are available. Upgrade mode preserves
existing stage assignments.

## Auth

Sign-in accepts exactly ten digits for the registered mobile number. Codes expire after five
minutes, are single-use, and are limited to three requests per 15 minutes and five
verification attempts. For requests made from localhost in local Development, the randomly generated code is returned as
`developmentOtp` and shown on the login page; it is not written to the API console. No fixed or
reusable OTP is configured. Production and non-local requests currently return
`503`; an SMS delivery provider is not implemented. JWT bearer tokens include `sub` (user id),
`role` (role name — used by
`[Authorize(Roles = "...")]`), and a custom `roleId` claim (used by the pending-approvals/dashboard
endpoints to avoid an extra DB round-trip per request). Contractor registration does not collect a
password; sign-in uses the registered mobile and OTP. In the local `tender_tracking` database, the
four development staff placeholders are active for local OTP testing: JE (`0000000001`), Tender
Cell (`0000000002`), Tender Committee Chairman (`0000000003`), and Director Auditor
(`0000000004`). These are not real phone numbers and must never be enabled in production. If a
request is inside the 60-second cooldown or three-request window, the API returns `429` with a
`Retry-After` header instead of advancing to an OTP form without issuing a new code.

## Endpoints

| Method | Path | Notes |
|---|---|---|
| POST | `/api/auth/request-otp` | Returns a generic `202`; includes `developmentOtp` only for an eligible local Development request |
| POST | `/api/auth/login` | Verifies `{ "mobile": "...", "otp": "123456" }` |
| POST | `/api/auth/register` | Contractor self-registration |
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

For local development, install a fresh database using the repository-root
`SMC_TENDER_DATABASE.sql` with `InstallMode=Fresh`, or rehearse an approved existing database
upgrade using `InstallMode=Upgrade` only on a verified restored copy. Never run Fresh mode against
a populated database. Configure the API to use Windows Authentication with the SQL Express
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