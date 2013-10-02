<%@ Page Language="VB" AutoEventWireup="false" CodeFile="MaintenanceTasks.aspx.vb" Inherits="MaintenanceTasks"
    StylesheetTheme="Blue" %>

<%@ Register TagPrefix="ajaxToolkit" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Cashier Supervisor</title>
    <link href="Css/redmond/jquery-ui-1.8.23.custom.css" rel="stylesheet" type="text/css" />
    <link href="Css/Tax.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="JavaScript/jquery-1.5.1.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery-ui-1.8.23.custom.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.maskedinput-1.3.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.validate.js"></script>
    <script type="text/javascript">
        $(document).ready(function () {
            // var clickNum = 0;
            // Setup Tabs
            //            $("#mainTabs").tabs({
            //                selected: window.location.hash,
            //                select: function (event, ui) {
            //                    window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
            //                }
            //            }).tabs("select", (window.location.hash ? window.location.hash : 0));


            $("#mainTabs").tabs({
                select: function (event, ui) {
                    switch (ui.index) {
                        case 0:
                            window.location.href = "TaxSupervisor.aspx#tabPosting";
                            break;
                        case 1:
                            window.location.href = "TaxSupervisor.aspx#tabFunctions";
                            break;
                        case 2:
                            window.location.href = "TaxSupervisor.aspx#tabBoardOrders";
                            break;
                        case 3:
                            window.location.href = "TaxSupervisor.aspx#tabSalePrep";
                            break;
                        case 4:
                            window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
                            break;
                    }
                }
            }).tabs("select", (window.location.hash ? window.location.hash : 4));

            // Form submit
            $("form").submit(function () {
                var action = document.getElementById("form1").action;
                if (action.lastIndexOf("#") >= 0) {
                    action = action.substr(0, action.lastIndexOf("#"));
                }
                document.getElementById("form1").action = action + window.location.hash;
            });


            // Apply style to all buttons
            //            $("#btnPostLoadSession").button();
            //            $("#btnLoadRefund").button();
            //            $("#btnSaveRefund").button();
            //            $("#btnLPSLoad").button();
            //            $("[id*=btnPrintCheck]").button();
            //            $("[id*=btnPost]").button();
            //            $("#btnLettersSave").button();
            //            $("[id*=btnViewCP]").button();
            //            $("[id*=btnPrintDeed]").button();
            //            $("#btnNightlyFunc").button();
            //            $("#btnCaptureLevy").button();
            //            $("#btnAgeInterest").button();
            //            $("#btnUpdateWebVals").button();
            //            $("#btnLoadForeclosures").button();
            //            $("#btnSendUnsecured").button();
            $("#btnDailyLetters").button();
            $("#btnCommitCAD").button();
            $("#btnSearchAll").button();
            $("#btnSearchMatch").button();
            $("#btnSearchPartial").button();
            $("#btnSearchRefund").button();
            $("#btnPrintDailyLetters").button();
            $("#btnPrintDailyLabels").button();
            $("#btnClearDailyLetters").button();
            $("#btnSearchReturnedChecks").button();
            $("#btnReturnChecksCommit").button();
            $("#btnPrintRefundLetters").button();
            $("#txtQuickPayment").focus();


            // btnViewCP Click Event
            $("[id$=btnViewCP]").click(function (event, ui) {
                // $("#divViewCP").dialog("open");
                showViewCPPopup("View CP");

                event.preventDefault();
            });

            $("#btnCloseCashierSession").click(function () {
                showLoadingBox();
            });





            //            $("#txtQuickPayment").change(function () {
            //                var quickPayment = $("#txtQuickPayment").val();
            //                $("#lblQuickPaymentRemainder").val(quickPayment);
            //                //gv.rows[rwIndex].cells[2].childNodes[0].value
            //                //gv.rows[rwIndex].cells[0].innerText
            //            });





        });          // End of document.ready function


        // showViewCPPopup - Show the View CP Popup Window
        function showViewCPPopup(title) {
            $("#divViewCP").dialog({
                modal: true,
                title: title,
                width: 800,
                close: function (e, ui) {
                    $(this).dialog("destroy");
                }
            });
        }

        

        function ShowPopUp_NoBG() {
        $("#PopUp").show('slow');
        $("#PopUpBody")[0].innerHTML = "This popup will not block access to any visible background controls.";
        };

        function ClosePopUp() {
            $("#PopUp").hide();
        };

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


        function openNewWin(url) {

            var x = window.open(url, 'mynewwin', 'width=600,height=600,toolbar=1');

           // x.focus();

        }

    </script>
</head>
<body>
<script type="text/javascript">

    $(document).ready(function () {

        $("[id$=btnReverseTrans2]").click(function (event, ui) {
            //   $("[id$=btnReverseTrans2]").attr("disabled", true);

            $("#<%=grdReturnedChecks2.ClientID %> tr").click(function (event) {
                //Skip first(header) row
                // if (!this.rowIndex) return;
                var idx = this.rowIndex;

                var transID = document.getElementById("<%=grdReturnedChecks2.ClientID %>").rows[idx].cells[0].innerHTML;
                var grpKey = document.getElementById("<%=grdReturnedChecks2.ClientID %>").rows[idx].cells[1].innerHTML;
                var payorName = document.getElementById("<%=grdReturnedChecks2.ClientID %>").rows[idx].cells[6].innerHTML;
                var amount = document.getElementById("<%=grdReturnedChecks2.ClientID %>").rows[idx].cells[8].innerHTML;
                //lblGroupKey
                //                   alert(transID);
                //                   alert(payorName);
                //                    alert(amount);

                $("#lblDeleteDivRecordID2").html(transID);
                $("#lblDeleteDivPayorName2").html(payorName);
                $("#lblDeleteDivAmount2").html(amount);
                $("#lblGroupKey2").html(grpKey);

                //  document.getElementById("inputHidden2").value = transID

            });

            showReverseTrans("Reverse Transaction");

            event.preventDefault();
        });

        //        $("#txtPosDate").datepicker().focus(function (event) {
        //            //$("#rdoPosDate").attr("checked", "checked");
        //        });
        //        $("#txtPosDate").datepicker();

        $("#txtBarcode").focus();
        $("#txtBarcode").keydown(txtBarcode_Keydown);

        $("[id$=txtQuickPayment]").change(function () {
            //  alert(this.value);
            var idx = this.rowIndex;
            //  alert(idx);
            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");

            //    var balance = grid.rows[idx].cells[2].innerHTML;

            var runningTotals = 0;
            for (i = 0; i < grid.rows.length; i++) {
                var col1 = grid.rows[i].cells[9];
                var col2 = grid.rows[i].cells[10];
                //  alert(col2);
                var col3 = grid.rows[i].cells[3];
                //   alert(col3);

                // var x=col1.childNodes.length;
                for (j = 0; j < col1.childNodes.length; j++) {
                    // if (col1.childNodes[j].type == "text") {
                    if (!isNaN(col1.childNodes[j].value) && col1.childNodes[j].value != "") {
                        var payment = parseFloat(col1.childNodes[j].value);
                        runningTotals = runningTotals + payment;
                        var balance = col3.innerHTML;
                        balance = balance.replace(/,/g, ""); // document.getElementById("<%=grdQuickPayments.ClientID %>").rows[j].cells[2].innerHTML;
                        var remainder = payment - (balance.substr(1));
                        // alert(col3.innerHTML);
                        //   alert("payment: " + payment);
                        //   alert("balance.substr(1): " + balance.substr(1));
                        col2.childNodes[j].innerText = (remainder.toFixed(2));
                        if (col2.childNodes[j].innerHTML < 0) {
                            col2.childNodes[j].innerHTML = '(' + col2.childNodes[j].innerHTML + ')';
                            col2.childNodes[j].style.color = "red";
                        } else {
                            col2.childNodes[j].style.color = "black";
                        }
                    }

                }
            }
            $("#txtRunningTotal").val(runningTotals.toFixed(2));

        });


        //        $("[id$=chkBI]").click(function () {
        //            //  var quickPayment = $("#txtQuickPayment").val();

        //            // if ($("#chkBI").attr('checked')) {

        //            //                $("#<%=grdQuickPayments.ClientID %> tr").click(function (event) {
        //            //                    var idx = this.rowIndex;
        //            //                    var interest = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[5].innerHTML;

        //            //                    var remainder = $("#lblQuickPaymentRemainder").html();
        //            //                    remainder = remainder.substr(1);

        //            //                    var newinterest = (parseFloat(interest.substr(1))) - Math.abs((remainder.replace(')', '')));
        //            //                    document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[6].innerHTML = newinterest.toFixed(2);
        //            //                    $("#lblQuickPaymentRemainder").html('0.00');
        //            //                    $("#lblQuickPaymentRemainder").css('color', 'black');


        //            //                });

        //            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
        //            for (i = 1; i < grid.rows.length; i++) {
        //                var col1 = grid.rows[i].cells[8];
        //                var col2 = grid.rows[i].cells[9];
        //                var col3 = grid.rows[i].cells[3];
        //                var col4 = grid.rows[i].cells[6];
        //                var col5 = grid.rows[i].cells[7];
        //                var col6 = grid.rows[i].cells[11];

        //              //  alert(col6.childNodes[0].checked);

        //               // if(col6.childNodes[0].checked = 'true') {
        //                //    alert("true");
        //                    if (!isNaN(col1.childNodes[0].value) && col1.childNodes[0].value != "") {
        //                        var payment = parseFloat(col1.childNodes[0].value);
        //                        var balance = col3.innerHTML;
        //                        var remainder = col2.childNodes[0].innerText;
        //                        remainder = remainder.replace(')', '');
        //                        remainder = Math.abs(remainder.substr(1));
        //                        var interest = col4.innerHTML;

        //                        col5.innerHTML = (col4.innerHTML).substr(1) - ((balance).substr(1) - payment).toFixed(2);
        //                        col5.innerHTML = '$' + col5.innerHTML;

        //                        col2.childNodes[0].innerText = '0.00'; // (remainder.toFixed(2));
        //                        if (col2.childNodes[0].innerHTML < 0) {
        //                            col2.childNodes[0].innerHTML = '(' + col2.childNodes[0].innerHTML + ')';
        //                            col2.childNodes[0].style.color = "red";
        //                        } else {
        //                            col2.childNodes[0].style.color = "black";
        //                        }
        //                    }

        //              //  } else {
        //             //       alert("false");
        //             //   }

        //                //                    if (col6.childNodes[0].checked = true) {
        //                //                        // alert((payment - (balance).substr(1)).toFixed(2));
        //                //                        col5.innerHTML = (col4.innerHTML).substr(1) - ((balance).substr(1) - payment).toFixed(2);
        //                //                        col5.innerHTML = '$' + col5.innerHTML;
        //                //                    }
        //                //                    else { // if (col6.childNodes[j].value != true) 
        //                //                        col5.innerHTML = (col4.innerHTML).substr(1);
        //                //                        col2.childNodes[0].innerText = (payment - (balance).substr(1)).toFixed(2);
        //                //                    }




        //                //}
        //            }


        //        });




        //        $("[id$=chkFG]").click(function () {
        //            //  var quickPayment = $("#txtQuickPayment").val();

        //            if ($("#chkFG").attr('checked')) {

        //                $("#<%=grdQuickPayments.ClientID %> tr").click(function (event) {
        //                    var idx = this.rowIndex;
        //                    var interest = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[6].innerHTML;
        //                    var balance = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[3].innerHTML;

        //                    var txtPayment = parseFloat(balance.substr(1)) - parseFloat(interest.substr(1));

        //                    // var newinterest = (parseFloat(interest.substr(1))) - remainder;
        //                    document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[7].innerHTML = '0.00';
        //                    // $("#lblQuickPaymentRemainder").html('0.00');
        //                    $("#txtQuickPayment").val(txtPayment);
        //                    //document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[8].innerHTML = txtPayment;

        //                });
        //            } else {

        //                $("#<%=grdQuickPayments.ClientID %> tr").click(function (event) {
        //                    var idx = this.rowIndex;
        //                    // alert(document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[2].innerHTML);
        //                    $("#txtQuickPayment").val((document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[3].innerHTML).substr(1));
        //                    var quickPayment = $("#txtQuickPayment").val();
        //                    var idx = this.rowIndex;
        //                    var balance = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[3].innerHTML;

        //                    var origInterest = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[6].innerHTML;
        //                    document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[7].innerHTML = origInterest;
        //                    var remainder = quickPayment - (parseFloat(balance.substr(1)));
        //                    $("#lblQuickPaymentRemainder").html(remainder.toFixed(2));

        //                    if ($("#lblQuickPaymentRemainder").html() < 0) {
        //                        $("#lblQuickPaymentRemainder").html('(' + $("#lblQuickPaymentRemainder").html() + ')')
        //                        $("#lblQuickPaymentRemainder").css('color', 'red');
        //                    } else if ($("#lblQuickPaymentRemainder").html() = 0) {
        //                        $("#lblQuickPaymentRemainder").css('color', 'black');
        //                    } else {
        //                        $("#lblQuickPaymentRemainder").css('color', 'black');
        //                    }
        //                });

        //            }


        //        });



        //        $("[id$=LinkButton1]").click(function (event, ui) {
        //            reversePopupInfo();
        //            $(this).fadeOut(1000, function () {
        //                    $(this).remove();

        //            });
        //        });


        $("[id$=btnUpdateQuickPayments]").click(function (event, ui) {

            var idx;

            var transID;
            var balance;
            var taxes;
            var interest;
            var payment;
            var difference;
            var chkPM;
            var chkBI;
            var chkFG;

            $("#<%=grdQuickPayments.ClientID %> tr").click(function (event) {
                //Skip first(header) row
                // if (!this.rowIndex) return;
                idx = this.rowIndex;

                transID = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[0].innerHTML;
                balance = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[3].innerHTML;
                taxes = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[4].innerHTML;
                interest = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[8].innerHTML;
                payment = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[9].childNodes[0].value;
                difference = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[10].innerText;

                chkPM = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[11].childNodes[0].checked;
                chkBI = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[12].childNodes[0].checked;
                chkFG = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[13].childNodes[0].checked;

                //  alert(chkBI);
                //   alert(difference);

                //     alert(transID);
                updateQuickPayments(transID, idx, balance.substr(1), taxes.substr(1), interest, payment, difference, chkPM, chkBI, chkFG);

            });

            event.preventDefault();
        });


        $("[id$=btnDeleteQuickPayment]").click(function (event, ui) {

            var idx;

            var transID;
            var runningTotals = 0;


            $("#<%=grdQuickPayments.ClientID %> tr").click(function (event) {
                //Skip first(header) row
                // if (!this.rowIndex) return;
                idx = this.rowIndex;

                transID = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[0].innerHTML;

                deleteQuickPayment(transID, idx);

                $(this).fadeOut(1000, function () {
                    $(this).remove();

                });


            });

            event.preventDefault();


        });



        //        $("#<%=grdQuickPayments.ClientID%> tr:has(td)").click(function () {
        //            $(this).fadeOut(1000, function () {
        //                $(this).remove();
        //            });
        //        });



    });

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

        // $("#ddlTaxYear").val(taxYear);
        $("#txtTaxRollScanned").val(taxRollNumber);
        //  $("[id$=txtAmountPaid]").val(amount);

        // $("#rdoTaxRollNumber").click();
        enableDisableInputs();
        $("#btnSearchQuickPayment").click();
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
        $("#txtTaxIDScanned").val(book + "" + map + "" + parcel + split);
        //   $("[id$=txtAmountPaid]").val(amount);

        // $("#rdoAPN").click();
        enableDisableInputs();
        $("#btnSearchQuickPayment").click();
    }

    function getSplitCharacter(splitNumber) {
        var numSplit = parseInt(splitNumber, 10);
        if (0 == numSplit) {
            return "";
        } else {
            return String.fromCharCode(64 + numSplit);
        }
    }

    function deleteQuickPayment(transID,idx) {
     //   alert(transID);
        $.ajax({
            type: "POST",
            url: "MaintenanceTasks.aspx/btnDeleteQuickPayment_Click",
            data: '{"transID":"' + transID + '"}', 
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (value) {
                var result = [];
                $.each(data.d, function (index, value) {
                    result.push({ value: index, label: value });
                });
                response(result);
            }

        });

//        var grid = document.getElementById('<%=grdQuickPayments.ClientID %>');
//        var totalcol1 = 0;
//        var totals = 0;

//        for (i = 0; i < grid.rows.length; i++) {
//            col1 = grid.rows[i].cells[9];
//            //col2 = grid.rows[i].cells[1];

//            //for (j = 0; j < col1.childNodes.length; j++) {
//            if (col1.childNodes[0].type == "text") {
//                //  if (!isNaN(col1.childNodes[j].value) && col1.childNodes[j].value != "") {
//                var payment = col1.childNodes[0].value;
//                payment = payment.replace(/,/g, "");
//                totalcol1 += parseFloat(payment);
//                //  alert(totalcol1);
//                //   }
//            }
//            //  }
//        }
//        totals=
//        $("#txtRunningTotal").val(totalcol1.toFixed(2));


      
    }

    function updateQuickPayments(transID, idx, balance, taxes, interest, payment, difference, chkPM, chkBI, chkFG) {
      //  alert("!!!!!!!");
     //   alert("transID: " + transID)
        //"InvestorIDorSSAN":"' + request.term +'",
        $.ajax({
            type: "POST",
            url: "MaintenanceTasks.aspx/btnUpdateQuickPayments_Click",
            data: '{"transID":"' + transID + '","idx":"' + idx + '","balance":"' + balance + '","taxes":"' + taxes + '","interest":"' + interest + '","payment":"' + payment + '","difference":"' + difference + '","chkPM":"' + chkPM + '","chkBI":"' + chkBI + '","chkFG":"' + chkFG + '"}', //, "idx":"' + idx + '", "balance":"' + balance + '", "chkPM":"' + chkPM + '", "chkBI":"' + chkBI + '", "chkFG":"' + chkFG + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (value) {
                var result = [];
                $.each(data.d, function (index, value) {
                    result.push({ value: index, label: value });
                });
                response(result);
            }

        });
      //  window.open("MaintenanceTasks.aspx#tabFunctions2");
      //  $find('<%=tabContainerFunctions.ClientID%>').set_activeTabIndex(6);
        

    }

    function checkRDOValueSearch(rdoValue) {

        if (rdoValue == "radioTaxIDSearch") {
          //  alert("0000000000000");
            $("#radioTaxIDSearch").attr("checked",true);
          //  alert("1111111111");
            $("#txtTaxIDSearch").css("background-color", "");
           // alert("22222222222");
            $("#txtPayorSearch").css("background-color", "gray");
          //  alert("333333333333");
            $("#txtCheckNumberSearch").css("background-color", "gray");
            //  alert("44444444444");

        } else if (rdoValue == "radioPayorSearch") {

            $("#radioPayorSearch").attr("checked", true);

            $("#txtPayorSearch").css("background-color", "");
            $("#txtCheckNumberSearch").css("background-color", "gray");
            $("#txtTaxIDSearch").css("background-color", "gray");

        } else if (rdoValue == "radioCheckNumberSearch") {

            $("#radioCheckNumberSearch").attr("checked", true);

            $("#txtCheckNumberSearch").css("background-color", "");
            $("#txtPayorSearch").css("background-color", "gray");
            $("#txtTaxIDSearch").css("background-color", "gray");
        } else {
            $("#radioSearchAllChecks").attr("checked", true);
            $("#txtCheckNumberSearch").css("background-color", "gray");
            $("#txtPayorSearch").css("background-color", "gray");
            $("#txtTaxIDSearch").css("background-color", "gray");
        }

    }

    function showpopup() {
        var width = (screen.width / 2) - (500 / 2);
        var height = (screen.height / 2) - 300;
        var divpopup = document.getElementById("divViewPostDetails");
        divpopup.style.left = width + "px";
        divpopup.style.top = height + "px";
        divpopup.style.display = "block";
       // divpopup.innerText = "Alwyn Duraisingh.M";
    }

    // showViewCPPopup - Show the View CP Popup Window
  
    function showViewCheckLPSActions(title) {

        $("#divCheckLPS").dialog({
            modal: true,
            title: title,
            width: 1000,
            close: function (e, ui) {
                $(this).dialog("hide");
            }
        });
        // 
    }

    function showViewCADActions(title) {

        $("#divCAD").dialog({
            modal: true,
            title: title,
            width: 1000,
            close: function (e, ui) {
                $(this).dialog("hide");
            }
        });
        // 
    }

    function checkPM(chkPM) {        
        var idx = chkPM.parentElement.parentElement.rowIndex;
        // alert(idx);
        if (chkPM.checked) {
            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
            //     for (i = 1; i < grid.rows.length; i++) {
            var col1 = grid.rows[idx].cells[9];
            var col2 = grid.rows[idx].cells[10];
            var col3 = grid.rows[idx].cells[3];
            var col4 = grid.rows[idx].cells[7];
            var col5 = grid.rows[idx].cells[8];
            var col6 = grid.rows[idx].cells[12];
            var col7 = grid.rows[idx].cells[6];

            // alert(col1.childNodes[0].value);
            if (col1.childNodes[0].value != "") {
                var payment = parseFloat(col1.childNodes[0].value);
                var balance = col3.innerHTML;
                balance = balance.replace(/,/g, "");
                // balance = balance.substr(1);
                // alert(balance);
                var remainder = col2.childNodes[0].innerText;
                remainder = remainder.replace(')', '');
                remainder = Math.abs(remainder.substr(1));
                var interest = col4.innerHTML;
                var priorInterest = (col7.innerHTML).substr(1);
                var newInterest = (col4.innerHTML).substr(1) - ((balance).substr(1) - payment);

               // alert(priorInterest);
                //alert(balance.substr(1));
                var newPayment = balance.substr(1) - interest.substr(1);
                newPayment = newPayment + parseFloat(priorInterest);

               // alert(newPayment);
//                 alert("interest: " + interest);
                //                 alert(newPayment);
                   col1.childNodes[0].value = newPayment.toFixed(2);
                col5.innerHTML = col7.innerHTML;
                // col2.childNodes[0].innerText = '0.00'; // (remainder.toFixed(2));
                if (col2.childNodes[0].innerHTML < 0) {
                    col2.childNodes[0].innerHTML = '(' + col2.childNodes[0].innerHTML + ')';
                    col2.childNodes[0].style.color = "red";
                } else {
                    col2.childNodes[0].style.color = "black";
                }
            }
            //  }
        } else {
            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
            //     for (i = 1; i < grid.rows.length; i++) {
            var col1 = grid.rows[idx].cells[9];
            var col2 = grid.rows[idx].cells[10];
            var col3 = grid.rows[idx].cells[3];
            var col4 = grid.rows[idx].cells[7];
            var col5 = grid.rows[idx].cells[8];
            var col6 = grid.rows[idx].cells[12];

            if (col1.childNodes[0].value != "") {
                var payment = parseFloat(col1.childNodes[0].value);
                var balance = col3.innerHTML;
                balance = balance.replace(/,/g, "");
                var remainder = col2.childNodes[0].innerText;
                remainder = remainder.replace(')', '');
                remainder = Math.abs(remainder.substr(1));
                var interest = col4.innerHTML;
               // var priorInterest = col7.innerHTML;
                var newInterest = (col4.innerHTML).substr(1) - ((balance).substr(1) - payment);

                var newPayment = balance.substr(1);
                newPayment = parseFloat(newPayment);

                // alert(newPayment);
                //                 alert("interest: " + interest);
                //                 alert(newPayment);
                col1.childNodes[0].value = newPayment.toFixed(2);
                col5.innerHTML = col4.innerHTML;

                if (col2.childNodes[0].innerHTML < 0) {
                    col2.childNodes[0].innerHTML = '(' + col2.childNodes[0].innerHTML + ')';
                    col2.childNodes[0].style.color = "red";
                } else {
                    col2.childNodes[0].style.color = "black";
                }
            }
            //  }
        }

    }

    function checkFG(chkFG) {
      //  alert(chkFG.parentElement.parentElement.rowIndex);
        var idx = chkFG.parentElement.parentElement.rowIndex;
       // alert(idx);
        if (chkFG.checked) {
            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
       //     for (i = 1; i < grid.rows.length; i++) {
                var col1 = grid.rows[idx].cells[9];
                var col2 = grid.rows[idx].cells[10];
                var col3 = grid.rows[idx].cells[3];
                var col4 = grid.rows[idx].cells[7];
                var col5 = grid.rows[idx].cells[8];
                var col6 = grid.rows[idx].cells[12];

               // alert(col1.childNodes[0].value);
                if (col1.childNodes[0].value != "") {
                    var payment = parseFloat(col1.childNodes[0].value);
                    var balance = col3.innerHTML;
                    balance = balance.replace(/,/g, "");
                   // balance = balance.substr(1);
                   // alert(balance);
                    var remainder = col2.childNodes[0].innerText;
                    remainder = remainder.replace(')', '');
                    remainder = Math.abs(remainder.substr(1));
                    var interest = col4.innerHTML;
                    var newInterest = (col4.innerHTML).substr(1) - ((balance).substr(1)- payment).toFixed(2);

                    var newPayment = balance.substr(1) - interest.substr(1);
                    
                   // alert("balance: " + balance);
                   // alert("interest: " + interest);
                   // alert(newPayment);
                    col1.childNodes[0].value = newPayment.toFixed(2);
                    col5.innerHTML = '0.00'; 
                   // col2.childNodes[0].innerText = '0.00'; // (remainder.toFixed(2));
                    if (col2.childNodes[0].innerHTML < 0) {
                        col2.childNodes[0].innerHTML = '(' + col2.childNodes[0].innerHTML + ')';
                        col2.childNodes[0].style.color = "red";
                    } else {
                        col2.childNodes[0].style.color = "black";
                    }
                }
          //  }
        } else {
                var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
                //     for (i = 1; i < grid.rows.length; i++) {
                var col1 = grid.rows[idx].cells[9];
                var col2 = grid.rows[idx].cells[10];
                var col3 = grid.rows[idx].cells[3];
                var col4 = grid.rows[idx].cells[7];
                var col5 = grid.rows[idx].cells[8];
                var col6 = grid.rows[idx].cells[12];

                if (col1.childNodes[0].value != "") {
                    var payment = parseFloat(col1.childNodes[0].value);
                    var balance = col3.innerHTML;
                    balance = balance.replace(/,/g, "");
                    var remainder = col2.childNodes[0].innerText;
                    remainder = remainder.replace(')', '');
                    remainder = Math.abs(remainder.substr(1));
                    var interest = col4.innerHTML;
                    var newInterest = (col4.innerHTML).substr(1) - ((balance).substr(1) - payment);

                  //  balance = balance.substr(1);
              //      interest = interest.substr(1);

                   // var newPayment = parseFloat(balance) + parseFloat(interest);

//                    alert(balance.substr(1));
//                    alert(interest.substr(1));


                    col5.innerHTML = col4.innerHTML;

                    col1.childNodes[0].value = balance.substr(1);

                    if (col2.childNodes[0].innerHTML < 0) {
                        col2.childNodes[0].innerHTML = '(' + col2.childNodes[0].innerHTML + ')';
                        col2.childNodes[0].style.color = "red";
                    } else {
                        col2.childNodes[0].style.color = "black";
                    }
                }
                //  }
        }

        }

        function checkAllCAD(chkCAD) {
            var grid = document.getElementById("<%=grdCAD2.ClientID %>");
            if (chkCAD.checked) {            
            for (i = 1; i < grid.rows.length; i++) 
                {                   
                    var col1 = grid.rows[i].cells[0];
                    col1.childNodes[0].checked = true;
                }

            } else {

                for (i = 1; i < grid.rows.length; i++) {
                    var col1 = grid.rows[i].cells[0];
                    col1.childNodes[0].checked = false;
                }
                
            }
        }

    function checkALLBI(chkALLBI) {
        if (chkALLBI.checked) {
            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
            var col6 = grid.rows[1].cells[12];
            col6.childNodes[0].checked = true;
            
            //            for (i = 1; i < grid.rows.length; i++) {
//            }
        }else{

        }

    }


    function checkBI(chkBI){
     //   alert(this.checked);

        if (chkBI.checked) {
            //alert("1111");

            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
            for (i = 1; i < grid.rows.length; i++) {
                var col1 = grid.rows[i].cells[9];
                var col2 = grid.rows[i].cells[10];
                var col3 = grid.rows[i].cells[3];
                var col4 = grid.rows[i].cells[7];
                var col5 = grid.rows[i].cells[8];
                var col6 = grid.rows[i].cells[12];

                //  alert(col6.childNodes[0].checked);

                // if(col6.childNodes[0].checked = 'true') {
                //    alert("true");
                if (col1.childNodes[0].value != "") {
                    var payment = parseFloat(col1.childNodes[0].value);
                    var balance = col3.innerHTML;
                    balance = balance.replace(/,/g, "");
                    var remainder = col2.childNodes[0].innerText;
                    remainder = remainder.replace(')', '');
                    remainder = Math.abs(remainder.substr(1));
                    var interest = col4.innerHTML;
                    var newInterest = (col4.innerHTML).substr(1) - ((balance).substr(1) - payment).toFixed(2);
                    col5.innerHTML = newInterest.toFixed(2);
                    col5.innerHTML = '$' + (col5.innerHTML);

                    col2.childNodes[0].innerText = '0.00'; // (remainder.toFixed(2));
                    if (col2.childNodes[0].innerHTML < 0) {
                        col2.childNodes[0].innerHTML = '(' + col2.childNodes[0].innerHTML + ')';
                        col2.childNodes[0].style.color = "red";
                    } else {
                        col2.childNodes[0].style.color = "black";
                    }
                }
            }
        } else  {
        //    alert("2222");

            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
            for (i = 1; i < grid.rows.length; i++) {
                var col1 = grid.rows[i].cells[9];
                var col2 = grid.rows[i].cells[10];
                var col3 = grid.rows[i].cells[3];
                var col4 = grid.rows[i].cells[7];
                var col5 = grid.rows[i].cells[8];
                var col6 = grid.rows[i].cells[12];

                //  alert(col6.childNodes[0].checked);

                // if(col6.childNodes[0].checked = 'true') {
                //    alert("true");
                if (col1.childNodes[0].value != "") {
                    var payment = parseFloat(col1.childNodes[0].value);
                    var balance = col3.innerHTML;
                    balance = balance.replace(/,/g, "");
                    var remainder = col2.childNodes[0].innerText;
                    remainder = remainder.replace(')', '');
                    remainder = Math.abs(remainder.substr(1));
                    var interest = col4.innerHTML;
                  //  var newInterest = (col4.innerHTML).substr(1) - ((balance).substr(1) - payment).toFixed(2);

                    col5.innerHTML = (col4.innerHTML).substr(1);
                    col5.innerHTML = "$" + col5.innerHTML
                    col2.childNodes[0].innerText = (payment - (balance).substr(1)).toFixed(2);

                   /// alert("payment" + payment);
                   // alert("balance" + balance);

                    var newDiff = payment - balance.substr(1);
                   col2.childNodes[0].innerText = newDiff.toFixed(2);
                     // (remainder.toFixed(2));
                    if (col2.childNodes[0].innerHTML < 0) {
                        col2.childNodes[0].innerHTML = '(' + col2.childNodes[0].innerHTML + ')';
                        col2.childNodes[0].style.color = "red";
                    } else {
                        col2.childNodes[0].style.color = "black";
                    }
                }
            }
            
        }
    }

    function checkMe(payment){
    //    alert(payment);

        var idx;
        $("#<%=grdQuickPayments.ClientID %> tr").click(function (event) {
            var quickPayment = payment; // $("[id$=txtQuickPayment]").val(); //document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[7].innerHTML;
            idx = this.rowIndex;
            var balance = document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[2].innerHTML;
            alert(balance);
            var remainder = quickPayment - (parseFloat(balance.substr(1)));

            //  $("#lblQuickPaymentRemainder").html(remainder.toFixed(2));
            alert(document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[9].innerHTML);
            document.getElementById("<%=grdQuickPayments.ClientID %>").rows[idx].cells[9].innerHTML = remainder.toFixed(2);

            if ($("#lblQuickPaymentRemainder").html() < 0) {
                $("#lblQuickPaymentRemainder").html('(' + $("#lblQuickPaymentRemainder").html() + ')')
                $("#lblQuickPaymentRemainder").css('color', 'red');
            } else if ($("#lblQuickPaymentRemainder").html() = 0) {
                $("#lblQuickPaymentRemainder").css('color', 'black');
            } else {
                $("#lblQuickPaymentRemainder").css('color', 'black');
            }


        });

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

    function showViewReturnedChecksActions() {

    
        showReturnedChecks()

    }

    function showViewRefundsActions() {

        showRefunds()

    }

    function showRefunds() {
        $("#divProcessRefunds").dialog({
            modal: true,
            title: "Refunds Awaiting Payment",
            width: 1000,
            height: 700,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
    }

    function showReturnedChecks() {
        $("#divReturnedChecks").dialog({
            modal: true,
            title: "Locate Returned Checks",
            width: 1500,
            height: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
    }

 

    function showReverseTrans(title) {
        //  $("[id$=lblDeleteDivPayorName]").val("MIA");

        $("#divReverseTrans").dialog({
            modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
        // 
    }
    //btnContinueRunNightlyFunc


    function deletePopupInfo() {
        var transID = $("#lblDeleteDivRecordID").html();// document.getElementById("lblDeleteDivRecordID").value
        var grpKey = $("#lblGroupKey").html();
        $.ajax({
            type: "POST",
            url: "TaxSupervisor.aspx/btnDeleteTransaction_Click",
            data: '{"grpKey":"' + grpKey + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (value) {
                    var result = [];
                    $.each(data.d, function (index, value) {
                        result.push({ value: index, label: value });
                    });
                  response(result);
              }
            
          });      
    }




    function reversePopupInfo() {
      //  showLoadingBox();
        var transID = $("#lblDeleteDivRecordID2").html(); // document.getElementById("lblDeleteDivRecordID").value
        var grpKey = $("#lblGroupKey2").html();
        $.ajax({
            type: "POST",
            url: "MaintenanceTasks.aspx/btnReverseTransaction_Click",
            data: '{"grpKey":"' + grpKey + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (value) {
                var result = [];
                $.each(data.d, function (index, value) {
                    result.push({ value: index, label: value });
                });
                response(result);

                // alert(msg);
            }
        });
        window.open("MaintenanceTasks.aspx", "_self");
       
        
    }


        function showLoginDialog() {
            $("#divLogin").dialog({
                modal: true
            }).parent().appendTo("form");
        }

        function showQPCommitDetails(runningTotal) {
            var grid = document.getElementById("<%=grdQuickPayments.ClientID %>");
            var count = grid.rows.length;
         // //  alert("count: " + grid.rows.length);
        //    alert("total: " + $("#txtRunningTotal").val());
            var total = $("#txtRunningTotal").val();

            $("#lblQPPaymentsCount").text(count);
            $("#lblQPPaymentsAmount").text(runningTotal);
            

            $("#divQPCommitDetails").dialog({
                modal: true,
                title: 'Quick Payments Commit Details',
                width: 500,
                close: function (e, ui) {
                    $(this).dialog("destroy");
                }
            });
            // 
        }

</script>


    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <!--Modal Popup divs-->

    <div id="divLoading" title="Loading, please wait..." class="divPopup">
        <img src="ajax-loader_redmond.gif" alt="Loading..." width="220" height="19" />
        <br />
        <asp:Label runat="server" ID="lblFeedback"></asp:Label>
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
            <%--<b>Cash at Login:</b>
            <asp:TextBox ID="txtLoginStartCash" runat="server" Width="100px" CssClass="required number"></asp:TextBox>--%>
        </fieldset>
        <asp:Button ID="btnLogin" runat="server" Text="Login" />
    </div>

    <div id="divViewCP" class="divPopup">
        CP Owned by Foreclosing Investor
        <br />
        <asp:GridView ID="grdViewCPOwned" runat="server" AutoGenerateColumns="False">
            <Columns>
                <asp:BoundField HeaderText="CP Number" DataField="CP" />
                <asp:BoundField HeaderText="Purchase Date" DataField="Purchase Date" />
                <asp:BoundField HeaderText="Face Value" DataField="Face Value" DataFormatString="{0:C}" />
            </Columns>
        </asp:GridView>
        <br />
        CP Help by Other Investors
        <br />
        <asp:GridView ID="grdViewCPHelp" runat="server" AutoGenerateColumns="False">
            <Columns>
                <asp:BoundField HeaderText="CP Number" DataField="CP" />
                <asp:BoundField HeaderText="Purchase Date" DataField="Purchase Date" />
                <asp:BoundField HeaderText="Face Value" DataField="Face Value" DataFormatString="{0:C}" />
                <asp:BoundField HeaderText="Investor" DataField="InvestorID" />
            </Columns>
        </asp:GridView>
        <br />
    </div>

    <div id="divReverseTrans" class="divPopup" runat="server">
    Transaction:
    <asp:Label ID="lblDeleteDivRecordID2" runat ="server"></asp:Label><br />
    <asp:HiddenField ID="hdnDeleteDivRecordID2" runat="server"/>
    <input type="hidden" id="inputHidden2" runat="server" />
    Group Key:
    <asp:Label ID="lblGroupKey2" runat ="server"></asp:Label><br />
    Payor:
    <asp:Label ID="lblDeleteDivPayorName2" runat ="server"></asp:Label><br />
    Amount:
    <asp:Label ID="lblDeleteDivAmount2" runat ="server"></asp:Label><br /><br />

        This transaction has been posted, and the following actions will be taken upon commit:
        <br />
        <br />
            (1) Equivalent negative values will be added to all group transaction payments.<br />
            (2) Equivalent negative values will be added to all group transaction apportionments.<br />
            (3) Transaction records will be marked as 'Reversed.'<br />
            (4) An NSF fee will be added to each transaction tax roll charges.
        <br />
        <br />

        <asp:LinkButton runat="server" Text="Reverse" ID="LinkButton1" onClientclick="javascript:reversePopupInfo();"  ForeColor="Red"  />&nbsp;&nbsp;
       <%-- <asp:LinkButton runat="server" ID="LinkButton8" Text="Cancel" OnClientClick="$('#divReverseTrans').dialog('close')" ForeColor="Red"  />--%>

    </div>


    <div id="divQPCommitDetails" class="divPopup" runat="server">
    <asp:Label ID="lblQPPaymentsCount" runat ="server"></asp:Label>
     Payment/s for $
    <asp:Label ID="lblQPPaymentsAmount" runat ="server"></asp:Label><br />
    has been saved to:<br /><br />

        Transactions;<br />
        Payments;<br />
        Apportionment, and; <br />
        Interest adjustments to charges.


        <br />
        <br />

        <asp:LinkButton runat="server" Text="OK" ID="LinkButton2"  ForeColor="Red"  />&nbsp;&nbsp;
       <%-- <asp:LinkButton runat="server" ID="LinkButton8" Text="Cancel" OnClientClick="$('#divReverseTrans').dialog('close')" ForeColor="Red"  />--%>

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
               <%-- <td>
                    Operator:
                    <asp:Label ID="lblOperatorName" runat="server"></asp:Label>
                </td>--%>
            </tr>
            <tr>
                <td>
                    <h2>
                        Cashier Supervisor</h2>
                </td>
              <%--  <td>
                    Date:
                    <asp:Label ID="lblCurrentDate" runat="server"></asp:Label>
                </td>--%>
            </tr>
        </table>
    </div>
    <!-- Main tabs -->
    <div id="mainTabs">
        <ul>
            <li><a href="#tabPosting">Posting</a></li>
           <%-- <li><a href="#tabRefunds">Refund Approval</a></li>--%>
           <li><a href="#tabFunctions">Functions</a></li>
       <%--     <li><a href="#tabLenderProcessing">LPS</a></li>--%>
            <li><a href="#tabBoardOrders">Board Orders</a></li>
            <li><a href="#tabSalePrep">Sale Prep</a></li>
           <%-- <li><a href="#tabLetters">Letters</a></li>--%>
            <li><a href="#tabFunctions2">Maintenance Tasks</a></li>
            
        </ul>
        <!-- Posting tab -->
        <div id="tabPosting">
          
        </div>
        <!-- Refunds tab -->
       <%-- <div id="tabRefunds">
            <div>
                <asp:Button ID="btnLoadRefund" runat="server" Text="Load Refunds" ClientIDMode="Static" />
                <br />
                <br />
            </div>
            <div>
                <fieldset title="Refunds">
                    <asp:Label ID="lblNoRefundData" runat="server" ClientIDMode="Static" Text="No Refunds have been loaded yet." />
                    <asp:GridView ID="grdRefunds" runat="server" AutoGenerateColumns="false" Width="100%">
                        <Columns>
                            <asp:BoundField HeaderText="Payor" DataField="PAYOR_NAME" />
                            <asp:BoundField HeaderText="Amount" DataField="REFUND_AMT" DataFormatString="{0:c}" />
                            <asp:BoundField HeaderText="Check Number" DataField="CHECK_NUMBER" />
                            <asp:BoundField HeaderText="Payment Date" DataField="PAYMENT_DATE" />
                            <asp:TemplateField HeaderText="Days">
                                <ItemTemplate>
                                    <%#GetRefundDays(DataBinder.Eval(Container.DataItem, "PAYMENT_DATE")) %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Remit">
                                <ItemTemplate>
                                    <asp:CheckBox ID="chkRefunRemit" runat="server" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </fieldset>
            </div>
            <div>
                <br />
                <asp:Button ID="btnSaveRefund" runat="server" Text="Save" ClientIDMode="Static" />
            </div>
        </div>--%>
        <!-- Lender Processing Services tab -->
       <%-- <div id="tabLenderProcessing">
            <div>
                <asp:Button ID="btnLPSLoad" runat="server" Text="Load Data" ClientIDMode="Static" />
                <br />
                <br />
            </div>
            <div>
                <fieldset>
                    <asp:Label ID="lblLPSNoData" runat="server" Text="No LPS data has been loaded yet." />
                    <asp:GridView ID="grdLPS" runat="server" AutoGenerateColumns="false" Width="100%">
                        <Columns>
                            <asp:BoundField HeaderText="Lender Processing Service" DataField="LPS_NAME" />
                            <asp:TemplateField HeaderText="2012">
                                <ItemTemplate>
                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2012)%>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="2011">
                                <ItemTemplate>
                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2011)%>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="2010">
                                <ItemTemplate>
                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2010)%>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="2009">
                                <ItemTemplate>
                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2009)%>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="2008">
                                <ItemTemplate>
                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2008)%>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </fieldset>
            </div>
        </div>--%>
        <!-- Board Orders tab -->
        <div id="tabBoardOrders">
        </div>
        <!-- Sale Prep tab -->
         <div id="tabSalePrep">

        </div>
         <!-- Letters tab -->
     <%--   <div id="tabLetters">
            <div>
                <fieldset>
                    <asp:Label ID="lblNoLetterData" runat="server" Text="No data has been loaded yet." />
                    <asp:GridView ID="grdLetters" runat="server" AutoGenerateColumns="false">
                        <Columns>
                            <asp:BoundField HeaderText="Cashier" DataField="CASHIER" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField HeaderText="Letter Type" DataField="DESCRIPTION" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField HeaderText="Tax Payor" DataField="OWNER_NAME" ItemStyle-HorizontalAlign="Left" />
                            <asp:TemplateField HeaderText="Tax Year/Roll">
                                <ItemTemplate>
                                    <%#Eval("TAX_YEAR")%>/<%#Eval("TAX_ROLL_NUMBER")%>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField HeaderText="Activity Date" DataField="LETTER_DATE" DataFormatString="{0:d}" />
                            <asp:TemplateField HeaderText="Age">
                                <ItemTemplate>
                                    <asp:Label ID="age" runat="server" Text='<%# TimeSpan(IIf(IsDBNull(Eval("LETTER_DATE")), DateTime.Now,Eval("LETTER_DATE"))) %>'></asp:Label>
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
                                    <asp:Button ID="btnPrintCheck" runat="server" Text="Print" Enabled="false" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </fieldset>
            </div>
            <div>
                <br />
                <asp:Button ID="btnLettersSave" runat="server" Text="Save" ClientIDMode="Static" />
            </div>
        </div>--%>
        <!-- Functions 2 tab -->
        <div id="tabFunctions2">
            <ajaxToolkit:TabContainer ID="tabContainerFunctions" runat="server">
                <ajaxToolkit:TabPanel ID="tabReturnedChecks" runat="server" HeaderText="Locate Returned Checks">
                
                <ContentTemplate>      
                        <td>
                            <asp:RadioButton ID="radioSearchAllChecks" runat="server" GroupName="SearchGroup" ClientIDMode="Static"
                                Text="Search All " onclick="javascript:checkRDOValueSearch('radioSearchAllChecks');" />                         
                        </td>             
                        <td>
                            <asp:RadioButton ID="radioTaxIDSearch" runat="server" GroupName="SearchGroup" ClientIDMode="Static"
                                Text="Tax ID Number: " onclick="javascript:checkRDOValueSearch('radioTaxIDSearch');" />
                            <asp:TextBox ID="txtTaxIDSearch" runat="server" Width="80px" onclick="javascript:checkRDOValueSearch('radioTaxIDSearch');" Wrap="false"  ClientIDMode="Static"/>
                        </td>
                        <td>
                            <asp:RadioButton ID="radioCheckNumberSearch" runat="server" GroupName="SearchGroup" Text="Check Number:"  ClientIDMode="Static" onclick="javascript:checkRDOValueSearch('radioCheckNumberSearch');"/>
                            <asp:TextBox ID="txtCheckNumberSearch" runat="server" Width="70px" Wrap="False" onclick="javascript:checkRDOValueSearch('radioCheckNumberSearch');" ClientIDMode="Static"/>
                        </td>
                        <td>
                            <asp:RadioButton ID="radioPayorSearch" runat="server" GroupName="SearchGroup" Text="Payor:" ClientIDMode="Static" onclick="javascript:checkRDOValueSearch('radioPayorSearch');"/>
                            <asp:TextBox ID="txtPayorSearch" runat="server" Width="80px" onclick="javascript:checkRDOValueSearch('radioPayorSearch');" ClientIDMode="Static"/>                            
                        </td>
                        


                             <%--   <asp:Button ID="searchReturnedChecks" runat="server" Text="Search" />--%>
                                <asp:LinkButton runat="server" Text="Search" ID="btnSearchReturnedChecks" Onclick="searchReturnedChecks_click2" ClientIDMode="Static"/>&nbsp;&nbsp;&nbsp;&nbsp;
                                <br />
                                <br />
                                <asp:GridView ID="grdReturnedChecks2" runat="server" AutoGenerateColumns="false" Width="100%">
                                        <EmptyDataTemplate>
                                            This table is empty</EmptyDataTemplate>
                                        <Columns>
                                            <asp:BoundField HeaderText="Transaction" DataField="RECORD_ID" />
                                            <asp:BoundField HeaderText="Group Key" DataField="GROUP_KEY" NullDisplayText ="" />
                                            <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                            <asp:BoundField HeaderText="Tax Roll" DataField="TAX_ROLL_NUMBER" />
                                            <asp:BoundField HeaderText="Payment Date" DataField="PAYMENT_DATE" DataFormatString="{0:d}" />
               
                                            <asp:BoundField HeaderText="Amount" DataField="PAYMENT_AMT" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField HeaderText="Payor" DataField="PAYOR_NAME" />
                                            <asp:BoundField HeaderText="Check Number" DataField="CHECK_NUMBER" />
                                            <asp:BoundField HeaderText="Tax Amount" DataField="TAX_AMT" DataFormatString="{0:$#,#.00;($#,#.00);''}" />
                                           <%-- <asp:BoundField HeaderText="Refund Amount" DataField="REFUND_AMT" DataFormatString="{0:$#,#.00;($#,#.00);''}" />
                                            <asp:BoundField HeaderText="Over/(Under)" DataField="KITTY_AMT" DataFormatString="{0:$#,#.00;($#,#.00);''}" />   --%>                                     
                                            <%--<asp:TemplateField HeaderText="Delete">
                                                <ItemTemplate >                                            
                                                    <asp:Button ID="btnReverseTrans2" Text="Reverse" runat="server"
                                                    CommandName ="ReverseTransaction"  CommandArgument ="<%#Container.DataItemIndex%>" ClientIDMode ="Static"/>
                                               
                                                </ItemTemplate>
                                            </asp:TemplateField>  --%> 
                                            <asp:TemplateField HeaderText="Reverse">
                                            <ItemTemplate >                                            
                                                <asp:Button ID="btnReverseTrans2" Text="Reverse" runat="server" 
                                                CommandName ="ReverseTransaction"  CommandArgument ="<%#Container.DataItemIndex%>" ClientIDMode ="Static" Enabled='<%# (Not Eval("Transaction_status")=4)%>'/>
                                               
                                            </ItemTemplate>
                                        </asp:TemplateField>                                          
                                        </Columns>
                                    </asp:GridView>
                                    <br />
                                    <br />

                           <%-- <asp:LinkButton runat="server" Text="Commit" ID="LinkButton5" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;--%><%--OnClick="ViewReturnedChecks"--%>
                            <%--<asp:LinkButton runat="server" Text="Cancel" ID="LinkButton13" OnClientClick="$('#divReturnedChecks').dialog('close')" ForeColor="Red" />&nbsp;&nbsp;--%>

                </ContentTemplate>
               
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="TabPanel1" runat="server" HeaderText="Process Refunds">
                <ContentTemplate>
                 <asp:Label ID="Label4" Text="Overpayments" runat="server" Font-Size="Medium" Font-Bold ="true"></asp:Label>
                                <asp:GridView ID="grdProcessRefunds2" runat="server" AutoGenerateColumns="false" Width="100%">
                                        <EmptyDataTemplate>
                                            This table is empty</EmptyDataTemplate>
                                        <Columns>
                                        <asp:TemplateField>
                                            <HeaderTemplate> 
                                                <input type="button" value="/"  name="CheckAll" onclick="javascript:$('#grdProcessRefunds2 input[type=checkbox]').each(function(){this.checked=!this.checked;});" />
                    
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                           <%-- <input type="checkbox" id="chkRefunds2" name="chkRefunds" runat="server" onselect="javascript:this.checked;" value="<%#Container.DataItemIndex %>"/>--%>
                                                <asp:CheckBox ID="chkRefunds2" runat="server"/> <%--AutoPostBack="true" OnCheckedChanged ="chkProcessRefunds" --%>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                            <asp:BoundField HeaderText="Trans (Group)" DataField="Transaction" />
                                            <asp:BoundField HeaderText="Year (Roll)" DataField="Year (Roll)" />
                                            <asp:BoundField HeaderText="Status" DataField="Status" />
                                            <asp:BoundField HeaderText="Date" DataField="Date" DataFormatString="{0:d}" />                      
                                            <asp:BoundField HeaderText="Apply To" DataField="Apply To" />
                                            <asp:BoundField HeaderText="Name" DataField="Name" />
                                              <asp:BoundField HeaderText="Payment" DataField="Payment" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                                <asp:BoundField HeaderText="Tax" DataField="Tax" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField HeaderText="Refund" DataField="Refund" />                                                                                  
                                        </Columns>
                                    </asp:GridView>
                                    <br />
                                    <br />

                                    <asp:Label ID="Label5" Text="Investor CP Refunds" runat="server"  Font-Size="Medium"  Font-Bold ="true"></asp:Label>
                                    <asp:GridView ID="grdCPRefunds2" runat="server" AutoGenerateColumns="false" Width="100%">
                                        <EmptyDataTemplate>
                                            This table is empty</EmptyDataTemplate>
                                        <Columns>
                                        <asp:TemplateField>
                                            <HeaderTemplate> 
                                                <input type="button" value="/"  name="CheckAll" onclick="javascript:$('#grdCPRefunds2 input[type=checkbox]').each(function(){this.checked=!this.checked;});" />
                    
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkCPRefunds2" runat="server" />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                            <asp:BoundField HeaderText="Trans (Group)" DataField="Trans (Group)" />
                                            <asp:BoundField HeaderText="Year (Roll)" DataField="Year (Roll)" />
                                            <asp:BoundField HeaderText="Status" DataField="Status" />
                                            <asp:BoundField HeaderText="Apply To" DataField="Apply To" />       
                                            <asp:BoundField HeaderText="Redeem Payment" DataField="Redeem Payment" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                                <asp:BoundField HeaderText="Charges" DataField="Charges" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                                <asp:BoundField HeaderText="Payments" DataField="Payments" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                                <asp:BoundField HeaderText="Apportioned" DataField="Apportioned" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField HeaderText="Refund" DataField="Refund" />                                                                                  
                                        </Columns>
                                    </asp:GridView>
                                    <input type="button" value="Commit" id="btnReturnChecksCommit"  clientidmode="Static" runat="server" onserverclick ="ProcessReturnedChecks2" />
                                    <input type="button" value="Print Letters" id="btnPrintRefundLetters" runat="server"  clientidmode="Static" />
                                   <%-- <asp:LinkButton runat="server" Text="Commit" ID="btnContinueProcessRefunds" ForeColor="Red" OnClick="ProcessReturnedChecks" />&nbsp;&nbsp;&nbsp;&nbsp;--%><%--OnClick="ViewReturnedChecks"--%>
                                    <%--<asp:LinkButton runat="server" Text="Cancel" ID="LinkButton14" OnClientClick="$('#divProcessRefunds').dialog('close')" ForeColor="Red" />&nbsp;&nbsp;--%>


                </ContentTemplate>
                       
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="TabPanel2" runat="server" HeaderText="Daily Letters">
               <ContentTemplate>
                 <asp:Label ID="Label2" Text="Daily Letters" runat="server" Font-Size="Medium" Font-Bold ="true"></asp:Label>
                                <asp:GridView ID="grdDailyLetters" runat="server" AutoGenerateColumns="false" Width="100%">
                                        <EmptyDataTemplate>
                                            This table is empty</EmptyDataTemplate>
                                        <Columns>       
                                        <asp:TemplateField>
                                            <HeaderTemplate> 
                                                <input type="button" value="/"  name="CheckAllDailyLetters" onclick="javascript:$('#grdDailyLetters input[type=checkbox]').each(function(){this.checked=!this.checked;});" />
                    
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkDailyLetters" runat="server" />
                                            </ItemTemplate>
                                        </asp:TemplateField>                                 
                                            <asp:BoundField HeaderText="Trans (Group)" DataField="Trans (Group)" />
                                            <asp:BoundField HeaderText="Year (Roll)" DataField="Year (Roll)" />
                                            <asp:BoundField HeaderText="Date" DataField="Date" DataFormatString="{0:d}" />                      
                                            <asp:BoundField HeaderText="Letter Reason" DataField="Letter Reason" />
                                              <asp:BoundField HeaderText="Payment" DataField="Payment" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                                <asp:BoundField HeaderText="Account Balance" DataField="Account Balance" DataFormatString="{0:c}"
                                                ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField HeaderText="CP Count" DataField="CP Count" />                                                                                  
                                        </Columns>
                                    </asp:GridView>
                                    <br />
                                    <br />
                               <asp:LinkButton runat="server" Text="Print Letters" ID="btnPrintDailyLetters" ClientIDMode ="Static"/>
                               <asp:LinkButton runat="server" Text="Print Labels" ID="btnPrintDailyLabels"  ClientIDMode ="Static" />
                               <asp:LinkButton runat="server" Text="Clear Letters" ID="btnClearDailyLetters" ClientIDMode ="Static"/>
                    </ContentTemplate>

                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="TabPanel3" runat="server" HeaderText="Check LPS">
                         <ContentTemplate>
                        <div>
                             <%--  <asp:Button ID="btnLPSLoad" runat="server" Text="Load Data" ClientIDMode="Static" OnClick="LoadLPS"/><br />--%>
                              <%-- <asp:LinkButton runat="server" Text="Load Data" ID="btnContinueCheckLPS" OnClick="LoadLPS" ForeColor="Red"/>
                                <br />
                                <br />--%>
                            </div>
                            <div>
                                <fieldset>
                                    <asp:Label ID="Label6" runat="server" Text="No LPS data has been loaded yet." />
                                    <asp:GridView ID="grdLPS2" runat="server" AutoGenerateColumns="false" Width="100%">
                                        <Columns>
                                            <asp:BoundField HeaderText="Lender Processing Service" DataField="LPS_NAME" />
                                            <asp:TemplateField HeaderText="2012">
                                                <ItemTemplate>
                                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2012)%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="2011">
                                                <ItemTemplate>
                                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2011)%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="2010">
                                                <ItemTemplate>
                                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2010)%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="2009">
                                                <ItemTemplate>
                                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2009)%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="2008">
                                                <ItemTemplate>
                                                    <%#GetLPSData(DataBinder.Eval(Container.DataItem, "RECORD_ID"), 2008)%>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </fieldset>
                            </div> 
                </ContentTemplate>

                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="TabPanel4" runat="server" HeaderText="Computer Aided Payments">
                <ContentTemplate>
                <table>
                    <tr>
                    <td>
                         <asp:Button ID="btnCADOpenSession" Text="Open Cashier Session" runat="server" OnClick="CADOpenSession" BackColor ="Yellow" />
                    </td>
                        <td>
                            Session ID:
                            <asp:Label ID="lblCADSessID" runat="server"></asp:Label>
                            <br />
                            Operator:
                            <asp:Label ID="lblCADOperator" runat="server"></asp:Label>
                            <br />
                            Opened:
                            <asp:Label ID="lblCADSessOpen" runat="server"></asp:Label>
                        </td>
                        <td>
                        &nbsp;
                        &nbsp;
                        </td>
                        <td>
                            Search By:<br />
                            <asp:Button runat="server" Text="All" ID="btnSearchAll" OnClick="LoadCAD2" ClientIDMode ="Static"/>
                            <asp:Button runat="server" Text="Match" ID="btnSearchMatch" OnClick="LoadCADMatch" ClientIDMode ="Static"/>
                            <asp:Button runat="server" Text="Partial" ID="btnSearchPartial"  OnClick="LoadCADPartial" ClientIDMode ="Static"/>
                            <asp:Button runat="server" Text="Refund" ID="btnSearchRefund" OnClick="LoadCADRefund" ClientIDMode ="Static"/>                            
                            &nbsp;
                            <asp:CheckBox ID="chkCADDates" Checked="false" Text="Date:" runat="server" /> 
                            <asp:DropDownList ID="ddlCADDates" runat="server" Width="120px" >                               
                        </asp:DropDownList>
                        </td>
                        <td>
                            Connection:                   
                        <asp:DropDownList ID="ddlConnection" runat="server" Width="120px" >                               
                        </asp:DropDownList>
                        </td>
                    </tr>                   
                </table>
                    <br />
                    <br />

                            <asp:GridView ID="grdCAD2" runat="server" AutoGenerateColumns="false" Width="100%">
                                <EmptyDataTemplate>
                                    This table is empty</EmptyDataTemplate>
                                <Columns>  
                                 <asp:TemplateField>
                                    <HeaderTemplate>                                                 
                                        <asp:CheckBox ID="chkALLCAD" runat="server" onclick="javascript:checkAllCAD(this);" Enabled ="false"/>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkCAD" runat="server" ClientIDMode ="Static" Enabled ="false"/>
                                    </ItemTemplate>
                                </asp:TemplateField>           
                                    <asp:BoundField HeaderText="Tax Year" DataField="TAX YEAR" />
                                    <asp:BoundField HeaderText="Tax Roll" DataField="Roll Number" />
                                    <asp:BoundField HeaderText="Payor" DataField="Payor" />
                                    <asp:BoundField HeaderText="Date" DataField="Date" DataFormatString="{0:d}" />        
                                    <asp:BoundField HeaderText="Amount" DataField="Amount" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" />
                                        <asp:BoundField HeaderText="Paid" DataField="Paid" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" />
                                        <asp:BoundField HeaderText="Balance" DataField="Balance" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" />
                                    <asp:BoundField HeaderText="Status" DataField="Status" />                                                                                  
                                </Columns>
                            </asp:GridView>
                            <br />
                            <br />
                            <%--<input type="button" value="Commit" id="Button1" runat="server" onserverclick ="ProcessReturnedChecks" />--%>
                            <asp:Button runat="server" Text="Commit" ID="btnCommitCAD" onclick="ProcessCAD" ClientIDMode ="Static" Enabled ="false"/>
                </ContentTemplate>
                </ajaxToolkit:TabPanel>

                <ajaxToolkit:TabPanel  ID="TabPanel5" runat="server" HeaderText="Quick Payments">
                    <ContentTemplate>
                    <table>
                        <tr>
                            <td>
                                <asp:Button ID="btnOpenCashierSession" Text="Open Cashier Session" runat="server" OnClick="QuickPaymentsOpenSession" BackColor ="Yellow" />
                                <br />
                                Scanner:
                                <asp:TextBox ID="txtBarcode" runat="server" Width="120px" ClientIDMode ="Static" ></asp:TextBox>
                            </td>
                            <td>    
                            <table>
                                <tr>
                                <%--<td>
                                    Barcode:
                                </td>
                                <td>                          
                                    <asp:TextBox ID="txtBarcode" runat="server"></asp:TextBox>
                                </td>--%>
                                </tr>
                            </table>                        
                                
                                
                            </td>
                            <td>
                                 Session ID:
                                 <asp:Label ID="lblSessionID" runat="server"></asp:Label>
                                 <br />
                                 Operator:
                                <asp:Label ID="lblOperatorQuickPayments" runat="server"></asp:Label>
                                <br />
                                Opened:
                                <asp:Label ID="lblOpenedQuickPayments" runat="server"></asp:Label>
                            </td>
                            <td>
                            <table>
                                <tr>
                                <td>
                                    Tax Year:
                                    <br />
                                    <br />
                                    Roll:
                                </td>
                                <td>
                                <asp:TextBox ID="txtTaxYearScanned" runat="server" Enabled ="false"></asp:TextBox>                                
                                <br />
                                <asp:TextBox ID="txtTaxRollScanned" runat="server"></asp:TextBox>
                                </td>

                                </tr>
                            </table>
                            </td>
                            <td>
                            <table>
                                <tr>
                                <td>
                                <asp:CheckBox ID="chkPayor" runat="server" Text="Payor"></asp:CheckBox>
                                <br />
                                Tax ID:
                                </td>
                                <td>
                                <asp:TextBox ID="txtQuickPaymentsPayor" runat="server"></asp:TextBox>
                                <br />                         
                                <asp:TextBox ID="txtTaxIDScanned" runat="server"></asp:TextBox>
                                </td>
                                </tr>
                            </table>
                            </td>
                    
                            <td>
                                <asp:Button ID="btnSearchQuickPayment" runat="server" Text="Add Record" OnClick="SearchQuickPayments" Enabled ="false" ClientIDMode ="Static"/>
                            </td>
                        </tr>
                    </table>

                    
                    
                    <br />
                            <asp:GridView ID="grdQuickPayments" runat="server" AutoGenerateColumns="false" Width="100%" >
                                <EmptyDataTemplate>
                                    This table is empty</EmptyDataTemplate>
                                <Columns>        
                                    <asp:BoundField HeaderText="ID" DataField="RECORD_ID" />    
                                    <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                    <asp:BoundField HeaderText="Tax Roll" DataField="Tax_Roll_Number" />      
                                    <asp:BoundField HeaderText="Balance" DataField="Balance" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" />
                                    <asp:BoundField HeaderText="Taxes" DataField="tax_amt" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" />
                                    <asp:BoundField HeaderText="Fees" DataField="Fees" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" />
                                    <asp:BoundField HeaderText="Prior (I)" DataField="PRIOR_MONTH" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" NullDisplayText ="0.00"/>
                                    <asp:BoundField HeaderText="Aged (I)" DataField="Interest" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" />
                                    <asp:BoundField HeaderText="Mod (I)" DataField="Interest" DataFormatString="{0:c}"
                                        ItemStyle-HorizontalAlign="Right" />

                                        <asp:TemplateField>
                                            <HeaderTemplate>                                                 
                                                Payment
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                             <%--   <asp:TextBox ID="txtQuickPayment" runat="server" ClientIDMode="Static"></asp:TextBox>--%>
                                                <input type="text" runat="server"  ID="txtQuickPayment" clientidmode="Static" tabindex ="0"/>
                                            </ItemTemplate>
                                        </asp:TemplateField> 

                                        <asp:TemplateField>
                                            <HeaderTemplate>                                                 
                                                Difference
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:Label ID="lblQuickPaymentRemainder" runat="server" ClientIDMode ="Static"></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField> 

                                        <asp:TemplateField>
                                            <HeaderTemplate>                                                 
                                                <asp:CheckBox ID="chkALLPM" runat="server" Text="PM"/>
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkPM" runat="server" ClientIDMode ="Static" onclick="javascript:checkPM(this);"/>
                                            </ItemTemplate>
                                        </asp:TemplateField> 

                                        <asp:TemplateField>
                                            <HeaderTemplate>
                                                    BI                                                 
                                              <%--  <asp:CheckBox ID="chkALLBI" runat="server" Text="BI" ClientIDMode ="Static" onclick=""/>--%><%--AutoPostBack ="true" OnCheckedChanged ="checkALLBI"--%>
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkBI" runat="server" ClientIDMode ="Static" tabindex ="1" onclick="javascript:checkBI(this);"/>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField>
                                            <HeaderTemplate>                                                 
                                                <asp:CheckBox ID="chkALLFG" runat="server" Text="FGI"/>
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkFG" runat="server" ClientIDMode ="Static" onclick="javascript:checkFG(this);"/>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField>
                                            <ItemTemplate>
                                               <asp:Button ID="btnUpdateQuickPayments" runat="server" Text="GO" BackColor ="ForestGreen"  tabindex ="2" ClientIDMode ="Static"/>
                                               <asp:Button ID="btnDeleteQuickPayment" runat="server" Text="X" BackColor ="Red" ClientIDMode ="Static" />
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <%--<asp:TemplateField>                                           
                                            <ItemTemplate>
                                                <asp:Button ID="btnPM" runat="server" Text="PM" />
                                                <asp:Button ID="Button1" runat="server" Text="BI" />
                                                <asp:Button ID="Button2" runat="server" Text="FGI" />
                                                <asp:Button ID="Button3" runat="server" Text="2/2" />
                                                <asp:Button ID="Button4" runat="server" Text="GO" />
                                            </ItemTemplate>
                                        </asp:TemplateField> --%>
                                                                                                                 
                                </Columns>
                            </asp:GridView>
                        <br />
                        <br />
                        <br />
                    <td>
                        <asp:Button ID="btnCloseCashierSession" Text="Close Cashier Session" runat="server" ClientIDMode ="Static" BackColor ="Orange" OnClick ="QuickPaymentsCloseSession" Enabled ="false"/>
                    </td>
                    <td>
                       
                        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                        &nbsp;&nbsp;&nbsp;
                    </td>
                   

                    <td>
                        <asp:Label ID="lblrunningtotal" runat="server" Text="Running Total:" ></asp:Label>
                        <asp:TextBox ID="txtRunningTotal" runat="server" ClientIDMode="Static"></asp:TextBox>
                    </td>

                        
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
            </ajaxToolkit:TabContainer>
        </div>

        <!-- Foreclosures tab -->
        <%--<div id="tabForeclosures">
            <div>
                <fieldset>
                    <asp:Label ID="lblNoForeclosuresData" runat="server" Text="No data has been loaded yet." />
                    <asp:GridView ID="grdForeclosures" runat="server" AutoGenerateColumns="False">
                        <Columns>
                            <asp:BoundField HeaderText="Parcel" DataField="APN" ItemStyle-HorizontalAlign="Center">
                            </asp:BoundField>
                            <asp:BoundField HeaderText="Investor" DataField="INVESTORID" ItemStyle-HorizontalAlign="Center">
                            </asp:BoundField>
                            <asp:BoundField HeaderText="Initiated" DataField="INITIATED" ItemStyle-HorizontalAlign="Center"
                                DataFormatString="{0:d}"></asp:BoundField>
                            <asp:BoundField HeaderText="Completed" DataField="COMPLETED" ItemStyle-HorizontalAlign="Center"
                                DataFormatString="{0:d}"></asp:BoundField>
                            <asp:BoundField HeaderText="Deed Type" DataField="DEEDTYPE" ItemStyle-HorizontalAlign="Center">
                            </asp:BoundField>
                            <asp:BoundField HeaderText="Cancelled" DataField="CANCELLED" ItemStyle-HorizontalAlign="Center">
                            </asp:BoundField>
                            <asp:TemplateField HeaderText="View CP">
                                <ItemTemplate>
                                    <asp:Button ID="btnViewCP" runat="server" Text="View CP" ClientIDMode="Static" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Print Deed">
                                <ItemTemplate>
                                    <asp:Button ID="btnPrintDeed" runat="server" Text="Print Deed" Enabled="false" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </fieldset>
            </div>
        </div>--%>

        <!-- Functions tab -->
        <div id="tabFunctions">
               
        </div>

    </div>
    </form>
</body>
</html>
