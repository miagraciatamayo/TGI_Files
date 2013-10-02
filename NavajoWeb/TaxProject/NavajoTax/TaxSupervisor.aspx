<%@ Page Language="VB" AutoEventWireup="false" CodeFile="TaxSupervisor.aspx.vb" Inherits="TaxSupervisor"
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
                            window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
                            break;
                        case 1:
                            window.location.href = "TaxSupervisor.aspx#tabFunctions";
                            break;
                        case 3:
                            window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
                            break;
                        case 4:
                            window.location.href = "MaintenanceTasks.aspx#tabFunctions2";
                            break;
                        case 2:
                            window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
                            break;
                            
                    }
                }
            //}).tabs("select", window.location.hash);
            }).tabs("select", (window.location.hash ? window.location.hash : 0));

            // Form submit
            $("form").submit(function () {
                var action = document.getElementById("form1").action;
                if (action.lastIndexOf("#") >= 0) {
                    action = action.substr(0, action.lastIndexOf("#"));
                }
                document.getElementById("form1").action = action + window.location.hash;
            });


            // Apply style to all buttons
            $("#btnPostLoadSession").button();
            $("#btnLoadRefund").button();
            $("#btnSaveRefund").button();
            $("#btnLPSLoad").button();
            $("[id*=btnPrintCheck]").button();
            $("[id*=btnPost]").button();
            $("#btnLettersSave").button();
            $("[id*=btnViewCP]").button();
            $("[id*=btnPrintDeed]").button();
            $("#btnNightlyFunc").button();
            $("#btnCaptureLevy").button();
            $("#btnAgeInterest").button();
            $("#btnUpdateWebVals").button();
            $("#btnLoadForeclosures").button();
            $("#btnSendUnsecured").button();
            $("#btnDailyLetters").button();
            $("#btnCommitCAD").button();
            $("#btnSearchMatch").button();
            $("#btnSearchPartial").button();
            $("#btnSearchRefund").button();
            $("#btnPrintDailyLetters").button();
            $("#btnPrintDailyLabels").button();
            $("#btnClearDailyLetters").button();
            $("#btnSearchReturnedChecks").button();
            $("#btnReturnChecksCommit").button();
            $("#btnPrintRefundLetters").button();

            

            // Set focus to btnPosLoadPosting
            $("#btnPosLoadPosting").button().focus();


            // Posting.
            $("#txtPosDate").datepicker().focus(function (event) {
                $("#rdoPosDate").attr("checked", "checked");
            });


            // Deposits.
            $("[name=rdoDepositSelect]").change(function () {
                if ($(this).is(":checked")) {
                    $("[id$=hdnPosSessionID]").val($(this).attr("value"));
                }
            }).filter("[value=" + $("[id$=hdnPosSessionID]").val() + "]").attr("checked", "checked");


            $("#chkLetterApprovedAll").change(function () {
                if ($(this).is(":checked")) {
                    $("[id$=chkLetterApproved]").attr("checked", "checked");
                }
                else {
                    $("[id$=chkLetterApproved]").removeAttr("checked");
                }
            });


            // btnViewCP Click Event
            $("[id$=btnViewCP]").click(function (event, ui) {
                // $("#divViewCP").dialog("open");
                showViewCPPopup("View CP");

                event.preventDefault();
            });

            // btnPost Click Event
            $("[id$=btnPost]").click(function (event, ui) {
                // clickNum++;
                // alert($("#lblCashier").val());
                showViewPostDetails("Post Cashier Session");

                event.preventDefault();
            });



            // btnPost Click Event
            $("[id$=btnNightlyFunc]").click(function (event, ui) {
                // clickNum++;
                // alert($("#lblCashier").val());
                showViewNightlyActions("Run Nightly Update Actions");

                event.preventDefault();
            });

            // btnPost Click Event
            $("[id$=btnCaptureLevy]").click(function (event, ui) {
                // clickNum++;
                // alert($("#lblCashier").val());
                showCaptureLevyActions("End-of-Month Levy Capture");

                event.preventDefault();
            });

            $("[id$=btnAgeInterest]").click(function (event, ui) {
                // clickNum++;
                // alert($("#lblCashier").val());
                showAgeInterestActions("Age Interest Process");

                event.preventDefault();
            });

            $("[id$=btnUpdateWebVals]").click(function (event, ui) {
                // clickNum++;
                // alert($("#lblCashier").val());
                showUpdateWebValsActions("Update Web Values");

                event.preventDefault();
            });


            $("[id$=btnLoadForeclosures]").click(function (event, ui) {
                // clickNum++;
                // alert($("#lblCashier").val());
                showLoadForeclosuresActions("Load Foreclosures");

                event.preventDefault();
            });

            $("[id$=btnSendUnsecured]").click(function (event, ui) {
                // clickNum++;
                // alert($("#lblCashier").val());
                showSendUnsecuredActions("Send Unsecured Delinquent Account to the Sheriff");

                event.preventDefault();
            });

//            $("#txtTaxIDSearch").click(function () {
//                //  alert("11");
//                $("#radioTaxIDSearch").attr("checked", "checked");
//            });

//            $("#txtPayorSearch").click(function () {
//                //    alert("22");
//                $("#radioPayorSearch").attr("checked", "checked");
//            });

//            $("#txtCheckNumberSearch").click(function () {
//                //   alert("33");
//                $("#radioCheckNumberSearch").attr("checked", "checked");
//            });



        });                      // End of document.ready function


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

    

        //btnDeleteTransaction
        $("[id$=btnDeleteTransaction]").click(function (event, ui) {

            document.getElementById('<%= hdnDeleteDivRecordID.ClientID %>').value = transID;
            $('#<%= hdnDeleteDivRecordID.ClientID %>').val(transID);
            // alert(document.getElementById('<%= hdnDeleteDivRecordID.ClientID %>').value);

            document.getElementById("lblDeleteDivRecordID").value = transID
            document.getElementById("inputHidden").value = transID
            $('#inputHidden').val(transID);

          

        });



        //btnDeleteTransaction
        $("[id$=btnContinueRunNightlyFunc]").click(function (event, ui) {
            showLoadingBox()
            $.ajax({
                type: "POST",
                url: "TaxSupervisor.aspx/btnUpdateNightlyFunction_click",
                // data: '{"transID":"' + transID + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                  
                }
            });
           

            $("#divLoading").dialog("destroy");
            alert("Nightly Function Run Done");
            window.open('TaxSupervisor.aspx#tabFunctions', '_self');
            return false;

        });

        $("[id$=btnContinueCaptureLevy]").click(function (event, ui) {
            showLoadingBox()
            $.ajax({
                type: "POST",
                url: "TaxSupervisor.aspx/btnCaptureLevy_click",
                // data: '{"transID":"' + transID + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                  
                }
            });
            alert("Capture Levy Done");
            $("#divLoading").dialog("destroy");
            window.open('TaxSupervisor.aspx#tabFunctions', '_self');
            return false;

        });

        //btnCommitForeclosures
        $("[id$=btnCommitForeclosures]").click(function (event, ui) {
            showLoadingBox()
            $.ajax({
                type: "POST",
                url: "TaxSupervisor.aspx/btnCommitForeclosures_click",
                // data: '{"transID":"' + transID + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                    
                }
            });
            alert("Foreclosures now loaded");
            $("#divLoading").dialog("destroy");
            window.open('TaxSupervisor.aspx#tabFunctions', '_self');
            return false;
        });


        $("[id$=btnCommitSendUnsecured]").click(function (event, ui) {
            showLoadingBox()
            $.ajax({
                type: "POST",
                url: "TaxSupervisor.aspx/btnCommitSendUnsecured_click",
                // data: '{"transID":"' + transID + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                  
                }
            });

            $("#divLoading").dialog("destroy");
            alert("Send Unsecured to Sheriff Done.");
            window.open('TaxSupervisor.aspx#tabFunctions', '_self');
            return false;
        });

        //btnDeleteTransaction
        $("[id$=btnContinueUpdateWebValues]").click(function (event, ui) {
            showLoadingBox()
            $.ajax({
                type: "POST",
                url: "TaxSupervisor.aspx/btnUpdateWebValues_click",
                // data: '{"transID":"' + transID + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                }
            });
            alert("Web Values now updated");
            $("#divLoading").dialog("destroy");
            //  window.location = "TaxSupervisor.aspx#tabFunctions";

            window.open('TaxSupervisor.aspx#tabFunctions', '_self');
            return false;
        });



        $("[id$=btnDeleteTrans]").click(function (event, ui) {
            $("#<%=grdPosTransactions.ClientID %> tr").click(function (event) {
                //Skip first(header) row
                // if (!this.rowIndex) return;
                var idx = this.rowIndex;

                var transID = document.getElementById("<%=grdPosTransactions.ClientID %>").rows[idx].cells[0].innerHTML;
                var grpKey = document.getElementById("<%=grdPosTransactions.ClientID %>").rows[idx].cells[1].innerHTML;
                var payorName = document.getElementById("<%=grdPosTransactions.ClientID %>").rows[idx].cells[7].innerHTML;
                var amount = document.getElementById("<%=grdPosTransactions.ClientID %>").rows[idx].cells[9].innerHTML;
               
                $("#lblDeleteDivRecordID").html(transID);
                $("#lblDeleteDivPayorName").html(payorName);
                $("#lblDeleteDivAmount").html(amount);
                $("#lblGroupKey").html(grpKey);

                document.getElementById("inputHidden").value = transID


               

            });

            showDeleteTrans("Delete Transaction");
            event.preventDefault();
        });

        $("[id$=btnReverseTrans]").click(function (event, ui) {
            $("#<%=grdPosTransactionsReverse.ClientID %> tr").click(function (event) {
                //Skip first(header) row
                // if (!this.rowIndex) return;
                var idx = this.rowIndex;

                var transID = document.getElementById("<%=grdPosTransactionsReverse.ClientID %>").rows[idx].cells[0].innerHTML;
                var grpKey = document.getElementById("<%=grdPosTransactionsReverse.ClientID %>").rows[idx].cells[1].innerHTML;
                var payorName = document.getElementById("<%=grdPosTransactionsReverse.ClientID %>").rows[idx].cells[6].innerHTML;
                var amount = document.getElementById("<%=grdPosTransactionsReverse.ClientID %>").rows[idx].cells[8].innerHTML;
                

                $("#lblDeleteDivRecordID2").html(transID);
                $("#lblDeleteDivPayorName2").html(payorName);
                $("#lblDeleteDivAmount2").html(amount);
                $("#lblGroupKey2").html(grpKey);

                document.getElementById("inputHidden2").value = transID


                

            });

            showReverseTrans("Reverse Transaction");
            event.preventDefault();
        });



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


       

    });

    function checkRDOValueSearch(rdoValue) {

        if (rdoValue == "radioTaxIDSearch") {
          
            $("#radioTaxIDSearch").attr("checked",true);
         
            $("#txtTaxIDSearch").css("background-color", "");
          
            $("#txtPayorSearch").css("background-color", "gray");
         
            $("#txtCheckNumberSearch").css("background-color", "gray");
          

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
    function showViewPostDetails(title) {

        $("#divViewPostDetails").dialog({
            modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
        // 
    }

    function showViewNightlyActions(title) {

        $("#divNightlyFunc").dialog({
           modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                $(this).dialog("hide");
            }
        });
        // 
    }

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

    function showCaptureLevyActions(title) {

        $("#divCaptureLevy").dialog({
            modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
        // 
    }

    function showAgeInterestActions(title) {

        $("#divAgeInterest").dialog({
            modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
        // 
    }

    //
    function showUpdateWebValsActions(title) {

        $("#divUpdateWebValues").dialog({
            modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                 $(this).dialog("destroy");
               //  $(this).dialog("close");
               // $(this).hide();
            }
        });
        // 
    }

    function showLoadForeclosuresActions(title) {

        $("#divLoadForeclosures").dialog({
            modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
        // 
    }
    //showSendUnsecuredActions
    function showSendUnsecuredActions(title) {

        $("#divSendUnsecured").dialog({
            modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
        // 
    }

    function showViewForeclosuresActions() {
        showForeclosures()
    }


    function showViewSendUnsecuredActions() {
 
        showSendUnsecured()
       
    }

    function showViewReturnedChecksActions() {

     //   $find('<%=tabPosTab.ClientID%>').set_activeTabIndex(1);
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

    function showSendUnsecured() {
        $("#divViewSendUnsecured").dialog({
            modal: true,
            title: "Send Unsecured to Sheriff",
            width: 1500,
            height: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
    }

    function showForeclosures() {
        $("#divViewForeclosures").dialog({
            modal: true,
            title: "View Foreclosures",
            width: 1500,
            height: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
    }

    //
    function showDeleteTrans(title) {
 
        $("#divDeleteTrans").dialog({
            modal: true,
            title: title,
            width: 500,
            close: function (e, ui) {
                $(this).dialog("destroy");
            }
        });
        // 
    }

    function showReverseTrans(title) {

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
        var transID = $("#lblDeleteDivRecordID2").html(); // document.getElementById("lblDeleteDivRecordID").value
        var grpKey = $("#lblGroupKey2").html();
      //  var sessID = document.getElementById("<%=grdPosDeposits.ClientID %>").rows[idx].cells[1].innerHTML;
      //  var transID = document.getElementById("<%=grdPosTransactionsReverse.ClientID %>").rows[idx].cells[0].innerHTML;
        $.ajax({
            type: "POST",
            url: "TaxSupervisor.aspx/btnReverseTransaction_Click",
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



        function showLoginDialog() {
            $("#divLogin").dialog({
                modal: true
            }).parent().appendTo("form");
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

    <div id="divDeleteTrans" class="divPopup" runat="server">
    Transaction:
    <asp:Label ID="lblDeleteDivRecordID" runat ="server"></asp:Label><br />
    <asp:HiddenField ID="hdnDeleteDivRecordID" runat="server"/>
    <input type="hidden" id="inputHidden" runat="server" />
    Group Key:
    <asp:Label ID="lblGroupKey" runat ="server"></asp:Label><br />
    Payor:
    <asp:Label ID="lblDeleteDivPayorName" runat ="server"></asp:Label><br />
    Amount:
    <asp:Label ID="lblDeleteDivAmount" runat ="server"></asp:Label><br /><br />

        This transaction has not been posted, and the following actions will be taken upon commit: 
        <br />
        <br />
            (1) All transactions tied by the Group Key will be retained, but marked as deleted.<br />
            (2) All associated apportion records will be deleted.<br />
            (3) All associated payment records will be deleted.
        <br />
        <br />

        <asp:LinkButton runat="server" Text="Delete" ID="btnDeleteTransaction"  onClientclick="javascript:deletePopupInfo();alert('Transaction successfully deleted.');" ForeColor="Red"  />&nbsp;&nbsp;
     <%--   <asp:LinkButton runat="server" ID="LinkButton2" Text="Cancel" OnClientClick="$('#divDeleteTrans').dialog('close')" ForeColor="Red"  />--%>
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

        <asp:LinkButton runat="server" Text="Reverse" ID="LinkButton1"  onClientclick="javascript:reversePopupInfo();alert('Transaction successfully reversed.');" ForeColor="Red"  />&nbsp;&nbsp;
       <%-- <asp:LinkButton runat="server" ID="LinkButton8" Text="Cancel" OnClientClick="$('#divReverseTrans').dialog('close')" ForeColor="Red"  />--%>

    </div>

    <div id="divViewPostDetails" class="divPopup" runat="server">
        Session: 
        <asp:Label ID="lblSession" runat ="server"></asp:Label><br />
        Operator: 
        <asp:Label ID="lblOperator" runat ="server"></asp:Label><br />
        Deposit Opened:
        <asp:Label ID="lblDepositOpened" runat ="server"></asp:Label><br />
            Deposit Closed: 
            <asp:Label ID="lblDepositClosed" runat ="server"></asp:Label><br />
            Transactions: 
            <asp:Label ID="lblTransactions" runat ="server"></asp:Label><br />
            Amount: 
            <asp:Label ID="lblAmount" runat ="server"></asp:Label><br />
         <!--   <asp:Label ID="lblReceiptNumber" runat ="server" Visible ="false"></asp:Label>-->
            <br />
            <br />
            Deposit Parameters:
            <asp:DropDownList runat ="server">
            <asp:ListItem Value="CFB">CFB</asp:ListItem>
            </asp:DropDownList>
            <br />
            <br />
            <br />
            &nbsp;&nbsp;&nbsp;
            <asp:LinkButton runat="server" Text="View Deposit" ID="btnViewDeposit" ForeColor="Red"/>&nbsp;&nbsp;
            <asp:LinkButton runat="server" Text="Print Deposit Ticket" ID="btnPrintDeposit" ForeColor="Red"  />&nbsp;&nbsp;
            <asp:LinkButton runat="server" ID="btnCommitDeposit" Text="Commit Deposit" ForeColor="Red"  />
            
    </div>

    <div id="divNightlyFunc"  class="divPopup" runat="server">
            <br />
            The following actions will be taken:
            <br />
            <br />
            (1) All Tax Roll Current Values will be set to zero.
            <br />
            (2) All Tax Rolls with a balance will be loaded with their current balance.
            <br />
            (3) The Status value will be updated for all Tax Rolls
            <br />
            &nbsp;&nbsp;&nbsp;&nbsp; 1: Unpaid
            <br />
            &nbsp;&nbsp;&nbsp;&nbsp; 2: Paid in Full
            <br />
            &nbsp;&nbsp;&nbsp;&nbsp; 3: Paid by Investor
            <br />
            &nbsp;&nbsp;&nbsp;&nbsp; 4: State CP
            <br />
            &nbsp;&nbsp;&nbsp;&nbsp; 5: Redeemed
            <br />
            &nbsp;&nbsp;&nbsp;&nbsp; 6: Deeded
            <br />
            &nbsp;&nbsp;&nbsp;&nbsp; 7: Charged Off
            <br />
            (4) The Tax Account Bankruptcy Flag will be reset.
            <br />
            (5) All Tax Account balances will be set to zero.
            <br />
            (6) The Tax Account balance wil be loaded with the sum of <br /> all associated roll balances.
            <br />
            (7) The Tax Account Parent Balance Flag will be reset.
            <br />
            <br />
            <br />
  <%--  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;--%>
            <asp:LinkButton runat="server" Text="Continue" ID="btnContinueRunNightlyFunc" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;
       <%--     <asp:LinkButton runat="server" Text="Cancel" ID="btnCancel" ForeColor="Red" OnClientClick ="window.open('TaxSupervisor.aspx#tabFunctions', '_self');return false;"/>&nbsp;&nbsp; --%> <%--OnClientClick="$(this).dialog('destroy');" --%>
            <%--<a href="" onclick="window.location('TaxSupervisor.aspx#tabFunctions');">Go</a>--%>

    </div>

     <div id="divCaptureLevy"  class="divPopup" runat="server">
    <br />
    The following actions will be taken:
    <br />
    <br />
    (1) The current values of Levy amount and Collections-to-date as contained in vLevyCapture will be inserted into the Levy Totals table.
    <br />
    (2) Note vLevyCapture view is live against the current date. This means a current levy can only be pulled at the close-of-bussiness for the  desired end-of-month. Valid figures cannot be pulled after the calendar has moved to the next month.  
    <br />
    <br />
    <br />
  <%--  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;--%>
            <asp:LinkButton runat="server" Text="Continue" ID="btnContinueCaptureLevy" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;
         <%--   <asp:LinkButton runat="server" Text="Cancel" ID="LinkButton3" OnClientClick="$('#divCaptureLevy').dialog('destroy')" ForeColor="Red"  />&nbsp;&nbsp;--%>
    </div>

     <div id="divAgeInterest"  class="divPopup" runat="server">
    <br />
    The following actions will be taken:
    <br />
    <br />
    (1) The current tax roll balance will be updated.
    <br />
    (2) Appropriate delinquent interest records(99901) will be deleted for tax rolls with a remaining balance.
    <br />
    (3) A new Aging History record will be created.
    <br />
    (4) An interest value for each tax roll balance greater than zero will be calculated and entered into Tax Roll Aged Interest table.
    <br />
    (5) The Aging History record will be closed out with the completion of the aging calculations.
    <br />
    (6) Upon approval, aged interest values will be inserted into the Tax Charges table.
    <br />
    <br />
    <br />
    The following input parameters are required:
    <br />
    Aging Type: [Full Roll | Current Year |V]
    <br />
    Aging Date: [default current date}
    <br />
    <br />
    <br />
  <%--  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;--%>
            <asp:LinkButton runat="server" Text="Continue" ID="LinkButton4" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;
  <%--          <asp:LinkButton runat="server" Text="Cancel" ID="LinkButton5" OnClientClick="$('#divAgeInterest').dialog('close')" ForeColor="Red"  />&nbsp;&nbsp;--%>

    </div>


     <div id="divUpdateWebValues"  class="divPopup" runat="server">
    <br />
    The following actions will be taken:
    <br />
    <br />
    (1) The current Web Values (ST_WEB_VALUES) table will be truncated to zero records.
    <br />
    (2) The values in vWebValues will be inserted into the truncated Web Values table.
    <br />
    (3) Zero balance tax rolls greater than seven years of age will be dropped.
    <br />
    <br />
    <br />
  <%--  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;--%>
            <asp:LinkButton runat="server" Text="Continue" ID="btnContinueUpdateWebValues" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;
        <%--    <asp:LinkButton runat="server" Text="Cancel" ID="LinkButton7" OnClientClick="$('#divUpdateWebValues').dialog('hide')" ForeColor="Red"  />&nbsp;&nbsp; --%><%-- --%>
    </div>


    <div id="divLoadForeclosures"  class="divPopup" runat="server">
    <br />
    The following actions will be taken:
    <br />
    <br />
    (1) Press Review records. A popup with the foreclosure candidates will appear.
    <br />
    (2) Review the list.
    <br />
    (3) Press append to load the candidates into the deeding database
    <br />
    <br />
    <br />
  <%--  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;--%>
            <asp:LinkButton runat="server" Text="Continue" ID="btnViewForeclosures" OnClick="ViewForeclosures" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;
          <%--  <asp:LinkButton runat="server" Text="Cancel" ID="LinkButton9" OnClientClick="$('#divLoadForeclosures').dialog('close')" ForeColor="Red"  />&nbsp;&nbsp;--%>
    </div>

    <div id="divViewForeclosures" class="divPopup" runat="server" style="height:100px">
    <asp:Panel runat="server">
    
    <asp:GridView ID="grdViewForeclosures" runat="server" AutoGenerateColumns="false" Width="100%">
            <EmptyDataTemplate>
                This table is empty</EmptyDataTemplate>
            <Columns>
                <asp:BoundField HeaderText="Tax Year" DataField="TaxYear" />
                <asp:BoundField HeaderText="APN" DataField="APN" />
                <asp:BoundField HeaderText="Owner Name" DataField="OWNER_NAME" />
                <asp:BoundField HeaderText="Owner Address" DataField="OWNER_ADDRESS" />
                <asp:BoundField HeaderText="Mail Address" DataField="MAIL_ADDRESS" />
                <asp:BoundField HeaderText="Mail City" DataField="MAIL_CITY" />
                <asp:BoundField HeaderText="Mail State" DataField="MAIL_STATE" />
                <asp:BoundField HeaderText="Mail Zip" DataField="MAIL_ZIP" />
            </Columns>
        </asp:GridView>
        <br />
        <br />

        <asp:LinkButton runat="server" Text="Commit" ID="btnCommitForeclosures" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;
       <%-- <asp:LinkButton runat="server" Text="Cancel" ID="LinkButton10" OnClientClick="$('#divViewForeclosures').dialog('close')" ForeColor="Red"  />&nbsp;&nbsp;--%>
    </asp:Panel>
    
    </div>

    <div id="divSendUnsecured"  class="divPopup" runat="server">
    <br />
    The following actions will be taken:
    <br />
    <br />
    (1) This process will mark delinquent unsecured property for Sheriff collection.
    <br />
    (2) Any property with a positive balance and not already posted to the Sheriff will be so posted.
    <br />
    <br />
    <br />
  <%--  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;--%>
            <asp:LinkButton runat="server" Text="Continue" ID="btnContinueSendUnsecured" OnClick="ViewSendUnsecured" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;
           <%-- <asp:LinkButton runat="server" Text="Cancel" ID="LinkButton6" OnClientClick="$('#divSendUnsecured').dialog('close')" ForeColor="Red"  />&nbsp;&nbsp;--%>
    </div>

    <div id="divViewSendUnsecured" class="divPopup" runat="server" style="height:100px">
    <asp:Panel ID="Panel1" runat="server">
    
    <asp:GridView ID="grdViewSendUnsecured" runat="server" AutoGenerateColumns="false" Width="100%">
            <EmptyDataTemplate>
                This table is empty</EmptyDataTemplate>
            <Columns>
                <asp:BoundField HeaderText="Tax ID" DataField="ParcelOrTaxID" />
                <asp:BoundField HeaderText="APN" DataField="APN" />
                <asp:BoundField HeaderText="SecuredUnsecured" DataField="SecuredUnsecured" />
                <asp:BoundField HeaderText="Account Status" DataField="ACCOUNT_STATUS" />
                <asp:BoundField HeaderText="Account Alert" DataField="ACCOUNT_ALERT" />
                <asp:BoundField HeaderText="Account Bankruptcy" DataField="ACCOUNT_BANKRUPTCY" />
    
            </Columns>
        </asp:GridView>
        <br />
        <br />

        <asp:LinkButton runat="server" Text="Commit" ID="btnCommitSendUnsecured" ForeColor="Red"/>&nbsp;&nbsp;&nbsp;&nbsp;
        <%--<asp:LinkButton runat="server" Text="Cancel" ID="LinkButton11" OnClientClick="$('#divViewSendUnsecured').dialog('close')" ForeColor="Red"  />&nbsp;&nbsp;--%>
    </asp:Panel>
    
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
            <asp:RadioButton ID="rdoPosNotPosted" runat="server" GroupName="rdoPosting" Text="Not Posted"
                Checked="true" />
            <asp:RadioButton ID="rdoPosDate" runat="server" GroupName="rdoPosting" Text="Date" />
            <asp:TextBox ID="txtPosDate" runat="server"></asp:TextBox>
            <asp:Button ID="btnPosLoadPosting" runat="server" Text="Load" />
            <asp:HiddenField ID="hdnTextValueTaxID" runat="server"/>
            <asp:HiddenField ID="hdnTextValuePayor" runat="server"/>
            <asp:HiddenField ID="hdnTextValueCheckNum" runat="server"/>
<%--            <input type="hidden" name="hdnTxtValue" runat="server" value="" />--%>
            <br />
            <br />
            <ajaxToolkit:TabContainer ID="tabPosTab" runat="server" ActiveTabIndex="0">
                <ajaxToolkit:TabPanel ID="tabPosDeposits" runat="server" HeaderText="Deposits">
                    <ContentTemplate>
                        <div>
                            <fieldset>
                                <asp:Label ID="lblNoDepositData" runat="server" Text="No Deposit data has been loaded yet." />
                                <asp:GridView ID="grdPosDeposits" runat="server" Width="100%" AutoGenerateColumns="False">
                                    <Columns>
                                        <asp:TemplateField HeaderText="Select">
                                            <ItemTemplate>
                                                <input type="radio" id="rdoDepositSelect" name="rdoDepositSelect" value='<%#DataBinder.Eval(Container.DataItem, "RECORD_ID") %>' />
                                                
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Session ID" DataField="RECORD_ID" />
                                        <asp:TemplateField>
                                            <HeaderTemplate>
                                                Cashier<br />
                                                Opened<br />
                                                Closed
                                            </HeaderTemplate>
                                            <ItemTemplate>                                            
                                                <%#DataBinder.Eval(Container.DataItem, "CASHIER")%>
                                                <br />
                                                <%#DataBinder.Eval(Container.DataItem, "START_TIME", "{0:M/d/yyyy hhmm tt}")%>
                                                <br />
                                                <%#DataBinder.Eval(Container.DataItem, "END_TIME", "{0:M/d/yyyy hhmm tt}")%>
                                            </ItemTemplate>
                                        </asp:TemplateField>                                        
                                        <asp:BoundField HeaderText="Deposit Number" DataField="RECEIPT_NUMBER" />
                                        <asp:BoundField HeaderText="Transactions" DataField="TRANS_COUNT" />
                                        <asp:BoundField HeaderText="Total Deposit" DataField="PAYMENT_AMT" DataFormatString="{0:c}" />
                                        <asp:BoundField HeaderText="Tax Amount" DataField="TAX_AMT" DataFormatString="{0:c}" />
                                        <asp:BoundField HeaderText="Apportioned" DataField="APPORTIONED" DataFormatString="{0:c}" />
                                        <asp:BoundField HeaderText="Refund" DataField="REFUND_AMT" DataFormatString="{0:c}" />
                                        <asp:BoundField HeaderText="Kitty" DataField="KITTY_AMT" DataFormatString="{0:c}" />
                                        <asp:BoundField HeaderText="Write-Off" DataField="WRITE_OFF" DataFormatString="{0:c}" />
                                        <asp:BoundField HeaderText="Difference" DataField="DIFFERENCE" DataFormatString="{0:c}" />
                                                                          
                                    </Columns>
                                </asp:GridView>
                            </fieldset>
                        </div>
  
                        <div>
                            <br />
                            <asp:HiddenField ID="hdnPosSessionID" runat="server" />
                            <asp:Button ID="btnPostLoadSession" runat="server" Text="Load Details" ClientIDMode="Static"/>

                            <asp:Button ID="btnPost" runat="server" Text="Post Deposit" ClientIDMode="Static" />
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabPosTransactions" runat="server" HeaderText="Transactions">
                    <ContentTemplate>
                        <div>
                            <fieldset>
                                <asp:Label ID="lblNoPosTransactionData" runat="server" Text="No data has been loaded yet." />
                               
                                <asp:GridView ID="grdPosTransactions" runat="server" AutoGenerateColumns="false"
                                    Width="100%">
                                    <EmptyDataTemplate>
                                        This table is empty</EmptyDataTemplate>
                                    <Columns>
                                        <asp:BoundField HeaderText="Transaction" DataField="RECORD_ID" />
                                        <asp:BoundField HeaderText="Group Key" DataField="GROUP_KEY" NullDisplayText ="" />
                                        <asp:BoundField HeaderText="Status" DataField="TRANS_STATUS" NullDisplayText =""/>
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
                                        <asp:TemplateField HeaderText="Delete">
                                            <ItemTemplate >                                            
                                                <asp:Button ID="btnDeleteTrans" Text="Delete" runat="server"
                                                CommandName ="DeleteTransaction" ClientIDMode ="Static" Enabled='<%# (Not Eval("Transaction_status")=3 OR Not Eval("Transaction_status")=3)%>'/>
                                               
                                            </ItemTemplate>
                                        </asp:TemplateField>                                        
                                    </Columns>
                                </asp:GridView>

                                <asp:GridView ID="grdPosTransactionsReverse" runat="server" AutoGenerateColumns="false"
                                    Width="100%">
                                    <EmptyDataTemplate>
                                        This table is empty</EmptyDataTemplate>
                                    <Columns>
                                        <asp:BoundField HeaderText="Transaction" DataField="RECORD_ID" />
                                        <asp:BoundField HeaderText="Group Key" DataField="GROUP_KEY" NullDisplayText ="" />
                                        <asp:BoundField HeaderText="Status" DataField="TRANS_STATUS" NullDisplayText ="" />
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
                                        <asp:TemplateField HeaderText="Reverse">
                                            <ItemTemplate >                                            
                                                <asp:Button ID="btnReverseTrans" Text="Reverse" runat="server"
                                                CommandName ="ReverseTransaction"  CommandArgument ="<%#Container.DataItemIndex%>" ClientIDMode ="Static"  Enabled='<%# (Not Eval("Transaction_status")=4)%>'/>
                                               
                                            </ItemTemplate>
                                        </asp:TemplateField>                                        
                                    </Columns>
                                </asp:GridView>
                            </fieldset>
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabPosApportionments" runat="server" HeaderText="Apportionments">
                    <ContentTemplate>
                        <div>
                            <fieldset>
                                <asp:Label ID="lblNoPosApportionmentData" runat="server" Text="No data has been loaded yet." />
                                <asp:GridView ID="grdPosApportionments" runat="server" AutoGenerateColumns="false">
                                    <EmptyDataTemplate>
                                        This table is empty</EmptyDataTemplate>
                                    <Columns>
                                        <asp:BoundField HeaderText="Tax Year" DataField="TaxYear" />
                                        <asp:BoundField HeaderText="Tax Roll" DataField="TaxRollNumber" />
                                        <asp:TemplateField HeaderText="Area Code">
                                            <ItemTemplate>
                                                <%#DataBinder.Eval(Container.DataItem, "AreaCode")%>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Charge Code">
                                            <ItemTemplate>
                                                <%#GetChargeCode(DataBinder.Eval(Container.DataItem, "TaxChargeCodeID"))%>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Tax Type">
                                            <ItemTemplate>
                                                <%#GetTaxType(DataBinder.Eval(Container.DataItem, "TaxTypeID"))%>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Payment Date" DataField="PaymentDate" DataFormatString="{0:d}" />
                                        <asp:BoundField HeaderText="GL Account" DataField="GLAccount" />
                                        <asp:BoundField HeaderText="Date Apportioned" DataField="DateApportioned" DataFormatString="{0:d}" />
                                        <asp:BoundField HeaderText="Apportioned Amount" DataField="DollarAmount" DataFormatString="{0:C}"
                                            ItemStyle-HorizontalAlign="Right" />
                                    </Columns>
                                </asp:GridView>
                            </fieldset>
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabPosRefunds" runat="server" HeaderText="Refunds">
                    <ContentTemplate>
                        <div>
                            <fieldset>
                                <asp:Label ID="lblNoPosRefundData" runat="server" Text="No data has been loaded yet." />
                                <asp:GridView ID="grdPosRefunds" runat="server" AutoGenerateColumns="false" Width="100%">
                                    <EmptyDataTemplate>
                                        This table is empty</EmptyDataTemplate>
                                    <Columns>
                                        <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                        <asp:BoundField HeaderText="Tax Roll" DataField="TAX_ROLL_NUMBER" />
                                        <asp:BoundField HeaderText="Amount" DataField="REFUND_AMT" DataFormatString="{0:c}"
                                            ItemStyle-HorizontalAlign="Right" />
                                    </Columns>
                                </asp:GridView>
                            </fieldset>
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabPosKitty" runat="server" HeaderText="Kitty">
                    <ContentTemplate>
                        <div>
                            <fieldset>
                                <asp:Label ID="lblNoPosKittyData" runat="server" Text="No data has been loaded yet." />
                                <asp:GridView ID="grdPosKittyFunds" runat="server" AutoGenerateColumns="false" Width="100%">
                                    <EmptyDataTemplate>
                                        This table is empty</EmptyDataTemplate>
                                    <Columns>
                                        <asp:BoundField HeaderText="Tax Year" DataField="TAX_YEAR" />
                                        <asp:BoundField HeaderText="Tax Roll" DataField="TAX_ROLL_NUMBER" />
                                        <asp:BoundField HeaderText="Amount" DataField="KITTY_AMT" DataFormatString="{0:c}"
                                            ItemStyle-HorizontalAlign="Right" />
                                    </Columns>
                                </asp:GridView>
                            </fieldset>
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabPosDeclined" runat="server" HeaderText="Declined Payments">
                    <ContentTemplate>
                        <div>
                            <fieldset>
                                <asp:Label ID="lblNoPosDeclinedData" runat="server" Text="No data has been loaded yet." />
                                <asp:GridView ID="grdPosDeclinedPayments" runat="server" AutoGenerateColumns="false"
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
                            </fieldset>
                        </div>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
            </ajaxToolkit:TabContainer>
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
            Tax Year:
            <asp:DropDownList ID="ddlSalePrepYear" runat="server" />
            <asp:Button ID="btnSalePrepGo" runat="server" Text="Go" ClientIDMode="Static" />
            <table border="1">
                <thead>
                    <tr>
                        <th>
                            &nbsp;
                        </th>
                        <th>
                            CP Processing Task
                        </th>
                        <th>
                            &nbsp;
                        </th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <th>
                            1.
                        </th>
                        <td>
                            Tax sale candidates
                        </td>
                        <td>
                            <asp:Label ID="lblSalePrepNumCandidates" runat="server">
                            </asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <th>
                            2.
                        </th>
                        <td>
                            Candidates not assigned advertisement fee
                        </td>
                        <td>
                            <asp:Label ID="lblSalePrepNumAdvFee" runat="server">
                            </asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <th>
                            3.
                        </th>
                        <td>
                            Post advertising fee
                        </td>
                        <td>
                            <asp:Label ID="lblSalePrepDateFeesPosted" runat="server"></asp:Label>
                            <asp:Button ID="btnSalePrepPostFees" runat="server" Text="Post" Enabled="false" ClientIDMode="Static" />
                        </td>
                    </tr>
                    <tr>
                        <th>
                            4.
                        </th>
                        <td>
                            Print candidate CSV
                        </td>
                        <td>
                            <asp:Button ID="btnSalePrepCSV" runat="server" Text="Download" Enabled="false" ClientIDMode="Static" />
                        </td>
                    </tr>
                    <tr>
                        <th>
                            5.
                        </th>
                        <td>
                            Rolls assigned CP shell
                        </td>
                        <td>
                            <asp:Label ID="lblSalePrepNumCPShell" runat="server">
                            </asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <th>
                            6.
                        </th>
                        <td>
                            Create CP shell
                        </td>
                        <td>
                            <asp:Button ID="btnSalePrepCreateCPShell" runat="server" Text="Post" Enabled="false"
                                ClientIDMode="Static" />
                        </td>
                    </tr>
                    <tr>
                        <th>
                            7.
                        </th>
                        <td>
                            CP Sold at Auction
                        </td>
                        <td>
                            <asp:Label ID="lblSalePrepNumSoldAtAuction" runat="server">
                            </asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <th>
                            8.
                        </th>
                        <td>
                            Unassigned CP
                        </td>
                        <td>
                            <asp:Label ID="lblSalePrepUnassignedCPs" runat="server">
                            </asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <th>
                            9.
                        </th>
                        <td>
                            Assign unsold CP to State
                        </td>
                        <td>
                            <asp:Button ID="btnSalePrepAssignToState" runat="server" Text="Post" Enabled="false"
                                ClientIDMode="Static" />
                        </td>
                    </tr>
                </tbody>
            </table>
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
      
              <%--<div>
              <br />

                    <asp:Label ID="Label2" runat="server" Text="Maintenance Tasks" Font-Bold="true" Font-Size="Large"/>
                        <br />
                        <br />
                        <table>
                            <tr>
                                <td>
                                     <asp:Button ID="btnLocateReturnedCheck" runat="server" Text="Locate Returned Check" Width ="300px" OnClick="ViewReturnedChecks"/>    
                                </td>
                                <td>
                                    Reverse a transaction due to NSF
                                </td>
                            </tr>

                            <tr>
                                <td>
                                     <asp:Button ID="btnProcessRefunds" runat="server" Text="Process Refunds" Width ="300px" OnClick="ViewRefunds"/>   
                                </td>
                                <td>
                                    Issue refund checks for CP and over-payments
                                </td>
                            </tr>
                            <tr>
                                <td>
                                     <asp:Button ID="btnDailyLetters" runat="server" Text="Daily Letters" Width ="300px" />   
                                </td>
                                <td>
                                    Print Daily letters
                                </td>
                            </tr>

                            <tr>
                                <td>
                                     <asp:Button ID="btnCheckLPS" runat="server" Text="Check LPS" Width ="300px" OnClick="LoadLPS"/>   
                                </td>
                                <td>
                                    Check duplicate Lender Processing Service numbers
                                </td>
                            </tr>
                            <tr>
                                    <td>
                                        <asp:Button ID="btnCAD" runat="server" Text="Computer Aided Payments" Width ="300px" OnClick="LoadCAD"/>                         
                                    </td>
                                    <td>
                                        Select a table for Computer Aided Processing

                                    </td>
                                </tr>
                        </table>
              </div>--%>  

             <div>
             <br />
             <br />
                    <asp:Label ID="Label1" runat="server" Text="Tax System Automatic Functions" Font-Bold="true" Font-Size="Large"/>
                        <br />
                        <br />
                            <table>
                                <tr>
                                    <td>
                                        <asp:Button ID="btnNightlyFunc" runat="server" Text="Run Nightly Functions" Width ="300px" />                         
                                    </td>
                                    <td>
                                        Force-run nightly database updates
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:Button ID="btnCaptureLevy" runat="server" Text="Capture Levy"  Width ="300px"/>                         
                                    </td>
                                    <td>
                                        Load current levy amounts and collections to the Levy Totals table
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:Button ID="btnAgeInterest" runat="server" Text="Age Interest" Width ="300px" />                         
                                    </td>
                                    <td>
                                        Replace existing interest records with interests for the following month
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:Button ID="btnUpdateWebVals" runat="server" Text="Update Web Values" Width ="300px" />                         
                                    </td>
                                    <td>
                                        Reloads the web date speed tables
                                    </td>
                                </tr>

                                <tr>
                                    <td>
                                        <asp:Button ID="btnLoadForeclosures" runat="server" Text="Load Foreclosures" Width ="300px" />                         
                                    </td>
                                    <td>
                                        Loads foreclosures
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:Button ID="btnSendUnsecured" runat="server" Text="Send Unsecured to Sheriff" Width ="300px" />                         
                                    </td>
                                    <td>
                                        Send Unsecured Delinquent Account to the Sheriff
                                    </td>
                                </tr>

                                

                            </table>
             </div>
           
        

        </div>

    </div>
    </form>
</body>
</html>
