# Department Tender Management

The application in this directory is served by `DepartmentModule.csproj`. It targets **.NET Framework 4.8**, uses `.aspx` pages with C# code-behind, and runs parameterized SQL Server queries through ADO.NET. IIS opens `Pages/Home.aspx` by default. React/Next.js source code, Node dependencies, and Next.js build output have been removed; this project has no Node.js or npm build step.

The separate `Backend` directory still contains the former ASP.NET Core 8 / Entity Framework API source. The Web Forms project does not reference or deploy that API; its page code-behind accesses SQL Server directly. That legacy API has not been migrated or removed in this change. Do not treat it as part of the Web Forms runtime.

## Scope

The current Web Forms implementation includes the public landing page and database-driven summaries, login and contractor registration, admin dashboard/user/department/announcement management, tender list and intake, supporting documents, draft editing and soft deletion, draft BOQ maintenance, role-authorized workflow submission/approval/rejection/history, pending approvals, technical-bid evaluation and financial-bid recommendation, and department-side agreements, work orders, progress, measurement books, bill approval, and payment release. Search, filtering, sorting, pagination, file download, server-side validation, and the existing purple/plum responsive styling are implemented in the included pages.

This is an in-progress migration, not yet verified as complete feature parity with the legacy API. Contractor accounts can submit technical bids and, after qualification, financial bids through the Web Forms bid page. An administrator must link each contractor login to a `dbo.Contractors` profile in Manage Users before bidding is enabled. No end-to-end IIS/SQL Server tests have been run.

The module relies on the main Department Panel for sign-in and authorization. The host must establish the trusted session values documented in [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) before a user can enter the module.

The responsive module UI is styled through `Content/Styles.css`, which combines the supplied `site.css` component conventions with the attached purple-to-plum color palette.

Workflow status and role transitions follow the existing `WorkflowEngine` sequence. A status change and its `ApprovalHistory` record are written in one SQL transaction. All user-originated database values are sent as SQL parameters; only the sort column in the tender list is selected from an explicit server-side whitelist.

## Requirements

- Windows Server/IIS with ASP.NET 4.8 and .NET Framework 4.8 Developer/Targeting Pack.
- IIS role services: ASP.NET 4.8, .NET Extensibility 4.8, ISAPI Extensions and ISAPI Filters.
- Microsoft SQL Server access to the existing `TenderManagementDB`.
- Visual Studio with the ASP.NET and web development workload, or Visual Studio Build Tools with Web Build Tools, to build the classic Web Application project.
- An authenticated Department Panel that performs the trusted session handoff. The module intentionally has no login form.

## Local setup

1. Open `DepartmentModule.csproj` in Visual Studio and build it with the .NET Framework 4.8 Web Application targets.
2. Set the `TenderManagement` connection string in `Web.config` to the approved SQL Server database. Prefer Integrated Security or a protected deployment transform/secret store; never commit a real password.
3. Run `..\Database\DepartmentModuleSchema.sql` against the target database. It is a read-only preflight and makes no schema changes.
4. Configure the main panel to authenticate staff and establish the trusted session values `UserId`, `RoleName`, and, when available, `DepartmentId`. See [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md).
5. Publish the Web Forms project to IIS over HTTPS. Its configured default document is `Pages/Home.aspx`; do not replace the main panel's `Web.config` or authentication system.

Before integration, set `DepartmentPanelLoginUrl` in `Web.config` to the main panel's actual sign-in URL for protected department pages. The Web Forms project currently also contains public portal login and contractor-registration pages; these do not replace the main panel's staff authentication.

`..\Database\DepartmentModuleSchema.sql` validates that required existing tables/columns are present. It deliberately does not create database objects, seed demo users, or create stored procedures. If the existing application database has not yet been provisioned, coordinate its approved SQL Server setup with the database owner before integrating this module.

## Validation and testing status

The classic project compiled with `dotnet build Frontend\DepartmentModule.csproj --no-restore` (0 warnings, 0 errors) after the edits. ASP.NET markup precompilation was attempted but could not complete because the installed compiler reports that `Microsoft.VisualC.CppCodeProvider` is unavailable; markup compilation therefore remains unverified. A live SQL Server and IIS workflow test were not run, so page submissions, database operations, workflow transitions, and upload/download execution still require testing in the target environment. The Web Forms project itself contains no ASP.NET Core, Entity Framework, AJAX/UpdatePanel, or stored-procedure usage. Use the checklist in [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) before release.
