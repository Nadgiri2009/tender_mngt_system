<%@ Page Title="Tender Management Portal" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="DepartmentModule.Pages.Home" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Solapur Municipal Corporation Tender Portal</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <section class="hero card">
        <div>
            <p class="eyebrow">Government of Maharashtra · Solapur Municipal Corporation</p>
            <h1>Official e-Tender Portal</h1>
            <p class="hero-copy">Transparent digital procurement, from department requirements through tendering and project delivery.</p>
            <a class="button" href="<%= ResolveUrl("~/Pages/Tenders.aspx") %>">Browse tenders</a>
            <a class="button secondary" href="<%= ResolveUrl("~/Pages/Register.aspx") %>">Register as a contractor</a>
        </div>
    </section>
    <asp:Literal ID="Message" runat="server" />
    <section class="cards" aria-label="Portal statistics">
        <div class="stat"><span class="muted">Active tenders</span><strong><asp:Literal ID="ActiveTenders" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Tenders published</span><strong><asp:Literal ID="PublishedTenders" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Registered contractors</span><strong><asp:Literal ID="RegisteredContractors" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Departments</span><strong><asp:Literal ID="DepartmentCount" runat="server" /></strong></div>
    </section>
    <section class="card">
        <div class="page-heading"><div><h2>Latest tenders</h2><p class="muted">Current procurement notices from the Corporation.</p></div><a href="<%= ResolveUrl("~/Pages/Tenders.aspx") %>">View all tenders</a></div>
        <asp:GridView ID="LatestTendersGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No published tenders are currently available.">
            <Columns>
                <asp:BoundField DataField="TenderNumber" HeaderText="Tender ID" />
                <asp:BoundField DataField="Title" HeaderText="Tender Title" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="SubmissionDeadline" HeaderText="Submission deadline" DataFormatString="{0:dd MMM yyyy}" />
            </Columns>
        </asp:GridView>
    </section>
    <section class="card">
        <div class="page-heading"><div><h2>Departments</h2><p class="muted">Tendering departments across Solapur Municipal Corporation.</p></div></div>
        <asp:GridView ID="DepartmentsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="Department information is not available.">
            <Columns>
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="DepartmentCode" HeaderText="Department code" />
            </Columns>
        </asp:GridView>
    </section>
    <section class="card" id="announcements">
        <div class="page-heading"><div><h2>Notices and announcements</h2><p class="muted">Official updates from the tender management system.</p></div></div>
        <asp:GridView ID="AnnouncementsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="There are no current announcements.">
            <Columns>
                <asp:BoundField DataField="Title" HeaderText="Notice" />
                <asp:BoundField DataField="Body" HeaderText="Details" />
                <asp:BoundField DataField="PublishDate" HeaderText="Published" DataFormatString="{0:dd MMM yyyy}" />
                <asp:BoundField DataField="ExpiryDate" HeaderText="Expiry" DataFormatString="{0:dd MMM yyyy}" />
            </Columns>
        </asp:GridView>
    </section>
    <section class="card">
        <h2>About this portal</h2>
        <p class="muted">This portal supports transparent tender notices and the complete municipal procurement lifecycle. Registered contractors can participate in eligible tenders; authorized staff manage tender review, approvals, agreements, execution, measurements, billing, and payments.</p>
    </section>
    <section class="card">
        <h2>Frequently asked questions</h2>
        <details><summary>How do I participate in a tender?</summary><p class="muted">Register as a contractor, sign in, and review the tender's notice and submission requirements.</p></details>
        <details><summary>Where can I find tender notices?</summary><p class="muted">Use the Tenders link to view tender records and the Notices section for current announcements.</p></details>
        <details><summary>How do I access department workflows?</summary><p class="muted">Sign in with an authorized municipal staff account. Staff accounts are provisioned by an administrator.</p></details>
    </section>
</asp:Content>
