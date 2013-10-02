<%@ Page Language="VB" AutoEventWireup="false" CodeFile="TaxPayments.aspx.vb" Inherits="TaxPayments"
    StylesheetTheme="Blue" ErrorPage="~/ErrorPage.aspx" %>

<%@ Register TagPrefix="ajaxToolkit" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Cashier</title>
    <link href="Css/redmond/jquery-ui-1.8.23.custom.css" rel="stylesheet" type="text/css" />
    <link href="Css/Tax.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="JavaScript/jquery-1.5.1.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery-ui-1.8.23.custom.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.maskedinput-1.3.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.validate.js"></script>
    <script type="text/javascript">
        $(document).ready(function () {
            $("#mainTabs").tabs({
                select: function (event, ui) {
                    switch (ui.index) {
                        case 0: window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
                            break;
                        case 1: window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
                            break;
                        //                        case 3:                              
                        //                            window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");                              
                        //                            break;                              
                        case 2:
                            window.location.href = "TaxInvestors.aspx#tabInvestors";
                            break;
                    }
                }
            }).tabs("select", window.location.hash);

            // Apply Jquery UI button styling
            $("#btnFindTaxInfo").button();
            $("#btnLogin").button();
            $("#btnLogout").button();
            $("#btnHeaderLogout").button();
            $("#btnLettersPrint").button();
            $("#btnCreateApportionment").button();
            $("#btnShowAccountRemarksPopup").button();
            $("#btnComputePriorYears").button();
            $("#btnPrintReceipt").button();
            $("#btnShowOtherYearRemarksPopup").button();
            $("#btnSavePayment").button();
            $("#btnRejectPayment").button();
            $("#btnGoLoadInterestCalc").button();

            $("form").submit(function () {
                // Form submit
                var action = document.getElementById("form1").action;

                if (action.lastIndexOf("#") >= 0) {
                    action = action.substr(0, action.lastIndexOf("#"));
                }

                document.getElementById("form1").action = action + window.location.hash;
            });

            $("#txtAmountPaid").keypress(function (e) {
                if (String.fromCharCode(e.keyCode).match(/[^0-9]/g)) return false;
            });
            //

            $("#txtTaxRollNumber").keypress(function (e) {
                if (String.fromCharCode(e.keyCode).match(/[^0-9]/g)) return false;
            });

            $("#txtPriorYearAmount").keypress(function (e) {
                if (String.fromCharCode(e.keyCode).match(/[^0-9]/g)) return false;
            });

            $("#btnFindTaxInfo").click(function () {
                // Reset amount paid if it has not been changed.
                var txtAmountPaid = $("[id$=txtAmountPaid]");
                if (txtAmountPaid.val() == txtAmountPaid.attr("defaultValue")) {
                    txtAmountPaid.val("0");
                }

                showLoadingBox();
            });

            $("#btnSavePayment").click(function () {
                // Reset amount paid if it has not been changed.
                //  var txtAmountPaid = $("[id$=txtAmountPaid]");
                // if (txtAmountPaid.val() == txtAmountPaid.attr("defaultValue")) {
                //      txtAmountPaid.val("0");
                //  }

                showLoadingBox();
            });

            $("#ImageButton1").click(function () {
                showLoadingBox();
            });

            //            $("#btnDecline").click(function () {
            //                // Reset amount paid if it has not been changed.
            //                //  var txtAmountPaid = $("[id$=txtAmountPaid]");
            //                // if (txtAmountPaid.val() == txtAmountPaid.attr("defaultValue")) {
            //                //      txtAmountPaid.val("0");
            //                //  }

            //                showLoadingBox();
            //            });

            $("#rdoTaxRollNumber, #rdoAPN, #rdoTaxID").change(function () {
                enableDisableInputs();
            });

            enableDisableInputs();

            $("#txtTaxRollNumber").focus(function () {
                $("#rdoTaxRollNumber").attr("checked", "checked");
            });

            $("#txtTaxID").focus(function () {
                $("#rdoTaxID").attr("checked", "checked");
            });

            $("#txtTaxAccount").focus(function () {

            }).mask("999-99-999?a");

            // Apply 999-99-9999 mask to txtAPN
            $("#txtAPN").focus(function () {
                $("#rdoAPN").attr("checked", "checked");
            }).mask("999-99-999?a");

            // Enable ENTER key for txtAPN and txtTaxRollNumber textboxes
            $("#txtAPN, #txtTaxRollNumber, #txtTaxID").keypress(function (e) {
                if (e.keyCode == 13) {
                    $("#ImageButton1").click();
                    return false;
                }
            });

            $("#txtRegSSAN").focus(function () {
                $("#rdoAPN").attr("checked", false);
                $("#rdoTaxID").attr("checked", false);
                $("#rdoTaxRollNumber").attr("checked", false);
                $("#chkTaxYear").attr("checked", false);
                $("#ddlTaxYear").attr("disabled", true);

            });


            $("[id$=txtAmountPaid]").change(function () {
                calculateDifference();
            });


            calculateDifference();

            $("#txtBarcode").focus();
            $("#txtBarcode").keydown(txtBarcode_Keydown);

            // Validation.
            $("form").validate({
                ignoreTitle: true,
                ignore: ":hidden",
                errorClass: "ui-state-error",
                errorPlacement: function (error, element) {
                    element.after(error).after(" ");
                }
            });


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

            $("#chkTaxYear2").click(function () {
                if (this.checked) {
                    $("#ddlTaxYear2").attr('disabled', false);
                } else {
                    $("#ddlTaxYear2").attr('disabled', true);
                }
            });

            //            $("[id$=btnSearchNameAddress]").click(function () {
            //                $("[id$=txtRegSSAN]").autocomplete();
            //            });
            //btnSearchNameAddress           

            // SSAN Autocomplete
            $("[id$=txtRegSSAN]").autocomplete({
                //,"SearchParam":"' + $("#ddlSearchIn").val() + '"
                delay: 500,
                minLength: 1,
                source: function (request, response) {
                    $.ajax({
                        type: "POST",
                        url: "TaxInvestors.aspx/GetInvestorTR",
                        data: '{"InvestorIDorSSAN":"' + request.term +
                        '","TaxYear":"' + $("#ddlTaxYear").val() + '","SearchParam":"' +
                        $("#ddlSearchIn").val() + '","BalanceOnly":"' +
                        $("#chkBalanceOnly").attr('checked') + '","Name1":"' +
                        $("#chkName1Only").attr('checked') + '","Drop":"' + $("#rdoDrop").attr('checked') + '","CheckTaxYear":"' + $("#chkTaxYear2").attr('checked') + '","TextTaxYear":"' + $("#ddlTaxYear2").val() + '"}', //

                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function (data, textStatus, jqXHR) {
                            var result = [];
                            var names = [];
                            if ($("#rdoDrop").attr('checked')) {
                                $.each(data.d, function (index, value) {
                                    result.push({ value: index, label: value });
                                });
                                response(result);
                            } else if ($("#rdoPop").attr('checked')) {

                                $.each(data.d, function (index, value) {
                                    result.push({ value: index, label: value });
                                    $("#listPopupResult").append('<option value="' + value + '">' + value + '</option>');

                                });

                                showPopupResult("Search Results");
                            }
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


            $("#btnHeaderLogout").click(function (e) {
                showLogoutDialog()
            });

            $("#btnRegSearch").click(function (e) {
                showLoadingBox();
            });
            //
            $("#ddlSearchIn").change(function (e) {
                if ($("#ddlSearchIn").val() == "Address") {
                    $("#chkName1Only").attr("disabled", true);
                } else {
                    $("#chkName1Only").attr("disabled", false);
                }
            });


            $("#txtPaymentDate").datepicker();

            $("#txtTargetDate").datepicker();

            $("#spnCheckNumber").hide();


            $("[id$=ddlPaymentType]").change(function (e) {
                if (1 == $(this).val()) {
                    // Check.
                    $("#spnCheckNumber").show();
                } else {
                    // Other payment types.
                    $("#spnCheckNumber").hide();
                }
            }).change();

            // Add remark
            $("[id$=txtRemarkDate]").datepicker();




            $("[id$=btnShowAccountRemarksPopup]").click(function (event, ui) {
                // $("#divAddRemark").dialog("open");
                showAddRemarkPopup("Account");

                event.preventDefault();
            });

            $("[id$=btnShowOtherYearRemarksPopup]").click(function (event, ui) {
                // $("#divAddRemark").dialog("open");
                showAddRemarkPopup("Other Year");

                event.preventDefault();
            });

            $("[id$=btnRejectPayment]").click(function (event, ui) {
                // $("#divAddRemark").dialog("open");
                showRejectPayment("Reject Payment");

                event.preventDefault();
            });

            $("#txtTaxID").keyup(function (event) {
                if (event.keyCode == 13) {
                    $("#ImageButton1").click();
                }
            });

            $("#txtAPN").keyup(function (event) {
                if (event.keyCode == 13) {
                    $("#ImageButton1").click();
                }
            });

            $("#txtTaxRollNumber").keyup(function (event) {
                if (event.keyCode == 13) {
                    $("#ImageButton1").click();
                }
            });

        });

        function showPopupResult(title) {

            $("#divPopupResult").dialog({
                modal: true,
                title: title,
                width: 750,
                close: function (e, ui) {
                    $(this).dialog("destroy");
                }
            });
            // 
        }


        function showAddRemarkPopup(type) {
            // Set up Add Remark Popup window
            $("#divAddRemark").dialog({
                autoOpen: false,
                modal: true,
                title: "Add " + type + " Remark",
                minWidth: 404,
                buttons: {
                    "Add Remark": function () {
                        $(this).dialog("close");

                        switch (type) {
                            case "Account":
                                $("[id$=btnAddNewAccountRemarks]").click();
                                break;

//                            case "Tax Roll":
//                                $("[id$=btnAddNewTaxRollRemarks]").click();
//                                break;

                            case "Other Year":
                                $("[id$=btnAddNewOtherYearRemarks]").click();
                                break;
                        }

                    },
                    "Cancel": function () {
                        $(this).dialog("close");
                    }
                }
            }).parent().appendTo($("form:first"));


            $("#divAddRemark").dialog("open");
        }

        function getTabID() {
            var matches = /[?&]tab=([^&$]*)/i.exec(window.location.search);
            if (null !== matches)
                return matches[1];
            else
                return -1;
        }

        function showLoadingBox() {
            $("#divLoading").dialog({
                title: "Loading",
                modal: true,
                position: "center",
                width: 260,
                height: 100,
                minHeight: 70,
                resizable: false
            });
        }

        function showRejectPayment(title) {
            //  $("[id$=lblDeleteDivPayorName]").val("MIA");

            $("#divRejectConfirmation").dialog({
                modal: true,
                title: title,
                width: 500,
                close: function (e, ui) {
                    $(this).dialog("destroy");
                }
            });
        }

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


        function isNumber(n) {
            return !isNaN(parseFloat(n)) && isFinite(n);
        }

        function enableDisableInputs() {
            if ($("#rdoTaxRollNumber").is(":checked")) {
                $("#txtTaxRollNumber").focus();
            }
            else if ($("#rdoAPN").is(":checked")) {
                $("#txtAPN").focus();
            }
            else if ($("#rdoTaxID").is(":checked")) {
                $("#txtTaxID").focus();
            }
        }

        function calculateDifference() {
         //   alert("aaaaa");
            var amountDue = $("[id$=hdnTxtRequiredAmount]").val();
            var amountPaid = $("[id$=txtAmountPaid]").val();
            var minimRefundAmount = $("#hdnMinimumRefundAmount").val();

            amountPaid = amountPaid.replace(/\,/g, "");

            if (isNaN(amountDue)) amountDue = 0;
            if (isNaN(amountPaid)) amountPaid = 0;
            var diff = amountPaid - amountDue;

            if (diff > 0) {
                $("[id$=rdoAmountUnder]").hide();
                $("[id$=rdoAmountOver]").show();

                if (Math.abs(diff) <= minimRefundAmount) {
                    // Under minimum refund amount. Select "Accept & Kitty"
                    $("[id$=rdoAmountOver] [value=kitty]").removeAttr("disabled").attr("checked", "checked");
                } else {
                    // Over minimum refund amount. Disable kitty and select refund.
                    $("[id$=rdoAmountOver] [value=refund]").attr("checked", "checked");
                    $("[id$=rdoAmountOver] [value=kitty]").attr("disabled", "disabled");
                }

                diff = "+" + diff.toFixed(2);
            } else if (diff < 0) {
                $("[id$=rdoAmountOver]").hide();
                $("[id$=rdoAmountUnder]").show();

                if (Math.abs(diff) <= minimRefundAmount) {
                    // Under minimum refund amount. Select "Accept & Write-off"
                    $("[id$=rdoAmountUnder] [value=writeoff]").removeAttr("disabled").attr("checked", "checked");
                } else {
                    // Over minimum refund amount. Disable write-off and select partial payment.
                    $("[id$=rdoAmountUnder] [value=partial]").attr("checked", "checked");
                    $("[id$=rdoAmountUnder] [value=writeoff]").attr("disabled", "disabled");
                }

                diff = "( " + diff.toFixed(2) + " )";
            } else {
                $("[id$=rdoAmountUnder]").hide();
                $("[id$=rdoAmountOver]").hide();
            }

          //  $("[id$=txtDifference]").val(diff);
        }

        function txtBarcode_Keydown(e) {
            //alert("mia");
            var key = e.charCode ? e.charCode : e.keyCode ? e.keyCode : 0;
            if (13 == key) {
                e.preventDefault();
                decodeBarcode();
            }
        }

        function decodeBarcode() {
            var bc = new String($("#txtBarcode").val());
            if (bc.charAt(0) != "#") {
                bc = "#" + bc;
            }

            if (bc.length == 29) {
                decodeParcelNumber(bc);
            } else if (bc.length == 38) {
                decodeTaxID(bc);
            }
        }

        function decodeTaxID(barcode) {
            var taxRollNumber = barcode.substr(13, 7);
            var taxYear = barcode.substr(20, 4);
            var amount = barcode.substr(24, 14);

            if (isNumber(amount)) {
                amount = parseInt(amount, 10) / 100;
            } else {
                amount = "";
            }

            $("#ddlTaxYear").val(taxYear);
            $("#txtTaxRollNumber").val(taxRollNumber);
          //  $("[id$=txtAmountPaid]").val(amount);

            $("#rdoTaxRollNumber").click();
            enableDisableInputs();
            $("#ImageButton1").click();
        }

        function decodeParcelNumber(barcode) {
            var apn = barcode.substr(3, 10);
            var taxYear = barcode.substr(14, 4);
            var amount = barcode.substr(18, 11);

            var book = apn.substr(0, 3);
            var map = apn.substr(3, 2);
            var parcel = apn.substr(5, 3);
            var split = apn.substr(8, 2);

            split = getSplitCharacter(split);
            if (split == "") split = "_";
            if (isNumber(amount)) {
                amount = parseInt(amount, 10) / 100;
            } else {
                amount = "";
            }

          //  $("#ddlTaxYear").val(taxYear);
            $("#txtAPN").val(book + "-" + map + "-" + parcel + split);
         //   $("[id$=txtAmountPaid]").val(amount);

            $("#rdoAPN").click();
            enableDisableInputs();
            $("#ImageButton1").click();
        }

        function getSplitCharacter(splitNumber) {
            var numSplit = parseInt(splitNumber, 10);
            if (0 == numSplit) {
                return "";
            } else {
                return String.fromCharCode(64 + numSplit);
            }
        }

        function openLetterDetailsDialog(title) {
            $("#divLetterDetail").dialog({
                modal: true,
                title: title,
                width: 800,
                close: function (e, ui) {
                    $(this).dialog("destroy");
                }
            });
        }

        function checkRDOValue(rdoValue) {
            if (rdoValue == "rdoAPN") {
                $('#ddlTaxYear').attr('disabled',true);
                $("#chkTaxYear").attr("checked", false);
                $("#txtAPN").css("background-color", "");
                $("#txtTaxRollNumber").css("background-color", "gray");
                $("#txtTaxID").css("background-color", "gray");
            } else if (rdoValue == "rdoTaxRollNumber") {
                $('#ddlTaxYear').attr('disabled', false);
                $("#chkTaxYear").attr("checked", true);
                $("#txtTaxRollNumber").css("background-color", "");
                $("#txtAPN").css("background-color", "gray");
                $("#txtTaxID").css("background-color", "gray");
            } else if (rdoValue == "rdoTaxID") {
                $('#ddlTaxYear').attr('disabled', true);
                $("#chkTaxYear").attr("checked", false);
                $("#txtTaxID").css("background-color", "");
                $("#txtTaxRollNumber").css("background-color", "gray");
                $("#txtAPN").css("background-color", "gray");
            } else {
            //do nothing
            }
        }

        function checkAllCP() {
            $("#chkCPSelectAll").attr("checked", true);
        }

        function clickFindAccount() {
            setInterval(function () { document.getElementById("ImageButton1").click(); }, 500);
        }

        function checkKey(e) {
            if (e.keycode == 13) {
                setInterval(function () { document.getElementById("ImageButton1").click(); }, 500);
            }
        }

        function loadInvestor(investor) {
            var investor = $("#listPopupResult").val();
            investor = investor.substr(3, 10);
            investor = investor.replace(/,/g, '');
            investor = investor.replace(/\s/g, '');            
            $("#txtTaxID").val(investor);
            $("#rdoTaxID").attr('checked', true);            
            $('#divPopupResult').dialog('close');
            $("#ImageButton1").click();

        }

        function showInterestCalcAction() {

            showInterestCalc()

        }

        function showInterestCalc() {
            $("#divInterestCalc").dialog({
                modal: true,
                title: "Interest Calculator",
                width: 1000,
                height: 500,
                close: function (e, ui) {
                    $(this).dialog("destroy");
                }
            });
        }


        function loadTaxID() {
            $("#txtAPN").val($("#txtTaxAccount").val());
            $("#rdoAPN").checked('checked', true);
          //  $("#hdnTargetDate").val($("#txtTargetDate").val());
            $("[id$=hdnTargetDate]").val($("#txtTargetDate").val());

        }

     


//        function doSearch(text) {
//            alert("mia");
//            var delayTimer;
//            clearTimeout(delayTimer);
//            delayTimer = setTimeout(function () {
//                // Do the ajax stuff
//            }, 10000); // Will do the ajax stuff after 1000 ms, or 1 s
//        }


    </script>
    <style type="text/css">
        .style2
        {
            width: 440px;
        }
        .style16
        {
            height: 44px;
        }
        .style20
        {
            height: 29px;
        }
        .style21
        {
            height: 33px;
        }
        .style22
        {
            width: 440px;
            height: 33px;
        }
        .style25
        {
            height: 44px;
            width: 156px;
        }
        .style27
        {
            height: 33px;
            width: 156px;
        }
        .style28
        {
            height: 29px;
            width: 156px;
        }
        .style30
        {
            height: 23px;
        }
        .style31
        {
            height: 26px;
        }
        .style33
        {
            width: 156px;
        }
        .style34
        {
            width: 362px;
        }
        </style>
</head>
<body>
<script type="text/javascript">

    $(document).ready(function () {
        //        $("#ddlInterest").change(function () {
        //            Gridview1 = document.getElementById('<%=grdPriorYears.ClientID%>');
        //            var idx = gridview1.
        //            var cell = Gridview1.rows[vIndex].cells[4]
        //            var dropdownSelectedValue = cell.childNodes[0].value;
        //            alert(dropdownSelectedValue);
        //        });

        $("[id$=ddlInterest]").change(function (event, ui) {

            //            var idx;
            //            var interest;
            //            var a;
            $("#<%=grdPriorYears.ClientID %> tr").click(function (event) {
                idx = this.rowIndex;

                var interest = $(this).find('option:selected').text()
                interest = interest.replace(/,/g, "");

                var start_pos = interest.indexOf('(') + 1;
                var end_pos = interest.indexOf(')');
                var text_to_get = interest.substring(start_pos, end_pos);

                var payments = document.getElementById("<%=grdPriorYears.ClientID %>").rows[idx].cells[6].innerHTML;
                payments = payments.replace(/,/g, "");
                payments = payments.substr(1);


                var fees = document.getElementById("<%=grdPriorYears.ClientID %>").rows[idx].cells[5].innerHTML;
                fees = fees.replace(/,/g, "");
                fees = fees.substr(1);

                var balance = document.getElementById("<%=grdPriorYears.ClientID %>").rows[idx].cells[7].innerHTML;
                balance = balance.replace(/,/g, "");
                balance = balance.substr(1);
                var taxes = document.getElementById("<%=grdPriorYears.ClientID %>").rows[idx].cells[3].innerHTML;
                taxes = taxes.replace(/,/g, "");
                taxes = taxes.substr(1);

                var newBalance = parseFloat(text_to_get) + parseFloat(taxes) - parseFloat(payments) + parseFloat(fees);
               // alert(payments);
                document.getElementById("<%=grdPriorYears.ClientID %>").rows[idx].cells[7].innerHTML = newBalance.toFixed(2);

                var chk = document.getElementById("<%=grdPriorYears.ClientID %>").rows[idx].cells[0].childNodes[0].checked;

                if (chk == true) {
                    document.getElementById("<%=grdPriorYears.ClientID %>").rows[idx].cells[8].children[0].value = newBalance.toFixed(2);
                }


            });

            event.preventDefault();


        });

    });

</script>
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <!-- Hidden variables -->
    <asp:HiddenField ID="hdnSessionRecordID" runat="server" />
    <asp:HiddenField ID="hdnMinimumRefundAmount" runat="server" />
    <!-- Modal popup divs -->
    <div id="divLoading" title="Loading, please wait..." class="divPopup">
        <img src="ajax-loader_redmond.gif" alt="Loading..." width="220" height="19" />
    </div>
    <div id="divMessage" class="divPopup">
        <p id="pMessage">
        </p>
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
            <asp:TextBox ID="txtLogoutEndCash" runat="server" Width="90px" CssClass="required number"></asp:TextBox>
            <br />
            <b>Required Cash:</b>
            <asp:TextBox ID="txtLogoutRequiredCash" runat="server" Enabled ="false" Width="90px" CssClass="required number"></asp:TextBox>
        </fieldset>
        <asp:Button ID="btnLogout" runat="server" Text="Logout" />
    </div>
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
    <div id="divRejectConfirmation" class="divPopup">
        <fieldset>
        Are you sure you want to reject the payment?
        <br />
        <br />
            <asp:Label ID="LblDeclinePayment" runat="server" AssociatedControlID="txtDeclineReason"
                Text="Decline Reason:"></asp:Label>
            <asp:TextBox ID="txtDeclineReason" runat="server" TextMode="MultiLine"
                Width="340px" Rows="3"></asp:TextBox>
                <br />            
        </fieldset>       
        <asp:LinkButton ID="btnDecline" runat="server" text="Confirm"  ForeColor="Red" />&nbsp;&nbsp;
        <asp:LinkButton runat="server" ID="LinkButton2" Text="Cancel" OnClientClick="$('#divRejectConfirmation').dialog('close')" ForeColor="Red" />
    </div>

    <div id="divInterestCalc" class="divPopup">
     <fieldset>
        <table>
    
            <tr>
                <td>
                     <asp:Label ID="lblTaxAccount" Text="Tax Account: " runat="server"></asp:Label>
                     <br />
                     <asp:Label ID="Label5" Text="Current Date: " runat="server"></asp:Label>
                     <br />
                     <asp:Label ID="LabelTargetDate" Text="Target Date: " runat="server"></asp:Label>
                </td>

                <td>
                    <asp:TextBox ID="txtTaxAccount" runat="server" ></asp:TextBox> <%-- ClientIDMode="Static" onclick="javascript:loadTaxID();"--%>
                    <%--<input type="text" id="txtTaxAccount2" name="txtTaxAccount2" runat="server" />--%>
                    <br />            
                    <asp:TextBox ID="txtCurrentDate" runat="server"></asp:TextBox>
                    <br />            
                    <asp:TextBox ID="txtTargetDate" runat="server"></asp:TextBox>
                </td>
                       
            </tr>

        </table>
            

            <br />
            <br />
        <asp:Label ID="LabelIntCalc" Text="Tax Rolls:" Font-Size ="Large" Font-Bold ="true" runat="server"></asp:Label>
        <br />
        <asp:GridView ID="grdInterestCalcTaxRolls" runat="server" AutoGenerateColumns="false">
            <EmptyDataTemplate>
                No Data</EmptyDataTemplate>
            <Columns>
                <asp:BoundField HeaderText="Tax ID" DataField="TaxID"/>
                <asp:BoundField HeaderText="Year" DataField="Year"/>
                <asp:BoundField HeaderText="Roll" DataField="Roll"/>
                <asp:BoundField HeaderText="Balance" DataField="Balance" />
                <asp:BoundField HeaderText="Taxes" DataField="Taxes" />
                <asp:BoundField HeaderText="(I) Current" DataField="Current (I)" />
                <asp:BoundField HeaderText="Current Fees" DataField="Current Fees" />
                <asp:BoundField HeaderText="(I) Future"/>
                <asp:BoundField HeaderText="Ad" />
                <asp:BoundField HeaderText="Sheriff"  />
                <asp:BoundField HeaderText="CP" />
                <asp:BoundField HeaderText="(I) Delta"  />
            </Columns>
        </asp:GridView>
        <br />
        <br />
        <asp:Label ID="Label7" Text="Investor CP:" Font-Size ="Large" Font-Bold ="true" runat="server"></asp:Label>
        <br />
        <asp:GridView ID="grdInvestorCP" runat="server" AutoGenerateColumns="false">
            <EmptyDataTemplate>
                No Data</EmptyDataTemplate>
            <Columns>
                <asp:BoundField HeaderText="CP" DataField="Certificate"/>
                <asp:BoundField HeaderText="Year" DataField="TaxYear"/>
                <asp:BoundField HeaderText="Roll" DataField="Roll Number"/>
                <asp:BoundField HeaderText="Value" DataField="Value" />
                <asp:BoundField HeaderText="(I) Current" DataField="Interest" />
                <asp:BoundField HeaderText="(I) Future" />
            </Columns>
        </asp:GridView>

        <br />
        <br />
        <asp:Label ID="Label8" Text="State CP:" Font-Size ="Large" Font-Bold ="true" runat="server"></asp:Label>
        <br />
        <asp:GridView ID="grdStateCP" runat="server" AutoGenerateColumns="false">
            <EmptyDataTemplate>
                No Data</EmptyDataTemplate>
            <Columns>
                <asp:BoundField HeaderText="CP" DataField="Certificate"/>
                <asp:BoundField HeaderText="Year" DataField="TaxYear"/>
                <asp:BoundField HeaderText="Roll" DataField="TaxRollNumber"/>
                <asp:BoundField HeaderText="Value" DataField="Taxes" />
                <asp:BoundField HeaderText="(I) Current" DataField="Interest" />
                <asp:BoundField HeaderText="(I) Future" />
            </Columns>
        </asp:GridView>
     </fieldset>
    

        <br />
        <br />
       <%-- <asp:Button ID="btnGoLoadInterestCalc" runat="server" Text="Go" OnClick="BindInterestCalc" />--%>
       <%--<asp:TextBox ID="txtabc" runat="server" Text="111111111" ></asp:TextBox>--%>
         <asp:LinkButton runat="server" Text="Go" ID="btnGoLoadInterestCalc" OnClick="LoadInterestCalculation" OnClientClick="javascript:loadTaxID();"/>
        <asp:Button ID="Button1" runat="server" Text="Print" />

    </div>

    <div id="divPopupResult" class="divPopup">

    <asp:ListBox ID="listPopupResult" runat="server" Font-Size ="Medium" Height ="300px" Width="700px" onclick="javascript:loadInvestor(this);"> <%--OnSelectedIndexChanged ="LoadInvestorFromPopup"--%>
    
    </asp:ListBox>

  <%--  <asp:Button ID="btnLoadInvestorPopup" runat="server"  Text="Select" />--%><%--OnClick ="LoadInvestorFromPopup"--%>
    
    </div>
    <div class="header">
        <table>
            <tr>
                <td rowspan="3">
                    <img alt="Logo" width="74" height="74" id="imgCountyLogo"  runat="server" src="logo.png"/><%--src="logo.png"--%> 

                </td>
            </tr>
            <tr>
                <td>
                    <h1 id="hdrCountyName" runat="server" >
                        </h1><%--title="Navajo County Treasurer"--%>
                </td>
                <td>
                    Operator:
                    <asp:Label ID="lblOperatorName" runat="server"></asp:Label>
                </td>
                <td>
                    Session ID:
                    <asp:Label ID="lblSessionID" runat="server"></asp:Label>
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
                    <input id="btnHeaderLogout" type="button" value="Logout"/>
                </td>
            </tr>
        </table>
    </div>
    <div id="mainTabs">
        <ul>
            <li><a href="#tabPayments">Payments</a></li>
            <li><a href="#tabCashierActivity">Session</a></li>
           <!-- <li><a href="#tabApportion">Apportion</a></li>-->
          <%--  <li><a href="#tabApportionPayments">Apportionment</a></li>--%>
            <li><a href="#tabInvestors">Investors</a></li>
           <%-- <li><a href="#tabLetters">Letters</a></li>--%>
        </ul>
        <!-- Payments tab -->
        <div id="tabPayments">

        <table>
           
            <tr>

            <td colspan ="3" style="border: 2px solid #000000; background-color: #E1E1E1;" >
                <table>
                      <tr>
                        <asp:TextBox ID="txtRegInvestorID" runat="server" Style="display: none;"></asp:TextBox>
                        <asp:Button ID="btnRegSearch" runat="server" Text="Search" ClientIDMode ="Static" CausesValidation="False"
                            Style="display: none;"/>
                        <asp:Button ID="btnRegAddNew" runat="server" Text="Add" CausesValidation="False"
                            Style="display: none;" />
                        <asp:Button ID="btnRegAddNewRemark" runat="server" Text="Add Remark" CausesValidation="False"
                            Style="display: none;" />

                    <td>
                        <%--<asp:Button ID="btnCurrentMonth" Text="Current" BackColor ="LightGreen" runat="server" Height="40px" Width="55px" Visible ="true" OnClick ="btnCurrentDate_Click" />
                        <asp:Button ID="btnPriorMonth2" Text="Prior" BackColor ="Red" runat="server" Height="40px" Width="55px" Visible ="false" OnClick ="btnPriorMonth_Click"/>--%>

                        <asp:ImageButton ID="ImageButton1" runat="server" Text="Search" ImageUrl ="~/search.jpg" ClientIDMode ="Static"/>
                    </td>

                    <td>
                        <table>
                            <tr>                            
                                <td>
                                    <asp:RadioButton ID="rdoTaxID" runat="server" GroupName="SearchGroup" Text="Tax ID:" Width ="80px" onclick="javascript:checkRDOValue('rdoTaxID');"/>
                                    
                                    <asp:RadioButton ID="rdoAPN" runat="server" GroupName="SearchGroup" Text="Parcel: " Width ="80px" onclick="javascript:checkRDOValue('rdoAPN');"/>
                                </td>
                               
                                <td>
                                     <asp:TextBox ID="txtTaxID" runat="server" Width="80px" onclick="javascript:checkRDOValue('rdoTaxID');"/>  
                                                                                    
                                    <asp:TextBox ID="txtAPN" runat="server" Width="80px" onclick="javascript:checkRDOValue('rdoAPN');"/>
                                </td>
                            </tr>
                        </table>                                                                        
                    </td>
                    <td>
                        <table>
                            <tr>
                                <td>
                                    <asp:CheckBox ID="chkTaxYear" Checked="false" Text="Tax Year:" runat="server" Width ="110px" OnCheckedChanged="chkTaxYear_checkChanged" AutoPostBack ="true"/> 
                                    <br />
                                    <asp:RadioButton ID="rdoTaxRollNumber" runat="server" GroupName="SearchGroup" Width ="125px" Text="Roll Number:"  onclick="javascript:checkRDOValue('rdoTaxRollNumber');"/> 
                                </td>

                                <td>
                                    <asp:DropDownList ID="ddlTaxYear" runat="server" Width="80px" Enabled ="false">                               
                                    </asp:DropDownList>
                                    <br />
                                    <asp:TextBox ID="txtTaxRollNumber" runat="server" Width="80px" Wrap="False" onclick="javascript:checkRDOValue('rdoTaxRollNumber');"/>
                                </td>
                            </tr>
                        </table>
                    </td>

                    <td>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="lblNameSearch" Text="Look For:" Width="80px" runat="server"></asp:Label>                                    
                                    
                                    <asp:Label ID="lblLabel6" Text="Search In:" Width="80px" runat="server"></asp:Label>
                                    
                                </td>

                                <td style =" width : 350px">
                                    <asp:TextBox ID="txtRegSSAN" runat="server" TabIndex="1" Width="269px" ></asp:TextBox>
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
                                                                                                                
                                    <asp:DropDownList ID="ddlSearchIn" runat="server" Width="300px" ClientIDMode ="Static">
                                    </asp:DropDownList>
                                   <%-- <asp:Button ID="btnSearchNameAddress" runat="server" ClientIDMode ="Static" Text="GO"/>--%>
                                </td>
                            </tr>
                        </table>
                           
                    </td>
                    <td>                       
                        <asp:CheckBox ID="chkPayBothHalves" runat="server" Text="Pay Both Halves" onclick="javascript:clickFindAccount();" Visible ="false"/>
                    </td>
                    <td style="width:150px">
                        <asp:CheckBox ID="chkBalanceOnly" Text="Balance Only" runat="server" />
                        <br />
                        <asp:CheckBox ID="chkName1Only" Text="Name 1 only" runat="server" Checked ="true"/>
                        <br />
                        <asp:CheckBox ID="chkTaxYear2" runat="server" /> 
                        <asp:DropDownList ID="ddlTaxYear2" runat="server" Width="60px" Enabled ="false">                               
                        </asp:DropDownList>
                    </td>
                    <td>
                        <asp:RadioButton GroupName ="DropPop" Text="Drop" runat="server" ID="rdoDrop"  Width="60px" Checked ="true"/>
                        <br />
                        <asp:RadioButton ID="rdoPop" GroupName ="DropPop" Text="Pop"  Width="60px" runat="server" />

                    </td>

                    <td style="width:150px">
                    <asp:HiddenField ID="hdnTargetDate" runat="server" />
                        <asp:ImageButton ID="btnInterestCalc" runat="server" Text="Search" ImageUrl ="~/calculator.jpg" OnClick="LoadInterestCalculation"/>
                        <asp:ImageButton  ID="btnCurrentMonth" runat="server"  Visible ="true" OnClick ="btnCurrentDate_Click" ImageUrl="~/current.jpg"/>
                        <asp:ImageButton  ID="btnPriorMonth2" runat="server"  Visible ="false" OnClick ="btnPriorMonth_Click" ImageUrl="~/prior.jpg" />
                   
        <%--                <asp:ImageButton ID="btnFindTaxInfo" runat="server" Text="Search" ImageUrl ="~/search.jpg" />--%>
                    </td>
                    <td>
                     Scanner:
                        <asp:TextBox ID="txtBarcode" runat="server" Width="115px" ClientIDMode ="Static"></asp:TextBox><br />
                                <asp:Button ID="Button2" runat="server" Text="Test Print" OnClick="btnPrintReceipt_Click"/>
                    </td>
                </tr>
                </table>
              
            </td>
       
                
            </tr>
            <tr>
                <td valign ="top" align="left" style="border: 2px solid #000000; background-color: #E1E1E1;padding-right: 100px; width:600px">
                        <ajaxToolkit:TabContainer ID="tabContainer1" runat="server" Width ="600px">
                            <ajaxToolkit:TabPanel ID="TabPanel2" runat="server" HeaderText="Summary" Width ="600px">
                                <ContentTemplate>

                                    <asp:Label id ="lblHdrAcctHist" Text ="Tax Year Navigator" runat="server" 
                                                Visible ="False" Font-Bold="True" Font-Size="Medium"></asp:Label>
                                                <br />
                                                 <asp:Label id ="lblParentParcel" Text ="Parent Parcel: " runat="server" 
                                                Visible ="False" Font-Bold="True" Font-Size="Small"></asp:Label>
                                                <asp:LinkButton ID="btnParentParcel" runat="server" ForeColor="Red" OnClick ="btnParentParcel_click" />
                                                <asp:GridView ID="dtaSummary" runat="server" 
                                        AutoGenerateColumns="False" AutoGenerateSelectButton="True"
                                                 OnSelectedIndexChanged ="GridView1_SelectedIndexChanged" 
                                        Font-Size="Small" BackColor="#666699">
                                                    <EmptyDataTemplate>
                                                        No Data</EmptyDataTemplate>
                                                    <Columns>
                                                        <asp:BoundField HeaderText="Tax Year" DataField="TaxYear"/>
                                                        <asp:BoundField HeaderText="Tax Roll" DataField="TaxRollNumber" />
                                                        <asp:BoundField HeaderText="Secure" DataField="SecuredUnsecured" />
                                                        <asp:BoundField HeaderText="Area" DataField="TaxArea" />
                                                        <asp:BoundField HeaderText="Status" DataField="Status" />
                                                        
                                                        <asp:BoundField HeaderText="Tax" DataField="ChargeAmount" 
                                                            DataFormatString="{0:C}" >
                                                        <ItemStyle HorizontalAlign="Right" />
                                                        </asp:BoundField>
                                                        <asp:BoundField HeaderText="Paid" DataField="NumPayments" NullDisplayText ="0"/>
                                                        <asp:BoundField HeaderText="Remitted" DataField="TotalPaymentAmount" DataFormatString="{0:C}"  NullDisplayText ="$0.00"/>
                                                      <%--  <asp:BoundField HeaderText="Remitted" DataField="Remitted" 
                                                            DataFormatString="{0:C}" >
                                                        <ItemStyle HorizontalAlign="Right" />
                                                        </asp:BoundField>--%>
                                                        <asp:BoundField HeaderText="Balance" DataField="CurrentBalance" 
                                                            DataFormatString="{0:C}" NullDisplayText ="$0.00">
                                                        <ItemStyle HorizontalAlign="Right" />
                                                        </asp:BoundField>
                                                    </Columns>
                                                </asp:GridView>
                                </ContentTemplate>
                            </ajaxToolkit:TabPanel>

                           <ajaxToolkit:TabPanel ID="tabMail" runat="server" HeaderText="Mail"  Width ="600px">
                            <ContentTemplate>
                             <div>
                                <fieldset>
                                    <table width="500px">
                                       <%-- <tr>
                                            <th colspan ="2" align="left"  >
			                                    Mail To:
		                                    </th>
                                        </tr>--%>

                                        <tr>
                                            <b>Mail To:</b>
                                            <br />

                                            <asp:TextBox ID="txtMailToAddress" runat="server" CssClass="ReadOnly" 
                            	            Height="59px" TextMode="MultiLine" Width="310px"></asp:TextBox>
                                        </tr>

                                        <tr>
                                        <td>
                                            <b>First Half Delinquent:</b>
                                            <br />
                                             <asp:TextBox ID="txtFirstHalfDelinquent" runat="server" CssClass="ReadOnly" ></asp:TextBox>
                                        </td>

                                        <td>
                                            <b>Second Half Delinquent:</b>
                                            <br />
                                             <asp:TextBox ID="txtSecondHalfDelinquent" runat="server" CssClass="ReadOnly" ></asp:TextBox>
                                        </td>
                                            
                                        </tr>
                                    </table>
                                </fieldset>
                            </div>
                            </ContentTemplate>
                            </ajaxToolkit:TabPanel>

                            <ajaxToolkit:TabPanel ID="tabAccountRemarks" runat="server" HeaderText="Remarks"  Width ="600px">
                                <ContentTemplate>
                                    <div>
                                        <fieldset>
                                            <table>
                                                <tr>
                                                    <td style="width: 150px;">
                                                        Account Remarks
                                                    </td>
                                                    <td>
                                                        <asp:Button runat="server" ID="btnShowAccountRemarksPopup" Text="Add" ClientIDMode="Static"
                                                            CausesValidation="false" Enabled="false" />
                                                        <asp:Button ID="btnAddNewAccountRemarks" runat="server" Text="Add Remark" ClientIDMode="Static"
                                                            CausesValidation="False" Style="display: none;" />
                                                        <br />
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <div style="margin-left: 20px">
                                                            <asp:GridView ID="gvAccountRemarks" runat="server" AutoGenerateColumns="false">
                                                                <Columns>
                                                                    <asp:BoundField HeaderText="Date" DataField="TASK_DATE" DataFormatString="{0:d}" />
                                                                    <asp:BoundField HeaderText="Remark" DataField="REMARKS" />
                                                                    <asp:TemplateField HeaderText="Attachment">
                                                                        <ItemTemplate>
                                                                            <%# IIf(IsDBNull(DataBinder.Eval(Container.DataItem, "IMAGE")), "&nbsp;", "<a target='_blank' href='GetBlobFromDB.ashx?tabname=genii_user.TAX_ACCOUNT_CALENDAR" & _
                                                                                "&colname=IMAGE&pknames=RECORD_ID&pkvalues=" & DataBinder.Eval(Container.DataItem, "RECORD_ID") & _
                                                                                "&filetype=" & DataBinder.Eval(Container.DataItem, "FILE_TYPE") & "'>" & _
                                                                                "<img border='0' src='view_image_icon.png' width='20px' height='20px' title='Click to view document' /></a>")%>
                                                                            <%-- <a target="_blank" href='GetBlobFromDB.ashx?tabname=genii_user.TAX_ACCOUNT_CALENDAR&colname=IMAGE&pknames=RECORD_ID&pkvalues=<%#DataBinder.Eval(Container.DataItem, "RECORD_ID") %>&filetype=<%#DataBinder.Eval(Container.DataItem, "FILE_TYPE") %>'>
                                                                                <img border="0" src="view_image_icon.png" width="20px" height="20px" title="Click to view document" />
                                                                            </a>--%>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>
                                        </fieldset>
                                    </div>
                                    <br />
                                 <%--   <div>
                                        <fieldset>
                                            <table>
                                                <tr>
                                                    <td style="width: 150px;">
                                                        Tax Roll Remarks
                                                    </td>
                                                    <td>
                                                        <asp:Button runat="server" ID="btnShowTaxRollRemarksPopup" Text="Add" ClientIDMode="Static"
                                                            CausesValidation="false" Enabled="false" />
                                                        <asp:Button ID="btnAddNewTaxRollRemarks" runat="server" Text="Add Remark" ClientIDMode="Static"
                                                            CausesValidation="False" Style="display: none;" />
                                                        <br />
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <div style="margin-left: 20px">
                                                            <asp:GridView ID="gvTaxRollRemarks" runat="server" AutoGenerateColumns="false">
                                                                <Columns>
                                                                    <asp:BoundField HeaderText="Date" DataField="TASK_DATE" DataFormatString="{0:d}" />
                                                                    <asp:BoundField HeaderText="Remark" DataField="REMARKS" />
                                                                    <asp:TemplateField HeaderText="Attachment">
                                                                        <ItemTemplate>
                                                                            <%# IIf(IsDBNull(DataBinder.Eval(Container.DataItem, "IMAGE")), "&nbsp;", "<a target='_blank' href='GetBlobFromDB.ashx?tabname=genii_user.TR_CALENDAR" & _
                                                                                "&colname=IMAGE&pknames=RECORD_ID&pkvalues=" & DataBinder.Eval(Container.DataItem, "RECORD_ID") & _
                                                                                "&filetype=" & DataBinder.Eval(Container.DataItem, "FILE_TYPE") & "'>" & _
                                                                                "<img border='0' src='view_image_icon.png' width='20px' height='20px' title='Click to view document' /></a>")%>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>
                                        </fieldset>
                                    </div> --%>
                                    <br />
                                  <%--  <div>
                                        <fieldset>
                                            <table>
                                                <tr>
                                                    <td style="width: 150px;">
                                                        Other Year Remarks
                                                    </td>
                                                    <td>
                                                        <asp:Button runat="server" ID="btnShowOtherYearRemarksPopup" Text="Add" ClientIDMode="Static"
                                                            CausesValidation="false" Enabled="false" />
                                                        <asp:Button ID="btnAddNewOtherYearRemarks" runat="server" Text="Add Remark" ClientIDMode="Static"
                                                            CausesValidation="False" Style="display: none;" />
                                                        <br />
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <div style="margin-left: 20px">
                                                            <asp:GridView ID="gvOtherYearRemarks" runat="server" AutoGenerateColumns="false">
                                                                <Columns>
                                                                    <asp:BoundField HeaderText="Date" DataField="TASK_DATE" DataFormatString="{0:d}" />
                                                                    <asp:BoundField HeaderText="Remark" DataField="REMARKS" />
                                                                    <asp:TemplateField HeaderText="Attachment">
                                                                        <ItemTemplate>
                                                                            <%# IIf(IsDBNull(DataBinder.Eval(Container.DataItem, "IMAGE")), "&nbsp;", "<a target='_blank' href='GetBlobFromDB.ashx?tabname=genii_user.TAX_ACCOUNT_CALENDAR" & _
                                                                                "&colname=IMAGE&pknames=RECORD_ID&pkvalues=" & DataBinder.Eval(Container.DataItem, "RECORD_ID") & _
                                                                                "&filetype=" & DataBinder.Eval(Container.DataItem, "FILE_TYPE") & "'>" & _
                                                                                "<img border='0' src='view_image_icon.png' width='20px' height='20px' title='Click to view document' /></a>")%>
                                                                            <%-- <a target="_blank" href='GetBlobFromDB.ashx?tabname=genii_user.TAX_ACCOUNT_CALENDAR&colname=IMAGE&pknames=RECORD_ID&pkvalues=<%#DataBinder.Eval(Container.DataItem, "RECORD_ID") %>&filetype=<%#DataBinder.Eval(Container.DataItem, "FILE_TYPE") %>'>
                                                                                <img border="0" src="view_image_icon.png" width="20px" height="20px" title="Click to view document" />
                                                                            </a>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>
                                        </fieldset>
                                    </div>--%>
                                </ContentTemplate>
                            </ajaxToolkit:TabPanel>
                           <%-- <ajaxToolkit:TabPanel ID="tabTaxHistory" runat="server" HeaderText="Account History">
                                <ContentTemplate>
                                    <b>Account:</b>
                                    <asp:Label ID="lblTaxHistoryAccount" runat="server"></asp:Label>
                                    <b>Status:</b>
                                    <asp:Label ID="lblTaxHistoryStatus" runat="server"></asp:Label>
                                    <br />
                                    <b>Address:</b>
                                    <asp:Label ID="lblTaxHistoryAddress" runat="server"></asp:Label>
                                    <br />
                                    <b>Mailing Address:</b>
                                    <asp:Label ID="lblTaxHistoryMailingAddress" runat="server"></asp:Label>
                                    <br />
                                    <br />
                                    <asp:GridView ID="grdTaxHistory" runat="server" AutoGenerateColumns="false">
                                        <EmptyDataTemplate>
                                            No Data</EmptyDataTemplate>
                                        <Columns>
                                            <asp:BoundField HeaderText="Tax Year" DataField="Tax Year" />
                                            <asp:BoundField HeaderText="Tax Roll" DataField="Tax Roll" />
                                            <asp:BoundField HeaderText="Status" DataField="Status" />
                                            <asp:BoundField HeaderText="Total Due" DataField="Taxes" DataFormatString="{0:C}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField HeaderText="Payments" DataField="Payments" />
                                            <asp:BoundField HeaderText="Remitted" DataField="Remitted" DataFormatString="{0:C}"
                                                ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField HeaderText="Balance" DataField="Balance" DataFormatString="{0:C}"
                                                ItemStyle-HorizontalAlign="Right" />
                                        </Columns>
                                    </asp:GridView>
                                </ContentTemplate>
                            </ajaxToolkit:TabPanel>--%>

                            <ajaxToolkit:TabPanel ID="tabPaymentHistory" runat="server" HeaderText="Payments" Width ="600px">
                                <ContentTemplate>
                                    <asp:GridView ID="grdPaymentHistory" runat="server" AutoGenerateColumns="false">
                                        <EmptyDataTemplate>
                                            No Data</EmptyDataTemplate>
                                        <Columns>
                                            <asp:BoundField HeaderText="Payment Date" DataField="PaymentDate" DataFormatString="{0:d}" />
                                            <asp:BoundField HeaderText="Effective Date" DataField="PaymentEffectiveDate" DataFormatString="{0:d}" />
                                            <asp:BoundField HeaderText="Payment Party" DataField="Pertinent1" />
                                            <asp:BoundField HeaderText="Payment Note" DataField="Pertinent2" />
                                            <asp:BoundField HeaderText="Amount" DataField="PaymentAmount" DataFormatString="{0:C}"
                                                ItemStyle-HorizontalAlign="Right" />
                                        </Columns>
                                    </asp:GridView>
                                </ContentTemplate>
                            </ajaxToolkit:TabPanel>

                            <ajaxToolkit:TabPanel ID="tabTaxCalc" runat="server" HeaderText="Taxes"  Width ="600px">
                                <ContentTemplate>
                                    <asp:GridView ID="grdTaxCalc" runat="server" AutoGenerateColumns="false">
                                        <Columns>
                                            <asp:BoundField HeaderText="Auth CD" DataField="Auth CD" />
                                            <asp:BoundField HeaderText="Authority" DataField="Authority" />
                                            <asp:BoundField HeaderText="Type" DataField="Type" />
                                            <asp:BoundField HeaderText="Type Description " DataField="Type Description" />
                                            <asp:BoundField HeaderText="Amount" DataField="ChargeAmount" />

                                            <%--<asp:TemplateField HeaderText="Tax Charge Code" ItemStyle-HorizontalAlign="Left">
                                                <ItemTemplate>
                                                    <%# GetChargeCode(DataBinder.Eval(Container.DataItem, "TaxChargeCodeID"))%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Tax Type">
                                                <ItemTemplate>
                                                    <%# GetTaxType(DataBinder.Eval(Container.DataItem, "TaxTypeID"))%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField HeaderText="Tax" DataField="ChargeAmount" DataFormatString="{0:C}" ItemStyle-HorizontalAlign="Right" />--%>
                                        </Columns>
                                        <EmptyDataTemplate>
                                            No Data</EmptyDataTemplate>
                                    </asp:GridView>
                                    <b>Total Taxes: </b>
                                    <asp:Label ID="lblTaxesTotal" runat="server"></asp:Label>
                                </ContentTemplate>
                            </ajaxToolkit:TabPanel>

                            <ajaxToolkit:TabPanel ID="tabCharges" runat="server" HeaderText="Fees"  Width ="600px">
                                <ContentTemplate>
                                    <asp:GridView ID="grdCharges" runat="server" AutoGenerateColumns="false" Width="100%">
                                        <EmptyDataTemplate>
                                            No Data</EmptyDataTemplate>
                                        <Columns>
                                           <%-- <asp:BoundField HeaderText="Charge Date" DataField="CHARGE_CALC_DATE" DataFormatString="{0:d}" />--%>
                                            <asp:TemplateField HeaderText="Tax Charge Code">
                                                <ItemStyle HorizontalAlign="Left" />
                                                <ItemTemplate>
                                                    <%# GetChargeCode(DataBinder.Eval(Container.DataItem, "TaxChargeCodeID"))%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Tax Type">
                                                <ItemTemplate>
                                                    <%# GetTaxType(DataBinder.Eval(Container.DataItem, "TaxTypeID"))%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField HeaderText="Charge" DataField="ChargeAmount" DataFormatString="{0:C}"
                                                ItemStyle-HorizontalAlign="Right" />
                                        </Columns>
                                    </asp:GridView>
                                </ContentTemplate>
                            </ajaxToolkit:TabPanel>

                            <ajaxToolkit:TabPanel ID="tabPaymentDist" runat="server" HeaderText="Apportion"  Width ="600px">
                            <ContentTemplate>
                                <asp:GridView ID="grdPaymentDist" runat="server" AutoGenerateColumns="false">
                                        <Columns>                                                                                      
                                            <asp:BoundField HeaderText="Auth CD" DataField="Auth_CD" />
                                            <asp:BoundField HeaderText="Authority" DataField="Authority" />
                                            <asp:BoundField HeaderText="Type" DataField="Type" />
                                            <asp:BoundField HeaderText="Type Description " DataField="Type_Description" />
                                            <asp:BoundField HeaderText="Date" DataField="Payment_Date" />
                                            <asp:BoundField HeaderText="GL Account" DataField="GL_Account" />
                                            <asp:BoundField HeaderText="Amount" DataField="DollarAmount" />
                                        </Columns>
                                        <EmptyDataTemplate>
                                            No Data</EmptyDataTemplate>
                                    </asp:GridView>
                                   <b>Total Apportionment:</b> 
                                    <asp:Label ID="lblTotalApportion" runat="server" ></asp:Label>
                            </ContentTemplate>
                            </ajaxToolkit:TabPanel>
                            <ajaxToolkit:TabPanel ID="tabSitus" runat="server" HeaderText="Situs"  Width ="600px">
                            <ContentTemplate>
                             <div>
                                <fieldset>
                                    <table width="600px">                 
                            
                                        <tr>
                                            <td style="width: 200px;">
                                            <b>Site Address</b><br />
                                            <asp:Label ID="lblPhysicalAddress" runat="server"></asp:Label><br />
                                            <asp:Label ID="lblPhysicalCity" runat="server"></asp:Label>
                                            <asp:Label ID="lblPhysicalZip" runat="server"></asp:Label>
                                            </td>
                                            <td style="width: 200px;">
                                            <b>GIS Address</b><br />
                                            <asp:Label ID="lblGISHouseNumber" runat="server"></asp:Label>
                                            <asp:Label ID="lblGIS_Road" runat="server"></asp:Label>
                                            </td>
                                        </tr>

                                        <tr>
                                            <td style="width: 200px;">
                                            <b>Personal Property Location</b><br />
                                            Parcel:
                                            <asp:Label ID="lblPPParcel" runat="server"></asp:Label><br />
                                            Space:
                                            <asp:Label ID="lblPPSpace" runat="server"></asp:Label><br />
                                            VIN:
                                            <asp:Label ID="lblVIN" runat="server"></asp:Label>
                                            </td>
                                            <td style="width: 200px;">
                                            <b>Property Description</b><br />
                                            Class:
                                            <asp:Label ID="lblAccountClass" runat="server"></asp:Label><br />
                                            Type:
                                            <asp:Label ID="lblAccountType" runat="server"></asp:Label><br />
                                            Acreage:
                                            <asp:Label ID="lblAcreage" runat="server"></asp:Label>
                                            </td>
                                        </tr>
                                        <tr>
                                        

                                        <tr>
                                            <td colspan ="2" align ="left">
                                            <b>Legal Description</b><br />
                                                <asp:Label ID="lblLegal" runat="server"></asp:Label>
                                            </td>
                                        </tr>
                                    </table>
                                </fieldset>
                                </div>
                            </ContentTemplate>
                            </ajaxToolkit:TabPanel>

                        </ajaxToolkit:TabContainer>
                </td>

                <td valign ="top" style="border: 2px solid #000000; background-color: #E1E1E1;">
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             <table>
                    <tr valign ="top">
                        <td align="left" class="style34" >
                        <!--for amount due -->
                            <table>                            

                                <tr>                                
                                    <td colspan ="2" align="left" class="style31" style=" padding-bottom:12px">
                                            &nbsp;&nbsp;<asp:TextBox ID="txtPayerName" runat="server" CssClass="ReadOnly" 
                                            Style="text-align: right; vertical-align:bottom" TabIndex="7" 
                                            Width="300px" Height="21px"></asp:TextBox>
                                    </td>                                            
                                </tr>

                                <tr>
                                        <td >
                                            <asp:Label ID="Label15" runat="server" Text="Transaction Type" Width ="200px"></asp:Label>
                                        </td>
                                        <td class="style2" align="left">
                                            <asp:DropDownList ID="ddlPaymentType" runat="server" TabIndex="9">
                                            </asp:DropDownList>
                                        </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:Label ID="Label16" runat="server" Text="Check Number:"></asp:Label>
                                            </td>
                                            <td class="style2" align="left">
                                                <asp:TextBox ID="txtCheckNumber" runat="server" TabIndex="10" Width="150px"></asp:TextBox>
                                            </td>
                                        </tr>

                                <tr class="trBorder">
                                    <td class="style25" style ="width:200px">
                                        <asp:Label ID="Label1" runat="server" Text="Total Account Balance:" ></asp:Label>
                                    </td>                                    
                                    <td class="style16" align="left">
                                        <asp:TextBox ID="txtTotalTaxes" runat="server" CssClass="ReadOnly" 
                                            ReadOnly="True"  TabIndex="1" style="vertical-align:bottom;text-align:right;" Width="150px" BorderStyle="None" Text="0.00"></asp:TextBox>                                                    
                                    </td>                                            
                                </tr>
                                <tr>
                                    <td>&nbsp;</td>
                                    <td>&nbsp;</td>
                                </tr> 
                                <%-- <tr>
                                    <td class="style33"  align ="right">
                                        <asp:Label ID="Label5" runat="server" Text="Tax Due now"></asp:Label>
                                    </td>
                                    <td align="right">
                                        <asp:Label ID="Label11" runat="server" Text=":"></asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txtCalculatedBalance" runat="server" CssClass="ReadOnly" 
                                            ReadOnly="True" TabIndex="5" style="vertical-align:bottom;text-align:right;" Width="100px" BorderStyle="None" Text="0.00"></asp:TextBox>
                                    </td>
                                </tr>--%>
                            <%--    <tr>
                                    <td class="style33" align ="right">
                                        <asp:Label ID="Label2" runat="server" Text="Total Interest"></asp:Label>
                                    </td>
                                    <td align="right">
                                        <asp:Label ID="Label8" runat="server"></asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txtTotalInterest" runat="server" CssClass="ReadOnly" 
                                            ReadOnly="True" TabIndex="2" style="vertical-align:bottom;text-align:right;" Width="100px" BorderStyle="None" Text="0.00"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="style27" align ="right">
                                        <asp:Label ID="Label3" runat="server" Text="Total Fees"></asp:Label>
                                    </td>
                                    <td align="right" class="style21">
                                        <asp:Label ID="Label9" runat="server" Text=":"></asp:Label>
                                    </td>
                                    <td class="style21">
                                        <asp:TextBox ID="txtTotalFees" runat="server" CssClass="ReadOnly" 
                                            ReadOnly="True" TabIndex="3" style="vertical-align:bottom;text-align:right;" Width="100px" BorderStyle="None" Text="0.00"></asp:TextBox>
                                    </td>
                                </tr>--%>
                               <%-- <tr>
                                    <td colspan ="5">
                                        <asp:Label ID="Label19" runat="server" Text="Prior Years Payments:"></asp:Label>
                                    </td>
                                    <%-- <td align="right" class="style20">
                                        <asp:Label ID="Label20" runat="server" Text=":"></asp:Label>
                                    </td>
                                    <td class="style20" align="right">
                                        <asp:TextBox ID="txtPriorYears" runat="server" CssClass="ReadOnly" 
                                            ReadOnly="True" TabIndex="4" style="vertical-align:bottom;text-align:right;" Width="100px" BorderStyle="None" Text="0.00"></asp:TextBox>
                                    </td>
                                </tr>--%>
                                <tr>
                                    <td>
                                        <asp:Label ID="Label4" runat="server" Text="Prior Years Payments:"></asp:Label>
                                    </td>
                                  
                                    <td align="left">
                                        <asp:TextBox ID="txtPriorYears" runat="server" CssClass="ReadOnly" ReadOnly="True" 
                                            TabIndex="5" style="vertical-align:bottom;text-align:right;" Width="150px" BorderStyle="None" Text="0.00"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:Label ID="Label17" runat="server" Text="CP from Investor:"></asp:Label>
                                    </td>
                                    
                                    <td align="left">
                                        <asp:TextBox ID="txtAddCP" runat="server" CssClass="ReadOnly" ReadOnly="True" 
                                            TabIndex="5" style="vertical-align:bottom;text-align:right;" Width="150px" BorderStyle="None" Text="0.00"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="style33">
                                        <asp:Label ID="lblcpredeemedfromInv" runat="server" Text="CP from State:"></asp:Label>
                                    </td>
                               
                                    <td align="left">
                                        <asp:TextBox ID="txtAddCPState" runat="server" CssClass="ReadOnly" ReadOnly="True" 
                                            TabIndex="5" style="vertical-align:bottom;text-align:right;" Width="150px" BorderStyle="None" Text="0.00"></asp:TextBox>
                                    </td>
                                </tr>
                                <!--   <tr>
                                    <td class="style29">
                                        <asp:Label ID="Label6" runat="server" Text="Amount Due"></asp:Label>
                                    </td>
                                    <td align="right" class="style24">
                                        &nbsp;
                                    </td>
                                    <td class="style24">
                                        <asp:TextBox ID="txtAmountDueNow" runat="server" TabIndex="6" Width="100px"></asp:TextBox>
                                    </td>
                                </tr>-->
                                <%-- <tr>
                                    <td class="style33" align ="right">
                                        <asp:Label ID="Label4" runat="server" Text="Payments To Date"></asp:Label>
                                    </td>
                                    <td align="right">
                                        <asp:Label ID="Label10" runat="server" Text=":"></asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txtTotalPayments" runat="server" CssClass="ReadOnly" 
                                            ReadOnly="True" TabIndex="4" style="vertical-align:bottom;text-align:right;" Width="100px" BorderStyle="None" Text="0.00"></asp:TextBox>
                                    </td>                                            
                                </tr>--%>
                                <tr>
                                    <td>&nbsp;</td>
                                    <td>&nbsp;</td>
                                </tr> 
                                

                                <tr>
                                    <td class="style33">
                                        <asp:Label ID="Label3" runat="server" Text="Total Required:"></asp:Label>
                                    </td>
                                    
                                    <td class="style2" align="left">
                                        <asp:TextBox ID="hdnTxtRequiredAmount" runat="server" Enabled ="false" TabIndex="9" Width="150px" BorderStyle ="None" style="vertical-align:bottom;text-align:right;"></asp:TextBox>
                                    </td>  

                                </tr>

                                <tr>
                                    <td class="style33">
                                        <asp:Label ID="Label21" runat="server" Text="Total Remitted:"></asp:Label>
                                    </td>
                                   
                                    <td class="style2" align="left">
                                        <asp:TextBox ID="txtAmountPaid" runat="server" Enabled ="false" TabIndex="8" Width="150px" BorderStyle ="None" style="vertical-align:bottom;text-align:right;"></asp:TextBox>
                                      
                                    </td>  

                                </tr>
                                <%--  
                                <tr>
                                    <td class="style21" colspan ="3">
                                        <asp:Label ID="Label14" runat="server" Text="Difference"></asp:Label>
                                    </td>
                                    <td class="style22">
                                        <asp:TextBox ID="txtDifference" runat="server" CssClass="ReadOnly" 
                                            TabIndex="11" Width="100px"  BorderStyle ="None" ></asp:TextBox>
                                    </td>
                                </tr>--%>

                                <tr>
                                    <td>&nbsp;</td>
                                    <td>&nbsp;</td>
                                </tr> 

                                <tr>
                                    <td align="left">
                                        <asp:Button ID="btnSavePayment" runat="server" Text="Save" Enabled="False" ClientIDMode="Static" />
                                    </td>
                                    <td align="left">
                                        <asp:Button ID="btnRejectPayment" runat="server" Text="Reject Payment" Enabled="False" ClientIDMode="Static" />
                                    </td>
                                   <%-- <td>
                                        <asp:Button ID="btnPrintReceipt" runat="server" Text="Print Receipt" Visible="False" OnClick="btnPrintReceipt_Click"/>
                                    </td>          --%>                                                                      
                                </tr>
                                <tr align="center">                                
                                    <td align="center" colspan="1">
                                    
                                        <asp:RadioButtonList ID="rdoAmountUnder" runat="server">
                                            <asp:ListItem Value="partial" Text="Accept Partial Payment" Selected="True"></asp:ListItem>
                                            <asp:ListItem Value="writeoff" Text="Accept & Write-off"></asp:ListItem>
                                        </asp:RadioButtonList>
                                        <asp:RadioButtonList ID="rdoAmountOver" runat="server" Style="display: none;">
                                            <asp:ListItem Value="refund" Text="Refund" Selected="True"></asp:ListItem>
                                            <asp:ListItem Value="kitty" Text="Accept & Kitty"></asp:ListItem>
                                        </asp:RadioButtonList>
                                        <%--<asp:Button ID="btnSavePayment" runat="server" Text="Save" Enabled="False" ClientIDMode="Static" />
                                        <asp:Button ID="btnRejectPayment" runat="server" Text="Reject Payment" Enabled="False" ClientIDMode="Static" />--%>
                                        <%-- <asp:Button ID="btnCreateReceipt" runat="server" OnClientClick="openPrintReceipt(); return false;" Text="Print Receipt" Visible="False" Enabled ="false"/>--%>
                                        <%--<asp:Button ID="btnPrintReceipt" runat="server" Text="Print Receipt" Visible="False" OnClick="btnPrintReceipt_Click"/>--%>
                                        <%-- <asp:Panel ID="pnlLetterQueuer" runat="server" Visible="False">
                                            <h4 style="margin-bottom: 6px">
                                                Queue letters for printing:</h4>
                                            <asp:CheckBox ID="chkQueueLetter1" runat="server" Text="Payment Accepted - Outstanding CP <br />" />
                                            <asp:CheckBox ID="chkQueueLetter2" runat="server" Text="Payment Early - Outstanding Balance <br />" />
                                            <asp:CheckBox ID="chkQueueLetter3" runat="server" Text="Payment Late - Outstanding Balance <br />" />
                                            <asp:CheckBox ID="chkQueueLetter4" runat="server" Text="CP Redeemed - Letter to Investor<br />" />
                                            <asp:Button ID="btnQueueLetters" runat="server" Text="Queue Letter(s)" />
                                        </asp:Panel>--%>
                                    </td>
                                </tr>

                            </table>
                                    
                        </td>
                                         
                    </tr>
                </table>
                </td>
                <td align="center" style="border: 2px solid #000000; background-color: #E1E1E1; width:300px">
                        <table>
                                    <tr valign ="middle" align="justify">
                                    <td align="center" valign ="top"
                                    style="border: medium groove #99CCFF; width:200px; border-collapse: collapse;" 
                                    bgcolor="Silver">
                                                <asp:Button ID="btnAccountStatusLight" runat="server" OnClientClick ="return false"
                                                Text="Account Status" Width="200px" />
                                                                                                                                                   
                                                <asp:Button ID="btnParentBal" runat="server" OnClientClick ="return false"
                                                    Text="Parent Parcel Balance" Width="200px" />
                                                
                                                <asp:Button ID="btnSuspendLight" runat="server" Text="Suspend"  OnClientClick ="return false"
                                                    Width="200px" />
                                                                                                                                               
                                                <asp:Button ID="btnBankruptcyLight" runat="server" OnClientClick ="return false"
                                                    Text="Bankruptcy" Width="200px" />
                                                
                                                <asp:Button ID="btnAlertLight" runat="server" Text="Alert"  OnClientClick ="return false"
                                                    Width="200px" />
                                                
                                                <asp:Button ID="btnCPLight" runat="server"  Text="CP"  OnClientClick ="return false"
                                                    Width="200px" />                                                                                                
                                        </td>
                                        
                                      
                                    </tr>
                                    <tr>
                                        <td>
                                        &nbsp;&nbsp;
                                        </td>
                                    </tr>
                                    <tr valign ="middle" align="center">                                    
                                          <td align="center" valign ="top"
                                        style="border: medium groove #99CCFF; width:150px; border-collapse: collapse;" 
                                        bgcolor="Silver">
                                             <asp:Button ID="btnRollStatusLight" runat="server" OnClientClick ="return false"
                                                        Text="Roll Status" Width="200px" />
                                             <asp:Button ID="btnBoardOrderLight" runat="server"  OnClientClick ="return false"
                                                    Text="Board Order" Width="200px" />

                                             <asp:Button ID="btnConfLight" runat="server" Text="Confidential"  OnClientClick ="return false"
                                                    Width="200px" />
                                                
                                            <asp:Button ID="btnRetMailLight" runat="server" OnClientClick ="return false"
                                                    Text="Returned Mail" Width="200px" />

                                        </td>
                                    </tr>
                                        
                                    </table>
                </td>
            </tr>

            <tr> 
                <td colspan ="3" style="border: 2px solid #000000; background-color: #E1E1E1;">
                <table>
                <tr>
                       <td valign ="top" align="left" style="width:1500px;">

                        <div style=" padding-top: 20px">
                        <asp:Label id ="lblPriorYearsHeader" Text ="Current Tax Roll Balances" runat="server" 
                                Visible ="False" Font-Bold="True" Font-Size="Medium" ></asp:Label>
                                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                               
                            <asp:GridView ID="grdPriorYears" runat="server" AutoGenerateColumns="False"
                                Width="100%">     
                            <EmptyDataTemplate><b>Current Tax Roll Balances: None</b></EmptyDataTemplate>
                            <Columns>
                                <asp:TemplateField>
                                    <HeaderTemplate> 
                                        <asp:CheckBox ID="chkPriorYearsSelectAll" runat="server" OnCheckedChanged ="checkPriorYearsAll" AutoPostBack="true"/><%----%>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkPriorYears" runat="server" OnCheckedChanged ="chkPriorYears_CheckedChanged" AutoPostBack="true"/><%--checked='<%# (Not IsDbNull(Eval("Enabled"))) AndAlso Eval("Enabled")=1%>' Enabled='<%# (Not IsDbNull(Eval("Enabled"))) AndAlso Eval("Enabled")=0%>'--%>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField HeaderText="Tax Year" DataField="TaxYear" />
                                <asp:BoundField HeaderText="Tax Roll" DataField="TaxRollNumber" />
                                <asp:BoundField HeaderText="Taxes" DataField="Taxes" 
                                    DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                 <asp:TemplateField>
                                    <HeaderTemplate>
                                        Interest
                                    </HeaderTemplate>
                                    <ItemTemplate >
                                        <asp:DropDownList ID="ddlInterest" runat="server" ClientIDMode ="Static" Enabled ="false"></asp:DropDownList>
                                    </ItemTemplate>
                                 </asp:TemplateField>

                         <%--        <asp:TemplateField>
                                    <HeaderTemplate> 
                                        <asp:Label Text="FGI" runat="server" ID="lblFGI"></asp:Label>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkFGI" runat="server" AutoPostBack="true" OnCheckedChanged ="chkFGI_CheckChanged" Enabled='<%#Eval("numPayments") = 0 %>'/>--%><%-- onclick="java:checkFGI(this);"  OnCheckedChanged ="chkFGI_CheckChanged" checked='<%# (Not IsDbNull(Eval("Enabled"))) AndAlso Eval("Enabled")=1%>' Enabled='<%# (Not IsDbNull(Eval("Enabled"))) AndAlso Eval("Enabled")=0%>'--%>                                        
                                 <%--   </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField>
                                    <HeaderTemplate> 
                                        <asp:Label Text="PM" runat="server" ID="lblPM"></asp:Label>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkPM" runat="server" AutoPostBack="true" OnCheckedChanged ="chkPM_CheckChanged" Enabled='<%#Eval("numPayments") = 0 %>'/>--%><%-- onclick="java:checkFGI(this);"  OnCheckedChanged ="chkFGI_CheckChanged" checked='<%# (Not IsDbNull(Eval("Enabled"))) AndAlso Eval("Enabled")=1%>' Enabled='<%# (Not IsDbNull(Eval("Enabled"))) AndAlso Eval("Enabled")=0%>'--%>                                        
                                <%--    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField HeaderText="Prior (I)" DataField="PRIOT_INTEREST" 
                                    DataFormatString="{0:C}" >                                    
                                <ItemStyle HorizontalAlign="Right"/>
                                </asp:BoundField>
                                <asp:BoundField HeaderText="Aged (I)" DataField="Interest" 
                                    DataFormatString="{0:C}" >                                    
                                <ItemStyle HorizontalAlign="Right"/>
                                </asp:BoundField>
                                <asp:BoundField HeaderText="Mod (I)" DataField="Interest" 
                                    DataFormatString="{0:C}" >                                    
                                <ItemStyle HorizontalAlign="Right"/>
                                </asp:BoundField>--%>
                               <%-- <asp:TemplateField >
                                    <ItemTemplate >
                                        <asp:HiddenField ID="hdnInterest" runat="server" Value='<%# Eval("Interest")%>' />
                                    </ItemTemplate>
                                </asp:TemplateField>--%>
                                <asp:BoundField HeaderText="Fees" DataField="Fees" 
                                    DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField HeaderText="Payments" DataField="Payments" 
                                    DataFormatString="{0:C}" NullDisplayText ="0.00">
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>

                                <asp:BoundField HeaderText="Balance" DataField="CurrentBalance" 
                                    DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:TemplateField>
                                    <HeaderTemplate>
                                        Amount  <asp:Button ID="btnComputePriorYears" Text="Compute" runat="server" OnClick ="btnCompute_click"/>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtPriorYearAmount" Text="0.00"  runat="server" Width="100px" CssClass="rightAlign" Enabled ="false" ClientIDMode ="Static"></asp:TextBox> <%--AutoPostBack ="true" OnTextChanged ="btnCompute_click"--%>
                                    </ItemTemplate>
                                    <ItemStyle HorizontalAlign="Right" Width="150px" />
                                </asp:TemplateField>
                            </Columns>                          
                        </asp:GridView>
                        </div>              
                                     
                        <div style=" padding-top: 20px">
                        <asp:Label id ="lblActiveCPHeader" Text ="CP Redemption from Investor " runat="server" 
                                Visible ="False" Font-Bold="True" Font-Size="Medium"></asp:Label>
                            <asp:GridView ID="grdCPsInvestor" runat="server" AutoGenerateColumns="False" 
                                Width="100%">      
                                         
                            <EmptyDataTemplate><b>CP Redemption from Investor: None</b></EmptyDataTemplate>
                            <Columns>
                                <asp:TemplateField>
                                    <HeaderTemplate> 
                                        <asp:CheckBox ID="chkCPSelectAll" runat="server" OnCheckedChanged ="checkCPAll" AutoPostBack="true"/>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkCP" runat="server"  OnCheckedChanged ="chkCP_CheckedChanged" AutoPostBack="true"/><%--Enabled='<%#CanRedeemCP(DataBinder.Eval(Container.DataItem, "CP_STATUS")) %>'--%>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField HeaderText="Tax Year" DataField="taxyear" />
                                <asp:BoundField HeaderText="Roll Number" DataField="Roll Number" />
                                <asp:BoundField HeaderText="Certificate" DataField="Certificate" />
                                <asp:BoundField HeaderText="Investor" DataField="Investor" />
                                <asp:BoundField HeaderText="Date of Purchase" DataField="Date of Purchase" />
                                <asp:BoundField HeaderText="Months @ Rate" DataField="Months @ Rate" />
                                <asp:BoundField HeaderText="Purchase Value" DataField="Value" DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>                                
                                <asp:BoundField HeaderText="Interest" DataField="Interest" 
                                    DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField HeaderText="Redeem Fee" DataField="RedeemFee" DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField HeaderText="Total" DataField="Total" DataFormatString="{0:C}" NullDisplayText ="0.00" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                            </Columns>
                        </asp:GridView>
                        </div>

                        <div style=" padding-top: 20px">
                        <asp:Label id ="lblActiveCPHeaderState" Text ="CP Redemption from State" runat="server" 
                                Visible ="False" Font-Bold="True" Font-Size="Medium"></asp:Label>
                            <asp:GridView ID="grdCPsState" runat="server" AutoGenerateColumns="False" 
                                Width="100%">      
                                         
                            <EmptyDataTemplate><b>CP Redemption from State: None</b></EmptyDataTemplate>
                            <Columns>
                                <asp:TemplateField>
                                    <HeaderTemplate> 
                                        <asp:CheckBox ID="chkCPStateSelectAll" runat="server" OnCheckedChanged ="chkCPStateAll" AutoPostBack="true"/>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkCPState" runat="server"  OnCheckedChanged ="chkCPState_CheckedChanged" AutoPostBack="true"/><%--Enabled='<%#CanRedeemCP(DataBinder.Eval(Container.DataItem, "CP_STATUS")) %>'--%>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField HeaderText="Tax Year" DataField="TaxYear" />
                                <asp:BoundField HeaderText="Roll Number" DataField="TaxRollNumber" />
                                <asp:BoundField HeaderText="Certificate" DataField="Certificate" />
                                <asp:BoundField HeaderText="Taxes" DataField="Taxes" />
                                <asp:BoundField HeaderText="Interest" DataField="Interest" 
                                    DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField HeaderText="Fees" DataField="Fees" 
                                    DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField HeaderText="Payments" DataField="Payments" 
                                    DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField HeaderText="RedeemFees" DataField="RedeemFees" DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField HeaderText="Total" DataField="Total" DataFormatString="{0:C}" >
                                <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                            </Columns>
                        </asp:GridView>
                        </div>
                        
                       
                       </td>
                </tr>
            </table>   
                </td>
            </tr>
        </table>
            
            <!-- Payments Tabs -->
            <br />         
            
        </div>
        <!-- Cashier Daily Activity (Pending Payments) tab -->
        <div id="tabCashierActivity">
            <ajaxToolkit:TabContainer ID="tabsPendingPayments" runat="server">
                <ajaxToolkit:TabPanel ID="tabPendingSummary" runat="server" HeaderText="Summary">
                    <ContentTemplate>
                        <p>
                            <b>Session:</b>
                            <br />
                            Cashier:
                            <asp:Label ID="lblPendCashier" runat="server"></asp:Label>
                            <br />
                            Open Time:
                            <asp:Label ID="lblPendLogin" runat="server"></asp:Label>
                            <br />
                            Transactions:
                            <asp:Label ID="lblPendTransNum" runat="server"></asp:Label>
                            <br />
                            Rejected Payments:
                            <asp:Label ID="lblPendDeclined" runat="server"></asp:Label>
                            <br />
                            <b>Cash Box Balance:</b>
                            <asp:Label ID="lblPendCashBoxBalance" runat="server"></asp:Label>
                        </p>
                        <p>
                            <b>Collections:</b>
                            <br />
                            Cash:
                            <asp:Label ID="lblPendCash" runat="server"></asp:Label>
                            <br />
                            Checks:
                            <asp:Label ID="lblPendChecks" runat="server"></asp:Label>
                            <br />
                            Money Order:
                            <asp:Label ID="lblPendMoneyOrder" runat="server"></asp:Label>
                            <br />
                            Credit Card:
                            <asp:Label ID="lblPendCreditCard" runat="server"></asp:Label>
                            <br />
                            Creditron:
                            <asp:Label ID="lblPendCreditron" runat="server"></asp:Label>
                            <br />
                            Other:
                            <asp:Label ID="lblPendOtherPaid" runat="server"></asp:Label>
                            <br />
                          <%--  CP from State:
                            <asp:Label ID="lblCPCollections" runat="server"></asp:Label>
                            <br />   
                            CP from Investor:
                            <asp:Label ID="lblCPCollectionsInvestor" runat="server"></asp:Label>
                            <br />                           --%>
                            <b>Total:</b>
                            <asp:Label ID="lblPendPayments" runat="server"></asp:Label>
                        </p>
                        <p>
                            <b>As Allocated:</b>
                            <br />
                            Tax:
                            <asp:Label ID="lblPendTax" runat="server"></asp:Label>
                            <br />                                                         
                            Refunds:
                            <asp:Label ID="lblPendRefunds" runat="server"></asp:Label>
                            <br />
                            Kitty:
                            <asp:Label ID="lblPendKittyFund" runat="server"></asp:Label>
                            <br />
                            <b>Total:</b>
                            <asp:Label ID="lblPendAllocatedTotal" runat="server"></asp:Label>
                            <br />
                            <br />
                            <b>Required Cash:</b>
                            <asp:Label ID="lblRequiredCash" runat="server"></asp:Label>
                            <br />
                            <br />
                            <b>As Apportioned:</b>                            
                            <asp:Label ID="lblAsApportioned" runat="server" Text="0.00"></asp:Label>
                            <br />
                        </p>
                        <b>Difference: </b><asp:Label ID="lblPendDifference" runat="server"></asp:Label>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabPendingPayments" runat="server" HeaderText="Transactions">
                    <ContentTemplate>
                        <asp:GridView ID="grdPendingPayments" runat="server" AutoGenerateColumns="false"
                            Width="100%">
                            <EmptyDataTemplate>
                                This table is empty</EmptyDataTemplate>
                            <Columns>
                                <asp:BoundField HeaderText="Transaction #" DataField="RECORD_ID" />
                                <asp:BoundField HeaderText="Group Key" DataField="GROUP_KEY" />
                                <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                <asp:BoundField HeaderText="Tax Roll" DataField="TAX_ROLL_NUMBER" />
                                <asp:BoundField HeaderText="Payment Date" DataField="PAYMENT_DATE" DataFormatString="{0:d}" />
                                <asp:TemplateField HeaderText="Payment Type">
                                    <ItemTemplate>
                                        <%#GetPaymentType(DataBinder.Eval(Container.DataItem, "PAYMENT_TYPE"))%>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField HeaderText="Amount" DataField="PAYMENT_AMT" DataFormatString="{0:c}"
                                    ItemStyle-HorizontalAlign="Right" />
                                <asp:BoundField HeaderText="Payor" DataField="PAYOR_NAME" />
                                <asp:BoundField HeaderText="Check Number" DataField="CHECK_NUMBER" />
                                <asp:BoundField HeaderText="Tax Amount" DataField="TAX_AMT" DataFormatString="{0:$#,#.00;($#,#.00);''}" />
                                <asp:BoundField HeaderText="Refund Amount" DataField="REFUND_AMT" DataFormatString="{0:$#,#.00;($#,#.00);''}" />
                                <asp:BoundField HeaderText="Over/(Under)" DataField="KITTY_AMT" DataFormatString="{0:$#,#.00;($#,#.00);''}" />
                            </Columns>
                        </asp:GridView>
                        Transactions Total:
                        <asp:Label ID="lblTotalPendingPayments" runat="server" Font-Bold="true"></asp:Label>
                        <br />
                        <asp:Button ID="btnClearPendingPayments" runat="server" Text="Clear Pending Payments" />
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabTax" runat="server" HeaderText="Tax Amounts">
                    <ContentTemplate>
                        <asp:GridView ID="grdPendingTax" runat="server" AutoGenerateColumns="false">
                            <EmptyDataTemplate>
                                This table is empty</EmptyDataTemplate>
                            <Columns>
                                <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                <asp:BoundField HeaderText="Tax Roll" DataField="TAX_ROLL_NUMBER" />
                                <asp:BoundField HeaderText="Amount" DataField="TAX_AMT" DataFormatString="{0:c}"
                                    ItemStyle-HorizontalAlign="Right" />
                                <asp:TemplateField HeaderText="Apportioned">
                                    <ItemTemplate>
                                        <%# Utilities.GetYesNo(DataBinder.Eval(Container.DataItem, "TRANSACTION_STATUS"))%>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="TabPanel1" runat="server" HeaderText="Apportionment">
                    <ContentTemplate>
                        <asp:GridView ID="grdApportionPayments" runat="server" AutoGenerateColumns="false">
                        <EmptyDataTemplate>This table is empty</EmptyDataTemplate>
                            <Columns>
                                <asp:BoundField HeaderText="Tax Year" DataField="TaxYear" />
                                <asp:BoundField HeaderText="Tax Roll" DataField="TaxRollNumber" />
                                <asp:BoundField HeaderText="Levy Authority" DataField="Levy Authority" />
                                <asp:BoundField HeaderText="Tax Type" DataField="TaxType" />
                                <asp:BoundField HeaderText="Payment Date" DataField="PaymentDate" DataFormatString="{0:d}" />
                                <asp:BoundField HeaderText="GL Account" DataField="GLAccount" />
                                <asp:BoundField HeaderText="Date Apportioned" DataField="DateApportioned" DataFormatString="{0:d}" />
                                <asp:BoundField HeaderText="Apportioned Amount" DataField="Amount" DataFormatString="{0:C}"
                                    ItemStyle-HorizontalAlign="Right" />
                            </Columns>
                        </asp:GridView>
                         Total Apportionment Dollar Amount:
                        <asp:Label ID="lblTotalApportionmentPayment" runat="server" Font-Bold="true"></asp:Label>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabRefunds" runat="server" HeaderText="Refunds">
                    <ContentTemplate>
                        <asp:GridView ID="grdRefunds" runat="server" AutoGenerateColumns="false" Width="100%">
                            <EmptyDataTemplate>
                                This table is empty</EmptyDataTemplate>
                            <Columns>
                                <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                <asp:BoundField HeaderText="Tax Roll" DataField="TAX_ROLL_NUMBER" />
                                <asp:BoundField HeaderText="Amount" DataField="REFUND_AMT" DataFormatString="{0:c}"
                                    ItemStyle-HorizontalAlign="Right" />
                            </Columns>
                        </asp:GridView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabKittyFunds" runat="server" HeaderText="Kitty Funds">
                    <ContentTemplate>
                        <asp:GridView ID="grdKittyFunds" runat="server" AutoGenerateColumns="false" Width="100%">
                            <EmptyDataTemplate>
                                This table is empty</EmptyDataTemplate>
                            <Columns>
                                <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                <asp:BoundField HeaderText="Tax Roll" DataField="TAX_ROLL_NUMBER" />
                                <asp:BoundField HeaderText="Amount" DataField="KITTY_AMT" DataFormatString="{0:c}"
                                    ItemStyle-HorizontalAlign="Right" />
                            </Columns>
                        </asp:GridView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabDeclinedPayments" runat="server" HeaderText="Rejected">
                    <ContentTemplate>
                        <asp:GridView ID="grdDeclinedPayments" runat="server" AutoGenerateColumns="false"
                            Width="100%">
                            <EmptyDataTemplate>
                                This table is empty</EmptyDataTemplate>
                            <Columns>
                                <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                <asp:BoundField HeaderText="Tax Roll" DataField="TAX_ROLL_NUMBER" />
                                <asp:BoundField HeaderText="Payment Date" DataField="PAYMENT_DATE" DataFormatString="{0:d}" />
                                <asp:TemplateField HeaderText="Payment Type">
                                    <ItemTemplate>
                                        <%#GetPaymentType(DataBinder.Eval(Container.DataItem, "PAYMENT_TYPE"))%>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField HeaderText="Amount" DataField="DECLINED_AMT" DataFormatString="{0:c}"
                                    ItemStyle-HorizontalAlign="Right" />
                                <asp:BoundField HeaderText="Payor" DataField="PAYOR_NAME" />
                                <asp:BoundField HeaderText="Check Number" DataField="CHECK_NUMBER" />
                            </Columns>
                        </asp:GridView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
            </ajaxToolkit:TabContainer>
        </div>
        <!-- Apportionments tab 
        <div id="tabApportion">
            <asp:Button ID="btnCreateApportionment" runat="server" Text="Calculate Apportionments" />
            <asp:GridView ID="grdApportionments" runat="server" AutoGenerateColumns="false">
                <Columns>
                    <asp:BoundField HeaderText="Tax Year" DataField="TaxYear" />
                    <asp:BoundField HeaderText="Tax Roll" DataField="TaxRollNumber" />
                    <%--<asp:TemplateField HeaderText="Tax Area">
                        <ItemTemplate>
                            <%#GetTaxArea(DataBinder.Eval(Container.DataItem, "AreaCode"))%>
                        </ItemTemplate>
                    </asp:TemplateField>--%>
                    <asp:TemplateField HeaderText="Levy Authority">
                        <ItemTemplate>
                            <%--#GetChargeCode(DataBinder.Eval(Container.DataItem, "TaxChargeCodeID"))--%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Tax Type">
                        <ItemTemplate>
                            <%--#GetTaxType(DataBinder.Eval(Container.DataItem, "TaxTypeID"))--%>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField HeaderText="Payment Date" DataField="PaymentDate" DataFormatString="{0:d}" />
                    <asp:BoundField HeaderText="GL Account" DataField="GLAccount" />
                    <asp:BoundField HeaderText="Date Apportioned" DataField="DateApportioned" DataFormatString="{0:d}" />
                    <asp:BoundField HeaderText="Apportioned Amount" DataField="DollarAmount" DataFormatString="{0:C}"
                        ItemStyle-HorizontalAlign="Right" />
                </Columns>
            </asp:GridView>
            Total Apportionment Dollar Amount:
            <asp:Label ID="lblTotalApportionment" runat="server" Font-Bold="true"></asp:Label>
            <br />
            <br />
            <asp:Button ID="btnSaveAll" runat="server" Font-Bold="true" Text="Save Payments and Apportionments"
                Visible="false" />
        </div>-->

       <%-- <!-- Apportion Payment tab -->
        <div id="tabApportionPayments">
        
            
        </div>
--%>
        <!-- Investors & CP tab -->
        <div id="tabInvestors">
        </div>
        <!-- Letters tab -->
        <%--  <div id="tabLetters">
            <asp:GridView ID="grdLetters" runat="server" AutoGenerateColumns="false">
                <Columns>
                    <asp:TemplateField>
                        <ItemTemplate>
                            <asp:CheckBox ID="chkLettersSelect" runat="server" Enabled='<%#Eval("LETTERS_COUNT") > 0 %>' />
                            <asp:HiddenField ID="hdnLetterType" runat="server" Value='<%#Eval("RECORD_ID") %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField HeaderText="Letter" DataField="DESCRIPTION" ItemStyle-HorizontalAlign="Left"
                        HeaderStyle-HorizontalAlign="Left" />
                    <asp:TemplateField HeaderText="Letters to Print">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkLetterCount" runat="server" CommandName="LetterDetail" CommandArgument='<%#Eval("RECORD_ID") %>'
                                Text='<%#Eval("LETTERS_COUNT") %>' Enabled='<%#Eval("LETTERS_COUNT") > 0 %>'></asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            <br />
            <asp:Button ID="btnLettersPrint" runat="server" Text="Download" />
            
            <!-- Letters Detail Grid -->
            <div id="divLetterDetail">
                <asp:GridView ID="grdLettersDetail" runat="server" AutoGenerateColumns="False" EnableModelValidation="True">
                    <Columns>
                        <asp:BoundField HeaderText="Cashier" DataField="CASHIER" />
                        <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                        <asp:BoundField HeaderText="Roll" DataField="TAX_ROLL_NUMBER" />
                        <asp:BoundField HeaderText="Tax Payor" DataField="OWNER_NAME" />
                        <asp:TemplateField HeaderText="Approved">
                            <ItemTemplate>
                                <asp:CheckBox ID="CheckBox1" runat="server" Checked='<%# (Not IsDbNull(Eval("APPROVED"))) AndAlso Eval("APPROVED")=1 %>'
                                    Enabled="false" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Print Label">
                            <ItemTemplate>
                                <asp:CheckBox ID="chkPrintLabel" runat="server" Enabled="false" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Print">
                            <ItemTemplate>
                                <asp:CheckBox ID="chkPrint" runat="server" Checked="true" Enabled="false" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Print Check">
                            <ItemTemplate>
                                <asp:Button ID="btnPrintCheck" runat="server" Text="Print" Enabled='<%# (Not IsDbNull(Eval("APPROVED"))) AndAlso Eval("APPROVED")=1 %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>--%>
 
    </div>
    
    </form>
</body>
</html>
