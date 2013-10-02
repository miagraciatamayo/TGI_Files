<%@ Page Language="VB" AutoEventWireup="false" CodeFile="PostingReport.aspx.vb" Inherits="Reports_InvestorHoldingsSummary" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div  style="height: 67px; width: 100%">
        <div id="divreportHeader" style="width:100%;">
            <asp:Label ID="lblHeader" runat="server"/>
        </div>
        <br />
        <div id="divreportContent">
            <div>
                <asp:Label ID="lblReportDate" runat="server"/>
                <br />
                <table style="height: 67px; width: 100%">
                    <tr align="center">
                    <td>
                       &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Deposit Record</b> 
                    </td>
                </tr>
                </table>
                
            </div>
            <br />
            <div id="divDepositRecord" style="text-align: center; width: 100%" >
                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;<b>Results</b><br />
                <asp:GridView ID="gvDepositRecord" runat="server" AutoGenerateColumns="False"
                    CellPadding="4" ForeColor="#333333" GridLines="None" HorizontalAlign="Center">
                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                    <Columns>
                        <asp:BoundField HeaderText="Session ID" DataField="Cashier Session"/>
                        <asp:BoundField HeaderText="Entry Date" DataField="ENTRY_DATE" dataformatstring="{0:M/d/yyyy hhmmtt}"/>
                        <asp:BoundField HeaderText="Record Date" DataField="RECORD_DATE" dataformatstring="{0:M/d/yyyy hhmmtt}"/>
                        <asp:BoundField HeaderText="Memo" DataField="MEMO" />
                        <asp:BoundField HeaderText="Memo Number" DataField="MEMO_NUMBER" />
                        <asp:BoundField HeaderText="Tax Receipt Open Date" DataField="TAX_RECEIPT_OPEN_DATE" dataformatstring="{0:M/d/yyyy hhmmtt}"   />
                        <asp:BoundField HeaderText="Reference" DataField="REFERENCE"  />
                        <asp:BoundField HeaderText="Posted Date" DataField="POSTED_DATE" dataformatstring="{0:M/d/yyyy hhmmtt}" NullDisplayText ="not posted" />
                        <asp:BoundField HeaderText="Amount" DataField="AMOUNT" />
                        <asp:BoundField HeaderText="From Date" DataField="FROM_DATE" dataformatstring="{0:M/d/yyyy hhmmtt}" />
                        <asp:BoundField HeaderText="To Date" DataField="TO_DATE" dataformatstring="{0:M/d/yyyy hhmmtt}" />
                        <asp:BoundField HeaderText="Entity" DataField="ENTITY" />
                        <asp:BoundField HeaderText="Status" DataField="STATUS" />
                        <asp:BoundField HeaderText="Account" DataField="ACCOUNT" />
                        <asp:BoundField HeaderText="Entry" DataField="ENTRY" />
                        <asp:BoundField HeaderText="Deposit" DataField="DEPOSIT" />
                        <asp:BoundField HeaderText="USR Record #" DataField="USR_RECORD_NUMBER" />
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

            <div id="div1" style="text-align: center; width: 100%">
                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Deposit Details<br />
                <asp:GridView ID="gvDepositDetails" runat="server" AutoGenerateColumns="False"
                    CellPadding="4" ForeColor="#333333" GridLines="None" HorizontalAlign="Center">
                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                    <Columns>
                        <asp:BoundField HeaderText="Receipt Number" DataField="RECEIPT_NUMBER" />
                        <asp:BoundField HeaderText="Amount" DataField="AMOUNT" />
                        <asp:BoundField HeaderText="Account" DataField="ACCOUNT" />
                        <asp:BoundField HeaderText="Memo" DataField="MEMO" />
                        <asp:BoundField HeaderText="Create User" DataField="CREATE_USER" />
                        <asp:BoundField HeaderText="Create Date" DataField="CREATE_DATE" dataformatstring="{0:M/d/yyyy hhmmtt}"   />
                        <asp:BoundField HeaderText="Edit User" DataField="EDIT_USER"  />
                        <asp:BoundField HeaderText="Edit Date" DataField="EDIT_DATE" dataformatstring="{0:M/d/yyyy hhmmtt}" NullDisplayText ="not posted" />
                                               
                    </Columns>
                    <EditRowStyle BackColor="#999999" />
                    <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                    <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
                    <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                 <%--   <SortedAscendingCellStyle BackColor="#E9E7E2" />
                    <SortedAscendingHeaderStyle BackColor="#506C8C" />
                    <SortedDescendingCellStyle BackColor="#FFFDF8" />
                    <SortedDescendingHeaderStyle BackColor="#6F8DAE" />--%>
                </asp:GridView>
            </div>
            
        </div>
    </div>
    </form>
</body>
</html>

