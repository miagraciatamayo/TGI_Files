<%@ Page Language="VB" AutoEventWireup="false" CodeFile="TaxCP.aspx.vb" Inherits="TaxCP"
    StylesheetTheme="Blue" %>

<%@ Register TagPrefix="ajaxToolkit" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Certificates of Purchase</title>
    <link href="Css/redmond/jquery-ui-1.8.23.custom.css" rel="stylesheet" type="text/css" />
    <link href="Css/Tax.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="JavaScript/jquery-1.5.1.min.js"></script>
    <script type="text/javascript" src="JavaScript/jquery-ui-1.8.23.custom.min.js"></script>
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
        });

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
                    <img alt="Navajo County Logo" width="74" height="74" src="logo.png" />
                </td>
            </tr>
            <tr>
                <td>
                    <h1>
                        Navajo County Treasurer</h1>
                </td>
                <td>
                    Operator:
                    <asp:Label ID="lblOperatorName" runat="server"></asp:Label>
                </td>
                <td>
                    Login Time:
                    <asp:Label ID="lblLoginTime" runat="server"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <h2>
                        Certificates of Purchase</h2>
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
                    <input id="btnHeaderLogout" type="button" value="Logout"></input>
                </td>
            </tr>
        </table>
    </div>
    <!-- Main Tabs -->
    <div id="mainTabs">
        <ul>
            <li><a href="#tabLookup">Lookup</a></li>
            <li><a href="#tabSalePrep">Sale Preparation</a></li>
            <li><a href="#tabTaxSale">Tax Sale</a></li>
            <li><a href="#tabExpirations">Expirations</a></li>
            <li><a href="#tabForeclosures">Foreclosures</a></li>
        </ul>
        <!-- Lookup Tab -->
        <div id="tabLookup">
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
        <!-- Sale Preparation Tab -->
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
        <!-- Tax Sale Tab -->
        <div id="tabTaxSale">
        </div>
        <!-- Expirations Tab -->
        <div id="tabExpirations">
        </div>
        <!-- Foreclosures Tab -->
        <div id="tabForeclosures">
        </div>
    </div>
    </form>
</body>
</html>
