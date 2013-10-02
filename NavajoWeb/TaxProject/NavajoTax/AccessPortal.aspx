<%@ Page Language="VB" AutoEventWireup="true" CodeFile="AccessPortal.aspx.vb" Inherits="AccessPortal.AccessPortal"
    StylesheetTheme="Blue" %>

<%@ Register TagPrefix="ajaxToolkit" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Tax Information Portal</title>
    <link href="Css/redmond/jquery-ui-1.8.23.custom.css" rel="stylesheet" type="text/css" />
    <link href="Css/Tax.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="JavaScript/jquery-1.5.1.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery-ui-1.8.23.custom.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery.validate.js"></script>
    <script type="text/javascript">
        $(document).ready(function () {
            // Form submit.
            $("form").submit(function () {
                var action = document.getElementById("form1").action;
                if (action.lastIndexOf("#") >= 0) {
                    action = action.substr(0, action.lastIndexOf("#"));
                }
                document.getElementById("form1").action = action + window.location.hash;

                showLoadingBox();
            });

            // Tabs
            $("#mainTabs").tabs({
                selected: window.location.hash,
                select: function (event, ui) {
                    window.location.hash = $("#mainTabs ul li:eq(" + ui.index + ") a").attr("href");
                }
            }).tabs("select", (window.location.hash ? window.location.hash : 0));



            // Buttons
            $("#btnHeaderLogout").button();
            $("#btnLookupNumberGo").button();
            $("#btnLookupInvestorGo").button();
            $("#btnLookupYearGo").button();
            $("#btnLookupCandidatesGo").button();
            $("#btnLookupForeclosure").button();
            $("#btnSalePrepGo").button();
            $("#btnSalePrepPostFees").button();
            $("#btnSalePrepCSV").button();
            $("#btnSalePrepCreateCPShell").button();
            $("#btnSalePrepAssignToState").button();
            $("#btnShowAccountRemarksPopup").button();


            $("#txtTaxIDSearch").keyup(function (event) {
                if (event.keyCode == 13) {
                    $("#btnTaxIDSearch").click();
                }
            });

            $("#txtLastNameSearch").keyup(function (event) {
                if (event.keyCode == 13) {
                    $("#btnLastNameSearch").click();
                }
            });
            //
            //
            $("#txtLastNameSearch2").keyup(function (event) {
                if (event.keyCode == 13) {
                    $("#btnLastNameSearch2").click();
                }
            });

            $("#txtTabOuterTaxRoll").keyup(function (event) {
                if (event.keyCode == 13) {
                    $("#btnTaxRollSearch").click();
                }
            });

            $("#txtTaxIDSearch2").keyup(function (event) {
                if (event.keyCode == 13) {
                    $("#btnTaxIDSearch2").click();
                }
            });

            // setup autocomplete for txtTaxIDSearch
            $("[id$=txtTaxIDSearch]").click({
                minLength: 1,
                source: function (request, response) {
                    $.ajax({
                        type: "POST",
                        url: "AccessPortal.aspx/GetParcelOrTaxID",
                        data: '{"parcelOrTaxID":"' + request.term + '"}',
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
                    // alert("APN: " + ui.item.label.split(",")[1]);
                    $("[id$=txtTaxIDSearch]").val(ui.item.value);
                    $("[id$=txtAPN]").val(ui.item.label.split(",")[1]);
                    $("[id$=btnTaxIDSearch]").click();
                }
            });

            $("[id$=txtTaxIDSearch2]").click({
                minLength: 1,
                source: function (request, response) {
                    $.ajax({
                        type: "POST",
                        url: "AccessPortal.aspx/GetParcelOrTaxID",
                        data: '{"parcelOrTaxID":"' + request.term + '"}',
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
                    // alert("APN: " + ui.item.label.split(",")[1]);
                    $("[id$=txtTaxIDSearch2]").val(ui.item.value);
                    $("[id$=txtAPN2]").val(ui.item.label.split(",")[1]);
                    $("[id$=btnTaxIDSearch2]").click();
                }
            });

//            // setup autocomplete for txtParcelIDSearch
//            $("[id$=txtParcelSearch]").autocomplete({
//                minLength: 1,
//                source: function (request, response) {
//                    $.ajax({
//                        type: "POST",
//                        url: "AccessPortal.aspx/GetParcelOrTaxID",
//                        data: '{"parcelOrTaxID":"' + request.term + '"}',
//                        contentType: "application/json; charset=utf-8",
//                        dataType: "json",
//                        success: function (data, textStatus, jqXHR) {
//                            var result = [];
//                            $.each(data.d, function (index, value) {
//                                result.push({ value: index, label: value });
//                            });
//                            response(result);
//                        }
//                    });
//                },
//                select: function (event, ui) {
//                    // alert("APN: " + ui.item.label.split(",")[1]);
//                    $("[id$=txtParcelSearch]").val(ui.item.value);
//                    $("[id$=txtAPN]").val(ui.item.label.split(",")[1]);
//                    $("[id$=btnParcelSearch]").click();
//                }
//            });

//            $("[id$=txtParcelSearch2]").autocomplete({
//                minLength: 1,
//                source: function (request, response) {
//                    $.ajax({
//                        type: "POST",
//                        url: "AccessPortal.aspx/GetParcelOrTaxID",
//                        data: '{"parcelOrTaxID":"' + request.term + '"}',
//                        contentType: "application/json; charset=utf-8",
//                        dataType: "json",
//                        success: function (data, textStatus, jqXHR) {
//                            var result = [];
//                            $.each(data.d, function (index, value) {
//                                result.push({ value: index, label: value });
//                            });
//                            response(result);
//                        }
//                    });
//                },
//                select: function (event, ui) {
//                    // alert("APN: " + ui.item.label.split(",")[1]);
//                    $("[id$=txtParcelSearch2]").val(ui.item.value);
//                    $("[id$=txtAPN2]").val(ui.item.label.split(",")[1]);
//                    $("[id$=btnParcelSearch2]").click();
//                }
//            });


            // setup autocomplete for txtLastNameSearch
            $("[id$=txtLastNameSearch]").click({
                minLength: 1,
                source: function (request, response) {
                    $.ajax({
                        type: "POST",
                        url: "AccessPortal.aspx/GetNameID",
                        data: '{"NameID":"' + request.term + '"}',
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
                    // alert("APN: " + ui.item.label.split(",")[1]);
                    $("[id$=txtLastNameSearch]").val(ui.item.value);
                    // $("[id$=txtAPN]").val(ui.item.label.split(",")[1]);
                    $("[id$=btnLastNameSearch]").click();
                }
            });

            $("[id$=txtLastNameSearch2]").click({
                minLength: 1,
                source: function (request, response) {
                    $.ajax({
                        type: "POST",
                        url: "AccessPortal.aspx/GetNameID",
                        data: '{"NameID":"' + request.term + '"}',
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
                    // alert("APN: " + ui.item.label.split(",")[1]);
                    $("[id$=txtLastNameSearch2]").val(ui.item.value);
                    // $("[id$=txtAPN]").val(ui.item.label.split(",")[1]);
                    $("[id$=btnLastNameSearch2]").click();
                }
            });

        });

        $("[id$=btnShowAccountRemarksPopup]").click(function (event, ui) {
            // $("#divAddRemark").dialog("open");
            showAddRemarkPopup("Account");

            event.preventDefault();
        });

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

        function openLookupCandidatesDialog() {
            $("#divLookupCandidates").dialog({
                modal: true,
                title: "Tax Sale Candidates",
                width: 800,
                close: function (e, ui) {
                    $(this).dialog("destroy");
                }
            });
        }
    </script>
    <style type="text/css">
        .style2
        {
            height: 45px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <!-- Hidden Controls -->
    <!-- Modal Dialogs -->
    <div id="divLoading" title="Loading, please wait..." class="divPopup">
        <img src="ajax-loader_redmond.gif" alt="Loading..." width="220" height="19" />
    </div>
    <div id="divLookupCandidates" class="divPopup">
        <asp:GridView ID="grdCandidatesChild" runat="server">
        </asp:GridView>
    </div>
    <!-- Header -->
    <div class="header">
        <table>
            <tr>
                <td rowspan="3">
                    <img alt="Logo" id="imageFromDB" width="74" height="74" src="logo.png" />
                </td>
            </tr>
            <tr>
                <td class="style2">
                    <h1 id="loadCountyTitle" runat="server">
                        </h1>
                </td>               
            </tr>
            <tr>
                <td>
                    <h2>
                        Tax Information Portal</h2>
                </td>
                             
            </tr>
        </table>
    </div>


    <!-- Main Tabs -->
    <div id="mainTabs">
        <ul>
            <li><a href="#tabTaxAccount">Tax Account</a></li>
            <li><a href="#tabOuterTaxRoll">Tax Roll</a></li>
            <li><a href="#tabCP">CP</a></li>
            <li><a href="#tabReports">Reports</a></li>

        </ul>
        <!-- Account Tab -->
        <div id="tabTaxAccount">
        <ajaxToolkit:TabContainer ID="TabContainer1" runat="server"  ActiveTabIndex ="0" >
                <ajaxToolkit:TabPanel ID="TabPanel1" runat="server" HeaderText="Account">
                    <ContentTemplate>                       

                        <table id="TaxAccountSearch">
                        <tr>
                            <td>
                                <b>Tax ID:</b>
                            </td>
                            <td>
                                <asp:TextBox ID="txtTaxIDSearch" TabIndex="1" runat="server" ClientIDMode="Static" />
                                <asp:TextBox ID="txtAPN" runat="server" ClientIDMode="Static" Style="display: none;"/>
                                <%--<asp:Button ID="btnTaxIDSearch" runat="server" Text="Search" 
                                    CausesValidation="False"  Style="display: none;"  ClientIDMode="Static"/>--%>
                                <asp:Button ID="btnTaxIDSearch" runat="server" Text="GO" ClientIDMode="Static" />
                            </td>
                        </tr>
                        <%--<tr>
                            <td>
                                <b>Parcel:</b>
                            </td>
                            <td>
                                <asp:TextBox ID="txtParcelSearch" TabIndex="2" runat="server" ClientIDMode="Static"/>
                                <asp:Button ID="btnParcelSearch" runat="server" Text="Search" 
                                    CausesValidation="False" Style="display: none;"  ClientIDMode="Static"/>
                            </td>
                        </tr>--%>
                        <tr>
                            <td>
                                <b>Last Name: </b>
                            </td>
                            <td>
                                <asp:TextBox ID="txtLastNameSearch" TabIndex="3" runat="server" ClientIDMode="Static" width="250px"/>
                                <%--<asp:Button ID="btnLastNameSearch" runat="server" Text="Search" 
                                    CausesValidation="False" Style="display: none;"  ClientIDMode="Static"/>--%>
                                <asp:Button ID="btnLastNameSearch" runat="server" Text="GO" 
                                    ClientIDMode="Static"/>
                            </td>
                        </tr>
                    </table>

                        <div id="TaxAccountDefinition" style="width:1000px">
                        <h1 style="text-align:center">Tax Account Definition</h1>
                            <hr />
                                <table style="width:990px">
                                    <tr >
                                        <td style="width:150px;">
                                            <b>Parcel or Tax ID:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblParcelTaxID" runat="server" />
                                        </td>
                                        <td style="width:150px;">
                                            <b>Account Status:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblAccountStatus" runat="server" />
                                        </td>
                                        <td style="width:150px;">
                                            <b>Collections Deputy:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblCollectionsDeputy" runat="server" />
                                        </td>
                                    </tr>
                                    <tr >
                                        <td style="width:150px;">
                                            <b>Secured:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblSecured" runat="server" />
                                        </td>
                                        <td style="width:150px;">
                                            <b>Alert Level:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblAlertLevel" runat="server" />
                                        </td>
                                        <td style="width:150px;">
                                            <b>Street Name:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblStreetName" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="width:150px;">
                                            <b>Parcel Number:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblParcelNumber" runat="server" />
                                        </td>
                                        <td style="width:150px;">
                                            <b>Account Suspended:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblAccountSuspend" runat="server" />
                                        </td>
                                    </tr>
                                </table>
                            </div>

                            <div id="PhysicalLocationParameters" style="width:1000px;margin-top:40px;" >
                              <h1 style="text-align:center" >Physical Location Parameters</h1>
                              <hr />
                                <table style="width:990px">
                                    <tr >
                                        <td style="width:150px;">
                                            <b>Latitude:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblLatitude" runat="server" />
                                        </td>
                                        <td style="width:150px;">
                                            <b>Longitude:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblLongitude" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="width:150px;">
                                            <b>House Number:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblHouseNumber" runat="server" />
                                        </td>
                                       <%-- <td style="width:150px;">
                                            <b>Street:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblStreet" runat="server" />
                                        </td>--%>
                                        <td style="width:150px;">
                                            <b>Location City:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblLocationCity" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="width:150px;">
                                            <b>Physical Address:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblPhysicalAddress" runat="server" />
                                        </td>
                                      <%--  <td style="width:150px;">
                                            <b>City:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblCity" runat="server" />
                                        </td>--%>
                                        <td style="width:150px;">
                                            <b>Postal Code:</b>
                                        </td>
                                        <td style="width:180px;">
                                            <asp:Label ID="lblPostalCode" runat="server" />
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        <asp:DetailsView ID="DetailsView1" runat="server">
                        </asp:DetailsView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>


                <ajaxToolkit:TabPanel  ID="tabTaxRoll" runat="server" HeaderText="Tax Rolls">
                    <ContentTemplate>
                        <asp:GridView ID="grdTaxRoll" runat="server" AutoGenerateColumns="false"
                        CellPadding="4" ForeColor="#333333" GridLines="None" width="800px" RowStyle-HorizontalAlign="Center">
                            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                            <Columns>                                
                                <asp:ButtonField Text ="Tax Year" DataTextField="TaxYear" ButtonType="Link" CommandName ="gotoOuterTaxRoll" HeaderText="Tax Year" />                                                               
                                <asp:BoundField HeaderText="Tax Year" DataField="TaxYear"/>
                                <asp:BoundField HeaderText="Tax Roll Number" DataField="TaxRollNumber" />
                                <asp:BoundField HeaderText="Secured" DataField="SecuredUnsecured"  />
                                <asp:BoundField HeaderText="Current Balance" DataField="CurrentBalance" DataFormatString="{0:C}"  /> 
                                <asp:BoundField HeaderText="Status" DataField="Status"  />
                            </Columns>
                            <EditRowStyle BackColor="#999999" />
                            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White"  />
                            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                            <RowStyle BackColor="#F7F6F3" ForeColor="#333333"  />
                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                        </asp:GridView> 
                    </ContentTemplate>                    
                </ajaxToolkit:TabPanel>

                <ajaxToolkit:TabPanel  ID="tabAccountRemarks" runat="server" HeaderText="Remarks">
                    <ContentTemplate>
                            <div>
	                        <fieldset>
		                        <table>                                    
			                        <tr>
				                        <td colspan="2">
					                        <div style="margin-left: 20px">
						                        <asp:GridView ID="gvAccountRemarks" runat="server">
							                        <Columns>
								                        
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
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>

                <ajaxToolkit:TabPanel  ID="tabDeeds" runat="server" HeaderText="Deeds">
                    <ContentTemplate>
                       <asp:GridView ID="grdDeeds" runat="server" AutoGenerateColumns="false"
                        CellPadding="4" ForeColor="#333333" GridLines="None" width="800px" RowStyle-HorizontalAlign="Center">
                                <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                                <Columns>       
                                    <asp:BoundField HeaderText="Deed Year" DataField="Deed Year"  />                                                     
                                    <asp:BoundField HeaderText="Initiated" DataField="Initiated" />
                                    <asp:BoundField HeaderText="Completed" DataField="Completed"  />
                                    <asp:BoundField HeaderText="Status" DataField="Status"/> 
                                    <asp:BoundField HeaderText="Foreclosing Party" DataField="Foreclosing Party"  />
                                    <asp:ButtonField Text ="Loss" DataTextField="WithLoss" ButtonType="Link" CommandName ="withLoss" HeaderText="Loss" />                                    
                                </Columns>
                                <EditRowStyle BackColor="#999999" />
                                <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White"  />
                                <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                                <RowStyle BackColor="#F7F6F3" ForeColor="#333333"  />
                                <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                        </asp:GridView>
                        <br />
                        <br />
                        <div>
                       
                        
                        <asp:Label ID="lblLoss" runat="server"> </asp:Label> 
                            <br />
                            <asp:GridView ID="grdLoss" runat="server" AutoGenerateColumns="false"
                            CellPadding="4" ForeColor="#333333" GridLines="None" width="800px" RowStyle-HorizontalAlign="Center">
                                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                                    <Columns>       
                                        <asp:BoundField HeaderText="Tax Year" DataField="Tax Year" />                                                     
                                        <asp:BoundField HeaderText="Roll" DataField="Roll"/>
                                        <asp:BoundField HeaderText="Revenue Loss" DataField="Revenue Loss"/>                                   
                                    </Columns>
                                    <EditRowStyle BackColor="#999999" />
                                    <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                    <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White"  />
                                    <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                                    <RowStyle BackColor="#F7F6F3" ForeColor="#333333"  />
                                    <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                    <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                    <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                    <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                    <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                            </asp:GridView>
                                                      
                        </div>

                    </ContentTemplate>                                                       
                </ajaxToolkit:TabPanel>
                </ajaxToolkit:TabContainer>

        </div>
        <!-- Tax Roll Tab -->
        <div id="tabOuterTaxRoll">
         <ajaxToolkit:TabContainer ID="TabContainer2" runat="server" ActiveTabIndex ="0" >
                <ajaxToolkit:TabPanel  ID="tabRollDef" runat="server" HeaderText="Roll Definition" >
                    <ContentTemplate>
                  <%--      <table id="Table1">
                       <tr>
                            <td>
                                <b>Tax Year:</b>
                            </td>
                            <td>
                                <asp:TextBox ID="txtTabOuterTaxRollYear" TabIndex="1" runat="server" ClientIDMode="Static"  Enabled ="false"/>                                                               
                            </td>
                           
                        </tr>
                           
                                              
                    </table>--%>

                    <table id="Table2">

                    <tr>
                         <td>
                            <b>Tax Year:</b>
                            </td>

                            <td>
                            <asp:DropDownList ID="ddlTaxYear" runat="server" Width="80">                               
                            </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <b>Tax Roll:</b>
                            </td>
                            <td>
                                <asp:TextBox ID="txtTabOuterTaxRoll" TabIndex="2" runat="server" />                                
                               <asp:Button ID="btnTaxRollSearch" runat="server" Text="GO" 
                                ClientIDMode="Static"/>
                            </td>
                        </tr> 
                        <tr>
                            <td>
                                <b>Tax ID:</b>
                            </td>
                            <td>
                                <asp:TextBox ID="txtTaxIDSearch2" TabIndex="1" runat="server" ClientIDMode="Static" />
                                <asp:TextBox ID="txtAPN2" runat="server" ClientIDMode="Static" Style="display: none;"/>
                                <%--<asp:Button ID="btnTaxIDSearch2" runat="server" Text="Search" 
                                    CausesValidation="False"  Style="display: none;"  ClientIDMode="Static"/>--%>
                                <asp:Button ID="btnTaxIDSearch2" runat="server" Text="GO" 
                                ClientIDMode="Static"/>
                            </td>
                        </tr>
                        <%--<tr>
                            <td>
                                <b>Parcel:</b>
                            </td>
                            <td>
                                <asp:TextBox ID="txtParcelSearch2" TabIndex="2" runat="server" ClientIDMode="Static"/>
                                <asp:Button ID="btnParcelSearch2" runat="server" Text="Search" 
                                    CausesValidation="False" Style="display: none;"  ClientIDMode="Static"/>
                            </td>
                        </tr>--%>
                        <tr>
                            <td>
                                <b>Last Name: </b>
                            </td>
                            <td>
                                <asp:TextBox ID="txtLastNameSearch2" TabIndex="3" runat="server" ClientIDMode="Static" Width="250px"/>
                               <%-- <asp:Button ID="btnLastNameSearch2" runat="server" Text="Search" 
                                    CausesValidation="False" Style="display: none;"  ClientIDMode="Static"/>--%>
                                    <asp:Button ID="btnLastNameSearch2" runat="server" Text="GO" 
                                    ClientIDMode="Static"/>
                            </td>
                        </tr>
                    </table>

                    <table>
                                <tr>
                                    <td align="left">
                                    <div id="Div1" style="width:800px">
                                    <h1 style="text-align:center" >Roll Definition</h1>
                                            <hr />
                                                <table style="width:690px;">
                                                    <tr >
                                                        <td style="width:150px;">
                                                            <b>Tax Year:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblTaxYear" runat="server" />
                                                        </td>
                                                        <td style="width:150px;">
                                                            <b>Tax Roll Number:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblTaxRollNumber" runat="server" />
                                                        </td>
                                                        <td rowspan ="9">
                                                
                                                        </td>
                                                        <%--<td style="width:150px;">
                                                            <b>Secured:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblSecuredOuter" runat="server" />
                                                        </td>--%>


                                                    </tr>
                                                    <tr >
                                                        <%--<td style="width:150px;">
                                                            <b>Tax ID Number:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblTaxIDNumber" runat="server" />
                                                        </td>--%>
                                                        <td style="width:150px;">
                                                            <b>District:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblDistrict" runat="server" />
                                                        </td>
                                                        <td style="width:150px;">
                                                            <b>Tax Area:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblTaxArea" runat="server" />
                                                        </td>
                                                        <%--<td style="width:150px;">
                                                            <b>Parcel Number:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblParcelNumberAPN" runat="server" />
                                                        </td>--%>
                                                        <%--<td style="width:150px;">
                                                            <b>Status:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblStatus" runat="server" />
                                                        </td>--%>
                                                    </tr>
                                                    <tr>
                                                        <td style="width:150px;">
                                                            <b>Lender Processing Service:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblLenderProcessingService" runat="server" />
                                                        </td>
                                                        <td style="width:150px;">
                                                            <b>Current Balance:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblCurrentBalance" runat="server" />
                                                        </td>
                                                        <%--<td style="width:150px;">
                                                            <b>District Area:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblDistrictArea" runat="server" />
                                                        </td>--%>
                                                    </tr>
                                                    <tr>
                                       
                                                       <%-- <td style="width:150px;">
                                                            <b>Latitude:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblLat" runat="server" />
                                                        </td>--%>
                                                        <%--<td style="width:150px;">
                                                            <b>Longitude:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblLong" runat="server" />
                                                        </td>--%>
                                                    </tr>
                                                   <%-- <tr>
                                                        <%--<td style="width:150px;">
                                                            <b>Tax Area:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblTaxArea" runat="server" />
                                                        </td>--%>
                                                        <%--<td style="width:150px;">
                                                            <b>Postal Code:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblPostalCodeOuter" runat="server" />
                                                        </td>--%>
                                                       <%-- <td style="width:150px;">
                                                            <b>Board Order:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblBoardOrder" runat="server" />
                                                        </td>
                                                    </tr>--%>
                                                    <tr>
                                                        <td style="width:150px;">
                                                            <b>First Half Delinquent:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblFirstHalfDelinquent" runat="server" />
                                                        </td>
                                                        <td style="width:150px;">
                                                            <b>Second Half Delingquent:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblSecondHalfDelinquent" runat="server" />
                                                        </td>   
                                                                  
                                                    </tr>
                                                </table>
                               
                                            </div>                            

                                            <div id="Div2" style="width:800px;margin-top:40px;" >
                                              <h1 style="text-align:center" >Tax Payer Name and Address</h1>
                                              <hr />
                                                <table style="width:690px">
                                                    <tr >
                                                        <td style="width:100px;">
                                                            <b>Name 1:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblFirstNameOuter" runat="server" />
                                                        </td>
                                       
                                                        <td style="width:100px;">
                                                            <b>c/o Address:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblCOAddressOuter" runat="server" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                     <td style="width:100px;">
                                                            <b>Name 2:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblMiddleNameOuter" runat="server" />
                                                        </td>
                                                          <td style="width:100px;">
                                                            <b>Mailing Address:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblMailingAddressOuter" runat="server" />
                                                        </td>
                                        
                                                        <%--<td style="width:150px;">
                                                            <b>Confidential Flag:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblConfidentialFlagOuter" runat="server" />
                                                        </td>--%>
                                                      <%--  <td style="width:150px;">
                                                            <b>Mail Return Flag:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblMailReturnFlagOuter" runat="server" />
                                                        </td>--%>
                                                    </tr>
                                                    <tr>
                                                    <td style="width:100px;">
                                                            <b>Name 3:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblLastNameOuter" runat="server" />
                                                        </td>
                                      
                                                        <td style="width:100px;">
                                                            <b>Mailing City:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblMailingCityOuter" runat="server" />
                                                      <%--  </td>
                                                        <td style="width:150px;">
                                                            <b>Mailing State:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblMailingStateOuter" runat="server" />
                                                        </td>--%>
                                                    </tr>
                                                    <tr>
                                                    <td style="width:100px;">
                                                            <b>Mailing State:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblMailingStateOuter" runat="server" />
                                                        </td>
                                                       <%-- <td style="width:150px;">
                                                            <b>eMail:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblEmailOuter" runat="server" />
                                                        </td> --%>                                       
                                                       <%-- <td style="width:150px;">
                                                            <b>Owner Group:</b>
                                                        </td>
                                                        <td style="width:180px;">
                                                            <asp:Label ID="lblOwnerGroupOuter" runat="server" />
                                                        </td>--%>
                                                    </tr>
                                                </table>
                                            </div>

                                    </td>
                                    <td align="right">

                                            <div align="right">
                                                <table>
                                                    <tr valign ="top">
                                                        <td align="left"   valign ="top"                                  
                                                            style="border: medium groove #99CCFF; width:200px; border-collapse: collapse;" 
                                                            bgcolor="Silver">
                                                                        <asp:Button ID="btnAccountStatusLight" runat="server"  OnClientClick ="return false"
                                                                        Text="Account Status" Width="200px" />
                                                                        
                                                                        <asp:Button ID="btnRollStatusLight" runat="server" OnClientClick ="return false"
                                                                            Text="Roll Status" Width="200px" />
                                                                       
                                                                        <asp:Button ID="btnSuspendLight" runat="server" Text="Suspend"  OnClientClick ="return false"
                                                                            Width="200px" />
                                                                       
                                                                        <asp:Button ID="btnBoardOrderLight" runat="server"  OnClientClick ="return false"
                                                                            Text="Board Order" Width="200px" />
                                                                       
                                                                        <asp:Button ID="btnBankruptcyLight" runat="server" OnClientClick ="return false"
                                                                            Text="Bankruptcy" Width="200px" />
                                                                        
                                                                        <asp:Button ID="btnAlertLight" runat="server" Text="Alert"  OnClientClick ="return false"
                                                                            Width="200px" />
                                                                        
                                                                        <asp:Button ID="btnCPLight" runat="server"  Text="CP"  OnClientClick ="return false"
                                                                            Width="200px" />
                                                                        
                                                                        <asp:Button ID="btnConfLight" runat="server" Text="Confidential"  OnClientClick ="return false"
                                                                            Width="200px" />
                                                                        
                                                                        <asp:Button ID="btnRetMailLight" runat="server" OnClientClick ="return false"
                                                                            Text="Returned Mail" Width="200px" />
                                                            </td>
                                                        </tr>
                                        
                                                </table>
                                        </div>
                                    </td>

                                </tr>
                    </table>

                    
                             
                    </ContentTemplate>
                    
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabRemarks" runat="server" HeaderText="Remarks">
                    <ContentTemplate>
                        <div>
	                        <fieldset>
		                        <table>                                    
			                        <tr>
				                        <td colspan="2">
					                        <div style="margin-left: 20px">
						                        <asp:GridView ID="gvTaxRollRemarks" runat="server">
							                        <Columns>
								                        
								                        <asp:TemplateField HeaderText="Attachment">
									                        <ItemTemplate>
										                        <%# IIf(IsDBNull(DataBinder.Eval(Container.DataItem, "IMAGE")), "&nbsp;", "<a target='_blank' href='GetBlobFromDB.ashx?tabname=genii_user.ST_BOARD_ORDER" & _
                                     "&colname=IMAGE&pknames=RECORD_ID&pkvalues=" & DataBinder.Eval(Container.DataItem, "RECORD_ID") & _
                                     "&filetype=" & DataBinder.Eval(Container.DataItem, "CHANGE_TYPE") & "'>" & _
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
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>

                <ajaxToolkit:TabPanel ID="tabCharges" runat="server" HeaderText="Charges">
                    <ContentTemplate>
                        <asp:GridView ID="grdTaxRollCharges" runat="server" AutoGenerateColumns="false"
                        CellPadding="4" ForeColor="#333333" GridLines="None" width="800px" RowStyle-HorizontalAlign="Center">
                            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                            <Columns>                                                                                               
                                <asp:BoundField HeaderText="Tax Year" DataField="Tax Year"/>
                                <asp:BoundField HeaderText="Roll Number" DataField="Roll Number" />
                                <asp:BoundField HeaderText="Authority" DataField="Authority" /> 
                                <asp:BoundField HeaderText="Tax Type" DataField="Tax Type"  />
                                <asp:BoundField HeaderText="Charge" DataField="Charge"  />
                                <asp:BoundField HeaderText="Original Value" DataField="Original Value"  />
                            </Columns>
                            <EditRowStyle BackColor="#999999" />
                            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White"  />
                            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                            <RowStyle BackColor="#F7F6F3" ForeColor="#333333"  />
                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                        </asp:GridView> 
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>

                <ajaxToolkit:TabPanel ID="tabPayments" runat="server" HeaderText="Payments">
                    <ContentTemplate>
                        <asp:GridView ID="grdTaxRollPayments" runat="server" AutoGenerateColumns="false"
                        CellPadding="4" ForeColor="#333333" GridLines="None" width="800px" RowStyle-HorizontalAlign="Center">
                            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                            <Columns>                                                                                               
                                <asp:BoundField HeaderText="Tax Year" DataField="Tax Year"/>
                                <asp:BoundField HeaderText="Roll Number" DataField="Roll Number" />
                                <asp:BoundField HeaderText="Payment Amount" DataField="Payment Amount" /> 
                                <asp:BoundField HeaderText="Effective Date" DataField="Effective Date"  />
                                <asp:BoundField HeaderText="Name on Instrument" DataField="Name on Instrument"  />
                                <asp:BoundField HeaderText="Instrument Note" DataField="Instrument Note"  />
                                <asp:BoundField HeaderText="Payor Description" DataField="Payor Description"  />
                                <asp:BoundField HeaderText="Payment Description" DataField="Payment Description"  />
                            </Columns>
                            <EditRowStyle BackColor="#999999" />
                            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White"  />
                            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                            <RowStyle BackColor="#F7F6F3" ForeColor="#333333"  />
                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                        </asp:GridView> 
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>

                <ajaxToolkit:TabPanel ID="tabTaxRollCP" runat="server" HeaderText="CP">
                    <ContentTemplate>
                            <asp:GridView ID="grdTaxRollCP" runat="server" AutoGenerateColumns="false"
                        CellPadding="4" ForeColor="#333333" GridLines="None" width="800px" RowStyle-HorizontalAlign="Center">
                            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                            <Columns>   
                                <asp:BoundField HeaderText="Certificate" DataField="Certificate"/>                                                                                            
                                <asp:BoundField HeaderText="Tax Year" DataField="Tax Year"/>
                                <asp:BoundField HeaderText="Roll Number" DataField="Roll Number" />
                                <asp:BoundField HeaderText="Interest" DataField="Interest" /> 
                                <asp:BoundField HeaderText="Face Value" DataField="Face Value"  />
                                <asp:BoundField HeaderText="Purchase Value" DataField="Purchase Value"  />
                                <asp:BoundField HeaderText="Investor" DataField="Investor"  />
                                <asp:BoundField HeaderText="Current Status" DataField="Current Status"  />
                                <asp:BoundField HeaderText="Date of Sale" DataField="Date of Sale"  />
                                <asp:BoundField HeaderText="Purchase Date" DataField="Purchase Date"  />
                                <asp:BoundField HeaderText="Date Redeemed" DataField="Date Dedeemed"  />
                                <asp:BoundField HeaderText="Interest Earned" DataField="Interest Earned"  />
                            </Columns>
                            <EditRowStyle BackColor="#999999" />
                            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White"  />
                            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
                            <RowStyle BackColor="#F7F6F3" ForeColor="#333333"  />
                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                        </asp:GridView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
         </ajaxToolkit:TabContainer>
        </div>
        <!-- Tax Roll Tab -->
        <div id="tabReports">            
               <table width="100%">
                <%--<tr>
                    <td>
                        <asp:HyperLink ID="HyperLink1" runat="server" rel="NoFollow" Enabled ="false" Target="_blank" NavigateURL="http://svrintweb6/genii_treasury/Genesis.GUI/ExecutivePanel/main.aspx?id=11145">Go to GENII Reports</asp:HyperLink>
                    </td>
                </tr>--%>
                <tr>
                    <td>
                        <iframe id="MyIFrame" runat="server" scrolling="auto" width="100%" height="768px" frameborder="0" src="http://svrintweb6/genii_treasury/Genesis.GUI/ExecutivePanel/main.aspx?id=11145"></iframe>
                    </td>
                </tr>
               </table>
        </div>
        <!-- CP Tab -->
        <div id="tabCP">
            <ajaxToolkit:TabContainer ID="tabLookupSub" runat="server">
                <ajaxToolkit:TabPanel ID="tabNumber" runat="server" HeaderText="Certificate Number">
                    <ContentTemplate>
                        Certificate Number:
                        <asp:TextBox ID="txtLookupNumber" runat="server"></asp:TextBox>
                        <asp:Button ID="btnLookupNumberGo" runat="server" Text="Go" ClientIDMode="Static" />
                        <asp:DetailsView ID="dtlLookupNumber" runat="server">
                        </asp:DetailsView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabInvestor" runat="server" HeaderText="Investor">
                    <ContentTemplate>
                        Investor ID:
                        <asp:TextBox ID="txtLookupInvestor" runat="server"></asp:TextBox>
                        <asp:Button ID="btnLookupInvestorGo" runat="server" Text="Go" ClientIDMode="Static" />
                        <asp:GridView ID="grdLookupInvestor" runat="server">
                        </asp:GridView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabYear" runat="server" HeaderText="Year">
                    <ContentTemplate>
                        Year:
                        <asp:DropDownList ID="ddlLookupYear" runat="server">
                        </asp:DropDownList>
                        <asp:Button ID="btnLookupYearGo" runat="server" Text="Go" ClientIDMode="Static" />
                        <asp:GridView ID="grdLookupYear" runat="server" />
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabCandidates" runat="server" HeaderText="Candidates">
                    <ContentTemplate>
                        <asp:Button ID="btnLookupCandidatesGo" runat="server" Text="Load Tax Sale Candidates"
                            ClientIDMode="Static" />
                        <asp:GridView ID="grdCandidatesTop" runat="server" AutoGenerateColumns="false">
                            <Columns>
                                <asp:BoundField DataField="Book" HeaderText="Book" />
                                <asp:TemplateField HeaderText="Candidates">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkNumCandidates" runat="server" Text='<%#DataBinder.Eval(Container.DataItem, "NumCandidates") %>'
                                            CommandName="OpenBook" CommandArgument='<%#DataBinder.Eval(Container.DataItem, "Book") %>'>
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
                <ajaxToolkit:TabPanel ID="tabForeclosure" runat="server" HeaderText="Foreclosure">
                    <ContentTemplate>
                        <asp:Button ID="btnLookupForeclosure" runat="server" Text="Load Properties in Foreclosure Process"
                            ClientIDMode="Static" />
                    </ContentTemplate>
                </ajaxToolkit:TabPanel>
            </ajaxToolkit:TabContainer>
        </div>
    </div>
    </form>
</body>
</html>
