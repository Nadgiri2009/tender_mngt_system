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
        protected TextBox FullNameText, EmailText, MobileText, PasswordText;
        protected TextBox ContractorNameText, ContractorRegistrationNoText, ContractorAddressText, ContractorContactText;
        protected DropDownList RoleSelect, DepartmentSelect, ContractorUserSelect;
        protected Button CreateUserButton, CreateContractorButton;
        protected GridView UsersGrid, ContractorsGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Admin")) return;
            if (!IsPostBack) LoadPage();
        }

        protected void Password_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = args.Value != null && args.Value.Length >= 6;
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
                @"SELECT u.UserId,u.FullName,u.Email,u.Mobile,r.RoleName,d.DepartmentName,u.IsActive,u.CreatedDate
                  FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                  LEFT JOIN dbo.Departments d ON d.DepartmentId=u.DepartmentId
                  ORDER BY u.CreatedDate DESC;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
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
                @"SELECT c.Name,c.RegistrationNo,c.Email,c.ContactNo,c.IsActive,u.FullName AS UserName
                  FROM dbo.Contractors c LEFT JOIN dbo.Users u ON u.UserId=c.UserId
                  ORDER BY c.Name;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable data = new DataTable();
                adapter.Fill(data);
                ContractorsGrid.DataSource = data;
                ContractorsGrid.DataBind();
            }
        }

        protected void CreateUserButton_Click(object sender, EventArgs e)
        {
            Page.Validate("User");
            if (!Page.IsValid) return;
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
                using (SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.Users (FullName,Email,Mobile,PasswordHash,RoleId,DepartmentId,IsActive,CreatedDate)
                      VALUES (@FullName,@Email,@Mobile,@PasswordHash,@RoleId,@DepartmentId,1,SYSUTCDATETIME());", connection))
                {
                    ModuleDb.AddParameter(command, "@FullName", SqlDbType.NVarChar, fullName, 150);
                    ModuleDb.AddParameter(command, "@Email", SqlDbType.NVarChar, email, 150);
                    ModuleDb.AddParameter(command, "@Mobile", SqlDbType.NVarChar, String.IsNullOrWhiteSpace(mobile) ? null : mobile, 20);
                    ModuleDb.AddParameter(command, "@PasswordHash", SqlDbType.NVarChar, BCrypt.Net.BCrypt.HashPassword(PasswordText.Text), 500);
                    ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, roleId);
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        optionalDepartmentId.HasValue ? (object)optionalDepartmentId.Value : null);
                    command.ExecuteNonQuery();
                }
                ShowMessage("Staff account created.", true);
                FullNameText.Text = EmailText.Text = MobileText.Text = PasswordText.Text = String.Empty;
                RoleSelect.SelectedIndex = 0;
                DepartmentSelect.SelectedIndex = 0;
                using (SqlConnection connection = ModuleDb.OpenConnection()) BindUsers(connection);
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
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    BindContractorUserOptions(connection);
                    BindContractors(connection);
                }
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
            if (e.CommandName != "ToggleActive") return;
            int userId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out userId) || userId <= 0)
            {
                ShowMessage("The selected user is invalid.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"UPDATE dbo.Users SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END,
                      ModifiedBy=@ModifiedBy,ModifiedDate=SYSUTCDATETIME() WHERE UserId=@UserId;", connection))
                {
                    ModuleDb.AddParameter(command, "@ModifiedBy", SqlDbType.Int, CurrentUserId);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                    if (command.ExecuteNonQuery() != 1)
                    {
                        ShowMessage("The selected user no longer exists.");
                        return;
                    }
                }
                ShowMessage("User access updated.", true);
                using (SqlConnection connection = ModuleDb.OpenConnection()) BindUsers(connection);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "User access could not be updated.", ex);
                ShowMessage("User access could not be updated. Please retry or contact support.");
            }
        }

        private void ShowMessage(string text, bool success = false)
        {
            Message.Text = "<div class=\"message" + (success ? " success" : String.Empty) + "\">" +
                System.Web.HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
