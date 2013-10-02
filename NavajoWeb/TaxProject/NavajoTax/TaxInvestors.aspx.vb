Imports System.Data
Imports System.Data.OleDb
Imports System.Drawing.Printing.PrintDocument
Imports System.Drawing.Printing
Imports System.Drawing

Partial Class TaxInvestors
    Inherits System.Web.UI.Page

    Private _fileType As String = String.Empty
    Private Const _SESSION_VAR_INVESTOR_ID As String = "TaxInvestorsASPX_InvestorID"
    Private _investorDataset As DataSet
    Dim CurrentTaxYearValue As String = String.Empty
    Dim ReportCurrentTaxYearDS As New DataSet

    Dim CashierRecordIDSessionID As Integer

    Private _priorMonthTaxID As String = String.Empty
    Private _priorMonthTaxRoll As String = String.Empty
    Private _priorMonthTaxYear As String = String.Empty

    Dim util As New Utilities()

    Private ReadOnly Property ConnectString As String
        Get
            Return ConfigurationManager.ConnectionStrings("ConnString").ConnectionString
        End Get
    End Property

#Region "Properties"
    Private ReadOnly Property CashierTransactionsTable() As DataTable
        Get
            Return InvestorDataset().Tables("CASHIER_TRANSACTIONS")
        End Get
    End Property

    Private ReadOnly Property ApportionDetailsTable As DataTable
        Get
            Return InvestorDataset.Tables("CASHIER_APPORTION")
        End Get
    End Property
    Public Property InvestorID As Integer
        Get
            Dim obj As Object = Session(_SESSION_VAR_INVESTOR_ID)

            If IsNumeric(obj) Then
                Return CInt(obj)
            Else
                Return 0
            End If
        End Get
        Set(value As Integer)
            Session(_SESSION_VAR_INVESTOR_ID) = value

            ' Reset subtax grid.
            Me.grdRegSubtax.DataSource = Nothing
            Me.grdRegSubtax.DataBind()
        End Set
    End Property


    Private Property InvestorDataset As DataSet
        Get
            If _investorDataset Is Nothing Then
                _investorDataset = New DataSet()
            End If

            If Not _investorDataset.Tables.Contains("ST_INVESTOR") Then
                LoadTable(_investorDataset, "ST_INVESTOR", "SELECT * FROM genii_user.ST_INVESTOR WHERE InvestorID = " & Me.InvestorID)
            End If

            If Not _investorDataset.Tables.Contains("ST_INVESTOR_CALENDAR") Then
                ' Load schema of all columns.
                LoadSchema(_investorDataset, "ST_INVESTOR_CALENDAR", "SELECT * FROM genii_user.ST_INVESTOR_CALENDAR")

                ' Load all columns except 
                LoadTable(_investorDataset, "ST_INVESTOR_CALENDAR", "SELECT * FROM genii_user.ST_INVESTOR_CALENDAR WHERE INVESTORID = " & Me.InvestorID)
            End If

            AddRelation(_investorDataset, "ST_INVESTOR", "InvestorID", "ST_INVESTOR_CALENDAR", "INVESTORID")

            Return _investorDataset
        End Get
        Set(value As DataSet)
            _investorDataset = value
        End Set
    End Property

    ''' <summary>
    ''' Returns ST_INVESTOR table from dataset.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property InvestorTable As DataTable
        Get
            Return Me.InvestorDataset.Tables("ST_INVESTOR")
        End Get
    End Property

    ''' <summary>
    ''' Returns row from ST_INVESTOR table for current <see cref="InvestorID">InvestorID</see>.
    ''' </summary>
    ''' <param name="errorLevel"></param>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property InvestorRow(Optional errorLevel As Integer = 0) As DataRow
        Get
            Dim rows As DataRow() = Me.InvestorTable.Select("InvestorID=" & Me.InvestorID)
            If rows.Length >= 1 Then
                Return rows(0)
            Else
                If errorLevel = 0 Then
                    ' Try reloading from database.
                    Me.InvestorDataset = Nothing
                    Return InvestorRow(1)
                Else
                    Return Nothing
                End If
            End If
        End Get
    End Property

    ''' <summary>
    ''' Return ST_INVESTOR_CALENDAR table from dataset.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property InvestorCalendarTable As DataTable
        Get
            Return Me.InvestorDataset.Tables("ST_INVESTOR_CALENDAR")
        End Get
    End Property

    ''' <summary>
    ''' Returns investor calendar view filtered to current <see cref="InvestorID">InvestorID</see>.
    ''' </summary>
    ''' <param name="errorLevel"></param>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property InvestorCalendarView(Optional errorLevel As Integer = 0) As DataView
        Get
            Dim vw As New DataView(Me.InvestorCalendarTable)
            vw.RowFilter = "INVESTORID=" & Me.InvestorID

            If vw.Count = 0 And errorLevel = 0 Then
                ' Try reloading from database.
                Me.InvestorDataset = Nothing
                Return InvestorCalendarView(1)
            End If

            Return vw
        End Get
    End Property


#End Region


#Region "Event Handlers"
    Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load



        If Not Me.IsPostBack Then
            LoadLoginInfo()
            PrepareControls()
            LoadCountyInfo()
            If Me.InvestorID > 0 Then
                LoadInvestorInfo()
                PrepareControls()
            End If
        End If
    End Sub



    Protected Sub Page_PreRender(sender As Object, e As System.EventArgs) Handles Me.PreRender
        ' Enable/disable buttons depending on whether investor is loaded or not.
        Dim investorLoaded As Boolean = (Me.InvestorID > 0)
        Me.btnRegAddRemark.Enabled = investorLoaded
        Me.btnRegIncomeStatement.Enabled = investorLoaded
        Me.btnRegHoldings.Enabled = investorLoaded
        Me.btnRegLoadSubtax.Enabled = investorLoaded
        Me.btnInvestorSummary.Enabled = investorLoaded
        Me.btnNoticeofExpiration.Enabled = investorLoaded

        LoadLogoutInfo()
    End Sub

    Private Sub LoadCountyInfo()

        Dim SQL As String = String.Format("SELECT parameter FROM genii_user.st_parameter WHERE parameter_name = 'SIGNATURE_BLOCK_TITLE'")

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCountyName As New DataTable()

            adt.Fill(tblCountyName)

            If tblCountyName.Rows.Count > 0 Then
                If (Not IsDBNull(tblCountyName.Rows(0)("parameter"))) Then
                    loadCountyTitle.InnerText = Convert.ToString(tblCountyName.Rows(0)("parameter"))
                End If

            End If
        End Using
    End Sub

    Protected Sub btnRegSearch_Click(sender As Object, e As System.EventArgs) Handles btnRegSearch.Click
        Me.InvestorID = CInt(Me.txtRegInvestorID.Text)
        If Me.InvestorRow Is Nothing Then
            ' Not found.
            Throw New ApplicationException("Investor not found. ID:" & Me.InvestorID)
        Else
            LoadInvestorInfo()
        End If
    End Sub

    Protected Sub btnRegAddNew_Click(sender As Object, e As System.EventArgs) Handles btnRegAddNew.Click
        AddNewInvestor(Me.txtRegSSAN.Text)
        LoadInvestorInfo()
    End Sub
    Public Function GetCurrentTaxYearValue() As Integer
        Dim myTaxYearValue As String = String.Empty

        '   If (Not (TaxYear > 0)) Then
        Dim SQL As String = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE PARAMETER_NAME = 'CURRENT_TAXYEAR'")

        LoadTable(ReportCurrentTaxYearDS, "ST_PARAMETER", SQL)

        Dim row As DataRow = ReportCurrentTaxYearDS.Tables(0).Rows(0)

        myTaxYearValue = IIf(IsDBNull(row("PARAMETER")), String.Empty, row("PARAMETER"))
        '  End If

        Return myTaxYearValue
    End Function
    Protected Sub btnRegSave_Click(sender As Object, e As System.EventArgs) Handles btnRegSave.Click
        Dim row As DataRow = Me.InvestorRow
        If row Is Nothing Then
            ' Investor is not loaded. Search for existing record or add new.
            Dim ssn As String = Me.txtRegSSAN.Text
            Dim investors As Generic.Dictionary(Of String, String) = GetInvestor(ssn)
            If investors.Count > 0 Then
                For Each key As String In investors.Keys
                    If IsNumeric(key) Then
                        Me.InvestorID = CInt(key)
                        Exit For
                    End If
                Next
                If Me.InvestorID = 0 Then
                    AddNewInvestor(ssn)
                End If
            Else
                ' SSN not found in investor table. Add new.
                AddNewInvestor(ssn)
            End If
        Else
            ' Investor is loaded in session. If SSN has changed, confirm whether
            ' user is updating investor's SSN or adding a new investor.
            Dim oldSSN As String = row.Item("SocialSecNum").ToString()
            Dim newSSN As String = Me.txtRegSSAN.Text
            Dim ssnChanged As Boolean = IsSSANDifferent(oldSSN, newSSN)

            If ssnChanged And String.IsNullOrEmpty(Me.hdnSaveAction.Value) Then
                ' Show dialog to confirm user action.
                Me.lblRegPrevSSAN.Text = oldSSN
                Me.lblRegNewSSAN.Text = newSSN

                Page.ClientScript.RegisterStartupScript(Me.GetType, "showAddOrSaveDialog", "showAddOrSaveDialog();", True)
                Exit Sub
            ElseIf ssnChanged And Me.hdnSaveAction.Value = "add" Then
                AddNewInvestor(newSSN)
            End If
        End If

        SaveInvestorInfo()
        LoadInvestorInfo()

        Me.hdnSaveAction.Value = String.Empty
    End Sub

    Protected Sub btnRegClear_Click(sender As Object, e As System.EventArgs) Handles btnRegClear.Click
        Me.InvestorID = 0

        LoadInvestorInfo()
    End Sub

    Protected Sub btnRegAddNewRemark_Click(sender As Object, e As System.EventArgs) Handles btnRegAddNewRemark.Click
        AddInvestorRemark()
    End Sub

    Protected Sub btnRegLoadSubtax_Click(sender As Object, e As System.EventArgs) Handles btnRegLoadSubtax.Click
        BindSubtaxGrid()
    End Sub

    Protected Sub btnSaveSubtax_Click(sender As Object, e As System.EventArgs) Handles btnSaveSubtax.Click
        If (ddlPaymentType.SelectedValue = 1 Or ddlPaymentType.SelectedValue = 3) Then
            If (txtCheckNumber.Text = String.Empty) Then
                Dim Caller As Control = Me
                ScriptManager.RegisterStartupScript(Caller, [GetType](), "Check Number", "showMessage('CheckNumber must not be null', 'Check Number');", True)
            Else
                SaveSubtax()
            End If
        End If
    End Sub
#End Region


    Public Function PreparePrintDocument(forPayment As String) As Printing.PrintDocument
        ' Make the PrintDocument object.
        Dim print_document As New Printing.PrintDocument
        'forRegularPayment
        'forRedeemFromState
        'Dim chkInvestor As CheckBox = grdRegSubtax.HeaderRow.FindControl("chkSubtaxAll")

        'Dim v1 As Integer = grdRegSubtax.Rows.Count
        'Dim taxYear As String
        'Dim taxRoll As String
        'Dim taxID As String


        'If (chkInvestor.Checked) Then
        '    For z1 = 0 To (v1 - 1)
        '        Dim chkInvestorCP As CheckBox = grdRegSubtax.Rows(z1).FindControl("chkSubtax")

        '        chkInvestorCP.Checked = True

        '    Next
        'End If

        'For x1 = 0 To (v1 - 1)
        '    Dim chkInvestorCP As CheckBox = grdRegSubtax.Rows(x1).FindControl("chkSubtax")
        '    If (chkInvestorCP.Checked) Then
        '        _priorMonthTaxID = grdRegSubtax.Rows(x1).Cells(2).Text


        '    End If
        'Next

        AddHandler print_document.PrintPage, AddressOf DrawStringPointF_PRINTHEADER
        AddHandler print_document.PrintPage, AddressOf DrawStringPointF

        ' Return the object.
        Return print_document
    End Function

    Public Sub DrawStringPointF_PRINTHEADER(ByVal sender As Object, ByVal e As PrintPageEventArgs)
        Dim SQL As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='SIGNATURE_BLOCK_TITLE' ")
        Dim sigBlockTitle As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblParameter As New DataTable()

            adt2.Fill(tblParameter)

            If tblParameter.Rows.Count > 0 Then

                For x = 0 To (tblParameter.Rows.Count - 1)
                    If (Not IsDBNull(tblParameter.Rows(x)("parameter"))) Then
                        sigBlockTitle = Convert.ToString(tblParameter.Rows(x)("parameter"))
                    End If
                Next
            End If
        End Using

        Dim SQL2 As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='SIGNATURE_BLOCK_NAME' ")
        Dim sigBlockName As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQL2, Me.ConnectString)
            Dim tblParameter2 As New DataTable()

            adt2.Fill(tblParameter2)

            If tblParameter2.Rows.Count > 0 Then

                For x = 0 To (tblParameter2.Rows.Count - 1)
                    If (Not IsDBNull(tblParameter2.Rows(x)("parameter"))) Then
                        sigBlockName = Convert.ToString(tblParameter2.Rows(x)("parameter"))
                    End If
                Next
            End If
        End Using

        Dim SQL3 As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='ADDRESS' ")
        Dim sigBlockAddress As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQL3, Me.ConnectString)
            Dim tblParameter3 As New DataTable()

            adt2.Fill(tblParameter3)

            If tblParameter3.Rows.Count > 0 Then

                For x = 0 To (tblParameter3.Rows.Count - 1)
                    If (Not IsDBNull(tblParameter3.Rows(x)("parameter"))) Then
                        sigBlockAddress = Convert.ToString(tblParameter3.Rows(x)("parameter"))
                    End If
                Next
            End If
        End Using

        Dim SQL4 As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='CITY_STATE_ZIP' ")
        Dim sigBlockCityStateZip As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQL4, Me.ConnectString)
            Dim tblParameter4 As New DataTable()

            adt2.Fill(tblParameter4)

            If tblParameter4.Rows.Count > 0 Then

                For x = 0 To (tblParameter4.Rows.Count - 1)
                    If (Not IsDBNull(tblParameter4.Rows(x)("parameter"))) Then
                        sigBlockCityStateZip = Convert.ToString(tblParameter4.Rows(x)("parameter"))
                    End If
                Next
            End If
        End Using

        Dim printFont10B = New Font("Arial", 9, FontStyle.Bold)
        Dim printFont9R = New Font("Arial", 9, FontStyle.Regular)
        Dim rect1 As New Rectangle(10, 10, 270, 250)

        Dim stringFormat As New StringFormat()
        stringFormat.Alignment = StringAlignment.Center
        stringFormat.LineAlignment = StringAlignment.Center

        Dim stringFormatNear As New StringFormat()
        stringFormatNear.Alignment = StringAlignment.Near
        stringFormatNear.LineAlignment = StringAlignment.Center
        Dim tabs As Single() = {100}
        stringFormatNear.SetTabStops(0, tabs)


        Dim y As String = String.Empty

        ' Dim defaultHeader As String() = {"---------------------------------------", sigBlockTitle, sigBlockName, sigBlockAddress, sigBlockCityStateZip, Date.Now, "Operator - "}
        Dim defaultHeader As String() = {"-----------------------------------------------------", "Operator - " & System.Web.HttpContext.Current.User.Identity.Name, Date.Now, sigBlockCityStateZip, sigBlockAddress, sigBlockName, sigBlockTitle, "-----------------------------------------------------"}

        For i = 0 To defaultHeader.Count - 1
            y = y & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(defaultHeader(i) & y, printFont10B, Brushes.Black, rect1, stringFormat)
        Next

    End Sub

    Public Sub DrawStringPointF(ByVal sender As Object, ByVal e As PrintPageEventArgs)


        Dim remainingBalance As Double
        Dim interestPaidToday As Double
        Dim feesPaidToday As Double
        Dim taxesPaidToday As Double
        Dim paidToday As Double

        Dim priorBalanceDue As Double
        Dim previousPayment As Double
        Dim totalInterest As Double
        Dim totalFees As Double
        Dim taxes As Double

        Dim Tax_Year As String = String.Empty
        Dim taxRollNumber As String = String.Empty
        Dim certNumber As String = String.Empty
        Dim dateOfSale As String = String.Empty
        Dim bidRate As String = String.Empty

        Dim PaymentDescription As String = String.Empty
        Dim WhoPaid As String = String.Empty


        Dim SQL5 As String = String.Format("SELECT genii_user.ST_INVESTOR.LastName + ' (' + CONVERT(varchar, genii_user.TR_CP.InvestorID) + ')' AS 'Investor',  " & _
                                            " CONVERT(varchar, genii_user.TR_CP.MonthlyRateOfInterest*100) + '%' AS 'Bid Rate',  " & _
                                            "   genii_user.TR_CP.APN AS 'Parcel',   " & _
                                            "         '$' + CONVERT(varchar, (genii_user.TR_CHARGES.ChargeAmount), 1) AS 'Current Balance',  " & _
                                            "         '$' + CONVERT(varchar, genii_user.TR_CHARGES.ChargeAmount, 1) AS 'Interest',  " & _
                                            "         '$' + CONVERT(varchar, 5, 1) AS 'Subtax Fee',  " & _
                                            "         '$' + CONVERT(varchar, genii_user.TR.CurrentBalance+5, 1) AS 'Total',  " & _
                                            "         genii_user.TR_CP.CertificateNumber, genii_user.TR.taxrollnumber, genii_user.TR.taxyear, genii_user.TR.TaxRollNumber, genii_user.TR_CP.dateOfSale " & _
                                            "         FROM genii_user.TR_CP " & _
                                            "   INNER JOIN genii_user.ST_INVESTOR ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID  " & _
                                            "     INNER JOIN genii_user.TR ON genii_user.TR_CP.APN = genii_user.TR.APN  " & _
                                            "       INNER JOIN genii_user.TR_CHARGES ON genii_user.TR.TaxYear = genii_user.TR_CHARGES.TaxYear  " & _
                                            "         AND genii_user.TR.TaxRollNumber = genii_user.TR_CHARGES.TaxRollNumber  " & _
                                            "         WHERE genii_user.TR_CP.DATE_REDEEMED Is NULL " & _
                                            "   AND genii_user.TR_CP.InvestorID= " + lblRegInvestorID.Text + " " & _
                                            "   AND genii_user.TR_CP.APN= '" + _priorMonthTaxID + "' " & _
                                            "     AND  genii_user.TR_CHARGES.TaxChargeCodeID = 99901 " & _
                                            "       AND genii_user.TR_CP.TaxYear = DATEPART(yyyy, GETDATE())-2  " & _
                                            "         AND genii_user.TR_CP.InvestorID <> 1  " & _
                                            "           AND genii_user.TR_CP.APN IN  " & _
                                            "             (SELECT TR_1.APN FROM genii_user.TR AS TR_1  " & _
                                            "               CROSS JOIN genii_user.TR_CP AS TR_CP_1  " & _
                                            "         WHERE TR_1.TaxYear = DatePart(yyyy, GETDATE()) - 1 " & _
                                            "                   AND TR_1.SecuredUnsecured = 'S'  )  " & _
                                            "                       AND genii_user.TR.TaxYear = DATEPART(yyyy, GETDATE())-1")

        Using adt As New OleDbDataAdapter(SQL5, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("Current Balance"))) Then
                    taxes = dv(0)("Current Balance").ToString()
                Else
                    taxes = "0.00"
                End If

                If (Not IsDBNull(dv(0)("Interest"))) Then
                    totalInterest = dv(0)("Interest").ToString()
                Else
                    totalInterest = "0.00"
                End If

                If (Not IsDBNull(dv(0)("Subtax Fee"))) Then
                    totalFees = dv(0)("Subtax Fee").ToString()
                Else
                    totalFees = "0.00"
                End If

                If (Not IsDBNull(dv(0)("TaxRollNumber"))) Then
                    taxRollNumber = dv(0)("TaxRollNumber").ToString()
                Else
                    taxRollNumber = "N/A"
                End If

                If (Not IsDBNull(dv(0)("TaxYear"))) Then
                    Tax_Year = dv(0)("TaxYear").ToString()
                Else
                    Tax_Year = "N/A"
                End If

                If (Not IsDBNull(dv(0)("CertificateNumber"))) Then
                    certNumber = dv(0)("CertificateNumber").ToString()
                Else
                    certNumber = "N/A"
                End If

                If (Not IsDBNull(dv(0)("DateOfSale"))) Then
                    dateOfSale = dv(0)("DateOfSale").ToString()
                Else
                    dateOfSale = "N/A"
                End If

                If (Not IsDBNull(dv(0)("Bid Rate"))) Then
                    bidRate = dv(0)("Bid Rate").ToString()
                Else
                    bidRate = "N/A"
                End If
            End If
        End Using

        'Dim SQL6 As String = String.Format("Select a.payment_amt,a.kitty_amt, a.refund_amt, b.paymentDescription,c.descriptionOfPayer " & _
        '                                  " from genii_user.cashier_transactions a, genii_user.st_payment_instrument b, genii_user.st_who_paid c" & _
        '                                 " where a.payment_type= b.paymentTypeCode and a.apply_to=c.paymentMadeByCode and a.tax_year =" + Tax_Year + " and a.Tax_Roll_Number=" + taxRollNumber + " and a.transaction_status = 1")

        'Using adt As New OleDbDataAdapter(SQL6, Me.ConnectString)
        '    Dim tblReceiptDetails As New DataTable()

        '    adt.Fill(tblReceiptDetails)

        '    If tblReceiptDetails.Rows.Count > 0 Then
        '        Dim dv As DataView = New DataView(tblReceiptDetails)
        '        If (Not IsDBNull(dv(0)("Payment_amt"))) Then
        '            paidToday = dv(0)("Payment_amt").ToString()
        '            'Else
        '            '    lblPhysicalAddress.Text = "N/A"
        '        End If

        '        If (Not IsDBNull(dv(0)("paymentDescription"))) Then
        '            PaymentDescription = dv(0)("paymentDescription").ToString()
        '        End If

        '        If (Not IsDBNull(dv(0)("descriptionOfPayer"))) Then
        '            WhoPaid = dv(0)("descriptionOfPayer").ToString()
        '        End If
        '    End If
        'End Using


        'Dim SQL7 As String = String.Format("SELECT TaxYear, TaxRollNumber, SUM(CASE WHEN taxtypeid <= 40 THEN dollarAmount ELSE 0 END) AS Taxes,  " & _
        '                                    " SUM(CASE WHEN taxtypeid = 80 THEN dollarAmount ELSE 0 END) AS Interest,   " & _
        '                                    " SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN dollarAmount ELSE 0 END) AS Fees   " & _
        '                                    "         FROM genii_user.cashier_apportion " & _
        '                                    "         where taxYear = " + _priorMonthTaxYear + " And taxRollNumber = " + _priorMonthTaxRoll + " " & _
        '                                    " GROUP BY TaxYear, TaxRollNumber")

        'Using adt As New OleDbDataAdapter(SQL7, Me.ConnectString)
        '    Dim tblReceiptDetails As New DataTable()

        '    adt.Fill(tblReceiptDetails)

        '    If tblReceiptDetails.Rows.Count > 0 Then
        '        Dim dv As DataView = New DataView(tblReceiptDetails)
        '        If (Not IsDBNull(dv(0)("Taxes"))) Then
        '            taxesPaidToday = dv(0)("Taxes").ToString()
        '        Else
        '            taxesPaidToday = "0.00"
        '        End If

        '        If (Not IsDBNull(dv(0)("Interest"))) Then
        '            interestPaidToday = dv(0)("Interest").ToString()
        '        Else
        '            interestPaidToday = "0.00"
        '        End If

        '        If (Not IsDBNull(dv(0)("Fees"))) Then
        '            feesPaidToday = dv(0)("Fees").ToString()
        '        Else
        '            feesPaidToday = "0.00"
        '        End If

        '    End If
        'End Using

        'Dim SQL8 As String = String.Format("SELECT * from genii_user.TR " & _
        '                                   "         where taxYear = " + _priorMonthTaxYear + " And taxRollNumber = " + _priorMonthTaxRoll + " ")

        'Using adt As New OleDbDataAdapter(SQL8, Me.ConnectString)
        '    Dim tblReceiptDetails As New DataTable()

        '    adt.Fill(tblReceiptDetails)

        '    If tblReceiptDetails.Rows.Count > 0 Then
        '        Dim dv As DataView = New DataView(tblReceiptDetails)
        '        If (Not IsDBNull(dv(0)("CurrentBalance"))) Then
        '            remainingBalance = dv(0)("CurrentBalance").ToString()
        '        Else
        '            remainingBalance = "0.00"
        '        End If

        '    End If
        'End Using


        'priorBalanceDue = taxesPaidToday + feesPaidToday + interestPaidToday
        'With Me.TaxRollMaster
        '    previousPayment = .GetTotalPayments2(_priorMonthTaxYear, _priorMonthTaxRoll)
        '    If (IsDBNull(previousPayment)) Then
        '        previousPayment = Math.Round(0.0, 2)
        '    End If
        'End With

        'Dim gvr As GridViewRow
        'gvr = dtaSummary.SelectedRow

        'Dim taxRollnumber = gvr.Cells(2).Text

        Dim taxYear As Integer = GetCurrentTaxYearValue()


        Dim printFont10B = New Font("Arial", 9, FontStyle.Bold)
        Dim printFont9R = New Font("Arial", 9, FontStyle.Regular)
        Dim rect1 As New Rectangle(10, 10, 270, 250)

        Dim rect2a As New Rectangle(10, 50, 270, 250)

        Dim rect2 As New Rectangle(10, 100, 270, 250)

        Dim rect3 As New Rectangle(10, 150, 270, 250)

        Dim rect4 As New Rectangle(10, 170, 270, 400)

        Dim rect5 As New Rectangle(10, 200, 270, 400)

        Dim rect6 As New Rectangle(10, 250, 270, 400)


        Dim stringFormat As New StringFormat()
        stringFormat.Alignment = StringAlignment.Center
        stringFormat.LineAlignment = StringAlignment.Center

        Dim stringFormatNear As New StringFormat()
        stringFormatNear.Alignment = StringAlignment.Near
        stringFormatNear.LineAlignment = StringAlignment.Center
        Dim tabs As Single() = {100}
        stringFormatNear.SetTabStops(0, tabs)


        Dim y As String = String.Empty
        Dim z As String = String.Empty
        Dim a As String = String.Empty
        Dim b As String = String.Empty
        Dim paymentDetails As String() = {"Subsequent Tax", "Receipt for Payment of "}
        z = String.Empty
        For i = 0 To paymentDetails.Count - 1
            z = z & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails(i) & z, printFont9R, Brushes.Black, rect2a, stringFormat)
        Next

        Dim paymentDetails2 As String() = {"Certificate of Purchase: " & certNumber, "Purchaser: " & lblRegInvestorID.Text & " - " & txtPayorName.Text}

        For i = 0 To paymentDetails2.Count - 1
            a = a & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(vbNewLine & vbNewLine & paymentDetails2(i) & a, printFont9R, Brushes.Black, rect2, stringFormat)
        Next

        Dim paymentDetails1 As String() = {"Payment applied to the " & taxYear & " Tax Year", "Thank you for your Payment of: $" & (taxes + totalFees)}
        z = String.Empty
        For i = 0 To paymentDetails1.Count - 1
            z = z & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails1(i) & z, printFont9R, Brushes.Black, rect3, stringFormat)
        Next

        Dim paymentDetails3 As String() = {"Parcel / Tax ID: " & _priorMonthTaxID, "Tax Roll: " & taxRollNumber, "Tax Year: " & taxYear}
        b = String.Empty
        For j = 0 To paymentDetails3.Count - 1
            b = b & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails3(j) & b, printFont9R, Brushes.Black, rect4, stringFormat)
        Next

        Dim paymentDetails3B As String() = {"Rate: " & bidRate, "Original Date of Sale: " & dateOfSale}
        b = String.Empty
        For j = 0 To paymentDetails3B.Count - 1
            b = b & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails3B(j) & b, printFont9R, Brushes.Black, rect5, stringFormat)
        Next

        Dim paymentReceipt1 As String() = {"- - -", "Total Paid: " & vbTab & vbTab & "$" & (taxes + totalFees), "Investor Fee: " & vbTab & vbTab & "$" & totalFees, "Investment: " & vbTab & vbTab & "$" & taxes}
        b = String.Empty
        For j = 0 To paymentReceipt1.Count - 1
            b = b & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentReceipt1(j) & b, printFont9R, Brushes.Black, rect6, stringFormatNear)
        Next


        ''-------------------------------------------------------------------------------------------------------------

    End Sub


    ''' <summary>
    ''' Loads existing user session if available. Calls StartNewSession() if not.
    ''' </summary>
    ''' <remarks>
    ''' Labels displaying session information are filled here.
    ''' </remarks>
    Private Sub LoadLoginInfo()
        ' Look for existing session.
        Dim userName As String = System.Web.HttpContext.Current.User.Identity.Name
        Dim loginTime As Date, startCash As Decimal
        Dim sqlGetSession As String = "SELECT * FROM genii_user.CASHIER_SESSION WHERE CASHIER = ? AND END_TIME IS NULL ORDER BY START_TIME DESC"


        Using adt As New OleDbDataAdapter(sqlGetSession, util.ConnectString)
            adt.SelectCommand.Parameters.AddWithValue("@CASHIER", userName)

            Dim dt As New DataTable()

            adt.Fill(dt)

            If dt.Rows.Count = 0 Then
                StartNewSession()
            Else
                ''SessionRecordID = dt.Rows(0)("RECORD_ID")
                loginTime = dt.Rows(0)("START_TIME")
                startCash = dt.Rows(0)("START_CASH")

                ' Header
                Me.lblOperatorName.Text = System.Web.HttpContext.Current.User.Identity.Name
                Me.lblCurrentDate.Text = Date.Today.ToShortDateString()
                Me.lblLoginTime.Text = loginTime.ToString("g")
                Me.lblStartCash.Text = startCash.ToString("C")
                Me.lblLogoutUsername.Text = System.Web.HttpContext.Current.User.Identity.Name

                CashierRecordIDSessionID = dt.Rows(0)("RECORD_ID")
                Me.lblSessionID.Text = dt.Rows(0)("RECORD_ID")

                ' Pending payments tab
                ''Me.lblPendCashier.Text = userName
                '' Me.lblPendLogin.Text = loginTime.ToString()
            End If
        End Using

    End Sub

    ''' <summary>
    ''' Prepares the logout dialog by filling in available cash in register.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub LoadLogoutInfo()
        ' Get cash in register.
        Dim startCash As Decimal

        If IsNumeric(Me.lblStartCash.Text) Then
            startCash = CDec(Me.lblStartCash.Text)
        Else
            startCash = 0
        End If

        'Dim cashTransactions As Object = Me.CashierTransactionsTable().Compute("SUM(PAYMENT_AMT)", "PAYMENT_TYPE = 2")

        'If IsNumeric(cashTransactions) Then
        'startCash = startCash + CDec(cashTransactions)
        'End If

        Me.txtLogoutEndCash.Text = startCash.ToString()
        ''Me.lblPendCashBoxBalance.Text = startCash.ToString("C")
    End Sub



    ''' <summary>
    ''' Prepares login dialog by filling in cash left in register in last session.
    ''' Login dialog is then opened.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub StartNewSession()
        ' Get cash in register.
        Dim userName As String = System.Web.HttpContext.Current.User.Identity.Name

        Using conn As New OleDbConnection(util.ConnectString)
            Dim cmd As New OleDbCommand()

            cmd.CommandText = "SELECT TOP 1 END_CASH FROM genii_user.CASHIER_SESSION WHERE CASHIER = ? ORDER BY END_TIME DESC"
            cmd.Connection = conn
            cmd.Parameters.AddWithValue("@CASHIER", userName)

            conn.Open()

            Dim startCash As Object = cmd.ExecuteScalar()

            If IsNumeric(startCash) Then
                Me.txtLoginStartCash.Text = startCash
            Else
                Me.txtLoginStartCash.Text = String.Empty
            End If
        End Using

        Me.lblLoginUsername.Text = userName

        ClientScript.RegisterStartupScript(Me.GetType, "Login", "$(document).ready(function() { showLoginDialog(); });", True)
    End Sub

    'Private Function GetTaxYear()

    'End Function

#Region "Web Service Methods"

    <System.Web.Services.WebMethod()> _
    Public Shared Function btnPopupResult() As Boolean

        Dim a As String = "mia"
        'place data in gridview of popup



        Return True

    End Function

    ''' <summary>
    ''' Search investor table by investor id or ssan
    ''' </summary>
    ''' <param name="InvestorIDorSSAN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <System.Web.Services.WebMethod()> _
    Public Shared Function GetInvestor(InvestorIDorSSAN As String) As Generic.Dictionary(Of String, String)
        ' Extract numerals from ssan.
        Dim ssn As String = ExtractSSANNumerals(InvestorIDorSSAN)
        Dim SQL As String = String.Empty
        Dim cmd As New OleDbCommand()
        Dim result As New Generic.Dictionary(Of String, String)

        Dim myUtil As New Utilities()

        ' Search database.
        Using conn As New OleDbConnection(myUtil.ConnectString)

            If (Not String.IsNullOrEmpty(ssn)) Then
                SQL = "SELECT TOP 10 InvestorID, SocialSecNum, FirstName, MiddleName, LastName FROM genii_user.ST_INVESTOR " & _
                      "WHERE InvestorID = ? OR REPLACE(SocialSecNum,'-','') LIKE ? "

                cmd = New OleDbCommand(SQL)

                cmd.Parameters.AddWithValue("@InvestorID", ssn)
                cmd.Parameters.AddWithValue("@SocialSecNum", ssn & "%")
            Else
                '' SSAN is empty try to search by lastname
                If (Not String.IsNullOrEmpty(InvestorIDorSSAN.Trim())) Then
                    SQL = "SELECT TOP 10 InvestorID, SocialSecNum, FirstName, MiddleName, LastName FROM genii_user.ST_INVESTOR " & _
                     "WHERE LastName LIKE ?"

                    cmd = New OleDbCommand(SQL)
                    cmd.Parameters.AddWithValue("@LastName", "%" & InvestorIDorSSAN & "%")
                End If
            End If

            'Dim cmd As New OleDbCommand("SELECT TOP 10 InvestorID, SocialSecNum, FirstName, MiddleName, LastName FROM genii_user.ST_INVESTOR " & _
            '                            "WHERE InvestorID = ? OR REPLACE(SocialSecNum,'-','') LIKE ? " & _
            '                            "OR LastName LIKE ?")

            'cmd.Parameters.AddWithValue("@InvestorID", ssn)
            'cmd.Parameters.AddWithValue("@SocialSecNum", ssn & "%")
            'cmd.Parameters.AddWithValue("@LastName", "%" & InvestorIDorSSAN & "%")

            If (Not String.IsNullOrEmpty(ssn) Or (Not String.IsNullOrEmpty(InvestorIDorSSAN.Trim()))) Then
                cmd.Connection = conn
                conn.Open()

                Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                While rdr.Read()
                    Dim investorID As Integer = rdr.GetInt32(0)
                    Dim socialSecNum As Object = rdr.GetValue(1)
                    Dim firstName As Object = rdr.GetValue(2)
                    Dim middleName As Object = rdr.GetValue(3)
                    Dim lastName As Object = rdr.GetValue(4)

                    Dim resultString As New StringBuilder()

                    resultString.AppendFormat("{0}, ", investorID)

                    If Not IsDBNull(socialSecNum) Then
                        resultString.AppendFormat("{0}, ", socialSecNum)
                    End If

                    If Not IsDBNull(firstName) Then
                        resultString.AppendFormat("{0} ", firstName)
                    End If

                    If Not IsDBNull(middleName) Then
                        resultString.AppendFormat("{0} ", middleName)
                    End If

                    If Not IsDBNull(lastName) Then
                        resultString.AppendFormat("{0}", lastName)
                    End If

                    result.Add(investorID.ToString(), resultString.ToString())
                End While

                If result.Count = 0 Then
                    result.Add("0", "Add New")
                End If

            End If

            Return result
        End Using
    End Function
    Public Function CountWords(ByVal value As String) As Integer
        ' Count matches.
        Dim collection As MatchCollection = Regex.Matches(value, "\S+")
        Return collection.Count
    End Function


    <System.Web.Services.WebMethod()> _
    Public Shared Function GetInvestorTR(InvestorIDorSSAN As String, TaxYear As String, SearchParam As String, BalanceOnly As Boolean, Name1 As Boolean, Drop As Boolean, CheckTaxYear As Boolean, TextTaxYear As String) As Generic.Dictionary(Of String, String)
        '
        ' Extract numerals from ssan.
        Dim ssn As String = ExtractSSANNumerals(InvestorIDorSSAN)
        Dim SQL As String = String.Empty
        Dim cmd As New OleDbCommand()
        Dim result As New Generic.Dictionary(Of String, String)
        Dim balanceQuery As String = String.Empty
        Dim nameQuery As String = String.Empty
        Dim dropDownVal As String = String.Empty
        Dim taxYearQuery As String = String.Empty
        Dim mailQuery As String = String.Empty
        ' Dim TaxYear As String = GetTaxYear()

        Dim myUtil As New Utilities()

        ' Search database.
        Using conn As New OleDbConnection(myUtil.ConnectString)

            If (Drop = True) Then
                SQL = "SELECT Parameter from genii_user.ST_PARAMETER where parameter_name='Query Search DropDown Value'"

                Using adt As New OleDbDataAdapter(SQL, myUtil.ConnectString)
                    Dim tblDropDownVal As New DataTable()

                    adt.Fill(tblDropDownVal)

                    If tblDropDownVal.Rows.Count > 0 Then
                        If (Not IsDBNull(tblDropDownVal.Rows(0)("parameter"))) Then
                            dropDownVal = Convert.ToString(tblDropDownVal.Rows(0)("parameter"))
                        End If
                    Else
                        dropDownVal = "TOP 5"
                    End If
                End Using
            Else
                dropDownVal = " "
            End If

            If (Not String.IsNullOrEmpty(ssn)) Then
                SQL = "select " & dropDownVal & " row_number() over (order by taxIDnumber desc)as row,a.* from (SELECT  distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                      "WHERE TaxYear=? OR REPLACE(TaxIDNumber,'-','') LIKE ? ) a"

                cmd = New OleDbCommand(SQL)
                cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                ' cmd.Parameters.AddWithValue("@TaxRollNumber", ssn)
                cmd.Parameters.AddWithValue("@TaxIDNumber", ssn & "%")
            Else

                'InvestorIDorSSAN

                Dim collection As MatchCollection = Regex.Matches(InvestorIDorSSAN, "\S+")
                Dim countInvestorWords As Integer = collection.Count

                If (countInvestorWords > 1) Then
                    If (Not String.IsNullOrEmpty(InvestorIDorSSAN.Trim())) Then
                        If (BalanceOnly = True) Then
                            balanceQuery = " CurrentBalance > 0 "
                        Else
                            balanceQuery = " CurrentBalance = 0 "
                        End If

                        If (CheckTaxYear = True) Then
                            taxYearQuery = "and TaxYear >= " + TextTaxYear + " "
                            '  Else
                            '    taxYearQuery = "and TaxYear = " + TaxYear + " "
                        End If

                        Dim words As String() = InvestorIDorSSAN.Split(New Char() {" "c})


                        If (SearchParam = "Name") Then

                            If (Name1 = True) Then

                                Dim word As String
                                For Each word In words
                                    nameQuery = nameQuery & " and OWNER_NAME_1 LIKE ?"
                                Next

                                '    nameQuery = " and OWNER_NAME_1 LIKE ?"
                                SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE " & " " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                                cmd = New OleDbCommand(SQL)
                                '  cmd.Parameters.AddWithValue("@TaxYear", TaxYear)

                                Dim cmdQuery As String
                                For Each cmdQuery In words
                                    cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & cmdQuery & "%")
                                Next



                            Else
                                Dim word As String
                                For Each word In words
                                    nameQuery = nameQuery & " and OWNER_NAME_1 LIKE ? OR OWNER_NAME_2 LIKE ? OR OWNER_NAME_3 LIKE ?"
                                Next

                                ' nameQuery = " and OWNER_NAME_1 LIKE ? OR OWNER_NAME_2 LIKE ? OR OWNER_NAME_3 LIKE ?"

                                SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                           "WHERE " & " " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                                cmd = New OleDbCommand(SQL)

                                Dim cmdQuery As String
                                For Each cmdQuery In words
                                    ' cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                                    cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & cmdQuery & "%")
                                    cmd.Parameters.AddWithValue("@OWNER_NAME_2", "%" & cmdQuery & "%")
                                    cmd.Parameters.AddWithValue("@OWNER_NAME_3", "%" & cmdQuery & "%")
                                Next

                            End If



                        ElseIf (SearchParam = "Address") Then
                            Dim word As String
                            For Each word In words
                                mailQuery = mailQuery & " and mail_address_1 like ? or mail_address_2 like ?  "
                            Next


                            SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                        "WHERE  " & balanceQuery & " " & taxYearQuery & " " & mailQuery & ") a"

                            cmd = New OleDbCommand(SQL)
                            Dim cmdQuery As String
                            For Each cmdQuery In words
                                cmd.Parameters.AddWithValue("@mail_address_1", "%" & cmdQuery & "%")
                                cmd.Parameters.AddWithValue("@mail_address_2", "%" & cmdQuery & "%")
                            Next

                        ElseIf (SearchParam = "Both") Then
                            If (Name1 = True) Then
                                Dim word As String
                                For Each word In words
                                    nameQuery = nameQuery & " and OWNER_NAME_1 LIKE ?"
                                    mailQuery = mailQuery & " and mail_address_1 like ? or mail_address_2 like ?  "
                                Next

                                SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE " & balanceQuery & " " & taxYearQuery & " " & mailQuery & ") a"

                                cmd = New OleDbCommand(SQL)
                                Dim cmdQuery As String
                                For Each cmdQuery In words
                                    '  cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                                    cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & cmdQuery & "%")
                                    cmd.Parameters.AddWithValue("@mail_address_1", "%" & cmdQuery & "%")
                                    cmd.Parameters.AddWithValue("@mail_address_2", "%" & cmdQuery & "%")
                                Next


                            Else
                                Dim word As String
                                For Each word In words
                                    nameQuery = nameQuery & " and OWNER_NAME_1 LIKE ? OR OWNER_NAME_2 LIKE ? OR OWNER_NAME_3 LIKE ?"
                                    mailQuery = mailQuery & " and mail_address_1 like ? or mail_address_2 like ?  "
                                Next

                                SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE " & balanceQuery & " " & taxYearQuery & " " & mailQuery & " and " & nameQuery & ") a"

                                cmd = New OleDbCommand(SQL)

                                Dim cmdQuery As String
                                For Each cmdQuery In words
                                    '  cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                                    cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & cmdQuery & "%")
                                    cmd.Parameters.AddWithValue("@OWNER_NAME_2", "%" & cmdQuery & "%")
                                    cmd.Parameters.AddWithValue("@OWNER_NAME_3", "%" & cmdQuery & "%")
                                    cmd.Parameters.AddWithValue("@mail_address_1", "%" & cmdQuery & "%")
                                    cmd.Parameters.AddWithValue("@mail_address_2", "%" & cmdQuery & "%")
                                Next

                            End If
                        Else
                            SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE mail_address_1 like ? or mail_address_2 like ? " & " and " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                            cmd = New OleDbCommand(SQL)
                            '  cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                            cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & InvestorIDorSSAN & "%")
                            cmd.Parameters.AddWithValue("@mail_address_1", "%" & InvestorIDorSSAN & "%")
                            cmd.Parameters.AddWithValue("@mail_address_2", "%" & InvestorIDorSSAN & "%")
                        End If


                    End If

                ElseIf (countInvestorWords = 1) Then
                    '' SSAN is empty try to search by lastname
                    If (Not String.IsNullOrEmpty(InvestorIDorSSAN.Trim())) Then
                        If (BalanceOnly = True) Then
                            balanceQuery = " CurrentBalance > 0 "
                        Else
                            balanceQuery = " CurrentBalance = 0 "
                        End If

                        If (CheckTaxYear = True) Then
                            taxYearQuery = "and TaxYear >= " + TextTaxYear + " "
                            '  Else
                            '    taxYearQuery = "and TaxYear = " + TaxYear + " "
                        End If


                        If (SearchParam = "Name") Then
                            If (Name1 = True) Then
                                nameQuery = " and OWNER_NAME_1 LIKE ?"
                                SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE " & " " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                                cmd = New OleDbCommand(SQL)
                                '  cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                                cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & InvestorIDorSSAN & "%")

                            Else
                                nameQuery = " and OWNER_NAME_1 LIKE ? OR OWNER_NAME_2 LIKE ? OR OWNER_NAME_3 LIKE ?"

                                SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                           "WHERE " & " " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                                cmd = New OleDbCommand(SQL)
                                ' cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                                cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & InvestorIDorSSAN & "%")
                                cmd.Parameters.AddWithValue("@OWNER_NAME_2", "%" & InvestorIDorSSAN & "%")
                                cmd.Parameters.AddWithValue("@OWNER_NAME_3", "%" & InvestorIDorSSAN & "%")
                            End If



                        ElseIf (SearchParam = "Address") Then
                            SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                        "WHERE mail_address_1 like ? or mail_address_2 like ? " & " and " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                            cmd = New OleDbCommand(SQL)
                            ' cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                            cmd.Parameters.AddWithValue("@mail_address_1", "%" & InvestorIDorSSAN & "%")
                            cmd.Parameters.AddWithValue("@mail_address_2", "%" & InvestorIDorSSAN & "%")

                        ElseIf (SearchParam = "Both") Then
                            If (Name1 = True) Then
                                nameQuery = " and OWNER_NAME_1 LIKE ?"
                                SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE " & " " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                                cmd = New OleDbCommand(SQL)
                                '  cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                                cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & InvestorIDorSSAN & "%")

                            Else
                                nameQuery = " and OWNER_NAME_1 LIKE ? OR OWNER_NAME_2 LIKE ? OR OWNER_NAME_3 LIKE ?"

                                SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE " & " " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                                cmd = New OleDbCommand(SQL)
                                ' cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                                cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & InvestorIDorSSAN & "%")
                                cmd.Parameters.AddWithValue("@OWNER_NAME_2", "%" & InvestorIDorSSAN & "%")
                                cmd.Parameters.AddWithValue("@OWNER_NAME_3", "%" & InvestorIDorSSAN & "%")
                            End If
                        Else
                            SQL = "select " & dropDownVal & " row_number() over (order by OWNER_NAME_1,OWNER_NAME_2,OWNER_NAME_3 ASC)as row,a.* from (SELECT   distinct OWNER_NAME_1, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2,  mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE mail_address_1 like ? or mail_address_2 like ? " & " and " & balanceQuery & " " & taxYearQuery & " " & nameQuery & ") a"

                            cmd = New OleDbCommand(SQL)
                            '  cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                            cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & InvestorIDorSSAN & "%")
                            cmd.Parameters.AddWithValue("@mail_address_1", "%" & InvestorIDorSSAN & "%")
                            cmd.Parameters.AddWithValue("@mail_address_2", "%" & InvestorIDorSSAN & "%")
                        End If


                    End If

                End If

                'Dim cmd As New OleDbCommand("SELECT TOP 10 InvestorID, SocialSecNum, FirstName, MiddleName, LastName FROM genii_user.ST_INVESTOR " & _
                '                            "WHERE InvestorID = ? OR REPLACE(SocialSecNum,'-','') LIKE ? " & _
                '                            "OR LastName LIKE ?")

                'cmd.Parameters.AddWithValue("@InvestorID", ssn)
                'cmd.Parameters.AddWithValue("@SocialSecNum", ssn & "%")
                'cmd.Parameters.AddWithValue("@LastName", "%" & InvestorIDorSSAN & "%")

                If (Not String.IsNullOrEmpty(ssn) Or (Not String.IsNullOrEmpty(InvestorIDorSSAN.Trim()))) Then
                    cmd.Connection = conn
                    conn.Open()

                    Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                    While rdr.Read()
                        Dim rowNumber As Object = rdr.GetValue(0)
                        Dim firstName As Object = rdr.GetValue(1)
                        Dim taxIDNumber As Object = rdr.GetValue(2)
                        Dim middleName As Object = rdr.GetValue(3)
                        Dim lastName As Object = rdr.GetValue(4)
                        Dim mail1 As Object = rdr.GetValue(5)
                        Dim mail2 As Object = rdr.GetValue(6)
                        '   Dim taxYear As Object = rdr.GetValue(5)

                        Dim resultString As New StringBuilder()
                        Dim keyString As String = String.Empty

                        keyString = rowNumber & "-" & taxIDNumber

                        '    resultString.AppendFormat("{0}, ", taxRollNumber)
                        resultString.AppendFormat("{0}, ", rowNumber)

                        If Not IsDBNull(taxIDNumber) Then
                            resultString.AppendFormat("{0}, ", taxIDNumber)
                        End If

                        If Not IsDBNull(firstName) Then
                            resultString.AppendFormat("{0}, ", firstName)
                        End If

                        If Not IsDBNull(middleName) Then
                            resultString.AppendFormat("{0} ", middleName)
                        End If

                        If Not IsDBNull(lastName) Then
                            resultString.AppendFormat("{0},", lastName)
                        End If

                        If Not IsDBNull(mail1) Then
                            resultString.AppendFormat("{0},", mail1)
                        End If
                        If Not IsDBNull(mail2) Then
                            resultString.AppendFormat("{0}", mail2)
                        End If


                        '   If Not IsDBNull(taxYear) Then
                        ''  'resultString.AppendFormat("{0}", taxYear)
                        '  End If

                        result.Add(keyString.ToString(), resultString.ToString())
                    End While

                    If result.Count = 0 Then
                        result.Add("0", "Add New")
                    End If
                End If



            End If

            Return result
        End Using
    End Function


    ''' <summary>
    ''' Search investor table by investor id or ssan
    ''' </summary>
    ''' <param name="InvestorIDorSSAN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <System.Web.Services.WebMethod()> _
    Public Shared Function GetCPInvestor(InvestorIDorSSAN As String) As Generic.Dictionary(Of String, String)
        ' Extract numerals from ssan.
        Dim ssn As String = ExtractSSANNumerals(InvestorIDorSSAN)
        Dim SQL As String = String.Empty
        Dim cmd As New OleDbCommand()
        Dim result As New Generic.Dictionary(Of String, String)

        Dim myUtil As New Utilities()

        ' Search database.
        Using conn As New OleDbConnection(myUtil.ConnectString)

            If (Not String.IsNullOrEmpty(ssn)) Then
                SQL = "SELECT TOP 10 InvestorID, SocialSecNum, FirstName, MiddleName, LastName FROM genii_user.ST_INVESTOR " & _
                      "WHERE InvestorID = ? OR REPLACE(SocialSecNum,'-','') LIKE ? "

                cmd = New OleDbCommand(SQL)

                cmd.Parameters.AddWithValue("@InvestorID", ssn)
                cmd.Parameters.AddWithValue("@SocialSecNum", ssn & "%")
            Else
                '' SSAN is empty try to search by lastname
                If (Not String.IsNullOrEmpty(InvestorIDorSSAN.Trim())) Then
                    SQL = "SELECT TOP 10 InvestorID, SocialSecNum, FirstName, MiddleName, LastName FROM genii_user.ST_INVESTOR " & _
                     "WHERE LastName LIKE ?"

                    cmd = New OleDbCommand(SQL)
                    cmd.Parameters.AddWithValue("@LastName", "%" & InvestorIDorSSAN & "%")
                End If
            End If


            If (Not String.IsNullOrEmpty(ssn) Or (Not String.IsNullOrEmpty(InvestorIDorSSAN.Trim()))) Then
                cmd.Connection = conn
                conn.Open()

                Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                While rdr.Read()
                    Dim investorID As Integer = rdr.GetInt32(0)
                    Dim socialSecNum As Object = rdr.GetValue(1)
                    Dim firstName As Object = rdr.GetValue(2)
                    Dim middleName As Object = rdr.GetValue(3)
                    Dim lastName As Object = rdr.GetValue(4)

                    Dim resultString As New StringBuilder()

                    resultString.AppendFormat("{0}, ", investorID)

                    If Not IsDBNull(socialSecNum) Then
                        resultString.AppendFormat("{0}, ", socialSecNum)
                    End If

                    If Not IsDBNull(firstName) Then
                        resultString.AppendFormat("{0} ", firstName)
                    End If

                    If Not IsDBNull(middleName) Then
                        resultString.AppendFormat("{0} ", middleName)
                    End If

                    If Not IsDBNull(lastName) Then
                        resultString.AppendFormat("{0}", lastName)
                    End If

                    result.Add(investorID.ToString(), resultString.ToString())
                End While

                If result.Count = 0 Then
                    result.Add("0", "No Results Found")
                End If

            End If

            Return result
        End Using
    End Function
#End Region

#Region "Database Methods"
    ''' <summary>
    ''' Loads table from database into dataset. Helper function for <see cref="InvestorDataset">InvestorDataset</see>.
    ''' </summary>
    ''' <param name="container"></param>
    ''' <param name="tableName"></param>
    ''' <param name="query"></param>
    ''' <remarks></remarks>
    Private Sub LoadTable(container As DataSet, tableName As String, query As String)
        Using adt As New OleDbDataAdapter(query, util.ConnectString)
            adt.Fill(container, tableName)
        End Using
    End Sub

    Private Sub LoadSchema(container As DataSet, tableName As String, query As String)
        Using adt As New OleDbDataAdapter(query, util.ConnectString)
            adt.FillSchema(container, SchemaType.Source, tableName)
        End Using
    End Sub

    ''' <summary>
    ''' Adds relation between two tables. Helper function for <see cref="InvestorDataset">InvestorDataset</see>.
    ''' </summary>
    ''' <param name="container"></param>
    ''' <param name="parentTable"></param>
    ''' <param name="parentColumn"></param>
    ''' <param name="childTable"></param>
    ''' <param name="childColumn"></param>
    ''' <remarks></remarks>
    Private Sub AddRelation(container As DataSet, parentTable As String, parentColumn As String, childTable As String, childColumn As String)
        Dim relName As String = String.Format("{0}-{1}", parentTable, childTable)

        If Not container.Relations.Contains(relName) Then
            Dim rel As New DataRelation(relName, container.Tables(parentTable).Columns(parentColumn), container.Tables(childTable).Columns(childColumn))

            container.Relations.Add(rel)

            rel.ChildKeyConstraint.DeleteRule = Rule.Cascade
            rel.ChildKeyConstraint.UpdateRule = Rule.Cascade
        End If
    End Sub

    ''' <summary>
    ''' Gets new value for column from database.
    ''' </summary>
    ''' <param name="columnName"></param>
    ''' <param name="tableName"></param>
    ''' <param name="connection"></param>
    ''' <param name="transaction"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetNewID(columnName As String, tableName As String, _
                              Optional connection As OleDbConnection = Nothing, _
                              Optional transaction As OleDbTransaction = Nothing) As Integer

        If connection Is Nothing Then
            connection = New OleDbConnection(util.ConnectString)
        End If

        Dim cmd As New OleDbCommand(String.Format("select MAX({0}) from {1}", columnName, tableName))

        cmd.Connection = connection

        If transaction IsNot Nothing Then
            cmd.Transaction = transaction
        End If

        If connection.State <> ConnectionState.Open Then
            connection.Open()
        End If

        Dim newID As Object = cmd.ExecuteScalar()

        If IsNumeric(newID) Then
            Return CInt(newID) + 1
        Else
            Return 1
        End If
    End Function

    Private Sub UpdateRecordIds(table As DataTable, tableName As String, columnName As String, _
                        ByVal connection As OleDbConnection, ByVal transaction As OleDbTransaction)
        Dim recordID As Integer = GetNewID(columnName, tableName, connection, transaction)
        For Each row As DataRow In table.Select(String.Empty, String.Empty, _
                                                DataViewRowState.Added)
            row(columnName) = recordID
            recordID += 1
        Next
    End Sub

    Private Sub CommitTable(table As DataTable, tableName As String, _
                    ByVal connection As OleDbConnection, ByVal transaction As OleDbTransaction)
        Using adt As New OleDbDataAdapter(String.Format("select * from {0}", tableName), connection)
            adt.SelectCommand.Transaction = transaction
            Dim bld As New OleDbCommandBuilder(adt)
            adt.UpdateCommand = bld.GetUpdateCommand()
            adt.InsertCommand = bld.GetInsertCommand()
            adt.DeleteCommand = bld.GetDeleteCommand()
            adt.UpdateCommand.Transaction = transaction
            adt.InsertCommand.Transaction = transaction
            adt.DeleteCommand.Transaction = transaction

            adt.Update(table)
        End Using
    End Sub

    Private Sub CommitDataset()
        Using conn As New OleDbConnection(util.ConnectString)
            conn.Open()
            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                ' Assign new record ids.
                UpdateRecordIds(Me.InvestorTable, "genii_user.ST_INVESTOR", "InvestorID", conn, trans)
                UpdateRecordIds(Me.InvestorCalendarTable, "genii_user.ST_INVESTOR_CALENDAR", "RECORD_ID", conn, trans)

                ' Commit tables.
                CommitTable(Me.InvestorTable, "genii_user.ST_INVESTOR", conn, trans)
                CommitTable(Me.InvestorCalendarTable, "genii_user.ST_INVESTOR_CALENDAR", conn, trans)

                trans.Commit()
            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
        End Using
    End Sub
#End Region

#Region "Page Methods"
    Private Sub BindControl(control As WebControl, row As DataRow, columnName As String)
        Dim val As String
        If row Is Nothing Then
            val = String.Empty
        Else
            val = row.Item(columnName).ToString()
        End If

        If TypeOf control Is TextBox Then
            DirectCast(control, TextBox).Text = val
        ElseIf TypeOf control Is Label Then
            DirectCast(control, Label).Text = val
        End If
    End Sub

    Private Sub PrepareControls()
        ' Payment types.
        Using adt As New OleDbDataAdapter("SELECT PaymentTypeCode, PaymentDescription FROM genii_user.ST_PAYMENT_INSTRUMENT WHERE SHOW_CASHIER = 1", Me.ConnectString)
            adt.SelectCommand.Connection.Open()

            Dim rdr As OleDbDataReader = adt.SelectCommand.ExecuteReader()

            While rdr.Read()
                Me.ddlPaymentType.Items.Add(New ListItem(rdr.Item("PaymentDescription").ToString(), rdr.Item("PaymentTypeCode")))
            End While
        End Using
    End Sub

    Private Sub LoadInvestorInfo()
        Dim row As DataRow = Me.InvestorRow
        BindControl(Me.txtRegSSAN, row, "SocialSecNum")
        BindControl(Me.lblRegInvestorID, row, "InvestorID")
        BindControl(Me.txtRegFirstName, row, "FirstName")
        BindControl(Me.txtRegMiddleName, row, "MiddleName")
        BindControl(Me.txtRegLastName, row, "LastName")
        BindControl(Me.txtPayorName, row, "LastName")
        BindControl(Me.txtRegAddress1, row, "Address1")
        BindControl(Me.txtRegAddress2, row, "Address2")
        BindControl(Me.txtRegCity, row, "City")
        BindControl(Me.txtRegState, row, "State")
        BindControl(Me.txtRegZip, row, "PostalCode")
        BindControl(Me.txtRegPhone, row, "PhoneNumber")
        BindControl(Me.txtRegEmail, row, "EMailAddress")
        BindControl(Me.txtNewWorldVID, row, "NW_Vendor")

        If row IsNot Nothing Then
            Me.chkRegActive.Checked = (row.Item("Active").ToString() = "1")
            Me.chkRegConfidential.Checked = (row.Item("ConfidentialFlag").ToString() = "1")
            Me.chkRegReturnedMail.Checked = (row.Item("MailReturnFlag").ToString() = "1")
        Else
            Me.chkRegActive.Checked = False
            Me.chkRegConfidential.Checked = False
            Me.chkRegReturnedMail.Checked = False
        End If

        BindRemarksGrid()

        ' Prepare add remark dialog.
        Me.txtRemarkText.Text = String.Empty
        Me.txtRemarkDate.Text = Date.Today.ToShortDateString()
    End Sub

    Private Sub SaveInvestorInfo()
        Dim row As DataRow = Me.InvestorRow
        row.Item("SocialSecNum") = Me.txtRegSSAN.Text
        row.Item("FirstName") = Me.txtRegFirstName.Text
        row.Item("MiddleName") = Me.txtRegMiddleName.Text
        row.Item("LastName") = Me.txtRegLastName.Text
        row.Item("Address1") = Me.txtRegAddress1.Text
        row.Item("Address2") = Me.txtRegAddress2.Text
        row.Item("City") = Me.txtRegCity.Text
        row.Item("State") = Me.txtRegState.Text
        row.Item("PostalCode") = Me.txtRegZip.Text
        row.Item("PhoneNumber") = Me.txtRegPhone.Text
        row.Item("EmailAddress") = Me.txtRegEmail.Text
        row.Item("Active") = Me.chkRegActive.Checked
        row.Item("ConfidentialFlag") = Me.chkRegConfidential.Checked
        row.Item("MailReturnFlag") = Me.chkRegReturnedMail.Checked
        '  row.Item("NW_VENDOR") = Me.txtNewWorldVID.Text
        Me.CommitDataset()
    End Sub

    Private Sub BindRemarksGrid()
        Me.grdRegRemarks.DataSource = Me.InvestorCalendarView
        Me.grdRegRemarks.DataBind()
    End Sub

    Private Sub BindSubtaxGrid()
        Me.grdRegSubtax.DataSource = GetSubtaxCandidates(Me.InvestorID)
        Me.grdRegSubtax.DataBind()
    End Sub

    Protected Function GetTotalSubTax(dataItem As Object) As Decimal
        Dim row As DataRow
        If TypeOf dataItem Is DataRow Then
            row = DirectCast(dataItem, DataRow)
        ElseIf TypeOf dataItem Is DataRowView Then
            row = DirectCast(dataItem, DataRowView).Row
        Else
            Throw New ArgumentException("Expected DataRow or DataRowView. Received " & dataItem.GetType().Name)
        End If

        Dim total As Decimal = 0
        If Not row.IsNull("Current Balance") Then
            total += CDec(row("Current Balance"))
        End If
        If Not row.IsNull("Interest") Then
            total += CDec(row("Interest"))
        End If
        If Not row.IsNull("Subtax Fee") Then
            total += CDec(row("Subtax Fee"))
        End If

        Return total
    End Function
#End Region

#Region "Investor Methods"
    Private Sub AddNewInvestor(ssan As String)
        Dim row As DataRow = Me.InvestorTable.NewRow()
        row.Item("InvestorID") = GetNewID("InvestorID", "genii_user.ST_INVESTOR")
        row.Item("SocialSecNum") = ssan
        row.Item("Active") = 1
        row.Item("CREATE_USER") = System.Web.HttpContext.Current.User.Identity.Name
        row.Item("CREATE_DATE") = Date.Now
        row.Item("EDIT_USER") = System.Web.HttpContext.Current.User.Identity.Name
        row.Item("EDIT_DATE") = Date.Now
        Me.InvestorTable.Rows.Add(row)

        CommitDataset()
        Me.InvestorID = row("InvestorID")
    End Sub

    Private Sub AddInvestorRemark()
        If Me.InvestorID = 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Message", "window.alert('Load investor data by entering SSAN before adding remark.');", True)
        Else
            Dim row As DataRow = Me.InvestorCalendarTable.NewRow()
            row.Item("RECORD_ID") = GetNewID("RECORD_ID", "genii_user.ST_INVESTOR_CALENDAR")
            row.Item("INVESTORID") = Me.InvestorID
            row.Item("REMARKS") = Me.txtRemarkText.Text
            row.Item("IMAGE") = Me.uplRemarkImage.FileBytes
            row.Item("TASK_DATE") = Me.txtRemarkDate.Text
            row.Item("CREATE_USER") = System.Web.HttpContext.Current.User.Identity.Name
            row.Item("CREATE_DATE") = Date.Now
            row.Item("EDIT_USER") = System.Web.HttpContext.Current.User.Identity.Name
            row.Item("EDIT_DATE") = Date.Now
            row.Item("FILE_TYPE") = util.GetUploadFileType(Me.uplRemarkImage.FileName)

            Me.InvestorCalendarTable.Rows.Add(row)

            CommitDataset()
            Me.InvestorDataset = Nothing
            LoadInvestorInfo()
        End If
    End Sub

    Private Function GetSubtaxCandidates(investorID As Integer) As DataTable
        'removed old sql select * from dbo.getsubtax(?,?) MTA 04252013
        Using adt As New OleDbDataAdapter("SELECT genii_user.ST_INVESTOR.LastName + ' (' + CONVERT(varchar, genii_user.TR_CP.InvestorID) + ')' AS 'Investor', " & _
                                             " CONVERT(varchar, genii_user.TR_CP.MonthlyRateOfInterest*100) + '%' AS 'Bid Rate', " & _
                                             "  genii_user.TR_CP.APN AS 'Parcel',  " & _
                                             "            '$' + CONVERT(varchar, (genii_user.TR.CurrentBalance-genii_user.TR_CHARGES.ChargeAmount), 1) AS 'Current Balance', " & _
                                             "            '$' + CONVERT(varchar, genii_user.TR_CHARGES.ChargeAmount, 1) AS 'Interest', " & _
                                             "            '$' + CONVERT(varchar, 5, 1) AS 'Subtax Fee', " & _
                                             "            '$' + CONVERT(varchar, genii_user.TR.CurrentBalance+5, 1) AS 'Total', " & _
                                             " genii_user.TR_CP.CertificateNumber, genii_user.TR.taxrollnumber,genii_user.TR.taxyear " & _
                                             "            FROM genii_user.TR_CP " & _
                                             "  INNER JOIN genii_user.ST_INVESTOR ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " & _
                                             "    INNER JOIN genii_user.TR ON genii_user.TR_CP.APN = genii_user.TR.APN " & _
                                             "      INNER JOIN genii_user.TR_CHARGES ON genii_user.TR.TaxYear = genii_user.TR_CHARGES.TaxYear " & _
                                             "        AND genii_user.TR.TaxRollNumber = genii_user.TR_CHARGES.TaxRollNumber " & _
                                             "            WHERE genii_user.TR_CP.DATE_REDEEMED Is NULL  " & _
                                             "  AND genii_user.TR_CP.InvestorID=? " & _
                                             "    AND  genii_user.TR_CHARGES.TaxChargeCodeID = 99901" & _
                                             "      AND genii_user.TR_CP.TaxYear = DATEPART(yyyy, GETDATE())-2 " & _
                                             "        AND genii_user.TR_CP.InvestorID <> 1 " & _
                                             "          AND genii_user.TR_CP.APN IN " & _
                                             "            (SELECT TR_1.APN FROM genii_user.TR AS TR_1 " & _
                                             "              CROSS JOIN genii_user.TR_CP AS TR_CP_1 " & _
                                             "         WHERE TR_1.TaxYear = DatePart(yyyy, GETDATE()) - 1 " & _
                                             "                  AND TR_1.SecuredUnsecured = 'S' " & _
                                             "                    AND TR_1.CurrentBalance > 0) " & _
                                             "                      AND genii_user.TR.TaxYear = DATEPART(yyyy, GETDATE())-1", util.ConnectString)

            '  "  and genii_user.TR.taxrollnumber not in (select taxrollnumber from genii_user.TR_CP where date_redeemed is null and investorID=? and taxyear=DATEPART(yyyy, GETDATE())-1) " & _

            adt.SelectCommand.Parameters.AddWithValue("@investorID", investorID)
            ' adt.SelectCommand.Parameters.AddWithValue("@taxYear", Date.Now.Year - 1)

            Dim dt As New DataTable("GetSubtax")
            adt.Fill(dt)

            Return dt
        End Using
    End Function

    Private Sub SaveSubtax()
        Dim i As Integer = 0

        Dim groupKey As Integer
        Dim SQL As String = String.Format("select isnull(max(group_key),0)+1  as group_key from genii_user.cashier_transactions ")

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblGRPKEY As New DataTable()

            adt.Fill(tblGRPKEY)

            If tblGRPKEY.Rows.Count > 0 Then
                If (Not IsDBNull(tblGRPKEY.Rows(0)("group_key"))) Then
                    groupKey = Convert.ToDouble(tblGRPKEY.Rows(0)("group_key"))
                End If
            End If
        End Using

        For Each row As GridViewRow In Me.grdRegSubtax.Rows
            If row.RowType = DataControlRowType.DataRow Then
                Dim chkSubtax As CheckBox = row.FindControl("chkSubtax")
                If chkSubtax IsNot Nothing AndAlso chkSubtax.Checked Then
                    Dim hdnTaxYear As HiddenField = row.FindControl("hdnTaxYear")
                    Dim hdnTaxRollNumber As HiddenField = row.FindControl("hdnTaxRollNumber")
                    '   Dim taxRoll As TextBox = (Textbox)grdRegSubtax.Rows(i).cells(5).FindControl("hdnTaxRollNumber")
                    '   Dim a As String = taxRoll.Text
                    ' Dim APN As String = grdRegSubtax.Rows(0).Cells(1).Text.ToString()
                    Dim APN As String = grdRegSubtax.Rows(i).Cells(2).Text.ToString()
                    Dim taxYear As Integer = GetCurrentTaxYearValue()
                    ' Dim APN3 As String = grdRegSubtax.Rows(0).Cells(3).Text.ToString()
                    '  Dim APN4 As String = grdRegSubtax.Rows(0).Cells(4).Text.ToString()
                    '  Dim APN5 As String = grdRegSubtax.Rows(0).Cells(5).Text.ToString()
                    '  Dim APN6 As String = grdRegSubtax.Rows(0).Cells(6).Text.ToString()
                    CreateNewRecord(Me.lblRegInvestorID.Text, APN, taxYear, groupKey)
                    _priorMonthTaxID = APN

                    Dim print_document As Printing.PrintDocument
                    print_document = PreparePrintDocument("Testing")
                    print_document.Print()

                End If
            End If
            i = i + 1
        Next

        '  Response.Write("<script>")
        '   Response.Write("setInterval(function () { document.getElementById('btnRegLoadSubtax').click(); }, 200);")
        '  Response.Write("</script>")

        '  GetSubtaxCandidates(Me.lblRegInvestorID.Text)
        Me.grdRegSubtax.DataSource = GetSubtaxCandidates(Me.InvestorID)
        Me.grdRegSubtax.DataBind()

    End Sub
    Private Sub SaveAcceptedPayment(taxAmount As Decimal, taxYear As String, TaxRollNumber As Integer)
        ' Add payment information to CASHIER_TRANSACTION table.
        Dim row As DataRow
        row = Me.CashierTransactionsTable.NewRow()

        row("SESSION_ID") = Me.lblSessionID.Text
        row("TAX_YEAR") = taxYear
        row("TAX_ROLL_NUMBER") = TaxRollNumber
        row("PAYMENT_DATE") = Date.Now 'Me.txtPaymentDate.Text
        row("PAYMENT_TYPE") = Me.ddlPaymentType.SelectedValue
        row("PAYOR_NAME") = Me.txtPayorName.Text

        If Me.ddlPaymentType.SelectedValue = 2 Then
            row("CHECK_NUMBER") = Me.txtCheckNumber.Text
        End If

        '   row("BARCODE") = Me.txtBarcode.Text
        row("PAYMENT_AMT") = taxAmount 'Utilities.GetDecimalOrDBNull(Me.txtAmountPaid.Text)
        row("TAX_AMT") = taxAmount
        ' row("KITTY_AMT") = kittyAmount
        '  row("REFUND_AMT") = refundAmount
        row("EDIT_USER") = System.Web.HttpContext.Current.User.Identity.Name
        row("EDIT_DATE") = Date.Now
        row("CREATE_USER") = System.Web.HttpContext.Current.User.Identity.Name
        row("CREATE_DATE") = Date.Now

        Me.CashierTransactionsTable.Rows.Add(row)

        ' CommitDataset()
    End Sub

    Private Sub CreateNewRecord(investorID As String, APN As String, currentTaxYear As Integer, grpKey As Integer) ', taxRollNumber As String, taxYear As String
        '   Dim userName As String = TaxPayments.CurrentUserName
        '   Dim startCash As Decimal = CDec(Me.txtLoginStartCash.Text)

        Dim taxRollNumber As Integer
        Dim chargeAmount As Double

        Dim TRCPDetails As DataSet = New DataSet()
        Dim sql2 As String = String.Format("SELECT genii_user.ST_INVESTOR.LastName + ' (' + CONVERT(varchar, genii_user.TR_CP.InvestorID) + ')' AS 'Investor', " & _
                                             " CONVERT(varchar, genii_user.TR_CP.MonthlyRateOfInterest) AS 'Bid Rate', " & _
                                             "  genii_user.TR_CP.APN AS 'Parcel', SUBSTRING(genii_user.TR_CP.APN,1,6) AS 'BOOK_MAP',  " & _
                                             "            '$' + CONVERT(varchar, genii_user.TR.CurrentBalance, 1) AS 'Current Balance', " & _
                                             "            '$' + CONVERT(varchar, genii_user.TR_CHARGES.ChargeAmount, 1) AS 'Interest', " & _
                                             "            '$' + CONVERT(varchar, 5, 1) AS 'Subtax Fee', " & _
                                             "            '$' + CONVERT(varchar, genii_user.TR.CurrentBalance+5, 1) AS 'Total', " & _
                                             " genii_user.TR_CP.CertificateNumber, genii_user.TR.TaxYear, CONVERT(numeric,genii_user.TR.TaxRollNumber) as 'TaxRollNumber', " & _
                                             " genii_user.TR_CP.DateOfSale, genii_user.TR_CP.DateCPPurchased " & _
                                             "            FROM genii_user.TR_CP " & _
                                             "  INNER JOIN genii_user.ST_INVESTOR ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " & _
                                             "    INNER JOIN genii_user.TR ON genii_user.TR_CP.APN = genii_user.TR.APN " & _
                                             "      INNER JOIN genii_user.TR_CHARGES ON genii_user.TR.TaxYear = genii_user.TR_CHARGES.TaxYear " & _
                                             "        AND genii_user.TR.TaxRollNumber = genii_user.TR_CHARGES.TaxRollNumber " & _
                                             "            WHERE genii_user.TR_CP.DATE_REDEEMED Is NULL  " & _
                                             "  AND genii_user.TR_CP.InvestorID={0} " & _
                                             "  AND genii_user.TR_CP.APN='{1}' " & _
                                             "    AND  genii_user.TR_CHARGES.TaxChargeCodeID = 99901 " & _
                                             "      AND genii_user.TR_CP.TaxYear = DATEPART(yyyy, GETDATE())-2 " & _
                                             "        AND genii_user.TR_CP.InvestorID <> 1 " & _
                                             "          AND genii_user.TR_CP.APN IN " & _
                                             "            (SELECT TR_1.APN FROM genii_user.TR AS TR_1 " & _
                                             "              CROSS JOIN genii_user.TR_CP AS TR_CP_1 " & _
                                             "         WHERE TR_1.TaxYear = DatePart(yyyy, GETDATE()) - 1 " & _
                                             "                  AND TR_1.SecuredUnsecured = 'S' " & _
                                             "                    AND TR_1.CurrentBalance > 0) " & _
                                             "                      AND genii_user.TR.TaxYear = DATEPART(yyyy, GETDATE())-1", investorID, APN) ', taxRollNumber, taxYear)




        LoadTable(TRCPDetails, "TR_CP", sql2)

        Dim row2 As DataRow
        row2 = TRCPDetails.Tables(0).Rows(0)

        '   If (IsDBNull(row2("PAYOR_NAME"))) Then
        'Me.lblDeleteDivPayorName.Text = String.Empty
        '   Else
        '  Me.lblDeleteDivPayorName.Text = row2("PAYOR_NAME")
        '   End If

        '''''''''''''''''''''''
        taxRollNumber = row2("TaxRollNumber")

        Dim SQL As String = String.Format("select parameter from genii_user.st_parameter " & _
                                         " where record_id='{0}' ", 99933)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCharges As New DataTable()

            adt.Fill(tblCharges)

            If tblCharges.Rows.Count > 0 Then
                If (Not IsDBNull(tblCharges.Rows(0)("parameter"))) Then
                    chargeAmount = Convert.ToDouble(tblCharges.Rows(0)("parameter"))
                End If
            End If
        End Using


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                '   ' Get new record id.
                '   Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_SESSION", conn, trans)


                ' Create new record.
                Dim cmdNewRec As New OleDbCommand("INSERT INTO genii_user.TR_CP " & _
                                                  "(CertificateNumber, MonthlyRateOfInterest, InvestorID, CP_STATUS, DateOfSale, " & _
                                                  " DateCPPurchased,FaceValueofCP,PurchaseValue, " & _
                                                  " TaxYear, TaxRollNumber,APN,Book_Map, " & _
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRec.Connection = conn
                cmdNewRec.Transaction = trans

                With cmdNewRec.Parameters
                    .AddWithValue("@CERTIFICATE_NUMBER", row2("CertificateNumber"))
                    .AddWithValue("@MonthlyRateOfInterest", Double.Parse(row2("Bid Rate")))
                    .AddWithValue("@InvestorID", investorID)
                    .AddWithValue("@CP_STATUS", 3)
                    .AddWithValue("@DateOfSale", row2("DateOfSale"))
                    .AddWithValue("@DateCPPurchased", row2("DateCPPurchased"))
                    .AddWithValue("@FaceValueofCP", CDec(row2("Total")))
                    .AddWithValue("@PurchaseValue", CDec(row2("Total")))
                    '  .AddWithValue("@DATE_REDEEMED", Date.Now)
                    .AddWithValue("@TaxYear", currentTaxYear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", row2("TaxRollNumber"))
                    .AddWithValue("@APN", row2("Parcel"))
                    .AddWithValue("@Book_Map", row2("BOOK_MAP"))

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRec.ExecuteNonQuery()
                ' trans.Commit()
                '    SessionRecordID = recordID

                '    System.Web.HttpContext.Current.User.Identity.Name = userName
                '    Me.lblCurrentDate.Text = Date.Today.ToShortDateString()
                '    Me.lblLoginTime.Text = Date.Now.ToString()
                '   Me.lblStartCash.Text = startCash.ToString("C")
                '   Me.lblLogoutUsername.Text = userName


                Dim cmdNewRecCharges As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                                                              "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                              " TaxTypeID,ChargeAmount, " & _
                                                              " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                              " VALUES (?,?,?,?,?,?,?,?,?)")

                cmdNewRecCharges.Connection = conn
                cmdNewRecCharges.Transaction = trans

                With cmdNewRecCharges.Parameters
                    .AddWithValue("@TaxYear", currentTaxYear)
                    .AddWithValue("@TaxRollNumber", row2("TaxRollNumber"))
                    .AddWithValue("@TaxChargeCodeID", 99933)
                    .AddWithValue("@TaxTypeID", 75)
                    .AddWithValue("@ChargeAmount", chargeAmount)

                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCharges.ExecuteNonQuery()




                Dim cmdNewRecCashierTrans As New OleDbCommand("INSERT INTO genii_user.CASHIER_TRANSACTIONS " & _
                                                  "(RECORD_ID,SESSION_ID,GROUP_KEY, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_DATE, " & _
                                                  " PAYMENT_TYPE,APPLY_TO,PAYOR_NAME,CHECK_NUMBER, " & _
                                                  " PAYMENT_AMT, TAX_AMT,TRANSACTION_STATUS, " & _
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecCashierTrans.Connection = conn
                cmdNewRecCashierTrans.Transaction = trans

                Dim recordIDCashierTrans As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_TRANSACTIONS", conn, trans)
                taxRollNumber = row2("TaxRollNumber")
                Dim isApportioned As String = 1

                With cmdNewRecCashierTrans.Parameters
                    .AddWithValue("@RECORD_ID", recordIDCashierTrans)
                    .AddWithValue("@SESSION_ID", Me.lblSessionID.Text)
                    .AddWithValue("@GROUP_KEY", grpKey)
                    .AddWithValue("@TAX_YEAR", currentTaxYear)
                    .AddWithValue("@TAX_ROLL_NUMBER", taxRollNumber)
                    .AddWithValue("@PAYMENT_DATE", Date.Now)
                    .AddWithValue("@PAYMENT_TYPE", Me.ddlPaymentType.SelectedValue)
                    .AddWithValue("@APPLY_TO", 4)
                    .AddWithValue("@PAYOR_NAME", Me.txtPayorName.Text)
                    .AddWithValue("@CHECK_NUMBER", Me.txtCheckNumber.Text)
                    .AddWithValue("@PAYMENT_AMT", CDec(row2("Total")))
                    .AddWithValue("@TAX_AMT", CDec(row2("Total")))
                    .AddWithValue("@TRANSACTION_STATUS", isApportioned)

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCashierTrans.ExecuteNonQuery()


                'Dim CashierApportionDetails As DataSet = New DataSet()
                'Dim sql3 As String = String.Format("SELECT * FROM DBO.GETAPPORTIONMENT({0},{1},{2},'{3}')", currentTaxYear, taxRollNumber, row2("Current Balance"), Date.Now)

                'LoadTable(CashierApportionDetails, "CASHIER_APPORTION", sql3)

                'Dim row3 As DataRow
                '' Dim x As Integer = 0

                'For(Integer x=0;x<=row3.table.rows.count;x++)

                'Next
                'While (row3.Table.Rows.Count >= 0)
                '    row3 = TRCPDetails.Tables(0).Rows()
                '    Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_APPORTION", conn, trans)

                '    Dim cmdNewRecApportion As New OleDbCommand("INSERT INTO genii_user.CASHIER_APPORTION " & _
                '                                      "(Record_ID, TRANS_ID,AreaCode, TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                '                                      " TaxTypeID,PaymentDate,GLAccount, " & _
                '                                      " DateApportioned, DollarAmount,  " & _
                '                                      " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                                      " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                '    cmdNewRecApportion.Connection = conn
                '    cmdNewRecApportion.Transaction = trans

                '    With cmdNewRecApportion.Parameters
                '        .AddWithValue("@Record_ID", recordID)
                '        .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                '        .AddWithValue("@AreaCode", row3("AreaCode"))
                '        .AddWithValue("@TaxYear", currentTaxYear)
                '        .AddWithValue("@TaxRollNumber", taxRollNumber)
                '        .AddWithValue("@TaxChargeCodeID", row3("TaxChargeCodeID"))
                '        .AddWithValue("@TaxTypeID", row3("TaxTypeID"))
                '        .AddWithValue("@PaymentDate", Date.Now)
                '        .AddWithValue("@GLAccount", row3("GLAccount"))
                '        .AddWithValue("@DateApportioned", Date.Now)
                '        .AddWithValue("@DollarAmount", CDec(row3("DollarAmount")))

                '        '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                '        .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                '        .AddWithValue("@EDIT_DATE", Date.Now)
                '        .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '        .AddWithValue("@CREATE_DATE", Date.Now)

                '    End With

                '    cmdNewRecApportion.ExecuteNonQuery()
                'End While



                ' Me.ApportionDetailsTable.Clear()

                '     For Each payRow As DataRow In Me.CashierTransactionsTable.Select("TAX_YEAR='" & _ currentTaxYear.ToString() + "' AND TAX_ROLL_NUMBER = '" & _ taxRollNumber.ToString() + "' AND IS_APPORTIONED IS NULL OR IS_APPORTIONED <> 1") 'MTA 04052013 change sql; add " and roll id, tax year, payment date, payment amount = ?,?,?,?"
                'For Each payRow As DataRow In Me.CashierTransactionsTable.Select("IS_APPORTIONED IS NULL OR IS_APPORTIONED <> 1 AND TAX_ROLL_NUMBER=" & _ +" AND TAX_YEAR=" & _ +" AND ")
                '  taxYear = payRow("TAX_YEAR") ' MTA change this to current tax year
                '  taxRollNumber = payRow("TAX_ROLL_NUMBER") ' MTA change this to current tax roll number
                'paymentAmount = payRow("TAX_AMT") ' MTA change this to current tax amount
                '  paymentDate = payRow("PAYMENT_DATE") ' MTA change this to current date


                Dim cmd As New OleDbCommand("INSERT INTO GENII_USER.CASHIER_APPORTION(TAXYEAR,TAXROLLNUMBER,AREACODE,TAXCHARGECODEID,TAXTYPEID,PAYMENTDATE,GLACCOUNT,SENTTOOTHERSYSTEM,RECEIPTNUMBER, " & _
                                                " DATEAPPORTIONED,DOLLARAMOUNT)SELECT TAXYEAR,TAXROLLNUMBER,AREACODE,TAXCHARGECODEID,TAXTYPEID,PAYMENTDATE,GLACCOUNT,SENTTOOTHERSYSTEM,RECEIPTNUMBER, " & _
                                                " DATEAPPORTIONED,DOLLARAMOUNT FROM dbo.GetApportionment(?,?,?,?)", conn)
                cmd.Transaction = trans

                cmd.Parameters.AddWithValue("@TaxYear", currentTaxYear)
                cmd.Parameters.AddWithValue("@TaxRollNumber", row2("TaxRollNumber"))
                cmd.Parameters.AddWithValue("@PaymentAmount", row2("Current Balance"))
                cmd.Parameters.AddWithValue("@PaymentDate", Date.Now)

                cmd.ExecuteNonQuery()

                'Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                'While rdr.Read()                  

                Dim SQL3 As String = String.Format("UPDATE genii_user.CASHIER_APPORTION " & _
                                    "SET TRANS_ID = {0}, " & _
                                    "EDIT_USER = '{1}', " & _
                                    "EDIT_DATE = '{2}', " & _
                                    "CREATE_USER = '{3}', " & _
                                    "CREATE_DATE = '{4}' " & _
                                    "WHERE taxrollnumber = '{5}' " & _
                                    "AND taxyear = '{6}' ",
                                    recordIDCashierTrans,
                                    System.Web.HttpContext.Current.User.Identity.Name,
                                    Date.Now,
                                    System.Web.HttpContext.Current.User.Identity.Name,
                                    Date.Now,
                                    row2("TaxRollNumber"),
                                    currentTaxYear)
                Dim cmdNewRecApportion1 As OleDbCommand = New OleDbCommand(SQL3)
                cmdNewRecApportion1.Connection = conn
                cmdNewRecApportion1.Transaction = trans
                cmdNewRecApportion1.ExecuteNonQuery()

                '    With cmdNewRecApportion1.Parameters
                '        '.AddWithValue("@Record_ID", recordID1)
                '        .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                '        .AddWithValue("@TaxYear", rdr.Item("TaxYear"))
                '        .AddWithValue("@TaxRollNumber", rdr.Item("TaxRollNumber"))
                '        .AddWithValue("@AreaCode", rdr.Item("AreaCode"))
                '        .AddWithValue("@TaxChargeCodeID", rdr.Item("TaxChargeCodeID"))
                '        .AddWithValue("@TaxTypeID", rdr.Item("TaxTypeID"))
                '        .AddWithValue("@PaymentDate", Date.Now)
                '        .AddWithValue("@GLAccount", rdr.Item("GLAccount"))
                '        .AddWithValue("@DateApportioned", Date.Now)
                '        .AddWithValue("@DollarAmount", rdr.Item("DollarAmount"))

                '        '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                '        .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                '        .AddWithValue("@EDIT_DATE", Date.Now)
                '        .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '        .AddWithValue("@CREATE_DATE", Date.Now)

                '    End With

                '    

                'End While

                ' payRow("IS_APPORTIONED") = 1
                '     Next
                '  cmd.ExecuteNonQuery()


                Dim recordID2 As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_APPORTION", conn, trans)

                Dim cmdNewRecApportion2 As New OleDbCommand("INSERT INTO genii_user.CASHIER_APPORTION " & _
                                                  "(TRANS_ID, TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                  " TaxTypeID,PaymentDate,GLAccount, " & _
                                                  " DateApportioned, DollarAmount,  " & _
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecApportion2.Connection = conn
                cmdNewRecApportion2.Transaction = trans

                With cmdNewRecApportion2.Parameters
                    '.AddWithValue("@Record_ID", recordID2)
                    .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                    .AddWithValue("@TaxYear", currentTaxYear)
                    .AddWithValue("@TaxRollNumber", row2("TaxRollNumber"))
                    ' .AddWithValue("@AreaCode", row2("AreaCode"))
                    .AddWithValue("@TaxChargeCodeID", "99933")
                    .AddWithValue("@TaxTypeID", "75")
                    .AddWithValue("@PaymentDate", Date.Now)
                    .AddWithValue("@GLAccount", "N00100547180")
                    .AddWithValue("@DateApportioned", Date.Now)
                    .AddWithValue("@DollarAmount", CDec(row2("Subtax Fee")))

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecApportion2.ExecuteNonQuery()


                Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.TR_PAYMENTS " & _
                                                  "(TRANS_ID, TaxYear, TaxRollNumber, PaymentEffectiveDate, " & _
                                                  " PaymentTypeCode,PaymentMadeByCode,Pertinent1, " & _
                                                  " Pertinent2, PaymentAmount,  " & _
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecPayments.Connection = conn
                cmdNewRecPayments.Transaction = trans

                Dim paymentAmount = CDec(row2("Total")) ' - CDec(row2("Subtax Fee")

                With cmdNewRecPayments.Parameters
                    .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                    .AddWithValue("@TaxYear", currentTaxYear)
                    .AddWithValue("@TaxRollNumber", row2("TaxRollNumber"))
                    .AddWithValue("@PaymentEffectiveDate", Date.Now)
                    .AddWithValue("@PaymentTypeCode", Me.ddlPaymentType.SelectedValue)
                    .AddWithValue("@PaymentMadeByCode", 2)
                    .AddWithValue("@Pertinent1", Me.txtPayorName.Text)
                    .AddWithValue("@Pertinent2", "CPSubtax - " & Date.Now)
                    .AddWithValue("@PaymentAmount", paymentAmount)

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecPayments.ExecuteNonQuery()

                'Dim SQL As String = String.Format("UPDATE genii_user.TR SET CURRENTBALANCE='0', EDIT_USER = '" & _ System.Web.HttpContext.Current.User.Identity.Name + "', EDIT_DATE = '" & _ Date.Now + "' WHERE TAXYEAR='" & _ currentTaxYear + "' AND TAXROLLNUMBER='" & _ taxRollNumber + "' ")

                'Dim cmdTR As New OleDbCommand(SQL, conn)

                'cmdTR.Transaction = trans
                '' cmdTR.Parameters.AddWithValue("@CURRENTBALANCE", 0)
                '' cmdTR.Parameters.AddWithValue("@EDIT_USER", Convert.ToString(System.Web.HttpContext.Current.User.Identity.Name))
                '' cmdTR.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
                'cmdTR.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using

        Using conn3 As New OleDbConnection(Me.ConnectString)
            conn3.Open()
            Dim trans3 As OleDbTransaction = conn3.BeginTransaction()


            'UPDATE STATUS to 3.....
            'UPDATE STATUS to 3.....
            'UPDATE STATUS to 3.....
            'UPDATE STATUS to 3.....

            ' Approvals.
            ' If approveIDs.Count > 0 Then
            Dim SQL4 As String = "UPDATE genii_user.TR SET CURRENTBALANCE=0.0, STATUS = 3," & _
                                               " EDIT_USER = '" + System.Web.HttpContext.Current.User.Identity.Name + "', EDIT_DATE = '" + Date.Now + "' WHERE TAXYEAR='" + currentTaxYear.ToString() + "' AND TAXROLLNUMBER='" + taxRollNumber.ToString() + "' "

            Dim cmdTRUpdate As New OleDbCommand(SQL4, conn3)

            cmdTRUpdate.Transaction = trans3
            '  cmdTRUpdate.Parameters.AddWithValue("@CURRENTBALANCE", CDec(0.0))
            '  cmdTRUpdate.Parameters.AddWithValue("@EDIT_USER", Me.lblOperatorName.Text)
            '  cmdTRUpdate.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
            cmdTRUpdate.ExecuteNonQuery()
            ' End If

            trans3.Commit()
            conn3.Close()
        End Using


        '  SaveAcceptedPayment(CDec(row2("Total")), currentTaxYear, taxRollNumber)
        '   CalculateApportionmentsOnSavePayment(currentTaxYear, taxRollNumber, CDec(row2("Total")))
    End Sub



    Private Function GetNewIDApportion(columnName As String, table As DataTable, Optional rowFilter As String = Nothing) As Integer
        Dim newID As Object = table.Compute(String.Format("MAX({0})", columnName), rowFilter)

        If IsNumeric(newID) Then
            Return CInt(newID) + 1
        Else
            Return 1
        End If
    End Function

    Private Shared Function ExtractSSANNumerals(ssan As String) As String
        Dim ssnBuild As New StringBuilder()
        For Each c As Char In ssan.ToCharArray()
            If Char.IsDigit(c) Then
                ssnBuild.Append(c)
            End If
        Next
        Return ssnBuild.ToString()
    End Function

    ''' <summary>
    ''' Returns true if <paramref name="oldSSAN">oldSSAN</paramref>
    ''' is different from <paramref name="newSSAN">newSSAN</paramref>.
    ''' </summary>
    ''' <param name="oldSSAN"></param>
    ''' <param name="newSSAN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function IsSSANDifferent(oldSSAN As String, newSSAN As String) As Boolean
        Dim oldNums As String = ExtractSSANNumerals(oldSSAN)
        Dim newNums As String = ExtractSSANNumerals(newSSAN)

        Return (oldNums <> newNums)
    End Function
#End Region


End Class

