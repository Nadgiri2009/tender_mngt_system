using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DepartmentModule.Helpers
{
    public static class ModuleDb
    {
        public static SqlConnection OpenConnection()
        {
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["TenderManagement"];
            if (setting == null || String.IsNullOrWhiteSpace(setting.ConnectionString) ||
                setting.ConnectionString.IndexOf("YOUR_SQL_SERVER", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new ConfigurationErrorsException("Set the TenderManagement connection string in Web.config before using the module.");
            }

            SqlConnection connection = new SqlConnection(setting.ConnectionString);
            connection.Open();
            return connection;
        }

        public static void AddParameter(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
        {
            SqlParameter parameter = size == -1
                ? command.Parameters.Add(name, type, -1)
                : size > 0 ? command.Parameters.Add(name, type, size) : command.Parameters.Add(name, type);
            parameter.Value = value ?? DBNull.Value;
            if (type == SqlDbType.Decimal && value is decimal)
            {
                int[] bits = Decimal.GetBits((decimal)value);
                parameter.Precision = 18;
                parameter.Scale = (byte)Math.Min(18, (bits[3] >> 16) & 0x7F);
            }
        }

        public static void AddAudit(SqlConnection connection, SqlTransaction transaction, int? userId,
            string action, string entityName, int? entityId, string details, string ipAddress)
        {
            using (SqlCommand command = new SqlCommand(
                @"INSERT INTO dbo.AuditLog (UserId, Action, EntityName, EntityId, Details, IPAddress)
                  VALUES (@UserId, @Action, @EntityName, @EntityId, @Details, @IPAddress);",
                connection, transaction))
            {
                AddParameter(command, "@UserId", SqlDbType.Int, userId.HasValue ? (object)userId.Value : null);
                AddParameter(command, "@Action", SqlDbType.NVarChar, action, 80);
                AddParameter(command, "@EntityName", SqlDbType.NVarChar, entityName, 100);
                AddParameter(command, "@EntityId", SqlDbType.Int, entityId.HasValue ? (object)entityId.Value : null);
                AddParameter(command, "@Details", SqlDbType.NVarChar, details);
                AddParameter(command, "@IPAddress", SqlDbType.NVarChar, ipAddress, 50);
                command.ExecuteNonQuery();
            }
        }

        public static string Html(object value)
        {
            return System.Web.HttpUtility.HtmlEncode(value == null || value == DBNull.Value ? String.Empty : Convert.ToString(value));
        }
    }
}
