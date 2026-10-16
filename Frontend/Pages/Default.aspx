<%@ Page Title="Department Dashboard" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DepartmentModule.Pages.DepartmentDashboard" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Department Dashboard</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading">
        <div><h1>Department Dashboard</h1><div class="muted">Tender processing and work-management overview.</div></div>
        <asp:HyperLink ID="NewTenderLink" runat="server" NavigateUrl="~/Pages/TenderForm.aspx" CssClass="button">New Tender</asp:HyperLink>
    </div>
    <asp:Literal ID="Message" runat="server" />
    <section class="cards" aria-label="Tender summary">
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx") %>"><span class="muted">Total tenders</span><strong><asp:Literal ID="TotalTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Approved") %>"><span class="muted">Approved</span><strong><asp:Literal ID="ApprovedTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Pending") %>"><span class="muted">Pending</span><strong><asp:Literal ID="PendingCount" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Rejected") %>"><span class="muted">Rejected</span><strong><asp:Literal ID="RejectedTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Under%20Review") %>"><span class="muted">Under review</span><strong><asp:Literal ID="UnderReviewTenders" runat="server" /></strong></a>
    </section>
    <asp:Panel ID="PendingApprovalsPanel" runat="server">
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
    </asp:Panel>
    <section class="card">
        <div class="page-heading"><div><h2><asp:Literal ID="RecentHeading" runat="server" /></h2></div><a href="Tenders.aspx">Browse tenders</a></div>
        <asp:GridView ID="RecentGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No tenders found.">
            <Columns>
                <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Tender No." />
                <asp:BoundField DataField="Title" HeaderText="Title" />
                <asp:BoundField DataField="TenderType" HeaderText="Category" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="AssignedOfficer" HeaderText="RESP Officer" />
                <asp:BoundField DataField="PublicationDate" HeaderText="Published" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="SubmissionStartDate" HeaderText="Submission starts" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="SubmissionDeadline" HeaderText="Submission ends" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Status" />
            </Columns>
        </asp:GridView>
    </section>
    <section class="card">
        <div class="page-heading"><div><h2>Tenders by category</h2><div class="muted">Counts use the existing tender type values.</div></div></div>
        <asp:GridView ID="CategoryStatsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No category statistics are available.">
            <Columns>
                <asp:BoundField DataField="Category" HeaderText="Category" />
                <asp:BoundField DataField="TenderCount" HeaderText="Tenders" />
            </Columns>
        </asp:GridView>
    </section>
    <section class="card">
        <div class="page-heading"><div><h2>Upcoming submission deadlines</h2></div><a href="Tenders.aspx">View tender list</a></div>
        <asp:GridView ID="UpcomingGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="There are no upcoming submission deadlines.">
            <Columns>
                <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Tender No." />
                <asp:BoundField DataField="Title" HeaderText="Title" />
                <asp:BoundField DataField="TenderType" HeaderText="Category" />
                <asp:BoundField DataField="AssignedOfficer" HeaderText="RESP Officer" />
                <asp:BoundField DataField="SubmissionStartDate" HeaderText="Submission starts" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="SubmissionDeadline" HeaderText="Submission ends" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Status" />
            </Columns>
        </asp:GridView>
    </section>
</asp:Content>
