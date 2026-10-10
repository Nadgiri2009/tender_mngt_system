# Department Panel integration guide

The original portal and API source have been restored in their original `Frontend/src` and `Backend/src` locations. The Web Forms department module remains at the `Frontend` root. Keep these entry points and their configurations distinct; do not replace or overwrite the main Department Panel's project, authentication pages, shared configuration, or navigation files.

## 1. Files and layout

The Web Forms project is rooted in the existing `Frontend` directory:

```text
Frontend/
  package.json                  # restored original Next.js portal
  src/
    app/(public)/login/page.tsx # original login route, restored unchanged
    app/(public)/register/page.tsx
  DepartmentModule.csproj
  Web.config
  Global.asax
  Helpers/
    ModuleDb.cs
    ModulePage.cs
    WorkflowRules.cs
  Pages/
    Site.Master
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

`Frontend/src` and `Backend/src` were restored byte-for-byte from the intact project snapshot; generated dependency and build folders were not copied. `Backend/TenderManagement.sln` is the original .NET 8 API solution. The Web Forms project is a separate classic ASP.NET Web Application targeting **.NET Framework 4.8** and is opened directly through `Frontend/DepartmentModule.csproj`.

To run the restored portal, run `npm ci` and `npm run dev` from `Frontend`; its login and registration routes are `/login` and `/register`. To build the original API, use `dotnet build Backend/TenderManagement.sln`. The Web Forms module runs through IIS Express/IIS at `Pages/Default.aspx`.

## 2. IIS and framework prerequisites

Install .NET Framework 4.8 and the matching targeting pack/build tools. Enable IIS ASP.NET 4.8, .NET Extensibility, ISAPI Extensions, and ISAPI Filters. Configure the application pool for CLR v4.0, Integrated pipeline, and 64-bit where required. Enforce HTTPS.

## 3. SQL Server connection string

Set `connectionStrings/TenderManagement` in the module `Web.config` to the existing SQL Server database. Prefer Windows Integrated Security where available. Otherwise use a protected deployment transform or secret provider; do not commit SQL credentials. Grant the application identity only the SQL permissions required by these pages (read and writes to existing department workflow tables).

## 4. Database preflight

Run `Database/DepartmentModuleSchema.sql` against `TenderManagementDB`. It checks required table and column presence and makes **no changes**. The department module does not require a schema migration and does not call stored procedures. Do not run the legacy full database setup script against production; it includes sample accounts and legacy stored-procedure definitions. If the database is new, have the database owner provision the existing application schema using the separately approved SQL Server deployment process first.

## 5. Entry page and navigation

The module entry is `Pages/Default.aspx`. Publish the module under a protected virtual application/path such as `/DepartmentTender`, then add a link in the main Department Panel to `/DepartmentTender/Pages/Default.aspx`. Keep the module project and static files under their own IIS application boundary if sharing one server; do not overwrite the panel's `Web.config`, `Global.asax`, master page, CSS, or route files.

## 6. Authentication, session, and authorization handoff

The restored portal retains its original login and registration pages under `Frontend/src`, along with the API authentication endpoints under `Backend/src`; these source files were restored unchanged. They are not automatically an authentication bridge for the separate Web Forms application. Before allowing Web Forms module entry, the host panel must authenticate the staff member and authorize access to the department module. The Web Forms module reads these session values:

| Purpose | Default key | Required value |
|---|---|---|
| Database user identity | `UserId` | Positive `dbo.Users.UserId` |
| Role | `RoleName` | Existing role name such as `DepartmentOfficer`, `CAFOOfficer`, `TenderSection`, or `TenderCommittee` |
| Department identity | `DepartmentId` | Optional positive `dbo.Departments.DepartmentId` |

If the host uses other key names, update `SessionUserIdKey`, `SessionRoleKey`, and `SessionDepartmentIdKey` in `Web.config`. The user ID must refer to the same `dbo.Users` row used by existing foreign keys, and the role value must be assigned by the trusted server-side authentication process (never from a request parameter or browser form). If the module is a separate IIS application, implement a secure host-to-module session/SSO bridge; do not assume ASP.NET session state is shared across applications. `ModulePage` rejects unauthenticated requests and every write handler repeats role and business-rule checks server-side.

The host must continue enforcing its own page authorization for the navigation link, deny untrusted users, and protect session cookies with HTTPS. Do not replace or alter the restored portal login flow. If the portal and Web Forms module need to share authentication, implement a secure trusted session/SSO handoff; do not assume their sessions or tokens are interchangeable.

## 7. Naming and configuration conflict prevention

- Keep the module assembly name `DepartmentModule`; change it consistently if the host already uses that name.
- Retain the existing `dbo` object names and database. The module creates no tables, indexes, roles, seed data, or procedures.
- Keep its connection string name `TenderManagement`; if merged into a host config, ensure the name is unique and point it to the established DB.
- Do not overwrite the host's `Web.config`, `Global.asax`, `Site.Master`, `Styles.css`, or application session/authentication settings. Merge only the module's connection-string/appSettings and IIS handlers as needed.
- Keep upload storage isolated under the configured `UploadRoot` (default `~/Uploads`) and grant write permission only to the IIS app identity. Files are downloaded through the authenticated module handler.
- If the host already owns `Default.aspx` or similar names, retain the module's `Pages/` path rather than copying module pages over host pages.

## 8. Protect the main panel

Keep the main panel files and authentication as the source of truth. Add only a navigation link and the explicit trusted session handoff described above. Review the IIS virtual-directory application boundary, session cookie path/domain/SameSite settings, access permissions to uploads, and role mapping before deploying. Do not copy the module's `Web.config` over the main panel's config.

## 9. Integration tests

Use non-production test accounts and a restored/test database:

1. Verify unauthenticated access is redirected to the main panel's login and an authenticated role cannot reach actions outside its grants.
2. Create a tender with each supported tender type; test required title, title length, department, amount, optional deadline, and allowed/blocked upload extensions and sizes.
3. Confirm department dropdown options come from active SQL Server department rows; check tender number uniqueness under concurrent test submissions.
4. Add/remove BOQ items in Draft only and verify computed line amounts and all SQL values persist.
5. Exercise each submit/approve/reject stage with the correct and incorrect role. Verify invalid transitions do not update `Tender` or `ApprovalHistory`, and valid moves write both atomically.
6. Confirm pending approvals are role-specific, status/history are accurate, and pagination/filter/search/sorting return expected results.
7. Evaluate technical bids, verify only `Qualified`/`Disqualified` is accepted, and recommend a financial bidder as Tender Committee. Confirm ranked bids are ordered lowest-first.
8. Record agreements, work orders, progress (including boundary values 0 and 100), measurements, bill approval, and payment. Verify only an approved bill can be paid and duplicate transaction numbers are rejected.
9. Upload, download through the authenticated handler, and verify invalid IDs/path traversal cannot read files outside the upload root.
10. Confirm SQL Server logs/audit records, database reads and writes, session expiry, IIS error logs, and responsive rendering at desktop and mobile widths.

## 10. Deploy and troubleshoot

Build the classic project in Visual Studio/Build Tools with the .NET Framework 4.8 Web Application targets. Publish to the dedicated IIS application, set the production connection string and HTTPS, grant least-privilege database and upload-folder permissions, and verify the panel session integration. Do not use `dotnet publish` for ASP.NET Core.

- **Parser/build errors:** verify the .NET Framework 4.8 targeting pack and Web Build Tools are installed and that the Web Application project is opened/built in Visual Studio/MSBuild.
- **Login redirect loop:** confirm the host and module share the intended session or the secure SSO bridge, and that the configured session keys are populated before navigating.
- **SQL connection failure:** check the connection string, SQL Server network/firewall, TLS settings, and IIS application-pool identity permissions.
- **SQL invalid object/column:** run the schema preflight, verify the target database, and coordinate database schema version with the database owner.
- **Upload failures:** verify request limits, allowed file extension, 20 MB per-file limit, folder write ACL, free disk space, and the configured `UploadRoot`.
- **403/incorrect approval queue:** verify the trusted role string matches the existing role names exactly and that it has the department panel permission.
- **Missing static styles:** verify `Content/Styles.css` is included in the published output and that the module virtual path is correct.
- **Workflow conflict:** refresh the tender. Transitions check the current status at write time and reject stale postbacks to prevent overwriting another user's action.

## Scope preserved

Department-side tender intake, dropdown lookup, draft BOQ and supporting documents, tender search/filter/sort/pagination, full defined status sequence, stage-level role gates, approval/rejection history, technical bid evaluation, financial bid ranking/recommendation, work execution, measurement books, bill approvals, and payment release are represented in the module. The public/contractor/admin screens remain outside this Department Panel module; contractor bid submission and admin account/configuration tasks stay with their existing host areas.
