<%@ Page Title="Tender Publication and Extensions" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="TenderLifecycle.aspx.cs" Inherits="DepartmentModule.Pages.TenderLifecycle" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Tender Publication and Extensions</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading">
        <div><h1>Tender publication and extensions</h1><div class="muted">Publication times are managed in Solapur local time (IST).</div></div>
        <asp:HyperLink ID="EvaluationLink" runat="server" NavigateUrl="~/Pages/TenderEvaluation.aspx" CssClass="button secondary">Bid opening and evaluation</asp:HyperLink>
    </div>
    <asp:Literal ID="Message" runat="server" />

    <asp:Panel ID="PublishedTendersPanel" runat="server" CssClass="card">
        <h2>Published tenders</h2>
        <p class="help">JE users can request an extension for a tender they own. CAFO users can configure a schedule and review extension requests.</p>
        <div class="table-wrap">
            <asp:GridView ID="PublishedTendersGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
                EmptyDataText="No published tenders are available for your role and department.">
                <Columns>
                    <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderLifecycle.aspx?tenderId={0}" HeaderText="Tender" />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                    <asp:BoundField DataField="PublicationStartUtc" HeaderText="Starts (IST)" />
                    <asp:BoundField DataField="OriginalClosingUtc" HeaderText="Original close (IST)" />
                    <asp:BoundField DataField="CurrentClosingUtc" HeaderText="Current close (IST)" />
                    <asp:BoundField DataField="ApprovedExtensions" HeaderText="Approved extensions" />
                    <asp:BoundField DataField="PendingExtensions" HeaderText="Pending requests" />
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>

    <asp:Panel ID="SchedulePanel" runat="server" CssClass="card" Visible="false">
        <h2>Configure and approve publication period</h2>
        <p><strong><asp:Literal ID="SelectedTender" runat="server" /></strong></p>
        <div class="row">
            <div class="field"><label for="StartDate">Publication start date (IST)</label><asp:TextBox ID="StartDate" runat="server" TextMode="Date" /></div>
            <div class="field"><label for="StartTime">Publication start time (IST)</label><asp:TextBox ID="StartTime" runat="server" TextMode="Time" /></div>
            <div class="field"><label for="ClosingDate">Publication closing date (IST)</label><asp:TextBox ID="ClosingDate" runat="server" TextMode="Date" /></div>
            <div class="field"><label for="ClosingTime">Publication closing time (IST)</label><asp:TextBox ID="ClosingTime" runat="server" TextMode="Time" /></div>
        </div>
        <div class="field"><label for="ScheduleRemarks">Publication remarks</label><asp:TextBox ID="ScheduleRemarks" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="2" /></div>
        <asp:Button ID="SaveScheduleButton" runat="server" Text="Approve publication schedule" OnClick="SaveScheduleButton_Click"
            OnClientClick="return confirm('Approve or update this publication schedule?');" />
        <asp:Literal ID="PublicationPeriod" runat="server" />
    </asp:Panel>

    <asp:Panel ID="ExtensionRequestPanel" runat="server" CssClass="card" Visible="false">
        <h2>Request publication extension</h2>
        <p class="help">Up to three approved extensions are allowed. The request must be submitted before the current close. Both documents are required.</p>
        <div class="row">
            <div class="field"><label for="ProposedClosingDate">Proposed closing date (IST)</label><asp:TextBox ID="ProposedClosingDate" runat="server" TextMode="Date" /></div>
            <div class="field"><label for="ProposedClosingTime">Proposed closing time (IST)</label><asp:TextBox ID="ProposedClosingTime" runat="server" TextMode="Time" /></div>
        </div>
        <div class="field"><label for="ExtensionReason">Reason for extension</label><asp:TextBox ID="ExtensionReason" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="3" /></div>
        <div class="row">
            <div class="field"><label for="ApplicationFile">Extension application</label><asp:FileUpload ID="ApplicationFile" runat="server" /></div>
            <div class="field"><label for="SupportingFile">Supporting document</label><asp:FileUpload ID="SupportingFile" runat="server" /></div>
        </div>
        <asp:Button ID="SubmitExtensionButton" runat="server" Text="Submit extension request" OnClick="SubmitExtensionButton_Click"
            OnClientClick="return confirm('Submit this extension request to CAFO for review?');" />
    </asp:Panel>

    <asp:Panel ID="PendingRequestsPanel" runat="server" CssClass="card">
        <h2>Extension requests awaiting CAFO review</h2>
        <div class="field"><label for="ReviewRemarks">Review remarks</label><asp:TextBox ID="ReviewRemarks" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="2" /></div>
        <div class="table-wrap">
            <asp:GridView ID="PendingRequestsGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
                OnRowCommand="PendingRequestsGrid_RowCommand" EmptyDataText="No extension requests are awaiting review.">
                <Columns>
                    <asp:BoundField DataField="TenderNumber" HeaderText="Tender" />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                    <asp:BoundField DataField="ExtensionNumber" HeaderText="Extension no." />
                    <asp:BoundField DataField="OriginalClosingUtc" HeaderText="Current close (IST)" />
                    <asp:BoundField DataField="ProposedClosingUtc" HeaderText="Proposed close (IST)" />
                    <asp:BoundField DataField="Reason" HeaderText="Reason" />
                    <asp:BoundField DataField="SubmittedByName" HeaderText="Submitted by" />
                    <asp:HyperLinkField DataTextField="ApplicationFileName" DataNavigateUrlFields="ExtensionRequestId" DataNavigateUrlFormatString="DocumentDownload.aspx?extensionRequestId={0}&amp;kind=application" HeaderText="Application" />
                    <asp:HyperLinkField DataTextField="SupportingFileName" DataNavigateUrlFields="ExtensionRequestId" DataNavigateUrlFormatString="DocumentDownload.aspx?extensionRequestId={0}&amp;kind=supporting" HeaderText="Supporting document" />
                    <asp:TemplateField HeaderText="Decision"><ItemTemplate>
                        <asp:LinkButton ID="ApproveExtension" runat="server" Text="Approve" CommandName="ApproveExtension" CommandArgument='<%# Eval("ExtensionRequestId") %>' CssClass="button"
                            OnClientClick="return confirm('Approve this extension and update the closing time?');" />
                        <asp:LinkButton ID="RejectExtension" runat="server" Text="Reject" CommandName="RejectExtension" CommandArgument='<%# Eval("ExtensionRequestId") %>' CssClass="button secondary"
                            OnClientClick="return confirm('Reject this extension request?');" />
                    </ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>

    <asp:Panel ID="ExtensionHistoryPanel" runat="server" CssClass="card">
        <h2>Extension history</h2>
        <div class="table-wrap">
            <asp:GridView ID="ExtensionHistoryGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
                EmptyDataText="No extension requests have been submitted.">
                <Columns>
                    <asp:BoundField DataField="TenderNumber" HeaderText="Tender" />
                    <asp:BoundField DataField="ExtensionRequestId" HeaderText="Request ID" />
                    <asp:BoundField DataField="ExtensionNumber" HeaderText="Extension no." />
                    <asp:BoundField DataField="OriginalClosingUtc" HeaderText="Original close (IST)" />
                    <asp:BoundField DataField="ProposedClosingUtc" HeaderText="Proposed close (IST)" />
                    <asp:BoundField DataField="Reason" HeaderText="Reason" />
                    <asp:HyperLinkField DataTextField="ApplicationFileName" DataNavigateUrlFields="ExtensionRequestId" DataNavigateUrlFormatString="DocumentDownload.aspx?extensionRequestId={0}&amp;kind=application" HeaderText="Application" />
                    <asp:HyperLinkField DataTextField="SupportingFileName" DataNavigateUrlFields="ExtensionRequestId" DataNavigateUrlFormatString="DocumentDownload.aspx?extensionRequestId={0}&amp;kind=supporting" HeaderText="Supporting document" />
                    <asp:BoundField DataField="SubmittedByName" HeaderText="Submitted by" />
                    <asp:BoundField DataField="SubmittedAtUtc" HeaderText="Submitted (IST)" />
                    <asp:BoundField DataField="ReviewStatus" HeaderText="Decision" />
                    <asp:BoundField DataField="ReviewedByName" HeaderText="Reviewed by" />
                    <asp:BoundField DataField="ReviewedAtUtc" HeaderText="Reviewed (IST)" />
                    <asp:BoundField DataField="ReviewRemarks" HeaderText="Review remarks" />
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>
</asp:Content>
