<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="IssueSelector.ascx.cs" Inherits="Xperience.Jira.Controls.IssueSelector" %>
<%@ Register Src="~/CMSFormControls/Basic/TextBoxControl.ascx" TagName="TextBox" TagPrefix="cms" %>

<cms:TextBox ID="txtSearch" WatermarkText="Search.." runat="server" />
<cms:CMSDropDownList ID="drpIssues" runat="server" />