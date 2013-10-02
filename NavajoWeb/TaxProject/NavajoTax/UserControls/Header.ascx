<%@ Control Language="VB" AutoEventWireup="false" CodeFile="Header.ascx.vb" Inherits="UserControls_Header" %>
<div class="header">
        <table>
            <tr>
                <td rowspan="3">
                    <img alt="Logo" width="74" height="74" src="logo.png" />
                </td>
            </tr>
            <tr>
                <td>
                    <h1>
                        <asp:Label ID="lblClientName" runat="server" />
                        <%--Navajo County Treasurer--%>
                        </h1>
                </td>
                <td>
                    Operator:
                    <asp:Label ID="lblOperatorName" runat="server"></asp:Label>
                </td>
                <td>
                    Login Time:
                    <asp:Label ID="lblLoginTime" runat="server"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <h2>
                    <asp:Label ID="lblPageName" runat="server"/>
                        <%--Cashier--%>
                        </h2>
                </td>
                <td>
                    Date:
                    <asp:Label ID="lblCurrentDate" runat="server"></asp:Label>
                </td>
                <td>
                    Cash in Register at Login:
                    <asp:Label ID="lblStartCash" runat="server"></asp:Label>
                </td>
                <%--<td>
                    <input id="btnHeaderLogout" type="button" value="Logout"/>
                </td>--%>
            </tr>
        </table>
    </div>
