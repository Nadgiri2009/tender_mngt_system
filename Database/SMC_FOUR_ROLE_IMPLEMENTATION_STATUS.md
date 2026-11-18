# SMC Four-Role Implementation Status

**Reviewed:** 18 November 2026  
**Target database:** `tender_tracking` on `localhost\SQLEXPRESS02`  
**Protected database:** `TenderManagementDB` was not modified.

This report is the current status for the four-role change. Older sections of
`SMC_TENDER_MASTER_DOCUMENTATION.md` describe the prior six-role/version-2
proposal and should not be used as the current role catalog or as evidence that
the end-to-end workflow is complete.

## Final role and permission matrix

| Role code | Canonical role | Assigned responsibilities | Database permission keys |
|---|---|---|---|
| `Junior Engineer (JE)` | Junior Engineer (JE) | Main Admin: user and department administration, permissions for the four fixed roles, tender creation/publication, tender opening/closing, pre-bid/pre-bill and measurement records, financial bid data entry, Work Order creation, Tender Agreement creation, and configured workflow administration. | `users.manage`, `masterdata.manage`, `roles.manage`, `tender.create.department`, `tender.publication.manage`, `tender.primary.open`, `tender.closing.manage`, `tender.prebill.manage`, `tender.measurement.manage`, `financial.open.execute`, `financial.bids.enter`, `workorder.manage`, `agreement.manage`, `workflow.configure`, `dashboard.view`, `document.read.assigned`, `tender.read.department`, `roles.view` |
| `TENDER_CELL` | Tender Cell | Document requirement management, document verification, shortfall generation, approval and clearance. | `document.requirements.manage`, `tender.cell.verify`, `shortfall.create`, `shortfall.approve`, `shortfall.clear`, `dashboard.view`, `document.read.assigned`, `tender.read.department` |
| `CHAIRMAN` | Tender Committee Chairman (Nivida Samiti Adhyaksha) | Financial bid opening approval. | `financial.open.approve`, `dashboard.view`, `document.read.assigned`, `tender.read.department` |
| `DIRECTOR_AUDITOR` | Director Auditor | Financial bid review and approval. | `financial.approve.final`, `dashboard.view`, `document.read.assigned`, `tender.read.department` |

External bidders are represented by `Users.AccountType='ExternalBidder'` with a
null `RoleId`; they are not a fifth internal role. Registration and sign-in use a
10-digit mobile number and one-time code. The registration form no longer asks
for a password or automatically creates an authenticated session. Email remains
contact information, not a login identifier. A randomly generated OTP is shown
as `Development OTP` in the login UI only for eligible localhost requests in
Development; it is not logged to the API console and is never returned for
production, non-local requests, unknown accounts, or rate-limited requests.

The database has exactly four active internal roles and 31 role-permission
grants. There is no `Admin`, `Super Admin`, `Main Admin`, or custom role. Junior Engineer (JE) is the
Main Admin through permission grants, not a separate role. User, department,
and fixed-role permission endpoints use SQL-backed permission policies. The
migration script grants `users.manage`, `masterdata.manage`, and `roles.manage`
to Junior Engineer (JE); these administrative routes remain unavailable until those grants exist
in the target database.

## Migration and preservation

The additive migration is
[`Migrations/SMC_TENDER_TRACKING_4ROLE_MIGRATION.sql`](./Migrations/SMC_TENDER_TRACKING_4ROLE_MIGRATION.sql).
It is restricted to `tender_tracking`, assigns stable role codes, configures
permissions and workflow transitions, and validates the final four-role
catalog. It does not drop the database or business tables. Obsolete permission
grants are written to `AuditLog` before they are removed.

Known label normalization supported by the migration:

| Existing role label | Canonical role code |
|---|---|
| `Junior Engineer (JE)`, `Junior Engineer (JE)` | `Junior Engineer (JE)` |
| `Nivida Kakash / Tender Cell`, `Nivida Kaksh (Tender Cell)`, `Tender Cell` | `TENDER_CELL` |
| `Nivida Samiti Adhaksha`, `Nivida Samiti Adhyaksha`, `Tender Committee Chairman (Nivida Samiti Adhyaksha)` | `CHAIRMAN` |
| `Chief Audit`, `Chief Auditor`, `Director Auditor` | `DIRECTOR_AUDITOR` |

The migration intentionally stops if it finds an unrecognized role or duplicate
role code. It does not guess how to transfer users or historical approvals from
legacy roles such as `Admin`, `DepartmentOfficer`, `TechnicalSanctionOfficer`,
`GBOfficer`, `DPDCOfficer`, `CAFOOfficer`, `MCAOfficer`, `RESP`,
`TenderCommittee`, `TenderSection`, `CompetentAuthority`, or legacy
`Contractor` roles. These require a reviewed, explicit data mapping before
upgrading that database. Existing users, permissions and history must not be
deleted or reassigned just to make the count equal four.

Legacy contractor identities must be explicitly classified as external bidder
accounts and linked to their contractor profile before their old role link can
be cleared. The migration does not silently perform that conversion. Existing
approval rows retain their actor and historical role text.

### Installation sequence

For a fresh `tender_tracking` installation:

1. Run `SMC_TENDER_DATABASE.sql` in Fresh mode against an empty database.
2. Run the four-role migration with SQLCMD against `tender_tracking`.
3. Verify the four roles, permission grants, transitions, and account assignments
   before enabling logins.

For an upgrade, take and verify a restorable backup, rehearse on a copy, run the
base installer in Upgrade mode, and then run the four-role migration. The
migration's guard is deliberate: an existing catalog containing non-canonical
roles must be mapped and reviewed before applying it. Never point either
command at `TenderManagementDB` as part of this task.

There is no separate trusted-user bootstrap script. Internal users must be
provisioned through a separately authorized process with verified mobile
numbers, department assignments and audit evidence.

## Workflow configuration and implementation status

The migration configures 11 version-2 transitions in `WorkflowTransitions`. The
stored procedure `dbo.sp_UpdateTenderStatus` checks the configured transition,
active role, account type, role permission, expected status and Junior Engineer (JE) department
before updating the tender and inserting `ApprovalHistory` atomically.

| Workflow area | Current implementation status |
|---|---|
| Junior Engineer (JE) tender creation/publication and technical opening | Role permissions and transitions are configured; existing UI/API paths are not yet proven end-to-end for version 2. |
| Tender Cell document verification | Permission and status transition are configured. Bidder/document verification data operations and the complete Tender Cell queue are not integrated end-to-end. |
| Shortfall generation, approval and response | Role grants exist and the schema supports shortfall items, submissions and deadlines. The API transition only changes status/history; it does not create shortfall rows, upload bidder responses, enforce the three-calendar-day deadline, or clear/re-verify each item. |
| Chairman financial-opening approval | Role permission and transition are configured. The financial-opening record and bidder-level data are not atomically integrated with this action. |
| Junior Engineer (JE) financial data entry | Transition is configured; bidder-level financial bid entry is not integrated with this transition. |
| Director Auditor review/approval | Permission and transition are configured. A complete financial review record/evidence workflow remains to be integrated. |
| Junior Engineer (JE) Work Order and Tender Agreement | Correct role grants and terminal transitions are configured. Existing API create methods can record basic records, but they are not coupled to transitions and do not yet provide versioned PDF/DOCX generation or upload/history. |
| Pre-bill and measurement | Junior Engineer (JE) permissions exist; API measurement entry is Junior Engineer (JE)-only. Full dashboard and workflow verification remains outstanding. |
| Workflow assignment | The schema contains `TenderWorkflowAssignments`, but the API transition service does not yet enforce a matching active assignment or maintain assignment rows after each transition. Role/status checks alone are not a substitute. |
| Workflow configuration UI | Transitions are migration-controlled. There is no safe UI/API to edit transition rules. `workflow.configure` is a grant only, not a completed administration feature. |

The transition sequence in the migration follows the previously confirmed
business clarification: Junior Engineer (JE) opens the financial-data stage after the Chairman's
approval, Junior Engineer (JE) enters financial data, Director Auditor reviews/approves, then Junior Engineer (JE)
creates the Work Order and Agreement. It does not invent an extra approval
between Chairman and Director Auditor.

## Role migration mapping and unresolved roles

The target database currently has role IDs 1–4 assigned to the four canonical
role codes. Role IDs are database keys, not portable identifiers; application
logic uses role codes/names and configured permissions.

For any legacy database, the following are candidates for an explicit review,
not automatic migrations:

| Legacy identity | Proposed handling | Status |
|---|---|---|
| Junior Engineer (JE) | `Junior Engineer (JE)` | Direct label match, subject to department/user validation. |
| Nivida Kaksh / Tender Cell | `TENDER_CELL` | Label match; verify users and retained history. |
| Nivida Samiti Adhyaksha | `CHAIRMAN` | Label match; verify users and retained history. |
| Chief Auditor | `DIRECTOR_AUDITOR` | Label match to the finalized responsibility. |
| Contractor / Contractor / Vendor | External bidder account type, null internal role | Must validate mobile, contractor profile, and ownership before conversion. |
| All other legacy roles | No automatic mapping | Business owner must identify whether duties/data can be safely reassigned. |

No old user, permission grant, approval row, workflow assignment or audit record
should be deleted or reassigned until the mapping is approved and rehearsed.

## Handwritten reference comparison and ambiguities

Inspected the supplied `Tender Image.jpeg` and `Tender Image -1.jpeg`.

The first image supports the four-role responsibility split:

- Junior Engineer (JE): tender publication, pre-bill/measurement details, tender opening/closing,
  Work Order, and Tender Agreement.
- Tender Cell / “Nivida Kakash”: document requirement approval, document
  verification, shortfall generation/approval, and shortfall duration entry.
- Nivida Samiti Adhyaksha: financial bid opening approval.
- Junior Engineer (JE): financial bid data entry after the opening-approval step.
- Director Auditor: financial approval/review.

The second image appears to list bid-opening/document-screen fields: bid count,
document checklist names with a Yes/No selection, pre-bill/date fields, and a
pre-bid meeting suggestion/upload Yes/No field.

The confirmed business sequence (Chairman approval → Junior Engineer (JE) financial data entry →
Director Auditor review/approval) is consistent with the first image. No
additional approval stage has been inferred.

Clarifications received after image review:

- Record the number of bids opened.
- Use configured document requirements rather than inventing a fixed checklist.
  The checklist layout/remarks details remain unconfirmed.
- Store pre-bid date/time details and require the suggestion/upload when a
  pre-bid meeting is enabled.
- Set the shortfall deadline to exactly three calendar days; no submission
  attempt limit.

The current application has not yet implemented the newly confirmed
pre-bid-upload requirement, bid-opening count, or shortfall deadline behavior.
No document checklist names or validation rules are hardcoded pending further
business direction.

## Verification performed

- SQL Server query against `tender_tracking`: exactly four active internal
  roles (`Junior Engineer (JE)`, `TENDER_CELL`, `CHAIRMAN`, `DIRECTOR_AUDITOR`); one assigned user
  for each; 11 active version-2 transitions.
- SQL Server query: no active transition points to an invalid role code or
  missing permission.
- `dotnet build Backend\TenderManagement.sln --no-restore`: passed with 0
  warnings and 0 errors.
- `dotnet build Frontend\DepartmentModule.csproj --no-restore`: passed with 0
  warnings and 0 errors.
- `npm run build` in `Frontend`: passed, including type checking and static
  generation.
- `git diff --check`: passed.

**Superseded verification note:** the migration source now grants `users.manage`,
`masterdata.manage`, and `roles.manage` to Junior Engineer (JE). The live permission state was not
rechecked after that edit, so do not assume those grants are deployed.

The current frontend also includes a mobile collapsible admin navigation,
department/user/role permission pages, and shared numbered pagination; the
tender listing uses the same server-side 10-record pagination controls.

These are build/schema checks, not end-to-end business workflow tests. No
functional tests were run for unauthorized requests, assignment scoping,
shortfall deadlines, bidder uploads, financial confidentiality, PDF/DOCX
generation, fresh-install-plus-migration rehearsal, or historical data
preservation on a populated legacy database.

## Remaining work before acceptance

1. Inspect both handwritten references and reconcile their checklists and
   edge-cases with the configured flow.
2. Implement and enforce active workflow assignment creation, completion and
   authorization.
3. Integrate document review and shortfall records, contractor responses,
   three-day deadline calculation, clearance and audit history.
4. Persist the financial-opening record and bidder-level financial data with
   the approval transitions; enforce the visibility gate.
5. Couple Work Order/Agreement creation to approved workflow states and add
   validated PDF/DOCX upload, version history and audit records.
6. Align all legacy Web Forms/API routes and role dashboards with the four
   responsibilities; verify every direct URL and postback server-side.
7. Add and run role-specific integration tests, including unauthorized and
   cross-department attempts, and rehearse Fresh and Upgrade on disposable
   databases with representative historical data.
