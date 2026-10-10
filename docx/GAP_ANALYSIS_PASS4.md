# Tender Management System — Status Report (Pass 4 / V3)

Read `GAP_ANALYSIS_PASS3.md` first. This pass implements the single
highest-leverage gap it flagged: **Bid submission/evaluation UI** (stages
11, 13, 15 of the official workflow — Technical Bid Opening/Evaluation,
Financial Bid Opening, Financial Evaluation).

## What changed this pass

### Backend (one real gap fixed, everything else was already there)

`BidsController`, `IBidService`/`BidService`, `TechnicalBid`, `FinancialBid`
were already fully implemented (see Pass 3 inventory) — **no changes were
needed there.**

The one real backend gap found while wiring the frontend: **there was no
way for a logged-in Contractor user to discover their own `ContractorId`.**
`Contractor.UserId` already models the link, but nothing read it back.
Fixed by adding:

- `GET /api/contractors/me` (`AdminController.cs`, `ContractorsController`)
  — `[Authorize(Roles = Contractor)]`, resolves the caller's `Contractor`
  row from `User.GetUserId()`. Returns 404 with a clear message if no
  Contractor row is linked yet (expected for a freshly-registered
  contractor account before an Admin links it).

No other backend files were touched. `GET /api/contractors` (list, used by
Admin to submit a bid on behalf of any contractor) already existed and was
left as-is.

### Frontend (net-new — this was the actual gap)

- **`lib/api/bids.ts`** — API client for all six `BidsController` /
  `contractors` endpoints used by this UI, plus `isBidStageOrLater()`, a
  status-sequence helper (mirrors the ordering already encoded in
  `lib/api/workflow.ts`) that decides when the Bids panel is relevant to
  show on a tender.
- **`components/tenders/BidsPanel.tsx`** — new panel rendered on the tender
  detail page once a tender reaches "Technical Bid Opening" or later:
  - **Contractor role**: submit a technical bid; once evaluated
    `Qualified`, submit a financial bid (blocked client-side and
    server-side otherwise — the backend's `BusinessRuleException` for
    "only Qualified contractors may submit a financial bid" surfaces as a
    toast if the UI's gating is ever bypassed).
  - **TenderSection role**: Qualify / Disqualify each submitted technical
    bid with optional remarks.
  - **TenderCommittee role**: view the ranked financial bid table (L1
    lowest first, as `GET /api/bids/{tenderId}/financial` already ranks
    them) and Select the winning contractor.
  - **Admin**: all of the above, on behalf of any contractor (picks from
    `GET /api/contractors`), matching the existing `[Authorize(Roles =
    "...,Admin")]` pattern already on every `BidsController` action.
  - Every mutation invalidates `technical-bids` / `financial-bids` query
    keys so the two tables and the tender's `WorkflowPanel` stay in sync
    without a manual refresh.
- Wired into `TenderDetailView.tsx` directly under the BOQ panel.

### Verified

- `tsc --noEmit`: clean.
- `next build`: 13/13 routes, unchanged route count (this is a component,
  not a new page) — build passed twice, once before and once from the
  final packaged tree.
- Backend: **not** compiled by me — no `dotnet` SDK available in this
  environment. The one new endpoint is a small, self-contained addition
  that follows the exact pattern of the three sibling controllers already
  in the same file (`DepartmentsController`, `RolesController`,
  `ContractorsController.GetAll`), all of which already compile in the
  shipped Pass 3 build. Verify with `dotnet build` before deploying.

## What's still explicitly not done

Unchanged from Pass 3 except the one item above:

- **Estimate module** — still has a Domain entity and nothing else; no
  service, no controller, no frontend.
- **Post-award pipeline** (Agreement, Work Order, Work Execution,
  Measurement Book, Bills, Payment) — `WorkManagementController` is real
  on the backend; still zero frontend. This remains the largest remaining
  gap (6 distinct data-entry workflows).
- Role-scoped dashboard stats API exists; still not wired into a page.
- No test suite, no CI/CD, client-side role gating is UX not security
  (identical caveat to every prior pass — the server is the actual
  authority in all cases, verified by this pass's UI intentionally
  deferring to server error messages rather than duplicating validation
  rules).
- Concurrent financial-bid-opening handling (e.g. two officers evaluating
  the same bid simultaneously) is unaddressed — `BidService` does a
  straightforward read-modify-write with no optimistic concurrency token.

## Recommended next increment

Post-award pipeline (Agreement → Payment) is now the only remaining
frontend gap standing between a tender and a fully demo-able end-to-end
run, now that a tender can actually reach "awarded."
