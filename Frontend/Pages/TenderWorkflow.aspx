<%@ Page Title="Finalized Tender Workflow" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="TenderWorkflow.aspx.cs" Inherits="DepartmentModule.Pages.TenderWorkflow" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Finalized Tender Workflow</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Finalized tender workflow</h1><div class="muted">Assigned tenders for the current role and department.</div></div><a href="Default.aspx" class="button secondary">Dashboard</a></div>
    <asp:Literal ID="Message" runat="server" />
    <asp:Panel ID="QueuePanel" runat="server" CssClass="card">
        <h2>My assigned queue</h2>
        <div class="table-wrap">
            <asp:GridView ID="WorkflowQueue" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No tenders are currently assigned to you.">
                <Columns>
                    <asp:HyperLinkField DataTextField="TenderNumber" DataNavigateUrlFields="TenderId" DataNavigateUrlFormatString="TenderWorkflow.aspx?id={0}" HeaderText="Tender" />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                    <asp:BoundField DataField="StageName" HeaderText="Assigned stage" />
                    <asp:BoundField DataField="CurrentStatus" HeaderText="Status" />
                    <asp:BoundField DataField="AssignedAtUtc" HeaderText="Assigned" DataFormatString="{0:dd MMM yyyy HH:mm}" />
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>
    <asp:Panel ID="DetailPanel" runat="server" Visible="false">
        <section class="card">
            <div class="page-heading"><div><h2><asp:Literal ID="TenderNumber" runat="server" /> — <asp:Literal ID="TenderTitle" runat="server" /></h2><div class="muted"><asp:Literal ID="DepartmentName" runat="server" /> · <asp:Literal ID="StageName" runat="server" /></div></div><a class="button secondary" href="TenderDetails.aspx?id=<%= Request.QueryString["id"] %>">Tender details</a></div>
            <p><strong>Current status:</strong> <asp:Literal ID="CurrentStatus" runat="server" /></p>
            <p><strong>Estimated cost:</strong> ₹<asp:Literal ID="EstimatedAmount" runat="server" /></p>
        </section>
        <asp:Panel ID="SubmitDraftPanel" runat="server" CssClass="card" Visible="false">
            <h2>Submit tender for publication processing</h2>
            <p class="help">Submission is blocked until a BOQ item and all four required tender document categories are present.</p>
            <div class="field"><label for="RequiredBidDocumentsText">Required bidder technical documents</label>
                <span class="help">Enter one required document type per line. Contractors will see this checklist before submitting bids.</span>
                <asp:TextBox ID="RequiredBidDocumentsText" runat="server" TextMode="MultiLine" MaxLength="8000" Rows="6" /></div>
            <div class="field"><label for="SubmitRemarksText">Remarks</label><asp:TextBox ID="SubmitRemarksText" runat="server" TextMode="MultiLine" MaxLength="2000" Rows="3" /></div>
            <asp:Button ID="SubmitDraftButton" runat="server" Text="Submit tender" OnClick="SubmitDraftButton_Click" />
        </asp:Panel>
        <asp:Panel ID="PublishPanel" runat="server" CssClass="card" Visible="false">
            <h2>Publication schedule</h2>
            <div class="row">
                <div class="field"><label for="PublicationStartText">Publication start (IST)</label><asp:TextBox ID="PublicationStartText" runat="server" TextMode="DateTimeLocal" /></div>
                <div class="field"><label for="PublicationCloseText">Publication closing date and time (IST)</label><asp:TextBox ID="PublicationCloseText" runat="server" TextMode="DateTimeLocal" /></div>
            </div>
            <div class="field"><label for="PublishRemarksText">Remarks</label><asp:TextBox ID="PublishRemarksText" runat="server" TextMode="MultiLine" MaxLength="2000" Rows="3" /></div>
            <asp:Button ID="PublishButton" runat="server" Text="Publish tender" OnClick="PublishButton_Click" />
        </asp:Panel>
        <asp:Panel ID="PrimaryOpeningPanel" runat="server" CssClass="card" Visible="false">
            <h2>Initial technical bid opening</h2>
            <p class="help">This records the initial technical opening only. Financial bid information remains unavailable.</p>
            <div class="field"><label for="PrimaryOpenRemarksText">Opening remarks</label>
                <asp:TextBox ID="PrimaryOpenRemarksText" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="3" /></div>
            <asp:Button ID="OpenTechnicalBidsButton" runat="server" Text="Record technical bid opening"
                OnClick="OpenTechnicalBidsButton_Click" />
        </asp:Panel>
        <asp:Panel ID="PrimaryVerificationPanel" runat="server" CssClass="card" Visible="false">
            <h2>JE primary document verification</h2>
            <p class="help">Review technical and eligibility documents only. Each submitted document and each bidder requires a recorded review before forwarding.</p>
            <h3>Bidder registration and primary review</h3>
            <div class="table-wrap">
                <asp:GridView ID="PrimaryBidderGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
                    DataKeyNames="TechnicalBidId,ContractorId" OnRowCommand="PrimaryBidderGrid_RowCommand"
                    EmptyDataText="No technical bids were received.">
                    <Columns>
                        <asp:BoundField DataField="ContractorName" HeaderText="Contractor" />
                        <asp:BoundField DataField="RegistrationNo" HeaderText="Registration" />
                        <asp:BoundField DataField="ContactNo" HeaderText="Contact" />
                        <asp:BoundField DataField="Email" HeaderText="Email" />
                        <asp:BoundField DataField="SubmittedDate" HeaderText="Bid submitted" DataFormatString="{0:dd MMM yyyy HH:mm}" />
                        <asp:BoundField DataField="DocumentCount" HeaderText="Documents" />
                        <asp:BoundField DataField="PrimaryDecision" HeaderText="JE status" />
                        <asp:BoundField DataField="PrimaryRemarks" HeaderText="Previous JE remarks" />
                        <asp:TemplateField HeaderText="Bidder review"><ItemTemplate>
                            <asp:TextBox ID="PrimaryBidderRemarksText" runat="server" TextMode="MultiLine" MaxLength="2000"
                                Rows="2" Text='<%# Eval("PrimaryRemarks") %>' />
                            <asp:Button ID="RecordPrimaryBidderReviewButton" runat="server" Text="Record bidder review"
                                CommandName="RecordPrimaryBidderReview" CommandArgument='<%# Container.DataItemIndex %>'
                                Visible='<%# String.IsNullOrEmpty(Convert.ToString(Eval("PrimaryDecision"))) %>' />
                        </ItemTemplate></asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
            <h3>Submitted technical documents</h3>
            <div class="table-wrap">
                <asp:GridView ID="PrimaryDocumentGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
                    DataKeyNames="TechnicalBidDocumentId,ContractorId,DocumentType,IsMissing" OnRowCommand="PrimaryDocumentGrid_RowCommand"
                    EmptyDataText="No technical bid documents were uploaded. Record bidder reviews for missing-document cases.">
                    <Columns>
                        <asp:BoundField DataField="ContractorName" HeaderText="Contractor" />
                        <asp:BoundField DataField="DocumentType" HeaderText="Document type" />
                        <asp:TemplateField HeaderText="Document"><ItemTemplate>
                            <asp:HyperLink ID="PrimaryDocumentPreviewLink" runat="server" Text="Preview"
                                Visible='<%# !Convert.ToBoolean(Eval("IsMissing")) && SupportsInlinePreview(Eval("FileName")) %>'
                                NavigateUrl='<%# ResolveUrl("~/Pages/DocumentDownload.aspx?technicalBidDocumentId=" + Eval("TechnicalBidDocumentId") + "&amp;preview=true") %>' />
                            <asp:HyperLink ID="PrimaryDocumentDownloadLink" runat="server" Text='<%# Eval("FileName") %>'
                                Visible='<%# !Convert.ToBoolean(Eval("IsMissing")) %>'
                                NavigateUrl='<%# ResolveUrl("~/Pages/DocumentDownload.aspx?technicalBidDocumentId=" + Eval("TechnicalBidDocumentId")) %>' />
                        </ItemTemplate></asp:TemplateField>
                        <asp:BoundField DataField="UploadedDate" HeaderText="Uploaded" DataFormatString="{0:dd MMM yyyy HH:mm}" />
                        <asp:BoundField DataField="VerificationStatus" HeaderText="Previous decision" />
                        <asp:TemplateField HeaderText="JE decision"><ItemTemplate>
                            <asp:DropDownList ID="PrimaryDocumentDecision" runat="server" SelectedValue='<%# Eval("VerificationStatus") %>'>
                                <asp:ListItem Value="">Select decision</asp:ListItem>
                                <asp:ListItem Value="Verified">Verified</asp:ListItem>
                                <asp:ListItem Value="Not Verified">Not Verified</asp:ListItem>
                            </asp:DropDownList>
                            <asp:TextBox ID="PrimaryDocumentRemarksText" runat="server" TextMode="MultiLine" MaxLength="2000"
                                Rows="2" Text='<%# Eval("VerificationRemarks") %>' />
                            <asp:Button ID="RecordPrimaryDocumentButton" runat="server" Text="Save decision"
                                CommandName="RecordPrimaryDocument" CommandArgument='<%# Container.DataItemIndex %>'
                                Visible='<%# String.IsNullOrEmpty(Convert.ToString(Eval("VerificationStatus"))) %>' />
                        </ItemTemplate></asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
            <div class="field"><label for="PrimaryCompletionRemarksText">Verification completion remarks</label>
                <asp:TextBox ID="PrimaryCompletionRemarksText" runat="server" TextMode="MultiLine" MaxLength="2000" Rows="2" /></div>
            <asp:Button ID="CompletePrimaryVerificationButton" runat="server" Text="Complete primary verification"
                OnClick="CompletePrimaryVerificationButton_Click" />
        </asp:Panel>
        <asp:Panel ID="PrimaryForwardPanel" runat="server" CssClass="card" Visible="false">
            <h2>Forward to Tender Cell</h2>
            <p class="help">Tender Cell will conduct a separate secondary review.</p>
            <div class="field"><label for="PrimaryForwardRemarksText">Forwarding remarks</label>
                <asp:TextBox ID="PrimaryForwardRemarksText" runat="server" TextMode="MultiLine" MaxLength="2000" Rows="2" /></div>
            <asp:Button ID="ForwardToTenderCellButton" runat="server" Text="Forward for secondary verification"
                OnClick="ForwardToTenderCellButton_Click" />
        </asp:Panel>
        <section class="card">
            <h2>Immutable workflow history</h2>
            <div class="table-wrap">
                <asp:GridView ID="HistoryGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No workflow history has been recorded.">
                    <Columns>
                        <asp:BoundField DataField="ActionAtUtc" HeaderText="Date" DataFormatString="{0:dd MMM yyyy HH:mm}" />
                        <asp:BoundField DataField="StageName" HeaderText="Stage" />
                        <asp:BoundField DataField="PreviousStatus" HeaderText="Previous status" />
                        <asp:BoundField DataField="NewStatus" HeaderText="New status" />
                        <asp:BoundField DataField="ActionName" HeaderText="Action" />
                        <asp:BoundField DataField="Actor" HeaderText="By" />
                        <asp:BoundField DataField="Remarks" HeaderText="Remarks" />
                    </Columns>
                </asp:GridView>
            </div>
        </section>
    </asp:Panel>
</asp:Content>
