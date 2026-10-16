<%@ Page Title="Tenders" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Tenders.aspx.cs" Inherits="DepartmentModule.Pages.Tenders" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Tenders</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading">
        <div><h1>Tenders</h1><div class="muted">Search, filter, sort, and page through tender records.</div></div>
        <asp:HyperLink ID="NewTenderLink" runat="server" NavigateUrl="~/Pages/TenderForm.aspx" CssClass="button">New Tender</asp:HyperLink>
    </div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <div class="row">
            <div class="field"><label for="SearchText">Search title or tender number</label><asp:TextBox ID="SearchText" runat="server" MaxLength="100" /></div>
            <div class="field"><label for="StatusFilter">Status</label><asp:DropDownList ID="StatusFilter" runat="server" /></div>
            <div class="field"><label for="CategoryFilter">Category</label><asp:DropDownList ID="CategoryFilter" runat="server" /></div>
            <div class="field"><label for="DateFrom">Deadline from</label><asp:TextBox ID="DateFrom" runat="server" TextMode="Date" /></div>
            <div class="field"><label for="DateTo">Deadline to</label><asp:TextBox ID="DateTo" runat="server" TextMode="Date" /></div>
            <div class="field"><label for="DepartmentFilter">Department</label><asp:DropDownList ID="DepartmentFilter" runat="server" /></div>
            <asp:Button ID="FilterButton" runat="server" Text="Apply filters" CssClass="button" OnClick="FilterButton_Click" />
        </div>
        <div class="table-wrap">
            <asp:GridView ID="TenderGrid" runat="server" AutoGenerateColumns="false" AllowPaging="true" AllowSorting="true"
                PageSize="10" GridLines="None" EmptyDataText="No tenders match your filters."
                OnPageIndexChanging="TenderGrid_PageIndexChanging" OnSorting="TenderGrid_Sorting">
                <Columns>
                    <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Tender No." SortExpression="TenderNumber" />
                    <asp:BoundField DataField="Title" HeaderText="Title" SortExpression="Title" />
                    <asp:BoundField DataField="TenderType" HeaderText="Category" />
                    <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                    <asp:BoundField DataField="AssignedOfficer" HeaderText="RESP Officer" />
                    <asp:BoundField DataField="PublicationDate" HeaderText="Published" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                    <asp:BoundField DataField="SubmissionStartDate" HeaderText="Submission starts" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                    <asp:BoundField DataField="SubmissionDeadline" HeaderText="Submission ends" DataFormatString="{0:dd MMM yyyy}" NullDisplayText="-" />
                    <asp:BoundField DataField="EstimatedAmount" HeaderText="Estimated Cost" DataFormatString="₹{0:N2}" SortExpression="Amount" />
                    <asp:TemplateField HeaderText="Status">
                        <ItemTemplate><span class='<%# TenderStatusCssClass(Eval("CurrentStatus")) %>'><%#: Eval("CurrentStatus") %></span></ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="CreatedDate" HeaderText="Created" DataFormatString="{0:dd MMM yyyy}" SortExpression="CreatedDate" />
                </Columns>
            </asp:GridView>
        </div>
        <div class="pagination"><asp:Literal ID="PageSummary" runat="server" /><span>Page size: 10</span></div>
    </section>
</asp:Content>
