<%@ Page Title="Department Dashboard" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DepartmentModule.Pages.DepartmentDashboard" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Department Dashboard</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading">
        <div><h1>Department Dashboard</h1><div class="muted">Tender processing and work-management overview.</div></div>
        <asp:HyperLink ID="NewTenderLink" runat="server" NavigateUrl="~/Pages/TenderForm.aspx" CssClass="button">New Tender</asp:HyperLink>
    </div>
    <asp:Literal ID="Message" runat="server" />
    <section class="cards" aria-label="Tender summary">
        <div class="stat"><span class="muted">All tenders</span><strong><asp:Literal ID="TotalTenders" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Published</span><strong><asp:Literal ID="PublishedTenders" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Needs action</span><strong><asp:Literal ID="PendingCount" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Completed</span><strong><asp:Literal ID="CompletedTenders" runat="server" /></strong></div>
    </section>
    <section class="card">
        <div class="page-heading"><div><h2>Pending my approval</h2><div class="muted">Tenders waiting for your role.</div></div><a href="Approvals.aspx">View queue</a></div>
        <asp:GridView ID="PendingGrid" runat="server" AutoGenerateColumns="false" CssClass="table-wrap" GridLines="None" EmptyDataText="Nothing is waiting for your approval.">
            <Columns>
                <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Tender No." />
                <asp:BoundField DataField="Title" HeaderText="Title" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Current Stage" />
            </Columns>
        </asp:GridView>
    </section>
    <section class="card">
        <div class="page-heading"><div><h2>Recently updated tenders</h2></div><a href="Tenders.aspx">Browse tenders</a></div>
        <asp:GridView ID="RecentGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No tenders found.">
            <Columns>
                <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Tender No." />
                <asp:BoundField DataField="Title" HeaderText="Title" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Status" />
                <asp:BoundField DataField="CreatedDate" HeaderText="Created" DataFormatString="{0:dd MMM yyyy}" />
            </Columns>
        </asp:GridView>
    </section>
</asp:Content>
