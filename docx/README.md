# Tender Management System

> **Current setup notice:** This document contains historical architecture and workflow notes.
> The supported SQL Server setup is the repository-root `SMC_TENDER_DATABASE.sql`; do not follow
> SQL commands, seeded-login instructions, or migration directions in older material. See the
> root `README.md`, `Frontend/README.md`, and `SMC_TENDER_MASTER_DOCUMENTATION.md` for current
> setup and deployment guidance.

A full-stack digitization of the traditional tender workflow — from department requirement through
estimate approval, technical sanction, GB/administrative/budget approvals, tender publication, bid
opening & evaluation, contractor selection, agreement, work execution, e-MB, billing, and payment
release — built as a clean, layered, role-based enterprise application.

```
TenderMngt/
├── SMC_TENDER_DATABASE.sql  Authoritative SQL Server setup and upgrade
├── Database/        Database relationship documentation
├── Backend/          ASP.NET Core 8 Web API — Clean Architecture (Domain / Application / Infrastructure / API)
└── Frontend/          React 18 + Material UI SPA
```

## How the pieces fit together

- **Database** — the repository-root `SMC_TENDER_DATABASE.sql` is the only supported SQL Server
  setup file. Its guarded SQLCMD `Fresh` and `Upgrade` modes create schema while preserving existing
  data. Fresh mode leaves application and reference tables empty; trusted account and workflow
  provisioning is required before use.

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

1. **Database** (SQL Server):
   ```bash
   sqlcmd -S "localhost\SQLEXPRESS02" -E -b -v DatabaseName="TenderManagementDB" -v InstallMode=Fresh -v BackupVerified=NO -i "SMC_TENDER_DATABASE.sql"
   ```
   Fresh mode is only for a new, empty database. For an existing database, take and restore-test a
   backup, then rehearse `InstallMode=Upgrade` on an isolated restored copy before production
   approval. Never run Fresh mode against a populated database.

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

4. **Provision users and departments** through the approved trusted administration process after
   installation. Do not rely on fixed role IDs, sample logins, or automatic Super Admin creation.

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
- For existing databases, rehearse the canonical installer's `Upgrade` mode on a verified restored
  copy; do not run individual historical migration scripts.

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
- *WorkflowStages admin UI* — the schema exists, but Fresh mode does not seed stage rows and there
  is no screen for managing them yet. Complete approved workflow provisioning before using
  role-specific approvals.
- *Contractor self-service portal* — Contractors authenticate and the API endpoints they need exist
  (submit bids, update progress, submit bills), but there's a single Work hub UI rather than a
  contractor-specific stripped-down view.

Everything above is real C#/SQL/JS — there is no mocked data and no placeholder business logic — the
list above is about *breadth of polish*, not missing functionality in the core pipeline.
