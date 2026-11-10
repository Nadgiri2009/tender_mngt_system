<%@ Page Title="Bid Opening and Tender Evaluation" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="TenderEvaluation.aspx.cs" Inherits="DepartmentModule.Pages.TenderEvaluation" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Bid opening and evaluation</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading">
        <div><h1>Bid opening and tender evaluation</h1><div class="muted">Opening and evaluation dates are recorded in UTC and displayed in Solapur local time (IST).</div></div>
        <asp:HyperLink ID="PublicationLink" runat="server" NavigateUrl="~/Pages/TenderLifecycle.aspx" CssClass="button secondary">Publication and extensions</asp:HyperLink>
    </div>
    <asp:Literal ID="Message" runat="server" />

    <asp:Panel ID="OpeningPanel" runat="server" CssClass="card">
        <h2>Eligible for bid opening</h2>
        <p class="help">A tender appears here only after its approved current closing time has passed. Opening is one-time and recorded against your authenticated account.</p>
        <div class="row">
            <div class="field"><label for="OpeningDesignation">Opening officer designation</label><asp:TextBox ID="OpeningDesignation" runat="server" MaxLength="150" /></div>
            <div class="field"><label for="OpeningRemarks">Opening remarks</label><asp:TextBox ID="OpeningRemarks" runat="server" MaxLength="1000" /></div>
        </div>
        <div class="table-wrap">
            <asp:GridView ID="OpeningGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
                OnRowCommand="OpeningGrid_RowCommand" EmptyDataText="No published tenders are eligible for bid opening.">
                <Columns>
                    <asp:BoundField DataField="TenderNumber" HeaderText="Tender" />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                    <asp:BoundField DataField="OriginalClosingUtc" HeaderText="Original close (IST)" />
                    <asp:BoundField DataField="CurrentClosingUtc" HeaderText="Current close (IST)" />
                    <asp:TemplateField HeaderText="Action"><ItemTemplate>
                        <asp:LinkButton ID="OpenTender" runat="server" Text="Open bids" CommandName="OpenBids"
                            CommandArgument='<%# Eval("TenderId") %>' CssClass="button"
                            OnClientClick="return confirm('Record the bid opening now? This action cannot be undone.');" />
                    </ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>

    <asp:Panel ID="OpenedPanel" runat="server" CssClass="card">
        <h2>Opened tenders and evaluation queue</h2>
        <div class="table-wrap">
            <asp:GridView ID="EvaluationGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
                OnRowCommand="EvaluationGrid_RowCommand" EmptyDataText="There are no opened tenders in your department.">
                <Columns>
                    <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderDetails.aspx?id={0}" HeaderText="Tender" />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                    <asp:BoundField DataField="ActualOpeningUtc" HeaderText="Bid opening (IST)" />
                    <asp:BoundField DataField="EvaluationEligibleAtUtc" HeaderText="Evaluation eligible (IST)" />
                    <asp:BoundField DataField="EvaluationStatus" HeaderText="Evaluation status" />
                    <asp:BoundField DataField="AssignedTeam" HeaderText="Assigned team" />
                    <asp:BoundField DataField="CurrentStatus" HeaderText="Tender status" />
                    <asp:TemplateField HeaderText="Evaluation"><ItemTemplate>
                        <asp:LinkButton ID="SelectEvaluation" runat="server" Text="Open evaluation" CommandName="SelectEvaluation"
                            CommandArgument='<%# Eval("TenderId") %>' CssClass="button secondary" />
                    </ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>

    <asp:Panel ID="EvaluationDetailPanel" runat="server" CssClass="card" Visible="false">
        <h2>Evaluation workspace</h2>
        <p><strong><asp:Literal ID="EvaluationTenderTitle" runat="server" /></strong></p>
        <p><asp:Literal ID="EvaluationTiming" runat="server" /></p>
        <p><asp:HyperLink ID="BidDocumentsLink" runat="server" CssClass="button secondary">View opened bid documents</asp:HyperLink></p>
        <asp:Literal ID="EvaluationDocumentHistory" runat="server" />
        <div class="field"><label for="TechnicalOutcome">Technical evaluation outcome</label><asp:TextBox ID="TechnicalOutcome" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="2" /></div>
        <div class="field"><label for="FinancialOutcome">Financial evaluation outcome</label><asp:TextBox ID="FinancialOutcome" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="2" /></div>
        <div class="field"><label for="Observations">Evaluation observations</label><asp:TextBox ID="Observations" runat="server" TextMode="MultiLine" MaxLength="4000" Rows="4" /></div>
        <div class="field"><label for="Recommendation">Recommendation</label><asp:TextBox ID="Recommendation" runat="server" TextMode="MultiLine" MaxLength="2000" Rows="3" /></div>
        <div class="field"><label for="EvaluationReportFile">Evaluation report (PDF, Office, JPEG or PNG; max 20 MB)</label><asp:FileUpload ID="EvaluationReportFile" runat="server" /></div>
        <div class="row">
            <asp:Button ID="StartEvaluationButton" runat="server" Text="Start evaluation" OnClick="StartEvaluationButton_Click"
                OnClientClick="return confirm('Start the evaluation now?');" />
            <asp:Button ID="SaveEvaluationButton" runat="server" Text="Save evaluation" OnClick="SaveEvaluationButton_Click" />
            <asp:Button ID="ForwardEvaluationButton" runat="server" Text="Submit recommendation to Tender Committee" OnClick="ForwardEvaluationButton_Click"
                OnClientClick="return confirm('Submit this evaluation recommendation to the existing Tender Committee workflow?');" />
        </div>
        <p class="help">The configured one-day delay is enforced by SQL Server. Tender Committee and Competent Authority approvals continue through the existing workflow.</p>
    </asp:Panel>

    <asp:Panel ID="SettingsPanel" runat="server" CssClass="card" Visible="false">
        <h2>Evaluation start delay</h2>
        <div class="row">
            <div class="field"><label for="DelayDays">Days after actual bid opening</label><asp:TextBox ID="DelayDays" runat="server" TextMode="Number" min="0" max="365" /></div>
            <asp:Button ID="SaveDelayButton" runat="server" Text="Save evaluation delay" OnClick="SaveDelayButton_Click" />
        </div>
        <p class="help">Default: 1 day. Changes apply to tenders opened after the setting is saved. Existing eligibility times do not change.</p>
    </asp:Panel>
</asp:Content>
