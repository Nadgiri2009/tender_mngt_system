<%@ Page Title="Sign in" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="DepartmentModule.Pages.Login" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Sign in</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="auth-layout">
        <section class="card auth-card">
            <h1>Sign in</h1>
            <p class="muted auth-description">Enter your mobile number. We will send you a one-time password.</p>
            <asp:Literal ID="Message" runat="server" />
            <div class="field">
                <asp:Label ID="MobileLabel" runat="server" AssociatedControlID="MobileText">Mobile number</asp:Label>
                <div class="mobile-number-input"><span class="country-code" aria-label="India country calling code">+91</span><asp:TextBox ID="MobileText" runat="server" TextMode="Phone" MaxLength="10" autocomplete="tel-national" placeholder="10-digit mobile number" /></div>
                <asp:RequiredFieldValidator ID="MobileRequired" runat="server" ControlToValidate="MobileText" ValidationGroup="LoginId" CssClass="validation" Display="Dynamic" ErrorMessage="Mobile number is required." />
                <asp:CustomValidator ID="MobileFormat" runat="server" ControlToValidate="MobileText" ValidationGroup="LoginId" CssClass="validation" Display="Dynamic" OnServerValidate="Mobile_ServerValidate" ErrorMessage="Enter a valid 10-digit Indian mobile number." />
                <span class="field-help">Enter your registered 10-digit mobile number.</span>
            </div>
            <div id="OtpAutoStatus" class="otp-auto-status" role="status" aria-live="polite"></div>
            <asp:Button ID="RequestOtpButton" runat="server" Text="Send login OTP" CssClass="button auth-submit auto-otp-submit" ValidationGroup="LoginId" OnClick="RequestOtpButton_Click" />
            <asp:Panel ID="OtpPanel" runat="server" Visible="false">
                <p class="muted auth-otp-note">Enter the six-digit OTP.</p>
                <div class="field">
                    <asp:Label ID="OtpLabel" runat="server" AssociatedControlID="OtpText">Login OTP</asp:Label>
                    <asp:TextBox ID="OtpText" runat="server" MaxLength="6" autocomplete="one-time-code" />
                    <asp:RequiredFieldValidator ID="OtpRequired" runat="server" ControlToValidate="OtpText" ValidationGroup="LoginOtp" CssClass="validation" Display="Dynamic" ErrorMessage="OTP is required." />
                    <asp:RegularExpressionValidator ID="OtpFormat" runat="server" ControlToValidate="OtpText" ValidationGroup="LoginOtp" CssClass="validation" Display="Dynamic" ValidationExpression="^\d{6}$" ErrorMessage="Enter the six-digit OTP." />
                </div>
                <asp:Button ID="VerifyOtpButton" runat="server" Text="Verify OTP and sign in" CssClass="button auth-submit" ValidationGroup="LoginOtp" OnClick="VerifyOtpButton_Click" />
            </asp:Panel>
            <p class="auth-note">New vendor? <asp:HyperLink ID="RegisterLink" runat="server">Register as Contractor</asp:HyperLink></p>
        </section>
    </div>
    <script>
        (function () {
            document.documentElement.classList.add("js");
            var mobile = document.getElementById("<%= MobileText.ClientID %>");
            var requestButton = document.getElementById("<%= RequestOtpButton.ClientID %>");
            var status = document.getElementById("OtpAutoStatus");
            var requestStarted = false;
            mobile.addEventListener("input", function () {
                requestStarted = false;
                var value = mobile.value.trim();
                if (!/^[6-9][0-9]{9}$/.test(value)) {
                    status.textContent = "";
                    status.classList.remove("is-active");
                    return;
                }

                if (requestStarted) return;
                requestStarted = true;
                status.textContent = "Requesting your login OTP...";
                status.classList.add("is-active");
                requestButton.click();
            });
        }());
    </script>
</asp:Content>
