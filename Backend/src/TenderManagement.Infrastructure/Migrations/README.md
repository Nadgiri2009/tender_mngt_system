# EF Core Migrations

This folder intentionally ships empty. The database schema for this project is authored directly
as SQL Server scripts in `/Database` (recommended — guarantees the schema matches exactly what's
documented, including stored procedures, computed columns, and seed data).

If you'd rather drive the schema from the EF Core model instead of the SQL scripts, generate a
migration once the solution restores successfully:

```bash
cd Backend/src/TenderManagement.API
dotnet ef migrations add InitialCreate --project ../TenderManagement.Infrastructure --startup-project .
dotnet ef database update --project ../TenderManagement.Infrastructure --startup-project .
```

Note: if you take this route, skip running `01_CreateDatabase_Schema.sql` (EF will create the
tables for you) but still run `02_StoredProcedures.sql` and `03_SeedData.sql` afterwards, since
those aren't part of the EF model.
