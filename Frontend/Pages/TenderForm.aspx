<%@ Page Title="New Tender" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="TenderForm.aspx.cs" Inherits="DepartmentModule.Pages.TenderForm" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server"><asp:Literal ID="FormTitle" runat="server" /></asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1><asp:Literal ID="FormHeading" runat="server" /></h1><div class="muted"><asp:Literal ID="FormDescription" runat="server" /></div></div><a href="Tenders.aspx" class="button secondary">Back to tenders</a></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <asp:ValidationSummary ID="ValidationSummary" runat="server" CssClass="message" HeaderText="Please correct the following:" />
        <div class="field"><label for="TitleText">Title</label><asp:TextBox ID="TitleText" runat="server" MaxLength="250" />
            <asp:RequiredFieldValidator ID="TitleRequired" runat="server" ControlToValidate="TitleText" ErrorMessage="Title is required." CssClass="validation" />
            <asp:CustomValidator ID="TitleLength" runat="server" ControlToValidate="TitleText" OnServerValidate="TitleLength_ServerValidate" ErrorMessage="Title must be 5 to 250 characters." CssClass="validation" /></div>
        <div class="field"><label for="DescriptionText">Description</label><asp:TextBox ID="DescriptionText" runat="server" TextMode="MultiLine" Rows="4" MaxLength="4000" /></div>
        <div class="row">
            <asp:Panel ID="DepartmentPanel" runat="server" CssClass="field"><label for="DepartmentSelect">Department</label><asp:DropDownList ID="DepartmentSelect" runat="server" />
                <asp:RequiredFieldValidator ID="DepartmentRequired" runat="server" ControlToValidate="DepartmentSelect" InitialValue="" ErrorMessage="A valid Department must be selected." CssClass="validation" /></asp:Panel>
            <div class="field"><label for="TenderTypeSelect">Tender Type</label><asp:DropDownList ID="TenderTypeSelect" runat="server" />
                <asp:RequiredFieldValidator ID="TenderTypeRequired" runat="server" ControlToValidate="TenderTypeSelect" InitialValue="" ErrorMessage="Tender type is required." CssClass="validation" /></div>
            <div class="field"><label for="AmountText">Estimated Amount (₹)</label><asp:TextBox ID="AmountText" runat="server" TextMode="Number" step="0.01" min="0.01" />
                <asp:RequiredFieldValidator ID="AmountRequired" runat="server" ControlToValidate="AmountText" ErrorMessage="Estimated amount is required." CssClass="validation" />
                <asp:RangeValidator ID="AmountRange" runat="server" ControlToValidate="AmountText" Type="Double" MinimumValue="0.01" MaximumValue="999999999999999" ErrorMessage="Estimated amount must be greater than zero." CssClass="validation" /></div>
        </div>
        <div class="field"><label for="DeadlineText">Submission Deadline</label><asp:TextBox ID="DeadlineText" runat="server" TextMode="DateTimeLocal" /></div>
        <asp:Panel ID="AttachmentPanel" runat="server" CssClass="field"><label for="Attachments">Attachments</label><asp:FileUpload ID="Attachments" runat="server" AllowMultiple="true" />
            <span class="help">Optional supporting documents. PDF, Office documents, JPEG, or PNG; maximum 20 MB each.</span></asp:Panel>
        <asp:Button ID="CreateButton" runat="server" Text="Create Tender" CssClass="button" OnClick="CreateButton_Click" />
    </section>
</asp:Content>
