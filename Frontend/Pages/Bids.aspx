<%@ Page Title="Tender Bids" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Bids.aspx.cs" Inherits="DepartmentModule.Pages.Bids" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Tender Bids</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Bids</h1><div class="muted"><asp:Literal ID="TenderNumber" runat="server" /> · <asp:Literal ID="TenderTitle" runat="server" /></div></div><asp:HyperLink ID="BackLink" runat="server" CssClass="button secondary">Tender details</asp:HyperLink></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <h2>Technical bids</h2>
        <asp:Panel ID="TechnicalSubmissionPanel" runat="server" CssClass="card">
            <h3>Submit technical bid</h3>
            <asp:Literal ID="ContractorProfileMessage" runat="server" />
            <div class="field" id="TechnicalContractorField" runat="server">
                <label for="TechnicalContractorSelect">Contractor</label>
                <asp:DropDownList ID="TechnicalContractorSelect" runat="server" />
            </div>
            <div class="field">
                <label for="TechnicalDocumentPathText">Document Path (optional)</label>
                <asp:TextBox ID="TechnicalDocumentPathText" runat="server" MaxLength="400" />
            </div>
            <asp:Button ID="SubmitTechnicalButton" runat="server" Text="Submit technical bid" OnClick="SubmitTechnicalButton_Click" />
        </asp:Panel>
        <div class="table-wrap"><asp:GridView ID="TechnicalGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No technical bids submitted yet.">
            <Columns>
                <asp:BoundField DataField="ContractorName" HeaderText="Contractor" />
                <asp:BoundField DataField="Status" HeaderText="Status" />
                <asp:BoundField DataField="SubmittedDate" HeaderText="Submitted" DataFormatString="{0:dd MMM yyyy HH:mm}" />
                <asp:BoundField DataField="Remarks" HeaderText="Remarks" />
            </Columns>
        </asp:GridView></div>
        <asp:Panel ID="EvaluatePanel" runat="server" CssClass="card">
            <h3>Evaluate a submitted technical bid</h3>
            <div class="row">
                <div class="field"><label for="TechnicalBidSelect">Bid</label><asp:DropDownList ID="TechnicalBidSelect" runat="server" /></div>
                <div class="field"><label for="TechnicalStatus">Decision</label><asp:DropDownList ID="TechnicalStatus" runat="server"><asp:ListItem Value="">Select decision</asp:ListItem><asp:ListItem Value="Qualified">Qualified</asp:ListItem><asp:ListItem Value="Disqualified">Disqualified</asp:ListItem></asp:DropDownList></div>
            </div>
            <div class="field"><label for="TechnicalRemarks">Remarks</label><asp:TextBox ID="TechnicalRemarks" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="2" /></div>
            <asp:Button ID="EvaluateButton" runat="server" Text="Record evaluation" OnClick="EvaluateButton_Click" />
        </asp:Panel>
    </section>
    <section class="card">
        <h2>Financial bids (lowest bid ranks first)</h2>
        <asp:Panel ID="FinancialSubmissionPanel" runat="server" CssClass="card">
            <h3>Submit financial bid</h3>
            <div class="field" id="FinancialContractorField" runat="server">
                <label for="FinancialContractorSelect">Contractor</label>
                <asp:DropDownList ID="FinancialContractorSelect" runat="server" />
            </div>
            <div class="field">
                <label for="FinancialBidAmountText">Bid Amount (₹)</label>
                <asp:TextBox ID="FinancialBidAmountText" runat="server" TextMode="Number" />
            </div>
            <asp:Button ID="SubmitFinancialButton" runat="server" Text="Submit financial bid" OnClick="SubmitFinancialButton_Click" />
        </asp:Panel>
        <div class="table-wrap"><asp:GridView ID="FinancialGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No financial bids submitted yet.">
            <Columns>
                <asp:BoundField DataField="Rank" HeaderText="Rank" DataFormatString="L{0}" />
                <asp:BoundField DataField="ContractorName" HeaderText="Contractor" />
                <asp:BoundField DataField="BidAmount" HeaderText="Bid Amount" DataFormatString="₹{0:N2}" />
                <asp:BoundField DataField="Status" HeaderText="Status" />
            </Columns>
        </asp:GridView></div>
        <asp:Panel ID="SelectPanel" runat="server" CssClass="card">
            <h3>Committee recommendation</h3>
            <div class="field"><label for="FinancialBidSelect">Select financial bid</label><asp:DropDownList ID="FinancialBidSelect" runat="server" /></div>
            <div class="field"><label for="SelectionRemarks">Remarks (optional)</label><asp:TextBox ID="SelectionRemarks" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="2" /></div>
            <asp:Button ID="SelectContractorButton" runat="server" Text="Recommend contractor" OnClick="SelectContractorButton_Click" />
        </asp:Panel>
    </section>
</asp:Content>
