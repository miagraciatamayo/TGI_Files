<%@ Page Language="VB" AutoEventWireup="false" CodeFile="NoticeofStateApplicationforDeed.aspx.vb" Inherits="Reports_NoticeofStateApplicationforDeed" %>

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
                <asp:Label ID="lblReportDate" runat="server" />
                <br />
                <br />
                Attention: <asp:Label ID="lblInvestor" runat="server" />
                <br />
                <br />
                Notice is hereby given that the STATE OF ARIZONA has applied for a Treasurer's Deed to the following described real property, owned by <asp:Label ID="lblPropertyOwner" runat="server" /> and situated in Navajo County, Arizona.
                <br />
                <br /> 
                <table style="width: 100%;">
                    <tr>
                        <td style="width: 150px;">
                            Parcel Number:
                        </td>
                        <td>
                          <asp:Label ID="lblParcel" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 150px; ">
                            Legal Description:
                        </td>
                        <td>
                           <asp:Label ID="lblLegalDescription" runat="server" />
                        </td>
                    </tr>
                </table>
                <br />
                Which on the <asp:Label ID="lblSaleDate" runat="server" /> was sold to the STATE OF ARIZONA for taxes, interest, penalties and charges amounting to <asp:Label ID="lblFaceValue" runat="server" />.
                <br />
                <br />
                YOU, AS A CP HOLDER, OWN THE LIEN ON THE <asp:Label ID="lblTaxYear" runat="server" /> TAXES. IF YOU DO NOT SUBEQUENT TAX THE LIENS OWNED BY THE STATE OF ARIZONA BY <asp:Label ID="lblForeclosureDate" runat="server" />, AND BEGIN FORECLOSURE PROCEEDINGS, YOU WILL FORFEIT YOUR INVESTMENT AND I WILL CONVEY SAID PREMISES TO SUCK APPLICATION OR HIS ASSIGNS.
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

