<%@ Page Language="VB" AutoEventWireup="false" CodeFile="TaxInvestors.aspx.vb" Inherits="TaxInvestors"
    StylesheetTheme="Blue" %>

<%@ Register TagPrefix="ajaxToolkit" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit" %>
<%@ Register Src="UserControls/CPTransfer.ascx" TagName="CPTransfer" TagPrefix="uc1" %>
<%@ Register Src="UserControls/CPPurchaseFromState.ascx" TagName="CPPurchaseFromState"
    TagPrefix="uc2" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Investor Management</title>
    <link href="Css/redmond/jquery-ui-1.8.23.custom.css" rel="stylesheet" type="text/css" />
    <link href="Css/Tax.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="JavaScript/jquery-1.5.1.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery-ui-1.8.23.custom.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.maskedinput-1.3.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.validate.js"></script>
    <script type="text/javascript">
        $(document).ready(function () {
            // Tabs
            $("#mainTabs").tabs({
                selected: window.location.hash,
                select: function (event, ui) {
                    switch (ui.index) {
                        case 0: window.location.href = "TaxPayments.aspx#tabPayments";
                            break;
                        case 1: window.location.href = "TaxPayments.aspx#tabCashierActivity";
                            break; 
                        case 2:
                            window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
                            break;
                    }
                }
            }).tabs("select", (window.location.hash ? window.location.hash : 2));

            // Button Formatting
            $("[id$=btnHeaderLogout]").button();
            $("[id$=btnRegSave]").button();
            $("[id$=btnRegClear]").button();
            $("[id$=btnRegIncomeStatement]").button();
            $("[id$=btnRegHoldings]").button();
            $("[id$=btnInvestorSummary]").button();
            $("[id$=btnNoticeofExpiration]").button();
            $("[id$=btnSaveSubtax]").button();
            $("[id$=btnSubtaxReceipt]").button();
            $("[id$=btnRegAddRemark]").button().css("padding-top", "0").css("padding-bottom", "0");
            $("[id$=btnRegLoadSubtax]").button().css("padding-top", "0").css("padding-bottom", "0");
            $("[id$=btnCPFromStateSaveData]").button();
            $("[id$=btnCPFromStatePrintReceipt]").button();
            $("[id$=btnRemoveParcelFromGrid]").button();
            $("[id$=btnCPFromStateAddParcel]").button();


            // SSAN Autocomplete
            $("[id$=txtRegSSAN]").autocomplete({
                minLength: 1,
                source: function (request, response) {
                    $.ajax({
                        type: "POST",
                        url: "TaxInvestors.aspx/GetInvestor",
                        data: '{"InvestorIDorSSAN":"' + request.term + '"}',
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function (data, textStatus, jqXHR) {
                            var result = [];
                            $.each(data.d, function (index, value) {
                                result.push({ value: index, label: value });
                            });
                            response(result);
                        }
                    });
                },
                select: function (event, ui) {
                    if ("0" == ui.item.value) {
                        $("[id$=btnRegAddNew]").click();
                        event.preventDefault();
                    }
                    else {
                        $("[id$=txtRegInvestorID]").val(ui.item.value);
                        $("[id$=btnRegSearch]").click();
                    }
                }
            });

            // Add datepicker to add remarks popup
            $("[id$=txtRemarkDate]").datepicker();

            // Dialog Box for divAddRemark.
            $("#divAddRemark").dialog({
                autoOpen: false,
                modal: true,
                title: "Add Investor Remark",
                minWidth: 404,
                buttons: {
                    "Add Remark": function () {
                        $(this).dialog("close");
                        $("[id$=btnRegAddNewRemark]").click();
                    },
                    "Cancel": function () {
                        $(this).dialog("close");
                    }
                }
            }).parent().appendTo($("form:first"));

            // btnRegAddRemark - Click Event
            $("[id$=btnRegAddRemark]").click(function (event, ui) {
                $("#divAddRemark").dialog("open");
                event.preventDefault();
            });

            // chkSubtaxAll Checked Event - This will check all subtax checkboxes in grid
            $("[id$=chkSubtaxAll]").change(function () {
                if ($(this).is(":checked")) {
                    // Check all checkboxes in grid.
                    $("[id$=chkSubtax]").attr("checked", "checked");
                }
                else {
                    // Uncheck all checkboxes in grid.
                    $("[id$=chkSubtax]").removeAttr("checked");
                }
                showSubtaxTotal();
            });

            // chkSubtax Checked event - This will recalc the subtotal for each subtax checkbox in grid.
            $("[id$=chkSubtax]").change(function () {
                showSubtaxTotal();
            });

            showSubtaxTotal();



            $("#btnLogin").click(function (e) {
                if (!$("form").valid()) {
                    e.preventDefault();
                }
            });

            $("#btnLogout").click(function (e) {
                if (!$("form").valid()) {
                    e.preventDefault();
                }
            });


            $("#btnHeaderLogout").click(function (e) {
                showLogoutDialog()
            });
        });


        function showLoginDialog() {
            $("#divLogin").dialog({
                modal: true
            }).parent().appendTo("form");
        }

        function showLogoutDialog() {
            $("#divLogout").dialog({
                modal: true
            }).parent().appendTo("form");
        }


        function showSubtaxTotal() {
            var total = new Number(0);
            var count = 0;

            // Go through each row in subtax grid.
            var rows = $("[id$=grdRegSubtax]").find(".RowStyle,.AlternatingRowStyle");

            rows.each(function (index, element) {
                var el = $(element)

                // Get checkbox.
                var chk = el.find("[id$=chkSubtax]");

                if (chk.is(":checked")) {
                    // Get total.
                    var val = new Number(el.find("[id$=hdnSubtaxTotal]").val());

                    total += val;
                    count++;
                }
            });

            $("#spnSubtaxTotal").text("$" + total.toFixed(2));
            //  alert("$" + total.toFixed(2));
            $("[id$=lblTotalAmountCPTransfer]").text(total.toFixed(2));

            if (count == 0) {
                // Disable Create Subtaxes button.
                // $("[id$=btnSaveSubtax]").attr("disabled", "disabled");
                $("[id$=btnSaveSubtax]").button("disable");
            }
            else {
                // Enable Create Subtaxes button.
                // $("[id$=btnSaveSubtax]").removeAttr("disabled");
                //$("[id$=btnSaveSubtax]").attr("disabled", "false");
                $("[id$=btnSaveSubtax]").button("enable");
            }
        }


        function showAddOrSaveDialog() {
            $("#divSaveOrAddNew").dialog({
                modal: true,
                title: "Confirm Save",
                minWidth: 512,
                close: function () {
                    $(this).dialog("destroy");
                },
                buttons: {
                    "Update SSAN": function () {
                        $("#hdnSaveAction").val("save");
                        $(this).dialog("close");
                        $("[id$=btnRegSave]").click();
                    },
                    "Add New Investor": function () {
                        $("#hdnSaveAction").val("add");
                        $(this).dialog("close");
                        $("[id$=btnRegSave]").click();
                    }
                }
            });
        }


        // Investor Holdings Summary Report
        function openInvestorHoldingsSummaryReport(reportID) {
            // Get InvestorID to feed report
            var investorID = $("#lblRegInvestorID").text();
          //  alert(reportID);
         //   alert(investorID);
            window.open('Reports/InvestorHoldingsSummary.aspx?ReportID=' + reportID + '&InvestorID=' + investorID, "_blank");
        }

        // Notice of Expiration Report
        function openNoticeofExpirationReport(reportID) {
            // Get InvestorID to feed report
            var investorID = $("#lblRegInvestorID").text();

            window.open('Reports/NoticeofExpiration.aspx?ReportID=' + reportID + '&InvestorID=' + investorID, "_blank");
        }


        //Investor Registration Summary Report
        function openInvestorRegistrationSummaryReport(reportID) {
            //Get InvestorID to feed report
            var investorID = $("#lblRegInvestorID").text();

            window.open('Reports/InvestorRegistrationSummary.aspx?ReportID=' + reportID + '&InvestorID=' + investorID, "_blank");
        }

        // Investor Income Statement Report
        function openInvestorIncomeStatementReport(reportID) {
            // Get InvestorID to feed report
            var investorID = $("#lblRegInvestorID").text();

            window.open('Reports/InvestorIncomeStatement.aspx?ReportID=' + reportID + '&InvestorID=' + investorID, "_blank");
        }

        function showMessage(message, title) {
            if (!title) {
                title = "Message";
            }

            $("#pMessage").text(message);

            $("#divMessage").dialog({
                title: title,
                modal: true,
                buttons: {
                    Ok: function () {
                        $(this).dialog("close");
                    }
                },
                close: function (event, ui) {
                    $(this).dialog("destroy");
                }
            });
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <!-- Hidden controls -->
    <asp:HiddenField ID="hdnSaveAction" runat="server" />
    <!-- Modal dialogs -->
    <div id="divSaveOrAddNew" class="divPopup">
        SSAN has been changed from
        <asp:Label ID="lblRegPrevSSAN" runat="server" Font-Bold="true"></asp:Label>
        to
        <asp:Label ID="lblRegNewSSAN" runat="server" Font-Bold="true"></asp:Label>.
        <br />
        Do you want to add a new investor or update the investor's SSAN?
    </div>
    <div id="divLogin" title="Login" class="divPopup">
        <fieldset>
            <b>Cashier:</b>
            <asp:Label ID="lblLoginUsername" runat="server"></asp:Label>
            <br />
            <b>Cash at Login:</b>
            <asp:TextBox ID="txtLoginStartCash" runat="server" Width="100px" CssClass="required number"></asp:TextBox>
        </fieldset>
        <asp:Button ID="btnLogin" runat="server" Text="Login" />
    </div>
    <div id="divLogout" title="Logout" class="divPopup">
        <fieldset>
            <b>Cashier:</b>
            <asp:Label ID="lblLogoutUsername" runat="server"></asp:Label>
            <br />
            <b>Cash at Logout:</b>
            <asp:TextBox ID="txtLogoutEndCash" runat="server" Width="100px" CssClass="required number"></asp:TextBox>
        </fieldset>
        <asp:Button ID="btnLogout" runat="server" Text="Logout" />
    </div>
    <div id="divMessage" class="divPopup">
        <p id="pMessage">
        </p>
    </div>
    <!-- Header -->
    <div class="header">
        <table>
            <tr>
                <td rowspan="3">
                    <img alt="Logo" width="74" height="74" src="logo.png" />
                </td>
            </tr>
            <tr>
                <td>
                     <h1 id="loadCountyTitle" runat="server">
                        </h1>
                </td>
                <td>
                    Operator:
                    <asp:Label ID="lblOperatorName" runat="server"></asp:Label>
                    <asp:Label ID="lblSessionID" runat="server" Visible ="false"></asp:Label>
                </td>
                <td>
                    Login Time:
                    <asp:Label ID="lblLoginTime" runat="server"></asp:Label>
                </td>

            </tr>
            <tr>
                <td>
                    <h2>
                        Cashier</h2>
                </td>
                <td>
                    Date:
                    <asp:Label ID="lblCurrentDate" runat="server"></asp:Label>
                </td>
                <td>
                    Cash in Register at Login:
                    <asp:Label ID="lblStartCash" runat="server"></asp:Label>
                </td>
                <td>
                    <input id="btnHeaderLogout" type="button" value="Logout" />
                </td>
            </tr>
        </table>
    </div>
    <!-- Main tabs -->
    <div id="mainTabs">
        <ul>
            <li><a href="#tabPayments">Payments</a></li>
            <li><a href="#tabCashierActivity">Session</a></li>
      <%--      <li><a href="#tabApportion">Apportion</a></li>--%>
            <li><a href="#tabInvestors">Investors</a></li>
           <%-- <li><a href="#tabLetters">Letters</a></li>--%>
        </ul>
        <!-- Payments tab -->
        <div id="tabPayments">
        </div>
        <!-- Cashier activity tab -->
        <div id="tabCashierActivity">
        </div>
       <%-- <!-- Apportion tab -->
        <div id="tabApportion">
        </div>--%>
        <!-- Investors & CP tab -->
        <div id="tabInvestors">
            <ajaxToolkit:TabContainer ID="tabContainer" runat="server" ActiveTabIndex="0">
                <ajaxToolkit:TabPanel ID="tabRegistration" runat="server" HeaderText="Registration and Subtax" >
                    <ContentTemplate>
                        <div>
                            <!-- Dialogs and hidden controls -->
                            <asp:TextBox ID="txtRegInvestorID" runat="server" Style="display: none;"></asp:TextBox>
                            <asp:Button ID="btnRegSearch" runat="server" Text="Search" CausesValidation="False"
                                Style="display: none;" />
                            <asp:Button ID="btnRegAddNew" runat="server" Text="Add" CausesValidation="False"
                                Style="display: none;" />
                            <asp:Button ID="btnRegAddNewRemark" runat="server" Text="Add Remark" CausesValidation="False"
                                Style="display: none;" />
                            <div id="divAddRemark" class="divPopup">
                                <fieldset>
                                    <asp:Label ID="lblRemarkText" runat="server" AssociatedControlID="txtRemarkText"
                                        Text="Remark" Style="display: block;"></asp:Label>
                                    <asp:TextBox ID="txtRemarkText" runat="server" Style="display: block;" TextMode="MultiLine"
                                        Width="340px" Rows="3"></asp:TextBox>
                                    <asp:Label ID="lblRemarkDate" runat="server" AssociatedControlID="txtRemarkDate"
                                        Text="Date" Style="display: block;"></asp:Label>
                                    <asp:TextBox ID="txtRemarkDate" runat="server" Style="display: block;"></asp:TextBox>
                                    <asp:Label ID="lblRemarkImage" runat="server" AssociatedControlID="uplRemarkImage"
                                        Text="Document" Style="display: block;"></asp:Label>
                                    <asp:FileUpload ID="uplRemarkImage" runat="server" Style="display: block;" Width="340px" />
                                </fieldset>
                            </div>
                            <div style="float: left; width: 40%;">
                                <table style="padding-bottom: 20px;">
                                    <caption>
                                        <h3 style=" float: left;">
                                            Investor Detail
                                        </h3>
                                        <tr>
                                            <td>
                                                SSAN:<br />
                                                <br />
                                            </td>
                                            <td>
                                                <asp:TextBox ID="txtRegSSAN" runat="server" TabIndex="1" Width="150px"></asp:TextBox>
                                                <asp:RequiredFieldValidator ID="reqRegSSAN" runat="server" 
                                                    ControlToValidate="txtRegSSAN" Display="Dynamic" 
                                                    ErrorMessage="SSAN cannot be blank" Font-Bold="True" Text="*" 
                                                    ValidationGroup="Registration"></asp:RequiredFieldValidator>
                                                <asp:RegularExpressionValidator ID="regRegSSAN" runat="server" 
                                                    ControlToValidate="txtRegSSAN" Display="Dynamic" 
                                                    ErrorMessage="SSAN format is invalid. Expected ###-##-#### or ##-#######" 
                                                    Font-Bold="True" Text="*" 
                                                    ValidationExpression="(\d{3}-\d{2}-\d{4})|(\d{2}-\d{7})|\d{9}" 
                                                    ValidationGroup="Registration"></asp:RegularExpressionValidator>
                                                <br />
                                                <br />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                First Name:
                                            </td>
                                            <td>
                                                <asp:TextBox ID="txtRegFirstName" runat="server" TabIndex="2" Width="150px"></asp:TextBox>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                Middle Name:
                                            </td>
                                            <td>
                                                <asp:TextBox ID="txtRegMiddleName" runat="server" TabIndex="3" Width="150px"></asp:TextBox>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                Last/Corporate Name:
                                            </td>
                                            <td>
                                                <asp:TextBox ID="txtRegLastName" runat="server" TabIndex="4" Width="250px"></asp:TextBox>
                                                <asp:RequiredFieldValidator ID="reqRegLastName" runat="server" 
                                                    ControlToValidate="txtRegLastName" Display="Dynamic" 
                                                    ErrorMessage="Last (Corporate) Name cannot be blank" Font-Bold="True" Text="*" 
                                                    ValidationGroup="Registration"></asp:RequiredFieldValidator>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                Address 1:
                                            </td>
                                            <td>
                                                <asp:TextBox ID="txtRegAddress1" runat="server" TabIndex="5" Width="250px"></asp:TextBox>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                Address 2:
                                            </td>
                                            <td>
                                                <asp:TextBox ID="txtRegAddress2" runat="server" TabIndex="6" Width="250px"></asp:TextBox>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                New World VID:
                                            </td>
                                            <td>
                                                <asp:TextBox ID="txtNewWorldVID" runat="server" TabIndex="7" Width="250px"></asp:TextBox>
                                            </td>
                                        </tr>
                                    </caption>
                                </table>
                            </div>
                            <div style="float: left; width: 30%;">
                                <table style="padding-bottom: 20px; padding-top: 10px">
                                    <tr>
                                        <td>
                                            Investor ID:<br />
                                            <br />
                                        </td>
                                        <td>
                                            <asp:Label ID="lblRegInvestorID" runat="server" ClientIDMode="Static"></asp:Label>
                                            <br />
                                            <br />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            City:
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtRegCity" runat="server" TabIndex="8"></asp:TextBox>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            State:
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtRegState" runat="server" TabIndex="9"></asp:TextBox>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            Zip:
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtRegZip" runat="server" TabIndex="10"></asp:TextBox>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            Phone:
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtRegPhone" runat="server" TabIndex="11"></asp:TextBox>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            E-mail:
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtRegEmail" runat="server" TabIndex="12" Width="250px"></asp:TextBox>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <div style="float: left; width: 30%; padding-bottom: 100px;">
                                Reports:<br />      
                                                       
                                <asp:LinkButton ID="btnInvestorSummary" runat="server" Text="Investor Summary" CausesValidation="False"
                                    ClientIDMode="Static" TabIndex="16" 
                                    OnClientClick="openInvestorRegistrationSummaryReport(25); return false;" /><br />
                                <asp:LinkButton ID="btnRegIncomeStatement" runat="server" Text="Income Statement" CausesValidation="False"
                                    TabIndex="17" ClientIDMode="Static" OnClientClick="openInvestorIncomeStatementReport(26); return false;" /><br />
                                <asp:LinkButton ID="btnRegHoldings" runat="server" Text="Holdings" CausesValidation="False"
                                    TabIndex="18" ClientIDMode="Static" OnClientClick="openInvestorHoldingsSummaryReport(27); return false;" /><br />
                                <asp:LinkButton ID="btnNoticeofExpiration" runat="server" Text="Notice of Expiration"
                                    CausesValidation="False" TabIndex="19" ClientIDMode="Static" 
                                    OnClientClick="openNoticeofExpirationReport(32); return false;" />
                            </div>
                            <div style="width: 100%; float: left; padding-bottom: 10px;">
                                <asp:CheckBox ID="chkRegActive" runat="server" Text="Active:" TextAlign="Left" TabIndex="12" />
                                &nbsp;
                                <asp:CheckBox ID="chkRegConfidential" runat="server" Text="Confidential:" TextAlign="Left"
                                    TabIndex="13" />
                                &nbsp;
                                <asp:CheckBox ID="chkRegReturnedMail" runat="server" Text="Returned Mail:" TextAlign="Left"
                                    TabIndex="14" />
                                <br />
                                <br />
                                <asp:ValidationSummary ID="idRegistration" runat="server" ValidationGroup="Registration"
                                    HeaderText="Fix these errors before saving:" ShowMessageBox="True" />
                                <asp:Button ID="btnRegSave" runat="server" Text="Save" ValidationGroup="Registration"
                                    ToolTip="Save changes" TabIndex="15" />
                                <asp:Button ID="btnRegClear" runat="server" Text="Clear" CausesValidation="False"
                                    ToolTip="Clear text boxes and unload investor data" ForeColor="OrangeRed" TabIndex="16" />
                            </div>

                             <!-- Subtax -->
                            <div style="padding-top:100px;">
                            <h3 align="left" style="width: 553px">
                                Subtax Candidates
                                <asp:Button ID="btnRegLoadSubtax" runat="server" Text="Load" />
                            </h3>
                            <div style=" padding-bottom:15px">
                            <asp:GridView ID="grdRegSubtax" runat="server" AutoGenerateColumns="False" 
                                ClientIDMode="Static" HorizontalAlign="Left">
                                <Columns>
                                    <asp:TemplateField>
                                        <HeaderTemplate>
                                            <asp:CheckBox ID="chkSubtaxAll" runat="server" ClientIDMode="Static" />
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <asp:CheckBox ID="chkSubtax" runat="server" />
                                            <asp:HiddenField ID="hdnTaxYear" runat="server" Value='<%#DataBinder.Eval(Container.DataItem, "TAXYEAR") %>' />
                                            <asp:HiddenField ID="hdnTaxRollNumber" runat="server" Value='<%#DataBinder.Eval(Container.DataItem, "TAXROLLNUMBER")%>' />
                                        </ItemTemplate> 
                                    </asp:TemplateField>
                                    <asp:BoundField HeaderText="Investor" DataField="Investor" />
                                    <asp:BoundField HeaderText="Parcel Number" DataField="Parcel" />
                                    <asp:BoundField HeaderText="Bid Rate" DataField="Bid Rate" />
                                    <asp:BoundField HeaderText="Taxes" DataField="Current Balance" DataFormatString="{0:c}" />
                                    <asp:BoundField HeaderText="Interest" DataField="Interest" DataFormatString="{0:c}" />
                                    <asp:BoundField HeaderText="Fees" DataField="Subtax Fee" DataFormatString="{0:c}" />
                                    <asp:TemplateField HeaderText="Total">
                                        <ItemTemplate>
                                            <%#GetTotalSubTax(Container.DataItem).ToString("c")%>
                                            <asp:HiddenField ID="hdnSubtaxTotal" runat="server" Value='<%#GetTotalSubTax(Container.DataItem)%>' />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                            </div>
                            
                            </div>


                            <!-- Investor Remarks -->
                            
                            <div>
                                <h3 style="height: 58px; width: 351px; margin-left: 0px; padding-bottom: 4px;" 
                                    align="left">
                                    &nbsp;</h3>
                                <h3 align="left" 
                                    style="height: 58px; width: 351px; margin-left: 0px; padding-bottom: 4px;">
                                    &nbsp;</h3>
                                <h3 align="left" 
                                    style="height: 58px; width: 351px; margin-left: 0px; padding-bottom: 4px;">
                                    Investor Remarks
                                    <asp:Button ID="btnRegAddRemark" runat="server" Text="Add" />
                                </h3>
                            <div style="width: 30%; float: left; padding-bottom: 10px; padding-top: 5px;">
                                <asp:GridView ID="grdRegRemarks" runat="server" AutoGenerateColumns="False" 
                                    Height="40px" HorizontalAlign="Left">
                                    <Columns>
                                        <asp:BoundField HeaderText="Date" DataField="TASK_DATE" DataFormatString="{0:d}" />
                                        <asp:BoundField HeaderText="Remark" DataField="REMARKS" />
                                        <asp:TemplateField HeaderText="Attachment">
                                            <ItemTemplate>
                                                <%# IIf(IsDBNull(DataBinder.Eval(Container.DataItem, "IMAGE")), "&nbsp;", "<a target='_blank' href='GetBlobFromDB.ashx?tabname=genii_user.ST_INVESTOR_CALENDAR" & _
                                                                    "&colname=IMAGE&pknames=RECORD_ID&pkvalues=" & DataBinder.Eval(Container.DataItem, "RECORD_ID") & _
                                                                    "&filetype=" & DataBinder.Eval(Container.DataItem, "FILE_TYPE") & "'>" & _
                                                                    "<img border='0' src='view_image_icon.png' width='20px' height='20px' title='Click to view document' /></a>")%>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </div>
                            </div>
                            <br />
                            <br />
                            <br />
                            <br />
                            <br />
                            <br />
                            <br />
                            <br />
                            <br />
                            <br />
                            <br />                                                 
                                 
                            <tr>
                                <td>
                                 <asp:Label ID="Label1" runat="server" Text="Total Due:"></asp:Label>                              
                                </td>
                                <td>     
                                    $                             
                                    <asp:Label ID="lblTotalAmountCPTransfer" runat="server" Text="0.00" />
                                </td>
                            </tr>
                            <br /> 
                            <br /> 
                            <tr>
                                <td>
                                    <asp:Label ID="Label12" runat="server" Text="Payor:"></asp:Label>
                                </td>
                                <td  style="width: 300px; padding-right:1">
                                    <asp:TextBox ID="txtPayorName" runat="server" Width="300px" TabIndex="7" Style="text-align: right;" CssClass="ReadOnly"></asp:TextBox>
                                </td>
                            </tr>
                                
                            <tr>
                                <td>
                                    <asp:Label ID="Label15" runat="server" Text="Transaction Type:"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlPaymentType" runat="server" TabIndex="9">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            
                                
                            <tr>
                                <td>                                
                                    <asp:Label ID="Label16" runat="server" Text="Check Number:"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtCheckNumber" runat="server" TabIndex="10" Width="100px"></asp:TextBox>
                                </td>
                            </tr>
                                
                                <br />
                                <br />


                            <asp:Button ID="btnSaveSubtax" runat="server" Text="Commit Purchase" Enabled="False"
                                ClientIDMode="Static" />
                            <asp:Button ID="btnSubtaxReceipt" runat="server" Text="Receipt" Enabled="False" ClientIDMode="Static" />
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabTransfer" runat="server" HeaderText="CP Transfer">
                    <ContentTemplate>
                        <div>
                            <uc1:CPTransfer ID="CPTransfer" runat="server" />
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabPurchase" runat="server" HeaderText="CP Purchase from State">
                    <ContentTemplate>
                        <div>
                            <uc2:CPPurchaseFromState ID="CPPurchaseFromState1" runat="server" />
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
            </ajaxToolkit:TabContainer>
        </div>
        <!-- Letters tab -->
        <div id="tabLetters">
        </div>
    </div>
    </form>
</body>
</html>
