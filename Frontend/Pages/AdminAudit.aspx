<%@ Page Title="Audit Logs" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="AdminAudit.aspx.cs" Inherits="DepartmentModule.Pages.AdminAudit" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Audit Logs</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Audit logs</h1><div class="muted">Read-only history of recorded application and administrative activity.</div></div><a class="button secondary" href="Admin.aspx">Back to administration</a></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <div class="row">
            <div class="field"><asp:Label runat="server" AssociatedControlID="SearchText">Search action, user, record, or details</asp:Label><asp:TextBox ID="SearchText" runat="server" MaxLength="100" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="EntityText">Module / entity</asp:Label><asp:TextBox ID="EntityText" runat="server" MaxLength="100" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="FromDateText">From date</asp:Label><asp:TextBox ID="FromDateText" runat="server" TextMode="Date" /></div>
            <div class="field"><asp:Label runat="server" AssociatedControlID="ToDateText">To date</asp:Label><asp:TextBox ID="ToDateText" runat="server" TextMode="Date" /></div>
            <asp:Button ID="FilterButton" runat="server" Text="Apply filters" CssClass="button" OnClick="FilterButton_Click" />
        </div>
        <div class="table-wrap">
            <asp:GridView ID="AuditGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No audit entries match these filters.">
                <Columns>
                    <asp:BoundField DataField="CreatedDate" HeaderText="Time (UTC)" DataFormatString="{0:dd MMM yyyy HH:mm:ss}" />
                    <asp:BoundField DataField="Actor" HeaderText="User" />
                    <asp:BoundField DataField="Action" HeaderText="Action" />
                    <asp:BoundField DataField="EntityName" HeaderText="Module / entity" />
                    <asp:BoundField DataField="EntityId" HeaderText="Record ID" />
                    <asp:BoundField DataField="Details" HeaderText="Details" />
                    <asp:BoundField DataField="IPAddress" HeaderText="IP address" />
                </Columns>
            </asp:GridView>
        </div>
        <div class="pagination">
            <asp:Button ID="PreviousButton" runat="server" Text="Previous" CssClass="button secondary" CausesValidation="false" OnClick="PreviousButton_Click" />
            <asp:Literal ID="PageSummary" runat="server" />
            <asp:Button ID="NextButton" runat="server" Text="Next" CssClass="button secondary" CausesValidation="false" OnClick="NextButton_Click" />
        </div>
    </section>
</asp:Content>
