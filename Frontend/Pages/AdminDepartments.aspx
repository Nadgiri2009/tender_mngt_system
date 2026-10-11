<%@ Page Title="Manage Departments" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="AdminDepartments.aspx.cs" Inherits="DepartmentModule.Pages.AdminDepartments" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Manage Departments</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Manage Departments</h1><div class="muted">Maintain department names, codes, and availability.</div></div><a class="button secondary" href="Admin.aspx">Back to administration</a></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <asp:HiddenField ID="DepartmentIdHidden" runat="server" />
        <h2><asp:Literal ID="FormHeading" runat="server">Add department</asp:Literal></h2>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="DepartmentNameText">Department name</asp:Label><asp:TextBox ID="DepartmentNameText" runat="server" MaxLength="150" /><asp:RequiredFieldValidator runat="server" ControlToValidate="DepartmentNameText" ValidationGroup="Department" CssClass="validation" ErrorMessage="Department name is required." /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="DepartmentCodeText">Department code</asp:Label><asp:TextBox ID="DepartmentCodeText" runat="server" MaxLength="20" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="HeadOfficerText">Head officer</asp:Label><asp:TextBox ID="HeadOfficerText" runat="server" MaxLength="150" /></div>
        </div>
        <asp:CheckBox ID="DepartmentActive" runat="server" Text="Active" Checked="true" />
        <div class="admin-actions"><asp:Button ID="SaveDepartmentButton" runat="server" Text="Save department" ValidationGroup="Department" OnClick="SaveDepartmentButton_Click" /><asp:Button ID="CancelEditButton" runat="server" Text="Cancel edit" CausesValidation="false" CssClass="button secondary" OnClick="CancelEditButton_Click" /></div>
    </section>
    <section class="card">
        <h2>Departments</h2>
        <asp:GridView ID="DepartmentsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" OnRowCommand="DepartmentsGrid_RowCommand" EmptyDataText="No departments found.">
            <Columns>
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="DepartmentCode" HeaderText="Code" />
                <asp:BoundField DataField="HeadOfficerName" HeaderText="Head officer" />
                <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
                <asp:TemplateField HeaderText="Actions"><ItemTemplate><asp:LinkButton runat="server" Text="Edit" CommandName="EditDepartment" CommandArgument='<%# Eval("DepartmentId") %>' CausesValidation="false" /> <asp:LinkButton runat="server" Text="Disable" CommandName="DisableDepartment" CommandArgument='<%# Eval("DepartmentId") %>' CausesValidation="false" OnClientClick="return confirm('Disable this department? Existing records will be preserved.');" /></ItemTemplate></asp:TemplateField>
            </Columns>
        </asp:GridView>
    </section>
</asp:Content>
