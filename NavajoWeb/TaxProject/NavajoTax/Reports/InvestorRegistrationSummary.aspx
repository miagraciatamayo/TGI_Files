<%@ Page Language="VB" AutoEventWireup="false" CodeFile="InvestorRegistrationSummary.aspx.vb" Inherits="Reports_InvestorRegistrationSummary" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div style="width: 800px">
        <div id="divReportHeader">
            <asp:Label ID="lblHeader" runat="server"/>
        </div>
        <br />
        <br />
        <div id="divreportContent">
            <div>
                <asp:Label ID="lblReportDate" runat="server" />
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
                Thank you for participating in the <asp:Label ID="lblClient" runat="server" /> tax lien investor program.  The following information is on file with the County:
                <br />
                <br />
                <table style="width: 100%;">
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            Active Vendor:
                        </td>
                        <td>
                          <asp:Label ID="lblActive" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            Tax Payer Identification:
                        </td>
                        <td>
                           <asp:Label ID="lblSSN" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            County Vendor Number
                        </td>
                        <td>
                            <asp:Label ID="lblVendorNum" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            Phone Number:
                        </td>
                        <td>
                            <asp:Label ID="lblPhone" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            E-Mail Address:
                        </td>
                        <td>
                            <asp:Label ID="lblEMail" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            Mailing Address Protected:
                        </td>
                        <td>
                            <asp:Label ID="lblConfidential" runat="server" />
                        </td>
                    </tr>
                </table>
                <br />
                <br />
                Please review our website and bulletin board to review curent and upcoming tax lien investment opportunities.
                <br />
                <br />
                Very truly yours,
                <br />
                <br />
                <asp:Label ID="lblSignature" runat="server" />
                <br />

            </div>
            <br />
            <br />
        </div>
    </div>
    </form>
</body>
</html>

