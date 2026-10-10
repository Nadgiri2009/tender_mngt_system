# Tender Management System

A full-stack digitization of the traditional tender workflow — from department requirement through
estimate approval, technical sanction, GB/administrative/budget approvals, tender publication, bid
opening & evaluation, contractor selection, agreement, work execution, e-MB, billing, and payment
release — built as a clean, layered, role-based enterprise application.

```
TenderMngt/
├── Database/        SQL Server 2022 schema, stored procedures, seed data, ER diagram
├── Backend/          ASP.NET Core 8 Web API — Clean Architecture (Domain / Application / Infrastructure / API)
└── Frontend/          React 18 + Material UI SPA
```

## How the pieces fit together

- **Database** — `Database/01_CreateDatabase_Schema.sql` creates every table from the spec (Users,
  Roles, Departments, Tender, Estimate, ApprovalHistory, TenderDocuments, BOQ, Contractor,
  TechnicalBid, FinancialBid, Agreement, WorkOrder, WorkExecution, MeasurementBook, Bills, Payments,
  AuditLog), plus a `WorkflowStages` config table. `02_StoredProcedures.sql` adds the procedures the
  API calls for the most safety-critical operation (atomic status transition + audit row) and for a
  few reporting queries. `03_SeedData.sql` seeds only the fixed Roles and the workflow-stage → role
  mapping — no demo departments, users, or business data of any kind.

- **Backend** — four projects:
  - `TenderManagement.Domain` — entities and enums, no dependencies.
  - `TenderManagement.Application` — DTOs, service interfaces, and the service layer
    (`AuthService`, `TenderService`, `WorkflowService`, `BidService`, `WorkService`), plus
    `WorkflowEngine`, the single source of truth for the 28-status state machine.
  - `TenderManagement.Infrastructure` — EF Core `ApplicationDbContext`, the generic
    repository + `TenderRepository`, `UnitOfWork` (which calls `sp_UpdateTenderStatus` for every
    workflow transition), JWT issuing, local file storage, and audit logging.
  - `TenderManagement.API` — controllers matching the spec's endpoint list, JWT bearer auth,
    role-based `[Authorize]`, global exception middleware, Swagger.

- **Frontend** — a single React app: JWT auth context, an Axios instance that auto-attaches the
  token and force-logs-out on 401, role-aware sidebar navigation, and pages for every module in the
  spec (Tenders, Approvals, Bid Evaluation, Work & Billing, Admin).

See `Database/ER_Diagram.md` for the entity-relationship diagram (Mermaid).

## Quick start

1. **Database** (SQL Server 2022 — Express, Developer, or via Docker all work):
   ```bash
   sqlcmd -S "localhost\SQLEXPRESS" -E -i Database/01_CreateDatabase_Schema.sql
   sqlcmd -S "localhost\SQLEXPRESS" -E -i Database/02_StoredProcedures.sql
   sqlcmd -S "localhost\SQLEXPRESS" -E -i Database/03_SeedData.sql
   ```
   (`-E` = Windows Authentication; swap for `-U sa -P <password>` if you're using SQL auth instead — see
   `Backend/README.md` for the connection-string equivalent.)

   `03_SeedData.sql` only inserts **Roles** and **WorkflowStages** — fixed reference/config data the
   app's authorization logic depends on by name. There is no demo Department, no demo Admin user, no
   placeholder password hash anywhere in this project.

2. **Backend**:
   ```bash
   cd Backend/src/TenderManagement.API
   # point ConnectionStrings:DefaultConnection in appsettings.json at your SQL instance
   # (defaults to Windows Authentication on localhost\SQLEXPRESS)

   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"   # generates + stores a real secret

   dotnet restore
   dotnet run
   # Swagger UI: http://localhost:5000/swagger
   ```
   The app will refuse to start with a clear error if `Jwt:Key` isn't set — that's intentional, so a
   real secret never accidentally ships hardcoded in `appsettings.json`.

3. **Frontend**:
   ```bash
   cd Frontend
   cp .env.example .env   # point REACT_APP_API_BASE_URL at the API
   npm install
   npm start
   # http://localhost:3000
   ```

4. **Create your first user** — there's no seeded login. Either use the Swagger UI or call directly:
   ```bash
   curl -X POST http://localhost:5000/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{"fullName":"Your Name","email":"you@example.com","mobile":"9999999999","password":"YourPassword123","roleId":1}'
   ```
   `roleId: 1` is Admin (per the seeded Roles table — check `GET /api/roles` to confirm the ids in your
   database). From there, log in and use Admin > Manage Departments to add real departments before
   creating tenders.

  Seeded logins (one per role)

  `Database/DatabaseSetup.sql` seeds one login for every role, so the entire tender lifecycle can be
  walked end-to-end without registering accounts by hand first:

  | Role | Email | Password |
  |---|---|---|
  | Admin | admin@example.com | Admin@1234 |
  | DepartmentOfficer | dept.officer@smc.gov.in | Password@123 |
  | TechnicalSanctionOfficer | ts.officer@smc.gov.in | Password@123 |
  | TenderSection | tender.section@smc.gov.in | Password@123 |
  | GBOfficer | gb.officer@smc.gov.in | Password@123 |
  | DPDCOfficer | dpdc.officer@smc.gov.in | Password@123 |
  | CAFOOfficer | cafo.officer@smc.gov.in | Password@123 |
  | MCAOfficer | mca.officer@smc.gov.in | Password@123 |
  | TenderCommittee | tender.committee@smc.gov.in | Password@123 |
  | CompetentAuthority | competent.authority@smc.gov.in | Password@123 |
  | Contractor | contractor@smc.gov.in | Password@123 |

  These are test credentials — change or remove them before any real deployment (delete the rows
  from `Users`, or don't run that section of the script).

## Officer SOP (PDF-Aligned, End-to-End)

Use this as the one-page operating checklist for officers.

Standalone printable handout: [OFFICER_SOP_PDF_ALIGNED.md](OFFICER_SOP_PDF_ALIGNED.md)

Bilingual handout (Hindi + Marathi): [OFFICER_SOP_PDF_ALIGNED_HI_MR.md](OFFICER_SOP_PDF_ALIGNED_HI_MR.md)

| Seq | Current Status | Who Logs In | Button/Action | Next Status |
|---|---|---|---|---|
| 1 | Draft | DepartmentOfficer | Submit / Advance | Requirement Submitted |
| 2 | Requirement Submitted | DepartmentOfficer | Submit / Advance | Estimate Prepared |
| 3 | Estimate Prepared | DepartmentOfficer | Submit / Advance | TS Approval Pending |
| 4 | TS Approval Pending | TechnicalSanctionOfficer | Approve | TS Approved |
| 5 | TS Approved | DepartmentOfficer | Submit / Advance | GB Approval Pending |
| 6 | GB Approval Pending | GBOfficer | Approve | GB Approved |
| 7 | GB Approved | DepartmentOfficer | Submit / Advance | Administrative Approval Pending |
| 8 | Administrative Approval Pending | DPDCOfficer | Approve | Administrative Approved |
| 9 | Administrative Approved | DepartmentOfficer | Submit / Advance | Budget Approval Pending |
| 10 | Budget Approval Pending | CAFOOfficer | Approve | Budget Approved |
| 11 | Budget Approved | DepartmentOfficer | Submit / Advance | Tender Draft Created |
| 12 | Tender Draft Created | DepartmentOfficer | Submit / Advance | Tender Committee Approval |
| 13 | Tender Committee Approval | TenderCommittee | Approve | Publication Approval Pending |
| 14 | Publication Approval Pending | CompetentAuthority | Approve | Tender Published |
| 15 | Tender Published | TenderCommittee or DepartmentOfficer | Submit / Advance | Technical Bid Opening |
| 16 | Technical Bid Opening | TenderCommittee | Submit / Advance | Technical Evaluation |
| 17 | Technical Evaluation | TenderSection | Approve | Technical Evaluation Approved |
| 18 | Technical Evaluation Approved | TenderCommittee | Approve | Financial Bid Opening |
| 19 | Financial Bid Opening | TenderCommittee | Submit / Advance | CAFO Rate Opinion |
| 20 | CAFO Rate Opinion | CAFOOfficer | Approve | Financial Evaluation |
| 21 | Financial Evaluation | TenderCommittee | Submit / Advance | MCA Opinion |
| 22 | MCA Opinion | MCAOfficer | Approve | Tender Committee Recommendation |
| 23 | Tender Committee Recommendation | TenderCommittee | Approve | Final Approval |
| 24 | Final Approval | CompetentAuthority | Approve | Agreement Completed |
| 25 | Agreement Completed | DepartmentOfficer or Contractor | Submit / Advance | Work Order Generated |
| 26 | Work Order Generated | DepartmentOfficer or Contractor | Submit / Advance | Work Started |
| 27 | Work Started | DepartmentOfficer or Contractor | Submit / Advance | Measurement Book Updated |
| 28 | Measurement Book Updated | Contractor | Submit / Advance | Bill Submitted |
| 29 | Bill Submitted | CAFOOfficer | Approve | Bill Approved |
| 30 | Bill Approved | CAFOOfficer or Admin | Submit / Advance | Payment Completed |

Notes:
- At each step, Approve/Reject is visible only to the mapped role for that status.
- "Rejected" can occur from approval stages and requires resubmission per department workflow.
- For existing databases, run `Database/05_AlignTechnicalEvaluationWithPdf.sql` to align Technical Evaluation ownership and stage mapping with this SOP.

## What's fully implemented vs. left as a clear extension point

This is a genuinely large enterprise system (30+ statuses, 10 roles, 18 tables). To keep every piece
**real, working code** rather than a sprawling, half-finished surface, the build prioritizes depth on
the parts that make the workflow actually function end-to-end:

**Fully wired, end-to-end (DB → API → UI):**
Auth (JWT, register/login, role claims) · Tender CRUD · the full `WorkflowEngine` state machine
covering every stage from Department Requirement through Payment, including the publication-approval
gate and the independent CAFO/MCA financial sign-offs
(Submit/Approve/Reject with role enforcement, atomic DB transitions, full audit trail) · BOQ ·
document upload/listing · technical & financial bids incl. L1 ranking and contractor selection ·
agreements, work orders, progress tracking, e-MB, bills, payments · pending-approvals queue ·
dashboard stats · admin user management.

**Implemented with a deliberately simple first pass, designed to extend without rework:**
- *Excel export / PDF reports* — not wired up. The DTO shapes returned by every list endpoint are
  already flat and export-ready; adding `ClosedXML`/`QuestPDF` calls in the relevant controller
  action is the natural next step.
- *Pagination/search/filter* — implemented on the main Tenders list; the same `PaginationQuery` /
  `GetPagedAsync` pattern is ready to copy onto Bills/Contractors/Users if you want server-side
  paging there too (those currently return full lists, which is fine at expected admin-table scale).
- *WorkflowStages admin UI* — the table and seed data exist so Admin can reassign which role owns
  which approval stage, but there's no screen for it yet (today it requires `UPDATE WorkflowStages`
  or a small admin page calling a new `WorkflowStagesController`).
- *Contractor self-service portal* — Contractors authenticate and the API endpoints they need exist
  (submit bids, update progress, submit bills), but there's a single Work hub UI rather than a
  contractor-specific stripped-down view.

Everything above is real C#/SQL/JS — there is no mocked data and no placeholder business logic — the
list above is about *breadth of polish*, not missing functionality in the core pipeline.
