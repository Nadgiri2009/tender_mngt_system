<%@ Page Title="Manage Users" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="AdminUsers.aspx.cs" Inherits="DepartmentModule.Pages.AdminUsers" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Manage Users</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Manage Users</h1><div class="muted">Create staff accounts and manage active access.</div></div><a class="button secondary" href="Admin.aspx">Back to administration</a></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <asp:HiddenField ID="UserIdHidden" runat="server" />
        <h2><asp:Literal ID="UserFormHeading" runat="server">Create staff account</asp:Literal></h2>
        <p class="muted">Public self-registration is restricted to the Contractor role. Staff accounts must be provisioned here.</p>
        <div class="field"><asp:Label runat="server" AssociatedControlID="FullNameText">Full name</asp:Label><asp:TextBox ID="FullNameText" runat="server" MaxLength="150" /><asp:RequiredFieldValidator runat="server" ControlToValidate="FullNameText" ValidationGroup="User" CssClass="validation" ErrorMessage="Full name is required." /></div>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="EmailText">Email</asp:Label><asp:TextBox ID="EmailText" runat="server" TextMode="Email" MaxLength="150" /><asp:RequiredFieldValidator runat="server" ControlToValidate="EmailText" ValidationGroup="User" CssClass="validation" ErrorMessage="Email is required." /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="MobileText">Mobile</asp:Label><asp:TextBox ID="MobileText" runat="server" MaxLength="20" /></div>
        </div>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="PasswordText">Temporary password (required when creating; optional when editing)</asp:Label><asp:TextBox ID="PasswordText" runat="server" TextMode="Password" autocomplete="new-password" /><asp:CustomValidator runat="server" ControlToValidate="PasswordText" ValidationGroup="User" CssClass="validation" OnServerValidate="Password_ServerValidate" ErrorMessage="Password must be at least 6 characters." /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="RoleSelect">Role</asp:Label><asp:DropDownList ID="RoleSelect" runat="server" /><asp:RequiredFieldValidator runat="server" ControlToValidate="RoleSelect" InitialValue="" ValidationGroup="User" CssClass="validation" ErrorMessage="Select a role." /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="DepartmentSelect">Department (optional)</asp:Label><asp:DropDownList ID="DepartmentSelect" runat="server" /></div>
        </div>
        <div class="admin-actions">
            <asp:Button ID="CreateUserButton" runat="server" Text="Create account" ValidationGroup="User" OnClick="CreateUserButton_Click" />
            <asp:Button ID="UpdateUserButton" runat="server" Text="Save user changes" ValidationGroup="User" OnClick="UpdateUserButton_Click" Visible="false" />
            <asp:Button ID="CancelUserEditButton" runat="server" Text="Cancel edit" CausesValidation="false" CssClass="button secondary" OnClick="CancelUserEditButton_Click" Visible="false" />
        </div>
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
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="ContractorSearchText">Search contractors</asp:Label><asp:TextBox ID="ContractorSearchText" runat="server" MaxLength="100" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="ContractorStatusFilter">Status</asp:Label><asp:DropDownList ID="ContractorStatusFilter" runat="server"><asp:ListItem Text="All statuses" Value="" /><asp:ListItem Text="Active" Value="1" /><asp:ListItem Text="Inactive" Value="0" /></asp:DropDownList></div>
            <asp:Button ID="FilterContractorsButton" runat="server" Text="Filter contractors" CssClass="button secondary" CausesValidation="false" OnClick="FilterContractorsButton_Click" />
        </div>
        <asp:GridView ID="ContractorsGrid" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="20" GridLines="None" OnPageIndexChanging="ContractorsGrid_PageIndexChanging" OnRowCommand="ContractorsGrid_RowCommand" EmptyDataText="No contractor profiles found.">
            <Columns>
                <asp:BoundField DataField="Name" HeaderText="Contractor" />
                <asp:BoundField DataField="RegistrationNo" HeaderText="Registration" />
                <asp:BoundField DataField="Email" HeaderText="Email" />
                <asp:BoundField DataField="ContactNo" HeaderText="Contact" />
                <asp:BoundField DataField="UserName" HeaderText="Linked login" />
                <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
                <asp:TemplateField HeaderText="Access"><ItemTemplate><asp:LinkButton runat="server" Text='<%# Convert.ToBoolean(Eval("IsActive")) ? "Deactivate" : "Reactivate" %>' CommandName="ToggleContractor" CommandArgument='<%# Eval("ContractorId") %>' CausesValidation="false" OnClientClick="return confirm('Change this contractor profile status?');" /></ItemTemplate></asp:TemplateField>
            </Columns>
        </asp:GridView>
    </section>
    <section class="card">
        <h2>Staff and vendor users</h2>
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="UserSearchText">Search users</asp:Label><asp:TextBox ID="UserSearchText" runat="server" MaxLength="100" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="UserStatusFilter">Status</asp:Label><asp:DropDownList ID="UserStatusFilter" runat="server"><asp:ListItem Text="All statuses" Value="" /><asp:ListItem Text="Active" Value="1" /><asp:ListItem Text="Inactive" Value="0" /></asp:DropDownList></div>
            <asp:Button ID="FilterUsersButton" runat="server" Text="Filter users" CssClass="button secondary" CausesValidation="false" OnClick="FilterUsersButton_Click" />
        </div>
        <asp:GridView ID="UsersGrid" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="20" GridLines="None" OnPageIndexChanging="UsersGrid_PageIndexChanging" OnRowCommand="UsersGrid_RowCommand" EmptyDataText="No users found.">
            <Columns>
                <asp:BoundField DataField="FullName" HeaderText="Name" />
                <asp:BoundField DataField="Email" HeaderText="Email" />
                <asp:BoundField DataField="Mobile" HeaderText="Mobile" />
                <asp:BoundField DataField="RoleName" HeaderText="Role" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
                <asp:BoundField DataField="CreatedDate" HeaderText="Created" DataFormatString="{0:dd MMM yyyy}" />
                <asp:BoundField DataField="LastLogin" HeaderText="Last successful login" DataFormatString="{0:dd MMM yyyy HH:mm}" NullDisplayText="Never" />
                <asp:TemplateField HeaderText="Actions"><ItemTemplate><asp:LinkButton runat="server" Text="Edit" CommandName="EditUser" CommandArgument='<%# Eval("UserId") %>' CausesValidation="false" /> <asp:LinkButton ID="ToggleActiveButton" runat="server" CommandName="ToggleActive" CommandArgument='<%# Eval("UserId") %>' Text='<%# Convert.ToBoolean(Eval("IsActive")) ? "Deactivate" : "Activate" %>' CausesValidation="false" OnClientClick="return confirm('Change this user account status?');" /></ItemTemplate></asp:TemplateField>
            </Columns>
        </asp:GridView>
    </section>
</asp:Content>
