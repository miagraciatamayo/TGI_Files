<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ErrorPage.aspx.vb" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">

<meta charset="UTF-8"/>
    <meta http-equiv="refresh" content="2;url=TaxPayments.aspx"/>
    <script type="text/javascript">
      //  window.location.href = "TaxPayments.aspx"
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
    
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="ajaxToolkit" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
    <title>Quick Tax Payments</title>
    <link href="Css/redmond/jquery-ui-1.8.23.custom.css" rel="stylesheet" 
        type="text/css" />
    <link href="Css/Tax.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="JavaScript/jquery-1.5.1.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery-ui-1.8.23.custom.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.maskedinput-1.3.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.validate.js"></script>
  

    <form id="form2" runat="server">
    <!-- Modal popup divs -->
    <div id="divLoading" class="divPopup" title="Loading, please wait...">
        <img alt="Loading..." height="19" src="ajax-loader_redmond.gif" width="220" />
    </div>
    <div id="divMessage" class="divPopup">
        <p id="pMessage">
        </p>
    </div>
    <div id="divLogin" class="divPopup" title="Login">
        <fieldset>
            <b>Cashier:</b>
            <asp:Label ID="lblLoginUsername" runat="server"></asp:Label>
            <br />
            <b>Cash at Login:</b>
            <asp:TextBox ID="txtLoginStartCash" runat="server" CssClass="required number" 
                Width="100px"></asp:TextBox>
        </fieldset>
        <asp:Button ID="btnLogin" runat="server" Text="Login" />
    </div>
    <div id="divLogout" class="divPopup" title="Logout">
        <fieldset>
            <b>Cashier:</b>
            <asp:Label ID="lblLogoutUsername" runat="server"></asp:Label>
            <br />
            <b>Cash at Logout:</b>
            <asp:TextBox ID="txtLogoutEndCash" runat="server" CssClass="required number" 
                Width="100px"></asp:TextBox>
        </fieldset>
        <asp:Button ID="btnLogout" runat="server" Text="Logout" />
    </div>
    <div id="divAddRemark" class="divPopup">
        <fieldset>
            <asp:Label ID="lblRemarkText" runat="server" 
                AssociatedControlID="txtRemarkText" Style="display: block;" Text="Remark"></asp:Label>
            <asp:TextBox ID="txtRemarkText" runat="server" Rows="3" Style="display: block;" 
                TextMode="MultiLine" Width="340px"></asp:TextBox>
            <asp:Label ID="lblRemarkDate" runat="server" 
                AssociatedControlID="txtRemarkDate" Style="display: block;" Text="Date"></asp:Label>
            <asp:TextBox ID="txtRemarkDate" runat="server" Style="display: block;"></asp:TextBox>
            <asp:Label ID="lblRemarkImage" runat="server" 
                AssociatedControlID="uplRemarkImage" Style="display: block;" Text="Document"></asp:Label>
            <asp:FileUpload ID="uplRemarkImage" runat="server" Style="display: block;" 
                Width="340px" />
        </fieldset>
    </div>
    <div class="header">
        <table>
            <tr>
                <td rowspan="3">
                    <img alt="Navajo County Logo" height="74" src="logo.png" width="74" />
                </td>
            </tr>
            <tr>
                <td class="style2">
                    <h1>
                        Navajo County Treasurer</h1>
                </td>
                <td class="style2">
                    &nbsp;</td>
                <td class="style2">
                    &nbsp;</td>
            </tr>
            <tr>
                <td>
                    <h2>
                        Error</h2>
                </td>
                <td>
                    </td>
                <td>
                    &nbsp;</td>
                <td>
                    &nbsp;</td>
            </tr>

            
        </table>
       
    </div>
    <div>
         <table>
        <tr>
                <td >
                <h2>
                        An unexpected error occurred. You are being redirected to the Home Page. <br /> <br />
                        If you have not been redirected, please click on HOME Page below <br /><br />
                         <asp:HyperLink runat="server" Text="HOME" NavigateUrl ="TaxPayments.aspx" >HOME</asp:HyperLink></h2>
                </td>
            </tr>
        </table>
    
    </div>
    </form>
</body>
</html>
