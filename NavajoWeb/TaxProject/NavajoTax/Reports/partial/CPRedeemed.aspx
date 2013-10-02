<%@ Page Language="VB" AutoEventWireup="false" CodeFile="CPRedeemed.aspx.vb" Inherits="Reports_CPRedeemed" %>

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
                <asp:Label ID="lblInvestor" runat="server"/> 
                <br />
                <asp:Label ID="lblAddress1" runat="server"/>
                <br />
                <asp:Label ID="lblAddress2" runat="server"/>
                <br />
                <asp:Label ID="lblCity" runat="server"/> <asp:Label ID="lblState" runat="server"/> <asp:Label ID="lblPostalCode" runat="server"/>
                <br />
                <br />
                RE: Parcel# <asp:Label ID="lblParcel" runat="server" />
                <br />
                Tax Year: <asp:Label ID="lblTaxYear" runat="server" /><br />
                Tax Roll: <asp:Label ID="lblTaxRoll" runat="server" />
            </div>
            <br />
            <br />
            <div id="divCertificateNotice" style="text-align: center;">
                This letter is to inform you that the Certificate(s) of Purchase you hold on the parcel listed above have been redeemed.
                Enclosed is the redemption check(s) totaling *?*.  Below is a summary of the information relating to this investment:
                <br />
                <br />
                <asp:GridView ID="gvCertificateNotice" runat="server" AutoGenerateColumns="False"
                    CellPadding="4" ForeColor="#333333" GridLines="None" HorizontalAlign="Center">
                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                    <Columns>
                        <asp:BoundField HeaderText="Tax Year" DataField="CP" />
                        <asp:BoundField HeaderText="Investment" DataField="APNPARCEL" />
                        <asp:BoundField HeaderText="Purchased" DataField="SaleDate" DataFormatString="{0:d}" />
                        <asp:BoundField HeaderText="Redeemed" DataField="Expiration" DataFormatString="{0:d}" />
                        <asp:BoundField HeaderText="Rate" DataField="FaceValueOfCP" DataFormatString="{0:C}" />
                        <asp:BoundField HeaderText="Interest" DataField="PurchaseValue" DataFormatString="{0:C}" />
                        <asp:BoundField HeaderText="Total" DataField=" " DataFormatString="{0:C}" />
                    </Columns>
                    <EditRowStyle BackColor="#999999" />
                    <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                    <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
                    <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                    <%--<SortedAscendingCellStyle BackColor="#E9E7E2" />
                    <SortedAscendingHeaderStyle BackColor="#506C8C" />
                    <SortedDescendingCellStyle BackColor="#FFFDF8" />
                    <SortedDescendingHeaderStyle BackColor="#6F8DAE" />--%>
                </asp:GridView>
            </div>
            <br />
            <div id="divReportSignature">
                If you have any questions regarding this transaciton, please feel free to phone us at the number listed above.
                <br />
                <br />
                <asp:Label ID="lblSignature" runat="server" />
            </div>
        </div>
    </div>
    </form>
</body>
</html>

