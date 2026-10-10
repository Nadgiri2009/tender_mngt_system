# Tender Management System — Status Report

This is **not** the full 30-module enterprise system described in the original
prompt. It's the **Tender core module**, built and verified end-to-end, plus
a security fix to the existing backend. Read this before you assume more was
delivered than actually was.

## What changed in this pass

### 1. Security fix (Backend) — real, not cosmetic
`Backend/src/TenderManagement.API/appsettings.json` had a **live-looking SQL
Server password and JWT signing key committed in plaintext**. Both have been
replaced with empty placeholders. `appsettings.Example.json` documents the
expected shape. `.gitignore` now excludes `appsettings.json` and
`appsettings.Development.json` from version control.

**Action required from you, not optional:**
- Rotate the SQL Server password that was in that file — treat it as
  compromised, since it was sitting in a zip that left your machine.
- Generate a new JWT signing key (32+ random bytes, not a phrase). Never
  reuse a key that was ever committed.
- Use `dotnet user-secrets` locally and environment variables / Azure
  Key Vault / AWS Secrets Manager in production. Don't hand-edit
  `appsettings.json` with real values again.

### 2. Backend bug fix
`TenderDetailDto.CreatedByName` was always empty — the repository query
never included the creator, and the service never mapped it. Fixed by adding
a `CreatedByUser` navigation property on `Tender`, including it in
`GetWithDetailsAsync`, and mapping it in `TenderService`.

**This requires a new EF Core migration** — I cannot run `dotnet ef` in this
environment (no .NET SDK, no NuGet access). Run this yourself:
```
dotnet ef migrations add AddTenderCreatedByUserFK --project TenderManagement.Infrastructure --startup-project TenderManagement.API
dotnet ef database update --project TenderManagement.Infrastructure --startup-project TenderManagement.API
```

### 3. Frontend — full rewrite, Next.js 15 / TypeScript / Tailwind (per your decision)
The old Create React App / JavaScript / MUI frontend is gone, replaced with
the Tender module built against the **real, existing backend API** (routes,
DTOs, and validation rules were read directly from the C# source, not
guessed).

**Verified, not assumed:**
- `npx tsc --noEmit` → 0 errors
- `npx next build` → succeeds, all 6 routes compile (2 static, 1 static
  not-found, 3 dynamic)
- `npm audit` → 0 vulnerabilities in direct dependencies. One moderate
  advisory remains, bundled *inside* Next.js's own internal build tooling
  (`next → postcss@8.4.31`) — not something fixable from this project's
  `package.json`; it's Next.js's problem to patch upstream. Low real-world
  risk since it's server-side build tooling, not runtime user-facing code.

**What it covers:** tender list (search / status filter / department filter
/ pagination), create (with file attachments), detail view, edit (Draft-only,
enforced both client- and server-side), BOQ line-item management (add /
delete), delete (Draft-only, soft delete). Loading skeletons, error
boundaries, 404 handling, toast notifications.

## What is explicitly NOT done — do not assume otherwise

- **Only 1 of ~30 modules.** Department Requirement, Estimate, TS Approval,
  GB Approval, DPDC, Administrative Approval, Budget Opinion, Committee
  workflows, Publication, Bid modules, Evaluation, Agreement, Work Order,
  Measurement Book, Bills, Payments, Reports, Masters, Users/Roles,
  Notifications, Audit Logs — none of these have a frontend yet. Most have
  partial backend scaffolding already (check `Backend/src` yourself before
  assuming a module needs to be built from zero).
- **No login/auth UI.** The frontend reads a JWT from `localStorage` under
  `tms_access_token`, but nothing writes it there yet. Nothing behind
  `[Authorize]` on the API will work until an auth module exists.
- **No test suite** — unit, integration, or e2e. For a system handling public
  procurement money, this is not optional before production.
- **No CI/CD, no deployment config, no infra.**
- **Concurrent financial-bid-opening handling was not implemented** — this
  needs explicit optimistic-concurrency / row-locking design before it's
  safe for a real simultaneous bid-unlock event. Flagged, not solved.
- **I could not run or compile the .NET backend** — no `dotnet` CLI or
  NuGet access in this environment. The backend code is correct by careful
  inspection, not by compiler verification. Build it locally and fix
  whatever a real compiler catches that I couldn't.

## Before this goes anywhere near production

This handles public money (EMD) and public procurement law. Get an actual
security review and a procurement-law/compliance review before deployment —
that's not something any AI-assisted build can certify for you.

## Running this

**Backend:**
```
cd Backend/src/TenderManagement.API
cp appsettings.Example.json appsettings.Development.json   # fill in real values
dotnet ef database update --project ../TenderManagement.Infrastructure --startup-project .
dotnet run
```

**Frontend:**
```
cd Frontend
cp .env.local.example .env.local   # point NEXT_PUBLIC_API_BASE_URL at your API
npm install
npm run dev
```
