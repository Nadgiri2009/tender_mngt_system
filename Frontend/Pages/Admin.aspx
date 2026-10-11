<%@ Page Title="Admin Dashboard" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="DepartmentModule.Pages.AdminDashboard" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Admin Dashboard</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Admin Dashboard</h1><div class="muted">Live system overview.</div></div></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="cards" aria-label="System summary">
        <div class="stat"><span class="muted">Total tenders</span><strong><asp:Literal ID="TotalTenders" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Draft tenders</span><strong><asp:Literal ID="DraftTenders" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Departments</span><strong><asp:Literal ID="Departments" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Staff &amp; vendor users</span><strong><asp:Literal ID="TotalUsers" runat="server" /></strong></div>
    </section>
    <section class="card">
        <h2>Tenders by status</h2>
        <asp:GridView ID="StatusGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No tenders found.">
            <Columns><asp:BoundField DataField="CurrentStatus" HeaderText="Status" /><asp:BoundField DataField="StatusCount" HeaderText="Count" /></Columns>
        </asp:GridView>
    </section>
    <section class="card">
        <h2>All tenders</h2>
        <asp:GridView ID="RecentTendersGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No tenders found.">
            <Columns>
                <asp:BoundField DataField="TenderNumber" HeaderText="Tender ID" />
                <asp:BoundField DataField="Title" HeaderText="Tender Title" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Pending stage" />
                <asp:BoundField DataField="CreatedDate" HeaderText="Created" DataFormatString="{0:dd MMM yyyy}" />
                <asp:HyperLinkField Text="View" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Action" />
            </Columns>
        </asp:GridView>
    </section>
    <section class="card admin-links">
        <h2>Administration</h2>
        <a class="button secondary" href="<%= ResolveUrl("~/Pages/AdminUsers.aspx") %>">Manage users</a>
        <a class="button secondary" href="<%= ResolveUrl("~/Pages/AdminDepartments.aspx") %>">Manage departments</a>
        <a class="button secondary" href="<%= ResolveUrl("~/Pages/AdminAnnouncements.aspx") %>">Manage announcements</a>
    </section>
</asp:Content>
