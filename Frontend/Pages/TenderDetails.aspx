<%@ Page Title="Tender Details" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="TenderDetails.aspx.cs" Inherits="DepartmentModule.Pages.TenderDetails" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server"><asp:Literal ID="PageTitle" runat="server" /></asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading">
        <div><h1><asp:Literal ID="TenderTitle" runat="server" /></h1>
            <div class="muted"><asp:Literal ID="TenderNumber" runat="server" /> · <asp:Literal ID="DepartmentName" runat="server" /></div></div>
        <a href="Tenders.aspx" class="button secondary">Back to tenders</a>
    </div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <h2>Details</h2>
        <p><strong>Description</strong><br /><asp:Literal ID="Description" runat="server" /></p>
        <div class="row">
            <p><strong>Tender Type</strong><br /><asp:Literal ID="TenderType" runat="server" /></p>
            <p><strong>Estimated Cost</strong><br />₹<asp:Literal ID="EstimatedAmount" runat="server" /></p>
            <p><strong>Submission Deadline</strong><br /><asp:Literal ID="SubmissionDeadline" runat="server" /></p>
            <p><strong>Created By</strong><br /><asp:Literal ID="CreatedBy" runat="server" /></p>
            <p><strong>Created Date</strong><br /><asp:Literal ID="CreatedDate" runat="server" /></p>
            <p><strong>Status</strong><br /><span class="badge"><asp:Literal ID="CurrentStatus" runat="server" /></span></p>
        </div>
    </section>
    <section class="card">
        <h2>Bill of Quantities</h2>
        <div class="table-wrap">
            <asp:GridView ID="BoqGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No BOQ items added yet." OnRowCommand="BoqGrid_RowCommand">
                <Columns>
                    <asp:BoundField DataField="ItemName" HeaderText="Item" />
                    <asp:BoundField DataField="Description" HeaderText="Description" />
                    <asp:BoundField DataField="Unit" HeaderText="Unit" />
                    <asp:BoundField DataField="Quantity" HeaderText="Quantity" DataFormatString="{0:N3}" />
                    <asp:BoundField DataField="Rate" HeaderText="Rate" DataFormatString="₹{0:N2}" />
                    <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="₹{0:N2}" />
                    <asp:TemplateField><ItemTemplate><asp:LinkButton ID="DeleteItem" runat="server" Text="Remove" CommandName="RemoveItem" CommandArgument='<%# Eval("BOQId") %>' CssClass="button secondary" /></ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <asp:Panel ID="AddBoqPanel" runat="server" CssClass="card">
            <h3>Add BOQ item</h3>
            <div class="row">
                <div class="field"><label for="ItemName">Item name</label><asp:TextBox ID="ItemName" runat="server" MaxLength="200" /></div>
                <div class="field"><label for="ItemDescription">Description</label><asp:TextBox ID="ItemDescription" runat="server" MaxLength="500" /></div>
            </div>
            <div class="row">
                <div class="field"><label for="Quantity">Quantity</label><asp:TextBox ID="Quantity" runat="server" TextMode="Number" step="0.001" min="0.001" /></div>
                <div class="field"><label for="Unit">Unit</label><asp:TextBox ID="Unit" runat="server" MaxLength="20" /></div>
                <div class="field"><label for="Rate">Rate (₹)</label><asp:TextBox ID="Rate" runat="server" TextMode="Number" step="0.01" min="0.01" /></div>
                <asp:Button ID="AddBoqButton" runat="server" Text="Add item" OnClick="AddBoqButton_Click" />
            </div>
        </asp:Panel>
    </section>
    <section class="card">
        <h2>Attachments</h2>
        <div class="table-wrap"><asp:GridView ID="DocumentsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No attachments.">
            <Columns>
                <asp:HyperLinkField DataTextField="FileName" DataNavigateUrlFields="DocumentId" DataNavigateUrlFormatString="DocumentDownload.aspx?id={0}" HeaderText="File" />
                <asp:BoundField DataField="DocumentType" HeaderText="Type" />
                <asp:BoundField DataField="UploadedDate" HeaderText="Uploaded" DataFormatString="{0:dd MMM yyyy HH:mm}" />
            </Columns>
        </asp:GridView></div>
        <asp:Panel ID="UploadPanel" runat="server" CssClass="card">
            <h3>Upload attachment</h3>
            <div class="row"><div class="field"><asp:FileUpload ID="DocumentFile" runat="server" /></div><asp:Button ID="UploadButton" runat="server" Text="Upload" OnClick="UploadButton_Click" /></div>
            <span class="help">PDF, Office documents, JPEG, or PNG; maximum 20 MB.</span>
        </asp:Panel>
    </section>
    <section class="card">
        <h2>Workflow action</h2>
        <div class="field"><label for="Remarks">Remarks (optional)</label><asp:TextBox ID="Remarks" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="3" /></div>
        <div class="row">
            <asp:Button ID="SubmitWorkflowButton" runat="server" Text="Submit / Forward" OnClick="SubmitWorkflowButton_Click" />
            <asp:Button ID="ApproveButton" runat="server" Text="Approve" OnClick="ApproveButton_Click" />
            <asp:Button ID="RejectButton" runat="server" Text="Reject" CssClass="button danger" OnClick="RejectButton_Click" />
        </div>
        <p class="help">Workflow status changes are validated against the current status and the signed-in role before being saved.</p>
    </section>
    <section class="card">
        <div class="page-heading"><h2>Workflow history</h2>
            <asp:HyperLink ID="BidsLink" runat="server" CssClass="button secondary">Review bids</asp:HyperLink>
            <asp:HyperLink ID="WorkLink" runat="server" CssClass="button secondary">Work management</asp:HyperLink>
        </div>
        <div class="table-wrap"><asp:GridView ID="HistoryGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No workflow actions have been recorded.">
            <Columns>
                <asp:BoundField DataField="ApprovalStage" HeaderText="Stage" />
                <asp:BoundField DataField="PreviousStatus" HeaderText="Previous status" />
                <asp:BoundField DataField="NewStatus" HeaderText="New status" />
                <asp:BoundField DataField="Status" HeaderText="Action" />
                <asp:BoundField DataField="ActionTakenBy" HeaderText="Action taken by" />
                <asp:BoundField DataField="RoleAtAction" HeaderText="Role" />
                <asp:BoundField DataField="Remarks" HeaderText="Remarks" />
                <asp:BoundField DataField="ApprovalDate" HeaderText="Date" DataFormatString="{0:dd MMM yyyy HH:mm}" />
            </Columns>
        </asp:GridView></div>
    </section>
</asp:Content>
