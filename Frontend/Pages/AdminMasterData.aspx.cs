using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class AdminMasterData : ModulePage
    {
        protected Literal Message;
        protected DropDownList CatalogSelect;
        protected HiddenField OriginalValue;
        protected Label ValueLabel;
        protected TextBox ValueText, SortOrderText;
        protected CheckBox IsActiveCheck;
        protected Button SaveButton, CancelButton;
        protected GridView CatalogGrid;

        private sealed class CatalogDefinition
        {
            public string TableName;
            public string KeyColumn;
            public int MaxLength;
            public string UsageQuery;
            public string DisplayName;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequirePermission("masterdata.manage")) return;
            if (!IsPostBack)
            {
                CatalogSelect.Items.Add(new ListItem("Tender categories", "categories"));
                CatalogSelect.Items.Add(new ListItem("Tender methods", "methods"));
                CatalogSelect.Items.Add(new ListItem("Document types", "documents"));
                CatalogSelect.Items.Add(new ListItem("Units", "units"));
                CatalogSelect.Items.Add(new ListItem("Officer designations", "designations"));
                BindCatalog();
            }
        }

        protected void CatalogSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetForm();
            BindCatalog();
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            Page.Validate("Catalog");
            string value = ValueText.Text.Trim();
            int sortOrder;
            CatalogDefinition catalog = GetCatalog();
            if (!Page.IsValid || value.Length == 0 || value.Length > catalog.MaxLength ||
                !Int32.TryParse(SortOrderText.Text, NumberStyles.None, CultureInfo.InvariantCulture, out sortOrder) ||
                sortOrder < 0 || sortOrder > 32767)
            {
                ShowMessage("Enter a name within the displayed catalog limit and a display order from 0 to 32767.");
                return;
            }

            bool isEditing = !String.IsNullOrEmpty(OriginalValue.Value);
            if (isEditing && !String.Equals(value, OriginalValue.Value, StringComparison.Ordinal))
            {
                ShowMessage("Catalog names cannot be renamed. Add a new value and retain the existing value for historical records.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                using (SqlCommand command = new SqlCommand(
                    isEditing
                        ? "UPDATE " + catalog.TableName + " SET SortOrder=@SortOrder,IsActive=@IsActive WHERE " + catalog.KeyColumn + "=@Value;"
                        : "INSERT INTO " + catalog.TableName + "(" + catalog.KeyColumn + ",SortOrder,IsActive) VALUES(@Value,@SortOrder,@IsActive);",
                    connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@Value", SqlDbType.NVarChar, value, catalog.MaxLength);
                    ModuleDb.AddParameter(command, "@SortOrder", SqlDbType.Int, sortOrder);
                    ModuleDb.AddParameter(command, "@IsActive", SqlDbType.Bit, IsActiveCheck.Checked);
                    int affected = command.ExecuteNonQuery();
                    if (affected != 1) throw new InvalidOperationException("The catalog entry was changed by another administrator. Refresh and retry.");

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId,
                        isEditing ? "Update" : "Create", "MasterData", null,
                        catalog.DisplayName + ": " + value, Request.UserHostAddress);
                    transaction.Commit();
                }

                ResetForm();
                BindCatalog();
                ShowMessage("Catalog entry saved.", true);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Master data item could not be saved.", ex);
                ShowMessage(ex.Number == 2601 || ex.Number == 2627
                    ? "An entry with that name already exists."
                    : "The catalog entry could not be saved. Please retry or contact support.");
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(ex.Message);
            }
        }

        protected void CatalogGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            CatalogDefinition catalog = GetCatalog();
            string value = Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture);
            if (String.IsNullOrWhiteSpace(value)) return;

            if (e.CommandName == "EditCatalogItem")
            {
                LoadCatalogItem(catalog, value);
                return;
            }
            if (e.CommandName != "DeleteCatalogItem") return;

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (SqlCommand command = new SqlCommand(catalog.UsageQuery, connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@Value", SqlDbType.NVarChar, value, catalog.MaxLength);
                        if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
                        {
                            ShowMessage("This entry is used by existing records and cannot be deleted. Deactivate it instead.");
                            transaction.Rollback();
                            return;
                        }
                    }
                    using (SqlCommand command = new SqlCommand(
                        "DELETE FROM " + catalog.TableName + " WHERE " + catalog.KeyColumn + "=@Value;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@Value", SqlDbType.NVarChar, value, catalog.MaxLength);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("The catalog entry was changed by another administrator. Refresh and retry.");
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Delete", "MasterData", null,
                        catalog.DisplayName + ": " + value, Request.UserHostAddress);
                    transaction.Commit();
                }
                BindCatalog();
                ShowMessage("Unused catalog entry deleted.", true);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Master data item could not be deleted.", ex);
                ShowMessage("The catalog entry could not be deleted. It may still be referenced; refresh and retry.");
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(ex.Message);
            }
        }

        protected void CancelButton_Click(object sender, EventArgs e)
        {
            ResetForm();
            BindCatalog();
        }

        private void LoadCatalogItem(CatalogDefinition catalog, string value)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    "SELECT " + catalog.KeyColumn + ",SortOrder,IsActive FROM " + catalog.TableName +
                    " WHERE " + catalog.KeyColumn + "=@Value;", connection))
                {
                    ModuleDb.AddParameter(command, "@Value", SqlDbType.NVarChar, value, catalog.MaxLength);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            ShowMessage("The selected entry no longer exists.");
                            return;
                        }
                        OriginalValue.Value = Convert.ToString(reader[catalog.KeyColumn], CultureInfo.InvariantCulture);
                        ValueText.Text = OriginalValue.Value;
                        SortOrderText.Text = Convert.ToString(reader["SortOrder"], CultureInfo.InvariantCulture);
                        IsActiveCheck.Checked = Convert.ToBoolean(reader["IsActive"], CultureInfo.InvariantCulture);
                    }
                }
                ValueText.ReadOnly = true;
                ValueLabel.Text = catalog.DisplayName;
                SaveButton.Text = "Save changes";
                CancelButton.Visible = true;
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Master data item could not be loaded.", ex);
                ShowMessage("The selected catalog entry could not be loaded. Please retry or contact support.");
            }
        }

        private void BindCatalog()
        {
            CatalogDefinition catalog = GetCatalog();
            try
            {
                DataTable data = new DataTable();
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    "SELECT " + catalog.KeyColumn + " AS CatalogValue,SortOrder,IsActive FROM " +
                    catalog.TableName + " ORDER BY SortOrder," + catalog.KeyColumn + ";", connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    adapter.Fill(data);

                data.Columns.Add("UsageCount", typeof(int));
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    foreach (DataRow row in data.Rows)
                    {
                        using (SqlCommand command = new SqlCommand(catalog.UsageQuery, connection))
                        {
                            ModuleDb.AddParameter(command, "@Value", SqlDbType.NVarChar,
                                Convert.ToString(row["CatalogValue"], CultureInfo.InvariantCulture), catalog.MaxLength);
                            row["UsageCount"] = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                        }
                    }
                }
                CatalogGrid.DataSource = data;
                CatalogGrid.DataBind();
                ValueLabel.Text = catalog.DisplayName;
                ValueText.MaxLength = catalog.MaxLength;
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Master data catalog could not be loaded.", ex);
                ShowMessage("The catalog could not be loaded. Please retry or contact support.");
                SaveButton.Enabled = false;
            }
        }

        private CatalogDefinition GetCatalog()
        {
            switch (CatalogSelect.SelectedValue)
            {
                case "categories":
                    return new CatalogDefinition
                    {
                        TableName = "dbo.TenderTypes", KeyColumn = "TypeName", MaxLength = 50,
                        DisplayName = "Category name",
                        UsageQuery = "SELECT COUNT(*) FROM dbo.Tender WHERE TenderCategory=@Value;"
                    };
                case "methods":
                    return new CatalogDefinition
                    {
                        TableName = "dbo.TenderMethods", KeyColumn = "MethodName", MaxLength = 50,
                        DisplayName = "Method name",
                        UsageQuery = "SELECT COUNT(*) FROM dbo.Tender WHERE TenderType=@Value;"
                    };
                case "documents":
                    return new CatalogDefinition
                    {
                        TableName = "dbo.DocumentTypes", KeyColumn = "TypeName", MaxLength = 80,
                        DisplayName = "Document type name",
                        UsageQuery = @"SELECT
                            (SELECT COUNT(*) FROM dbo.TenderDocuments WHERE DocumentType=@Value) +
                            (SELECT COUNT(*) FROM dbo.TechnicalBidDocuments WHERE DocumentType=@Value) +
                            (SELECT COUNT(*) FROM dbo.TenderRequiredBidDocuments WHERE DocumentType=@Value);"
                    };
                case "units":
                    return new CatalogDefinition
                    {
                        TableName = "dbo.Units", KeyColumn = "UnitName", MaxLength = 20,
                        DisplayName = "Unit name",
                        UsageQuery = "SELECT COUNT(*) FROM dbo.BOQ WHERE Unit=@Value;"
                    };
                case "designations":
                    return new CatalogDefinition
                    {
                        TableName = "dbo.Designations", KeyColumn = "DesignationName", MaxLength = 150,
                        DisplayName = "Designation name",
                        UsageQuery = "SELECT COUNT(*) FROM dbo.TenderBidOpening WHERE Designation=@Value;"
                    };
                default:
                    throw new InvalidOperationException("Select a valid master-data catalog.");
            }
        }

        private void ResetForm()
        {
            OriginalValue.Value = String.Empty;
            ValueText.Text = String.Empty;
            ValueText.ReadOnly = false;
            SortOrderText.Text = "0";
            IsActiveCheck.Checked = true;
            SaveButton.Text = "Add item";
            SaveButton.Enabled = true;
            CancelButton.Visible = false;
        }

        private void ShowMessage(string text, bool success = false)
        {
            Message.Text = "<div class=\"message" + (success ? " success" : String.Empty) + "\">" +
                System.Web.HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
