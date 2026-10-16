# Tender Management System

The active application stack is **Next.js 15 / React / TypeScript** in this directory, the **ASP.NET Core 8 API** in `../Backend`, and **SQL Server**. Next.js routes live under `app/`; shared UI and API clients live under `src/`. The frontend calls the API at `NEXT_PUBLIC_API_BASE_URL` (defaults to `http://localhost:5000/api`).

For local development, run `npm ci` and `npm run dev` from `Frontend`, then run the API with `dotnet run --project Backend\src\TenderManagement.API\TenderManagement.API.csproj` from the repository root. Configure the API's SQL Server connection string and JWT key through local user-secrets or environment variables; see [Backend configuration](../Backend/README.md#configuration). The database remains SQL Server and is provisioned using the scripts under `../Database`.

The `.aspx` pages and `DepartmentModule.csproj` in this directory are a **legacy Web Forms module**, not the active frontend. Do not use them to launch or publish the Next.js application.

## Legacy Web Forms module (not the active frontend)

## Scope

The current Web Forms implementation includes the public landing page and database-driven summaries, SMS-OTP login (currently development-console delivery only), contractor registration, admin dashboard/user/department/announcement management, tender list and intake, supporting documents, draft editing and soft deletion, draft BOQ maintenance, role-authorized workflow submission/approval/rejection/history, pending approvals, technical-bid evaluation and financial-bid recommendation, and department-side agreements, work orders, progress, measurement books, bill approval, and payment release. Tender detail pages are available publicly; contractors sign in before submitting bids. Search, filtering, sorting, pagination, file download, server-side validation, and responsive styling are implemented in the included pages.

This is an in-progress migration, not yet verified as complete feature parity with the legacy API. Contractor accounts can submit technical bids and, after qualification, financial bids through the Web Forms bid page. An administrator must link each contractor login to a `dbo.Contractors` profile in Manage Users before bidding is enabled. No end-to-end IIS/SQL Server tests have been run.

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
3. Apply `..\Database\06_AddTenderSubmissionStartDate.sql` once (safe to rerun), then run `..\Database\DepartmentModuleSchema.sql` as a read-only schema preflight.
4. For development/testing only, enable `@EnableDashboardDemoData` in `..\Database\07_SeedTenderDashboardDemoData.sql` and run it against a disposable database. It adds eleven deterministic records for an active `DepartmentOfficer` (and a twelfth owned by a second active `DepartmentOfficer`, when one exists) and does not update or delete existing tenders. Workflow history exercises approvals assigned to different existing roles. Leave the opt-in disabled in production.
5. Configure the main panel to authenticate staff and establish the trusted session values `UserId`, `RoleName`, and, when available, `DepartmentId`. `DepartmentOfficer` dashboard counts and tender rows are scoped to that user's `CreatedBy` records. See [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md).
6. Publish the Web Forms project to IIS over HTTPS. Its configured default document is `Pages/Home.aspx`; do not replace the main panel's `Web.config` or authentication system.

Login uses the registered mobile number and a six-digit OTP. The UI displays a fixed `+91` prefix and accepts ten-digit Indian mobile numbers. Contractor registration also verifies the mobile number by OTP and does not ask the registrant to create a password; the contact email remains required by the existing `dbo.Users` schema. OTPs expire after five minutes and allow at most five verification attempts and three requests per session in 15 minutes. The current development configuration uses `OtpDeliveryMode=Console`; for local requests, OTPs are written to `%TEMP%\TenderManagement-DevelopmentOtp.log` and are not sent by SMS. In a VS Code PowerShell terminal, run `Get-Content "$env:TEMP\TenderManagement-DevelopmentOtp.log" -Wait -Tail 1` to display the latest code and follow later requests. Console delivery refuses non-local requests and must never be enabled in production. When the domain/IP and provider configuration are ready, switch to `OtpDeliveryMode=Sms` to require SMS delivery, or `Auto` to use SMS when all settings are present and otherwise use console output. SMS settings can be supplied through the `SmsGateway*` and `SmsOtpMessageTemplate` app settings or corresponding `SMC_SmsGateway*` / `SMC_SmsOtpMessageTemplate` environment variables; keep credentials in a protected environment or secret store and configure the exact DLT-approved message template. Codes are never rendered in the browser. Existing accounts need a registered, unique mobile number to sign in; ambiguous duplicate mobile numbers are refused.

The Web Forms project includes public portal login and contractor-registration pages; these do not replace the main panel's staff authentication. For IIS deployment steps and the required production configuration, see [IIS deployment](./INTEGRATION_GUIDE.md#10-iis-deployment).

`..\Database\DepartmentModuleSchema.sql` validates that required tables/columns are present. The standalone `06_AddTenderSubmissionStartDate.sql` migration adds the one nullable date column the existing schema lacks. The optional `07_SeedTenderDashboardDemoData.sql` script is opt-in, rerunnable, and limited to development/test use; it requires existing role and department records and never creates users or overwrites tender data. If the existing application database has not yet been provisioned, coordinate its approved SQL Server setup with the database owner before integrating this module.

The dashboard uses the existing `TenderType` values as categories and derives publication dates from `ApprovalHistory`. A `DepartmentOfficer` is treated as the RESP officer for this module: only tenders whose `CreatedBy` matches that authenticated user appear in their dashboard/list, and unpublished records owned by another officer are not accessible through tender details.

## Validation and testing status

The classic project compiled with `dotnet build Frontend\DepartmentModule.csproj --no-restore` (0 warnings, 0 errors). ASP.NET markup precompilation and live SQL Server/IIS workflows have not been verified in the target environment. Page submissions, database operations, workflow transitions, and upload/download execution still require deployment testing. The Web Forms project itself contains no ASP.NET Core, Entity Framework, AJAX/UpdatePanel, or stored-procedure usage. Use the checklist in [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) before release.
