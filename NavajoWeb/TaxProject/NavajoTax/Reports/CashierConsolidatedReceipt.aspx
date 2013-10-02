<%@ Page Language="VB" AutoEventWireup="false" CodeFile="CashierConsolidatedReceipt.aspx.vb" Inherits="ConsolidatedCashierReceipt"  StylesheetTheme ="Blue" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
  
    <script type="text/javascript">
       
    </script>
    <title>Treasury Receipt</title>
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
                <p class="style1" style ="width: 800px" align="center"><b>Consolidated Treasury Receipt</b> <br />

               <%-- <asp:Label ID="TaxIDNumber" runat="server"></asp:Label>--%>
                <b>Transaction Date: </b>
                <asp:Label ID="GETDATE" runat="server" ></asp:Label></p>
                <br />
                <b>Payment Party</b><br /><br />
                Pertinent 1: 
                <asp:Label ID="Pert1" runat="server"></asp:Label>
                <br />
                Pertinent 2: 
                <asp:Label ID="Pert2" runat="server" ></asp:Label>
                <br />
                Description: 
                <asp:Label ID="Desc" runat="server"></asp:Label>
                 <%--and Tax Roll Number <asp:Label ID="TaxRollNumber" runat="server"></asp:Label>:--%>
                <br />
                <br />
                <table style="width: 100%;">
                    <tr>
                        <td style="width: 200px; padding-left: 20px;">
                            On <asp:Label ID="PaymentDate" runat="server"></asp:Label>, a payment was processed at the <asp:Label ID="CountyName" runat="server"></asp:Label> Treasury
                            for Tax Account <asp:Label ID="TaxIDNumber" runat="server"></asp:Label>. The Payment in the amount of $<asp:Label ID="Amount" runat="server"></asp:Label>
                            was allocated to the following:
                            <br />
                        </td>
                    </tr>

                    <tr>
                        <td align="center">
                            <asp:GridView ID="grdTransactionData" runat="server" AutoGenerateColumns="false">
                            <EmptyDataTemplate><b>Transaction Data: None</b></EmptyDataTemplate>
                            <Columns>                                
                                <asp:BoundField HeaderText="Tax Year" DataField="Tax Year" />
                                <asp:BoundField HeaderText="Roll Number" DataField="Roll" />
                                <asp:BoundField HeaderText="Date" DataField="Date" />
                                <asp:BoundField HeaderText="Payment Applied" DataField="Payment Applied" />                       
                                <asp:BoundField HeaderText="Applied To" DataField="Applied To" NullDisplayText ="N/A" />   
                            </Columns>                            
                            </asp:GridView>
                             
                        </td>
                    </tr>

                    <tr>
                        <td align="center"> 
                            <br />
                            The following Certificates of Purchase have veen redeemed by this transaction:
                            <asp:GridView ID="grdCPRedeemed" runat="server" AutoGenerateColumns="false">
                            <EmptyDataTemplate><b>CP Redeemed Data: None</b></EmptyDataTemplate>
                            <Columns>                                
                                <asp:BoundField HeaderText="CP" DataField="CertificateNumber" />
                                <asp:BoundField HeaderText="APN" DataField="APN" />
                                <asp:BoundField HeaderText="Tax Year" DataField="TaxYear" />
                                <asp:BoundField HeaderText="Roll" DataField="TaxRollNumber" />                       
                                <asp:BoundField HeaderText="Date Redeemed" DataField="Date_Redeemed" />   
                            </Columns>                            
                            </asp:GridView>
                             
                        </td>
                    </tr>

                     <tr>
                        <td align="center"> 
                            <br />
                            The following Certificates of Purchase remain against the parcel:
                            <asp:GridView ID="grdCPRemain" runat="server" AutoGenerateColumns="false">
                            <EmptyDataTemplate><b>CP Redeemed Data: None</b></EmptyDataTemplate>
                            <Columns>                                
                                <asp:BoundField HeaderText="CP" DataField="CP" />
                                <asp:BoundField HeaderText="APN" DataField="APN" />
                                <asp:BoundField HeaderText="Tax Year" DataField="TaxYear" />
                                <asp:BoundField HeaderText="Roll" DataField="Roll" />                       
                                <asp:BoundField HeaderText="Amount" DataField="Amount" />   
                                <asp:BoundField HeaderText="Interest" DataField="Interest" />   
                            </Columns>
                            </asp:GridView>
                             
                        </td>
                    </tr>

                    </table>
                <%--<p>Payment History:</p>
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
				</table>--%>
               <%-- <br />
                Very truly yours,
                <br />
                <br />
                <asp:Label ID="lblSignature" runat="server" ></asp:Label>
                <br />--%>

            </div>
            <br />
            <br />
        </div>
    </div>
    </form>
</body>
</html>
