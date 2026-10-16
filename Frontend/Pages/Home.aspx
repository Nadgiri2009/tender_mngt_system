<%@ Page Title="Tender Management Portal" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="DepartmentModule.Pages.Home" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Solapur Municipal Corporation Tender Portal</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="Message" runat="server" />
    <section class="cards home-statistics" aria-label="Portal statistics">
        <div class="stat"><span class="muted">Active tenders</span><strong><asp:Literal ID="ActiveTenders" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Tenders published</span><strong><asp:Literal ID="PublishedTenders" runat="server" /></strong></div>
        <div class="stat"><span class="muted">Registered contractors</span><strong><asp:Literal ID="RegisteredContractors" runat="server" /></strong></div>
    </section>
    <section class="card">
        <div class="page-heading"><div><h2>Published tenders</h2><p class="muted">View tender details and check whether applications are open.</p></div></div>
        <div class="table-wrap tender-list-wrap">
        <asp:GridView ID="TendersGrid" runat="server" AutoGenerateColumns="false" GridLines="None" CssClass="tender-list" EmptyDataText="No published tenders are currently available.">
            <Columns>
                <asp:TemplateField HeaderText="Sr. No.">
                    <ItemTemplate><%# TenderSerialNumber(Container.DataItemIndex) %></ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="TenderNumber" HeaderText="Tender No." />
                <asp:BoundField DataField="Title" HeaderText="Tender title" />
                <asp:BoundField DataField="TenderType" HeaderText="Category" />
                <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                <asp:BoundField DataField="PublicationDate" HeaderText="Publication date" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="SubmissionStartDate" HeaderText="Submission start" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                <asp:BoundField DataField="SubmissionDeadline" HeaderText="Submission deadline" DataFormatString="{0:dd MMM yyyy}" />
                <asp:BoundField DataField="CurrentStatus" HeaderText="Status" />
                <asp:TemplateField HeaderText="Action">
                    <ItemTemplate>
                        <asp:HyperLink ID="ViewTenderLink" runat="server" CssClass="button secondary tender-details-link"
                            Text="View details" NavigateUrl='<%# ResolveUrl("~/Pages/TenderDetails.aspx?id=" + Eval("TenderId")) %>' />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
        </div>
        <nav class="pagination tender-pagination" aria-label="Tender pages">
            <asp:LinkButton ID="PreviousPageButton" runat="server" CssClass="button secondary pagination-control" Text="Previous" OnClick="PreviousPageButton_Click" />
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
            <asp:LinkButton ID="NextPageButton" runat="server" CssClass="button secondary pagination-control" Text="Next" OnClick="NextPageButton_Click" />
            <asp:Literal ID="PageSummary" runat="server" />
        </nav>
    </section>
    <section class="card" id="announcements">
        <div class="page-heading"><div><h2>Notices and Announcements</h2><p class="muted">Official updates from the tender management system.</p></div></div>
        <div class="table-wrap">
        <asp:GridView ID="AnnouncementsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="There are no current announcements.">
            <Columns>
                <asp:BoundField DataField="Title" HeaderText="Notice" />
                <asp:BoundField DataField="Body" HeaderText="Details" />
                <asp:BoundField DataField="PublishDate" HeaderText="Published" DataFormatString="{0:dd MMM yyyy}" />
                <asp:BoundField DataField="ExpiryDate" HeaderText="Expiry" DataFormatString="{0:dd MMM yyyy}" />
            </Columns>
        </asp:GridView>
        </div>
    </section>
</asp:Content>
