<%@ Page Title="Department Dashboard" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DepartmentModule.Pages.DepartmentDashboard" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Department Dashboard</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading">
        <div><h1>Department Dashboard</h1><div class="muted">Tender processing and work-management overview.</div></div>
        <asp:HyperLink ID="NewTenderLink" runat="server" NavigateUrl="~/Pages/TenderForm.aspx" CssClass="button">New Tender</asp:HyperLink>
        <asp:HyperLink ID="WorkflowQueueLink" runat="server" NavigateUrl="~/Pages/TenderWorkflow.aspx" CssClass="button secondary" Visible="false">My workflow queue</asp:HyperLink>
    </div>
    <asp:Literal ID="Message" runat="server" />
    <asp:Panel ID="FinalWorkflowDashboardPanel" runat="server" Visible="false">
        <section class="cards dashboard-statistics" aria-label="Assigned workflow summary">
            <div class="stat"><span class="muted">Assigned tenders</span><strong><asp:Literal ID="FinalQueueCount" runat="server" /></strong></div>
            <div class="stat"><span class="muted">Verification stages</span><strong><asp:Literal ID="FinalVerificationCount" runat="server" /></strong></div>
            <div class="stat"><span class="muted">Shortfall actions</span><strong><asp:Literal ID="FinalShortfallCount" runat="server" /></strong></div>
            <div class="stat"><span class="muted">Approval stages</span><strong><asp:Literal ID="FinalApprovalCount" runat="server" /></strong></div>
        </section>
        <section class="card">
            <div class="page-heading"><div><h2>My assigned workflow queue</h2><div class="muted">Tenders are limited to your role and current workflow assignment.</div></div><a href="TenderWorkflow.aspx">Open queue</a></div>
            <div class="table-wrap">
                <asp:GridView ID="FinalWorkflowGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No tenders are currently assigned to you.">
                    <Columns>
                        <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderWorkflow.aspx?id={0}" HeaderText="Tender" />
                        <asp:BoundField DataField="Title" HeaderText="Title" />
                        <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                        <asp:BoundField DataField="StageName" HeaderText="Stage" />
                        <asp:BoundField DataField="CurrentStatus" HeaderText="Status" />
                    </Columns>
                </asp:GridView>
            </div>
        </section>
    </asp:Panel>
    <asp:Panel ID="LegacyDashboardPanel" runat="server">
    <section class="cards" aria-label="Tender summary">
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx") %>"><span class="muted">Total tenders</span><strong><asp:Literal ID="TotalTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Approved") %>"><span class="muted">Approved</span><strong><asp:Literal ID="ApprovedTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Pending") %>"><span class="muted">Pending</span><strong><asp:Literal ID="PendingCount" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Rejected") %>"><span class="muted">Rejected</span><strong><asp:Literal ID="RejectedTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Under%20Review") %>"><span class="muted">Under review</span><strong><asp:Literal ID="UnderReviewTenders" runat="server" /></strong></a>
    </section>
    <asp:Panel ID="LifecycleSummaryPanel" runat="server" CssClass="cards dashboard-statistics">
        <a class="stat" href="<%= ResolveUrl("~/Pages/Approvals.aspx") %>"><span class="muted">Pending approvals</span><strong><asp:Literal ID="LifecyclePendingApprovals" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/TenderLifecycle.aspx") %>"><span class="muted">Tenders awaiting publication</span><strong><asp:Literal ID="LifecycleAwaitingPublication" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx") %>"><span class="muted">Currently published</span><strong><asp:Literal ID="LifecyclePublished" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/TenderLifecycle.aspx") %>"><span class="muted">Extension requests</span><strong><asp:Literal ID="LifecycleExtensions" runat="server" /></strong></a>
        <asp:PlaceHolder ID="LifecycleReviewSummaryPanel" runat="server">
        <a class="stat" href="<%= ResolveUrl("~/Pages/TenderEvaluation.aspx") %>"><span class="muted">Pending bid openings</span><strong><asp:Literal ID="LifecycleBidOpenings" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/TenderEvaluation.aspx") %>"><span class="muted">Awaiting evaluation</span><strong><asp:Literal ID="LifecycleAwaitingEvaluation" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/TenderEvaluation.aspx") %>"><span class="muted">Under evaluation</span><strong><asp:Literal ID="LifecycleUnderEvaluation" runat="server" /></strong></a>
        </asp:PlaceHolder>
    </asp:Panel>
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
                <asp:BoundField DataField="TenderCategory" HeaderText="Category" />
                <asp:BoundField DataField="TenderType" HeaderText="Tender method" />
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
        <div class="page-heading"><div><h2>Tenders by category</h2><div class="muted">Historical tenders without a category remain uncategorized.</div></div></div>
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
                <asp:BoundField DataField="TenderCategory" HeaderText="Category" />
                <asp:BoundField DataField="TenderType" HeaderText="Tender method" />
                <asp:BoundField DataField="AssignedOfficer" HeaderText="RESP Officer" />
                <asp:BoundField DataField="SubmissionStartDate" HeaderText="Submission starts" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="SubmissionDeadline" HeaderText="Submission ends" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Status" />
            </Columns>
        </asp:GridView>
    </section>
    </asp:Panel>
</asp:Content>
