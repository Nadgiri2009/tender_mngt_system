# Tender Management System

The active application stack is **Next.js 15 / React / TypeScript** in this directory, the **ASP.NET Core 8 API** in `../Backend`, and **SQL Server**. Next.js routes live under `app/`; shared UI and API clients live under `src/`. The frontend calls the API at `NEXT_PUBLIC_API_BASE_URL` (defaults to `http://localhost:5050/api`).

For local development, run `npm ci` and `npm run dev` from `Frontend`, then run the API with `dotnet run --project Backend\src\TenderManagement.API\TenderManagement.API.csproj` from the repository root. Configure the API's SQL Server connection string and JWT key through local user-secrets or environment variables; see [Backend configuration](../Backend/README.md#configuration). The database remains SQL Server and is provisioned using the scripts under `../Database`.

The `.aspx` pages and `DepartmentModule.csproj` in this directory are a **legacy Web Forms module**, not the active frontend. Do not use them to launch or publish the Next.js application.

## Legacy Web Forms module (not the active frontend)

## Scope

The current Web Forms implementation includes the public landing page and database-driven summaries, SMS-OTP login (currently development-console delivery only), contractor registration, admin dashboard/user/department/announcement management, tender list and intake, supporting documents, draft editing and soft deletion, draft BOQ maintenance, role-authorized workflow submission/approval/rejection/history, pending approvals, technical-bid evaluation and financial-bid recommendation, and department-side agreements, work orders, progress, measurement books, bill approval, and payment release. Tender detail pages are available publicly; contractors sign in before submitting bids. Search, filtering, sorting, pagination, file download, server-side validation, and responsive styling are implemented in the included pages.

This is an in-progress migration, not yet verified as complete feature parity with the legacy API. Contractor accounts can submit technical and financial bids during an approved publication window. Bid contents are available to evaluation users only after the authorized bid-opening operation. An administrator must link each contractor login to a `dbo.Contractors` profile in Manage Users before bidding is enabled.

The lifecycle additions are available in the Web Forms module through **Tender Lifecycle** and **Tender Evaluation**. They preserve the existing status/approval flow: Department Officer submission, Technical Sanction Officer, GB Officer, DPDC approval (the existing Administrative Approval stage), CAFO budget approval, department tender preparation, Tender Committee, and Competent Authority publication approval. A negative CAFO budget opinion returns the proposal for owner revision and resubmission through the existing CAFO stage; prior approval and budget opinions remain in history. After publication approval, CAFO manages the publication schedule, a Department Officer may request up to three approved extensions, and the designated opening authority records bid opening. An administrator must configure an evaluation delay in Tender Evaluation before bids can be opened; there is no default delay on a fresh installation. Evaluation continues through the existing technical evaluation and committee stages.

The additive finalized-workflow foundation adds database-backed permissions, protected primary roles, role management, department-required JE provisioning, workflow-version-2 tender intake, immutable workflow history, and a role/assignment-scoped queue. New tenders are assigned to a JE in their department; submission is blocked until a BOQ item and the four existing required document categories are present, and publication requires a future schedule. Existing tenders remain on workflow version 1. Estimate, technical-sanction, and budget copies can also be categorized using the existing `TenderDocuments.DocumentType` column.

**The finalized workflow is not complete yet.** The queue now supports per-tender required-document checklists, initial technical bid opening, and JE primary document review, with validated bidder-document uploads. Tender Cell verification, shortfall actions, financial-bid opening approval and entry, Chief Auditor decisions, work-order/agreement generation, real PDF generation, and end-to-end role/data-isolation tests remain to be implemented. Do not treat this partial implementation as production completion.

Fresh installation creates role, permission, workflow-stage, and lookup tables empty; it does not
seed the six primary roles or their permission mappings. A trusted provisioning process must create
the initial authorized account before staff can sign in. After that, the administration pages can
manage roles, permissions, departments, and supported lookup catalogs. Workflow-stage CRUD is not
implemented yet, so those rows require a separately approved provisioning process. Super Admin
must be assigned only after trusted identity verification; no account is privileged by phone number.
Upgrade mode preserves existing role and workflow rows without inserting or changing them.

Publication and evaluation timestamps are stored in UTC and shown in India Standard Time. SQL Server procedures enforce the extension ceiling, publication-window bid eligibility, opening cutoff, one-time opening, department/role checks, and evaluation delay. The Web Forms integration also scopes users and documents by role and department. No bidder-notification integration was present in the existing module, so approved extensions are recorded and shown in the portal but do not send notifications.

The public tender list includes only notices with an approved publication schedule, so every listed notice has start and closing dates; unscheduled notices remain hidden until CAFO approves a schedule.

Protected Web Forms pages reload the active user, role, department, and permissions from SQL Server using the authenticated session's user ID; session-supplied role and department values are not authorization sources. The host must establish the trusted session values documented in [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) before a user can enter the module.

The responsive module UI is styled through `Content/Styles.css`. Its primary gradient follows the supplied color reference (`#AC5288`, `#75316E`, and `#3E1154`); buttons and dark navigation use white text, while light surfaces retain high-contrast plum text.

Workflow status and role transitions follow the existing `WorkflowEngine` sequence. A status change and its `ApprovalHistory` record are written in one SQL transaction. All user-originated database values are sent as SQL parameters; only the sort column in the tender list is selected from an explicit server-side whitelist.

## Requirements

- Windows Server/IIS with ASP.NET 4.8 and .NET Framework 4.8 Developer/Targeting Pack.
- IIS role services: ASP.NET 4.8, .NET Extensibility 4.8, ISAPI Extensions and ISAPI Filters.
- Microsoft SQL Server access to the existing `TenderManagementDB`.
- Visual Studio with the ASP.NET and web development workload, or Visual Studio Build Tools with Web Build Tools, to build the classic Web Application project.
- An authenticated Department Panel that performs the trusted session handoff. The module also includes public contractor login and registration pages.

## Local setup

1. Open `DepartmentModule.csproj` in Visual Studio and build it with the .NET Framework 4.8 Web Application targets.
2. Set the `TenderManagement` connection string in `Web.config` to the approved SQL Server database. Prefer Integrated Security or a protected deployment transform/secret store; never commit a real password. `TrustServerCertificate=True` is set for the local SQL Express development instance only; production must use a trusted SQL Server certificate and set it to `False`.
3. Use the repository-root `SMC_TENDER_DATABASE.sql` as the only supported setup file. For a fresh, empty database, run from the repository root:

   ```powershell
   sqlcmd -S "localhost\SQLEXPRESS02" -E -b -v DatabaseName="TenderManagementDB" -v InstallMode=Fresh -v BackupVerified=NO -i "SMC_TENDER_DATABASE.sql"
   ```

   For an existing database, first take and restore-test a full backup, rehearse `InstallMode=Upgrade` on an isolated restored copy, and verify its compatibility report before any approved production change. Upgrade mode refuses unsupported schemas and does not drop existing tables. Never run Fresh mode against a populated database.
4. The setup creates no application or reference rows (including users, roles, permissions,
   departments, workflow stages, lookup catalogs, contractors, tenders, bids, or demo
   transactions). Complete the trusted initial-account and workflow configuration process before
   staff use the module. Then manage supported catalogs and operational data through the
   application; provision Super Admin only through trusted identity verification.
5. Configure the main panel to authenticate staff and establish the trusted session values `UserId`, `RoleName`, and, when available, `DepartmentId`. `DepartmentOfficer` dashboard counts and tender rows are scoped to that user's `CreatedBy` records. See [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md).
6. Publish the Web Forms project to IIS over HTTPS. Its configured default document is `Pages/Home.aspx`; do not replace the main panel's `Web.config` or authentication system.

Login uses the registered mobile number and a six-digit OTP. The UI displays a fixed `+91` prefix and accepts ten-digit Indian mobile numbers. Contractor registration also verifies the mobile number by OTP and does not ask the registrant to create a password; the contact email remains required by the existing `dbo.Users` schema. OTPs expire after five minutes and allow at most five verification attempts and three requests per session in 15 minutes. The current development configuration uses `OtpDeliveryMode=Console`; for local requests, OTPs are written to `%TEMP%\TenderManagement-DevelopmentOtp.log` and are not sent by SMS. In a VS Code PowerShell terminal, run `Get-Content "$env:TEMP\TenderManagement-DevelopmentOtp.log" -Wait -Tail 1` to display the latest code and follow later requests. Console delivery refuses non-local requests and must never be enabled in production. When the domain/IP and provider configuration are ready, switch to `OtpDeliveryMode=Sms` to require SMS delivery, or `Auto` to use SMS when all settings are present and otherwise use console output. SMS settings can be supplied through the `SmsGateway*` and `SmsOtpMessageTemplate` app settings or corresponding `SMC_SmsGateway*` / `SMC_SmsOtpMessageTemplate` environment variables; keep credentials in a protected environment or secret store and configure the exact DLT-approved message template. Codes are never rendered in the browser. Existing accounts need a registered, unique mobile number to sign in; ambiguous duplicate mobile numbers are refused.

The Web Forms project includes public portal login and contractor-registration pages; these do not replace the main panel's staff authentication. For IIS deployment steps and the required production configuration, see [IIS deployment](./INTEGRATION_GUIDE.md#10-iis-deployment).

`SMC_TENDER_DATABASE.sql` is the only active SQL setup file. Original scripts remain in the
verified archive outside the active project; do not apply historical migrations individually.

Rollback guidance: roll back the application without removing database objects or operational data. Do not drop document, verification, approval, shortfall, workflow, or generated-document history. For a database rollback, restore a verified backup through the approved recovery process.

The Web Forms administration area manages departments, roles and permission mappings, tender categories, tender methods, document types, units, and officer designations. Workflow transition values remain deployment-controlled because they are coupled to server-side transition rules. The evaluation delay is configurable in the application. Historical `Tender.TenderType` values remain procurement methods; a separate nullable `TenderCategory` preserves the old value and maps only exact matches to configured categories.

The public and staff dashboards display category and tender method separately. Historical rows with no unambiguous category remain `Uncategorized`; their original method value is not rewritten.

## Validation and testing status

`dotnet msbuild Frontend\DepartmentModule.csproj /t:Build /p:Configuration=Debug /v:minimal` succeeds with 0 warnings and 0 errors after the document-upload and primary-verification changes. Migrations 11 and 12 and the read-only preflight are applied/checked against the local `TenderManagementDB`. No version-2 test tenders or Super Admin account were created. End-to-end role authorization, the later version-2 workflow stages, document access isolation, PDF output, and IIS deployment have not been verified. Do not deploy the partial finalized workflow as complete. Use the checklist in [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) after the remaining implementation and testing are complete.
