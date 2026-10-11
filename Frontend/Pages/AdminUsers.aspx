<%@ Page Title="Manage Users" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="AdminUsers.aspx.cs" Inherits="DepartmentModule.Pages.AdminUsers" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Manage Users</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Manage Users</h1><div class="muted">Create staff accounts and manage active access.</div></div><a class="button secondary" href="Admin.aspx">Back to administration</a></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <h2>Create staff account</h2>
        <p class="muted">Public self-registration is restricted to the Contractor role. Staff accounts must be provisioned here.</p>
        <div class="field"><asp:Label runat="server" AssociatedControlID="FullNameText">Full name</asp:Label><asp:TextBox ID="FullNameText" runat="server" MaxLength="150" /><asp:RequiredFieldValidator runat="server" ControlToValidate="FullNameText" ValidationGroup="User" CssClass="validation" ErrorMessage="Full name is required." /></div>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="EmailText">Email</asp:Label><asp:TextBox ID="EmailText" runat="server" TextMode="Email" MaxLength="150" /><asp:RequiredFieldValidator runat="server" ControlToValidate="EmailText" ValidationGroup="User" CssClass="validation" ErrorMessage="Email is required." /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="MobileText">Mobile</asp:Label><asp:TextBox ID="MobileText" runat="server" MaxLength="20" /></div>
        </div>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="PasswordText">Temporary password</asp:Label><asp:TextBox ID="PasswordText" runat="server" TextMode="Password" /><asp:CustomValidator runat="server" ControlToValidate="PasswordText" ValidationGroup="User" CssClass="validation" OnServerValidate="Password_ServerValidate" ErrorMessage="Password must be at least 6 characters." /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="RoleSelect">Role</asp:Label><asp:DropDownList ID="RoleSelect" runat="server" /><asp:RequiredFieldValidator runat="server" ControlToValidate="RoleSelect" InitialValue="" ValidationGroup="User" CssClass="validation" ErrorMessage="Select a role." /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="DepartmentSelect">Department (optional)</asp:Label><asp:DropDownList ID="DepartmentSelect" runat="server" /></div>
        </div>
        <asp:Button ID="CreateUserButton" runat="server" Text="Create account" ValidationGroup="User" OnClick="CreateUserButton_Click" />
    </section>
    <section class="card">
        <h2>Link a contractor profile</h2>
        <p class="muted">Link an existing contractor login to a SQL Server contractor profile so the account can submit bids.</p>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="ContractorUserSelect">Contractor login</asp:Label><asp:DropDownList ID="ContractorUserSelect" runat="server" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="ContractorNameText">Business / contractor name</asp:Label><asp:TextBox ID="ContractorNameText" runat="server" MaxLength="150" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="ContractorRegistrationNoText">Registration number</asp:Label><asp:TextBox ID="ContractorRegistrationNoText" runat="server" MaxLength="100" /></div>
        </div>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="ContractorAddressText">Address</asp:Label><asp:TextBox ID="ContractorAddressText" runat="server" MaxLength="300" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="ContractorContactText">Contact number</asp:Label><asp:TextBox ID="ContractorContactText" runat="server" MaxLength="20" /></div>
        </div>
        <asp:Button ID="CreateContractorButton" runat="server" Text="Create and link profile" OnClick="CreateContractorButton_Click" />
    </section>
    <section class="card">
        <h2>Contractor profiles</h2>
        <asp:GridView ID="ContractorsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No contractor profiles found.">
            <Columns>
                <asp:BoundField DataField="Name" HeaderText="Contractor" />
                <asp:BoundField DataField="RegistrationNo" HeaderText="Registration" />
                <asp:BoundField DataField="Email" HeaderText="Email" />
                <asp:BoundField DataField="ContactNo" HeaderText="Contact" />
                <asp:BoundField DataField="UserName" HeaderText="Linked login" />
                <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
            </Columns>
        </asp:GridView>
    </section>
    <section class="card">
        <h2>Staff and vendor users</h2>
        <asp:GridView ID="UsersGrid" runat="server" AutoGenerateColumns="false" GridLines="None" OnRowCommand="UsersGrid_RowCommand" EmptyDataText="No users found.">
            <Columns>
                <asp:BoundField DataField="FullName" HeaderText="Name" />
                <asp:BoundField DataField="Email" HeaderText="Email" />
                <asp:BoundField DataField="Mobile" HeaderText="Mobile" />
                <asp:BoundField DataField="RoleName" HeaderText="Role" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
                <asp:BoundField DataField="CreatedDate" HeaderText="Created" DataFormatString="{0:dd MMM yyyy}" />
                <asp:TemplateField HeaderText="Access"><ItemTemplate><asp:LinkButton ID="ToggleActiveButton" runat="server" CommandName="ToggleActive" CommandArgument='<%# Eval("UserId") %>' Text='<%# Convert.ToBoolean(Eval("IsActive")) ? "Deactivate" : "Activate" %>' CausesValidation="false" /></ItemTemplate></asp:TemplateField>
            </Columns>
        </asp:GridView>
    </section>
</asp:Content>
