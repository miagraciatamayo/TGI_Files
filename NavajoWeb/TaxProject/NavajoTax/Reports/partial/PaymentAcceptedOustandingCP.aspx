<%@ Page Language="VB" AutoEventWireup="false" CodeFile="PaymentAcceptedOustandingCP.aspx.vb" Inherits="Reports_PaymentAcceptedOustandingCP" %>

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
                <asp:Label ID="lblFirstName" runat="server"/> <asp:Label ID="lblMiddleName" runat="server"/> <asp:Label ID="lblLastName" runat="server"/>
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
                The Treasurer's office has received your payment of <asp:Label ID="lblAmount1" runat="server" />; however, we would like to remind you that there is at least
                one lein (Certificate of Purchase) for non-payment of taxes against your property.  You may clear all tax lien(s) and/or taxes owed against your property by
                sending certified funds (cashier's check or money order) in the amount of <asp:Label ID="lblAmount2" runat="server" /> on or before <asp:Label ID="lblDate1a" runat="server" />.
                 If you choose to pay the balance after <asp:Label ID="lblDate1b" runat="server" />, but on or before <asp:Label ID="lblDate2" runat="server" />, the balance will be <asp:Label ID="lblAmount3" runat="server" />.
                <br />
                <br />
                Thank you for your attention in this matter.  If you have any questions, please feel free to phone us at the number listed above.
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

