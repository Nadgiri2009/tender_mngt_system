<%@ Page Title="Reports" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="AdminReports.aspx.cs" Inherits="DepartmentModule.Pages.AdminReports" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Reports</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Reports and analytics</h1><div class="muted">Build filtered reports from live database records.</div></div><a class="button secondary" href="Admin.aspx">Back to administration</a></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="ReportTypeSelect">Report</asp:Label><asp:DropDownList ID="ReportTypeSelect" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ReportTypeSelect_SelectedIndexChanged">
                <asp:ListItem Text="Tender status and department report" Value="tenders" />
                <asp:ListItem Text="Upcoming tender deadlines" Value="deadlines" />
                <asp:ListItem Text="Contractor registration report" Value="contractors" />
                <asp:ListItem Text="User access and login activity" Value="users" />
            </asp:DropDownList></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="DepartmentSelect">Department</asp:Label><asp:DropDownList ID="DepartmentSelect" runat="server" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="StatusSelect">Status</asp:Label><asp:DropDownList ID="StatusSelect" runat="server" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="FromDateText">From date</asp:Label><asp:TextBox ID="FromDateText" runat="server" TextMode="Date" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="ToDateText">To date</asp:Label><asp:TextBox ID="ToDateText" runat="server" TextMode="Date" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="SearchText">Search</asp:Label><asp:TextBox ID="SearchText" runat="server" MaxLength="100" /></div>
        </div>
        <div class="admin-actions">
            <asp:Button ID="RunReportButton" runat="server" Text="Run report" OnClick="RunReportButton_Click" />
            <asp:Button ID="ExportButton" runat="server" Text="Export CSV" CssClass="button secondary" CausesValidation="false" OnClick="ExportButton_Click" />
        </div>
    </section>
    <section class="card">
        <h2>Report results</h2>
        <div class="table-wrap">
            <asp:GridView ID="ReportGrid" runat="server" AutoGenerateColumns="true" AllowPaging="true" PageSize="20" GridLines="None" OnPageIndexChanging="ReportGrid_PageIndexChanging" EmptyDataText="Run a report to display matching records." />
        </div>
        <div class="pagination"><asp:Literal ID="ResultSummary" runat="server" /><span>Page size: 20</span></div>
    </section>
</asp:Content>
