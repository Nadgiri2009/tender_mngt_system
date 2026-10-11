using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class AdminDepartments : ModulePage
    {
        protected Literal Message, FormHeading;
        protected HiddenField DepartmentIdHidden;
        protected TextBox DepartmentNameText, DepartmentCodeText, HeadOfficerText;
        protected CheckBox DepartmentActive;
        protected Button SaveDepartmentButton, CancelEditButton;
        protected GridView DepartmentsGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Admin")) return;
            if (!IsPostBack) BindDepartments();
        }

        private void BindDepartments()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    "SELECT DepartmentId,DepartmentName,DepartmentCode,HeadOfficerName,IsActive FROM dbo.Departments ORDER BY DepartmentName;", connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable data = new DataTable();
                    adapter.Fill(data);
                    DepartmentsGrid.DataSource = data;
                    DepartmentsGrid.DataBind();
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Department list could not be loaded.", ex);
                ShowMessage("Department records could not be loaded. Please retry or contact support.");
            }
        }

        protected void SaveDepartmentButton_Click(object sender, EventArgs e)
        {
            Page.Validate("Department");
            if (!Page.IsValid) return;
            string name = DepartmentNameText.Text.Trim();
            string code = DepartmentCodeText.Text.Trim();
            string head = HeadOfficerText.Text.Trim();
            if (name.Length > 150 || code.Length > 20 || head.Length > 150)
            {
                ShowMessage("Department name, code, or head officer exceeds the allowed length.");
                return;
            }

            int departmentId;
            if (!String.IsNullOrEmpty(DepartmentIdHidden.Value) &&
                !Int32.TryParse(DepartmentIdHidden.Value, NumberStyles.None, CultureInfo.InvariantCulture, out departmentId))
            {
                ShowMessage("The selected department is invalid.");
                return;
            }
            departmentId = Int32.TryParse(DepartmentIdHidden.Value, out departmentId) ? departmentId : 0;

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    using (SqlCommand duplicate = new SqlCommand(
                        @"SELECT COUNT(*) FROM dbo.Departments
                          WHERE DepartmentName=@Name AND DepartmentId<>@DepartmentId;", connection))
                    {
                        ModuleDb.AddParameter(duplicate, "@Name", SqlDbType.NVarChar, name, 150);
                        ModuleDb.AddParameter(duplicate, "@DepartmentId", SqlDbType.Int, departmentId);
                        if (Convert.ToInt32(duplicate.ExecuteScalar()) > 0)
                        {
                            ShowMessage("A department with this name already exists.");
                            return;
                        }
                    }
                    if (code.Length > 0)
                    {
                        using (SqlCommand duplicate = new SqlCommand(
                            @"SELECT COUNT(*) FROM dbo.Departments
                              WHERE DepartmentCode=@Code AND DepartmentId<>@DepartmentId;", connection))
                        {
                            ModuleDb.AddParameter(duplicate, "@Code", SqlDbType.NVarChar, code, 20);
                            ModuleDb.AddParameter(duplicate, "@DepartmentId", SqlDbType.Int, departmentId);
                            if (Convert.ToInt32(duplicate.ExecuteScalar()) > 0)
                            {
                                ShowMessage("A department with this code already exists.");
                                return;
                            }
                        }
                    }

                    string sql = departmentId == 0
                        ? @"INSERT INTO dbo.Departments (DepartmentName,DepartmentCode,HeadOfficerName,IsActive)
                            VALUES (@Name,@Code,@Head,@IsActive);"
                        : @"UPDATE dbo.Departments SET DepartmentName=@Name,DepartmentCode=@Code,
                            HeadOfficerName=@Head,IsActive=@IsActive WHERE DepartmentId=@DepartmentId;";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        AddDepartmentParameters(command, name, code, head, departmentId);
                        ModuleDb.AddParameter(command, "@IsActive", SqlDbType.Bit, DepartmentActive.Checked);
                        if (command.ExecuteNonQuery() != 1)
                        {
                            ShowMessage("The selected department could not be found.");
                            return;
                        }
                    }
                }
                ResetForm();
                ShowMessage("Department saved.", true);
                BindDepartments();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Department could not be saved.", ex);
                ShowMessage(ex.Number == 2601 || ex.Number == 2627
                    ? "A department with this name or code already exists."
                    : "Department could not be saved. Please retry or contact support.");
            }
        }

        protected void DepartmentsGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int departmentId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out departmentId) || departmentId <= 0)
            {
                ShowMessage("The selected department is invalid.");
                return;
            }
            if (e.CommandName == "EditDepartment")
            {
                try
                {
                    using (SqlConnection connection = ModuleDb.OpenConnection())
                    using (SqlCommand command = new SqlCommand(
                        "SELECT DepartmentId,DepartmentName,DepartmentCode,HeadOfficerName,IsActive FROM dbo.Departments WHERE DepartmentId=@DepartmentId;", connection))
                    {
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read()) { ShowMessage("The selected department no longer exists."); return; }
                            DepartmentIdHidden.Value = departmentId.ToString(CultureInfo.InvariantCulture);
                            DepartmentNameText.Text = Convert.ToString(reader["DepartmentName"]);
                            DepartmentCodeText.Text = reader["DepartmentCode"] == DBNull.Value ? String.Empty : Convert.ToString(reader["DepartmentCode"]);
                            HeadOfficerText.Text = reader["HeadOfficerName"] == DBNull.Value ? String.Empty : Convert.ToString(reader["HeadOfficerName"]);
                            DepartmentActive.Checked = Convert.ToBoolean(reader["IsActive"]);
                            FormHeading.Text = "Edit department";
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Trace.Warn("DepartmentModule", "Department could not be opened for editing.", ex);
                    ShowMessage("Department details could not be loaded.");
                }
                return;
            }
            if (e.CommandName != "DisableDepartment") return;

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    "UPDATE dbo.Departments SET IsActive=0 WHERE DepartmentId=@DepartmentId;", connection))
                {
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                    if (command.ExecuteNonQuery() != 1) { ShowMessage("The selected department no longer exists."); return; }
                }
                ShowMessage("Department disabled; existing tender and staff records remain unchanged.", true);
                BindDepartments();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Department could not be disabled.", ex);
                ShowMessage("Department could not be disabled. Please retry or contact support.");
            }
        }

        protected void CancelEditButton_Click(object sender, EventArgs e) { ResetForm(); }

        private static void AddDepartmentParameters(SqlCommand command, string name, string code, string head, int departmentId)
        {
            ModuleDb.AddParameter(command, "@Name", SqlDbType.NVarChar, name, 150);
            ModuleDb.AddParameter(command, "@Code", SqlDbType.NVarChar, String.IsNullOrWhiteSpace(code) ? null : code, 20);
            ModuleDb.AddParameter(command, "@Head", SqlDbType.NVarChar, String.IsNullOrWhiteSpace(head) ? null : head, 150);
            if (departmentId > 0) ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
        }

        private void ResetForm()
        {
            DepartmentIdHidden.Value = String.Empty;
            DepartmentNameText.Text = DepartmentCodeText.Text = HeadOfficerText.Text = String.Empty;
            DepartmentActive.Checked = true;
            FormHeading.Text = "Add department";
        }

        private void ShowMessage(string text, bool success = false)
        {
            Message.Text = "<div class=\"message" + (success ? " success" : String.Empty) + "\">" +
                System.Web.HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
