# Tender Management stack integration guide

The active frontend is Next.js 15 in `Frontend/app` and `Frontend/src`, backed by the ASP.NET Core 8 API in `Backend` and SQL Server. Configure `NEXT_PUBLIC_API_BASE_URL` for the frontend and the API connection string/JWT settings through local user-secrets or environment variables. See [Frontend setup](./README.md) and [Backend setup](../Backend/README.md).

The API exposes public, read-only `GET /api/dashboard/public-tenders` and `GET /api/dashboard/public-departments` endpoints for the homepage. Public tender results are limited to published notices; unpublished tender records, staff lists, and workflow actions remain protected. Authenticated DepartmentOfficer listing, dashboard counts, pending actions, and unpublished detail access are scoped to the signed-in officer.

The `.aspx` pages and `DepartmentModule.csproj` remain as a legacy Web Forms module only. The sections below document that legacy integration; they do not describe or configure the active Next.js/API runtime.

## Legacy Web Forms integration notes

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
    AdminRoles.aspx(.cs)
    AdminMasterData.aspx(.cs)
    AdminDepartments.aspx(.cs)
    AdminAnnouncements.aspx(.cs)
    Default.aspx(.cs)
    Tenders.aspx(.cs)
    TenderForm.aspx(.cs)
    TenderDetails.aspx(.cs)
    TenderWorkflow.aspx(.cs)
    Approvals.aspx(.cs)
    Bids.aspx(.cs)
    WorkManagement.aspx(.cs)
    DocumentDownload.aspx(.cs)
    Error.aspx(.cs)
  Content/Styles.css
  README.md
  INTEGRATION_GUIDE.md
SMC_TENDER_DATABASE.sql                 authoritative setup and upgrade entry point
```

The existing Web Forms project targets **.NET Framework 4.8** and is built from `Frontend/DepartmentModule.csproj`. Its root default document is configured as `Pages/Home.aspx`. It does not require Node.js, the ASP.NET Core API, or Entity Framework.

Migration coverage is not yet complete or end-to-end verified. The version-2 finalized workflow supports JE draft creation, draft submission checks, publication scheduling, scoped queue reads, per-tender required document checklists, initial technical bid opening, and JE primary document review. Contractor technical documents are uploaded as files and served through the authorized download handler. Tender Cell verification, shortfall, financial approval/opening, and final-document stages are not implemented yet. A newly registered contractor must first be linked to a `dbo.Contractors` record by an administrator in Manage Users. Do not retire the legacy API or treat the migration as feature-complete until remaining behavior has been implemented and tested.

## 2. IIS and framework prerequisites

Install .NET Framework 4.8 and the matching targeting pack/build tools. Enable IIS ASP.NET 4.8, .NET Extensibility, ISAPI Extensions, and ISAPI Filters. Configure the application pool for CLR v4.0, Integrated pipeline, and 64-bit where required. Enforce HTTPS.

## 3. SQL Server connection string

Set `connectionStrings/TenderManagement` in the module `Web.config` to the existing SQL Server database. Prefer Windows Integrated Security where available. Otherwise use a protected deployment transform or secret provider; do not commit SQL credentials. Grant the application identity only the SQL permissions required by these pages, including the additive RBAC, workflow, and bidder-document tables introduced by migrations 10-12.

## 4. Database setup and upgrade

`SMC_TENDER_DATABASE.sql` is the single authoritative SQL Server setup file. It supports explicit
`Fresh` and additive `Upgrade` modes through SQLCMD variables and refuses an unsafe or unsupported
target. Historical scripts are preserved in a verified archive outside the active project and
must not be run separately.

For a fresh, empty development database, run from the repository root:

```powershell
sqlcmd -S "localhost\SQLEXPRESS02" -E -b -v DatabaseName="TenderManagementDB" -v InstallMode=Fresh -v BackupVerified=NO -i "SMC_TENDER_DATABASE.sql"
```

Never use `Fresh` against a database containing objects or production records. For an existing database, take a full backup and verify a restore, then rehearse the `Upgrade` mode against that isolated restored copy:

```powershell
sqlcmd -S "localhost\SQLEXPRESS02" -E -b -v DatabaseName="TenderManagementDB_UpgradeTest" -v InstallMode=Upgrade -v BackupVerified=YES -i "SMC_TENDER_DATABASE.sql"
```

Use the upgrade mode only after confirming its preflight accepts the exact legacy schema and validating row counts, relationships, constraints, procedures, and application behavior. It never deletes existing tables or production records; if the schema differs or migration meaning is ambiguous, stop and prepare a reviewed mapping rather than modifying the script target to bypass checks. Do not run it against the populated `TenderManagementDB` until the backup, isolated rehearsal, and change approval are complete.

Fresh mode leaves every application table empty except for its setup ledger. It does not create
users, roles, permissions, departments, workflow stages, lookup values, contractors, tenders, bids,
or sample transactions. Complete trusted account provisioning and configure the required workflow
before staff use the application. Upgrade mode preserves existing configuration and business rows
without seeding or overwriting them. The setup records its schema stages in
`dbo.TenderSchemaMigrations`.

Rollback guidance: the application can be rolled back while leaving these additive schema changes in place. Do not drop the document metadata columns after uploads or verification history have been recorded. Drop the migration-12 checklist table only if it is confirmed empty and no deployed code uses it; otherwise restore a reviewed database backup or keep the additive schema.

## 5. Entry page and navigation

The configured root entry is `Pages/Home.aspx`; the staff dashboard is `Pages/Default.aspx`. Add the appropriate link in the existing Department Panel to the staff dashboard (or its application-relative URL). Keep the current `Pages/` path and merge the Web Forms pages into the existing application without replacing the panel's project, `Web.config`, `Global.asax`, master page, CSS, or route files. If the host requires an IIS child application, use a trusted authentication handoff as described below.

## 6. Authentication, session, and authorization handoff

The host Department Panel must authenticate the staff member and authorize access before opening the module. The module uses the configured session user-ID key to identify the user, then reloads active role, department, and permissions from SQL Server. Session role and department values are not authorization sources:

| Purpose | Default key | Required value |
|---|---|---|
| Database user identity | `UserId` | Positive `dbo.Users.UserId` |
| Role | `RoleName` | Optional display/handoff value; authorization uses active database role assignments |
| Department identity | `DepartmentId` | Optional display/handoff value; authorization uses active database department assignment |

Set `DepartmentPanelLoginUrl` in `Web.config` to the main panel's actual sign-in URL before integration. The Web Forms project includes public portal login/registration pages, but protected staff pages must continue to use the main panel's authenticated identity. If the host uses other session key names, set `SessionUserIdKey`; the role and department setting names remain for display/handoff compatibility. The user ID must refer to the same `dbo.Users` row used by existing foreign keys and must be established by the trusted server-side authentication process (never from a request parameter or browser form). `ModulePage` rejects unauthenticated/inactive users and loads authorization from the database. JE department access is derived from the user's active department assignment. The new workflow queue additionally requires a matching tender assignment. Existing legacy pages retain their existing workflow authorization rules; the new queue and role-management pages enforce their database permissions server-side. Draft edits and soft deletion in the legacy workflow remain restricted to `Admin`; only draft tenders can be edited or deleted.

Public login uses the registered mobile number and verifies a six-digit one-time code. In local Development, the API returns the generated code and the login page displays it on the OTP step; codes are not written to the API console. The local staff placeholders `0000000001` through `0000000004` are enabled for JE, Tender Cell, Chairman, and Director Auditor testing only and must not be enabled in production. Codes expire after five minutes; verification permits five attempts, and requests are limited to three per number in a 15-minute window with a 60-second cooldown. Rate-limited requests return HTTP 429 and a `Retry-After` header, keeping the user on the mobile-number step. Existing accounts need a registered mobile number; if multiple active accounts share the same normalized mobile number, login refuses to choose an account. Development OTP delivery is restricted to local requests; no SMS provider is configured. Do not expose development codes in production.

The host must enforce its own page authorization for the navigation link, deny untrusted users, and protect session cookies with HTTPS. If the module is hosted as a separate IIS application, implement a secure trusted session/SSO handoff; do not assume ASP.NET session state is shared across applications.

## 7. Naming and configuration conflict prevention

- Keep the existing assembly name `DepartmentModule`; change it consistently if the host already uses that name.
- Use only `SMC_TENDER_DATABASE.sql` for schema installation or the reviewed existing-database upgrade. Do not run retained historical migrations individually.
- `Tender.TenderType` is the procurement method / legacy value; `Tender.TenderCategory` is separate and nullable for historical rows whose category cannot be inferred. Method and category must not be interchanged.
- Catalog CRUD is available for tender categories, methods, document types, units, and officer designations; departments and roles/permissions have separate administration pages. Workflow status transitions remain coupled to application rules and are not directly editable from the master-data screen.
- For `DepartmentOfficer` sessions, dashboard data and tender details are limited to records created by that authenticated `UserId`.
- Keep its connection string name `TenderManagement`; if merged into a host config, ensure the name is unique and point it to the established DB.
- Do not overwrite the host's `Web.config`, `Global.asax`, `Site.Master`, `Styles.css`, or application session/authentication settings. Merge only the module's connection-string/appSettings and IIS handlers as needed.
- Keep upload storage isolated under the configured `UploadRoot` (default `~/Uploads`) and grant write permission only to the IIS app identity. Bidder uploads use generated names under `Uploads/tender-bids/{TenderId}` and are downloaded through the authorization-checked module handler; do not expose stored paths as public URLs.
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
6. Categorize uploaded proposal files as Tender Proposal, Tender Document, Technical Supporting Document, and Financial Supporting Document. Verify the JE/Department Officer cannot forward preparation until all four categories and at least one BOQ item exist, and can forward after completing the pack.
7. Approve a publication schedule as CAFO and verify only scheduled published tenders appear on the public homepage with the correct India Standard Time start and closing values.
8. Exercise each submit/approve/reject stage with the correct and incorrect role. Verify invalid transitions do not update `Tender` or `ApprovalHistory`, and valid moves write both atomically.
9. Confirm pending approvals are role-specific, status/history are accurate, and pagination/filter/search/sorting return expected results.
10. Evaluate technical bids, verify only `Qualified`/`Disqualified` is accepted, and recommend a financial bidder as Tender Committee. Confirm ranked bids are ordered lowest-first.
11. Record agreements, work orders, progress (including boundary values 0 and 100), measurements, bill approval, and payment. Verify only an approved bill can be paid and duplicate transaction numbers are rejected.
12. Upload, download through the authenticated handler, and verify invalid IDs/path traversal cannot read files outside the upload root.
13. Confirm SQL Server logs/audit records, database reads and writes, session expiry, IIS error logs, and responsive rendering at desktop and mobile widths.

## 10. IIS deployment

1. Install the IIS **ASP.NET 4.8**, **.NET Extensibility 4.8**, **ISAPI Extensions**, and **ISAPI Filters** role services, plus the .NET Framework 4.8 runtime. On the build machine, install the .NET Framework 4.8 targeting pack and Visual Studio ASP.NET/Web Build Tools.
2. Open `Frontend/DepartmentModule.csproj` in Visual Studio, restore NuGet packages, choose **Release**, then select **Publish → Folder** and publish to a clean staging directory. Deploy the full publish output, including `Pages`, `Content`, `bin`, `Global.asax`, and `Web.config`; do not copy only source `.aspx` files. Do not use `dotnet publish` for ASP.NET Core.
3. In the deployment `Web.config`, set the `TenderManagement` connection string to the production SQL Server/database, preserving encryption and certificate validation. Set `DepartmentPanelLoginUrl` to the intended login route. Do not commit production credentials.
4. Apply the reviewed `SMC_TENDER_DATABASE.sql` mode against the approved target only after a backup/restore test and isolated upgrade rehearsal; the setup file performs its own preflight and post-install checks.
5. In IIS Manager, create a site or application pointing to the publish directory. Use a dedicated application pool configured for **.NET CLR v4.0**, **Integrated** pipeline, and compatible 64-bit settings; do not select **No Managed Code**.
6. Bind the site to HTTPS with a trusted TLS certificate. The configured session cookies require SSL (`requireSSL=true`), so test login over HTTPS.
7. Grant the application-pool identity read/execute access to the published directory and only the necessary SQL permissions. If uploads are enabled, grant write permissions only to the configured `UploadRoot`.
8. **Do not launch publicly with the current OTP console delivery.** It is restricted to local requests and is not an SMS service. Integrate and test an approved SMS provider before enabling live authentication; never enable debug compilation on a public server.
9. Test the home page, `/login`, `/register`, OTP request and verification, sign-out, authorized/unauthorized pages, and upload/download flows using a test database and accounts. In local development, use the OTP log watcher described above; do not consider the login ready for public production until the SMS provider is implemented and tested.

## 11. Troubleshooting

- **Parser/build errors:** verify the .NET Framework 4.8 targeting pack and Web Build Tools are installed and that the Web Application project is opened/built in Visual Studio/MSBuild.
- **Login redirect loop:** confirm the host and module share the intended session or the secure SSO bridge, and that the configured session keys are populated before navigating.
- **SQL connection failure:** check the connection string, SQL Server network/firewall, TLS settings, and IIS application-pool identity permissions.
- **SQL invalid object/column:** run the schema preflight, verify the target database, and coordinate database schema version with the database owner.
- **Upload failures:** verify request limits, allowed file extension, 20 MB per-file limit, folder write ACL, free disk space, and the configured `UploadRoot`.
- **403/incorrect workflow queue:** verify the user has an active database role, required JE department, matching workflow assignment, and the required permission mapping.
- **Missing static styles:** verify `Content/Styles.css` is included in the published output and that the module virtual path is correct.
- **Workflow conflict:** refresh the tender. Transitions check the current status at write time and reject stale postbacks to prevent overwriting another user's action.

## Scope preserved

The existing version-1 workflow includes department-side tender intake, administrator draft editing/deletion, draft BOQ and supporting documents, approval/rejection history, contractor bids, technical evaluation, financial recommendation, and work execution records. Version 2 is additive and preserves existing tenders; so far it supports JE intake, checked draft submission, publication scheduling, assigned queues, role administration, and the RBAC/schema foundation. Version-2 bidder verification, shortfall handling, financial approval/opening, work-order/agreement generation, and PDF generation are outstanding. No end-to-end role-isolation or IIS tests have been run. Verify parity against the retained API before removing it.
