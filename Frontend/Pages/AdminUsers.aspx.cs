using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class AdminUsers : ModulePage
    {
        protected Literal Message;
        protected Literal UserFormHeading;
        protected HiddenField UserIdHidden;
        protected TextBox FullNameText, EmailText, MobileText, PasswordText;
        protected TextBox UserSearchText, ContractorSearchText;
        protected TextBox ContractorNameText, ContractorRegistrationNoText, ContractorAddressText, ContractorContactText;
        protected DropDownList RoleSelect, DepartmentSelect, ContractorUserSelect, UserStatusFilter, ContractorStatusFilter;
        protected Button CreateUserButton, UpdateUserButton, CancelUserEditButton, CreateContractorButton,
            FilterUsersButton, FilterContractorsButton;
        protected GridView UsersGrid, ContractorsGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Admin")) return;
            if (!IsPostBack) LoadPage();
        }

        protected void Password_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = String.IsNullOrEmpty(args.Value) || args.Value.Length >= 6;
        }

        private void LoadPage()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    using (SqlCommand command = new SqlCommand("SELECT RoleId,RoleName FROM dbo.Roles WHERE IsActive=1 ORDER BY RoleName;", connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        RoleSelect.Items.Clear();
                        RoleSelect.Items.Add(new ListItem("Select role", ""));
                        while (reader.Read())
                            RoleSelect.Items.Add(new ListItem(reader.GetString(1), reader.GetInt32(0).ToString()));
                    }
                    using (SqlCommand command = new SqlCommand("SELECT DepartmentId,DepartmentName FROM dbo.Departments WHERE IsActive=1 ORDER BY DepartmentName;", connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        DepartmentSelect.Items.Clear();
                        DepartmentSelect.Items.Add(new ListItem("No department", ""));
                        while (reader.Read())
                            DepartmentSelect.Items.Add(new ListItem(reader.GetString(1), reader.GetInt32(0).ToString()));
                    }
                    BindUsers(connection);
                    BindContractorUserOptions(connection);
                    BindContractors(connection);
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "User administration data could not be loaded.", ex);
                Message.Text = "<div class=\"message\">User records could not be loaded. Please retry or contact support.</div>";
                CreateUserButton.Enabled = false;
            }
        }

        private void BindUsers(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT u.UserId,u.FullName,u.Email,u.Mobile,r.RoleName,d.DepartmentName,u.IsActive,u.CreatedDate,
                         lastLogin.LastLogin
                  FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                  LEFT JOIN dbo.Departments d ON d.DepartmentId=u.DepartmentId
                  OUTER APPLY (
                      SELECT MAX(a.CreatedDate) AS LastLogin
                      FROM dbo.AuditLog a
                      WHERE a.UserId=u.UserId AND a.Action=N'Login' AND a.EntityName=N'User'
                  ) lastLogin
                  WHERE (@Search=N'' OR u.FullName LIKE @SearchPattern OR u.Email LIKE @SearchPattern
                         OR u.Mobile LIKE @SearchPattern)
                    AND (@IsActive IS NULL OR u.IsActive=@IsActive)
                  ORDER BY u.CreatedDate DESC,u.UserId DESC;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                AddUserFilterParameters(command);
                DataTable data = new DataTable();
                adapter.Fill(data);
                UsersGrid.DataSource = data;
                UsersGrid.DataBind();
            }
        }

        private void BindContractorUserOptions(SqlConnection connection)
        {
            ContractorUserSelect.Items.Clear();
            ContractorUserSelect.Items.Add(new ListItem("Select contractor login", ""));
            using (SqlCommand command = new SqlCommand(
                @"SELECT u.UserId,u.FullName,u.Email
                  FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                  WHERE r.RoleName=N'Contractor' AND u.IsActive=1
                    AND NOT EXISTS (SELECT 1 FROM dbo.Contractors c WHERE c.UserId=u.UserId)
                  ORDER BY u.FullName;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    ContractorUserSelect.Items.Add(new ListItem(
                        Convert.ToString(reader["FullName"]) + " (" + Convert.ToString(reader["Email"]) + ")",
                        Convert.ToString(reader["UserId"], System.Globalization.CultureInfo.InvariantCulture)));
                }
            }
            CreateContractorButton.Enabled = ContractorUserSelect.Items.Count > 1;
        }

        private void BindContractors(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT c.ContractorId,c.Name,c.RegistrationNo,c.Email,c.ContactNo,c.IsActive,u.FullName AS UserName
                  FROM dbo.Contractors c LEFT JOIN dbo.Users u ON u.UserId=c.UserId
                  WHERE (@Search=N'' OR c.Name LIKE @SearchPattern OR c.RegistrationNo LIKE @SearchPattern
                         OR c.ContactNo LIKE @SearchPattern OR c.Email LIKE @SearchPattern)
                    AND (@IsActive IS NULL OR c.IsActive=@IsActive)
                  ORDER BY c.Name,c.ContractorId;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                AddContractorFilterParameters(command);
                DataTable data = new DataTable();
                adapter.Fill(data);
                ContractorsGrid.DataSource = data;
                ContractorsGrid.DataBind();
            }
        }

        private void RebindUsers()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection()) BindUsers(connection);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "User list could not be refreshed.", ex);
                ShowMessage("The user list could not be refreshed. Please retry or contact support.");
            }
        }

        private void RebindContractors()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection()) BindContractors(connection);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Contractor list could not be refreshed.", ex);
                ShowMessage("The contractor list could not be refreshed. Please retry or contact support.");
            }
        }

        private void RebindContractorOptions()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection()) BindContractorUserOptions(connection);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Eligible contractor accounts could not be refreshed.", ex);
                ShowMessage("Eligible contractor accounts could not be refreshed. Please retry or contact support.");
            }
        }

        private void AddUserFilterParameters(SqlCommand command)
        {
            string search = UserSearchText.Text == null ? String.Empty : UserSearchText.Text.Trim();
            AddText(command, "@Search", SqlDbType.NVarChar, search, 100);
            AddText(command, "@SearchPattern", SqlDbType.NVarChar, "%" + search + "%", 102);
            ModuleDb.AddParameter(command, "@IsActive", SqlDbType.Bit,
                UserStatusFilter.SelectedValue == "1" ? (object)true :
                UserStatusFilter.SelectedValue == "0" ? (object)false : null);
        }

        private void AddContractorFilterParameters(SqlCommand command)
        {
            string search = ContractorSearchText.Text == null ? String.Empty : ContractorSearchText.Text.Trim();
            AddText(command, "@Search", SqlDbType.NVarChar, search, 100);
            AddText(command, "@SearchPattern", SqlDbType.NVarChar, "%" + search + "%", 102);
            ModuleDb.AddParameter(command, "@IsActive", SqlDbType.Bit,
                ContractorStatusFilter.SelectedValue == "1" ? (object)true :
                ContractorStatusFilter.SelectedValue == "0" ? (object)false : null);
        }

        protected void UpdateUserButton_Click(object sender, EventArgs e)
        {
            Page.Validate("User");
            if (!Page.IsValid) return;

            int userId, roleId, departmentId;
            if (!Int32.TryParse(UserIdHidden.Value, out userId) || userId <= 0 ||
                !Int32.TryParse(RoleSelect.SelectedValue, out roleId) || roleId <= 0 ||
                (!String.IsNullOrEmpty(DepartmentSelect.SelectedValue) &&
                 (!Int32.TryParse(DepartmentSelect.SelectedValue, out departmentId) || departmentId <= 0)))
            {
                ShowMessage("The selected user, role, or department is invalid.");
                return;
            }

            string fullName = FullNameText.Text.Trim();
            string email = EmailText.Text.Trim();
            string mobile = MobileText.Text.Trim();
            if (fullName.Length < 2 || fullName.Length > 150 ||
                !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ||
                (mobile.Length > 0 && !Regex.IsMatch(mobile, @"^[+0-9() .-]{1,20}$")) ||
                (!String.IsNullOrEmpty(PasswordText.Text) && PasswordText.Text.Length < 6))
            {
                ShowMessage("Enter a valid name, email address, mobile number, and optional password of at least 6 characters.");
                return;
            }

            int? optionalDepartmentId = Int32.TryParse(DepartmentSelect.SelectedValue, out departmentId) && departmentId > 0
                ? (int?)departmentId : null;

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    int currentRoleId;
                    bool isActive;
                    string currentMobile;
                    string currentRole;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT u.RoleId,u.IsActive,u.Mobile,r.RoleName
                          FROM dbo.Users u WITH (UPDLOCK,HOLDLOCK)
                          INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                          WHERE u.UserId=@UserId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowMessage("The selected user no longer exists.");
                                return;
                            }
                            currentRoleId = Convert.ToInt32(reader["RoleId"]);
                            isActive = Convert.ToBoolean(reader["IsActive"]);
                            currentMobile = reader["Mobile"] == DBNull.Value ? String.Empty : Convert.ToString(reader["Mobile"]);
                            currentRole = Convert.ToString(reader["RoleName"]);
                        }
                    }

                    if (userId == CurrentUserId && roleId != currentRoleId)
                    {
                        ShowMessage("Administrators cannot change their own role.");
                        return;
                    }
                    if (!String.Equals(currentMobile.Trim(), mobile.Trim(), StringComparison.Ordinal))
                    {
                        ShowMessage("Mobile-number changes are disabled until a trusted OTP identity-verification service is configured.");
                        return;
                    }
                    if (isActive && String.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                        roleId != currentRoleId && GetActiveAdministratorCount(connection, transaction, userId) == 0)
                    {
                        ShowMessage("The last active administrator cannot be removed from the Main Administrator role.");
                        return;
                    }

                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.Users
                          SET FullName=@FullName,Email=@Email,RoleId=@RoleId,DepartmentId=@DepartmentId,
                              PasswordHash=CASE WHEN @PasswordHash IS NULL THEN PasswordHash ELSE @PasswordHash END,
                              ModifiedBy=@ModifiedBy,ModifiedDate=SYSUTCDATETIME()
                          WHERE UserId=@UserId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@FullName", SqlDbType.NVarChar, fullName, 150);
                        ModuleDb.AddParameter(command, "@Email", SqlDbType.NVarChar, email, 150);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                            optionalDepartmentId.HasValue ? (object)optionalDepartmentId.Value : null);
                        ModuleDb.AddParameter(command, "@PasswordHash", SqlDbType.NVarChar,
                            String.IsNullOrEmpty(PasswordText.Text) ? null : BCrypt.Net.BCrypt.HashPassword(PasswordText.Text), 500);
                        ModuleDb.AddParameter(command, "@ModifiedBy", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                        if (command.ExecuteNonQuery() != 1)
                        {
                            ShowMessage("The selected user could not be updated.");
                            return;
                        }
                    }

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Update", "User", userId,
                        "Profile and access settings updated; no credentials recorded.", Request.UserHostAddress);
                    transaction.Commit();
                }

                ResetUserForm();
                ShowMessage("User account updated.", true);
                RebindUsers();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "User account update failed.", ex);
                ShowMessage(ex.Number == 2601 || ex.Number == 2627
                    ? "A user with this email already exists."
                    : "The user account could not be updated. Please retry or contact support.");
            }
        }

        protected void CancelUserEditButton_Click(object sender, EventArgs e)
        {
            ResetUserForm();
        }

        private void ResetUserForm()
        {
            UserIdHidden.Value = String.Empty;
            UserFormHeading.Text = "Create staff account";
            FullNameText.Text = EmailText.Text = MobileText.Text = PasswordText.Text = String.Empty;
            RoleSelect.SelectedIndex = 0;
            DepartmentSelect.SelectedIndex = 0;
            CreateUserButton.Visible = true;
            UpdateUserButton.Visible = false;
            CancelUserEditButton.Visible = false;
        }

        private static int GetActiveAdministratorCount(SqlConnection connection, SqlTransaction transaction, int exceptUserId)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT COUNT(*) FROM dbo.Users u WITH (UPDLOCK,HOLDLOCK)
                  INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                  WHERE r.RoleName=N'Admin' AND u.IsActive=1 AND u.UserId<>@UserId;", connection, transaction))
            {
                ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, exceptUserId);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        protected void CreateUserButton_Click(object sender, EventArgs e)
        {
            Page.Validate("User");
            if (!Page.IsValid) return;
            if (String.IsNullOrEmpty(PasswordText.Text) || PasswordText.Text.Length < 6)
            {
                ShowMessage("Enter a temporary password with at least 6 characters.");
                return;
            }
            string fullName = FullNameText.Text.Trim();
            string email = EmailText.Text.Trim();
            string mobile = MobileText.Text.Trim();
            if (fullName.Length < 2 || fullName.Length > 150 ||
                !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ||
                (mobile.Length > 0 && !Regex.IsMatch(mobile, @"^[+0-9() .-]{1,20}$")))
            {
                ShowMessage("Enter a valid name, email address, and mobile number.");
                return;
            }

            int roleId;
            int departmentId;
            if (!Int32.TryParse(RoleSelect.SelectedValue, out roleId) || roleId <= 0 ||
                (!String.IsNullOrEmpty(DepartmentSelect.SelectedValue) &&
                 (!Int32.TryParse(DepartmentSelect.SelectedValue, out departmentId) || departmentId <= 0)))
            {
                ShowMessage("Select a valid role and department.");
                return;
            }
            int? optionalDepartmentId = Int32.TryParse(DepartmentSelect.SelectedValue, out departmentId) && departmentId > 0
                ? (int?)departmentId : null;

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    int userId;
                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.Users (FullName,Email,Mobile,PasswordHash,RoleId,DepartmentId,IsActive,CreatedDate,CreatedBy)
                          VALUES (@FullName,@Email,@Mobile,@PasswordHash,@RoleId,@DepartmentId,1,SYSUTCDATETIME(),@CreatedBy);
                          SELECT CAST(SCOPE_IDENTITY() AS int);", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@FullName", SqlDbType.NVarChar, fullName, 150);
                        ModuleDb.AddParameter(command, "@Email", SqlDbType.NVarChar, email, 150);
                        ModuleDb.AddParameter(command, "@Mobile", SqlDbType.NVarChar, String.IsNullOrWhiteSpace(mobile) ? null : mobile, 20);
                        ModuleDb.AddParameter(command, "@PasswordHash", SqlDbType.NVarChar, BCrypt.Net.BCrypt.HashPassword(PasswordText.Text), 500);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                            optionalDepartmentId.HasValue ? (object)optionalDepartmentId.Value : null);
                        ModuleDb.AddParameter(command, "@CreatedBy", SqlDbType.Int, CurrentUserId);
                        userId = Convert.ToInt32(command.ExecuteScalar());
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Create", "User", userId,
                        "Account created by administrator.", Request.UserHostAddress);
                    transaction.Commit();
                }
                ShowMessage("Staff account created.", true);
                FullNameText.Text = EmailText.Text = MobileText.Text = PasswordText.Text = String.Empty;
                RoleSelect.SelectedIndex = 0;
                DepartmentSelect.SelectedIndex = 0;
                RebindUsers();
                RebindContractorOptions();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Staff account creation failed.", ex);
                ShowMessage(ex.Number == 2601 || ex.Number == 2627
                    ? "A user with this email already exists."
                    : "The account could not be created. Please retry or contact support.");
            }
        }

        protected void CreateContractorButton_Click(object sender, EventArgs e)
        {
            string name = ContractorNameText.Text.Trim();
            string registrationNo = ContractorRegistrationNoText.Text.Trim();
            string address = ContractorAddressText.Text.Trim();
            string contact = ContractorContactText.Text.Trim();
            int userId;

            if (name.Length < 2 || name.Length > 150 || registrationNo.Length == 0 || registrationNo.Length > 100 ||
                address.Length > 300 || contact.Length > 20 ||
                (contact.Length > 0 && !Regex.IsMatch(contact, @"^[+0-9() .-]+$")) ||
                !Int32.TryParse(ContractorUserSelect.SelectedValue, out userId) || userId <= 0)
            {
                ShowMessage("Select an eligible contractor login and enter valid name, registration number, address, and contact details.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    int emailRows;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT COUNT(1) FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                          WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=N'Contractor'
                            AND NOT EXISTS (SELECT 1 FROM dbo.Contractors c WHERE c.UserId=u.UserId);",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                        emailRows = Convert.ToInt32(command.ExecuteScalar());
                    }

                    if (emailRows != 1)
                    {
                        ShowMessage("That login is no longer eligible for a contractor profile. Refresh and try again.");
                        return;
                    }

                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.Contractors (Name,RegistrationNo,Address,ContactNo,Email,UserId,IsActive,CreatedDate)
                          SELECT @Name,@RegistrationNo,@Address,@ContactNo,u.Email,u.UserId,1,SYSUTCDATETIME()
                          FROM dbo.Users u WHERE u.UserId=@UserId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@Name", SqlDbType.NVarChar, name, 150);
                        ModuleDb.AddParameter(command, "@RegistrationNo", SqlDbType.NVarChar, registrationNo, 100);
                        ModuleDb.AddParameter(command, "@Address", SqlDbType.NVarChar, String.IsNullOrWhiteSpace(address) ? null : address, 300);
                        ModuleDb.AddParameter(command, "@ContactNo", SqlDbType.NVarChar, String.IsNullOrWhiteSpace(contact) ? null : contact, 20);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                        if (command.ExecuteNonQuery() != 1)
                        {
                            ShowMessage("The contractor login could not be linked.");
                            return;
                        }
                    }

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Create", "Contractor", userId, registrationNo, Request.UserHostAddress);
                    transaction.Commit();
                }

                ContractorNameText.Text = ContractorRegistrationNoText.Text = ContractorAddressText.Text = ContractorContactText.Text = String.Empty;
                ShowMessage("Contractor profile created and linked to the login.", true);
                RebindContractorOptions();
                RebindContractors();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Contractor profile creation failed.", ex);
                ShowMessage(ex.Number == 2601 || ex.Number == 2627
                    ? "That contractor registration number or login is already linked."
                    : "The contractor profile could not be saved. Please retry or contact support.");
            }
        }

        protected void UsersGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int userId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out userId) || userId <= 0)
            {
                ShowMessage("The selected user is invalid.");
                return;
            }

            if (e.CommandName == "EditUser")
            {
                LoadUserForEdit(userId);
                return;
            }
            if (e.CommandName != "ToggleActive") return;
            if (userId == CurrentUserId)
            {
                ShowMessage("You cannot change your own account status.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    bool isActive;
                    string roleName;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT u.IsActive,r.RoleName FROM dbo.Users u WITH (UPDLOCK,HOLDLOCK)
                          INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId WHERE u.UserId=@UserId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowMessage("The selected user no longer exists.");
                                return;
                            }
                            isActive = Convert.ToBoolean(reader["IsActive"]);
                            roleName = Convert.ToString(reader["RoleName"]);
                        }
                    }

                    if (isActive && String.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) &&
                        GetActiveAdministratorCount(connection, transaction, userId) == 0)
                    {
                        ShowMessage("The last active administrator account cannot be deactivated.");
                        return;
                    }

                    bool newStatus = !isActive;
                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.Users SET IsActive=@IsActive,ModifiedBy=@ModifiedBy,
                          ModifiedDate=SYSUTCDATETIME() WHERE UserId=@UserId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@IsActive", SqlDbType.Bit, newStatus);
                        ModuleDb.AddParameter(command, "@ModifiedBy", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                        if (command.ExecuteNonQuery() != 1)
                        {
                            ShowMessage("The selected user no longer exists.");
                            return;
                        }
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId,
                        newStatus ? "Activate" : "Deactivate", "User", userId,
                        "Account status changed by administrator.", Request.UserHostAddress);
                    transaction.Commit();
                }

                ShowMessage("User access updated.", true);
                RebindUsers();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "User access could not be updated.", ex);
                ShowMessage("User access could not be updated. Please retry or contact support.");
            }
        }

        private void LoadUserForEdit(int userId)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"SELECT UserId,FullName,Email,Mobile,RoleId,DepartmentId
                      FROM dbo.Users WHERE UserId=@UserId;", connection))
                {
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            ShowMessage("The selected user no longer exists.");
                            return;
                        }
                        string roleId = Convert.ToString(reader["RoleId"]);
                        if (RoleSelect.Items.FindByValue(roleId) == null)
                        {
                            ShowMessage("This user's role is inactive and cannot be assigned for editing. Reactivate the role first.");
                            return;
                        }

                        UserIdHidden.Value = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        FullNameText.Text = Convert.ToString(reader["FullName"]);
                        EmailText.Text = Convert.ToString(reader["Email"]);
                        MobileText.Text = reader["Mobile"] == DBNull.Value ? String.Empty : Convert.ToString(reader["Mobile"]);
                        RoleSelect.SelectedValue = roleId;
                        string departmentId = reader["DepartmentId"] == DBNull.Value
                            ? String.Empty : Convert.ToString(reader["DepartmentId"]);
                        if (!String.IsNullOrEmpty(departmentId) && DepartmentSelect.Items.FindByValue(departmentId) == null)
                            DepartmentSelect.Items.Add(new ListItem("Inactive department (currently assigned)", departmentId));
                        DepartmentSelect.SelectedValue = departmentId;
                        PasswordText.Text = String.Empty;
                        UserFormHeading.Text = "Edit user account";
                        CreateUserButton.Visible = false;
                        UpdateUserButton.Visible = true;
                        CancelUserEditButton.Visible = true;
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "User details could not be loaded for editing.", ex);
                ShowMessage("User details could not be loaded. Please retry or contact support.");
            }
        }

        protected void ContractorsGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ToggleContractor") return;
            int contractorId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out contractorId) || contractorId <= 0)
            {
                ShowMessage("The selected contractor profile is invalid.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    bool isActive;
                    using (SqlCommand command = new SqlCommand(
                        "SELECT IsActive FROM dbo.Contractors WITH (UPDLOCK,HOLDLOCK) WHERE ContractorId=@ContractorId;",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                        object value = command.ExecuteScalar();
                        if (value == null || value == DBNull.Value)
                        {
                            ShowMessage("The selected contractor profile no longer exists.");
                            return;
                        }
                        isActive = Convert.ToBoolean(value);
                    }

                    bool newStatus = !isActive;
                    using (SqlCommand command = new SqlCommand(
                        "UPDATE dbo.Contractors SET IsActive=@IsActive WHERE ContractorId=@ContractorId;",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@IsActive", SqlDbType.Bit, newStatus);
                        ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                        if (command.ExecuteNonQuery() != 1)
                        {
                            ShowMessage("The selected contractor profile no longer exists.");
                            return;
                        }
                    }

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId,
                        newStatus ? "Activate" : "Deactivate", "Contractor", contractorId,
                        "Contractor profile status changed by administrator.", Request.UserHostAddress);
                    transaction.Commit();
                }
                ShowMessage("Contractor profile status updated.", true);
                RebindContractors();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Contractor profile status could not be updated.", ex);
                ShowMessage("Contractor profile status could not be updated. Please retry or contact support.");
            }
        }

        protected void FilterUsersButton_Click(object sender, EventArgs e)
        {
            UsersGrid.PageIndex = 0;
            RebindUsers();
        }

        protected void FilterContractorsButton_Click(object sender, EventArgs e)
        {
            ContractorsGrid.PageIndex = 0;
            RebindContractors();
        }

        protected void UsersGrid_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            UsersGrid.PageIndex = e.NewPageIndex;
            RebindUsers();
        }

        protected void ContractorsGrid_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            ContractorsGrid.PageIndex = e.NewPageIndex;
            RebindContractors();
        }

        private void ShowMessage(string text, bool success = false)
        {
            Message.Text = "<div class=\"message" + (success ? " success" : String.Empty) + "\">" +
                System.Web.HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
