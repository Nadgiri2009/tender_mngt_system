<%@ Page Title="Sign in" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="DepartmentModule.Pages.Login" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Sign in</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <main class="auth-layout">
        <section class="card auth-card">
            <div class="auth-mark" aria-hidden="true">SMC</div>
            <h1>Sign in</h1>
            <p class="muted">Solapur Municipal Corporation Tender Portal</p>
            <asp:Literal ID="Message" runat="server" />
            <div class="field">
                <asp:Label ID="EmailLabel" runat="server" AssociatedControlID="EmailText">Email</asp:Label>
                <asp:TextBox ID="EmailText" runat="server" TextMode="Email" MaxLength="150" autocomplete="email" />
                <asp:RequiredFieldValidator ID="EmailRequired" runat="server" ControlToValidate="EmailText" ValidationGroup="Login" CssClass="validation" ErrorMessage="Email is required." />
                <asp:RegularExpressionValidator ID="EmailFormat" runat="server" ControlToValidate="EmailText" ValidationGroup="Login" CssClass="validation" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="Enter a valid email address." />
            </div>
            <div class="field">
                <asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="PasswordText">Password</asp:Label>
                <asp:TextBox ID="PasswordText" runat="server" TextMode="Password" autocomplete="current-password" />
                <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="PasswordText" ValidationGroup="Login" CssClass="validation" ErrorMessage="Password is required." />
                <asp:CustomValidator ID="PasswordLength" runat="server" ControlToValidate="PasswordText" ValidationGroup="Login" CssClass="validation" OnServerValidate="PasswordLength_ServerValidate" ErrorMessage="Password must be at least 6 characters." />
            </div>
            <asp:Button ID="SignInButton" runat="server" Text="Sign in" CssClass="button auth-submit" ValidationGroup="Login" OnClick="SignInButton_Click" />
            <p class="auth-note">New vendor? <a href="<%= ResolveUrl("~/Pages/Register.aspx") %>">Register as Contractor</a></p>
        </section>
    </main>
</asp:Content>
