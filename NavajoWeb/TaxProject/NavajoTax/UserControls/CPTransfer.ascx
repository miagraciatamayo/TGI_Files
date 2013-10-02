<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CPTransfer.ascx.cs" Inherits="UserControls_CPTransfer" %>
<link href="<%= Page.ResolveUrl("~/Css/redmond/jquery-ui-1.8.23.custom.css") %>"
    rel="stylesheet" type="text/css" />
<link href="<%= Page.ResolveUrl("~/Css/Tax.css") %>" rel="stylesheet" type="text/css" />
<style type="text/css">
    .hiddencolumn
    {
        display: none;
    }
</style>
<script type="text/javascript" src="<%= Page.ResolveUrl("~/JavaScript/jquery-1.5.1.min.js") %>"></script>
<script type="text/javascript" src="<%= Page.ResolveUrl("~/JavaScript/jquery-ui-1.8.23.custom.min.js") %>"></script>
<script type="text/javascript">
    var allCheckBoxSelector = '#gvInvestorOwnedCPs input[id*="cbAllItems"]:checkbox';
    var checkBoxSelector = '#gvInvestorOwnedCPs input[id*="cbSelectItem"]:checkbox';


    $(document).ready(function () {
        // Style Buttons
        $("[id$=btnCommit]").button();
        $("[id$=btnPrintReceipt]").button();
        $("[id$=btnLetterOfAgreement]").button();
        $("[id$=btnCurrentSSANSearch]").button();


        // AUTOCOMPLETE FOR txtCurrentSSAN
        $("[id$=txtCurrentSSAN]").autocomplete({
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
                    $("[id$=txtCurrentSSAN]").val("");
                    event.preventDefault();
                }
                else {
                    $("[id$=txtCurrentSSAN]").val(ui.item.value);
                    $("[id$=btnCurrentSSANSearch]").click();
                }
            }
        });

        // AUTOCOMPLETE FOR txtNewSSAN
        $("[id$=txtNewSSAN]").autocomplete({
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
                    $("[id$=txtNewSSAN]").val("");
                    event.preventDefault();
                }
                else {
                    $("[id$=txtNewSSAN]").val(ui.item.value);
                    $("[id$=btnNewSSANSearch]").click();
                }
            }
        });


        // Setup checkbox events for Investor CP Grid
        $(allCheckBoxSelector).live('click', function () {
            $(checkBoxSelector).attr('checked', $(this).is(':checked'));

            ToggleCheckBoxes();
        });

        $(checkBoxSelector).live('click', ToggleCheckBoxes);




        ToggleCheckBoxes();

        $("[id$=btnLetterOfAgreement]").click(function (event, ui) {
            showLetterOfAgreementPopup("Letter Of Agreement");

            event.preventDefault();
        });
    });



    function ToggleCheckBoxes() {
        var totalCheckboxes = $(checkBoxSelector);

        var checkedCheckboxes = totalCheckboxes.filter(":checked");
        var noCheckboxesAreChecked = (checkedCheckboxes.length === 0);
        var allCheckboxesAreChecked = (totalCheckboxes.length === checkedCheckboxes.length);

        $(allCheckBoxSelector).attr('checked', allCheckboxesAreChecked);

        if (noCheckboxesAreChecked == true) {
            $("[id$=btnCommit]").button("disable");
        }
        else if ((noCheckboxesAreChecked == false) && ($("[id$=txtNewSSAN]").val().length > 0) && ($("[id$=txtCurrentSSAN]").val().length > 0)) {
            $("[id$=btnCommit]").button("enable");
        }

        // Column 2 is Fee column
        calculateSumOfFees(3);
    }


    function calculateSumOfFees(columnIndex) {
        var total = 0.0;

        $('input[id*="cbSelectItem"]:checkbox:checked').each(function () {
            var parentTR = $(this).parents('tr');
            var currencyAmount = parentTR.children().eq(columnIndex).text();

            // Strip out currency symbols
            var amount = Number(currencyAmount.replace(/[^0-9\.]+/g, ""));

            total += parseFloat(amount);
        });

        $("[id$=lblTotalAmount]").text(total.toFixed(2));
    }


    function showLetterOfAgreementPopup(type) {
        // Set up Add Remark Popup window
        $("#divUploadDocument").dialog({
            autoOpen: false,
            modal: true,
            title: "Upload Letter Of Agreement",
            minWidth: 404,
            buttons: {
                "Accept": function () {
                    $(this).dialog("close");
                    // $("[id$=btnAddNewLetterOfAgreement]").click();
                },
                "Cancel": function () {
                    $(this).dialog("close");
                }
            }
        }).parent().appendTo($("form:first"));


        $("#divUploadDocument").dialog("open");
    }  
</script>
<div id="divUploadDocument" class="divPopup">
    <fieldset>
        <asp:Label ID="lblUploadDocument" runat="server" AssociatedControlID="uplCPTransferDocument"
            Text="Document:" Style="display: block;"></asp:Label>
        <asp:FileUpload ID="uplCPTransferDocument" runat="server" Style="display: block;"
            Width="340px" />
    </fieldset>
</div>
<div>
    <table>
        <tr>
            <td>
                Current SSAN:
            </td>
            <td>
                <asp:TextBox ID="txtCurrentSSAN" ClientIDMode="Static" runat="server" />
                <asp:Button ID="btnCurrentSSANSearch" runat="server" Text="Search" CausesValidation="False"
                    OnClick="btnCurrentSSANSearch_Click" ClientIDMode="Static" Style="display: none;" />
            </td>
            <td width="150px">
            </td>
            <td>
                New SSAN:
            </td>
            <td>
                <asp:TextBox ID="txtNewSSAN" runat="server" ClientIDMode="Static" />
                <asp:Button ID="btnNewSSANSearch" runat="server" Text="Search" CausesValidation="False"
                    OnClick="btnNewSSANSearch_Click" Style="display: none;" />
            </td>
        </tr>
        <tr>
            <td>
                Current Investor Name:
            </td>
            <td>
                <asp:Label ID="lblCurrentInvestor" ClientIDMode="Static" runat="server" />
                <asp:Label ID="lblSessionID" runat="server" Visible ="false"  />
            </td>
            <td>
            </td>
            <td>
                New Investor Name:
            </td>
            <td>
                <asp:Label ID="lblNewInvestor" runat="server" ClientIDMode="Static" />
            </td>
        </tr>
        <tr>
            <td>
                Letter of Agreement:
            </td>
            <td>
                <asp:Button runat="server" ID="btnLetterOfAgreement" Text="Add" ClientIDMode="Static"
                    CausesValidation="false" />
                <%--<asp:Button ID="btnAddNewLetterOfAgreement" runat="server" Text="Add" ClientIDMode="Static"
                    CausesValidation="False" OnClick="btnAddNewLetterOfAgreement_Click" Style="display: none;" />--%>
                <br />
            </td>
            <td>
            </td>
            <td>
                &nbsp;
            </td>
            <td>
                &nbsp;
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <br />
                <div style="overflow: auto; height: 450px; width: 600px; border: 1px solid darkgrey;">
                    <asp:GridView ID="gvInvestorOwnedCPs" runat="server" AutoGenerateColumns="False"
                        ClientIDMode="Static">
                        <Columns>
                            <asp:TemplateField>
                                <HeaderTemplate>
                                    <asp:CheckBox runat="server" ID="cbAllItems" Text="Select" ClientIDMode="Static" />
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <asp:CheckBox ID="cbSelectItem" runat="server" class="cbSelectItem" ClientIDMode="Static" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="APN" HeaderText="Parcel" ItemStyle-Width ="200px">
<ItemStyle Width="200px" HorizontalAlign="Left"></ItemStyle>
                            </asp:BoundField>
                            <asp:BoundField DataField="CERTIFICATENUMBER" HeaderText="Certificate Number" />
                            <asp:BoundField DataField="FEE" HeaderText="Transfer Fee" DataFormatString="{0:c}" />
                            <asp:BoundField DataField="TAXYEAR" HeaderText="TaxYear" ItemStyle-CssClass="hiddencolumn"
                                HeaderStyle-CssClass="hiddencolumn" >
<HeaderStyle CssClass="hiddencolumn"></HeaderStyle>

<ItemStyle CssClass="hiddencolumn"></ItemStyle>
                            </asp:BoundField>
                            <asp:BoundField DataField="TAXROLLNUMBER" HeaderText="TaxRollNumber" ItemStyle-CssClass="hiddencolumn"
                                HeaderStyle-CssClass="hiddencolumn" >
<HeaderStyle CssClass="hiddencolumn"></HeaderStyle>

<ItemStyle CssClass="hiddencolumn"></ItemStyle>
                            </asp:BoundField>
                        </Columns>
                    </asp:GridView>
                </div>
                <br />
            </td>
            <td>
            </td>
            <td colspan="2">
                &nbsp;
            </td>
        </tr>
        <tr>
            <td>
                Total:
            </td>
            <td>
                $
                <asp:Label ID="lblTotalAmount" runat="server" Text="0.00" />
            </td>
            <td>
            </td>
            <td>
                &nbsp;
            </td>
            <td>
                &nbsp;
            </td>
        </tr>
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
        <tr>
            <td>
                <asp:Button ID="btnCommit" runat="server" ClientIDMode="Static" Text="Commit" OnClick="btnCommit_Click" />
            </td>
            <td>
                <asp:Button ID="btnPrintReceipt" runat="server" ClientIDMode="Static" Text="Print Receipt" />
            </td>
            <td>
            </td>
            <td>
                &nbsp;
            </td>
            <td>
                &nbsp;
            </td>
        </tr>
    </table>
</div>
