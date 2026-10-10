<%@ Page Title="Work Management" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="WorkManagement.aspx.cs" Inherits="DepartmentModule.Pages.WorkManagement" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Work Management</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading"><div><h1>Post-award Work Management</h1><div class="muted"><asp:Literal ID="TenderNumber" runat="server" /> · <asp:Literal ID="TenderTitle" runat="server" /></div></div><asp:HyperLink ID="BackLink" runat="server" CssClass="button secondary">Tender details</asp:HyperLink></div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <h2>Agreement</h2>
        <div class="row"><div class="field"><label for="AgreementNo">Agreement number</label><asp:TextBox ID="AgreementNo" runat="server" MaxLength="50" /></div>
            <div class="field"><label for="AgreementDate">Agreement date</label><asp:TextBox ID="AgreementDate" runat="server" TextMode="Date" /></div>
            <div class="field"><label for="AgreementContractor">Contractor</label><asp:DropDownList ID="AgreementContractor" runat="server" /></div></div>
        <div class="field"><label for="AgreementDocument">Agreement document path (optional)</label><asp:TextBox ID="AgreementDocument" runat="server" MaxLength="400" /></div>
        <asp:Button ID="CreateAgreementButton" runat="server" Text="Record agreement" OnClick="CreateAgreementButton_Click" />
        <div class="table-wrap"><asp:GridView ID="AgreementGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No agreement recorded."><Columns>
            <asp:BoundField DataField="AgreementNo" HeaderText="Agreement No." /><asp:BoundField DataField="ContractorName" HeaderText="Contractor" /><asp:BoundField DataField="AgreementDate" HeaderText="Agreement Date" DataFormatString="{0:dd MMM yyyy}" /><asp:BoundField DataField="DocumentPath" HeaderText="Document" />
        </Columns></asp:GridView></div>
    </section>
    <section class="card">
        <h2>Work order</h2>
        <div class="row"><div class="field"><label for="OrderNo">Order number</label><asp:TextBox ID="OrderNo" runat="server" MaxLength="50" /></div>
            <div class="field"><label for="IssueDate">Issue date</label><asp:TextBox ID="IssueDate" runat="server" TextMode="Date" /></div>
            <div class="field"><label for="OrderContractor">Contractor</label><asp:DropDownList ID="OrderContractor" runat="server" /></div>
            <div class="field"><label for="OrderAmount">Amount (₹)</label><asp:TextBox ID="OrderAmount" runat="server" TextMode="Number" step="0.01" min="0.01" /></div></div>
        <asp:Button ID="CreateWorkOrderButton" runat="server" Text="Create work order" OnClick="CreateWorkOrderButton_Click" />
        <div class="table-wrap"><asp:GridView ID="WorkOrderGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No work order recorded."><Columns>
            <asp:BoundField DataField="OrderNo" HeaderText="Order No." /><asp:BoundField DataField="ContractorName" HeaderText="Contractor" />            <asp:BoundField DataField="IssueDate" HeaderText="Issue Date" DataFormatString="{0:dd MMM yyyy}" /><asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="₹{0:N2}" />
        </Columns></asp:GridView></div>
    </section>
    <section class="card">
        <h2>Work execution progress</h2>
        <div class="row"><div class="field"><label for="Progress">Progress percentage</label><asp:TextBox ID="Progress" runat="server" TextMode="Number" step="0.01" min="0" max="100" /></div></div>
        <div class="field"><label for="ProgressRemarks">Remarks</label><asp:TextBox ID="ProgressRemarks" runat="server" TextMode="MultiLine" MaxLength="1000" Rows="2" /></div>
        <asp:Button ID="UpdateProgressButton" runat="server" Text="Record progress" OnClick="UpdateProgressButton_Click" />
        <div class="table-wrap"><asp:GridView ID="ProgressGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No progress updates."><Columns>
            <asp:BoundField DataField="ProgressPercentage" HeaderText="Progress" DataFormatString="{0:N2}%" /><asp:BoundField DataField="Remarks" HeaderText="Remarks" /><asp:BoundField DataField="UpdatedByName" HeaderText="Updated By" /><asp:BoundField DataField="UpdatedDate" HeaderText="Date" DataFormatString="{0:dd MMM yyyy HH:mm}" />
        </Columns></asp:GridView></div>
    </section>
    <section class="card">
        <h2>Measurement book</h2>
        <div class="row"><div class="field"><label for="MeasurementDescription">Description</label><asp:TextBox ID="MeasurementDescription" runat="server" MaxLength="500" /></div>
            <div class="field"><label for="MeasurementQuantity">Quantity</label><asp:TextBox ID="MeasurementQuantity" runat="server" TextMode="Number" step="0.001" min="0.001" /></div>
            <asp:Button ID="CreateMeasurementButton" runat="server" Text="Add measurement" OnClick="CreateMeasurementButton_Click" /></div>
        <div class="table-wrap"><asp:GridView ID="MeasurementGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No measurements recorded."><Columns>
            <asp:BoundField DataField="Description" HeaderText="Description" /><asp:BoundField DataField="Quantity" HeaderText="Quantity" DataFormatString="{0:N3}" /><asp:BoundField DataField="RecordedByName" HeaderText="Recorded By" /><asp:BoundField DataField="MeasurementDate" HeaderText="Date" DataFormatString="{0:dd MMM yyyy HH:mm}" /><asp:BoundField DataField="ApprovalStatus" HeaderText="Approval Status" />
        </Columns></asp:GridView></div>
    </section>
    <section class="card">
        <h2>Bills and payments</h2>
        <div class="table-wrap"><asp:GridView ID="BillsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No bill has been submitted."><Columns>
            <asp:BoundField DataField="BillNo" HeaderText="Bill No." /><asp:BoundField DataField="ContractorName" HeaderText="Contractor" /><asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="₹{0:N2}" /><asp:BoundField DataField="Status" HeaderText="Status" /><asp:BoundField DataField="SubmittedDate" HeaderText="Submitted" DataFormatString="{0:dd MMM yyyy HH:mm}" />
            <asp:TemplateField><ItemTemplate><asp:Button ID="ApproveBillButton" runat="server" Text="Approve" CommandName="ApproveBill" CommandArgument='<%# Eval("BillId") %>' Visible='<%# Convert.ToString(Eval("Status")) == "Submitted" %>' OnCommand="Bill_Command" /></ItemTemplate></asp:TemplateField>
            <asp:TemplateField><ItemTemplate><asp:Button ID="PayBillButton" runat="server" Text="Pay" CommandName="PayBill" CommandArgument='<%# Eval("BillId") %>' Visible='<%# Convert.ToString(Eval("Status")) == "Approved" %>' OnCommand="Bill_Command" /></ItemTemplate></asp:TemplateField>
        </Columns></asp:GridView></div>
        <asp:Panel ID="PaymentPanel" runat="server" CssClass="card">
            <h3>Process payment for an approved bill</h3>
            <div class="row"><div class="field"><label for="BillSelect">Bill</label><asp:DropDownList ID="BillSelect" runat="server" /></div><div class="field"><label for="PaymentAmount">Amount</label><asp:TextBox ID="PaymentAmount" runat="server" TextMode="Number" step="0.01" min="0.01" /></div><div class="field"><label for="TransactionNo">Transaction number</label><asp:TextBox ID="TransactionNo" runat="server" MaxLength="100" /></div></div>
            <asp:Button ID="ProcessPaymentButton" runat="server" Text="Process payment" OnClick="ProcessPaymentButton_Click" />
        </asp:Panel>
        <div class="table-wrap"><asp:GridView ID="PaymentsGrid" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No payments processed."><Columns>
            <asp:BoundField DataField="BillNo" HeaderText="Bill No." /><asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="₹{0:N2}" /><asp:BoundField DataField="TransactionNo" HeaderText="Transaction No." /><asp:BoundField DataField="PaymentDate" HeaderText="Payment Date" DataFormatString="{0:dd MMM yyyy HH:mm}" />
        </Columns></asp:GridView></div>
    </section>
</asp:Content>
