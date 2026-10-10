<%@ Page Title="Module Error" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="DepartmentModule.Pages.ModuleError" %>
<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Module Error</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <section class="card error-page">
        <h1><asp:Literal ID="Heading" runat="server" /></h1>
        <p class="muted"><asp:Literal ID="Message" runat="server" /></p>
        <a class="button" href="Default.aspx">Return to module dashboard</a>
    </section>
</asp:Content>
