using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class AdminAnnouncements : ModulePage
    {
        protected Literal Message, FormHeading;
        protected HiddenField AnnouncementIdHidden;
        protected TextBox TitleText, BodyText, PublishDateText, ExpiryDateText;
        protected CheckBox PublishedCheckBox;
        protected Button SaveAnnouncementButton, CancelEditButton;
        protected GridView AnnouncementsGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Admin")) return;
            if (!IsPostBack) BindAnnouncements();
        }

        private void BindAnnouncements()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"SELECT AnnouncementId,Title,Body,IsPublished,PublishDate,ExpiryDate
                      FROM dbo.Announcements WHERE IsDeleted=0 ORDER BY CreatedDate DESC;", connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable data = new DataTable();
                    adapter.Fill(data);
                    AnnouncementsGrid.DataSource = data;
                    AnnouncementsGrid.DataBind();
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Announcement list could not be loaded.", ex);
                ShowMessage("Announcement records could not be loaded. Please retry or contact support.");
            }
        }

        protected void SaveAnnouncementButton_Click(object sender, EventArgs e)
        {
            Page.Validate("Announcement");
            if (!Page.IsValid) return;
            string title = TitleText.Text.Trim();
            string body = BodyText.Text;
            if (title.Length > 250 || (body != null && body.Length > 4000))
            {
                ShowMessage("Title must be at most 250 characters and details at most 4,000 characters.");
                return;
            }

            DateTime? publishDate;
            DateTime? expiryDate;
            if (!TryOptionalDate(PublishDateText.Text, out publishDate) || !TryOptionalDate(ExpiryDateText.Text, out expiryDate) ||
                (publishDate.HasValue && expiryDate.HasValue && expiryDate.Value < publishDate.Value))
            {
                ShowMessage("Enter valid publication dates; expiry cannot be earlier than publication.");
                return;
            }
            int announcementId;
            if (!String.IsNullOrEmpty(AnnouncementIdHidden.Value) &&
                !Int32.TryParse(AnnouncementIdHidden.Value, NumberStyles.None, CultureInfo.InvariantCulture, out announcementId))
            {
                ShowMessage("The selected announcement is invalid.");
                return;
            }
            announcementId = Int32.TryParse(AnnouncementIdHidden.Value, out announcementId) ? announcementId : 0;

            try
            {
                string sql = announcementId == 0
                    ? @"INSERT INTO dbo.Announcements (Title,Body,IsPublished,PublishDate,ExpiryDate,CreatedBy,CreatedDate)
                        VALUES (@Title,@Body,@IsPublished,@PublishDate,@ExpiryDate,@UserId,SYSUTCDATETIME());"
                    : @"UPDATE dbo.Announcements SET Title=@Title,Body=@Body,IsPublished=@IsPublished,
                        PublishDate=@PublishDate,ExpiryDate=@ExpiryDate,ModifiedBy=@UserId,ModifiedDate=SYSUTCDATETIME()
                        WHERE AnnouncementId=@AnnouncementId AND IsDeleted=0;";
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    AddAnnouncementParameters(command, title, body, publishDate, expiryDate);
                    ModuleDb.AddParameter(command, "@IsPublished", SqlDbType.Bit, PublishedCheckBox.Checked);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    if (announcementId > 0) ModuleDb.AddParameter(command, "@AnnouncementId", SqlDbType.Int, announcementId);
                    if (command.ExecuteNonQuery() != 1)
                    {
                        ShowMessage("The selected announcement no longer exists.");
                        return;
                    }
                }
                ResetForm();
                ShowMessage("Announcement saved.", true);
                BindAnnouncements();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Announcement could not be saved.", ex);
                ShowMessage("Announcement could not be saved. Please retry or contact support.");
            }
        }

        protected void AnnouncementsGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0)
            {
                ShowMessage("The selected announcement is invalid.");
                return;
            }
            if (e.CommandName == "EditAnnouncement")
            {
                try
                {
                    using (SqlConnection connection = ModuleDb.OpenConnection())
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT Title,Body,IsPublished,PublishDate,ExpiryDate FROM dbo.Announcements
                          WHERE AnnouncementId=@AnnouncementId AND IsDeleted=0;", connection))
                    {
                        ModuleDb.AddParameter(command, "@AnnouncementId", SqlDbType.Int, id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read()) { ShowMessage("The selected announcement no longer exists."); return; }
                            AnnouncementIdHidden.Value = id.ToString(CultureInfo.InvariantCulture);
                            TitleText.Text = Convert.ToString(reader["Title"]);
                            BodyText.Text = reader["Body"] == DBNull.Value ? String.Empty : Convert.ToString(reader["Body"]);
                            PublishedCheckBox.Checked = Convert.ToBoolean(reader["IsPublished"]);
                            PublishDateText.Text = DateInputValue(reader["PublishDate"]);
                            ExpiryDateText.Text = DateInputValue(reader["ExpiryDate"]);
                            FormHeading.Text = "Edit announcement";
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Trace.Warn("DepartmentModule", "Announcement could not be opened for editing.", ex);
                    ShowMessage("Announcement details could not be loaded.");
                }
                return;
            }
            if (e.CommandName != "DeleteAnnouncement") return;

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"UPDATE dbo.Announcements SET IsDeleted=1,ModifiedBy=@UserId,ModifiedDate=SYSUTCDATETIME()
                      WHERE AnnouncementId=@AnnouncementId AND IsDeleted=0;", connection))
                {
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    ModuleDb.AddParameter(command, "@AnnouncementId", SqlDbType.Int, id);
                    if (command.ExecuteNonQuery() != 1) { ShowMessage("The selected announcement no longer exists."); return; }
                }
                ShowMessage("Announcement deleted.", true);
                BindAnnouncements();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Announcement could not be deleted.", ex);
                ShowMessage("Announcement could not be deleted. Please retry or contact support.");
            }
        }

        protected void CancelEditButton_Click(object sender, EventArgs e) { ResetForm(); }

        private static void AddAnnouncementParameters(SqlCommand command, string title, string body,
            DateTime? publishDate, DateTime? expiryDate)
        {
            ModuleDb.AddParameter(command, "@Title", SqlDbType.NVarChar, title, 250);
            ModuleDb.AddParameter(command, "@Body", SqlDbType.NVarChar, String.IsNullOrWhiteSpace(body) ? null : body, -1);
            ModuleDb.AddParameter(command, "@PublishDate", SqlDbType.DateTime2, publishDate.HasValue ? (object)publishDate.Value : null);
            ModuleDb.AddParameter(command, "@ExpiryDate", SqlDbType.DateTime2, expiryDate.HasValue ? (object)expiryDate.Value : null);
        }

        private static bool TryOptionalDate(string value, out DateTime? date)
        {
            date = null;
            if (String.IsNullOrWhiteSpace(value)) return true;
            DateTime parsed;
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)) return false;
            date = parsed;
            return true;
        }

        private static string DateInputValue(object value)
        {
            return value == DBNull.Value ? String.Empty : Convert.ToDateTime(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private void ResetForm()
        {
            AnnouncementIdHidden.Value = String.Empty;
            TitleText.Text = BodyText.Text = PublishDateText.Text = ExpiryDateText.Text = String.Empty;
            PublishedCheckBox.Checked = false;
            FormHeading.Text = "Create announcement";
        }

        private void ShowMessage(string text, bool success = false)
        {
            Message.Text = "<div class=\"message" + (success ? " success" : String.Empty) + "\">" +
                System.Web.HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
