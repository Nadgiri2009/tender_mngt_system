<%@ Page Title="Vendor registration" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="DepartmentModule.Pages.Register" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Vendor Registration</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="auth-layout">
        <section class="card auth-card">
            <h1>Contractor registration</h1>
            <p class="muted auth-description">Enter your details and mobile number. We will send a one-time password to verify your number.</p>
            <asp:Literal ID="Message" runat="server" />
            <div class="field">
                <asp:Label ID="FullNameLabel" runat="server" AssociatedControlID="FullNameText">Full Name</asp:Label>
                <asp:TextBox ID="FullNameText" runat="server" MaxLength="150" autocomplete="name" />
                <asp:RequiredFieldValidator ID="FullNameRequired" runat="server" ControlToValidate="FullNameText" ValidationGroup="Register" CssClass="validation" Display="Dynamic" ErrorMessage="Full name is required." />
                <asp:CustomValidator ID="FullNameLength" runat="server" ControlToValidate="FullNameText" ValidationGroup="Register" CssClass="validation" Display="Dynamic" OnServerValidate="FullName_ServerValidate" ErrorMessage="Enter a name between 2 and 150 characters." />
            </div>
            <div class="field">
                <asp:Label ID="EmailLabel" runat="server" AssociatedControlID="EmailText">Contact email</asp:Label>
                <asp:TextBox ID="EmailText" runat="server" TextMode="Email" MaxLength="150" autocomplete="email" />
                <asp:RequiredFieldValidator ID="EmailRequired" runat="server" ControlToValidate="EmailText" ValidationGroup="Register" CssClass="validation" Display="Dynamic" ErrorMessage="Email is required." />
                <asp:RegularExpressionValidator ID="EmailFormat" runat="server" ControlToValidate="EmailText" ValidationGroup="Register" CssClass="validation" Display="Dynamic" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="Enter a valid email address." />
                <span class="field-help">For account contact only; sign-in uses your mobile number and OTP.</span>
            </div>
            <div class="field">
                <asp:Label ID="MobileLabel" runat="server" AssociatedControlID="MobileText">Mobile Number</asp:Label>
                <div class="mobile-number-input"><span class="country-code" aria-label="India country calling code">+91</span><asp:TextBox ID="MobileText" runat="server" TextMode="Phone" MaxLength="10" autocomplete="tel-national" placeholder="10-digit mobile number" /></div>
                <asp:RequiredFieldValidator ID="MobileRequired" runat="server" ControlToValidate="MobileText" ValidationGroup="Register" CssClass="validation" Display="Dynamic" ErrorMessage="Mobile number is required." />
                <asp:CustomValidator ID="MobileFormat" runat="server" ControlToValidate="MobileText" ValidationGroup="Register" CssClass="validation" Display="Dynamic" OnServerValidate="Mobile_ServerValidate" ErrorMessage="Enter a valid 10-digit Indian mobile number." />
                <span class="field-help">Enter a valid 10-digit Indian mobile number.</span>
            </div>
            <div id="OtpAutoStatus" class="otp-auto-status" role="status" aria-live="polite"></div>
            <asp:Button ID="RequestOtpButton" runat="server" Text="Send registration OTP" CssClass="button auth-submit auto-otp-submit" ValidationGroup="Register" OnClick="RequestOtpButton_Click" />
            <asp:Panel ID="OtpPanel" runat="server" Visible="false">
                <p class="muted auth-otp-note">Enter the six-digit OTP.</p>
                <div class="field">
                    <asp:Label ID="OtpLabel" runat="server" AssociatedControlID="OtpText">Registration OTP</asp:Label>
                    <asp:TextBox ID="OtpText" runat="server" MaxLength="6" autocomplete="one-time-code" />
                    <asp:RequiredFieldValidator ID="OtpRequired" runat="server" ControlToValidate="OtpText" ValidationGroup="RegisterOtp" CssClass="validation" Display="Dynamic" ErrorMessage="OTP is required." />
                    <asp:RegularExpressionValidator ID="OtpFormat" runat="server" ControlToValidate="OtpText" ValidationGroup="RegisterOtp" CssClass="validation" Display="Dynamic" ValidationExpression="^[0-9]{6}$" ErrorMessage="Enter the six-digit OTP." />
                </div>
                <asp:Button ID="RegisterButton" runat="server" Text="Verify OTP and register" CssClass="button auth-submit" ValidationGroup="RegisterOtp" OnClick="RegisterButton_Click" />
            </asp:Panel>
            <p class="auth-note">Already registered? <asp:HyperLink ID="LoginLink" runat="server">Sign in</asp:HyperLink></p>
        </section>
    </div>
    <script>
        (function () {
            document.documentElement.classList.add("js");
            var fullName = document.getElementById("<%= FullNameText.ClientID %>");
            var email = document.getElementById("<%= EmailText.ClientID %>");
            var mobile = document.getElementById("<%= MobileText.ClientID %>");
            var requestButton = document.getElementById("<%= RequestOtpButton.ClientID %>");
            var status = document.getElementById("OtpAutoStatus");
            var mobileEntered = false;
            var requestStarted = false;
            var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            function requestOtpWhenReady() {
                if (!mobileEntered || requestStarted) return;
                var name = fullName.value.trim();
                var address = email.value.trim();
                var number = mobile.value.trim();
                if (name.length < 2 || name.length > 150 ||
                    !emailPattern.test(address) ||
                    !/^[6-9][0-9]{9}$/.test(number)) {
                    if (/^[6-9][0-9]{9}$/.test(number)) {
                        status.textContent = "Complete your name and contact email to request the OTP.";
                        status.classList.add("is-active");
                    }
                    return;
                }

                requestStarted = true;
                status.textContent = "Requesting your registration OTP...";
                status.classList.add("is-active");
                requestButton.click();
            }

            mobile.addEventListener("input", function () {
                mobileEntered = true;
                requestStarted = false;
                status.textContent = "";
                status.classList.remove("is-active");
                requestOtpWhenReady();
            });
            fullName.addEventListener("input", requestOtpWhenReady);
            email.addEventListener("input", requestOtpWhenReady);
        }());
    </script>
</asp:Content>
