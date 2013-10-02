<%@ Page Language="VB" AutoEventWireup="false" CodeFile="PrintCashierReceipt.aspx.vb" Inherits="PrintCashierReceipt"  StylesheetTheme ="Blue" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
  
    <script type="text/javascript">
       
    </script>
    <title></title>
    <style type="text/css">

        .divPopup
        {
            display: none;
        }
        .header
        {
            width: 100%;
            color: White;
            background-color: #4682B4;
        }
        .header td
        {
            padding-right: 24px;
        }
        .header h1, .header h2, .header h3
        {
            margin-top: 0px;
            margin-bottom: 0px;
        }
        
        #btnLogin, #btnHeaderLogout, #btnLogout, #btnShowAccountRemarksPopup, #btnShowTaxRollRemarksPopup, #btnShowOtherYearRemarksPopup, #btnPosLoadPosting,#btnViewCP,#btnPrintDeed
        {
            padding-top: 0px;
            padding-bottom: 0px;
        }
        .style2
        {
            height: 45px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div style="width: 800px">
        <div class="header">
        <div id="divreportHeader" style="width:100%;">
            <asp:Label ID="lblHeader" runat="server"/>
        </div>
        <br />
    </div>
        <br />
        <br />
        <div id="divreportContent">
            <div>
                <br />
                <br />
                <p class="style1" style ="width: 800px"><b>Tax Payment Summary for</b> <br />

                <asp:Label ID="TaxIDNumber" runat="server"></asp:Label>

                <asp:Label ID="GETDATE" runat="server" ></asp:Label></p>
                <br />
				<asp:Label ID="OWNER_NAME_1" runat="server"></asp:Label>
                <br />
                <asp:Label ID="MAIL_ADDRESS_2" runat="server"></asp:Label>
                <br />
                <asp:Label ID="MAIL_CITY" runat="server"></asp:Label> <asp:Label ID="MAIL_STATE" runat="server"></asp:Label> <asp:Label ID="MAIL_CODE" runat="server"></asp:Label>
                <br />
                <br />
                Payment summary for tax year <asp:Label ID="TaxYear" runat="server"></asp:Label> and Tax Roll Number <asp:Label ID="TaxRollNumber" runat="server"></asp:Label>:
                <br />
                <br />
                <table style="width: 100%;">
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            Total Tax:
                        </td>
                        <td>
                          <asp:Label ID="lblActive" runat="server" ></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            Total Interest:
                        </td>
                        <td>
                           <asp:Label ID="lblSSN" runat="server" ></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            Total Fees:</td>
                        <td>
                            <asp:Label ID="lblVendorNum" runat="server" ></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            Total Payments:
                        </td>
                        <td>
                            <asp:Label ID="lblPhone" runat="server" ></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 200px; padding-left: 20px; height: 23px;">
                            Balance:
                        </td>
                        <td style="height: 23px">
                            <asp:Label ID="lblEMail" runat="server" ></asp:Label>
                        </td>
                    </tr>
                    </table>
                <p>Payment History:</p>
                <table align="center" style="width: 70%">
					<tr>
						<td style="width: 200px;">Remitted By</td>
						<td style="width: 200px;">Payment Date</td>
						<td style="width: 200px;">Payment Amount</td>
					</tr>
					<tr>
						<td>
                            <asp:Label ID="Pertinent1" runat="server" ></asp:Label>
                        </td>
						<td>
                            <asp:Label ID="PaymentEffectiveDate" runat="server" ></asp:Label>
                        </td>
						<td>
                            <asp:Label ID="PaymentAmount" runat="server" ></asp:Label>
                        </td>
					</tr>
				</table>
                <br />
                Very truly yours,
                <br />
                <br />
                <asp:Label ID="lblSignature" runat="server" ></asp:Label>
                <br />

            </div>
            <br />
            <br />
        </div>
    </div>
    </form>
</body>
</html>
