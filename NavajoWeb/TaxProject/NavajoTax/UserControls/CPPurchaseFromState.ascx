<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CPPurchaseFromState.ascx.cs"
    Inherits="UserControls_CPPurchaseFromState" %>
<link href="<%= Page.ResolveUrl("~/Css/redmond/jquery-ui-1.8.23.custom.css") %>"
    rel="stylesheet" type="text/css" />
<link href="<%= Page.ResolveUrl("~/Css/Tax.css") %>" rel="stylesheet" type="text/css" />
<script type="text/javascript" src="<%= Page.ResolveUrl("~/JavaScript/jquery-1.5.1.min.js") %>"></script>
<script type="text/javascript" src="<%= Page.ResolveUrl("~/JavaScript/jquery-ui-1.8.23.custom.min.js") %>"></script>
<script type="text/javascript">
    //    var allCheckBoxSelector = '#gvInvestorOwnedCPs input[id*="cbAllItems"]:checkbox';
    //    var checkBoxSelector = '#gvInvestorOwnedCPs input[id*="cbSelectItem"]:checkbox';


    $(document).ready(function () {
        // Style Buttons
        $("[id$=btnAddParcel]").button();
        $("[id$=btnCommitData]").button();
        $("[id$=btnPrintReceipt]").button();
        $("[id$=btnRemoveParcelFromGrid]").button();

        $("#btnCommitData").click(function () {
            // Reset amount paid if it has not been changed.
            //  var txtAmountPaid = $("[id$=txtAmountPaid]");
            // if (txtAmountPaid.val() == txtAmountPaid.attr("defaultValue")) {
            //      txtAmountPaid.val("0");
            //  }

            showLoadingBox();
        });

        // SSAN Autocomplete
        $("[id$=txtCPFromStateInvestorSSAN]").autocomplete({
            minLength: 1,
            source: function (request, response) {
                $.ajax({
                    type: "POST",
                    url: "TaxInvestors.aspx/GetCPInvestor",
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
                    $("[id$=txtCPFromStateInvestorSSAN]").val("");
                    event.preventDefault();
                }
                else {
                    $("[id$=txtCPFromStateInvestorSSAN]").val(ui.item.value);
                    $("[id$=btnCPFromStateSSANSearch]").click();
                }
            }
        });


        //        // Setup checkbox events for Investor CP Grid
        //                $(allCheckBoxSelector).live('click', function () {
        //                    $(checkBoxSelector).attr('checked', $(this).is(':checked'));

        //                    ToggleCheckBoxes();
        //                });

        //                $(checkBoxSelector).live('click', ToggleCheckBoxes);

        //                ToggleCheckBoxes();
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

    //        function ToggleCheckBoxes() {
    //            var totalCheckboxes = $(checkBoxSelector),
    //             checkedCheckboxes = totalCheckboxes.filter(":checked"),
    //             noCheckboxesAreChecked = (checkedCheckboxes.length === 0),
    //             allCheckboxesAreChecked = (totalCheckboxes.length === checkedCheckboxes.length);

    //            $(allCheckBoxSelector).attr('checked', allCheckboxesAreChecked);

    //            calculateSum(3);
    //        }


    //        function calculateSum(columnIndex) {
    //            var total = 0.0;

    //            $('input[id*="cbSelectItem"]:checkbox:checked').each(function () {
    //                var parentTR = $(this).parents('tr');
    //                var amount = parentTR.children().eq(2).text();

    //                total += parseFloat(amount);
    //            });

    //            $('#lblTotalAmountCPPurchase').text(total.toFixed(2));
    //        }
        
</script>
<div>
<div id="divLoading" title="Loading, please wait..." class="divPopup">
        <img src="ajax-loader_redmond.gif" alt="Loading..." width="220" height="19" />
</div>
    <fieldset>
        <div>
            <fieldset class="ui-widget ui-widget-content">
                <legend class="ui-widget-header ui-corner-all">Investor Information</legend>
                <table>
                    <tr>
                        <td>
                            Investor SSAN:
                        </td>
                        <td>
                            <asp:TextBox ID="txtCPFromStateInvestorSSAN" runat="server" ClientIDMode="Static" />
                            <asp:Button ID="btnCPFromStateSSANSearch" runat="server" Text="Search" CausesValidation="False"
                                ClientIDMode="Static" OnClick="btnCPFromStateSSANSearch_Click" Style="display: none;" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            Investor Name:
                        </td>
                        <td>
                            <asp:Label ID="lblCPFromStateInvestorName" runat="server" />
                            <asp:Label ID="lblSessionID" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <br />
                            <br />
                        </td>
                        <td>
                        </td>
                    </tr>
                </table>
            </fieldset>
            <br />
            <fieldset class="ui-widget ui-widget-content">
                <legend class="ui-widget-header ui-corner-all">CP To Purchase</legend>
                <table>
                    <tr>
                        <td>
                            Enter Parcel:
                        </td>
                        <td>
                            <asp:TextBox ID="txtCPFromStateParcelNumber" runat="server" ClientIDMode="Static" />&nbsp;
                            <asp:Button ID="btnAddParcel" runat="server" Text="Add Parcel" ClientIDMode="Static"
                                OnClick="btnAddParcel_Click" />
                        </td>
                    </tr>
                </table>
            </fieldset>
        </div>
        <br />
        <div id="divCPFromStateGrid">
            <asp:GridView ID="gvCPFromStateParcelGrid" runat="server" AutoGenerateColumns="False"
                ClientIDMode="Static" OnRowCommand="gvCPFromStateParcelGrid_RowCommand" OnRowDeleting="gvCPFromStateParcelGrid_RowDeleting">
                <Columns>
                    
                    <asp:BoundField HeaderText="Parcel Number" DataField="APN" /> 
                    <asp:BoundField HeaderText="Years" DataField="TAXYEAR" />  
                    <asp:BoundField HeaderText="Certificate" DataField="CERTIFICATENUMBER"  ItemStyle-Width ="10px"  />  
                    <asp:BoundField HeaderText="Range" DataField="TAXYEARRANGE" ItemStyle-Width ="6px" />                  
                    <asp:BoundField HeaderText="Taxes" DataField="TAXES" ItemStyle-Width ="6px"/>
                    <asp:BoundField HeaderText="Interest" DataField="INTEREST" DataFormatString="{0:c}"  ItemStyle-Width ="6px" />
                    <asp:BoundField HeaderText="Fees" DataField="FEES" DataFormatString="{0:c}"  ItemStyle-Width ="6px" />
                    <asp:BoundField HeaderText="Trans Fees" DataField="TRANSACTIONFEES" DataFormatString="{0:c}"  ItemStyle-Width ="6px" />
                    <asp:BoundField HeaderText="Total" DataField="TOTAL" DataFormatString="{0:c}"/>
                    
                    <asp:TemplateField ShowHeader="False">
                        <ItemTemplate>
                            <asp:Button ID="btnRemoveParcelFromGrid" runat="server" CausesValidation="False"
                                CommandName="Delete" Text="Remove" ClientIDMode="Static" />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <br />
        <div>
            <span>Total:</span>
            $
            <asp:Label ID="lblTotalAmountCPPurchase" runat="server" Text="0.00"/>
            <br />
            <br />
      <!--      Tax Year Count:
            <asp:Label ID="lblTaxYearCount" runat="server" Text="0" />
            <br />
            Tax Year Range:
            <asp:Label ID="lblTaxYearRange" runat="server" Text="0" />
            <br />
            <br />
            -->
            <tr>
                <td>
                    <asp:Label ID="Label12" runat="server" Text="Payor:"></asp:Label>
                </td>
                <td  style="width: 300px; padding-right:1">
                    <asp:TextBox ID="txtPayorName" runat="server" Width="300px" TabIndex="7" Style="text-align: right;" CssClass="ReadOnly"></asp:TextBox>
                </td>
            </tr>
            <br />
                       
            <tr>
                <td>
                    <asp:Label ID="Label15" runat="server" Text="Transaction Type:"></asp:Label>
                </td>
                <td>
                    <asp:DropDownList ID="ddlPaymentType" runat="server" TabIndex="9">                   
                    </asp:DropDownList>
                </td>
            </tr>
            <br />           
                                
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

            <asp:Button ID="btnCommitData" runat="server" ClientIDMode="Static" Text="Commit"
                OnClick="btnCommitData_Click" />&nbsp;
            <asp:Button ID="btnPrintReceipt" runat="server" ClientIDMode="Static" Text="Print Receipt"
                OnClick="btnPrintReceipt_Click" />
        </div>
    </fieldset>
</div>
