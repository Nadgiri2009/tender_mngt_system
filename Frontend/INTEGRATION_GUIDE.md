# Department Panel integration guide

The Web Forms implementation is in the existing `Frontend` directory as a .NET Framework application. React/Next.js source, its Node package manifests/dependencies, and generated Next.js output have been removed. Keep the main Department Panel's authentication and authorization as the source of truth for staff access. The Web Forms project also contains public portal login and contractor-registration pages, but those do not replace the main panel's staff authentication.

The separate `Backend` directory still contains an ASP.NET Core 8 / Entity Framework API. It is not referenced or deployed by this Web Forms project and has not been migrated or removed. The Web Forms pages use direct ADO.NET SQL Server access.

## 1. Files and layout

The Web Forms project is rooted in the existing `Frontend` directory:

```text
Frontend/
  DepartmentModule.csproj
  Web.config
  Global.asax
  Helpers/
    ModuleDb.cs
    ModulePage.cs
    WorkflowRules.cs
  Pages/
    Site.Master
    Home.aspx(.cs)
    Login.aspx(.cs)
    Register.aspx(.cs)
    Admin.aspx(.cs)
    AdminUsers.aspx(.cs)
    AdminDepartments.aspx(.cs)
    AdminAnnouncements.aspx(.cs)
    Default.aspx(.cs)
    Tenders.aspx(.cs)
    TenderForm.aspx(.cs)
    TenderDetails.aspx(.cs)
    Approvals.aspx(.cs)
    Bids.aspx(.cs)
    WorkManagement.aspx(.cs)
    DocumentDownload.aspx(.cs)
    Error.aspx(.cs)
  Content/Styles.css
  README.md
  INTEGRATION_GUIDE.md
Database/
  DepartmentModuleSchema.sql
```

The existing Web Forms project targets **.NET Framework 4.8** and is built from `Frontend/DepartmentModule.csproj`. Its root default document is configured as `Pages/Home.aspx`. It does not require Node.js, the ASP.NET Core API, or Entity Framework.

Migration coverage is not yet complete or end-to-end verified. Contractor-side technical and qualified financial bid submission are implemented on the Web Forms bid page. A newly registered contractor must first be linked to a `dbo.Contractors` record by an administrator in Manage Users. Do not retire the legacy API or treat the migration as feature-complete until remaining behavior has been ported and tested.

## 2. IIS and framework prerequisites

Install .NET Framework 4.8 and the matching targeting pack/build tools. Enable IIS ASP.NET 4.8, .NET Extensibility, ISAPI Extensions, and ISAPI Filters. Configure the application pool for CLR v4.0, Integrated pipeline, and 64-bit where required. Enforce HTTPS.

## 3. SQL Server connection string

Set `connectionStrings/TenderManagement` in the module `Web.config` to the existing SQL Server database. Prefer Windows Integrated Security where available. Otherwise use a protected deployment transform or secret provider; do not commit SQL credentials. Grant the application identity only the SQL permissions required by these pages (read and writes to existing department workflow tables).

## 4. Database preflight

Run `Database/DepartmentModuleSchema.sql` against `TenderManagementDB`. It checks required table and column presence and makes **no changes**. The department module does not require a schema migration and does not call stored procedures. Do not run the legacy full database setup script against production; it includes sample accounts and legacy stored-procedure definitions. If the database is new, have the database owner provision the existing application schema using the separately approved SQL Server deployment process first.

## 5. Entry page and navigation

The configured root entry is `Pages/Home.aspx`; the staff dashboard is `Pages/Default.aspx`. Add the appropriate link in the existing Department Panel to the staff dashboard (or its application-relative URL). Keep the current `Pages/` path and merge the Web Forms pages into the existing application without replacing the panel's project, `Web.config`, `Global.asax`, master page, CSS, or route files. If the host requires an IIS child application, use a trusted authentication handoff as described below.

## 6. Authentication, session, and authorization handoff

The host Department Panel must authenticate the staff member and authorize access before opening the module. The Web Forms module reads these session values:

| Purpose | Default key | Required value |
|---|---|---|
| Database user identity | `UserId` | Positive `dbo.Users.UserId` |
| Role | `RoleName` | Existing role name such as `DepartmentOfficer`, `CAFOOfficer`, `TenderSection`, or `TenderCommittee` |
| Department identity | `DepartmentId` | Optional positive `dbo.Departments.DepartmentId` |

Set `DepartmentPanelLoginUrl` in `Web.config` to the main panel's actual sign-in URL before integration. The Web Forms project includes public portal login/registration pages, but protected staff pages must continue to use the main panel's authenticated identity. If the host uses other session key names, update `SessionUserIdKey`, `SessionRoleKey`, and `SessionDepartmentIdKey`. The user ID must refer to the same `dbo.Users` row used by existing foreign keys, and the role value must be assigned by the trusted server-side authentication process (never from a request parameter or browser form). `ModulePage` rejects unauthenticated requests and every write handler repeats role and business-rule checks server-side. Draft edits and soft deletion are restricted to `Admin`; only draft tenders can be edited or deleted.

The host must enforce its own page authorization for the navigation link, deny untrusted users, and protect session cookies with HTTPS. If the module is hosted as a separate IIS application, implement a secure trusted session/SSO handoff; do not assume ASP.NET session state is shared across applications.

## 7. Naming and configuration conflict prevention

- Keep the existing assembly name `DepartmentModule`; change it consistently if the host already uses that name.
- Retain the existing `dbo` object names and database. The module creates no tables, indexes, roles, seed data, or procedures.
- Keep its connection string name `TenderManagement`; if merged into a host config, ensure the name is unique and point it to the established DB.
- Do not overwrite the host's `Web.config`, `Global.asax`, `Site.Master`, `Styles.css`, or application session/authentication settings. Merge only the module's connection-string/appSettings and IIS handlers as needed.
- Keep upload storage isolated under the configured `UploadRoot` (default `~/Uploads`) and grant write permission only to the IIS app identity. Files are downloaded through the authenticated module handler.
- If the host already owns a default page or similar names, retain the existing `Pages/` path rather than overwriting the host page.

## 8. Protect the main panel

Keep the main panel files and authentication as the source of truth. Add the module pages/project into the existing source tree and a navigation link, then implement the trusted session handoff described above. Review the IIS application boundary, session cookie path/domain/SameSite settings, access permissions to uploads, and role mapping before deploying. Do not copy this module's `Web.config` over the main panel's config.

## 9. Integration tests

Use non-production test accounts and a restored/test database:

1. After setting `DepartmentPanelLoginUrl`, verify unauthenticated access is redirected to the main panel's login and an authenticated role cannot reach actions outside its grants.
2. Create a tender with each supported tender type; test required title, title length, department, amount, optional deadline, and allowed/blocked upload extensions and sizes.
3. Confirm department dropdown options come from active SQL Server department rows; check tender number uniqueness under concurrent test submissions.
4. Edit and soft-delete draft tenders as Admin; verify non-admin users are denied and non-draft tenders cannot be edited or deleted.
5. Add/remove BOQ items in Draft only and verify computed line amounts and all SQL values persist.
6. Exercise each submit/approve/reject stage with the correct and incorrect role. Verify invalid transitions do not update `Tender` or `ApprovalHistory`, and valid moves write both atomically.
7. Confirm pending approvals are role-specific, status/history are accurate, and pagination/filter/search/sorting return expected results.
8. Evaluate technical bids, verify only `Qualified`/`Disqualified` is accepted, and recommend a financial bidder as Tender Committee. Confirm ranked bids are ordered lowest-first.
9. Record agreements, work orders, progress (including boundary values 0 and 100), measurements, bill approval, and payment. Verify only an approved bill can be paid and duplicate transaction numbers are rejected.
10. Upload, download through the authenticated handler, and verify invalid IDs/path traversal cannot read files outside the upload root.
11. Confirm SQL Server logs/audit records, database reads and writes, session expiry, IIS error logs, and responsive rendering at desktop and mobile widths.

## 10. Deploy and troubleshoot

Build the classic project in Visual Studio/Build Tools with the .NET Framework 4.8 Web Application targets. Publish the Web Forms application to IIS, set the production connection string and HTTPS, grant least-privilege database and upload-folder permissions, and verify the panel session integration. Do not use `dotnet publish` for ASP.NET Core.

- **Parser/build errors:** verify the .NET Framework 4.8 targeting pack and Web Build Tools are installed and that the Web Application project is opened/built in Visual Studio/MSBuild.
- **Login redirect loop:** confirm the host and module share the intended session or the secure SSO bridge, and that the configured session keys are populated before navigating.
- **SQL connection failure:** check the connection string, SQL Server network/firewall, TLS settings, and IIS application-pool identity permissions.
- **SQL invalid object/column:** run the schema preflight, verify the target database, and coordinate database schema version with the database owner.
- **Upload failures:** verify request limits, allowed file extension, 20 MB per-file limit, folder write ACL, free disk space, and the configured `UploadRoot`.
- **403/incorrect approval queue:** verify the trusted role string matches the existing role names exactly and that it has the department panel permission.
- **Missing static styles:** verify `Content/Styles.css` is included in the published output and that the module virtual path is correct.
- **Workflow conflict:** refresh the tender. Transitions check the current status at write time and reject stale postbacks to prevent overwriting another user's action.

## Scope preserved

Department-side tender intake, administrator draft editing/deletion, dropdown lookup, draft BOQ and supporting documents, tender search/filter/sort/pagination, defined status sequence, stage-level role gates, approval/rejection history, contractor technical/financial bid submissions, technical bid evaluation, financial bid ranking/recommendation, work execution, measurement books, bill approvals, and payment release are represented in the module. The module also includes public home/login/registration pages and admin user, contractor-profile, department, and announcement management. Verify parity against the retained API before removing it.
