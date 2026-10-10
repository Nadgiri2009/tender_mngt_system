# Tender Management applications

The original application source has been restored in place: the Next.js frontend is under `src/` and the .NET 8 API is under `Backend/src/`. The original login and registration pages and API authentication endpoints were copied byte-for-byte from the intact project snapshot. The Web Forms department module remains alongside them in this existing `Frontend` directory, targeting .NET Framework 4.8 with C# code-behind and direct parameterized ADO.NET queries against SQL Server.

The project therefore has two distinct entry points: `npm run dev` from `Frontend/` runs the original portal, and `Pages/Default.aspx` is the separate Web Forms department module. `Backend/TenderManagement.sln` is the restored original API solution; open `DepartmentModule.csproj` directly to build the Web Forms module.

## Scope

The Web Forms module includes the department tender list and dashboard, tender intake and supporting files, BOQ maintenance, role-authorized workflow transitions and history, approval queue, technical-bid evaluation and financial-bid recommendation, agreement/work-order recording, work progress, measurement book, bill approval, and payment release. The original login, registration, public portal, contractor portal, and administrative pages remain in the restored portal source; they have not been removed or rewritten as part of restoring that source. The Web Forms module continues to require its configured trusted session/authentication handoff.

The responsive module UI is styled through `Content/Styles.css`, which combines the supplied `site.css` component conventions with the attached purple-to-plum color palette.

Workflow status and role transitions follow the existing `WorkflowEngine` sequence. A status change and its `ApprovalHistory` record are written in one SQL transaction. All user-originated database values are sent as SQL parameters; only the sort column in the tender list is selected from an explicit server-side whitelist.

## Requirements

- Node.js/npm to install and run the restored original frontend (`npm ci`, then `npm run dev`).
- .NET 8 SDK to build and run the restored original API solution.

- Windows Server/IIS with ASP.NET 4.8 and .NET Framework 4.8 Developer/Targeting Pack.
- IIS role services: ASP.NET 4.8, .NET Extensibility 4.8, ISAPI Extensions and ISAPI Filters.
- Microsoft SQL Server access to the existing `TenderManagementDB`.
- Visual Studio with the ASP.NET and web development workload, or Visual Studio Build Tools with Web Build Tools, to build the classic Web Application project.
- The module must be hosted inside the authenticated Department Panel or behind an equivalent trusted session handoff. It has no login form.

## Local setup

1. For the restored portal, run `npm ci` and `npm run dev` from this directory; its original login and registration routes are `/login` and `/register`.
2. For the restored API, build `..\Backend\TenderManagement.sln` or run its API project.
3. For the Web Forms department module, open `DepartmentModule.csproj` in Visual Studio.
4. Set the `TenderManagement` connection string in `Web.config`. Use Integrated Security for Windows authentication or a deployment secret store/secured transform for SQL authentication. Never commit real passwords.
5. Set HTTPS for local and deployed use; the configuration marks cookies Secure and HttpOnly.
6. Ensure the host establishes the Web Forms module's session values for the user ID, role name, and (when available) department ID. Default keys are `UserId`, `RoleName`, and `DepartmentId`; change the matching appSettings keys if the host uses different names.
7. Execute `..\Database\DepartmentModuleSchema.sql` against the existing database. It is a read-only schema preflight and makes no schema changes.
8. Build and run the Web Forms module under IIS Express or IIS. Its entry page is `Pages/Default.aspx`.

`..\Database\DepartmentModuleSchema.sql` validates that required existing tables/columns are present. It deliberately does not create database objects, seed demo users, or create stored procedures. If the existing application database has not yet been provisioned, coordinate its approved SQL Server setup with the database owner before integrating this module.

## Validation and testing status

The C# project build completed with `dotnet build` with 0 warnings and 0 errors. The Web.config and project XML parsed successfully, all nine ASPX page/code-behind registrations and declared event handlers were checked, and a focused source scan found no ASP.NET Core, Entity Framework, AJAX/UpdatePanel, `AddWithValue`, or stored-procedure usage in the Web Forms project. The ASP.NET Web Forms markup compiler, IIS, and live SQL Server are not available in this workspace, so full ASPX compilation, live database operations, workflow submissions, and file-upload execution are not claimed as tested. Run the integration checks in `INTEGRATION_GUIDE.md` on the target environment before release.

No Entity Framework, ASP.NET Core, Web API, AJAX, UpdatePanel, or stored-procedure calls are included in this Web Forms project.
