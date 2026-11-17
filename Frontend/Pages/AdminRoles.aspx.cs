using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class AdminRoles : ModulePage
    {
        protected Literal Message, FormHeading;
        protected HiddenField RoleIdHidden;
        protected TextBox RoleNameText, RoleDescriptionText;
        protected CheckBoxList PermissionsList;
        protected Button SaveRoleButton, CancelEditButton;
        protected GridView RolesGrid;
        protected HiddenField PermissionIdHidden;
        protected TextBox PermissionKeyText, PermissionDescriptionText;
        protected Button SavePermissionButton, CancelPermissionButton;
        protected GridView PermissionsGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequirePermission("roles.manage")) return;
            if (!IsPostBack)
            {
                try
                {
                    using (SqlConnection connection = ModuleDb.OpenConnection())
                    {
                        BindPermissions(connection);
                        BindRoles(connection);
                    }
                }
                catch (SqlException ex)
                {
                    Trace.Warn("DepartmentModule", "Role administration data could not be loaded.", ex);
                    ShowMessage("Role data could not be loaded. Please retry or contact support.");
                    SaveRoleButton.Enabled = false;
                }
            }
        }

        private void BindPermissions(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                "SELECT PermissionKey,Description FROM dbo.Permissions WHERE IsActive=1 ORDER BY PermissionKey;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                PermissionsList.Items.Clear();
                while (reader.Read())
                {
                    string key = Convert.ToString(reader["PermissionKey"]);
                    PermissionsList.Items.Add(new ListItem(
                        key + " — " + Convert.ToString(reader["Description"]), key));
                }
            }
            using (SqlCommand command = new SqlCommand(
                @"SELECT p.PermissionId,p.PermissionKey,p.Description,p.IsActive,
                         (SELECT COUNT(*) FROM dbo.RolePermissions rp WHERE rp.PermissionId=p.PermissionId) AS AssignedRoles
                  FROM dbo.Permissions p ORDER BY p.PermissionKey;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable data = new DataTable();
                adapter.Fill(data);
                PermissionsGrid.DataSource = data;
                PermissionsGrid.DataBind();
            }
        }

        private void BindRoles(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT r.RoleId,r.RoleName,r.Description,r.IsActive,r.IsSystemRole,
                         (SELECT COUNT(*) FROM dbo.RolePermissions rp WHERE rp.RoleId=r.RoleId) AS PermissionCount,
                         (SELECT COUNT(*) FROM dbo.Users u WHERE u.RoleId=r.RoleId) AS AssignedUsers
                  FROM dbo.Roles r ORDER BY r.IsSystemRole DESC,r.RoleName;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable data = new DataTable();
                adapter.Fill(data);
                RolesGrid.DataSource = data;
                RolesGrid.DataBind();
            }
        }

        protected void RolesGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int roleId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out roleId) || roleId <= 0)
            {
                ShowMessage("The selected role is invalid.");
                return;
            }

            if (e.CommandName == "EditRole")
            {
                LoadRole(roleId);
                return;
            }
            if (e.CommandName == "ToggleRole")
            {
                ToggleRole(roleId);
                return;
            }
            if (e.CommandName == "DeleteRole") DeleteRole(roleId);
        }

        private void LoadRole(int roleId)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    BindPermissions(connection);
                    using (SqlCommand command = new SqlCommand(
                        "SELECT RoleName,Description FROM dbo.Roles WHERE RoleId=@RoleId;", connection))
                    {
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowMessage("The selected role no longer exists.");
                                return;
                            }
                            RoleNameText.Text = Convert.ToString(reader["RoleName"]);
                            RoleDescriptionText.Text = reader["Description"] == DBNull.Value
                                ? String.Empty
                                : Convert.ToString(reader["Description"]);
                        }
                    }

                    using (SqlCommand command = new SqlCommand(
                        @"SELECT p.PermissionKey FROM dbo.RolePermissions rp
                          INNER JOIN dbo.Permissions p ON p.PermissionId=rp.PermissionId
                          WHERE rp.RoleId=@RoleId;", connection))
                    {
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ListItem item = PermissionsList.Items.FindByValue(Convert.ToString(reader["PermissionKey"]));
                                if (item != null) item.Selected = true;
                            }
                        }
                    }
                }

                RoleIdHidden.Value = roleId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                FormHeading.Text = "Edit role";
                CancelEditButton.Visible = true;
                SaveRoleButton.Text = "Save role changes";
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Role details could not be loaded.", ex);
                ShowMessage("The selected role could not be loaded. Please retry or contact support.");
            }
        }

        protected void SaveRoleButton_Click(object sender, EventArgs e)
        {
            Page.Validate("Role");
            string name = RoleNameText.Text.Trim();
            string description = RoleDescriptionText.Text.Trim();
            int roleId;
            bool isEditing = Int32.TryParse(RoleIdHidden.Value, out roleId) && roleId > 0;
            if (!Page.IsValid || name.Length == 0 || name.Length > 100 || description.Length > 400)
            {
                ShowMessage("Enter a role name of up to 100 characters and a description of up to 400 characters.");
                return;
            }

            List<string> permissionKeys = PermissionsList.Items.Cast<ListItem>()
                .Where(item => item.Selected)
                .Select(item => item.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    string oldName = null;
                    bool isSystemRole = false;
                    if (isEditing)
                    {
                        using (SqlCommand command = new SqlCommand(
                            "SELECT RoleName,IsSystemRole FROM dbo.Roles WITH (UPDLOCK,HOLDLOCK) WHERE RoleId=@RoleId;", connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    ShowMessage("The selected role no longer exists.");
                                    return;
                                }
                                oldName = Convert.ToString(reader["RoleName"]);
                                isSystemRole = Convert.ToBoolean(reader["IsSystemRole"]);
                            }
                        }
                        if (isSystemRole && !String.Equals(oldName, name, StringComparison.Ordinal))
                        {
                            ShowMessage("Protected system role names cannot be changed.");
                            return;
                        }
                    }

                    List<int> permissionIds = LoadPermissionIds(connection, transaction, permissionKeys);
                    if (permissionIds.Count != permissionKeys.Count)
                    {
                        ShowMessage("One or more selected permissions are no longer active. Refresh and retry.");
                        return;
                    }

                    bool alreadyHasSystemAdmin = false;
                    if (isEditing)
                    {
                        using (SqlCommand command = new SqlCommand(
                            @"SELECT COUNT(*) FROM dbo.RolePermissions rp
                              INNER JOIN dbo.Permissions p ON p.PermissionId=rp.PermissionId
                              WHERE rp.RoleId=@RoleId AND p.PermissionKey=N'system.admin';", connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                            alreadyHasSystemAdmin = Convert.ToInt32(command.ExecuteScalar()) > 0;
                        }
                    }
                    if (permissionKeys.Contains("system.admin", StringComparer.OrdinalIgnoreCase) &&
                        !alreadyHasSystemAdmin &&
                        !(String.Equals(name, "Super Admin", StringComparison.OrdinalIgnoreCase) &&
                          String.Equals(CurrentRole, "Super Admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        ShowMessage("Only an existing Super Admin can grant the system administration permission.");
                        return;
                    }

                    if (isEditing)
                    {
                        using (SqlCommand command = new SqlCommand(
                            "UPDATE dbo.Roles SET RoleName=@RoleName,Description=@Description WHERE RoleId=@RoleId;",
                            connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@RoleName", SqlDbType.NVarChar, name, 100);
                            ModuleDb.AddParameter(command, "@Description", SqlDbType.NVarChar,
                                String.IsNullOrWhiteSpace(description) ? null : description, 400);
                            ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (SqlCommand command = new SqlCommand(
                            @"INSERT INTO dbo.Roles(RoleName,Description,IsActive,IsSystemRole)
                              VALUES(@RoleName,@Description,1,0); SELECT CAST(SCOPE_IDENTITY() AS int);",
                            connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@RoleName", SqlDbType.NVarChar, name, 100);
                            ModuleDb.AddParameter(command, "@Description", SqlDbType.NVarChar,
                                String.IsNullOrWhiteSpace(description) ? null : description, 400);
                            roleId = Convert.ToInt32(command.ExecuteScalar());
                        }
                    }

                    using (SqlCommand command = new SqlCommand(
                        "DELETE FROM dbo.RolePermissions WHERE RoleId=@RoleId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                        command.ExecuteNonQuery();
                    }
                    for (int i = 0; i < permissionIds.Count; i++)
                    {
                        using (SqlCommand command = new SqlCommand(
                            @"INSERT INTO dbo.RolePermissions(RoleId,PermissionId,AssignedBy)
                              VALUES(@RoleId,@PermissionId,@AssignedBy);", connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                            ModuleDb.AddParameter(command, "@PermissionId", SqlDbType.Int, permissionIds[i]);
                            ModuleDb.AddParameter(command, "@AssignedBy", SqlDbType.Int, CurrentUserId);
                            command.ExecuteNonQuery();
                        }
                    }

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId,
                        isEditing ? "Update" : "Create", "Role", roleId,
                        "Role details and permission assignments updated.", Request.UserHostAddress);
                    transaction.Commit();
                }

                ResetForm();
                ShowMessage("Role saved.", true);
                RebindRoles();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Role could not be saved.", ex);
                ShowMessage(ex.Number == 2601 || ex.Number == 2627
                    ? "A role with that name already exists."
                    : "The role could not be saved. Please retry or contact support.");
            }
        }

        private static List<int> LoadPermissionIds(SqlConnection connection, SqlTransaction transaction, IList<string> keys)
        {
            List<int> ids = new List<int>();
            if (keys.Count == 0) return ids;
            List<string> parameterNames = new List<string>();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;
                for (int i = 0; i < keys.Count; i++)
                {
                    string parameterName = "@Permission" + i;
                    parameterNames.Add(parameterName);
                    ModuleDb.AddParameter(command, parameterName, SqlDbType.NVarChar, keys[i], 120);
                }
                command.CommandText = "SELECT PermissionId FROM dbo.Permissions WHERE IsActive=1 AND PermissionKey IN (" +
                    String.Join(",", parameterNames) + ");";
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) ids.Add(Convert.ToInt32(reader["PermissionId"]));
                }
            }
            return ids;
        }

        private void ToggleRole(int roleId)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    bool isActive, isSystemRole;
                    using (SqlCommand command = new SqlCommand(
                        "SELECT IsActive,IsSystemRole FROM dbo.Roles WITH (UPDLOCK,HOLDLOCK) WHERE RoleId=@RoleId;",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowMessage("The selected role no longer exists.");
                                return;
                            }
                            isActive = Convert.ToBoolean(reader["IsActive"]);
                            isSystemRole = Convert.ToBoolean(reader["IsSystemRole"]);
                        }
                    }
                    if (isSystemRole)
                    {
                        ShowMessage("Protected system roles cannot be deactivated.");
                        return;
                    }
                    if (isActive)
                    {
                        using (SqlCommand command = new SqlCommand(
                            "SELECT COUNT(*) FROM dbo.Users WITH (UPDLOCK,HOLDLOCK) WHERE RoleId=@RoleId AND IsActive=1;",
                            connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                            if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                            {
                                ShowMessage("A role assigned to active users cannot be deactivated.");
                                return;
                            }
                        }
                    }
                    using (SqlCommand command = new SqlCommand(
                        "UPDATE dbo.Roles SET IsActive=@IsActive WHERE RoleId=@RoleId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@IsActive", SqlDbType.Bit, !isActive);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                        command.ExecuteNonQuery();
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, isActive ? "Deactivate" : "Activate",
                        "Role", roleId, "Role active status changed.", Request.UserHostAddress);
                    transaction.Commit();
                }
                ShowMessage("Role status updated.", true);
                RebindRoles();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Role status could not be changed.", ex);
                ShowMessage("The role status could not be changed. Please retry or contact support.");
            }
        }

        private void DeleteRole(int roleId)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (SqlCommand command = new SqlCommand(
                        @"IF EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleId=@RoleId AND IsSystemRole=1)
                              THROW 51010,'Protected role cannot be deleted.',1;
                          IF EXISTS(SELECT 1 FROM dbo.Users WITH (UPDLOCK,HOLDLOCK) WHERE RoleId=@RoleId)
                              THROW 51011,'Role is assigned to users.',1;
                          IF EXISTS(SELECT 1 FROM dbo.WorkflowStages WHERE ApproverRoleId=@RoleId)
                              THROW 51012,'Role is referenced by workflow stages.',1;
                          DELETE FROM dbo.RolePermissions WHERE RoleId=@RoleId;
                          DELETE FROM dbo.Roles WHERE RoleId=@RoleId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                        command.ExecuteNonQuery();
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Delete", "Role", roleId,
                        "Unassigned custom role deleted.", Request.UserHostAddress);
                    transaction.Commit();
                }
                ShowMessage("Custom role deleted.", true);
                RebindRoles();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Role could not be deleted.", ex);
                ShowMessage(ex.Number >= 51010 && ex.Number <= 51012
                    ? "Only unassigned custom roles not referenced by workflow stages can be deleted."
                    : "The role could not be deleted. Please retry or contact support.");
            }
        }

        protected void CancelEditButton_Click(object sender, EventArgs e)
        {
            ResetForm();
        }

        protected void SavePermissionButton_Click(object sender, EventArgs e)
        {
            string key = PermissionKeyText.Text.Trim();
            string description = PermissionDescriptionText.Text.Trim();
            int permissionId;
            bool isEditing = Int32.TryParse(PermissionIdHidden.Value, out permissionId) && permissionId > 0;
            if (!Regex.IsMatch(key, @"\A[a-z][a-z0-9._-]{1,119}\z") || description.Length == 0 || description.Length > 400)
            {
                ShowMessage("Enter a permission key using lowercase letters, digits, dots, underscores or hyphens, and a description up to 400 characters.");
                return;
            }
            if (isEditing && PermissionKeyText.ReadOnly == false)
            {
                ShowMessage("Permission keys cannot be renamed.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                using (SqlCommand command = new SqlCommand(
                    isEditing
                        ? "UPDATE dbo.Permissions SET Description=@Description WHERE PermissionId=@PermissionId;"
                        : "INSERT INTO dbo.Permissions(PermissionKey,Description) VALUES(@PermissionKey,@Description); SELECT CAST(SCOPE_IDENTITY() AS int);",
                    connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@PermissionKey", SqlDbType.NVarChar, key, 120);
                    ModuleDb.AddParameter(command, "@Description", SqlDbType.NVarChar, description, 400);
                    if (isEditing) ModuleDb.AddParameter(command, "@PermissionId", SqlDbType.Int, permissionId);
                    if (isEditing)
                    {
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("The selected permission no longer exists.");
                    }
                    else
                    {
                        permissionId = Convert.ToInt32(command.ExecuteScalar());
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId,
                        isEditing ? "Update" : "Create", "Permission", permissionId, key, Request.UserHostAddress);
                    transaction.Commit();
                }
                ResetPermissionForm();
                RebindRoles();
                ShowMessage("Permission saved.", true);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Permission could not be saved.", ex);
                ShowMessage(ex.Number == 2601 || ex.Number == 2627
                    ? "A permission with that key already exists."
                    : "The permission could not be saved. Please retry or contact support.");
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(ex.Message);
            }
        }

        protected void PermissionsGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int permissionId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out permissionId) || permissionId <= 0)
            {
                ShowMessage("The selected permission is invalid.");
                return;
            }
            if (e.CommandName == "EditPermission")
            {
                LoadPermission(permissionId);
                return;
            }
            if (e.CommandName == "TogglePermission") TogglePermission(permissionId);
            else if (e.CommandName == "DeletePermission") DeletePermission(permissionId);
        }

        private void LoadPermission(int permissionId)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    "SELECT PermissionKey,Description FROM dbo.Permissions WHERE PermissionId=@PermissionId;", connection))
                {
                    ModuleDb.AddParameter(command, "@PermissionId", SqlDbType.Int, permissionId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            ShowMessage("The selected permission no longer exists.");
                            return;
                        }
                        PermissionKeyText.Text = Convert.ToString(reader["PermissionKey"]);
                        PermissionDescriptionText.Text = Convert.ToString(reader["Description"]);
                    }
                }
                PermissionIdHidden.Value = permissionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                PermissionKeyText.ReadOnly = true;
                SavePermissionButton.Text = "Save description";
                CancelPermissionButton.Visible = true;
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Permission details could not be loaded.", ex);
                ShowMessage("The permission could not be loaded. Please retry or contact support.");
            }
        }

        private void TogglePermission(int permissionId)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    string permissionKey;
                    bool isActive;
                    using (SqlCommand command = new SqlCommand(
                        "SELECT PermissionKey,IsActive FROM dbo.Permissions WITH (UPDLOCK,HOLDLOCK) WHERE PermissionId=@PermissionId;",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@PermissionId", SqlDbType.Int, permissionId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowMessage("The selected permission no longer exists.");
                                return;
                            }
                            permissionKey = Convert.ToString(reader["PermissionKey"]);
                            isActive = Convert.ToBoolean(reader["IsActive"]);
                        }
                    }
                    if (IsProtectedPermission(permissionKey))
                    {
                        ShowMessage("Core administration permissions cannot be deactivated.");
                        return;
                    }
                    if (isActive)
                    {
                        using (SqlCommand command = new SqlCommand(
                            "SELECT COUNT(*) FROM dbo.RolePermissions WITH (UPDLOCK,HOLDLOCK) WHERE PermissionId=@PermissionId;",
                            connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@PermissionId", SqlDbType.Int, permissionId);
                            if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                            {
                                ShowMessage("Remove this permission from every role before deactivating it.");
                                return;
                            }
                        }
                    }
                    using (SqlCommand command = new SqlCommand(
                        "UPDATE dbo.Permissions SET IsActive=@IsActive WHERE PermissionId=@PermissionId;",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@IsActive", SqlDbType.Bit, !isActive);
                        ModuleDb.AddParameter(command, "@PermissionId", SqlDbType.Int, permissionId);
                        command.ExecuteNonQuery();
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, isActive ? "Deactivate" : "Activate",
                        "Permission", permissionId, permissionKey, Request.UserHostAddress);
                    transaction.Commit();
                }
                RebindRoles();
                ShowMessage("Permission status updated.", true);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Permission status could not be changed.", ex);
                ShowMessage("The permission status could not be changed. Please retry or contact support.");
            }
        }

        private void DeletePermission(int permissionId)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    string permissionKey;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT PermissionKey FROM dbo.Permissions WITH (UPDLOCK,HOLDLOCK)
                          WHERE PermissionId=@PermissionId AND IsActive=0;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@PermissionId", SqlDbType.Int, permissionId);
                        permissionKey = Convert.ToString(command.ExecuteScalar());
                    }
                    if (String.IsNullOrEmpty(permissionKey) || IsProtectedPermission(permissionKey))
                    {
                        ShowMessage("Only unused, inactive, non-system permissions can be deleted.");
                        return;
                    }
                    using (SqlCommand command = new SqlCommand(
                        @"IF EXISTS(SELECT 1 FROM dbo.RolePermissions WITH (UPDLOCK,HOLDLOCK) WHERE PermissionId=@PermissionId)
                              THROW 51020,'Permission is still assigned to a role.',1;
                          DELETE FROM dbo.Permissions WHERE PermissionId=@PermissionId AND IsActive=0;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@PermissionId", SqlDbType.Int, permissionId);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("The selected permission could not be deleted.");
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Delete", "Permission",
                        permissionId, permissionKey, Request.UserHostAddress);
                    transaction.Commit();
                }
                RebindRoles();
                ShowMessage("Unused permission deleted.", true);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Permission could not be deleted.", ex);
                ShowMessage("The permission could not be deleted. Remove it from role assignments and retry.");
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(ex.Message);
            }
        }

        private static bool IsProtectedPermission(string key)
        {
            return String.Equals(key, "system.admin", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(key, "roles.manage", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(key, "masterdata.manage", StringComparison.OrdinalIgnoreCase);
        }

        protected void CancelPermissionButton_Click(object sender, EventArgs e)
        {
            ResetPermissionForm();
        }

        private void ResetPermissionForm()
        {
            PermissionIdHidden.Value = String.Empty;
            PermissionKeyText.Text = String.Empty;
            PermissionKeyText.ReadOnly = false;
            PermissionDescriptionText.Text = String.Empty;
            SavePermissionButton.Text = "Add permission";
            CancelPermissionButton.Visible = false;
        }

        private void ResetForm()
        {
            RoleIdHidden.Value = String.Empty;
            RoleNameText.Text = RoleDescriptionText.Text = String.Empty;
            FormHeading.Text = "Create custom role";
            SaveRoleButton.Text = "Save role";
            CancelEditButton.Visible = false;
            foreach (ListItem item in PermissionsList.Items) item.Selected = false;
        }

        private void RebindRoles()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    BindPermissions(connection);
                    BindRoles(connection);
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Role catalog could not be refreshed.", ex);
                ShowMessage("Role list could not be refreshed. Please retry or contact support.");
            }
        }

        private void ShowMessage(string text, bool success = false)
        {
            Message.Text = "<div class=\"message" + (success ? " success" : String.Empty) + "\">" +
                System.Web.HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
