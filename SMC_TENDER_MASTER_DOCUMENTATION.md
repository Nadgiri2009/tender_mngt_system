# SMC Tender Management Portal — Master Documentation

**Inventory snapshot:** 17 November 2026
**Database inspected:** `TenderManagementDB` on `localhost\SQLEXPRESS02`
**Status:** This document describes the inspected source and local database. It does not assert that all workflow stages are implemented or deployment-ready.

## 1. Project Overview

The repository contains two application generations sharing SQL Server object names: an active Next.js frontend and ASP.NET Core API, and a legacy Classic ASP.NET Web Forms module. The Web Forms module is where the additive version-2 SMC tender workflow has been implemented. The newer frontend/API still uses the older role catalog and version-1 tender lifecycle. Treat the two authentication and workflow surfaces as distinct until parity and integration are verified.

## 2. Project Objectives

- Digitize the municipal tender lifecycle from departmental requirement and approvals through publication, bids, award, work execution, measurement, billing, and payment.
- Preserve existing version-1 data and behavior while adding version-2 tender creation, department/role authorization, workflow assignments, and audit history.
- Prevent cross-department, cross-tender, and cross-contractor access through server-side checks.
- Keep publication, bid opening, financial review, document handling, and post-award records attributable and auditable.
- Provide public tender information without exposing unpublished tenders, staff-only records, or bid contents.

## 3. Technology Stack

| Area | Current implementation |
|---|---|
| Active web frontend | Next.js 15, React, TypeScript |
| Active API | ASP.NET Core 8 Web API, C# |
| API persistence | EF Core / SQL Server |
| Legacy module | ASP.NET Web Forms, C#, .NET Framework 4.8, ADO.NET |
| Database | Microsoft SQL Server; local Web Forms connection points to `localhost\SQLEXPRESS02`, database `TenderManagementDB` |
| Legacy staff portal authentication | Registered Indian mobile number plus six-digit OTP |
| API authentication | Email/password, BCrypt hash verification, JWT bearer token |
| Legacy file storage | IIS-hosted upload root; bidder files use generated names and an authorized download handler |

Use the SQL scripts as the schema authority. The Infrastructure EF migrations folder says it is intentionally empty and the project is SQL-first.

## 4. Existing Folder Structure

```text
tender_mngt_system/
  Backend/
    src/TenderManagement.API/             ASP.NET Core API, controllers, middleware, configuration
    src/TenderManagement.Application/      DTOs, interfaces, services, workflow rules
    src/TenderManagement.Domain/           entities and enums
    src/TenderManagement.Infrastructure/   EF DbContext, repositories, SQL/JWT/file services
  Database/                                SQL setup, additive migrations, preflight, ER diagram
  Frontend/
    app/, src/                             active Next.js pages, components, hooks, API clients
    Pages/, Helpers/                       legacy Web Forms pages and ADO.NET helpers
    Content/, public/, Uploads/            shared styling, static assets, legacy upload area
  docx/                                    workflow analyses, status reports, SOPs
  .github/instructions/                   Postman prompt instructions (not project documentation)
```

Generated folders such as `node_modules`, `.next`, `bin`, and `obj` are intentionally omitted.

## 5. Application Architecture

The active API follows `API → Infrastructure → Application → Domain`. The API uses controllers, JWT authentication, authorization attributes, global exception middleware, and Swagger in Development. EF Core maps the shared core tender tables and `UnitOfWork` executes the legacy `sp_UpdateTenderStatus` procedure for API workflow transitions.

The Web Forms module uses `ModulePage`/`ModuleDb`, parameterized ADO.NET, code-behind authorization, and the same `dbo` database. `WorkflowEngine`/`WorkflowRules` govern the legacy flow; `TenderWorkflow.aspx` uses version-2 stage definitions, explicit assignment checks, and transactions. Public API dashboard routes return published tenders/departments only; unpublished tender records, staff lists, and workflow actions are protected. The module's default document is `Pages/Home.aspx`; the active Next.js homepage is `/`. These are not interchangeable deployment entry points.

## 6. Authentication and OTP Login

### Web Forms portal

- Login identifier: registered mobile number; the UI accepts ten-digit Indian mobile numbers.
- OTP: six digits, five-minute expiry, at most five verification attempts, and three requests per session in fifteen minutes.
- OTP values are salted and hashed in session state; they are not rendered in the browser.
- Registration also verifies a mobile OTP, requires name and email under the existing `Users` schema, and creates the `Contractor / Vendor` role account without requesting a password.
- Web Forms email is a required contact/schema field, not the OTP login identifier. The module uses only the authenticated `UserId` supplied by a trusted server-side host/session handoff; it reloads active role, department, and permissions from SQL. Session `RoleName`/`DepartmentId` values are display/handoff hints, never authorization evidence.
- Current `Web.config` sets `OtpDeliveryMode=Console`. Delivery is restricted to local requests and writes to a development log; it is not SMS and must not be used in production. `Sms` requires configured HTTPS gateway settings; `Auto` uses SMS only when configured.
- The SMS settings are `SmsGatewayUrl`, `SmsGatewayAppId`, `SmsGatewayUserId`, `SmsGatewayPassword`, `SmsGatewaySender`, `SmsGatewayDltTemplateId`, and `SmsOtpMessageTemplate`; each supports an `SMC_`-prefixed environment variable. The template must match the sender's DLT registration and include `{OTP}`. For local troubleshooting, the OTP is written to `%TEMP%\TenderManagement-DevelopmentOtp.log`; never expose it in the browser or use Console delivery publicly.
- After login, the module reloads active roles, permissions, and department from SQL Server. Session role/department strings are not the authorization source.

### ASP.NET Core API

The Next.js/API sign-in now requests a six-digit mobile OTP at `/api/auth/request-otp` and verifies it at `/api/auth/login`. Codes expire after five minutes, are single-use, and are limited to three requests per 15 minutes and five verification attempts. In local Development, the randomly generated code is logged by the API; no fixed OTP is used. Production and non-local requests currently return `503`; no SMS provider is implemented. The in-memory challenge store is for single-instance development only.

Contractor self-registration still requires email/password and remains restricted by `AuthService` to the legacy `Contractor` role. API authorization still uses legacy `[Authorize(Roles = "...")]` role claims; it does not yet evaluate `Permissions`/`RolePermissions`. Do not assume the Web Forms permission mappings automatically authorize API endpoints.

The live database contains five OTP/rate-limit-related tables that have no matching source references in the inspected repository. Their owning component and operational status are unknown; preserve them, do not expose or inspect their challenge contents, and do not assume they implement the Web Forms OTP flow.

## 7. Six Primary Roles

These are the version-2 SMC role definitions:

1. **Super Admin** — full authorized administration and access governance. Requires controlled, independently verified provisioning.
2. **Junior Engineer (JE)** — department-scoped version-2 tender preparation, publication, primary review, shortfall approval, financial opening, and post-approval document generation.
3. **Nivida Kaksh (Tender Cell)** — assigned secondary verification, shortfall creation/clearance, and workflow responsibilities.
4. **Nivida Samiti Adhyaksha** — assigned financial-bid-opening approval or rejection.
5. **Chief Auditor** — assigned final financial approval or return/rejection.
6. **Contractor / Vendor** — own registration, bids, documents, and shortfall responses.

The database also retains legacy roles such as `Admin`, `DepartmentOfficer`, `TechnicalSanctionOfficer`, `TenderCommittee`, `TenderSection`, and `Contractor`. Do not equate similarly named legacy and version-2 roles.

## 8. Role and Login ID Inventory

### Version-2 role assignments

The six role definitions exist and are active in the inspected database. **None currently has a user assigned to it.** The displayed account counts are from `Users.RoleId`; there is no user-to-many-roles mapping table.

| No. | Role name | Current login ID / user | User name | Department | Account status | Provisioning status | Login method |
|---:|---|---|---|---|---|---|---|
| 1 | Super Admin | No account assigned. `9588629955` currently belongs to an `Admin` account, not this role. | None assigned | — | No account | Not provisioned; trusted identity verification still required | OTP in Web Forms when provisioned |
| 2 | Junior Engineer (JE) | None assigned | None assigned | — | No account | Role defined; user provisioning pending | OTP in Web Forms |
| 3 | Nivida Kaksh (Tender Cell) | None assigned | None assigned | — | No account | Role defined; user provisioning pending | OTP in Web Forms |
| 4 | Nivida Samiti Adhyaksha | None assigned | None assigned | — | No account | Role defined; user provisioning pending | OTP in Web Forms |
| 5 | Chief Auditor | None assigned | None assigned | — | No account | Role defined; user provisioning pending | OTP in Web Forms |
| 6 | Contractor / Vendor | None assigned to this new role | None assigned | — | No account | Role defined; user provisioning pending | OTP in Web Forms |

**Super Admin safeguard:** the registered `Administrator` account with mobile `9588629955` is currently assigned the legacy `Admin` role. A matching mobile number is not proof of identity and does not authorize changing that account to Super Admin.

### Separate `tender_tracking` target database

The requested `tender_tracking` database was created separately; `TenderManagementDB` was not modified. It contains the four requested active role definitions and 33 role-permission mappings copied from the corresponding existing workflow roles. Four disabled placeholder user rows were also created because real phone numbers have not yet been provided. Their login identifiers are invalid dummy values (`0000000001` through `0000000004`), their email addresses use the reserved `.invalid` domain, and their password hashes were generated from discarded random values. These rows cannot sign in; replace each mobile number with a verified number and activate the account only when ready. No real OTP or login credential is available for these placeholders.

### All existing user accounts

The following inventory is from read-only SQL queries against `dbo.Users`, joined to the current single `dbo.Roles` assignment, `dbo.Departments`, and contractor profiles. Email is included as the API's configured login identifier; mobile is the Web Forms OTP identifier. No password hashes or OTP challenge values were queried.

| No. | Existing role | Login ID (mobile) | API login ID (email) | User name | Department | Account status | Contractor profile |
|---:|---|---|---|---|---|---|---|
| 1 | Admin | `9588629955` | `admin@example.com` | Administrator | Administration | Active | — |
| 2 | CAFOOfficer | `9000000005` | `cafo.officer@smc.gov.in` | CAFO Officer | Administration | Active | — |
| 3 | CompetentAuthority | `9000000008` | `competent.authority@smc.gov.in` | Competent Authority | Administration | Active | — |
| 4 | Contractor | `9000000009` | `contractor@smc.gov.in` | Demo Contractor | Unassigned | Active | No profile linked |
| 5 | Contractor | `9000000012` | `shreyasnadgiri20@gmail.com` | Shreyas | Administration | Active | `PORTAL-00000013` |
| 6 | DepartmentOfficer | `9000000004` | `dept.officer@smc.gov.in` | Department Officer | Administration | Active | — |
| 7 | DPDCOfficer | `9000000003` | `dpdc.officer@smc.gov.in` | DPDC Officer | Administration | Active | — |
| 8 | DPDCOfficer | `9000000013` | `shreyasnadgiri@gmail.com` | Rahul | Garden and Parks | Active | — |
| 9 | GBOfficer | `9000000002` | `gb.officer@smc.gov.in` | GB Officer | Administration | Active | — |
| 10 | MCAOfficer | `9000000006` | `mca.officer@smc.gov.in` | MCA Officer | Administration | Active | — |
| 11 | RESP | `9000000010` | `resp.officer@smc.gov.in` | RESP Officer | Administration | Inactive | — |
| 12 | RESP | `9000000011` | `resp.officer2@smc.gov.in` | RESP Officer Two | Administration | Inactive | — |
| 13 | TechnicalSanctionOfficer | `9000000001` | `ts.officer@smc.gov.in` | Technical Sanction Officer | Administration | Active | — |
| 14 | TenderCommittee | `9000000007` | `tender.committee@smc.gov.in` | Tender Committee | Administration | Active | — |

There are 14 user rows: 12 active and 2 inactive. The normalized mobile values and unique email values are distinct in this snapshot. All users have a valid single role; two users have no department (one is the unlinked Contractor), and no user has multiple roles. Existing legacy `Contractor` accounts are not accounts for the new `Contractor / Vendor` role. Only one contractor profile exists, linked to Shreyas. No account is in a separate `Pending` state in the inspected schema.

## 9. Role-Based Access Control

`Roles` is the user-to-role source (`Users.RoleId`). `Permissions` and `RolePermissions` define role grants; `IsSystemRole` protects system roles from ordinary role administration. The live database contains 18 roles and 152 role-permission mappings. `TenderWorkflowAssignments` scopes version-2 queue actions to tender, stage, role, optional assigned user, department, and current assignment.

The inspected pre-consolidation database contained existing role and permission rows. The
canonical Fresh installer now leaves these tables empty; Upgrade preserves any existing rows
without adding or changing them. Do not enable staff access until a trusted bootstrap has created
the initial account and required role/permission configuration. Ordinary users must not be able to
assign or elevate themselves to Super Admin. Client-side button visibility is not an authorization
boundary; every page action, postback, direct URL, and document download must be checked server-side.

## 10. Department-Level Restrictions

- JE access requires the tender's department to match the JE's active department assignment and the current workflow assignment.
- Tender Cell, Samiti, and Chief Auditor access requires a matching assigned workflow queue; role possession alone is insufficient.
- DepartmentOfficer version-1 lists and unpublished records are scoped to their own `CreatedBy` tenders; published public notices remain readable.
- Contractor access is limited to their own linked contractor profile, bids, documents, and shortfalls.
- Super Admin's broad access is only available after trusted provisioning.
- Current local data has seven active departments; the new version-2 roles have no assigned users or workflow rows.

## 11. User Management

`AdminUsers.aspx` manages users, activation, department assignment, and contractor-profile linkage; creation and role grants require server-side permission. `AdminRoles.aspx` manages custom roles and permission mappings, while system roles are protected from rename/delete/deactivation. User management does not provision Super Admin through public registration. Account state is represented by `Users.IsActive`; `RoleId` is non-null and foreign-key constrained.

## 12. Contractor Registration

Web Forms registration accepts verified mobile OTP, name, and email and creates a `Contractor / Vendor` user. It does not create a contractor profile; an administrator must link a `dbo.Contractors` record before bidding. The separate API registration uses email/password and legacy `Contractor` role and creates a contractor profile. Current data has two active legacy Contractor users but only one linked profile.

## 13. Complete Tender Lifecycle

Two status/workflow tracks coexist:

- **Version 1 / API legacy:** `Draft` through pre-publication approvals, bid/evaluation stages, and post-award work, billing, and payment.
- **Version 2 / Web Forms:** separate `WorkflowVersion=2` stages, explicit assignment-based queues, JE primary review, intended Tender Cell/shortfall/financial approval chain, and intended Work Order/Agreement completion.

Do not treat the presence of version-2 tables or stage rows as proof that the corresponding pages and server actions are complete.

## 14. Version-2 Tender Creation

JE creates a version-2 tender for the assigned department using the existing `Tender` record (`WorkflowVersion=2`), not a parallel tender table. The workflow retains budget and technical-sanction data, pre-bid meeting fields, a scheduled technical opening field, and a required bidder-document checklist. Existing tenders remain version 1. Current local records: 29 tenders, all version 1.

The legacy API's `CreateTenderDto` documents a title length of 5–250 characters, description up to 4,000 characters, positive department ID, estimated amount greater than 0.01, tender type up to 50 characters, optional submission deadline, and supporting document uploads. The legacy API path is distinct from JE version-2 intake and its authorization rules; do not assume these DTO constraints are all enforced by the Web Forms form.

## 15. JE Draft Submission

The JE submission gate requires the established tender pack, including a BOQ item, required tender-document categories, and at least one configured required bidder-document type. The legacy preparation pack requires the four categories **Tender Proposal**, **Tender Document**, **Technical Supporting Document**, and **Financial Supporting Document**, plus at least one BOQ item before it can be forwarded. Draft BOQ edits are limited to Draft; the legacy application restricts draft edit/soft-delete to Admin and scopes DepartmentOfficer data to the officer's own `CreatedBy` tenders. Submission and workflow history use the same Tender ID and transactions. The version-2 draft submission and required-document configuration are implemented; end-to-end testing with a version-2 JE account has not been performed.

## 16. Tender Publication Scheduling

Legacy publication scheduling records UTC start, original/current close, duration, approver, remarks, and modification time. The CAFO/Admin schedule procedure requires a published tender, a future valid period, no existing opening/bids, and applicable department checks. Extension requests are limited to three approved requests, require supporting files/reason, and are reviewed by CAFO/Admin. The public tender list exposes scheduled published notices only.

Version-2 publication scheduling is available through the JE workflow. Do not equate publication approval/status with the separate schedule dates.

## 17. Pre-Bid Meeting Management

Tender stores `PreBidMeetingUtc`, `PreBidLocation`, and `PreBidDetails` for version-2 meeting information. Inputs are converted to UTC and shown in India Standard Time. The existing documentation does not establish a notification integration; do not claim that contractors receive pre-bid or extension notices automatically.

## 18. Initial Tender Opening

This is the initial technical/eligibility opening, not financial bid opening. The legacy lifecycle procedure enforces the approved publication closing time, single opening record, actual UTC timestamp, user, operator, designation, and remarks, then makes evaluation eligible after the configured delay. The version-2 JE opening waits until the publication window closes and transitions to primary verification. Bid amounts must remain hidden at this stage.

## 19. JE Primary Verification

Implemented in the Web Forms version-2 queue, subject to end-to-end verification:

- Contractors upload validated technical documents under generated server paths and document types configured for the tender.
- JE reviews bidders and each uploaded document separately, records decisions/remarks/user/timestamp, and records missing required types as not verified.
- Completion is blocked until every bidder and required document has a decision; forwarding is gated on completion.
- The JE verification page does not query or expose financial bids.

Migration 11 adds `TechnicalBidDocuments.DocumentType` and nullable `UploadedBy`; migration 12 creates `TenderRequiredBidDocuments`.

## 20. Tender Cell Secondary Verification

The schema and version-2 workflow stages exist, including `TenderVerificationReviews` and document-wise history. Tender Cell must independently review each bidder/document, inspect JE results and remarks, and either approve or start a shortfall. **The Tender Cell UI/action implementation remains pending**; do not mark secondary verification complete based on schema alone.

## 21. Shortfall Management

The schema supports multiple shortfall items per bidder, descriptions, corrective action, configurable positive submission duration/deadline, history, and status. No unverified number of days should be hardcoded. Shortfall generation and contractor response/clearance actions are **pending implementation**.

## 22. JE Shortfall Approval

Migration 10 defines a JE approval stage and stores approval user/time. Only an approved shortfall is intended to be released to a contractor. JE approval/rejection UI, transition validation, and assignment scoping remain pending.

## 23. Contractor Document Submission

The database has `TenderShortfallSubmissions` for individual item uploads. The contractor should see only their own approved shortfalls and deadlines, upload corrected files using server-side validation and generated paths, and see submission status. The version-2 contractor shortfall dashboard/actions remain pending.

## 24. Tender Cell Shortfall Clearance

The database can retain submissions and clearance/continued-deficiency status. Tender Cell clearance, deficiency continuation with full history, notifications, and resume-verification actions remain pending implementation. Existing no-notification claims must be respected until a delivery service is implemented.

## 25. Nivida Samiti Adhyaksha Approval

The intended queue only accepts tenders after primary and secondary review and all shortfalls are cleared. It records approval/rejection, actor, role, timestamp, and remarks. Database structures and stages exist; Samiti approval UI and server workflow actions remain pending. A rejected opening must return through an explicit review path rather than proceed automatically.

## 26. JE Financial Bid Opening

Financial opening is separate from the initial technical opening. Migration 10 defines `TenderFinancialBidOpening`, bidder-level `TenderFinancialBidOpeningDetails`, and supporting documents. Only Samiti-approved tenders should be opened; the JE records actual opening time/operator/remarks and bidder quoted amounts before sending to Chief Auditor. These actions remain pending implementation.

## 27. Chief Auditor Final Financial Approval

The intended Chief Auditor queue contains only JE-submitted financial openings and includes tender, estimate, bidder amounts, and supporting evidence. The decision must be append-only and capture user, time, remarks, and approved amount/document where applicable. Approval/return workflow and enforcement are pending; JE cannot generate final documents before approval.

## 28. JE Work Order Generation

Version-2 work-order table extensions and `TenderGeneratedDocuments` support approved amount, work description, terms, signatory, and versioned generated files. Dedicated draft/finalize/PDF preview/download/history operations are pending. Existing version-1 `WorkManagement.aspx` functionality must remain distinct and intact.

## 29. JE Tender Agreement Generation

Version-2 agreement extensions and generated-document history support agreement number/date, approved amount, terms, and version numbers. The new final-approval-gated generation/PDF/versioning flow remains pending. Do not overwrite finalized versions or use fabricated tender/contractor/amount data.

## 30. BOQ and Document Management

The existing schema includes tender documents, computed BOQ amount (`Quantity * Rate`), technical bids and multiple technical bid documents, financial bids/documents, evaluation documents, shortfall submissions, generated work orders/agreements, and document metadata/history. `TenderDocuments` categories include proposal/tender/technical/financial support, estimate, technical sanction, and budget copies. File paths are stored as application metadata; user-supplied paths must not be trusted. Version-2 bidder uploads use generated names and the authenticated `DocumentDownload.aspx` handler.

## 31. Tender Status Management

The legacy API's exact `TenderStatus` strings are: `Draft`, `Requirement Submitted`, `Estimate Prepared`, `TS Approval Pending`, `TS Approved`, `GB Approval Pending`, `GB Approved`, `Administrative Approval Pending`, `Administrative Approved`, `Budget Approval Pending`, `Budget Approved`, `Tender Draft Created`, `Tender Committee Approval`, `Publication Approval Pending`, `Tender Published`, `Technical Bid Opening`, `Technical Evaluation`, `Technical Evaluation Approved`, `Financial Bid Opening`, `Financial Evaluation`, `CAFO Rate Opinion`, `MCA Opinion`, `Tender Committee Recommendation`, `Final Approval`, `Agreement Completed`, `Work Order Generated`, `Work Started`, `Measurement Book Updated`, `Bill Submitted`, `Bill Approved`, `Payment Completed`, and `Rejected`. Tender types in the API enum are `Open`, `Limited`, `Single`, and `EOI`; the pre-consolidation SQL catalog instead contained `Works`, `Goods`, `Services`, and `Consultancy` (see conflicts below). A Fresh install leaves that catalog empty.

The legacy `WorkflowEngine` is authoritative for API transitions. The inspected pre-consolidation
database contained a `WorkflowStages` mapping; the canonical Fresh installer leaves that table
empty, and workflow-stage CRUD is not implemented. Provision stage mappings through an approved
process before role-specific workflow queues are used. The existing mapping was:

| Order | Stage | Pending → approve status | Reject status | Approver role |
|---:|---|---|---|---|
| 1 | RequirementSubmission | Requirement Submitted → Estimate Prepared | Draft | DepartmentOfficer |
| 2 | TechnicalSanction | TS Approval Pending → TS Approved | Rejected | TechnicalSanctionOfficer |
| 3 | GBApproval | GB Approval Pending → GB Approved | Rejected | GBOfficer |
| 4 | AdministrativeApproval | Administrative Approval Pending → Administrative Approved | Rejected | DPDCOfficer |
| 5 | BudgetApproval | Budget Approval Pending → Budget Approved | Rejected | CAFOOfficer |
| 6 | TenderCommitteeApproval | Tender Committee Approval → Publication Approval Pending | Rejected | TenderCommittee |
| 7 | PublicationApproval | Publication Approval Pending → Tender Published | Rejected | CompetentAuthority |
| 8 | TechnicalEvaluation | Technical Evaluation → Technical Evaluation Approved | Rejected | TenderSection |
| 9 | TechnicalEvaluationCommitteeApproval | Technical Evaluation Approved → Financial Bid Opening | Rejected | TenderCommittee |
| 10 | CAFORateOpinion | CAFO Rate Opinion → Financial Evaluation | Rejected | CAFOOfficer |
| 11 | MCAOpinion | MCA Opinion → Tender Committee Recommendation | Rejected | MCAOfficer |
| 12 | TenderCommitteeRecommendation | Tender Committee Recommendation → Final Approval | Rejected | TenderCommittee |
| 13 | FinalAuthorityApproval | Final Approval → Agreement Completed | Rejected | CompetentAuthority |
| 14 | BillApproval | Bill Submitted → Bill Approved | Rejected | CAFOOfficer |

Version-2 stage rows in `WorkflowStages` define separate edges; `Tender.WorkflowVersion` distinguishes them. Status strings are stored in `Tender.CurrentStatus`; do not renumber statuses or replace history.

## 32. Workflow Transition Rules

- Use the same `TenderId` through every stage.
- Check authenticated user, role/permission, current assignment, department, tender ID, and current status on the server for every action.
- Never rely on button visibility alone.
- Use a SQL transaction for a status change plus corresponding history/audit/assignment updates.
- Preserve separate `ApprovalHistory`, `TenderLifecycleHistory`, and `TenderWorkflowHistory`; they serve different workflows and must not be merged.
- Enforce one-time technical and financial opening and reject stale/duplicate actions.
- Do not allow direct status updates that bypass approval and shortfall gates.
- A no-bid/cancellation transition is not specified; this remains a business decision.

The migration-10 version-2 stage map, including the configured role and approve/reject destinations, is:

| Order | Stage | Role | Pending → approve status | Reject status |
|---:|---|---|---|---|
| 101 | Final_JE_SubmitTender | JE | Draft → Submitted | Draft |
| 102 | Final_JE_PublishTender | JE | Submitted → Tender Published | Submitted |
| 103 | Final_JE_OpenTechnicalBids | JE | Tender Published → Primary Verification Pending | Tender Published |
| 104 | Final_JE_CompletePrimaryVerification | JE | Primary Verification Pending → Primary Verification Completed | Primary Verification Pending |
| 105 | Final_JE_ForwardToTenderCell | JE | Primary Verification Completed → Tender Cell Verification Pending | Primary Verification Completed |
| 106 | Final_Cell_CompleteSecondaryVerification | Tender Cell | Tender Cell Verification Pending → Financial Bid Opening Approval Pending | Tender Cell Verification Pending |
| 107 | Final_Cell_CreateShortfall | Tender Cell | Tender Cell Verification Pending → Shortfall Pending JE Approval | Tender Cell Verification Pending |
| 108 | Final_JE_ApproveShortfall | JE | Shortfall Pending JE Approval → Awaiting Contractor Submission | Tender Cell Verification Pending |
| 109 | Final_Contractor_SubmitShortfall | Contractor / Vendor | Awaiting Contractor Submission → Shortfall Documents Submitted | Awaiting Contractor Submission |
| 110 | Final_Cell_ClearShortfall | Tender Cell | Shortfall Documents Submitted → Shortfall Cleared | Awaiting Contractor Submission |
| 111 | Final_Cell_ResumeVerification | Tender Cell | Shortfall Cleared → Tender Cell Verification Pending | Shortfall Cleared |
| 112 | Final_Samiti_ApproveFinancialOpening | Samiti Adhyaksha | Financial Bid Opening Approval Pending → Financial Bid Opening Approved | Financial Bid Opening Rejected |
| 113 | Final_Cell_ReverifyRejectedOpening | Tender Cell | Financial Bid Opening Rejected → Tender Cell Verification Pending | Financial Bid Opening Rejected |
| 114 | Final_JE_OpenFinancialBids | JE | Financial Bid Opening Approved → Final Financial Approval Pending | Financial Bid Opening Approved |
| 115 | Final_Auditor_ApproveFinancialBids | Chief Auditor | Final Financial Approval Pending → Final Financial Approval Completed | Final Financial Approval Rejected |
| 116 | Final_JE_ResubmitFinancialApproval | JE | Final Financial Approval Rejected → Final Financial Approval Pending | Final Financial Approval Rejected |
| 117 | Final_JE_StartWorkOrder | JE | Final Financial Approval Completed → Work Order Pending | Final Financial Approval Completed |
| 118 | Final_JE_GenerateWorkOrder | JE | Work Order Pending → Work Order Generated | Work Order Pending |
| 119 | Final_JE_StartAgreement | JE | Work Order Generated → Tender Agreement Pending | Work Order Generated |
| 120 | Final_JE_GenerateAgreement | JE | Tender Agreement Pending → Tender Agreement Generated | Tender Agreement Pending |
| 121 | Final_JE_CompleteTender | JE | Tender Agreement Generated → Completed | Tender Agreement Generated |

This is the migration's configured transition map, not a claim that all listed actions have application handlers. Sections 20–29 distinguish implemented and pending UI/server behavior.

## 33. Database Architecture

The application uses a shared `dbo` schema. `SMC_TENDER_DATABASE.sql` is the only supported
installer and supports guarded Fresh and additive Upgrade modes. Its isolated Fresh rehearsal
created 57 user tables (56 application tables plus the setup ledger), 106 foreign keys, 116
application indexes, 10 stored procedures, 4 triggers, and no views. The API EF model maps the
core roles/users/departments/tender/bid/work/post-award objects; the Web Forms module uses the
shared lifecycle, RBAC, assignment, verification, shortfall, and generated-document objects.
These counts describe the tested installer output, not the populated local database, which was
not modified.

The five live procedures are `sp_UpdateTenderStatus`, `sp_GetDashboardStats`, `sp_GetPendingApprovalsForRole`, `sp_GetFinancialBidRanking`, and `sp_GetTenderFullHistory`. The four live triggers are `TR_TenderBidOpening_Immutable`, `TR_TenderEvaluationDocuments_Immutable`, `TR_TenderLifecycleHistory_Immutable`, and `TR_TenderBudgetOpinions_Immutable`. The five migration-09 lifecycle procedures listed in Sections 35 and 40 are absent from the live database.

There are 29 tender rows, all `WorkflowVersion=1`; one contractor profile, seven departments, 14 users, 18 roles, 152 role-permission mappings, and zero active workflow assignments. No version-2 tenders or version-2 verification/shortfall/financial approval/generated-document rows were found.

## 34. Database Tables and Relationships

No tables were merged. Similar names conceal different business meanings; keep them separate.

| Group | Tables | Main relationships/purpose |
|---|---|---|
| Identity/master | `Roles`, `Users`, `Departments`, `Contractors`, `Permissions`, `RolePermissions`, `SuperAdminProvisioningAudit` | Users reference one role and optional department; contractors may link to one user; role permissions map roles to permissions; provisioning audit records privileged assignment evidence. |
| Tender core | `Tender`, `Estimate`, `ApprovalHistory`, `TenderDocuments`, `BOQ`, `Announcements`, `WorkflowStages` | Tender references department/creator; estimate, documents, BOQ, and histories reference Tender; announcements reference creator; stages reference approver role. |
| Bids/award/work | `TechnicalBid`, `TechnicalBidDocuments`, `FinancialBid`, `Agreement`, `WorkOrder`, `WorkExecution`, `MeasurementBook`, `Bills`, `Payments`, `AuditLog` | Bid records link tender and contractor; work/billing records retain their own actor, contractor, tender, and payment relationships. |
| Publication/evaluation | `TenderPublicationSchedule`, `TenderExtensionRequests`, `TenderBidOpening`, `TenderEvaluationSettings`, `TenderEvaluation`, `TenderEvaluationDocuments`, `TenderLifecycleHistory`, `TenderBudgetOpinions` | Schedule/extension/opening/evaluation records retain UTC times, actors, review status, and history; several audit tables are protected by immutability triggers. |
| Version-2 workflow/documents | `TenderWorkflowAssignments`, `TenderWorkflowHistory`, `TenderVerificationReviews`, `TenderDocumentVerificationHistory`, `TenderRequiredBidDocuments` | Tender/stage/role/user/department assignments; bidder and document review history; per-tender required types. |
| Version-2 shortfalls | `TenderShortfalls`, `TenderShortfallItems`, `TenderShortfallSubmissions`, `TenderNotifications` | A shortfall belongs to a tender and contractor, has multiple items and item-level submissions, and may generate user notifications. |
| Version-2 financial/final documents | `TenderFinancialBidOpening`, `TenderFinancialBidOpeningDetails`, `TenderFinancialBidDocuments`, `TenderFinancialApprovals`, `TenderGeneratedDocuments` | Financial opening is separate from initial opening; details point to individual financial bids; generated document versions reference work order or agreement. |
| Auxiliary security | `AuthenticationAttempts`, `LoginOtpChallenges`, `LoginOtpRateLimits`, `MobileOtpChallenges`, `MobileOtpVerificationLockouts` | Present in the live database, but no matching code references were found; preserve until their owner/purpose is confirmed. |
| Lookup/master data | `DocumentTypes`, `TenderTypes`, `TenderMethods`, `Units`, `Designations` | Editable catalogs. `TenderTypes` is the category catalog; `TenderMethods` is the separate procurement-method catalog. |
| Setup tracking | `TenderSchemaMigrations` | Records successfully applied canonical setup stages. It is not business data and is not seeded with user or tender records. |

Core key columns and business data:

| Table | Key and business columns |
|---|---|
| `Roles` | `RoleId`, `RoleName`, `Description`, `IsActive`, `IsSystemRole` |
| `Departments` | `DepartmentId`, `DepartmentName`, `DepartmentCode`, `HeadOfficerName`, `IsActive` |
| `Users` | `UserId`, `FullName`, `Email`, `Mobile`, `PasswordHash`, `RoleId`, `DepartmentId`, `IsActive`, lockout/failure/login timestamps |
| `Contractors` | `ContractorId`, `Name`, unique `RegistrationNo`, contact fields, optional linked `UserId`, `IsActive` |
| `Tender` | `TenderId`, unique `TenderNumber`/`TrackingId`, `Title`, `Description`, `DepartmentId`, `EstimatedAmount`, `TenderType`, `CurrentStatus`, submission/publication dates, `CreatedBy`, `IsDeleted`, `WorkflowVersion`, budget/technical-sanction/pre-bid/opening fields, `CreationToken`, `RowVer` |
| `Estimate` | `EstimateId`, `TenderId`, `Amount`, `PreparedBy`, `DocumentPath`, `Status` |
| `ApprovalHistory` | `ApprovalId`, `TenderId`, stage, previous/new status, action status, actor/role, remarks, UTC action time |
| `TenderDocuments` | `DocumentId`, `TenderId`, `DocumentType`, original file name/path, uploader and upload time |
| `BOQ` | `BOQId`, `TenderId`, item/description, quantity, unit, rate, persisted computed `Amount = Quantity * Rate` |
| `TechnicalBid` / `FinancialBid` | Bid ID, `TenderId`, `ContractorId`, state/submission details; technical terms and technical bid document collection; financial amount/rank |
| `TechnicalBidDocuments` | Document ID, `TechnicalBidId`, file name/path, upload time, document type, optional uploader |
| `Agreement` / `WorkOrder` | Record ID, `TenderId`, `ContractorId`, unique agreement/order number, issue/effective date, amount and, where configured, approved amount, work description, signatory, terms |
| `WorkExecution` / `MeasurementBook` | Tender, progress/remarks/update actor and time; measurement description/quantity/date/recording and approval actors/status |
| `Bills` / `Payments` | Bill/tender/contractor number, amount, status, submission/approval; payment bill link, amount/date, unique transaction number and processor |
| `AuditLog` | `LogId`, user, action, entity name/ID, details, IP address, UTC timestamp |
| `WorkflowStages` | Stage ID/order/name, pending/approve/reject statuses, approver role, active flag, version |
| `TenderPublicationSchedule` | Tender, publication start, original/current close, duration, approver, remarks, modified time |
| `TenderExtensionRequests` | Tender, request number, original/proposed close, application/supporting files, reason, submitter, review decision/actor/time |
| `TenderBidOpening` / `TenderEvaluation` | One opening/evaluation per tender; closing/opening/eligibility times, actor/operator/designation, evaluation state/outcomes/recommendation/report |
| `TenderWorkflowAssignments` / `TenderWorkflowHistory` | Tender/stage/role/user/department, current/completed assignment; workflow version, old/new status, action, actor, timestamp, remarks/document |
| Verification and shortfall tables | Bidder/document, review stage/decision/reviewer/time; shortfall duration/deadline/status, item-specific requirements/corrective action, submissions and clearance history |
| Financial review/final documents | Opening actor/time/status; financial bid detail and quoted amount; approval decision/amount/actor/evidence; generated Work Order/Agreement kind, version, path, generator/time |

The authoritative installer defines 57 user tables in dependency order. Existing business tables keep
their names and relationships; the installer adds `Designations`, `TenderMethods`, and
`TenderSchemaMigrations`. The table groups above comprise the inventory; the SQL file remains the
source of truth for columns, types, defaults, keys, indexes, and database objects.

Additional relationships: `TenderDocuments` is for tender/department documents; `TechnicalBidDocuments` is for a contractor's technical bid; `TenderEvaluationDocuments` is evaluation evidence; `TenderFinancialBidDocuments` belongs to a financial bid; `TenderShortfallSubmissions` belongs to a shortfall item; `TenderGeneratedDocuments` stores immutable document versions. They are not interchangeable.

## 35. SQL Consolidation and Legacy Mapping

Only `SMC_TENDER_DATABASE.sql` is an active SQL setup file. Its setup ledger records the canonical
schema stages (`00-CORE`, `04`-`06`, `08`-`12`) in dependency order. It does not seed application or
reference rows; it creates no accounts, roles, permissions, departments, workflow stages, lookup
values, contractors, tenders, bids, or sample transactions.

The previous scripts and their original SHA-256 manifest are preserved outside the active project
at the session backup location. Their scripts are retired from the active tree after validation:

| Retired file(s) | Consolidated replacement |
|---|---|
| `Database/DatabaseSetup.sql`, `Database/04_AddAnnouncements_Incremental.sql`, `Database/05_AlignTechnicalEvaluationWithPdf.sql`, `Database/06_AddTenderSubmissionStartDate.sql`, `Database/08_AddContractorTenderApplications.sql`, `Database/09_AddTenderLifecycle.sql`, `Database/10_FinalizedTenderWorkflowRbac.sql`, `Database/11_TechnicalBidDocumentMetadata.sql`, `Database/12_TenderBidDocumentRequirements.sql` | `SMC_TENDER_DATABASE.sql` Fresh/Upgrade modes |
| `Database/07_SeedTenderDashboardDemoData.sql` | Removed from supported setup; no demo data is inserted |
| `Database/DepartmentModuleSchema.sql` | Installer preflight and post-install validation |
| `SMC_TENDER_COMPLETE_DATABASE_SETUP.sql` | `SMC_TENDER_DATABASE.sql` |

No existing business tables are merged, renamed, or dropped. Old-to-new table mapping is identity
for every retained table: each existing table retains its name and business meaning. The only new
tables are `Designations`, `TenderMethods`, and `TenderSchemaMigrations`. The category catalog is
`TenderTypes`; the method catalog is separate. This avoids combining unrelated entities or
rewriting historical `Tender.TenderType` values.

The practical design retains separate tables for user/role/permission relationships; tender and
contractor entities; centralized document metadata by business stage; bid opening; document-wise
verification; shortfall items and submission history; financial opening and approvals; Work Order;
Tender Agreement; workflow/audit history. These distinctions preserve responsible-stage ownership,
confidentiality gates, document versions, and append-only approval evidence. No schema merge was
performed solely to reduce table count.

## 36. Deployment and Database Setup

### Fresh database

Run SQLCMD from the repository root against a new, empty database:

```powershell
sqlcmd -S "localhost\SQLEXPRESS02" -E -b -v DatabaseName="TenderManagementDB" -v InstallMode=Fresh -v BackupVerified=NO -i "SMC_TENDER_DATABASE.sql"
```

Fresh mode refuses a non-empty target. Apart from installer-ledger metadata, all application
tables are empty. No accounts, roles, permissions, departments, workflow stages, lookup values,
contractors, tenders, bids, Super Admin accounts, or sample transactions are inserted. Trusted
account and workflow provisioning is required before the application can authorize staff.

### Existing database upgrade

Before any existing-database upgrade, take a full backup, verify a restore, and rehearse on an
isolated restored copy:

```powershell
sqlcmd -S "localhost\SQLEXPRESS02" -E -b -v DatabaseName="TenderManagementDB_UpgradeTest" -v InstallMode=Upgrade -v BackupVerified=YES -i "SMC_TENDER_DATABASE.sql"
```

Upgrade mode is additive: it checks the supported legacy schema, records pre-upgrade business
table row counts, adds missing objects/columns/indexes, and stops on an unsupported or ambiguous
target. It does not seed or overwrite role, permission, workflow, or lookup rows; it does not drop
tables or delete existing data. Verify counts, foreign keys,
`DBCC CHECKCONSTRAINTS`, database objects, and application behavior before seeking production
approval. Never test against the populated `TenderManagementDB` database.

The isolated legacy Upgrade rehearsal preserved its 11 users, 1 department, 0 contractors, and 0
tenders and completed `DBCC CHECKCONSTRAINTS` without errors. The populated local
`TenderManagementDB` was not changed; its earlier read-only inspection reported five missing
lifecycle procedures, so that separate production issue remains unverified and is not repaired by
the rehearsal.

## 37. IIS Configuration

For the legacy Web Forms module:

1. Install .NET Framework 4.8 and the matching targeting pack/Web Build Tools; enable IIS ASP.NET 4.8, .NET Extensibility, ISAPI Extensions, and ISAPI Filters.
2. Publish `Frontend/DepartmentModule.csproj` in Visual Studio/MSBuild as Release to a clean staging folder; do not use `dotnet publish` for this .NET Framework Web Application.
3. Use a dedicated CLR v4 Integrated application pool and HTTPS. Deploy the full output, not just `.aspx` files.
4. Merge the `TenderManagement` connection string and required settings into the host configuration. Preserve the host's authentication, `Web.config`, `Global.asax`, master page, CSS, routes, and default page. The module home is `Pages/Home.aspx`; staff dashboard is `Pages/Default.aspx`.
5. Configure `DepartmentPanelLoginUrl` and, where needed, `SessionUserIdKey`. A trusted server-side host must set the positive database `UserId`; do not accept it from a request parameter/browser form. If the module runs as a separate IIS app, implement secure SSO/session handoff rather than assuming ASP.NET session sharing.
6. Grant the application-pool identity read/execute on publish output, only the necessary SQL permissions, and write access solely to the configured upload root. Validate request limits and file rules; documented Web Forms upload limit is 20 MB per file.
7. Test login, role/department scoping, unauthorized direct URLs, uploads/downloads, and session/cookie settings on a restored test database before deployment. Never deploy Console OTP or debug compilation publicly.

The active Next.js/API deployment uses its own `NEXT_PUBLIC_API_BASE_URL`, API SQL connection string, JWT key, CORS origins, and runtime. Keep those settings in user secrets/environment/secret stores, not source control. The Web Forms module is not a substitute for the active frontend deployment.

## 38. Security Requirements

- No Super Admin assignment based solely on `9588629955`; use trusted identity verification, controlled provisioning, and audit evidence.
- No seeded demo credentials in production.
- Enforce role, assignment, tender, department, contractor ownership, status, and permission checks server-side.
- Use parameterized SQL; keep connection credentials and JWT/SMS secrets out of source.
- Require HTTPS, secure/HttpOnly cookies, session expiration, OTP throttling, and provider-backed SMS for public deployment.
- Validate file size/type/content, generate server filenames, store outside public static paths, and authorize every download.
- Hide financial bid data until the approved financial opening stage.
- Preserve append-only workflow, verification, approval, and generated-document histories.
- No user-controlled filesystem paths or unrestricted public document URLs.

## 39. Audit Trail

`AuditLog` stores user/action/entity/IP/details; `ApprovalHistory` records legacy transition decisions; `TenderLifecycleHistory` records publication/opening/evaluation actions; `TenderWorkflowHistory` records version-2 stage transitions; verification/shortfall/financial tables preserve their own evidence. Migration 09 installs immutable triggers for bid-opening, evaluation-document, lifecycle-history, and budget-opinion records. Do not merge these histories or claim every history table is database-enforced immutable unless its actual trigger/permissions are verified.

## 40. Testing and Validation

### Inspected/verified in this task

- Fresh-mode installation succeeded in disposable `SMC_TenderSetup_Verify_20261117J`: 57 user
  tables including the setup ledger, 106 foreign keys, 116 indexes, 10 stored procedures, 4
  triggers, and 9 setup ledger rows.
- Fresh-mode left application and reference tables empty: roles, permissions, role mappings,
  workflow stages, catalogs, and evaluation settings all contained zero rows.
- Upgrade-mode installation succeeded in disposable `SMC_TenderUpgrade_Verify_20261117K`: 57
  user tables, 106 foreign keys, 116 indexes, 10 stored procedures, 4 triggers, and 9 ledger
  rows. Existing counts remained unchanged: 11 users, 1 department, 17 roles, 50 permissions,
  149 role-permission mappings, and 35 workflow stages.
- `DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS` completed without reporting violations in both
  disposable installation databases.
- The populated local `TenderManagementDB` was not modified.
- Read-only baseline inspection (before this consolidation work) reported 14 users, 18 roles, 7
  departments, 29 version-1 tenders, and no version-2 assignments. That inspection also found five
  lifecycle procedures used by Web Forms missing from `TenderManagementDB`; the populated database
  remains unchanged and that operational issue still requires an approved, separate review.
- SHA-256 checks matched all 12 retired SQL files against their preserved archive copies.
- Web Forms and API builds succeeded; the API reported zero warnings and zero errors. The Next.js
  production build, including lint/type checking, succeeded.

### Not demonstrated

No Web Forms end-to-end role/department tests, OTP SMS delivery, IIS deployment, PDF generation, or
complete version-2 workflow test was performed. Database rehearsals do not establish production
readiness. Workflow-status CRUD is not available: transitions remain deployment-controlled because
they are coupled to server-side authorization and transition rules.

Recommended test matrix: cross-department JE; unassigned Tender Cell/Samiti/Auditor; contractor ownership; direct URL and forged POST; role changes; duplicate opening/approval; missing/late shortfall document; financial visibility gate; document path traversal; session expiry; OTP limits; workflow concurrency; file validation; generated PDFs against actual database data; and backup/restore plus upgrade rehearsal.

## 41. Pending Implementation

- Tender Cell secondary verification UI/actions.
- Shortfall creation, JE approval, contractor upload, Tender Cell clearance/continued-deficiency history, and notification behavior.
- Samiti financial-opening approval/rejection.
- JE financial opening and bidder-wise details/documents.
- Chief Auditor approval/return and strict precondition enforcement.
- Version-2 Work Order and Tender Agreement draft/finalize/PDF/version/history features.
- Safe administrative CRUD for workflow statuses and system configuration values; these remain
  deployment-controlled until edits can be validated against server-side transition rules.
- Full role/department/document-access and end-to-end tests.
- Production SMS integration, IIS deployment validation, and secure Super Admin provisioning.

The database already contains much of the later-stage schema; schema presence must not be reported as completed application functionality.

## 42. Known Limitations

- The local DB has all six new role definitions but no assigned users for any of them.
- `9588629955` is on a legacy Admin account, not a Super Admin account.
- The local DB has 29 version-1 tenders and no version-2 assignments/test records.
- Five lifecycle procedures called by Web Forms code are absent from the local database.
- Five auxiliary OTP/auth tables and three lookup tables exist in the DB without checked-in source DDL; the OTP tables have no source-code references found in this repository.
- The retired `DepartmentModuleSchema.sql` was a partial schema preflight; the canonical installer now performs guarded preflight and post-install checks, but those are not behavioral tests.
- Historical status documents report no comprehensive test suite/CI pipeline and contain older build-success claims; those reports are not a current CI or full-stack verification result.
- No no-bid/cancellation business transition is defined.
- The Web Forms Console OTP mode is development-only; no SMS provider is configured in the inspected `Web.config`.

## 43. Future Enhancements

After business approval and testing: implement remaining version-2 stages, an approved no-bid/cancellation path, production OTP/SMS with monitoring, notification delivery, concurrency controls, richer document retention/versioning, automated upgrade tests, least-privilege SQL deployment roles, and parity decisions for the legacy API versus the active Next.js/API stack.

## Conflicts and Resolution Notes

| Source statements that conflict or are stale | Current evidence and resolution |
|---|---|
| `docx/README.md` describes React 18/MUI and obsolete SQL setup files; older files also describe seeded logins. | The current root `README.md`, `Frontend/README.md`, and `SMC_TENDER_DATABASE.sql` describe the supported installer. The docx README is marked historical, no longer lists credentials, and points to the canonical setup. |
| The retired `DatabaseSetup.sql` inserted development accounts and business data. | It is archived outside the active project. The canonical setup omits all user/department/contractor/tender/bid/transaction records; Super Admin provisioning remains a separate trusted process. |
| Historical docs say there were nine/ten legacy roles or refer to role IDs by number; migration 10 and the live DB show 18 role definitions and role IDs are database-generated. | Use role names and live role mappings, not hardcoded IDs. The six version-2 roles coexist with legacy roles. |
| Older implementation notes give tender-type values `Open/Restricted/Negotiated`; the current API enum is `Open/Limited/Single/EOI`; the pre-consolidation SQL lookup list was `Works/Goods/Services/Consultancy`. | These are three distinct vocabularies in separate layers. Do not silently normalize or seed one into another; confirm the intended mapping with the business owner before changing API/UI/database behavior. Fresh installs leave configurable catalogs empty. |
| An older implementation summary describes API tender CRUD as Admin-only, while version-2 Web Forms intake assigns tender creation to the department's JE. | These are different application/workflow paths. Preserve their existing authorization boundaries until a deliberate parity decision is approved; the front-end guard alone is not security enforcement. |
| Historical analysis/SOP text lists different opening actors and stage ordering; migration 09's SQL authorization checks currently allow CAFO/TenderCommittee/TenderSection/Admin to open legacy bids, and `TenderEvaluation.aspx.cs` calls its stored procedure. | Treat the current SQL procedure as the supported legacy implementation when installed. Keep this separate from version-2 JE opening and note that the local DB is missing that procedure. |
| Older documentation describes a one-day legacy evaluation delay while version-2 shortfall duration is required to be configurable. | `TenderEvaluationSettings.StartDelayDays` has no seeded value. An administrator must save a delay in the Web Forms evaluation page before bid opening; the page inserts or updates the setting. Version-2 shortfall duration belongs on each shortfall and must not use a fixed duration. |
| Historical migration 04 adds `Departments.HeadOfficerName` only inside the branch that creates Announcements. | The canonical installer defines/checks the objects independently and refuses unsupported upgrades rather than relying on the retired script's branch behavior. |
| `docx/WORKFLOW_ANALYSIS_vs_PDF.md` and older status reports describe bugs/features as pending that later status documents say were fixed or implemented. | The current code is the latest authority where verified; historical reports are preserved as sources but their old findings are not presented as current behavior. |

## Source-Document Index

The following Markdown files were inspected and incorporated where they contain project, workflow, implementation, deployment, or testing facts. Historical source documents remain available; current setup instructions were updated where needed:

- `README.md` — current database setup entry point and documentation links.
- `Frontend/README.md`
- `Frontend/INTEGRATION_GUIDE.md`
- `Database/ER_Diagram.md`
- `Backend/README.md`
- `Backend/src/TenderManagement.Infrastructure/Migrations/README.md`
- `docx/README.md` (stale architecture/setup statements preserved in the conflict section)
- `docx/README_STATUS.md`
- `docx/README_STATUS_PASS2.md`
- `docx/IMPLEMENTATION_SUMMARY.md`
- `docx/GAP_ANALYSIS_PASS3.md`
- `docx/GAP_ANALYSIS_PASS4.md`
- `docx/COMPLETE_ANALYSIS_SUMMARY.md`
- `docx/CHANGELOG_WORKFLOW_FIX.md`
- `docx/WORKFLOW_ANALYSIS_vs_PDF.md`
- `docx/OFFICER_SOP_PDF_ALIGNED.md`
- `docx/OFFICER_SOP_PDF_ALIGNED_HI_MR.md`

The following six Markdown files were inspected and excluded because they are authoring instructions for Postman prompts, not SMC application or database documentation:

- `.github/instructions/postman-collections-post-response.instructions.md`
- `.github/instructions/postman-collections-pre-request.instructions.md`
- `.github/instructions/postman-folder-post-response.instructions.md`
- `.github/instructions/postman-folder-pre-request.instructions.md`
- `.github/instructions/postman-http-request-post-response.instructions.md`
- `.github/instructions/postman-http-request-pre-request.instructions.md`

The historical SQL scripts were separately inspected, consolidated, and preserved in the verified
archive outside the active project. `SMC_TENDER_DATABASE.sql` is the only active SQL setup file.
