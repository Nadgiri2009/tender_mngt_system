# Workflow correction pass — matched against `Traditional_Tender_processes_V_1.0.pdf`

## What was wrong

1. **Missing step**: PDF step 10, "Tender publication approval as per power delegation," did not
   exist as its own gate. The code jumped straight from Tender Committee's documentation approval
   to `Tender Published`, collapsing two distinct sign-offs into one.
2. **Sequence inverted**: PDF orders Financial bid opening → CAFO Rate opinion → Financial
   Evaluation → MCA opinion. The code ran Financial Evaluation *before* CAFO Rate Opinion, which
   defeats the purpose of the rate opinion (it's meant to inform the evaluation, not follow it).
3. **Role conflation**: MCA opinion (Municipal Commissioner's Approval — a distinct authority) was
   approved by the same `CAFOOfficer` role that had just given the rate opinion, so there was no
   independent check.
4. **Only one login existed** (`admin@example.com`) despite nine roles being defined — none of the
   approval stages could actually be exercised end-to-end.

## What changed

- `Backend/.../Domain/Enums/TenderStatus.cs` — added `PublicationApprovalPending` status and
  `MCAOfficer` role.
- `Backend/.../Application/Services/WorkflowEngine.cs` — the state machine:
  - `TenderCommitteeApproval` now approves into `PublicationApprovalPending`, which is approved
    by `CompetentAuthority` before moving to `TenderPublished`.
  - `FinancialBidOpening` → `CAFORateOpinion` (approved by `CAFOOfficer`) → `FinancialEvaluation`
    → `MCAOpinion` (approved by `MCAOfficer`) → `TenderCommitteeRecommendation`, matching the PDF
    order and giving MCA its own independent approver.
- `Backend/.../API/Controllers/PortalDashboardController.cs` — `activeTenders` stat now includes
  the CAFO/MCA stages that sit between financial bid opening and committee recommendation.
- `Database/DatabaseSetup.sql`:
  - Added `MCAOfficer` role.
  - `WorkflowStages` seed rewritten to match the corrected sequence and fixed role IDs (unused by
    the app today — permissions are enforced by `WorkflowEngine.cs` — but kept accurate since it's
    real seed data and the intended path for an eventual admin-configurable stage screen).
  - Added one login per role (10 total) so the full lifecycle can be exercised immediately. See
    `README.md` for the credential table.
- `Frontend/src/types/auth.ts` — added `MCAOfficer` to the `ROLE_NAMES` constant for consistency.
  No UI/markup was touched — the frontend renders roles, statuses, and workflow actions generically
  from API data (confirmed: no hardcoded status lists, stage arrays, or role-specific pages exist
  in the frontend), so it did not need any changes to support the corrected flow.

## Not changed on purpose

- `Backend/.../Migrations/` stays empty — this project is schema-first via `Database/*.sql`, as
  documented there.
- Build artifacts (`node_modules`, `.next`, `bin`, `obj`) were stripped from this archive. Run
  `npm install` in `Frontend/` and restore/build the `Backend/` solution as usual.
