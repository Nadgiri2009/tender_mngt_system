<%@ Page Title="Master Data" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="AdminMasterData.aspx.cs" Inherits="DepartmentModule.Pages.AdminMasterData" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Master Data</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-heading">
        <div><h1>Master data</h1><div class="muted">Manage tender categories, methods, document types, units, and officer designations.</div></div>
        <a class="button secondary" href="Admin.aspx">Back to administration</a>
    </div>
    <asp:Literal ID="Message" runat="server" />
    <section class="card">
        <div class="field">
            <asp:Label runat="server" AssociatedControlID="CatalogSelect">Catalog</asp:Label>
            <asp:DropDownList ID="CatalogSelect" runat="server" AutoPostBack="true" OnSelectedIndexChanged="CatalogSelect_SelectedIndexChanged" />
        </div>
        <asp:HiddenField ID="OriginalValue" runat="server" />
        <div class="row">
            <div class="field">
                <asp:Label ID="ValueLabel" runat="server" AssociatedControlID="ValueText">Name</asp:Label>
                <asp:TextBox ID="ValueText" runat="server" MaxLength="150" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="ValueText" ValidationGroup="Catalog" CssClass="validation" ErrorMessage="A name is required." />
            </div>
            <div class="field">
                <asp:Label runat="server" AssociatedControlID="SortOrderText">Display order</asp:Label>
                <asp:TextBox ID="SortOrderText" runat="server" Text="0" TextMode="Number" />
            </div>
        </div>
        <div class="field"><asp:CheckBox ID="IsActiveCheck" runat="server" Text="Active" Checked="true" /></div>
        <div class="admin-actions">
            <asp:Button ID="SaveButton" runat="server" Text="Add item" ValidationGroup="Catalog" OnClick="SaveButton_Click" />
            <asp:Button ID="CancelButton" runat="server" Text="Cancel edit" CausesValidation="false" CssClass="button secondary" OnClick="CancelButton_Click" Visible="false" />
        </div>
    </section>
    <section class="card">
        <h2>Catalog entries</h2>
        <p class="muted">Existing business records are retained. Entries referenced by tenders or documents cannot be deleted.</p>
        <div class="table-wrap">
            <asp:GridView ID="CatalogGrid" runat="server" AutoGenerateColumns="false" GridLines="None"
                OnRowCommand="CatalogGrid_RowCommand" EmptyDataText="No entries are configured.">
                <Columns>
                    <asp:BoundField DataField="CatalogValue" HeaderText="Name" />
                    <asp:BoundField DataField="SortOrder" HeaderText="Display order" />
                    <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
                    <asp:BoundField DataField="UsageCount" HeaderText="References" />
                    <asp:TemplateField HeaderText="Actions"><ItemTemplate>
                        <asp:LinkButton runat="server" Text="Edit" CommandName="EditCatalogItem" CommandArgument='<%# Eval("CatalogValue") %>' CausesValidation="false" />
                        <asp:LinkButton runat="server" Text="Delete" CommandName="DeleteCatalogItem" CommandArgument='<%# Eval("CatalogValue") %>' CausesValidation="false" OnClientClick="return confirm('Delete this unused catalog entry?');" />
                    </ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>
</asp:Content>
