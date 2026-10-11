<%@ Page Title="Vendor registration" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="DepartmentModule.Pages.Register" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Vendor Registration</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <main class="auth-layout">
        <section class="card auth-card">
            <div class="auth-mark" aria-hidden="true">SMC</div>
            <h1>Vendor / Contractor Registration</h1>
            <p class="muted">Register to bid on Solapur Municipal Corporation tenders. Government staff accounts are provisioned by an administrator.</p>
            <asp:Literal ID="Message" runat="server" />
            <div class="field">
                <asp:Label ID="FullNameLabel" runat="server" AssociatedControlID="FullNameText">Full Name</asp:Label>
                <asp:TextBox ID="FullNameText" runat="server" MaxLength="150" autocomplete="name" />
                <asp:RequiredFieldValidator ID="FullNameRequired" runat="server" ControlToValidate="FullNameText" ValidationGroup="Register" CssClass="validation" ErrorMessage="Full name is required." />
                <asp:CustomValidator ID="FullNameLength" runat="server" ControlToValidate="FullNameText" ValidationGroup="Register" CssClass="validation" OnServerValidate="FullName_ServerValidate" ErrorMessage="Enter a name between 2 and 150 characters." />
            </div>
            <div class="field">
                <asp:Label ID="EmailLabel" runat="server" AssociatedControlID="EmailText">Email</asp:Label>
                <asp:TextBox ID="EmailText" runat="server" TextMode="Email" MaxLength="150" autocomplete="email" />
                <asp:RequiredFieldValidator ID="EmailRequired" runat="server" ControlToValidate="EmailText" ValidationGroup="Register" CssClass="validation" ErrorMessage="Email is required." />
                <asp:RegularExpressionValidator ID="EmailFormat" runat="server" ControlToValidate="EmailText" ValidationGroup="Register" CssClass="validation" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="Enter a valid email address." />
            </div>
            <div class="field">
                <asp:Label ID="MobileLabel" runat="server" AssociatedControlID="MobileText">Mobile Number</asp:Label>
                <asp:TextBox ID="MobileText" runat="server" TextMode="Phone" MaxLength="20" autocomplete="tel" />
                <asp:RequiredFieldValidator ID="MobileRequired" runat="server" ControlToValidate="MobileText" ValidationGroup="Register" CssClass="validation" ErrorMessage="Mobile number is required." />
                <asp:CustomValidator ID="MobileFormat" runat="server" ControlToValidate="MobileText" ValidationGroup="Register" CssClass="validation" OnServerValidate="Mobile_ServerValidate" ErrorMessage="Enter a valid mobile number with 10 to 15 digits." />
            </div>
            <div class="field">
                <asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="PasswordText">Password</asp:Label>
                <asp:TextBox ID="PasswordText" runat="server" TextMode="Password" autocomplete="new-password" />
                <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="PasswordText" ValidationGroup="Register" CssClass="validation" ErrorMessage="Password is required." />
                <asp:CustomValidator ID="PasswordLength" runat="server" ControlToValidate="PasswordText" ValidationGroup="Register" CssClass="validation" OnServerValidate="Password_ServerValidate" ErrorMessage="Password must be at least 6 characters." />
            </div>
            <asp:Button ID="RegisterButton" runat="server" Text="Register as Contractor" CssClass="button auth-submit" ValidationGroup="Register" OnClick="RegisterButton_Click" />
            <p class="auth-note">Already registered? <a href="<%= ResolveUrl("~/Pages/Login.aspx") %>">Sign in</a></p>
        </section>
    </main>
</asp:Content>
