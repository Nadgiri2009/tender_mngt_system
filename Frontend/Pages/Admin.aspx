<%@ Page Title="Admin Dashboard" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="DepartmentModule.Pages.AdminDashboard" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Admin Dashboard</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Main Administrator</h1><div class="muted">Live system overview and recent activity.</div></div></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="cards dashboard-statistics" aria-label="System summary">
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx") %>"><span class="muted">Total tenders</span><strong><asp:Literal ID="TotalTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Approved") %>"><span class="muted">Approved</span><strong><asp:Literal ID="ApprovedTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Pending") %>"><span class="muted">Pending</span><strong><asp:Literal ID="PendingTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Rejected") %>"><span class="muted">Rejected</span><strong><asp:Literal ID="RejectedTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/Tenders.aspx?bucket=Under%20Review") %>"><span class="muted">Under review</span><strong><asp:Literal ID="UnderReviewTenders" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/AdminUsers.aspx") %>"><span class="muted">Registered contractors</span><strong><asp:Literal ID="TotalContractors" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/AdminUsers.aspx") %>"><span class="muted">Active contractors</span><strong><asp:Literal ID="ActiveContractors" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/AdminUsers.aspx") %>"><span class="muted">Total users</span><strong><asp:Literal ID="TotalUsers" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/AdminUsers.aspx") %>"><span class="muted">Active user accounts</span><strong><asp:Literal ID="ActiveUsers" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/AdminDepartments.aspx") %>"><span class="muted">Departments</span><strong><asp:Literal ID="Departments" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/AdminUsers.aspx") %>"><span class="muted">Inactive contractors</span><strong><asp:Literal ID="InactiveContractors" runat="server" /></strong></a>
        <a class="stat" href="<%= ResolveUrl("~/Pages/AdminUsers.aspx") %>"><span class="muted">Inactive users</span><strong><asp:Literal ID="InactiveUsers" runat="server" /></strong></a>
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
        <div class="page-heading"><div><h2>Recent tenders</h2></div><a href="Tenders.aspx">View all tenders</a></div>
        <div class="table-wrap"><asp:GridView ID="RecentTendersGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No tenders found.">
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
        </asp:GridView></div>
    </section>
    <section class="card">
        <div class="page-heading"><div><h2>Upcoming submission deadlines</h2></div><a href="Tenders.aspx">View tender list</a></div>
        <div class="table-wrap"><asp:GridView ID="UpcomingDeadlinesGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="There are no upcoming submission deadlines.">
            <Columns>
                <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Tender No." />
                <asp:BoundField DataField="Title" HeaderText="Title" />
                <asp:BoundField DataField="TenderType" HeaderText="Category" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="AssignedOfficer" HeaderText="RESP Officer" />
                <asp:BoundField DataField="SubmissionStartDate" HeaderText="Submission starts" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="SubmissionDeadline" HeaderText="Submission ends" DataFormatString="{0:dd MMM yyyy}" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Status" />
            </Columns>
        </asp:GridView></div>
    </section>
    <section class="card">
        <h2>All tenders and recent activity</h2>
        <div class="table-wrap">
        <asp:GridView ID="DashboardFeedGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
            CssClass="admin-feed" EmptyDataText="No tenders or recent activity found.">
            <Columns>
                <asp:TemplateField HeaderText="Sr. No.">
                    <HeaderStyle CssClass="feed-serial" />
                    <ItemStyle CssClass="feed-serial" />
                    <ItemTemplate><%# FeedSerialNumber(Container.DataItemIndex) %></ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="ActivityDate" HeaderText="Date" DataFormatString="{0:dd MMM yyyy HH:mm}"
                    HeaderStyle-CssClass="feed-date" ItemStyle-CssClass="feed-date" />
                <asp:BoundField DataField="FeedType" HeaderText="Type" />
                <asp:BoundField DataField="Area" HeaderText="Tender / area" />
                <asp:BoundField DataField="Details" HeaderText="Details" />
                <asp:BoundField DataField="Actor" HeaderText="By" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Status"
                    HeaderStyle-CssClass="feed-status" ItemStyle-CssClass="feed-status" />
                <asp:TemplateField HeaderText="Action">
                    <HeaderStyle CssClass="feed-action" />
                    <ItemStyle CssClass="feed-action" />
                    <ItemTemplate>
                        <asp:HyperLink ID="ViewTenderLink" runat="server" CssClass="button secondary tender-details-link"
                            Text="View" NavigateUrl='<%# ResolveUrl("~/Pages/TenderDetails.aspx?id=" + Eval("TenderId")) %>'
                            Visible='<%# Eval("TenderId") != DBNull.Value %>' />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
        </div>
        <nav class="pagination tender-pagination" aria-label="Dashboard feed pages">
            <asp:LinkButton ID="PreviousPageButton" runat="server" CssClass="button secondary pagination-control"
                Text="Previous" OnClick="PreviousPageButton_Click" />
            <div class="pagination-pages">
                <asp:Repeater ID="PageNumbers" runat="server" OnItemCommand="PageNumbers_ItemCommand">
                    <ItemTemplate>
                        <asp:LinkButton ID="PageButton" runat="server" CssClass="button secondary pagination-number"
                            Text='<%# Eval("PageNumber") %>' CommandArgument='<%# Eval("PageNumber") %>'
                            Visible='<%# !Convert.ToBoolean(Eval("IsCurrent")) %>' />
                        <asp:Label ID="CurrentPageNumber" runat="server" CssClass="pagination-current" aria-current="page"
                            Text='<%# Eval("PageNumber") %>' Visible='<%# Convert.ToBoolean(Eval("IsCurrent")) %>' />
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            <asp:LinkButton ID="NextPageButton" runat="server" CssClass="button secondary pagination-control"
                Text="Next" OnClick="NextPageButton_Click" />
            <asp:Literal ID="PageSummary" runat="server" />
        </nav>
    </section>
</asp:Content>
