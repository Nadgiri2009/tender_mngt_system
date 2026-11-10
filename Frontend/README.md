# Tender Management System

The active application stack is **Next.js 15 / React / TypeScript** in this directory, the **ASP.NET Core 8 API** in `../Backend`, and **SQL Server**. Next.js routes live under `app/`; shared UI and API clients live under `src/`. The frontend calls the API at `NEXT_PUBLIC_API_BASE_URL` (defaults to `http://localhost:5000/api`).

For local development, run `npm ci` and `npm run dev` from `Frontend`, then run the API with `dotnet run --project Backend\src\TenderManagement.API\TenderManagement.API.csproj` from the repository root. Configure the API's SQL Server connection string and JWT key through local user-secrets or environment variables; see [Backend configuration](../Backend/README.md#configuration). The database remains SQL Server and is provisioned using the scripts under `../Database`.

The `.aspx` pages and `DepartmentModule.csproj` in this directory are a **legacy Web Forms module**, not the active frontend. Do not use them to launch or publish the Next.js application.

## Legacy Web Forms module (not the active frontend)

## Scope

The current Web Forms implementation includes the public landing page and database-driven summaries, SMS-OTP login (currently development-console delivery only), contractor registration, admin dashboard/user/department/announcement management, tender list and intake, supporting documents, draft editing and soft deletion, draft BOQ maintenance, role-authorized workflow submission/approval/rejection/history, pending approvals, technical-bid evaluation and financial-bid recommendation, and department-side agreements, work orders, progress, measurement books, bill approval, and payment release. Tender detail pages are available publicly; contractors sign in before submitting bids. Search, filtering, sorting, pagination, file download, server-side validation, and responsive styling are implemented in the included pages.

This is an in-progress migration, not yet verified as complete feature parity with the legacy API. Contractor accounts can submit technical and financial bids during an approved publication window. Bid contents are available to evaluation users only after the authorized bid-opening operation. An administrator must link each contractor login to a `dbo.Contractors` profile in Manage Users before bidding is enabled.

The lifecycle additions are available in the Web Forms module through **Tender Lifecycle** and **Tender Evaluation**. They preserve the existing status/approval flow: Department Officer submission, Technical Sanction Officer, GB Officer, DPDC approval (the existing Administrative Approval stage), CAFO budget approval, department tender preparation, Tender Committee, and Competent Authority publication approval. A negative CAFO budget opinion returns the proposal for owner revision and resubmission through the existing CAFO stage; prior approval and budget opinions remain in history. After publication approval, CAFO manages the publication schedule, a Department Officer may request up to three approved extensions, and the designated opening authority records bid opening. Evaluation starts only after the configured delay (one day by default) and continues through the existing technical evaluation and committee stages.

Publication and evaluation timestamps are stored in UTC and shown in India Standard Time. SQL Server procedures enforce the extension ceiling, publication-window bid eligibility, opening cutoff, one-time opening, department/role checks, and evaluation delay. The Web Forms integration also scopes users and documents by role and department. No bidder-notification integration was present in the existing module, so approved extensions are recorded and shown in the portal but do not send notifications.

The module relies on the main Department Panel for sign-in and authorization. The host must establish the trusted session values documented in [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) before a user can enter the module.

The responsive module UI is styled through `Content/Styles.css`, which combines the supplied `site.css` component conventions with the attached palette (`#b64d91`, `#7d3274`, and `#42104f`).

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
3. Apply `..\Database\06_AddTenderSubmissionStartDate.sql` once (safe to rerun), then apply `..\Database\09_AddTenderLifecycle.sql` to add the lifecycle tables and guarded procedures. Both scripts are additive and reuse the existing tender, user, approval, document, and audit tables.
4. Run `..\Database\DepartmentModuleSchema.sql` as a read-only schema preflight after the migrations.
5. For development/testing only, enable `@EnableDashboardDemoData` in `..\Database\07_SeedTenderDashboardDemoData.sql` and run it against a disposable database. It adds eleven deterministic records for an active `DepartmentOfficer` (and a twelfth owned by a second active `DepartmentOfficer`, when one exists) and does not update or delete existing tenders. Workflow history exercises approvals assigned to different existing roles. Leave the opt-in disabled in production.
6. Configure the main panel to authenticate staff and establish the trusted session values `UserId`, `RoleName`, and, when available, `DepartmentId`. `DepartmentOfficer` dashboard counts and tender rows are scoped to that user's `CreatedBy` records. See [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md).
7. Publish the Web Forms project to IIS over HTTPS. Its configured default document is `Pages/Home.aspx`; do not replace the main panel's `Web.config` or authentication system.

Login uses the registered mobile number and a six-digit OTP. The UI displays a fixed `+91` prefix and accepts ten-digit Indian mobile numbers. Contractor registration also verifies the mobile number by OTP and does not ask the registrant to create a password; the contact email remains required by the existing `dbo.Users` schema. OTPs expire after five minutes and allow at most five verification attempts and three requests per session in 15 minutes. The current development configuration uses `OtpDeliveryMode=Console`; for local requests, OTPs are written to `%TEMP%\TenderManagement-DevelopmentOtp.log` and are not sent by SMS. In a VS Code PowerShell terminal, run `Get-Content "$env:TEMP\TenderManagement-DevelopmentOtp.log" -Wait -Tail 1` to display the latest code and follow later requests. Console delivery refuses non-local requests and must never be enabled in production. When the domain/IP and provider configuration are ready, switch to `OtpDeliveryMode=Sms` to require SMS delivery, or `Auto` to use SMS when all settings are present and otherwise use console output. SMS settings can be supplied through the `SmsGateway*` and `SmsOtpMessageTemplate` app settings or corresponding `SMC_SmsGateway*` / `SMC_SmsOtpMessageTemplate` environment variables; keep credentials in a protected environment or secret store and configure the exact DLT-approved message template. Codes are never rendered in the browser. Existing accounts need a registered, unique mobile number to sign in; ambiguous duplicate mobile numbers are refused.

The Web Forms project includes public portal login and contractor-registration pages; these do not replace the main panel's staff authentication. For IIS deployment steps and the required production configuration, see [IIS deployment](./INTEGRATION_GUIDE.md#10-iis-deployment).

`..\Database\DepartmentModuleSchema.sql` validates that required tables/columns are present. `06_AddTenderSubmissionStartDate.sql` and `09_AddTenderLifecycle.sql` are additive migrations; apply them in order and back up the target database according to the organization's change process. The lifecycle migration uses `CREATE OR ALTER` and requires SQL Server 2016 SP1 or later. The optional `07_SeedTenderDashboardDemoData.sql` script is opt-in, rerunnable, and limited to development/test use; it requires existing role and department records and never creates users or overwrites tender data. If the existing application database has not yet been provisioned, coordinate its approved SQL Server setup with the database owner before integrating this module.

The dashboard uses the existing `TenderType` values as categories and derives publication dates from `ApprovalHistory`. A `DepartmentOfficer` is treated as the RESP officer for this module: only tenders whose `CreatedBy` matches that authenticated user appear in their dashboard/list, and unpublished records owned by another officer are not accessible through tender details.

## Validation and testing status

`dotnet build Frontend\DepartmentModule.csproj --no-restore` succeeds with 0 warnings and 0 errors, validating the C# project build. ASP.NET markup precompilation and the lifecycle migration/workflows have not been verified against the target IIS and SQL Server environment. Verify migration 09, role/department restrictions, upload/download behavior, and the approval, extension, bid-opening, and evaluation scenarios against a non-production database before release. The classic Web Forms project is distinct from the active Next.js/ASP.NET Core application; these changes do not port lifecycle behavior to that active application. Use the checklist in [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) before release.
