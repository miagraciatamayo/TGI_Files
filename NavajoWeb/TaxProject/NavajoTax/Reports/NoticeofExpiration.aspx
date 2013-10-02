<%@ Page Language="VB" AutoEventWireup="false" CodeFile="NoticeofExpiration.aspx.vb" Inherits="Reports_NoticeofExpiration" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div style="width: 800px">
        <div id="divreportHeader">
            <asp:Label ID="lblHeader" runat="server"/>
        </div>
        <br />
        <br />
        <div id="divreportContent">
            <div>
                <asp:Label ID="lblReportDate" runat="server"/>
                <br />
                <br />
                Attention:
                <br />
                <br />
                <asp:Label ID="lblFirstName" runat="server"/> <asp:Label ID="lblMiddleName" runat="server"/> <asp:Label ID="lblLastName" runat="server"/>
                <br />
                <asp:Label ID="lblAddress1" runat="server"/>
                <br />
                <asp:Label ID="lblAddress2" runat="server"/>
                <br />
                <asp:Label ID="lblCity" runat="server"/> <asp:Label ID="lblState" runat="server"/> <asp:Label ID="lblPostalCode" runat="server"/>
            </div>
            <br />
            <br />
            <div id="divCertificateNotice" style="text-align: center;">
                Pursuant to ARS 42-18208 we must inform you that the Certificates of Purchase listed below are within six months of their<br />
                ten-year expiration date. As the holder of these Certificates of Purchase, you will forfeit your investment if you do not<br />
                begin foreclosure proceedings.<br />
                <br />
                <br />
                <asp:GridView ID="gvCertificateNotice" runat="server" AutoGenerateColumns="False"
                    CellPadding="4" ForeColor="#333333" GridLines="None" HorizontalAlign="Center">
                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                    <Columns>
                        <asp:BoundField HeaderText="CP" DataField="CP" />
                        <asp:BoundField HeaderText="Parcel Number" DataField="APNPARCEL" />
                        <asp:BoundField HeaderText="Date Of Sale" DataField="SaleDate" DataFormatString="{0:d}" />
                        <asp:BoundField HeaderText="Date of Expiration" DataField="Expiration" DataFormatString="{0:d}" />
                        <asp:BoundField HeaderText="Face Value" DataField="FaceValueOfCP" DataFormatString="{0:C}" />
                        <asp:BoundField HeaderText="Purchase Value" DataField="PurchaseValue" DataFormatString="{0:C}" />
                    </Columns>
                    <EditRowStyle BackColor="#999999" />
                    <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                    <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
                    <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                   <%-- <SortedAscendingCellStyle BackColor="#E9E7E2" />
                    <SortedAscendingHeaderStyle BackColor="#506C8C" />
                    <SortedDescendingCellStyle BackColor="#FFFDF8" />
                    <SortedDescendingHeaderStyle BackColor="#6F8DAE" />--%>
                </asp:GridView>
            </div>
            <br />
            <div id="divReportSignature">
                <asp:Label ID="lblSignature" runat="server" />
            </div>
        </div>
    </div>
    </form>
</body>
</html>

