# Database setup

The application database is installed and upgraded only through the repository-root
`SMC_TENDER_DATABASE.sql`. The `Migrations` folder has no active EF Core migrations. Do not follow
the former `01_CreateDatabase_Schema.sql`, `02_StoredProcedures.sql`, or `03_SeedData.sql`
instructions; those files are not part of the supported setup.

Use the SQLCMD Fresh mode only for a new, empty database. Use Upgrade mode only after a verified
backup and a successful rehearsal on an isolated restored copy. See the repository-root
`SMC_TENDER_MASTER_DOCUMENTATION.md` for commands, safeguards, and validation results.
