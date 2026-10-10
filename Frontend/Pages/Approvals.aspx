<%@ Page Title="Pending Approvals" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Approvals.aspx.cs" Inherits="DepartmentModule.Pages.Approvals" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Pending Approvals</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Pending My Approval</h1><div class="muted">Current department workflow queue for role: <asp:Literal ID="RoleName" runat="server" /></div></div></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <div class="table-wrap">
            <asp:GridView ID="ApprovalGrid" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="10" GridLines="None" EmptyDataText="Nothing is waiting on your approval right now." OnPageIndexChanging="ApprovalGrid_PageIndexChanging">
                <Columns>
                    <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Tender No." />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                    <asp:BoundField DataField="EstimatedAmount" HeaderText="Estimated Cost" DataFormatString="₹{0:N2}" />
                    <asp:BoundField DataField="CurrentStatus" HeaderText="Current Stage" />
                    <asp:BoundField DataField="CreatedDate" HeaderText="Since" DataFormatString="{0:dd MMM yyyy}" />
                </Columns>
            </asp:GridView>
        </div>
    </section>
</asp:Content>
