<%@ Page Title="Manage Announcements" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="AdminAnnouncements.aspx.cs" Inherits="DepartmentModule.Pages.AdminAnnouncements" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Manage Announcements</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Manage Announcements</h1><div class="muted">Publish and maintain portal notices.</div></div><a class="button secondary" href="Admin.aspx">Back to administration</a></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <asp:HiddenField ID="AnnouncementIdHidden" runat="server" />
        <h2><asp:Literal ID="FormHeading" runat="server">Create announcement</asp:Literal></h2>
        <div class="field"><asp:Label runat="server" AssociatedControlID="TitleText">Title</asp:Label><asp:TextBox ID="TitleText" runat="server" MaxLength="250" /><asp:RequiredFieldValidator runat="server" ControlToValidate="TitleText" ValidationGroup="Announcement" CssClass="validation" ErrorMessage="Title is required." /></div>
        <div class="field"><asp:Label runat="server" AssociatedControlID="BodyText">Details</asp:Label><asp:TextBox ID="BodyText" runat="server" TextMode="MultiLine" /></div>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="PublishDateText">Publish date (optional)</asp:Label><asp:TextBox ID="PublishDateText" runat="server" TextMode="Date" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="ExpiryDateText">Expiry date (optional)</asp:Label><asp:TextBox ID="ExpiryDateText" runat="server" TextMode="Date" /></div>
        </div>
        <asp:CheckBox ID="PublishedCheckBox" runat="server" Text="Published" />
        <div class="admin-actions"><asp:Button ID="SaveAnnouncementButton" runat="server" Text="Save announcement" ValidationGroup="Announcement" OnClick="SaveAnnouncementButton_Click" /><asp:Button ID="CancelEditButton" runat="server" Text="Cancel edit" CausesValidation="false" CssClass="button secondary" OnClick="CancelEditButton_Click" /></div>
    </section>
    <section class="card">
        <h2>All announcements</h2>
        <asp:GridView ID="AnnouncementsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" OnRowCommand="AnnouncementsGrid_RowCommand" EmptyDataText="No announcements found.">
            <Columns>
                <asp:BoundField DataField="Title" HeaderText="Title" />
                <asp:BoundField DataField="PublishDate" HeaderText="Publish date" DataFormatString="{0:dd MMM yyyy}" />
                <asp:BoundField DataField="ExpiryDate" HeaderText="Expiry date" DataFormatString="{0:dd MMM yyyy}" />
                <asp:CheckBoxField DataField="IsPublished" HeaderText="Published" />
                <asp:TemplateField HeaderText="Actions"><ItemTemplate><asp:LinkButton runat="server" Text="Edit" CommandName="EditAnnouncement" CommandArgument='<%# Eval("AnnouncementId") %>' CausesValidation="false" /> <asp:LinkButton runat="server" Text="Delete" CommandName="DeleteAnnouncement" CommandArgument='<%# Eval("AnnouncementId") %>' CausesValidation="false" OnClientClick="return confirm('Delete this announcement?');" /></ItemTemplate></asp:TemplateField>
            </Columns>
        </asp:GridView>
    </section>
</asp:Content>
