Imports System.Data
Imports System.Data.OleDb
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine
Imports ICSharpCode.SharpZipLib.Zip
Imports Utilities
Imports System.Drawing
Imports System.Data.SqlClient
Imports System.Drawing.Printing.PrintDocument
Imports System.Drawing.Printing
Imports Microsoft.PointOfService

Partial Class TaxPayments
    Inherits System.Web.UI.Page

    Private Const COMMAND_TIMEOUT As Integer = 600

    Private _sessionDataset As DataSet
    Private _AccountRemarksDataset As DataSet
    Private _taxRollRemarksDataset As DataSet
    Private _otherYearRemarksDataset As DataSet

    Private Const TAX_ROLL_MASTER_SESS_ID As String = "TaxRollMaster"

    Private _tblPaymentType As DataTable
    Private _tblTaxAuthority As DataTable
    Private _tblTaxType As DataTable
    Private _tblTaxDistrict As DataTable

    Private _fileType As String = String.Empty
    Private _accountAlert As Integer = 0
    Private _accountSuspend As Integer = 0
    Private _accountStatus As Integer = 0
    Private _accountBankruptcy As Integer = 0
    Private _trStatus As Integer = 0
    Private _trBoardOrder As Integer = 0
    Private _trCP As Integer = 0
    Private _trConfidential As Integer = 0
    Private _trMailReturned As Integer = 0
    Private _TRPaymentRule As Integer = 0
    Private _accountParentBal As Integer = 0

    Private _priorMonth As Integer = 0
    Private _txtInterest() As String

    Private _priorMonthTaxRoll As String = String.Empty
    Private _priorMonthTaxYear As String = String.Empty
    Private _priorMonthTransID As Integer = 0

    Private _currentTaxYear As String = String.Empty

    Private Const _SESSION_VAR_INVESTOR_ID As String = "TaxInvestorsASPX_InvestorID"
    Private _investorDataset As DataSet

    Dim util As New Utilities()


    Private Enum PaymentTypeEnum
        Cash
        Check
        CreditCard
        Creditron
    End Enum


#Region "Properties"
    Private _tblTRStatus As Object

    Public Property AccountAlert As Integer
        Get
            Return _accountAlert
        End Get
        Set(ByVal value As Integer)
            _accountAlert = value
        End Set
    End Property

    Public Property AccountParentBal As Integer
        Get
            Return _accountParentBal
        End Get
        Set(ByVal value As Integer)
            _accountParentBal = value
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

    Private ReadOnly Property InvestorTable As DataTable
        Get
            Return Me.InvestorDataset.Tables("ST_INVESTOR")
        End Get
    End Property

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
            '   Me.grdRegSubtax.DataSource = Nothing
            '   Me.grdRegSubtax.DataBind()
        End Set
    End Property

    Public Property AccountSuspend As Integer
        Get
            Return _accountSuspend
        End Get
        Set(ByVal value As Integer)
            _accountSuspend = value
        End Set
    End Property


    Public Property AccountStatus As Integer
        Get
            Return _accountStatus
        End Get
        Set(ByVal value As Integer)
            _accountStatus = value
        End Set
    End Property

    Public Property AccountBankruptcy As Integer
        Get
            Return _accountBankruptcy
        End Get
        Set(ByVal value As Integer)
            _accountBankruptcy = value
        End Set
    End Property

    Public Property TRStatus As Integer
        Get
            Return _trStatus
        End Get
        Set(ByVal value As Integer)
            _trStatus = value
        End Set
    End Property

    Public Property TRCP As Integer
        Get
            Return _trCP
        End Get
        Set(ByVal value As Integer)
            _trCP = value
        End Set
    End Property

    Public Property TRBoardOrder As Integer
        Get
            Return _trBoardOrder
        End Get
        Set(ByVal value As Integer)
            _trBoardOrder = value
        End Set
    End Property

    Public Property TRConfidential As Integer
        Get
            Return _trConfidential
        End Get
        Set(ByVal value As Integer)
            _trConfidential = value
        End Set
    End Property

    Public Property TRMailReturned As Integer
        Get
            Return _trMailReturned
        End Get
        Set(ByVal value As Integer)
            _trMailReturned = value
        End Set
    End Property

#End Region



#Region "Page Events"

    Private Property tblTRStatus(p1 As String) As Object
        Get
            Return _tblTRStatus
        End Get
        Set(value As Object)
            _tblTRStatus = value
        End Set
    End Property

    Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
        If Not Me.IsPostBack Then
            FillTaxYearDropDown()
            FillSearchInDropDown()
            LoadParameters()
            PrepareControls()
            LoadLoginInfo()
            LoadCountyInfo()
            ResetCautionLights()
            '  Dim hostName = System.Net.Dns.GetHostName()
            ' Dim machineName = Environment.UserName
            '   Dim compName = System.Environment.GetEnvironmentVariable("COMPUTERNAME")


            '   Me.txtPaymentDate.Text = Date.Today.ToShortDateString()
            txtRemarkDate.Text = Date.Today.ToShortDateString()
            txtCurrentDate.Text = Date.Today.ToShortDateString()
            txtTargetDate.Text = Date.Today.ToShortDateString()
            Me.TaxRollMaster = Nothing


            BindApportionsGrid()
            BindPendingPaymentsGrids()
            ''''''' ' BindLettersGrid()

            dtaSummary.SelectedIndex = 0

        End If

    End Sub
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

    Private Sub LoadSchema(container As DataSet, tableName As String, query As String)
        Using adt As New OleDbDataAdapter(query, util.ConnectString)
            adt.FillSchema(container, SchemaType.Source, tableName)
        End Using
    End Sub


    Protected Sub Page_PreRender(sender As Object, e As System.EventArgs) Handles Me.PreRender
        LoadLogoutInfo()
    End Sub

   
    Private Sub LoadCountyInfo()

        Dim SQL As String = String.Format("SELECT parameter FROM genii_user.st_parameter WHERE parameter_name = 'SIGNATURE_BLOCK_TITLE'")

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCountyName As New DataTable()

            adt.Fill(tblCountyName)

            If tblCountyName.Rows.Count > 0 Then
                If (Not IsDBNull(tblCountyName.Rows(0)("parameter"))) Then
                    hdrCountyName.InnerText = Convert.ToString(tblCountyName.Rows(0)("parameter"))
                End If

            End If
        End Using

    End Sub
    Protected Sub btnRegSearch_Click(sender As Object, e As System.EventArgs) Handles btnRegSearch.Click
        ' Me.InvestorID = CInt(Me.txtRegInvestorID.Text)
        If Me.txtRegInvestorID.Text Is Nothing Then
            ' Not found.
            Throw New ApplicationException("Tax Roll Number not found. ID:" & Me.InvestorID)
        Else
            Me.rdoTaxID.Checked = True
            txtRegInvestorID.Text = txtRegInvestorID.Text.Substring(1)
            txtTaxID.Text = txtRegInvestorID.Text.Substring(1)
            LoadInvestorInfo()
            '  ClientScript.RegisterStartupScript(Me.GetType, "Login", "$(document).ready(function() { showLoadingBox(); });", True)
            btnFindTaxInfo_Click(Me, EventArgs.Empty)

        End If
    End Sub

    Public Sub BindInterestCalc(TaxID As String)
        Dim TaxRollNumber As String = String.Empty

        If (txtAPN.Text <> String.Empty) Then
            TaxRollNumber = txtAPN.Text
        Else
            TaxRollNumber = Me.TaxRollMaster.APN
        End If

        If (txtTaxAccount.Text <> String.Empty) Then
            TaxID = txtTaxAccount.Text
        End If

        If (TaxID <> String.Empty) Then
            TaxID = TaxID.Replace("-", "")
        End If


        Dim InterestCalcTaxRollSQL As String = String.Format("select * from dbo.vInterestCalculator where TaxID = '" + TaxID + "' ")
        BindGrid(Me.grdInterestCalcTaxRolls, InterestCalcTaxRollSQL)


        Dim InterestCalcInvestorCPSQL As String = String.Format("select * from dbo.vCPRedeemInvest  where APN='" + TaxRollNumber + "' ")
        BindGrid(Me.grdInvestorCP, InterestCalcInvestorCPSQL)

        Dim InterestCalcStateCPSQL As String = String.Format("select * from dbo.vCPRedeemState  where APN='" + TaxRollNumber + "' ")
        BindGrid(Me.grdStateCP, InterestCalcStateCPSQL)

    End Sub

    Public Sub LoadInterestCalculation()

        If (txtAPN.Text <> String.Empty) Then
            txtTaxAccount.Text = txtAPN.Text
            BindInterestCalc(txtTaxAccount.Text)

            Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showInterestCalcAction('Testing');", True)
        Else
            BindInterestCalc(Request.Form("txtTaxAccount2"))

            Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showInterestCalcAction('Testing');", True)
        End If



    End Sub


    ''' <summary>
    ''' Saves payments in database.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks>TR_CALENDAR and TR_PAYMENTS tables are written to.</remarks>
    ''' 

    Private Sub LoadInvestorInfo()
        'Dim row As DataRow = Me.InvestorRow
        'BindControl(Me.txtRegSSAN, row, "SocialSecNum")
        ''BindControl(Me.lblRegInvestorID, row, "InvestorID")
        ''BindControl(Me.txtRegFirstName, row, "FirstName")
        ''BindControl(Me.txtRegMiddleName, row, "MiddleName")
        ''BindControl(Me.txtRegLastName, row, "LastName")
        ''BindControl(Me.txtPayorName, row, "LastName")
        ''BindControl(Me.txtRegAddress1, row, "Address1")
        ''BindControl(Me.txtRegAddress2, row, "Address2")
        ''BindControl(Me.txtRegCity, row, "City")
        ''BindControl(Me.txtRegState, row, "State")
        ''BindControl(Me.txtRegZip, row, "PostalCode")
        ''BindControl(Me.txtRegPhone, row, "PhoneNumber")
        ''BindControl(Me.txtRegEmail, row, "EMailAddress")
        ''BindControl(Me.txtNewWorldVID, row, "NW_Vendor")

        Dim SQL As String = String.Format("select * from genii_user.tr " & _
                                          " where taxIDNumber='{0}' and taxyear={1} ", Me.txtTaxID.Text, ddlTaxYear.Text)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblSSAN As New DataTable()

            adt.Fill(tblSSAN)

            If tblSSAN.Rows.Count > 0 Then
                If (Not IsDBNull(tblSSAN.Rows(0)("TAXROLLNUMBER"))) Then
                    txtTaxRollNumber.Text = Convert.ToString(tblSSAN.Rows(0)("TAXROLLNUMBER"))
                End If
                If (Not IsDBNull(tblSSAN.Rows(0)("TAXIDNUMBER"))) Then
                    txtTaxID.Text = Convert.ToString(tblSSAN.Rows(0)("TAXIDNUMBER"))
                End If
                If (Not IsDBNull(tblSSAN.Rows(0)("APN"))) Then
                    txtAPN.Text = Convert.ToString(tblSSAN.Rows(0)("APN"))
                End If

                If (Not IsDBNull(tblSSAN.Rows(0)("OWNER_NAME_1"))) Then
                    txtRegInvestorID.Text = Convert.ToString(tblSSAN.Rows(0)("OWNER_NAME_1"))
                End If
            End If
        End Using



    End Sub

    Protected Sub btnSaveApportionment_Click(sender As Object, e As System.EventArgs) Handles btnSaveAll.Click
        'NOT USED ANYMORE
        'NOT USED ANYMORE
        'NOT USED ANYMORE
        'NOT USED ANYMORE
        'NOT USED ANYMORE
        'NOT USED ANYMORE
        Using conn As New OleDbConnection(Me.ConnectString)
            ' Prepare tables and adapters.
            conn.Open()

            ''Dim adtCalendar As New OleDbDataAdapter("SELECT * FROM genii_user.TR_CALENDAR", conn)
            ''Dim dtCalendar As New DataTable("TR_CALENDAR")

            ''adtCalendar.FillSchema(dtCalendar, SchemaType.Source)
            ''adtCalendar.InsertCommand = New OleDbCommandBuilder(adtCalendar).GetInsertCommand()

            Dim adtPayments As New OleDbDataAdapter("SELECT * FROM genii_user.TR_PAYMENTS", conn)
            Dim dtPayments As New DataTable("TR_PAYMENTS")

            adtPayments.FillSchema(dtPayments, SchemaType.Source)
            adtPayments.InsertCommand = New OleDbCommandBuilder(adtPayments).GetInsertCommand()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            ''adtCalendar.InsertCommand.Transaction = trans
            adtPayments.InsertCommand.Transaction = trans

            ' Get new record id.
            ''Dim newRecordID As Integer = GetNewID("RECORD_ID", "genii_user.TR_CALENDAR", conn, trans)

            ' Save payments
            '  For Each payRow As DataRow In Me.CashierTransactionsTable.Select("TAX_YEAR=" & _ taxYear + " AND TAX_ROLL_NUMBER = " & _ taxRollNumber + " AND IS_APPORTIONED IS NULL OR IS_APPORTIONED <> 1") 'MTA 04052013 change sql; add " and roll id, tax year, payment date, payment amount = ?,?,?,?"
            ''For Each payRow As DataRow In Me.CashierTransactionsTable.Select("IS_APPORTIONED IS NULL OR IS_APPORTIONED <> 1 AND TAX_ROLL_NUMBER=" & _ +" AND TAX_YEAR=" & _ +" AND ")
            '   taxYear = payRow("TAX_YEAR") ' MTA change this to current tax year
            ' taxRollNumber = payRow("TAX_ROLL_NUMBER") ' MTA change this to current tax roll number
            ' '  'paymentAmount = payRow("TAX_AMT") ' MTA change this to current tax amount
            '  paymentDate = payRow("PAYMENT_DATE") ' MTA change this to current date

            For Each rowScanned As DataRow In Me.CashierTransactionsTable().Rows
                'Dim newCalendar As DataRow = dtCalendar.NewRow()
                'newCalendar("RECORD_ID") = newRecordID
                'newCalendar("TaxYear") = rowScanned("TAX_YEAR")
                'newCalendar("TaxRollNumber") = rowScanned("TAX_ROLL_NUMBER")
                'newCalendar("TASK_ID") = 101    ' Owner payment
                'newCalendar("TASK_DATE") = rowScanned("PAYMENT_DATE")
                'newCalendar("ADMIN_REVIEW") = 0
                'newCalendar("EDIT_USER") = TaxPayments.CurrentUserName
                'newCalendar("EDIT_DATE") = Date.Now
                'newCalendar("CREATE_USER") = newCalendar("EDIT_USER")
                'newCalendar("CREATE_DATE") = Date.Now
                'dtCalendar.Rows.Add(newCalendar)

                Dim newPayment As DataRow = dtPayments.NewRow()

                ''newPayment("RECORD_ID") = newRecordID
                ' Following two lines should be removed once TaxYear and TaxRollNumber are removed from TR_PAYMENTS.
                newPayment("TRANS_ID") = rowScanned("RECORD_ID")
                newPayment("TaxYear") = rowScanned("TAX_YEAR")
                newPayment("TaxRollNumber") = rowScanned("TAX_ROLL_NUMBER")

                newPayment("PaymentEffectiveDate") = CDate(rowScanned("PAYMENT_DATE")).Date
                newPayment("PaymentTypeCode") = ddlPaymentType.SelectedValue '4   ' Scanned Payment
                newPayment("PaymentMadeByCode") = 1 ' Owner Paid
                newPayment("Pertinent1") = txtPayerName.Text 'rowScanned("PAYOR_NAME")
                newPayment("Pertinent2") = If(rowScanned.IsNull("CHECK_NUMBER"), DBNull.Value, "Check # " & rowScanned("CHECK_NUMBER"))
                newPayment("PaymentAmount") = rowScanned("PAYMENT_AMT")
                newPayment("CalcPayDate") = rowScanned("PAYMENT_DATE")
                dtPayments.Rows.Add(newPayment)

                ''newRecordID += 1
            Next

            Try
                ' Save calendar tables.
                ''adtCalendar.Update(dtCalendar)
                adtPayments.Update(dtPayments)
            Catch ex As Exception
                trans.Rollback()
                conn.Close()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try

            trans.Commit()
            conn.Close()
        End Using

        ' Clear session
        Me.SessionDataset = Nothing
    End Sub
    Protected Sub SaveApportionments(taxRollNumber As String, taxYear As String)
        Using conn As New OleDbConnection(Me.ConnectString)
            ' Prepare tables and adapters.
            conn.Open()

            ''Dim adtCalendar As New OleDbDataAdapter("SELECT * FROM genii_user.TR_CALENDAR", conn)
            ''Dim dtCalendar As New DataTable("TR_CALENDAR")

            ''adtCalendar.FillSchema(dtCalendar, SchemaType.Source)
            ''adtCalendar.InsertCommand = New OleDbCommandBuilder(adtCalendar).GetInsertCommand()

            Dim adtPayments As New OleDbDataAdapter("SELECT * FROM genii_user.TR_PAYMENTS", conn)
            Dim dtPayments As New DataTable("TR_PAYMENTS")

            adtPayments.FillSchema(dtPayments, SchemaType.Source)
            adtPayments.InsertCommand = New OleDbCommandBuilder(adtPayments).GetInsertCommand()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            ''adtCalendar.InsertCommand.Transaction = trans
            adtPayments.InsertCommand.Transaction = trans

            ' Get new record id.
            ''Dim newRecordID As Integer = GetNewID("RECORD_ID", "genii_user.TR_CALENDAR", conn, trans)

            ' Save payments
            For Each rowScanned As DataRow In Me.CashierTransactionsTable().Rows
                '   For Each rowScanned As DataRow In Me.CashierTransactionsTable.Select(" TAX_YEAR=" + taxYear + " AND TAX_ROLL_NUMBER=" + taxRollNumber + " ")
                ' For Each rowScanned As DataRow In Me.CashierTransactionsTable.Select("TAX_YEAR=" + taxYear + " AND TAX_ROLL_NUMBER = " + taxRollNumber + " ")
                '  For Each rowScanned As DataRow In Me.CashierTransactionsTable.Select("TAX_YEAR=" + taxYear + " AND TAX_ROLL_NUMBER= " + taxRollNumber + "")

                If (rowScanned("TAX_YEAR") = taxYear And rowScanned("TAX_ROLL_NUMBER") = taxRollNumber) Then
                    Dim newPayment As DataRow = dtPayments.NewRow()

                    ''newPayment("RECORD_ID") = newRecordID
                    ' Following two lines should be removed once TaxYear and TaxRollNumber are removed from TR_PAYMENTS.
                    newPayment("TRANS_ID") = rowScanned("RECORD_ID")
                    newPayment("TaxYear") = rowScanned("TAX_YEAR")
                    newPayment("TaxRollNumber") = rowScanned("TAX_ROLL_NUMBER")

                    newPayment("PaymentEffectiveDate") = CDate(rowScanned("PAYMENT_DATE")).Date
                    newPayment("PaymentTypeCode") = ddlPaymentType.SelectedValue '4   ' Scanned Payment
                    newPayment("PaymentMadeByCode") = 1 ' Owner Paid
                    newPayment("Pertinent1") = txtPayerName.Text 'rowScanned("PAYOR_NAME")
                    newPayment("Pertinent2") = If(rowScanned.IsNull("CHECK_NUMBER"), DBNull.Value, "Check # " & rowScanned("CHECK_NUMBER"))
                    newPayment("PaymentAmount") = rowScanned("PAYMENT_AMT")
                    newPayment("CalcPayDate") = rowScanned("PAYMENT_DATE")
                    newPayment("PAYMENT_RULE") = _TRPaymentRule

                    newPayment("CREATE_USER") = System.Web.HttpContext.Current.User.Identity.Name
                    newPayment("CREATE_DATE") = Date.Now
                    newPayment("EDIT_USER") = System.Web.HttpContext.Current.User.Identity.Name
                    newPayment("EDIT_DATE") = Date.Now

                    dtPayments.Rows.Add(newPayment)

                End If

                ''newRecordID += 1
            Next

            Try
                ' Save calendar tables.
                ''adtCalendar.Update(dtCalendar)
                adtPayments.Update(dtPayments)
            Catch ex As Exception
                trans.Rollback()
                conn.Close()
                ' Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try

            trans.Commit()
            conn.Close()
        End Using

        ' Clear session
        Me.SessionDataset = Nothing
    End Sub

    Protected Sub btnLogin_Click(sender As Object, e As System.EventArgs) Handles btnLogin.Click
        CreateNewSession()
    End Sub


    Protected Sub btnLogout_Click(sender As Object, e As System.EventArgs) Handles btnLogout.Click
        DoLogout()
        ' Response.Redirect("TaxPayments.aspx")

    End Sub
    Protected Sub btnRejectPayment_Click(sender As Object, e As System.EventArgs) Handles btnDecline.Click
        Dim paymentAmount As Decimal = Utilities.GetDecimalOrZero(Me.txtAmountPaid.Text)
        Dim taxAmount As Decimal = Utilities.GetDecimalOrZero(Me.txtAmountPaid.Text)
        '  Dim txtDiff As Decimal = Utilities.GetDecimalOrZero(Me.txtDifference.Text)
        Dim diff As Decimal = taxAmount - (txtAmountPaid.Text)



        'If diff <> txtDiff Then
        '    ClientScript.RegisterStartupScript(Me.GetType(), "DiffChanged", "showMessage('Amount Paid, Amount Due or Difference is not correct. Please check values and try again.', 'Retry');", True)
        '    Exit Sub
        'End If

        SaveDeclinedPayment(Me.txtDeclineReason.Text)



        Me.txtBarcode.Text = String.Empty
        '  Me.btnSavePayment.Enabled = False
        '  Me.btnRejectPayment.Enabled = False
        '   Me.btnCreateReceipt.Visible = True
        '    ShowLetterQueuer(-diff)

        BindPendingPaymentsGrids()


        'If (_TRPaymentRule = 1) Then
        '    Me.divPaymentRemark.Visible = True
        '    lblPaymentRemark.Text = "Both halves paid."
        'ElseIf (_TRPaymentRule = 2) Then
        '    Me.divPaymentRemark.Visible = True
        '    lblPaymentRemark.Text = "Total less than $100, tax not split."
        'ElseIf (_TRPaymentRule = 3) Then
        '    Me.divPaymentRemark.Visible = True
        '    lblPaymentRemark.Text = "First half tax payment made."
        'ElseIf (_TRPaymentRule = 4) Then
        '    Me.divPaymentRemark.Visible = True
        '    lblPaymentRemark.Text = "Total tax paid by the End of December."
        'End If

    End Sub


    Private Sub CalculateApportionmentsOnSavePaymentNoFees(taxRollNumber As String, taxYear As String, paymentAmount As Decimal)


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            ' Call GetApportionment SQL function for each payment.
            Dim paymentDate As Date 'check payment... how to .. if payment payed or payment due  ''''paymentAmount As Decimal,
            ' taxYear As Integer, taxRollNumber As String, 

            Me.ApportionDetailsTable.Clear()

            For Each payRow As DataRow In Me.CashierTransactionsTable.Select("TAX_YEAR=" + taxYear + " AND TAX_ROLL_NUMBER = " + taxRollNumber + " AND TRANSACTION_STATUS IS NULL")
                ''MTA 04052013 change sql; add " and roll id, tax year, payment date, payment amount = ?,?,?,?"
                'For Each payRow As DataRow In Me.CashierTransactionsTable.Select("IS_APPORTIONED IS NULL OR IS_APPORTIONED <> 1 AND TAX_ROLL_NUMBER=" & _ +" AND TAX_YEAR=" & _ +" AND ")
                taxYear = payRow("TAX_YEAR") ' MTA change this to current tax year
                taxRollNumber = payRow("TAX_ROLL_NUMBER") ' MTA change this to current tax roll number
                'paymentAmount = payRow("TAX_AMT") ' MTA change this to current tax amount
                paymentDate = payRow("PAYMENT_DATE") ' MTA change this to current date


                Dim cmd As New OleDbCommand("SELECT * FROM dbo.GetApportionment_No_Fees(?,?,?,?)", conn)

                cmd.Parameters.AddWithValue("@TaxYear", taxYear)
                cmd.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber)
                cmd.Parameters.AddWithValue("@PaymentAmount", paymentAmount)
                cmd.Parameters.AddWithValue("@PaymentDate", paymentDate)

                Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                While rdr.Read()
                    Dim row As DataRow = Me.ApportionDetailsTable.NewRow()

                    'row("RECORD_ID") = GetNewID("RECORD_ID", Me.ApportionDetailsTable)
                    row("TRANS_ID") = payRow("RECORD_ID")
                    row("TaxYear") = rdr.Item("TaxYear")
                    row("TaxRollNumber") = rdr.Item("TaxRollNumber")
                    row("AreaCode") = rdr.Item("AreaCode")
                    row("TaxChargeCodeID") = rdr.Item("TaxChargeCodeID")
                    row("TaxTypeID") = rdr.Item("TaxTypeID")
                    row("PaymentDate") = rdr.Item("PaymentDate")
                    row("GLAccount") = rdr.Item("GLAccount")
                    row("SentToOtherSystem") = rdr.Item("SentToOtherSystem")
                    row("ReceiptNumber") = rdr.Item("ReceiptNumber")
                    row("DateApportioned") = rdr.Item("DateApportioned")
                    row("DollarAmount") = rdr.Item("DollarAmount")
                    row("EDIT_USER") = Me.SessionRow("CASHIER").ToString()
                    row("EDIT_DATE") = Date.Now
                    row("CREATE_USER") = Me.SessionRow("CASHIER").ToString()
                    row("CREATE_DATE") = Date.Now

                    Me.ApportionDetailsTable.Rows.Add(row)
                End While

                payRow("TRANSACTION_STATUS") = 1
            Next
        End Using

        CommitDataset()
        BindPendingPaymentsGrids()
        BindApportionsGrid()
    End Sub
    Public Sub vSumApportion(taxYear As String, taxRollNumber As String, paymentAmount As Double)
        'check taxyear and taxrollnumber in vsumApportion view. if with prior payment, goto getapportionment_no fees
        Dim PaidAmount As Double

        Dim SQL As String = String.Format("select Paid from dbo.vsumApportion " & _
                                          " where taxYear='{0}' and TaxRollNumber='{1}' ", taxYear, taxRollNumber)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblPayment As New DataTable()

            adt.Fill(tblPayment)

            If tblPayment.Rows.Count > 0 Then
                If (Not IsDBNull(tblPayment.Rows(0)("Paid"))) Then
                    PaidAmount = Convert.ToDouble(tblPayment.Rows(0)("Paid"))
                End If
            End If
        End Using

        If (PaidAmount > 0) Then
            CalculateApportionmentsOnSavePaymentNoFees(taxRollNumber, taxYear, paymentAmount)
        End If


    End Sub

    Public Sub SaveStateApportionRecords(taxyear As String, taxrollnumber As String, paymentAmount As Double, interest As Double, faceValue As Double, redemptionFee As Double)
        Dim chargeAmount As Double
        Dim chargeAmount2 As Double
        Dim chargeAmount3 As Double

        Dim SQL As String = String.Format("select parameter from genii_user.st_parameter " & _
                                          " where record_id='{0}' ", 99920)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCharges As New DataTable()

            adt.Fill(tblCharges)

            If tblCharges.Rows.Count > 0 Then
                If (Not IsDBNull(tblCharges.Rows(0)("parameter"))) Then
                    chargeAmount = Convert.ToDouble(tblCharges.Rows(0)("parameter"))
                End If
            End If
        End Using

        Dim SQL2 As String = String.Format("select parameter from genii_user.st_parameter " & _
                                          " where record_id='{0}' ", 99930)

        Using adt As New OleDbDataAdapter(SQL2, Me.ConnectString)
            Dim tblCharges As New DataTable()

            adt.Fill(tblCharges)

            If tblCharges.Rows.Count > 0 Then
                If (Not IsDBNull(tblCharges.Rows(0)("parameter"))) Then
                    chargeAmount2 = Convert.ToDouble(tblCharges.Rows(0)("parameter"))
                End If
            End If
        End Using



        Dim SQL4 As String = String.Format("select record_id from genii_user.cashier_transactions " & _
                                          " where tax_year='{0}' and tax_roll_number= '{1}' ", taxyear, taxrollnumber)
        Dim recordIDCashierTrans As Integer

        Using adt As New OleDbDataAdapter(SQL4, Me.ConnectString)
            Dim tblTrans As New DataTable()

            adt.Fill(tblTrans)

            If tblTrans.Rows.Count > 0 Then
                If (Not IsDBNull(tblTrans.Rows(0)("RECORD_ID"))) Then
                    recordIDCashierTrans = Convert.ToInt32(tblTrans.Rows(0)("RECORD_ID"))
                End If
            End If
        End Using


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try


                'Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_APPORTION", conn, trans)

                'Dim cmdNewRecApportion As New OleDbCommand("INSERT INTO genii_user.CASHIER_APPORTION " & _
                '                                  "( TRANS_ID, TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                '                                  " TaxTypeID,PaymentDate,GLAccount, " & _
                '                                  " DateApportioned, DollarAmount,  " & _
                '                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                'cmdNewRecApportion.Connection = conn
                'cmdNewRecApportion.Transaction = trans

                'With cmdNewRecApportion.Parameters
                '    '  .AddWithValue("@Record_ID", recordID)
                '    .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                '    '  .AddWithValue("@AreaCode", row3("AreaCode"))
                '    .AddWithValue("@TaxYear", taxyear)
                '    .AddWithValue("@TaxRollNumber", taxrollnumber)
                '    .AddWithValue("@TaxChargeCodeID", 99930)
                '    .AddWithValue("@TaxTypeID", 75)
                '    .AddWithValue("@PaymentDate", Date.Now)
                '    .AddWithValue("@GLAccount", "N00100547180")
                '    .AddWithValue("@DateApportioned", Date.Now)
                '    .AddWithValue("@DollarAmount", redemptionFee)

                '    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@CREATE_DATE", Date.Now)

                'End With

                'cmdNewRecApportion.ExecuteNonQuery()

                'Dim cmdNewRecApportion2 As New OleDbCommand("INSERT INTO genii_user.CASHIER_APPORTION " & _
                '                                  "( TRANS_ID, TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                '                                  " TaxTypeID,PaymentDate,GLAccount, " & _
                '                                  " DateApportioned, DollarAmount,  " & _
                '                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                'cmdNewRecApportion2.Connection = conn
                'cmdNewRecApportion2.Transaction = trans

                'With cmdNewRecApportion2.Parameters
                '    '    .AddWithValue("@Record_ID", recordID + 1)
                '    .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                '    '  .AddWithValue("@AreaCode", row3("AreaCode"))
                '    .AddWithValue("@TaxYear", taxyear)
                '    .AddWithValue("@TaxRollNumber", taxrollnumber)
                '    .AddWithValue("@TaxChargeCodeID", 99920)
                '    .AddWithValue("@TaxTypeID", 75)
                '    .AddWithValue("@PaymentDate", Date.Now)
                '    .AddWithValue("@GLAccount", "N00100547180")
                '    .AddWithValue("@DateApportioned", Date.Now)
                '    .AddWithValue("@DollarAmount", chargeAmount)

                '    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@CREATE_DATE", Date.Now)

                'End With

                'cmdNewRecApportion2.ExecuteNonQuery()



                Dim cmdNewRecCharges As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                                                                      "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                                      " TaxTypeID,ChargeAmount, " & _
                                                                      " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                                      " VALUES (?,?,?,?,?,?,?,?,?)")

                cmdNewRecCharges.Connection = conn
                cmdNewRecCharges.Transaction = trans

                With cmdNewRecCharges.Parameters
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@TaxChargeCodeID", 99930)
                    .AddWithValue("@TaxTypeID", 75)
                    .AddWithValue("@ChargeAmount", chargeAmount2)

                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCharges.ExecuteNonQuery()

                Dim cmdNewRecCharges2 As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                                                                      "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                                      " TaxTypeID,ChargeAmount, " & _
                                                                      " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                                      " VALUES (?,?,?,?,?,?,?,?,?)")

                cmdNewRecCharges2.Connection = conn
                cmdNewRecCharges2.Transaction = trans

                With cmdNewRecCharges2.Parameters
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@TaxChargeCodeID", 99920)
                    .AddWithValue("@TaxTypeID", 75)
                    .AddWithValue("@ChargeAmount", chargeAmount)

                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCharges2.ExecuteNonQuery()


                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                '  Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using

    End Sub


    Public Sub SaveInvestorApportionRecords(taxyear As String, taxrollnumber As String, paymentAmount As Double, interest As Double, faceValue As Double, redemptionFee As Double)
        Dim chargeAmount As Double
        'Dim chargeAmount2 As Double
        Dim chargeAmount3 As Double

        Dim SQL As String = String.Format("select parameter from genii_user.st_parameter " & _
                                          " where record_id='{0}' ", 99930)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCharges As New DataTable()

            adt.Fill(tblCharges)

            If tblCharges.Rows.Count > 0 Then
                If (Not IsDBNull(tblCharges.Rows(0)("parameter"))) Then
                    chargeAmount = Convert.ToDouble(tblCharges.Rows(0)("parameter"))
                End If
            End If
        End Using

        'Dim SQL2 As String = String.Format("select parameter from genii_user.st_parameter " & _
        '                                  " where record_id='{0}' ", 99922)

        'Using adt As New OleDbDataAdapter(SQL2, Me.ConnectString)
        '    Dim tblCharges2 As New DataTable()



        '    adt.Fill(tblCharges2)

        '    If tblCharges2.Rows.Count > 0 Then
        '        If (Not IsDBNull(tblCharges2.Rows(0)("parameter"))) Then
        '            chargeAmount2 = Convert.ToDouble(tblCharges2.Rows(0)("parameter"))
        '        End If
        '    End If
        'End Using

        Dim SQL3 As String = String.Format("select parameter from genii_user.st_parameter " & _
                                          " where record_id='{0}' ", 99932)

        Using adt As New OleDbDataAdapter(SQL3, Me.ConnectString)
            Dim tblCharges As New DataTable()

            adt.Fill(tblCharges)

            If tblCharges.Rows.Count > 0 Then
                If (Not IsDBNull(tblCharges.Rows(0)("parameter"))) Then
                    chargeAmount3 = Convert.ToDouble(tblCharges.Rows(0)("parameter"))
                End If
            End If
        End Using

        Dim SQL4 As String = String.Format("select record_id from genii_user.cashier_transactions " & _
                                          " where tax_year='{0}' and tax_roll_number= '{1}' ", taxyear, taxrollnumber)
        Dim recordIDCashierTrans As Integer

        Using adt As New OleDbDataAdapter(SQL4, Me.ConnectString)
            Dim tblTrans As New DataTable()

            adt.Fill(tblTrans)

            If tblTrans.Rows.Count > 0 Then
                If (Not IsDBNull(tblTrans.Rows(0)("RECORD_ID"))) Then
                    recordIDCashierTrans = Convert.ToInt32(tblTrans.Rows(0)("RECORD_ID"))
                End If
            End If
        End Using


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try


                Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_APPORTION", conn, trans)

                Dim cmdNewRecApportion As New OleDbCommand("INSERT INTO genii_user.CASHIER_APPORTION " & _
                                                  "( TRANS_ID, TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                  " TaxTypeID,PaymentDate,GLAccount, " & _
                                                  " DateApportioned, DollarAmount,  " & _
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecApportion.Connection = conn
                cmdNewRecApportion.Transaction = trans

                With cmdNewRecApportion.Parameters
                    '  .AddWithValue("@Record_ID", recordID)
                    .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                    '  .AddWithValue("@AreaCode", row3("AreaCode"))
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@TaxChargeCodeID", 99930)
                    .AddWithValue("@TaxTypeID", 75)
                    .AddWithValue("@PaymentDate", Date.Now)
                    .AddWithValue("@GLAccount", "N00100547180")
                    .AddWithValue("@DateApportioned", Date.Now)
                    .AddWithValue("@DollarAmount", redemptionFee)

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecApportion.ExecuteNonQuery()

                Dim cmdNewRecApportion2 As New OleDbCommand("INSERT INTO genii_user.CASHIER_APPORTION " & _
                                                  "( TRANS_ID, TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                  " TaxTypeID,PaymentDate,GLAccount, " & _
                                                  " DateApportioned, DollarAmount,  " & _
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecApportion2.Connection = conn
                cmdNewRecApportion2.Transaction = trans

                With cmdNewRecApportion2.Parameters
                    '    .AddWithValue("@Record_ID", recordID + 1)
                    .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                    '  .AddWithValue("@AreaCode", row3("AreaCode"))
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@TaxChargeCodeID", 99922)
                    .AddWithValue("@TaxTypeID", 91)
                    .AddWithValue("@PaymentDate", Date.Now)
                    .AddWithValue("@GLAccount", "T00880321900")
                    .AddWithValue("@DateApportioned", Date.Now)
                    .AddWithValue("@DollarAmount", interest)

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecApportion2.ExecuteNonQuery()

                Dim cmdNewRecApportion3 As New OleDbCommand("INSERT INTO genii_user.CASHIER_APPORTION " & _
                                                 "( TRANS_ID, TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                 " TaxTypeID,PaymentDate,GLAccount, " & _
                                                 " DateApportioned, DollarAmount,  " & _
                                                 " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                 " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecApportion3.Connection = conn
                cmdNewRecApportion3.Transaction = trans

                With cmdNewRecApportion3.Parameters
                    '  .AddWithValue("@Record_ID", recordID + 2)
                    .AddWithValue("@TRANS_ID", recordIDCashierTrans) ' payRow("Record_ID"))
                    '  .AddWithValue("@AreaCode", row3("AreaCode"))
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@TaxChargeCodeID", 99932)
                    .AddWithValue("@TaxTypeID", 92)
                    .AddWithValue("@PaymentDate", Date.Now)
                    .AddWithValue("@GLAccount", "T00880321900")
                    .AddWithValue("@DateApportioned", Date.Now)
                    .AddWithValue("@DollarAmount", faceValue)

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecApportion3.ExecuteNonQuery()

                Dim cmdNewRecCharges As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                                                                      "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                                      " TaxTypeID,ChargeAmount, " & _
                                                                      " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                                      " VALUES (?,?,?,?,?,?,?,?,?)")

                cmdNewRecCharges.Connection = conn
                cmdNewRecCharges.Transaction = trans

                With cmdNewRecCharges.Parameters
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@TaxChargeCodeID", 99930)
                    .AddWithValue("@TaxTypeID", 75)
                    .AddWithValue("@ChargeAmount", redemptionFee)

                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCharges.ExecuteNonQuery()

                Dim cmdNewRecCharges2 As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                                                                      "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                                      " TaxTypeID,ChargeAmount, " & _
                                                                      " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                                      " VALUES (?,?,?,?,?,?,?,?,?)")

                cmdNewRecCharges2.Connection = conn
                cmdNewRecCharges2.Transaction = trans

                With cmdNewRecCharges2.Parameters
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@TaxChargeCodeID", 99922)
                    .AddWithValue("@TaxTypeID", 91)
                    .AddWithValue("@ChargeAmount", interest)

                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCharges2.ExecuteNonQuery()

                Dim cmdNewRecCharges3 As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                                                                      "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                                      " TaxTypeID,ChargeAmount, " & _
                                                                      " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                                      " VALUES (?,?,?,?,?,?,?,?,?)")

                cmdNewRecCharges3.Connection = conn
                cmdNewRecCharges3.Transaction = trans

                With cmdNewRecCharges3.Parameters
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@TaxChargeCodeID", 99932)
                    .AddWithValue("@TaxTypeID", 92)
                    .AddWithValue("@ChargeAmount", faceValue)

                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCharges3.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using

    End Sub
    Public Sub SaveTransactionInvestor(taxyear As String, taxrollnumber As String, paymentAmount As Double, pertinent2 As String, grpKey As Integer, interest As Double)

        Dim chargeAmount As Double
        Dim chargeAmount2 As Double

        Dim SQL As String = String.Format("select parameter from genii_user.st_parameter " & _
                                          " where record_id='{0}' ", 99930)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCharges As New DataTable()

            adt.Fill(tblCharges)

            If tblCharges.Rows.Count > 0 Then
                If (Not IsDBNull(tblCharges.Rows(0)("parameter"))) Then
                    chargeAmount = Convert.ToDouble(tblCharges.Rows(0)("parameter"))
                End If
            End If
        End Using

        'Dim SQL2 As String = String.Format("select parameter from genii_user.st_parameter " & _
        '                                  " where record_id='{0}' ", 99920)

        'Using adt As New OleDbDataAdapter(SQL2, Me.ConnectString)
        '    Dim tblCharges2 As New DataTable()



        '    adt.Fill(tblCharges2)

        '    If tblCharges2.Rows.Count > 0 Then
        '        If (Not IsDBNull(tblCharges2.Rows(0)("parameter"))) Then
        '            chargeAmount2 = Convert.ToDouble(tblCharges2.Rows(0)("parameter"))
        '        End If
        '    End If
        'End Using


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)
            '       Dim trans2 As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)
            Try
                '   ' Get new record id.
                '   Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_SESSION", conn, trans)


                ' Create new record.
                Dim cmdNewRec As New OleDbCommand("UPDATE genii_user.TR_CP " & _
                                                  " SET CP_STATUS = ?, DATE_REDEEMED = ?,INTEREST_EARNED=?,EDIT_USER=?,EDIT_DATE=? " & _
                                                  " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                cmdNewRec.Connection = conn
                cmdNewRec.Transaction = trans

                With cmdNewRec.Parameters

                    .AddWithValue("@CP_STATUS", 5)
                    .AddWithValue("@DATE_REDEEMED", Date.Now)
                    .AddWithValue("@INTEREST_EARNED", interest)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxyear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)


                End With

                cmdNewRec.ExecuteNonQuery()

              

                'Dim cmdUpdateCashierTrans As New OleDbCommand("UPDATE genii_user.TR_CP " & _
                '                                 " SET CP_STATUS = ?, DATE_REDEEMED = ?,EDIT_USER=?,EDIT_DATE=? " & _
                '                                 " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                'cmdUpdateCashierTrans.Connection = conn
                'cmdUpdateCashierTrans.Transaction = trans

                'With cmdUpdateCashierTrans.Parameters

                '    .AddWithValue("@CP_STATUS", 5)
                '    .AddWithValue("@DATE_REDEEMED", Date.Now)
                '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@TaxYear", taxyear) 'currentTaxYear)
                '    .AddWithValue("@TaxRollNumber", taxrollnumber)


                'End With

                'cmdUpdateCashierTrans.ExecuteNonQuery()


                Dim cmdUpdateTR As New OleDbCommand("UPDATE genii_user.TR " & _
                                                  " SET STATUS = ?, CurrentBalance = ?,EDIT_USER=?,EDIT_DATE=? " & _
                                                  " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                cmdUpdateTR.Connection = conn
                cmdUpdateTR.Transaction = trans

                With cmdUpdateTR.Parameters

                    .AddWithValue("@STATUS", 5)
                    .AddWithValue("@CurrentBalance", 0.0)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxyear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)


                End With

                cmdUpdateTR.ExecuteNonQuery()


                'Dim cmdNewRecCharges As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                '                                              "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                '                                              " TaxTypeID,ChargeAmount, " & _
                '                                              " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                                              " VALUES (?,?,?,?,?,?,?,?,?)")

                'cmdNewRecCharges.Connection = conn
                'cmdNewRecCharges.Transaction = trans

                'With cmdNewRecCharges.Parameters
                '    .AddWithValue("@TaxYear", taxyear)
                '    .AddWithValue("@TaxRollNumber", taxrollnumber)
                '    .AddWithValue("@TaxChargeCodeID", 99922)
                '    .AddWithValue("@TaxTypeID", 75)
                '    .AddWithValue("@ChargeAmount", chargeAmount2)

                '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@CREATE_DATE", Date.Now)

                'End With

                'cmdNewRecCharges.ExecuteNonQuery()

                'Dim cmdNewRecCharges2 As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                '                                               "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                '                                               " TaxTypeID,ChargeAmount, " & _
                '                                               " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                                               " VALUES (?,?,?,?,?,?,?,?,?)")

                'cmdNewRecCharges2.Connection = conn
                'cmdNewRecCharges2.Transaction = trans

                'With cmdNewRecCharges2.Parameters
                '    .AddWithValue("@TaxYear", taxyear)
                '    .AddWithValue("@TaxRollNumber", taxrollnumber)
                '    .AddWithValue("@TaxChargeCodeID", 99930)
                '    .AddWithValue("@TaxTypeID", 75)
                '    .AddWithValue("@ChargeAmount", faceValue)

                '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@CREATE_DATE", Date.Now)

                'End With

                'cmdNewRecCharges2.ExecuteNonQuery()

                'Dim row As DataRow = Me.CashierTransactionsTable.NewRow()

                'row("SESSION_ID") = Me.SessionRecordID
                'row("TAX_YEAR") = taxyear
                'row("TAX_ROLL_NUMBER") = taxrollnumber
                'row("PAYMENT_DATE") = Me.txtPaymentDate.Text
                'row("PAYMENT_TYPE") = Me.ddlPaymentType.SelectedValue
                'row("PAYOR_NAME") = Me.txtPayerName.Text

                'If Me.ddlPaymentType.SelectedValue = 2 Then
                '    row("CHECK_NUMBER") = Me.txtCheckNumber.Text
                'End If

                'row("BARCODE") = Me.txtBarcode.Text
                'row("PAYMENT_AMT") = paymentAmount 'Utilities.GetDecimalOrDBNull(Me.txtAmountPaid.Text)
                'row("TAX_AMT") = paymentAmount
                'row("KITTY_AMT") = 0
                'row("REFUND_AMT") = 0
                'row("EDIT_USER") = TaxPayments.CurrentUserName
                'row("EDIT_DATE") = Date.Now
                'row("CREATE_USER") = TaxPayments.CurrentUserName
                'row("CREATE_DATE") = Date.Now

                'Me.CashierTransactionsTable.Rows.Add(row)
                ''   CommitDataset()

                'Dim cmdNewRecCashierTrans As New OleDbCommand("INSERT INTO genii_user.CASHIER_TRANSACTIONS " & _
                '                               "(RECORD_ID,SESSION_ID,GROUP_KEY, TRANSACTION_STATUS, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_DATE, " & _
                '                               " PAYMENT_TYPE,APPLY_TO, LETTER_TAG, REFUND_TAG, PAYOR_NAME,CHECK_NUMBER, " & _
                '                               " PAYMENT_AMT, TAX_AMT, " & _
                '                               " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                               " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                'cmdNewRecCashierTrans.Connection = conn
                'cmdNewRecCashierTrans.Transaction = trans

                'Dim recordIDCashierTrans As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_TRANSACTIONS", conn, trans)
                '' taxrollnumber = row2("TaxRollNumber")
                '' Dim isApportioned As String = 1

                'With cmdNewRecCashierTrans.Parameters
                '    .AddWithValue("@RECORD_ID", recordIDCashierTrans)
                '    .AddWithValue("@SESSION_ID", Me.lblSessionID.Text)
                '    .AddWithValue("@GROUP_KEY", )
                '    .AddWithValue("@TRANSACTION_STATUS", 1)
                '    .AddWithValue("@TAX_YEAR", taxyear)
                '    .AddWithValue("@TAX_ROLL_NUMBER", taxrollnumber)
                '    .AddWithValue("@PAYMENT_DATE", Date.Now)
                '    .AddWithValue("@PAYMENT_TYPE", Me.ddlPaymentType.SelectedValue)
                '    .AddWithValue("@APPLY_TO", 2)
                '    .AddWithValue("@LETTER_TAG", 0)
                '    .AddWithValue("@REFUND_TAG", 1)
                '    .AddWithValue("@PAYOR_NAME", Me.txtRegSSAN.Text)
                '    .AddWithValue("@CHECK_NUMBER", Me.txtCheckNumber.Text)
                '    .AddWithValue("@PAYMENT_AMT", paymentAmount)
                '    .AddWithValue("@TAX_AMT", paymentAmount)
                '    ' .AddWithValue("@IS_APPORTIONED", isApportioned)

                '    .AddWithValue("@EDIT_USER", Me.lblOperatorName.Text)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@CREATE_USER", Me.lblOperatorName.Text)
                '    .AddWithValue("@CREATE_DATE", Date.Now)

                'End With

                'cmdNewRecCashierTrans.ExecuteNonQuery()

                Dim SQL3 As String = String.Format("select record_id from genii_user.cashier_transactions " & _
                                          " where tax_year='{0}' and tax_roll_number= '{1}' and transaction_status is null", taxyear, taxrollnumber)
                Dim recordIDCashierTrans As Integer

                Using adt As New OleDbDataAdapter(SQL3, Me.ConnectString)
                    Dim tblTrans As New DataTable()

                    adt.Fill(tblTrans)

                    If tblTrans.Rows.Count > 0 Then
                        If (Not IsDBNull(tblTrans.Rows(0)("RECORD_ID"))) Then
                            recordIDCashierTrans = Convert.ToInt32(tblTrans.Rows(0)("RECORD_ID"))
                        End If
                    End If
                End Using


                Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.TR_PAYMENTS " & _
                                                  "(TRANS_ID, TaxYear, TaxRollNumber, PaymentEffectiveDate, " & _
                                                  " PaymentTypeCode,PaymentMadeByCode,Pertinent1, " & _
                                                  " Pertinent2, PaymentAmount,  " & _
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecPayments.Connection = conn
                cmdNewRecPayments.Transaction = trans


                With cmdNewRecPayments.Parameters
                    .AddWithValue("@TRANS_ID", recordIDCashierTrans)
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@PaymentEffectiveDate", Date.Now)
                    .AddWithValue("@PaymentTypeCode", Me.ddlPaymentType.SelectedValue)
                    .AddWithValue("@PaymentMadeByCode", 3)
                    .AddWithValue("@Pertinent1", Me.txtRegSSAN.Text)
                    .AddWithValue("@Pertinent2", pertinent2 & "-" & Date.Now)
                    .AddWithValue("@PaymentAmount", paymentAmount)

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecPayments.ExecuteNonQuery()

                Dim cmdUpdateTrans As New OleDbCommand("UPDATE genii_user.CASHIER_TRANSACTIONS " & _
                                                " SET TRANSACTION_STATUS = ? " & _
                                                " WHERE TAX_YEAR=? AND TAX_ROLL_NUMBER=?  AND TRANSACTION_STATUS IS NULL")

                cmdUpdateTrans.Connection = conn
                cmdUpdateTrans.Transaction = trans

                With cmdUpdateTrans.Parameters

                    .AddWithValue("@TRANSACTION_STATUS", 1)
                    .AddWithValue("@TAX_YEAR", taxyear) 'currentTaxYear)
                    .AddWithValue("@TAX_ROLL_NUMBER", taxrollnumber)


                End With

                cmdUpdateTrans.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using
    End Sub

    Public Sub SaveTransactionState(taxyear As String, taxrollnumber As String, paymentAmount As Double, pertinent2 As String, grpKey As Integer, interest As Double)

        Dim chargeAmount As Double
        Dim chargeAmount2 As Double

        Dim SQL As String = String.Format("select parameter from genii_user.st_parameter " & _
                                          " where record_id='{0}' ", 99930)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCharges As New DataTable()

            adt.Fill(tblCharges)

            If tblCharges.Rows.Count > 0 Then
                If (Not IsDBNull(tblCharges.Rows(0)("parameter"))) Then
                    chargeAmount = Convert.ToDouble(tblCharges.Rows(0)("parameter"))
                End If
            End If
        End Using

        'Dim SQL2 As String = String.Format("select parameter from genii_user.st_parameter " & _
        '                                  " where record_id='{0}' ", 99920)

        'Using adt As New OleDbDataAdapter(SQL2, Me.ConnectString)
        '    Dim tblCharges2 As New DataTable()



        '    adt.Fill(tblCharges2)

        '    If tblCharges2.Rows.Count > 0 Then
        '        If (Not IsDBNull(tblCharges2.Rows(0)("parameter"))) Then
        '            chargeAmount2 = Convert.ToDouble(tblCharges2.Rows(0)("parameter"))
        '        End If
        '    End If
        'End Using


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                '   ' Get new record id.
                '   Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_SESSION", conn, trans)


                ' Create new record.
                Dim cmdNewRec As New OleDbCommand("UPDATE genii_user.TR_CP " & _
                                                  " SET CP_STATUS = ?, DATE_REDEEMED = ?,INTEREST_EARNED=?,EDIT_USER=?,EDIT_DATE=? " & _
                                                  " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                cmdNewRec.Connection = conn
                cmdNewRec.Transaction = trans

                With cmdNewRec.Parameters

                    .AddWithValue("@CP_STATUS", 5)
                    .AddWithValue("@DATE_REDEEMED", Date.Now)
                    .AddWithValue("@INTEREST_EARNED", 0.0)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxyear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)


                End With

                cmdNewRec.ExecuteNonQuery()

                'Dim cmdUpdateCashierTrans As New OleDbCommand("UPDATE genii_user.TR_CP " & _
                '                                 " SET CP_STATUS = ?, DATE_REDEEMED = ?,EDIT_USER=?,EDIT_DATE=? " & _
                '                                 " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                'cmdUpdateCashierTrans.Connection = conn
                'cmdUpdateCashierTrans.Transaction = trans

                'With cmdUpdateCashierTrans.Parameters

                '    .AddWithValue("@CP_STATUS", 5)
                '    .AddWithValue("@DATE_REDEEMED", Date.Now)
                '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@TaxYear", taxyear) 'currentTaxYear)
                '    .AddWithValue("@TaxRollNumber", taxrollnumber)


                'End With

                'cmdUpdateCashierTrans.ExecuteNonQuery()


                Dim cmdUpdateTR As New OleDbCommand("UPDATE genii_user.TR " & _
                                                  " SET STATUS = ?, CurrentBalance = ?,EDIT_USER=?,EDIT_DATE=? " & _
                                                  " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                cmdUpdateTR.Connection = conn
                cmdUpdateTR.Transaction = trans

                With cmdUpdateTR.Parameters

                    .AddWithValue("@STATUS", 5)
                    .AddWithValue("@CurrentBalance", 0.0)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxyear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)


                End With

                cmdUpdateTR.ExecuteNonQuery()


                'Dim cmdNewRecCharges As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                '                                              "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                '                                              " TaxTypeID,ChargeAmount, " & _
                '                                              " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                                              " VALUES (?,?,?,?,?,?,?,?,?)")

                'cmdNewRecCharges.Connection = conn
                'cmdNewRecCharges.Transaction = trans

                'With cmdNewRecCharges.Parameters
                '    .AddWithValue("@TaxYear", taxyear)
                '    .AddWithValue("@TaxRollNumber", taxrollnumber)
                '    .AddWithValue("@TaxChargeCodeID", 99922)
                '    .AddWithValue("@TaxTypeID", 75)
                '    .AddWithValue("@ChargeAmount", chargeAmount2)

                '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@CREATE_DATE", Date.Now)

                'End With

                'cmdNewRecCharges.ExecuteNonQuery()

                'Dim cmdNewRecCharges2 As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                '                                               "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                '                                               " TaxTypeID,ChargeAmount, " & _
                '                                               " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                                               " VALUES (?,?,?,?,?,?,?,?,?)")

                'cmdNewRecCharges2.Connection = conn
                'cmdNewRecCharges2.Transaction = trans

                'With cmdNewRecCharges2.Parameters
                '    .AddWithValue("@TaxYear", taxyear)
                '    .AddWithValue("@TaxRollNumber", taxrollnumber)
                '    .AddWithValue("@TaxChargeCodeID", 99930)
                '    .AddWithValue("@TaxTypeID", 75)
                '    .AddWithValue("@ChargeAmount", faceValue)

                '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                '    .AddWithValue("@CREATE_DATE", Date.Now)

                'End With

                'cmdNewRecCharges2.ExecuteNonQuery()

                'Dim row As DataRow = Me.CashierTransactionsTable.NewRow()

                'row("SESSION_ID") = Me.SessionRecordID
                'row("TAX_YEAR") = taxyear
                'row("TAX_ROLL_NUMBER") = taxrollnumber
                'row("PAYMENT_DATE") = Me.txtPaymentDate.Text
                'row("PAYMENT_TYPE") = Me.ddlPaymentType.SelectedValue
                'row("PAYOR_NAME") = Me.txtPayerName.Text

                'If Me.ddlPaymentType.SelectedValue = 2 Then
                '    row("CHECK_NUMBER") = Me.txtCheckNumber.Text
                'End If

                'row("BARCODE") = Me.txtBarcode.Text
                'row("PAYMENT_AMT") = paymentAmount 'Utilities.GetDecimalOrDBNull(Me.txtAmountPaid.Text)
                'row("TAX_AMT") = paymentAmount
                'row("KITTY_AMT") = 0
                'row("REFUND_AMT") = 0
                'row("EDIT_USER") = TaxPayments.CurrentUserName
                'row("EDIT_DATE") = Date.Now
                'row("CREATE_USER") = TaxPayments.CurrentUserName
                'row("CREATE_DATE") = Date.Now

                'Me.CashierTransactionsTable.Rows.Add(row)
                ''   CommitDataset()

                'Dim cmdNewRecCashierTrans As New OleDbCommand("INSERT INTO genii_user.CASHIER_TRANSACTIONS " & _
                '                               "(RECORD_ID,SESSION_ID,GROUP_KEY, TRANSACTION_STATUS, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_DATE, " & _
                '                               " PAYMENT_TYPE,APPLY_TO, LETTER_TAG, REFUND_TAG, PAYOR_NAME,CHECK_NUMBER, " & _
                '                               " PAYMENT_AMT, TAX_AMT, " & _
                '                               " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                '                               " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                'cmdNewRecCashierTrans.Connection = conn
                'cmdNewRecCashierTrans.Transaction = trans

                'Dim recordIDCashierTrans As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_TRANSACTIONS", conn, trans)
                '' taxrollnumber = row2("TaxRollNumber")
                '' Dim isApportioned As String = 1

                'With cmdNewRecCashierTrans.Parameters
                '    .AddWithValue("@RECORD_ID", recordIDCashierTrans)
                '    .AddWithValue("@SESSION_ID", Me.lblSessionID.Text)
                '    .AddWithValue("@GROUP_KEY", )
                '    .AddWithValue("@TRANSACTION_STATUS", 1)
                '    .AddWithValue("@TAX_YEAR", taxyear)
                '    .AddWithValue("@TAX_ROLL_NUMBER", taxrollnumber)
                '    .AddWithValue("@PAYMENT_DATE", Date.Now)
                '    .AddWithValue("@PAYMENT_TYPE", Me.ddlPaymentType.SelectedValue)
                '    .AddWithValue("@APPLY_TO", 2)
                '    .AddWithValue("@LETTER_TAG", 0)
                '    .AddWithValue("@REFUND_TAG", 1)
                '    .AddWithValue("@PAYOR_NAME", Me.txtRegSSAN.Text)
                '    .AddWithValue("@CHECK_NUMBER", Me.txtCheckNumber.Text)
                '    .AddWithValue("@PAYMENT_AMT", paymentAmount)
                '    .AddWithValue("@TAX_AMT", paymentAmount)
                '    ' .AddWithValue("@IS_APPORTIONED", isApportioned)

                '    .AddWithValue("@EDIT_USER", Me.lblOperatorName.Text)
                '    .AddWithValue("@EDIT_DATE", Date.Now)
                '    .AddWithValue("@CREATE_USER", Me.lblOperatorName.Text)
                '    .AddWithValue("@CREATE_DATE", Date.Now)

                'End With

                'cmdNewRecCashierTrans.ExecuteNonQuery()

                Dim SQL3 As String = String.Format("select record_id from genii_user.cashier_transactions " & _
                                          " where tax_year='{0}' and tax_roll_number= '{1}' ", taxyear, taxrollnumber)
                Dim recordIDCashierTrans As Integer

                Using adt As New OleDbDataAdapter(SQL3, Me.ConnectString)
                    Dim tblTrans As New DataTable()

                    adt.Fill(tblTrans)

                    If tblTrans.Rows.Count > 0 Then
                        If (Not IsDBNull(tblTrans.Rows(0)("RECORD_ID"))) Then
                            recordIDCashierTrans = Convert.ToInt32(tblTrans.Rows(0)("RECORD_ID"))
                        End If
                    End If
                End Using


                Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.TR_PAYMENTS " & _
                                                  "(TRANS_ID, TaxYear, TaxRollNumber, PaymentEffectiveDate, " & _
                                                  " PaymentTypeCode,PaymentMadeByCode,Pertinent1, " & _
                                                  " Pertinent2, PaymentAmount,  " & _
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecPayments.Connection = conn
                cmdNewRecPayments.Transaction = trans


                With cmdNewRecPayments.Parameters
                    .AddWithValue("@TRANS_ID", recordIDCashierTrans)
                    .AddWithValue("@TaxYear", taxyear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)
                    .AddWithValue("@PaymentEffectiveDate", Date.Now)
                    .AddWithValue("@PaymentTypeCode", Me.ddlPaymentType.SelectedValue)
                    .AddWithValue("@PaymentMadeByCode", 3)
                    .AddWithValue("@Pertinent1", Me.txtRegSSAN.Text)
                    .AddWithValue("@Pertinent2", pertinent2 & "-" & Date.Now)
                    .AddWithValue("@PaymentAmount", paymentAmount)

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecPayments.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                '  Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using
    End Sub
    Public Sub btnCompute_click()
        Dim grandTotal As Double
        Dim requiredTotal As Double
        Dim y As Integer = grdPriorYears.Rows.Count
        'grandTotal = txtGrandTotal.Text
        For x = 0 To (y - 1)
            Dim chkPriorYears As CheckBox = grdPriorYears.Rows(x).FindControl("chkPriorYears")
            If (chkPriorYears.Checked) Then
                Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(x).FindControl("txtPriorYearAmount")
                Dim taxYear As String = grdPriorYears.Rows(x).Cells(1).Text
                Dim taxRoll As String = grdPriorYears.Rows(x).Cells(2).Text

                Dim amount As Double = txtPriorYearAmount.Text
                Dim ddlInterest As DropDownList = grdPriorYears.Rows(x).Cells(4).FindControl("ddlInterest")
                Dim stringArray() As String = Split(ddlInterest.SelectedValue, "I")
                Dim interestType As String = Trim(stringArray(0))
                '  stringArray(1) = Trim(stringArray(1).Substring(1))

                '   Dim interestValue As Double = Double.Parse(stringArray(1))
                Dim currentInterest As Double
                Dim priorInterest As Double

                Dim SQL8 As String = String.Format("SELECT * from dbo.vPriorYearsOwed " & _
                                         "         where taxYear = " + taxYear + " And taxRollNumber = " + taxRoll + " ")

                Using adt As New OleDbDataAdapter(SQL8, Me.ConnectString)
                    Dim tblReceiptDetails As New DataTable()

                    adt.Fill(tblReceiptDetails)

                    If tblReceiptDetails.Rows.Count > 0 Then
                        Dim dv As DataView = New DataView(tblReceiptDetails)
                        If (Not IsDBNull(dv(0)("Interest"))) Then
                            currentInterest = Convert.ToDouble(dv(0)("Interest"))
                        End If
                        If (Not IsDBNull(dv(0)("PRIOT_INTEREST"))) Then
                            priorInterest = Convert.ToDouble(dv(0)("PRIOT_INTEREST"))
                        End If

                    End If
                End Using

                Dim balance As Double = CDec(grdPriorYears.Rows(x).Cells(3).Text) + CDec(grdPriorYears.Rows(x).Cells(5).Text) - CDec(grdPriorYears.Rows(x).Cells(6).Text)
                If (interestType = "Aged") Then
                    balance = balance + currentInterest
                ElseIf (interestType = "Prior") Then
                    balance = (balance) + priorInterest
                ElseIf (interestType = "No") Then
                    balance = (balance)
                End If

                grdPriorYears.Rows(x).Cells(7).Text = balance

                'If (amount > balance) Then
                '    amount = balance
                '    txtPriorYearAmount.Text = balance
                'End If

                grandTotal = grandTotal + amount
                requiredTotal = balance + requiredTotal
            End If
        Next
        txtPriorYears.Text = grandTotal
        ' txtGrandTotal.Text = grandTotal
        txtAmountPaid.Text = grandTotal
        hdnTxtRequiredAmount.Text = requiredTotal
        '     hdnAmountRequired.Value = requiredTotal

        '   ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Calculate Difference", "calculateDifference()", True)

    End Sub

    Public Sub btnPriorMonth_Click()
        Dim lastDayPriorMonth = DateAdd("m", 0, DateSerial(Year(Today), Month(Today), 0))
        btnPriorMonth2.Visible = False
        btnCurrentMonth.Visible = True
        'txtPaymentDate.Text = lastDayPriorMonth
        'txtPaymentDate.Enabled = False
        _priorMonth = 1

    End Sub

    Public Sub btnCurrentDate_Click()
        btnPriorMonth2.Visible = True
        btnCurrentMonth.Visible = False
        Dim currDate As Date = Date.Now
        'txtPaymentDate.Text = currDate.ToString("d")
        'txtPaymentDate.Enabled = True
        _priorMonth = 0

    End Sub

    Public Sub SaveTRPaymentSingleTrans(taxyear As String, taxrollnumber As String, amount As Double, totalAmount As Double, TRstatus As Integer, interestType As String, priorInterest As Double, currentInterest As Double)
        Dim currentBalance As String = String.Empty

        Dim SQL8 As String = String.Format("SELECT * from genii_user.TR " & _
                                           "         where taxYear = " + _priorMonthTaxYear + " And taxRollNumber = " + _priorMonthTaxRoll + " ")

        Using adt As New OleDbDataAdapter(SQL8, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("CurrentBalance"))) Then
                    currentBalance = dv(0)("CurrentBalance").ToString()
                Else
                    currentBalance = "0.00"
                End If

            End If
        End Using
        Dim parcelBalance As Double
        Dim updateInterest As Double
        If (interestType = "Aged") Then
            currentBalance = currentBalance
            updateInterest = currentInterest
            parcelBalance = CDec(txtTotalTaxes.Text) - currentBalance - amount
        ElseIf (interestType = "Prior") Then
            currentBalance = currentBalance - currentInterest + priorInterest
            parcelBalance = CDec(txtTotalTaxes.Text) - currentBalance - currentInterest + priorInterest - amount
            updateInterest = priorInterest
        ElseIf (interestType = "No") Then
            currentBalance = currentBalance - currentInterest
            parcelBalance = CDec(txtTotalTaxes.Text) - currentBalance - currentInterest - amount
            updateInterest = 0.0
        Else
            currentBalance = currentBalance
            parcelBalance = CDec(txtTotalTaxes.Text) - currentBalance - amount
            updateInterest = currentInterest
        End If





        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdUpdateTRCharges As New OleDbCommand("UPDATE genii_user.TR_charges " & _
                                                                 " SET ChargeAmount = ?, EDIT_USER=?, EDIT_DATE=? " & _
                                                                 " WHERE TAXYEAR=? AND TAXROLLNUMBER=? AND TAXCHARGECODEID=99901 ")

                cmdUpdateTRCharges.Connection = conn
                cmdUpdateTRCharges.Transaction = trans

                With cmdUpdateTRCharges.Parameters

                    .AddWithValue("@ChargeAmount", updateInterest)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxyear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)


                End With

                cmdUpdateTRCharges.ExecuteNonQuery()

                Dim cmdUpdateTR As New OleDbCommand("UPDATE genii_user.TR " & _
                                                                 " SET STATUS = ?, CurrentBalance = ?,EDIT_USER=?,EDIT_DATE=? " & _
                                                                 " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                cmdUpdateTR.Connection = conn
                cmdUpdateTR.Transaction = trans

                With cmdUpdateTR.Parameters

                    .AddWithValue("@STATUS", TRstatus)
                    .AddWithValue("@CurrentBalance", Convert.ToDecimal(currentBalance) - amount)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxyear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxrollnumber)


                End With

                cmdUpdateTR.ExecuteNonQuery()

                Dim cmdUpdateTAXACCOUNT As New OleDbCommand("UPDATE genii_user.Tax_Account " & _
                                                                 " SET account_balance = ?,EDIT_USER=?,EDIT_DATE=? " & _
                                                                 " WHERE ParcelOrTaxID=?  ")

                cmdUpdateTAXACCOUNT.Connection = conn
                cmdUpdateTAXACCOUNT.Transaction = trans

                With cmdUpdateTAXACCOUNT.Parameters

                    .AddWithValue("@account_balance", parcelBalance)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@ParcelOrTaxID", TaxRollMaster.TaxIDNumber) 'currentTaxYear)

                End With

                cmdUpdateTAXACCOUNT.ExecuteNonQuery()

                trans.Commit()

                txtTotalTaxes.Text = parcelBalance

            Catch ex As Exception
                trans.Rollback()
                ' Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using
    End Sub

    Public Sub UpdateCharges(taxYear As String, taxRollNumber As String)
        Dim amount As Double = 0.0
        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdUpdateCharges As New OleDbCommand("UPDATE genii_user.TR_Charges " & _
                                                                 " SET chargeAmount = ?, EDIT_USER=?,EDIT_DATE=? " & _
                                                                 " WHERE TAXYEAR=? AND TAXROLLNUMBER=?  and TAXCHARGECODEID=?")

                cmdUpdateCharges.Connection = conn
                cmdUpdateCharges.Transaction = trans

                With cmdUpdateCharges.Parameters

                    .AddWithValue("@chargeAmount", amount)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxYear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxRollNumber)
                    .AddWithValue("@TAXCHARGECODEID", 99901)


                End With

                cmdUpdateCharges.ExecuteNonQuery()


                Dim cmdUpdateApportion As New OleDbCommand("UPDATE genii_user.cashier_apportion " & _
                                                                 " SET DollarAmount = ?, EDIT_USER=?,EDIT_DATE=? " & _
                                                                 " WHERE TAXYEAR=? AND TAXROLLNUMBER=?  and TAXCHARGECODEID=?")

                cmdUpdateApportion.Connection = conn
                cmdUpdateApportion.Transaction = trans

                With cmdUpdateApportion.Parameters

                    .AddWithValue("@DollarAmount", amount)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxYear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxRollNumber)
                    .AddWithValue("@TAXCHARGECODEID", 99901)


                End With

                cmdUpdateApportion.ExecuteNonQuery()
                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using
    End Sub


    ''' <summary>
    ''' Saves cashier transaction in database.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Protected Sub btnSavePayment_Click(sender As Object, e As System.EventArgs) Handles btnSavePayment.Click
        Dim paymentAmount As Decimal = Utilities.GetDecimalOrZero(Me.txtAmountPaid.Text)
        Dim taxAmount As Decimal = Utilities.GetDecimalOrZero(Me.txtAmountPaid.Text)
        '   Dim amountDue As Decimal = CDec(Me.txtTotalPayments.Text) 'CDec(Me.txtCalculatedBalance.Text) + CDec(Me.txtTotalInterest.Text) + CDec(Me.txtTotalFees.Text) - CDec(Me.txtTotalPayments.Text)
        '   amountDue = Utilities.GetDecimalOrZero(amountDue)
        '  Dim txtDiff As Decimal = Utilities.GetDecimalOrZero(Me.txtDifference.Text)
        Dim diff As Decimal = taxAmount - (txtAmountPaid.Text)


        'check ddlpaymenttype...

        If (ddlPaymentType.SelectedValue = 1 Or ddlPaymentType.SelectedValue = 3) Then
            If ((txtCheckNumber.Text = String.Empty)) Then
                Dim Caller As Control = Me
                ScriptManager.RegisterStartupScript(Caller, [GetType](), "Check Number", "showMessage('CheckNumber must not be null', 'Check Number');", True)
                Exit Sub
            End If
        End If


        Try
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

            'If diff <> txtDiff Then
            '    ClientScript.RegisterStartupScript(Me.GetType(), "DiffChanged", "showMessage('Amount Paid, Amount Due or Difference is not correct. Please check values and try again.', 'Retry');", True)
            '    Exit Sub
            'End If

            'If (txtPriorYears.Text = String.Empty Or txtPriorYears.Text = "0.00") Then

            '    Select Case diff
            '        Case 0
            '            If (amountDue <> 0) Then
            '                SaveAcceptedPayment(amountDue, 0, 0, Me.txtTaxRollNumber.Text, ddlTaxYear.SelectedValue, groupKey)
            '                SaveTRPaymentSingleTrans(ddlTaxYear.SelectedValue, Me.txtTaxRollNumber.Text)
            '            End If

            '        Case Is > 0
            '            Select Case Me.rdoAmountOver.SelectedValue
            '                Case "refund"
            '                    SaveAcceptedPayment(taxAmount, 0, paymentAmount - taxAmount, Me.txtTaxRollNumber.Text, ddlTaxYear.SelectedValue, groupKey)
            '                Case "kitty"
            '                    SaveAcceptedPayment(taxAmount, paymentAmount - taxAmount, 0, Me.txtTaxRollNumber.Text, ddlTaxYear.SelectedValue, groupKey)
            '                Case Else
            '                    Throw New NotImplementedException(Me.rdoAmountOver.SelectedItem.Text)
            '            End Select
            '        Case Is < 0
            '            Select Case Me.rdoAmountUnder.SelectedValue
            '                Case "partial"
            '                    SaveAcceptedPayment(paymentAmount, 0, 0, Me.txtTaxRollNumber.Text, ddlTaxYear.SelectedValue, groupKey)
            '                Case "writeoff"
            '                    SaveAcceptedPayment(taxAmount, paymentAmount - taxAmount, 0, Me.txtTaxRollNumber.Text, ddlTaxYear.SelectedValue, groupKey)
            '                Case Else
            '                    Throw New NotImplementedException(Me.rdoAmountUnder.SelectedItem.Text)
            '            End Select
            '    End Select
            '    If (amountDue <> 0) Then
            '        CalculateApportionmentsOnSavePayment(Me.txtTaxRollNumber.Text, ddlTaxYear.SelectedValue, amountDue)
            '        SaveApportionments(Me.txtTaxRollNumber.Text, ddlTaxYear.SelectedValue)
            '    End If

            'ElseIf (txtPriorYears.Text <> String.Empty And txtPriorYears.Text <> "0.00") Then

            ' Dim currentTotal As Double = CDec(txtTotalTaxes.Text) '+ CDec(txtTotalInterest.Text) + CDec(txtTotalFees.Text)
            'Payment for current Year

            '  SaveAcceptedPayment(currentTotal, 0, 0, txtTaxRollNumber.Text, ddlTaxYear.SelectedValue, groupKey)
            '  SaveTRPaymentSingleTrans(ddlTaxYear.SelectedValue, Me.txtTaxRollNumber.Text)
            '   CalculateApportionmentsOnSavePayment(txtTaxRollNumber.Text, ddlTaxYear.SelectedValue, currentTotal)
            '   SaveApportionments(txtTaxRollNumber.Text, ddlTaxYear.SelectedValue)

            If ((txtPriorYears.Text <> String.Empty And txtPriorYears.Text <> "0.00")) Then
                Dim chk As CheckBox = grdPriorYears.HeaderRow.FindControl("chkPriorYearsSelectAll")

                Dim total As Double = 0
                Dim totalPaid As Double = 0

                Dim v As Integer = grdPriorYears.Rows.Count
                Dim x As Integer = 0
                Dim y As Integer = grdPriorYears.Rows.Count
                Dim z As Integer = 0
                Dim ctr As Integer = 0
                Dim txroll As String
                Dim txyear As String

                If (chk.Checked) Then
                    For z = 0 To (v - 1)
                        Dim chkPriorYears As CheckBox = grdPriorYears.Rows(z).FindControl("chkPriorYears")

                        chkPriorYears.Checked = True

                    Next
                End If

                For x = 0 To (y - 1)
                    Dim chkPriorYears As CheckBox = grdPriorYears.Rows(x).FindControl("chkPriorYears")
                    If (chkPriorYears.Checked) Then
                        txroll = grdPriorYears.Rows(x).Cells(2).Text
                        txyear = grdPriorYears.Rows(x).Cells(1).Text
                        Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(x).FindControl("txtPriorYearAmount")
                        Dim ddlInterest As DropDownList = grdPriorYears.Rows(x).Cells(4).FindControl("ddlInterest")
                        Dim stringArray() As String = Split(ddlInterest.SelectedValue, "I")
                        Dim interestType As String = Trim(stringArray(0))
                        ' Dim interestValue As Double = Trim(stringArray(2))
                        Dim currentInterest As Double
                        Dim priorInterest As Double

                        _priorMonthTaxRoll = txroll
                        _priorMonthTaxYear = txyear
                        _priorMonthTransID = groupKey

                        Dim currentBalance As Double
                        total = txtPriorYearAmount.Text '
                        Dim totalPaymentAmount As Double = grdPriorYears.Rows(x).Cells(7).Text
                        Dim TotalPriorYearPayments As Double = txtPriorYears.Text
                        Dim totalBalance As Double

                        Dim SQL8 As String = String.Format("SELECT * from dbo.vPriorYearsOwed " & _
                                          "         where taxYear = " + _priorMonthTaxYear + " And taxRollNumber = " + _priorMonthTaxRoll + " ")

                        Using adt As New OleDbDataAdapter(SQL8, Me.ConnectString)
                            Dim tblReceiptDetails As New DataTable()

                            adt.Fill(tblReceiptDetails)

                            If tblReceiptDetails.Rows.Count > 0 Then
                                Dim dv As DataView = New DataView(tblReceiptDetails)
                                If (Not IsDBNull(dv(0)("Interest"))) Then
                                    currentInterest = Convert.ToDouble(dv(0)("Interest"))
                                End If

                                If (Not IsDBNull(dv(0)("PRIOT_INTEREST"))) Then
                                    priorInterest = Convert.ToDouble(dv(0)("PRIOT_INTEREST"))
                                End If

                                If (Not IsDBNull(dv(0)("CurrentBalance"))) Then
                                    totalBalance = Convert.ToDouble(dv(0)("CurrentBalance"))
                                End If
                            End If

                        End Using

                        If (interestType = "Aged") Then
                            currentBalance = CDec(txtTotalTaxes.Text) - total
                        ElseIf (interestType = "Prior") Then
                            currentBalance = ((CDec(txtTotalTaxes.Text) - currentInterest) + CDec(priorInterest)) - total
                        ElseIf (interestType = "No") Then
                            currentBalance = ((CDec(txtTotalTaxes.Text) - currentInterest)) - total
                        End If

                        'total = CDec(txtAmountPaid.Text) - (CDec(txtTotalTaxes.Text) + CDec(txtTotalInterest.Text) + CDec(txtTotalFees.Text) - CDec(txtTotalPayments.Text))
                        '  totalPaid = total - CDec(grdriorYears.Rows(x).Cells(7).Text)

                        Dim difference As Double
                        difference = total - totalPaymentAmount



                        Select Case difference
                            Case 0
                                If (total <> 0) Then
                                    SaveAcceptedPayment(total, 0, 0, txroll, txyear, groupKey, 1)
                                    ' System.Threading.Thread.Sleep(1000)
                                    SaveTRPaymentSingleTrans(txyear, txroll, total, currentBalance, 2, interestType, priorInterest, currentInterest)
                                End If

                            Case Is > 0
                                Select Case Me.rdoAmountOver.SelectedValue
                                    Case "refund"
                                        SaveAcceptedPayment(total, 0, total - totalPaymentAmount, txroll, txyear, groupKey, 1)
                                        SaveTRPaymentSingleTrans(txyear, txroll, total, CDec(txtTotalTaxes.Text) - total, 2, "", 0, 0)
                                    Case "kitty"
                                        SaveAcceptedPayment(total, total - totalPaymentAmount, 0, txroll, txyear, groupKey, 1)
                                        SaveTRPaymentSingleTrans(txyear, txroll, total, CDec(txtTotalTaxes.Text) - total, 2, "", 0, 0)
                                    Case Else
                                        Throw New NotImplementedException(Me.rdoAmountOver.SelectedItem.Text)
                                End Select
                            Case Is < 0
                                Select Case Me.rdoAmountUnder.SelectedValue
                                    Case "partial"
                                        SaveAcceptedPayment(total, 0, 0, txroll, txyear, groupKey, 1)
                                        SaveTRPaymentSingleTrans(txyear, txroll, total - totalPaymentAmount, CDec(txtTotalTaxes.Text) - total, 1, "", 0, 0)
                                        vSumApportion(txyear, txroll, total)
                                        SaveApportionments(txroll, txyear)
                                    Case "writeoff"
                                        SaveAcceptedPaymentWriteOff(total, totalPaymentAmount - total, 0, txroll, txyear, groupKey, 1)
                                        SaveTRPaymentSingleTrans(txyear, txroll, total - totalPaymentAmount, CDec(txtTotalTaxes.Text) - total, 1, "", 0, 0)
                                    Case Else
                                        Throw New NotImplementedException(Me.rdoAmountUnder.SelectedItem.Text)
                                End Select
                        End Select

                        If (total > 0) Then 'And difference >= 0
                            CalculateApportionmentsOnSavePayment(txroll, txyear, total)
                            '   System.Threading.Thread.Sleep(1000)
                            SaveApportionments(txroll, txyear)
                            Dim isOnline As Boolean = False
                            Dim printerName As String = String.Empty
                            printerName = "EPSON TM-T88IV Receipt AAAAA"
                            Dim print_document As Printing.PrintDocument
                            print_document = PreparePrintDocument("forRegularPayment")
                            print_document.PrinterSettings.PrinterName = printerName
                            isOnline = print_document.PrinterSettings.IsValid
                            If (isOnline = True) Then
                                print_document.Print()

                            End If
                            '' ''  PrintRegularPayment()
                            '' '' SaveTRPaymentSingleTrans(ddlTaxYear.SelectedValue, Me.txtTaxRollNumber.Text)
                            '' ''update charges here set 99901 to amount=0
                            '' ''Dim chkFGI As CheckBox = grdPriorYears.Rows(x).FindControl("chkFGI")
                            '' ''If (chkFGI.Checked) Then
                            '' ''    UpdateCharges(txyear, txroll)
                            '' ''End If
                        End If

                        'Dim taxHistorySQL As String = String.Format("SELECT TaxYear AS 'Tax Year',  " & _
                        '                                 " TaxRollNumber AS 'Tax Roll', Status,  " & _
                        '                               " isnull(ChargeAmount,0) AS 'Taxes',  " & _
                        '                                 " isnull(NumPayments,0) AS 'Payments',  " & _
                        '                            " isnull(TotalPaymentAmount,0) AS 'Remitted',  " & _
                        '                            " ChargeAmount - ISNULL(TotalPaymentAmount,0) AS 'Balance' " & _
                        '                            " FROM " & _
                        '                            " vTaxHistory WHERE TaxIDNumber = '{0}' " & _
                        '                            " ORDER BY 'Tax Year' DESC", Me.TaxRollMaster.TaxIDNumber, Me.TaxRollMaster.TaxYear)


                        Dim taxHistorySQL As String = String.Format("select * from dbo.vTaxHistory WHERE TaxIDNumber = '{0}' " & _
                                                   " ORDER BY TaxYear DESC", Me.TaxRollMaster.TaxIDNumber, Me.TaxRollMaster.TaxYear)

                        BindGrid(Me.dtaSummary, taxHistorySQL)

                        If (dtaSummary.Rows.Count = 1) Then
                            dtaSummary.SelectedIndex = 0
                        End If

                        'SaveAcceptedPayment(total, 0, 0, txroll, txyear, groupKey)
                        'CalculateApportionmentsOnSavePayment(txroll, txyear, total)
                        'SaveApportionments(txroll, txyear)
                        ctr = ctr + 1

                    End If

                Next
            End If


            If (txtAddCP.Text <> String.Empty And txtAddCP.Text <> "0.00") Then
                Dim chkInvestor As CheckBox = grdCPsInvestor.HeaderRow.FindControl("chkCPSelectAll")

                Dim v1 As Integer = grdCPsInvestor.Rows.Count
                Dim x1 As Integer = 0
                Dim y1 As Integer = grdCPsInvestor.Rows.Count
                Dim z1 As Integer = 0
                Dim ctr1 As Integer = 0
                Dim txroll1 As String
                Dim txyear1 As String
                Dim total1 As String
                Dim faceValue As Double
                Dim interest As Double
                Dim redemptionFee As Double

                If (chkInvestor.Checked) Then
                    For z1 = 0 To (v1 - 1)
                        Dim chkInvestorCP As CheckBox = grdCPsInvestor.Rows(z1).FindControl("chkCP")

                        chkInvestorCP.Checked = True

                    Next
                End If

                For x1 = 0 To (y1 - 1)
                    Dim chkInvestorCP As CheckBox = grdCPsInvestor.Rows(x1).FindControl("chkCP")
                    If (chkInvestorCP.Checked) Then
                        txroll1 = grdCPsInvestor.Rows(x1).Cells(2).Text
                        txyear1 = grdCPsInvestor.Rows(x1).Cells(1).Text
                        total1 = grdCPsInvestor.Rows(x1).Cells(10).Text
                        faceValue = grdCPsInvestor.Rows(x1).Cells(7).Text
                        interest = grdCPsInvestor.Rows(x1).Cells(8).Text
                        redemptionFee = grdCPsInvestor.Rows(x1).Cells(9).Text

                        _priorMonthTaxRoll = txroll1
                        _priorMonthTaxYear = txyear1


                        'total = CDec(txtAmountPaid.Text) - (CDec(txtTotalTaxes.Text) + CDec(txtTotalInterest.Text) + CDec(txtTotalFees.Text) - CDec(txtTotalPayments.Text))
                        '  totalPaid = total - CDec(grdPriorYears.Rows(x).Cells(7).Text)
                        SaveAcceptedPaymentInvestorState(total1, 0, 0, txroll1, txyear1, groupKey, 2, 1)
                        SaveTransactionInvestor(txyear1, txroll1, total1, "Investor CP Redeem", groupKey, interest)
                        SaveInvestorApportionRecords(txyear1, txroll1, total1, interest, faceValue, redemptionFee)

                        'Dim print_document As Printing.PrintDocument
                        'print_document = PreparePrintDocument("forRedeemFromInvestor")
                        'print_document.Print()
                        Dim isOnline As Boolean = False
                        Dim printerName As String = String.Empty
                        printerName = "EPSON TM-T88IV Receipt AAAAA"
                        Dim print_document As Printing.PrintDocument
                        print_document = PreparePrintDocument("forRedeemFromInvestor")
                        print_document.PrinterSettings.PrinterName = printerName
                        isOnline = print_document.PrinterSettings.IsValid
                        If (isOnline = True) Then
                            print_document.Print()

                        End If

                        'funds cannot be sent to the apportion function because the investor gets the money  max 07112013
                        '  CalculateApportionmentsOnSavePayment(txroll1, txyear1, total1)
                        ctr1 = ctr1 + 1

                    End If

                Next
            End If

            If (txtAddCPState.Text <> String.Empty And txtAddCPState.Text <> "0.00") Then
                Dim chkState As CheckBox = grdCPsState.HeaderRow.FindControl("chkCPStateSelectAll")
                Dim v2 As Integer = grdCPsState.Rows.Count
                Dim x2 As Integer = 0
                Dim y2 As Integer = grdCPsState.Rows.Count
                Dim z2 As Integer = 0
                Dim ctr2 As Integer = 0
                Dim txroll2 As String
                Dim txyear2 As String
                Dim total2 As String
                Dim faceValue As Double
                Dim interest As Double
                Dim redemptionFee As Double


                If (chkState.Checked) Then
                    For z2 = 0 To (v2 - 1)
                        Dim chkStateCP As CheckBox = grdCPsState.Rows(z2).FindControl("chkCPState")

                        chkStateCP.Checked = True

                    Next
                End If

                For x2 = 0 To (y2 - 1)
                    Dim chkStateCP As CheckBox = grdCPsState.Rows(x2).FindControl("chkCPState")
                    If (chkStateCP.Checked) Then
                        txroll2 = grdCPsState.Rows(x2).Cells(2).Text
                        txyear2 = grdCPsState.Rows(x2).Cells(1).Text
                        total2 = grdCPsState.Rows(x2).Cells(9).Text
                        faceValue = grdCPsState.Rows(x2).Cells(4).Text
                        interest = grdCPsState.Rows(x2).Cells(5).Text
                        redemptionFee = grdCPsState.Rows(x2).Cells(8).Text

                        _priorMonthTaxRoll = txroll2
                        _priorMonthTaxYear = txyear2

                        'total = CDec(txtAmountPaid.Text) - (CDec(txtTotalTaxes.Text) + CDec(txtTotalInterest.Text) + CDec(txtTotalFees.Text) - CDec(txtTotalPayments.Text))
                        '  totalPaid = total - CDec(grdPriorYears.Rows(x).Cells(7).Text)
                        SaveAcceptedPaymentState(total2, 0, 0, txroll2, txyear2, groupKey, 3)                        
                        '    SaveAcceptedPaymentInvestorState(total2, 0, 0, txroll2, txyear2, groupKey, 3, 0)
                        SaveTransactionState(txyear2, txroll2, total2, "State CP Redeemed", groupKey, interest)
                        SaveStateApportionRecords(txyear2, txroll2, total2, interest, faceValue, redemptionFee)
                        System.Threading.Thread.Sleep(1000)
                        CalculateApportionmentsOnSavePayment(txroll2, txyear2, total2)

                        'Dim print_document As Printing.PrintDocument
                        'print_document = PreparePrintDocument("forRedeemFromState")
                        'print_document.Print()
                        Dim isOnline As Boolean = False
                        Dim printerName As String = String.Empty
                        printerName = "EPSON TM-T88IV Receipt AAAAA"
                        Dim print_document As Printing.PrintDocument
                        print_document = PreparePrintDocument("forRedeemFromState")
                        print_document.PrinterSettings.PrinterName = printerName
                        isOnline = print_document.PrinterSettings.IsValid
                        If (isOnline = True) Then
                            print_document.Print()

                        End If

                        ctr2 = ctr2 + 1

                    End If

                Next

            End If

            CheckAccountStatus()
            Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showMessage('Transaction complete.', 'Transaction complete');", True)

        Catch ex As Exception
            '  Response.Redirect("ErrorPage.aspx")
            Throw ex
        End Try
        '  If Not (rdoAmountOver.SelectedValue = "decline" Or rdoAmountUnder.SelectedValue = "decline") Then
        'SAVE APPORTIIONMENT HERE... MTA 04052013
        'calculate apportionment....

        'MUST CALCULATE APPORTIONMENTS HERE PER PRIOR YEAR AND PER TAXROLLNUMBER MTA 06092013





        ' CalculateApportionmentsOnSavePayment(Me.ddlTaxYear.SelectedValue, Me.txtTaxRollNumber.Text, paymentAmount)

        'save apportionment
        'call button click on button save all
        'SaveApportionments()
        '   End If


        Me.txtBarcode.Text = String.Empty
        Me.btnSavePayment.Enabled = False
        Me.btnRejectPayment.Enabled = False
        '  Me.btnPrintReceipt.Visible = True
        ' ShowLetterQueuer(-diff)

        BindPendingPaymentsGrids()        


        'If (_TRPaymentRule = 1) Then
        '    Me.divPaymentRemark.Visible = True
        '    lblPaymentRemark.Text = "Both halves paid."
        'ElseIf (_TRPaymentRule = 2) Then
        '    Me.divPaymentRemark.Visible = True
        '    lblPaymentRemark.Text = "Total less than $100, tax not split."
        'ElseIf (_TRPaymentRule = 3) Then
        '    Me.divPaymentRemark.Visible = True
        '    lblPaymentRemark.Text = "First half tax payment made."
        'ElseIf (_TRPaymentRule = 4) Then
        '    Me.divPaymentRemark.Visible = True
        '    lblPaymentRemark.Text = "Total tax paid by the End of December."
        'End If



    End Sub

    '  Protected Sub btnCreateReceipt_Click(sender As Object, e As System.EventArgs) Handles btnCreateReceipt.Click
    '  Response.Redirect("counter_receipt.htm")
    'HttpContext.Current.Response.Redirect("counter_receipt.htm", False)


    '     Response.Write("<script>")
    '  Response.Write("window.open('counter_receipt.htm')")
    ' Response.Write("window.print('counter_receipt.htm')")
    '   Response.Write("window.open('PrintReceipt.aspx?TaxIDNumber=1234'")
    '      Response.Write("window.open('PrintReceipt.aspx?TaxIDNumber=1234'),'_blank'")
    ' window.open('Reports/InvestorHoldingsSummary.aspx?ReportID=' + reportID + '&InvestorID=' + investorID, "_blank");
    '      Response.Write("</script>")
    '

    '   End Sub

    Protected Sub btnPrintReceipt_click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim print_document As Printing.PrintDocument
        print_document = PreparePrintDocument("Testing")
        print_document.Print()


    End Sub
    Public Function PreparePrintDocument(forPayment As String) As Printing.PrintDocument
        ' Make the PrintDocument object.
        Dim print_document As New Printing.PrintDocument
        'forRegularPayment
        'forRedeemFromState
        If (forPayment.Equals("Testing")) Then
            ' Install the PrintPage event handler.
            AddHandler print_document.PrintPage, AddressOf DrawStringPointFTesting 'Print_PrintPage
        ElseIf (forPayment.Equals("forRegularPayment")) Then
            AddHandler print_document.PrintPage, AddressOf DrawStringPointF_PRINTHEADER
            AddHandler print_document.PrintPage, AddressOf DrawStringPointF 'Print_PrintPage

        ElseIf (forPayment.Equals("forRedeemFromInvestor")) Then
            ' Install the PrintPage event handler.
            AddHandler print_document.PrintPage, AddressOf DrawStringPointF_PRINTHEADER
            AddHandler print_document.PrintPage, AddressOf DrawStringPointFRedeemFromInvestor 'Print_PrintPage
        ElseIf (forPayment.Equals("forRedeemFromState")) Then
            ' Install the PrintPage event handler.
            AddHandler print_document.PrintPage, AddressOf DrawStringPointF_PRINTHEADER
            AddHandler print_document.PrintPage, AddressOf DrawStringPointFRedeemFromState 'Print_PrintPage
        End If

      

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
        Dim TransID As Integer

        Dim PaymentDescription As String = String.Empty
        Dim WhoPaid As String = String.Empty


        Dim SQLTrans As String = String.Format("SELECT record_id " & _
                                        " FROM genii_user.cashier_transactions " & _
                                        " where tax_Year = '{0}' And tax_Roll_Number = '{1}' ", _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQLTrans, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("record_id"))) Then
                    TransID = dv(0)("record_id").ToString()
                Else
                    TransID = 0
                End If
            End If
        End Using

        Dim SQL5 As String = String.Format("SELECT TaxYear, TaxRollNumber, SUM(CASE WHEN taxtypeid <= 40 THEN chargeamount ELSE 0 END) AS Taxes, " & _
                                          " SUM(CASE WHEN taxtypeid = 80 THEN chargeamount ELSE 0 END) AS Interest,  " & _
                                         " SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN chargeamount ELSE 0 END) AS Fees  " & _
                                         " FROM genii_user.TR_CHARGES " & _
                                         " where taxYear = '{0}' And taxRollNumber = '{1}' GROUP BY TaxYear, TaxRollNumber", _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQL5, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("Taxes"))) Then
                    taxes = dv(0)("Taxes").ToString()
                Else
                    taxes = "0.00"
                End If

                If (Not IsDBNull(dv(0)("Interest"))) Then
                    totalInterest = dv(0)("Interest").ToString()
                Else
                    totalInterest = "0.00"
                End If

                If (Not IsDBNull(dv(0)("Fees"))) Then
                    totalFees = dv(0)("Fees").ToString()
                Else
                    totalFees = "0.00"
                End If

            End If
        End Using

        Dim SQL6 As String = String.Format("Select a.payment_amt,a.kitty_amt, a.refund_amt, b.paymentDescription,c.descriptionOfPayer " & _
                                          " from genii_user.cashier_transactions a, genii_user.st_payment_instrument b, genii_user.st_who_paid c" & _
                                         " where a.payment_type= b.paymentTypeCode and a.apply_to=c.paymentMadeByCode and a.tax_year ='{0}' and a.Tax_Roll_Number='{1}' and a.transaction_status = 1",
                                         _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQL6, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("Payment_amt"))) Then
                    paidToday = dv(0)("Payment_amt").ToString()
                    'Else
                    '    lblPhysicalAddress.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("paymentDescription"))) Then
                    PaymentDescription = dv(0)("paymentDescription").ToString()
                End If

                If (Not IsDBNull(dv(0)("descriptionOfPayer"))) Then
                    WhoPaid = dv(0)("descriptionOfPayer").ToString()
                End If
            End If
        End Using


        Dim SQL7 As String = String.Format("SELECT TaxYear, TaxRollNumber, SUM(CASE WHEN taxtypeid <= 40 THEN dollarAmount ELSE 0 END) AS Taxes,  " & _
                                            " SUM(CASE WHEN taxtypeid = 80 THEN dollarAmount ELSE 0 END) AS Interest,   " & _
                                            " SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN dollarAmount ELSE 0 END) AS Fees   " & _
                                            "         FROM genii_user.cashier_apportion " & _
                                            "         where taxYear = '{0}' And taxRollNumber = '{1}' and Trans_ID= {2} " & _
                                            " GROUP BY TaxYear, TaxRollNumber", _priorMonthTaxYear, _priorMonthTaxRoll, TransID)

        Using adt As New OleDbDataAdapter(SQL7, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("Taxes"))) Then
                    taxesPaidToday = dv(0)("Taxes").ToString()
                Else
                    taxesPaidToday = "0.00"
                End If

                If (Not IsDBNull(dv(0)("Interest"))) Then
                    interestPaidToday = dv(0)("Interest").ToString()
                Else
                    interestPaidToday = "0.00"
                End If

                If (Not IsDBNull(dv(0)("Fees"))) Then
                    feesPaidToday = dv(0)("Fees").ToString()
                Else
                    feesPaidToday = "0.00"
                End If

            End If
        End Using

        Dim SQL8 As String = String.Format("SELECT * from genii_user.TR " & _
                                           "         where taxYear = '{0}' And taxRollNumber = '{1}' ", _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQL8, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("CurrentBalance"))) Then
                    remainingBalance = dv(0)("CurrentBalance").ToString()
                Else
                    remainingBalance = "0.00"
                End If

            End If
        End Using


        priorBalanceDue = remainingBalance + paidToday
        With Me.TaxRollMaster
            previousPayment = .GetTotalPayments2(_priorMonthTaxYear, _priorMonthTaxRoll)
            If (IsDBNull(previousPayment)) Then
                previousPayment = Math.Round(0.0, 2)
            Else
                previousPayment = previousPayment - paidToday
            End If
        End With



        Dim printFont10B = New Font("Arial", 9, FontStyle.Bold)
        Dim printFont9R = New Font("Arial", 9, FontStyle.Regular)
        Dim rect1 As New Rectangle(10, 10, 270, 250)

        Dim rect2 As New Rectangle(10, 50, 270, 250)

        Dim rect3 As New Rectangle(10, 85, 270, 250)

        Dim rect4 As New Rectangle(10, 110, 270, 400)

        Dim rect5 As New Rectangle(10, 190, 270, 400)

        Dim rect6 As New Rectangle(10, 290, 270, 400)


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

        '' Dim defaultHeader As String() = {"---------------------------------------", sigBlockTitle, sigBlockName, sigBlockAddress, sigBlockCityStateZip, Date.Now, "Operator - "}
        'Dim defaultHeader As String() = {"-----------------------------------------------------", "Operator - " & System.Web.HttpContext.Current.User.Identity.Name, Date.Now, sigBlockCityStateZip, sigBlockAddress, sigBlockName, sigBlockTitle, "-----------------------------------------------------"}

        'For i = 0 To defaultHeader.Count - 1
        '    y = y & " " & vbNewLine & vbNewLine
        '    e.Graphics.DrawString(defaultHeader(i) & y, printFont10B, Brushes.Black, rect1, stringFormat)
        'Next

        Dim paymentDetails1 As String() = {"Payment applied to the " & _currentTaxYear & " Tax Year", "Thank you for your Payment of: $" & Math.Round(paidToday, 2)}

        For i = 0 To paymentDetails1.Count - 1
            z = z & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails1(i) & z, printFont9R, Brushes.Black, rect2, stringFormat)
        Next


        Dim paymentDetails2 As String() = {txtPayerName.Text, WhoPaid & " by " & PaymentDescription}

        For i = 0 To paymentDetails2.Count - 1
            a = a & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(vbNewLine & vbNewLine & paymentDetails2(i) & a, printFont9R, Brushes.Black, rect3, stringFormat)
        Next

        Dim paymentDetails3 As String() = {"Parcel / Tax ID: " & TaxRollMaster.TaxIDNumber, "Tax Roll: " & _priorMonthTaxRoll, "Tax Year: " & _priorMonthTaxYear}
        b = String.Empty
        For j = 0 To paymentDetails3.Count - 1
            b = b & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails3(j) & b, printFont9R, Brushes.Black, rect4, stringFormat)
        Next

        Dim paymentReceipt1 As String() = {"Prior Balance Due: " & vbTab & " $" & priorBalanceDue, "Previous Payment: " & vbTab & " $" & previousPayment, "Total Interest: " & vbTab & vbTab & " $" & totalInterest, "Total Fees: " & vbTab & vbTab & " $" & totalFees, "Taxes:" & vbTab & vbTab & " $" & taxes}
        b = String.Empty
        For j = 0 To paymentReceipt1.Count - 1
            b = b & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentReceipt1(j) & b, printFont9R, Brushes.Black, rect5, stringFormatNear)
        Next


        Dim paymentReceipt2 As String() = {"- - -", "Remaining Balance :" & vbTab & " $" & remainingBalance, "Interest Paid Today:" & vbTab & " $" & interestPaidToday, "Fees Paid Today :" & vbTab & " $" & feesPaidToday, "Taxes Paid Today: " & vbTab & " $" & taxesPaidToday, "Paid Today : " & vbTab & vbTab & " $" & paidToday}
        b = String.Empty
        For j = 0 To paymentReceipt2.Count - 1
            b = b & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentReceipt2(j) & b, printFont9R, Brushes.Black, rect6, stringFormatNear)
        Next


        ''-------------------------------------------------------------------------------------------------------------

    End Sub


    Public Sub DrawStringPointFRedeemFromInvestor(ByVal sender As Object, ByVal e As PrintPageEventArgs)

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

        Dim PaymentDescription As String = String.Empty
        Dim WhoPaid As String = String.Empty

        Dim certNumber As String = String.Empty
        Dim investor As String = String.Empty
        Dim dateOfSale As String = String.Empty
        Dim monthsAtRate As String = String.Empty
        Dim CPInvestValue As Decimal = 0.0
        Dim CPInvestInterest As Decimal = 0.0
        Dim CPInvestFee As Decimal = 0.0
        Dim TransID As Integer

        Dim SQL6 As String = String.Format("Select a.record_id, a.payment_amt,a.kitty_amt, a.refund_amt, b.paymentDescription,c.descriptionOfPayer " & _
                                         " from genii_user.cashier_transactions a, genii_user.st_payment_instrument b, genii_user.st_who_paid c" & _
                                        " where a.payment_type= b.paymentTypeCode and a.apply_to=c.paymentMadeByCode and a.tax_year = '{0}' and a.Tax_Roll_Number='{1}' and a.transaction_status = 1",
                                        _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQL6, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)

                If (Not IsDBNull(dv(0)("record_id"))) Then
                    TransID = dv(0)("record_id").ToString()                   
                End If

                If (Not IsDBNull(dv(0)("Payment_amt"))) Then
                    paidToday = dv(0)("Payment_amt").ToString()
                    'Else
                    '    lblPhysicalAddress.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("paymentDescription"))) Then
                    PaymentDescription = dv(0)("paymentDescription").ToString()
                End If

                If (Not IsDBNull(dv(0)("descriptionOfPayer"))) Then
                    WhoPaid = dv(0)("descriptionOfPayer").ToString()
                End If
            End If
        End Using


        Dim SQL9 As String = String.Format("SELECT     TOP (100) PERCENT genii_user.TR_CP.CertificateNumber AS Certificate, genii_user.TR_CP.TaxYear, genii_user.TR_CP.TaxRollNumber AS [Roll Number], " & _
                                            "  genii_user.ST_INVESTOR.LastName AS Investor, CONVERT(varchar(10), genii_user.TR_CP.DateCPPurchased, 101) AS [DateofPurchase], CONVERT(varchar(10),  " & _
                                            "   DATEDIFF(mm, genii_user.TR_CP.DateCPPurchased, GETDATE())) + ' @ ' + CONVERT(varchar(10), genii_user.TR_CP.MonthlyRateOfInterest * 100)  " & _
                                            "  + '%' AS [Months@Rate], genii_user.TR_CP.PurchaseValue AS Value,  " & _
                                            "   ROUND(genii_user.TR_CP.MonthlyRateOfInterest / 12 * genii_user.TR_CP.PurchaseValue * DateDiff(mm, genii_user.TR_CP.DateCPPurchased, GETDATE()), 2) " & _
                                            "                 AS Interest, CASE WHEN INITIAL_CP_YEAR = TaxYear THEN " & _
                                            "                     (SELECT     PARAMETER " & _
                                            "   FROM genii_user.ST_PARAMETER " & _
                                            "                       WHERE      genii_user.ST_PARAMETER.RECORD_ID = 99930) ELSE 0 END AS RedeemFee, genii_user.TR_CP.APN, genii_user.TR_CP.CP_STATUS, genii_user.TR_CP.InvestorID, genii_user.TR_CP.DateOfSale " & _
                                            " FROM         genii_user.TR_CP INNER JOIN " & _
                                            "                 genii_user.ST_INVESTOR ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " & _
                                            "   where taxYear = '{0}' And TaxRollNumber = '{1}' " & _
                                            "   ORDER BY 'Certificate', 'TaxYear'", _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQL9, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("Certificate"))) Then
                    certNumber = dv(0)("Certificate").ToString()
                Else
                    certNumber = "N/A"
                End If

                If (Not IsDBNull(dv(0)("Investor"))) Then
                    investor = dv(0)("Investor").ToString()
                Else
                    investor = "N/A"
                End If

                If (Not IsDBNull(dv(0)("dateOfSale"))) Then
                    dateOfSale = dv(0)("dateOfSale").ToString()
                Else
                    dateOfSale = "N/A"
                End If

                If (Not IsDBNull(dv(0)("Months@Rate"))) Then
                    monthsAtRate = dv(0)("Months@Rate").ToString()
                Else
                    monthsAtRate = "N/A"
                End If

                If (Not IsDBNull(dv(0)("Value"))) Then
                    CPInvestValue = dv(0)("Value")
                Else
                    CPInvestValue = 0.0
                End If

                If (Not IsDBNull(dv(0)("Interest"))) Then
                    CPInvestInterest = dv(0)("Interest")
                Else
                    CPInvestInterest = 0.0
                End If

                If (Not IsDBNull(dv(0)("RedeemFee"))) Then
                    CPInvestFee = dv(0)("RedeemFee")
                Else
                    CPInvestFee = 0.0
                End If


            End If
        End Using

        CPInvestValue = Math.Round(CPInvestValue, 2)
        CPInvestInterest = Math.Round(CPInvestInterest, 2)
        CPInvestFee = Math.Round(CPInvestFee, 2)



        Dim printFont10B = New Font("Arial", 9, FontStyle.Bold)
        Dim printFont9R = New Font("Arial", 9, FontStyle.Regular)
        Dim rect1 As New Rectangle(10, 10, 270, 250)

        Dim rect2 As New Rectangle(10, 80, 270, 250)

        Dim rect3 As New Rectangle(10, 130, 270, 250)

        Dim rect4 As New Rectangle(10, 150, 270, 400)

        Dim rect5 As New Rectangle(10, 180, 270, 400)

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

        '' Dim defaultHeader As String() = {"---------------------------------------", sigBlockTitle, sigBlockName, sigBlockAddress, sigBlockCityStateZip, Date.Now, "Operator - "}
        'Dim defaultHeader As String() = {"-----------------------------------------------------", "Operator - " & System.Web.HttpContext.Current.User.Identity.Name, Date.Now, sigBlockCityStateZip, sigBlockAddress, sigBlockName, sigBlockTitle, "-----------------------------------------------------"}

        'For i = 0 To defaultHeader.Count - 1
        '    y = y & " " & vbNewLine & vbNewLine
        '    e.Graphics.DrawString(defaultHeader(i) & y, printFont10B, Brushes.Black, rect1, stringFormat)
        'Next

        Dim paymentDetails2 As String() = {txtPayerName.Text, WhoPaid & " by " & PaymentDescription, "Redemption Receipt"}

        For i = 0 To paymentDetails2.Count - 1
            a = a & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails2(i) & a, printFont9R, Brushes.Black, rect2, stringFormat)
        Next

        Dim paymentDetails As String() = {"Certificate of Purchase: " & certNumber, "Payment applied to the " & _currentTaxYear & " Tax Year", "Thank you for your Payment of: $" & Math.Round(paidToday, 2)}

        For i = 0 To paymentDetails.Count - 1
            y = y & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails(i) & y, printFont9R, Brushes.Black, rect3, stringFormat)
        Next

        'Dim paymentDetails1 As String() = {"Payment applied to the " & ddlTaxYear.SelectedValue & " Tax Year", "Thank you for your Payment of: $" & Math.Round(paidToday, 2)}

        'For i = 0 To paymentDetails1.Count - 1
        '    z = z & " " & vbNewLine & vbNewLine
        '    e.Graphics.DrawString(paymentDetails1(i) & z, printFont9R, Brushes.Black, rect2, stringFormat)
        'Next


        Dim paymentDetails3 As String() = {"Parcel / Tax ID: " & TaxRollMaster.TaxIDNumber, "Tax Roll: " & _priorMonthTaxRoll, "Tax Year: " & _priorMonthTaxYear}
        b = String.Empty
        For j = 0 To paymentDetails3.Count - 1
            b = b & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails3(j) & b, printFont9R, Brushes.Black, rect4, stringFormat)
        Next

        Dim paymentDetails3B As String() = {"Original Date of Sale: " & dateOfSale}
        b = String.Empty
        For j = 0 To paymentDetails3B.Count - 1
            b = b & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails3B(j) & b, printFont9R, Brushes.Black, rect5, stringFormat)
        Next

        Dim paymentReceipt1 As String() = {"- - -", "Total Paid: " & vbTab & vbTab & "$" & (CPInvestValue + CPInvestFee + CPInvestInterest), "Fees: " & vbTab & vbTab & "$" & CPInvestFee, "16% Interest :" & vbTab & vbTab & "$" & CPInvestInterest, "Purchase Value :" & vbTab & vbTab & "$" & CPInvestValue}
        b = String.Empty
        For j = 0 To paymentReceipt1.Count - 1
            b = b & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentReceipt1(j) & b, printFont9R, Brushes.Black, rect6, stringFormatNear)
        Next


        'Dim paymentReceipt2 As String() = {"- - -", "Remaining Balance :" & vbTab & " $" & remainingBalance, "Interest Paid Today:" & vbTab & " $" & interestPaidToday, "Fees Paid Today :" & vbTab & " $" & feesPaidToday, "Taxes Paid Today: " & vbTab & " $" & taxesPaidToday, "Paid Today : " & vbTab & vbTab & " $" & paidToday}
        'b = String.Empty
        'For j = 0 To paymentReceipt2.Count - 1
        '    b = b & " " & vbNewLine & vbNewLine
        '    e.Graphics.DrawString(paymentReceipt2(j) & b, printFont9R, Brushes.Black, rect6, stringFormatNear)
        'Next


        ''-------------------------------------------------------------------------------------------------------------

    End Sub


    Public Sub DrawStringPointFRedeemFromState(ByVal sender As Object, ByVal e As PrintPageEventArgs)

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

        Dim PaymentDescription As String = String.Empty
        Dim WhoPaid As String = String.Empty

        Dim certNumber As String = String.Empty
        Dim investor As String = String.Empty
        Dim dateOfSale As String = String.Empty
        Dim monthsAtRate As String = String.Empty
        Dim CPInvestValue As Decimal = 0.0
        Dim CPInvestInterest As Decimal = 0.0
        Dim CPInvestFee As Decimal = 0.0
        Dim CPInvestRedeemFee As Decimal = 0.0
        Dim TransID As Integer

        Dim SQL6 As String = String.Format("Select a.record_id,a.payment_amt,a.kitty_amt, a.refund_amt, b.paymentDescription,c.descriptionOfPayer " & _
                                         " from genii_user.cashier_transactions a, genii_user.st_payment_instrument b, genii_user.st_who_paid c" & _
                                        " where a.payment_type= b.paymentTypeCode and a.apply_to=c.paymentMadeByCode and a.tax_year ='{0}' and a.Tax_Roll_Number='{1}' and a.transaction_status = 1",
                                        _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQL6, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)

                If (Not IsDBNull(dv(0)("record_id"))) Then
                    TransID = dv(0)("record_id").ToString()
                End If

                If (Not IsDBNull(dv(0)("Payment_amt"))) Then
                    paidToday = dv(0)("Payment_amt").ToString()
                    'Else
                    '    lblPhysicalAddress.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("paymentDescription"))) Then
                    PaymentDescription = dv(0)("paymentDescription").ToString()
                End If

                If (Not IsDBNull(dv(0)("descriptionOfPayer"))) Then
                    WhoPaid = dv(0)("descriptionOfPayer").ToString()
                End If
            End If
        End Using
        Dim SQL5 As String = String.Format("SELECT TaxYear, TaxRollNumber, SUM(CASE WHEN taxtypeid <= 40 THEN chargeamount ELSE 0 END) AS Taxes, " & _
                                        " SUM(CASE WHEN taxtypeid = 80 THEN chargeamount ELSE 0 END) AS Interest,  " & _
                                       " SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN chargeamount ELSE 0 END) AS Fees  " & _
                                       " FROM genii_user.TR_CHARGES " & _
                                       " where taxYear = '{0}' And taxRollNumber = '{1}' GROUP BY TaxYear, TaxRollNumber", _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQL5, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("Taxes"))) Then
                    CPInvestValue = dv(0)("Taxes")
                Else
                    CPInvestValue = 0.0
                End If

                If (Not IsDBNull(dv(0)("Interest"))) Then
                    CPInvestInterest = dv(0)("Interest")
                Else
                    CPInvestInterest = 0.0
                End If

                If (Not IsDBNull(dv(0)("Fees"))) Then
                    CPInvestFee = dv(0)("Fees")
                Else
                    CPInvestFee = 0.0
                End If

            End If
        End Using

        Dim SQL9 As String = String.Format("select a.certificateNumber as Certificate, a.DateOfSale,a.FaceValueofCP, b.LastName as Investor, (CONVERT(int, " & _
                                           " (SELECT     PARAMETER " & _
                                           "   FROM genii_user.ST_PARAMETER " & _
                                           "    WHERE      RECORD_ID = 99920)) + CONVERT(int, " & _
                                           "  (SELECT     PARAMETER " & _
                                           "    FROM          genii_user.ST_PARAMETER AS ST_PARAMETER_1 " & _
                                           "    WHERE      RECORD_ID = 99930)) ) as RedeemFees " & _
                                            " from genii_user.tr_cp a, genii_user.St_investor b " & _
                                            "         where a.investorID = b.investorID " & _
                                            " and b.investorID=1 and taxYear= '{0}' and taxRollNumber= '{1}' ", _priorMonthTaxYear, _priorMonthTaxRoll)

        Using adt As New OleDbDataAdapter(SQL9, Me.ConnectString)
            Dim tblReceiptDetails As New DataTable()

            adt.Fill(tblReceiptDetails)

            If tblReceiptDetails.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblReceiptDetails)
                If (Not IsDBNull(dv(0)("Certificate"))) Then
                    certNumber = dv(0)("Certificate").ToString()
                Else
                    certNumber = "N/A"
                End If

                If (Not IsDBNull(dv(0)("Investor"))) Then
                    investor = dv(0)("Investor").ToString()
                Else
                    investor = "N/A"
                End If

                If (Not IsDBNull(dv(0)("DateofSale"))) Then
                    dateOfSale = dv(0)("DateofSale").ToString()
                Else
                    dateOfSale = "N/A"
                End If

                'If (Not IsDBNull(dv(0)("RedeemFees"))) Then
                '    CPInvestRedeemFee = dv(0)("RedeemFees")
                'Else
                '    CPInvestRedeemFee = 0.0
                'End If

            End If
        End Using

        CPInvestValue = Math.Round(CPInvestValue, 2)
        CPInvestInterest = Math.Round(CPInvestInterest, 2)
        CPInvestFee = Math.Round(CPInvestFee, 2)



        Dim printFont10B = New Font("Arial", 9, FontStyle.Bold)
        Dim printFont9R = New Font("Arial", 9, FontStyle.Regular)
        Dim rect1 As New Rectangle(10, 10, 270, 250)

        Dim rect2 As New Rectangle(10, 80, 270, 250)

        Dim rect3 As New Rectangle(10, 130, 270, 250)

        Dim rect4 As New Rectangle(10, 150, 270, 400)

        Dim rect5 As New Rectangle(10, 180, 270, 400)

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

        '' Dim defaultHeader As String() = {"---------------------------------------", sigBlockTitle, sigBlockName, sigBlockAddress, sigBlockCityStateZip, Date.Now, "Operator - "}
        'Dim defaultHeader As String() = {"-----------------------------------------------------", "Operator - " & System.Web.HttpContext.Current.User.Identity.Name, Date.Now, sigBlockCityStateZip, sigBlockAddress, sigBlockName, sigBlockTitle, "-----------------------------------------------------"}

        'For i = 0 To defaultHeader.Count - 1
        '    y = y & " " & vbNewLine & vbNewLine
        '    e.Graphics.DrawString(defaultHeader(i) & y, printFont10B, Brushes.Black, rect1, stringFormat)
        'Next

        Dim paymentDetails2 As String() = {txtPayerName.Text, WhoPaid & " by " & PaymentDescription, "Redemption Receipt"}

        For i = 0 To paymentDetails2.Count - 1
            a = a & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails2(i) & a, printFont9R, Brushes.Black, rect2, stringFormat)
        Next

        Dim paymentDetails As String() = {"Certificate of Purchase: " & certNumber, "Payment applied to the " & _currentTaxYear & " Tax Year", "Thank you for your Payment of: $" & Math.Round(paidToday, 2)}

        For i = 0 To paymentDetails.Count - 1
            y = y & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails(i) & y, printFont9R, Brushes.Black, rect3, stringFormat)
        Next

        'Dim paymentDetails1 As String() = {"Payment applied to the " & ddlTaxYear.SelectedValue & " Tax Year", "Thank you for your Payment of: $" & Math.Round(paidToday, 2)}

        'For i = 0 To paymentDetails1.Count - 1
        '    z = z & " " & vbNewLine & vbNewLine
        '    e.Graphics.DrawString(paymentDetails1(i) & z, printFont9R, Brushes.Black, rect2, stringFormat)
        'Next


        Dim paymentDetails3 As String() = {"Parcel / Tax ID: " & TaxRollMaster.TaxIDNumber, "Tax Roll: " & _priorMonthTaxRoll, "Tax Year: " & _priorMonthTaxYear}
        b = String.Empty
        For j = 0 To paymentDetails3.Count - 1
            b = b & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails3(j) & b, printFont9R, Brushes.Black, rect4, stringFormat)
        Next

        Dim paymentDetails3B As String() = {"Original Date of Sale: " & dateOfSale}
        b = String.Empty
        For j = 0 To paymentDetails3B.Count - 1
            b = b & " " & vbNewLine & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails3B(j) & b, printFont9R, Brushes.Black, rect5, stringFormat)
        Next

        Dim paymentReceipt1 As String() = {"- - -", "Total Paid: " & vbTab & vbTab & "$" & (CPInvestValue + CPInvestFee + CPInvestInterest), "Fees: " & vbTab & vbTab & "$" & CPInvestFee, "16% Interest :" & vbTab & vbTab & "$" & CPInvestInterest, "Purchase Value :" & vbTab & vbTab & "$" & CPInvestValue}
        b = String.Empty
        For j = 0 To paymentReceipt1.Count - 1
            b = b & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentReceipt1(j) & b, printFont9R, Brushes.Black, rect6, stringFormatNear)
        Next


        'Dim paymentReceipt2 As String() = {"- - -", "Remaining Balance :" & vbTab & " $" & remainingBalance, "Interest Paid Today:" & vbTab & " $" & interestPaidToday, "Fees Paid Today :" & vbTab & " $" & feesPaidToday, "Taxes Paid Today: " & vbTab & " $" & taxesPaidToday, "Paid Today : " & vbTab & vbTab & " $" & paidToday}
        'b = String.Empty
        'For j = 0 To paymentReceipt2.Count - 1
        '    b = b & " " & vbNewLine & vbNewLine
        '    e.Graphics.DrawString(paymentReceipt2(j) & b, printFont9R, Brushes.Black, rect6, stringFormatNear)
        'Next

    End Sub
    Public Sub DrawStringPointFTesting(ByVal sender As Object, ByVal e As PrintPageEventArgs)
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
        Dim z As String = String.Empty
        Dim a As String = String.Empty
        Dim b As String = String.Empty

        ' Dim defaultHeader As String() = {"---------------------------------------", sigBlockTitle, sigBlockName, sigBlockAddress, sigBlockCityStateZip, Date.Now, "Operator - "}
        Dim defaultHeader As String() = {"-----------------------------------------------------", "Operator - " & System.Web.HttpContext.Current.User.Identity.Name, Date.Now, sigBlockCityStateZip, sigBlockAddress, sigBlockName, sigBlockTitle, "-----------------------------------------------------", "TESTING.TESTING.TESTING"}

        For i = 0 To defaultHeader.Count - 1
            y = y & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(defaultHeader(i) & y, printFont10B, Brushes.Black, rect1, stringFormat)
        Next

      

        ''-------------------------------------------------------------------------------------------------------------

        'e.Graphics.DrawString("Thank you for your payment of ", printFont10B, Brushes.Black, rect2, stringFormat)


        'Const ESC_ALIGN_LEFT As String = Chr(27) & Chr(97) & Chr(48)
        'Const ESC_ALIGN_CENTER As String = Chr(27) & Chr(97) & Chr(49)
        'Const ESC_ALIGN_RIGHT As String = Chr(27) & Chr(97) & Chr(50)
        'Const ESC_FONT_REGULAR As String = Chr(27) & Chr(69) & Chr(0)
        'Const ESC_FONT_BOLD As String = Chr(27) & Chr(69) & Chr(1)
        'Const ESC_CHAR_WIDE As String = Chr(29) & Chr(33) & Chr(16)

        '  stringFormat.FormatFlags = StringFormatFlags.NoWrap
        ' e.Graphics.DrawRectangle(Pens.Black, rect1)

        ''Static _printer As PosPrinter
        ''_printer.PrintNormal(PrinterStation.Receipt, ESC_ALIGN_CENTER & "mia" & vbCrLf)
        ''_printer.PrintNormal(PrinterStation.Receipt, ESC_ALIGN_CENTER & "jonatz" & vbCrLf)



        'e.Graphics.DrawString(ESC_ALIGN_CENTER & "mia" & vbCrLf, printFont10B, Brushes.Black, rect1)
        'e.Graphics.DrawString(ESC_ALIGN_CENTER & "jonatz" & vbCrLf, printFont10B, Brushes.Black, rect1)

        'e.Graphics.DrawString("-----------------" & vbCrLf & sigBlockTitle & vbCrLf, printFont10B, Brushes.Black, rect1, stringFormat)
        'e.Graphics.DrawString( & sigBlockName + ", Treasurer" & vbCrLf, printFont10B, Brushes.Black, rect1, stringFormat)
        'e.Graphics.DrawString(vbNewLine & vbNewLine & vbNewLine & vbNewLine & sigBlockAddress, printFont10B, Brushes.Black, rect1, stringFormat)
        'e.Graphics.DrawString(vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & sigBlockCityStateZip, printFont10B, Brushes.Black, rect1, stringFormat)

        'e.Graphics.DrawString(vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & Date.Now, printFont9R, Brushes.Black, rect1, stringFormat)
        'e.Graphics.DrawString(vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & "Operator - ", printFont9R, Brushes.Black, rect1, stringFormat)

        '   e.Graphics.DrawString(Chr(27) & Chr(97) & Chr(49) & "-----------------" & vbCrLf, printFont10B, Brushes.Aqua, rect1, stringFormat)







    End Sub
    Protected Sub ResetCautionLights()
        btnAccountStatusLight.Enabled = False
        btnRollStatusLight.Enabled = False
        btnSuspendLight.Enabled = False
        btnBoardOrderLight.Enabled = False
        btnBankruptcyLight.Enabled = False
        btnAlertLight.Enabled = False
        btnCPLight.Enabled = False
        btnConfLight.Enabled = False
        btnRetMailLight.Enabled = False
        btnParentBal.Enabled = False

    End Sub
    Public Sub LoadDefaultTaxYear()
        Dim SQL As String = String.Format("SELECT parameter FROM genii_user.st_parameter WHERE parameter_name = 'CURRENT_TAXYEAR'")

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCountyName As New DataTable()

            adt.Fill(tblCountyName)

            If tblCountyName.Rows.Count > 0 Then
                If (Not IsDBNull(tblCountyName.Rows(0)("parameter"))) Then
                    ddlTaxYear.Text = Convert.ToString(tblCountyName.Rows(0)("parameter"))
                    _currentTaxYear = Convert.ToString(tblCountyName.Rows(0)("parameter"))
                End If

            End If
        End Using
    End Sub

    ''' <summary>
    ''' Loads tax roll and fills in tax roll information.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Protected Sub btnFindTaxInfo_Click(sender As Object, e As System.EventArgs) Handles ImageButton1.Click 'Handles btnFindTaxInfo.Click
        'CHECK REGULAR EXPRESSIONS HERE...
        '  Me.chkRedeemOnly.Checked = False

        'If (txtBarcode.Text <> String.Empty) Then
        '    Dim TaxIDSubstr As String = txtBarcode.Text
        '    rdoTaxID.Checked = True
        '    txtTaxID.Text = TaxIDSubstr.Substring(2, 8)

        'End If
        '        LoadDefaultTaxYear()

        'If (rdoTaxID.Checked = False And rdoAPN.Checked = False And rdoTaxRollNumber.Checked = False And txtRegSSAN.Text <> String.Empty) Then

        '    Exit Sub
        'End If

        Me.txtAddCP.Text = "0.00"
        Me.txtPriorYears.Text = "0.00"
        Me.txtAddCPState.Text = "0.00"
        Me.txtTotalTaxes.Text = "0.00"
        Me.txtMailToAddress.Text = String.Empty

        'reset caution lights
        ResetCautionLights()

        LoadTaxInfo()

        '  Me.pnlLetterQueuer.Visible = False

        If Me.TaxRollMaster.IsLoaded() Then
            LoadData()
            BindTaxRollInfoGrids()

            CheckAccountStatus()

            Me.txtCheckNumber.Text = String.Empty

            Me.btnSavePayment.Enabled = False
            Me.btnRejectPayment.Enabled = False
            Me.btnShowAccountRemarksPopup.Enabled = True
            ' Me.btnShowTaxRollRemarksPopup.Enabled = True
            '   Me.btnShowOtherYearRemarksPopup.Enabled = True

            If (Me.AccountAlert >= 0) Then
                SetAlertMessage()

                Me.btnAlertLight.Enabled = True
            Else
                Me.btnAlertLight.Enabled = False
            End If

            If (Me.AccountParentBal >= 0) Then
                SetParentBalAlert()

                Me.btnParentBal.Enabled = True
            Else
                Me.btnParentBal.Enabled = False
            End If

            If (Me.AccountSuspend >= 0) Then
                SetSuspendMessage()

                ' Me.btnSuspendLight.Enabled = True
            Else
                Me.btnSuspendLight.Enabled = False
            End If

            If (Me.AccountStatus >= 0) Then
                SetStatusMessage()
                Me.btnAccountStatusLight.Enabled = True
            Else
                Me.btnAccountStatusLight.Enabled = False
            End If

            If (Me.AccountBankruptcy >= 0) Then
                SetBankruptcyMessage()
                Me.btnBankruptcyLight.Enabled = True
            Else
                Me.btnBankruptcyLight.Enabled = False
            End If

            If (Me.TRStatus >= 0) Then
                SetTRStatusMessage()
                Me.btnRollStatusLight.Enabled = True
            Else
                Me.btnRollStatusLight.Enabled = False
            End If


            If (Me.TRBoardOrder >= 0) Then
                SetTRBoardOrderMessage()
                Me.btnBoardOrderLight.Enabled = True
            Else
                Me.btnBoardOrderLight.Enabled = False
            End If

            If (Me.TRCP >= 0) Then
                SetTRCPMessage()
                Me.btnCPLight.Enabled = True
            Else
                Me.btnCPLight.Enabled = False
            End If

            If (Me.TRConfidential >= 0) Then
                SetTRConfidentialMessage()
                Me.btnConfLight.Enabled = True
            Else
                Me.btnConfLight.Enabled = False
            End If

            If (Me.TRMailReturned >= 0) Then
                SetTRMailReturnedMessage()
                Me.btnRetMailLight.Enabled = True
            Else
                Me.btnRetMailLight.Enabled = False
            End If

            chkPriorYears_CheckedChanged2()

        Else
            ClientScript.RegisterStartupScript(Me.GetType(), "TaxRollNotFound", "showMessage('Tax roll not found.', 'Not Found');", True)
            Me.btnSavePayment.Enabled = False
            Me.btnRejectPayment.Enabled = False
            Me.btnShowAccountRemarksPopup.Enabled = False
            '  Me.btnShowTaxRollRemarksPopup.Enabled = False
            '   Me.btnShowOtherYearRemarksPopup.Enabled = False
        End If
    End Sub
#End Region

    Private Sub FillSearchInDropDown()
        Dim SQL As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='Query Search Parameters' ")
        Dim searchItem As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblTRStatus As New DataTable()

            adt2.Fill(tblTRStatus)

            If tblTRStatus.Rows.Count > 0 Then

                For x = 0 To (tblTRStatus.Rows.Count - 1)
                    If (Not IsDBNull(tblTRStatus.Rows(x)("parameter"))) Then
                        searchItem = Convert.ToString(tblTRStatus.Rows(x)("parameter"))
                    End If
                    ddlSearchIn.Items.Add(searchItem)

                Next
            End If
        End Using
    End Sub

    Private Sub FillTaxYearDropDown()
        For i As Integer = 40 To 0 Step -1
            Dim myYear As Integer = i + 1980
            Dim newItem As ListItem = New ListItem(myYear, myYear)

            ddlTaxYear.Items.Add(newItem)
            ddlTaxYear2.Items.Add(newItem)
        Next

        ddlTaxYear.SelectedValue = Date.Today.Year - 1
        ddlTaxYear2.SelectedValue = Date.Today.Year - 1
    End Sub

    Public Sub btnParentParcel_click()
        txtTaxID.Text = btnParentParcel.Text
        btnFindTaxInfo_Click(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' CheckAccountStatus - Check for ACCOUNT_ALERT or ACCOUNT_SUSPEND flags in TAX_ACCOUNT table
    ''' If flags found, show Account Alert and/or Account Suspend notifications to user.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub CheckAccountStatus()
        Dim apn As String

        If (Me.TaxRollMaster.APN.Replace("-", String.Empty) = String.Empty) Then
            If (txtTaxID.Text = String.Empty) Then
                apn = Me.TaxRollMaster.TaxIDNumber
            Else
                apn = txtTaxID.Text
            End If

        Else
            apn = Me.TaxRollMaster.APN.Replace("-", String.Empty)
        End If
        Dim SQL As String = String.Format("SELECT ACCOUNT_ALERT, ACCOUNT_SUSPEND,ACCOUNT_STATUS, isnull(ACCOUNT_BANKRUPTCY,0) as ACCOUNT_BANKRUPTCY, ISNULL(PARENT_BALANCE,0)as PARENT_BALANCE, PARENT_PARCEL " & _
                                          " FROM genii_user.TAX_ACCOUNT " & _
                                          " WHERE ParcelOrTaxID = '{0}' ", apn)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblTaxAccount As New DataTable()

            adt.Fill(tblTaxAccount)

            If tblTaxAccount.Rows.Count > 0 Then


                If (Not IsDBNull(tblTaxAccount.Rows(0)("PARENT_BALANCE"))) Then
                    AccountParentBal = Convert.ToInt32(tblTaxAccount.Rows(0)("PARENT_BALANCE"))
                End If

                If (Not IsDBNull(tblTaxAccount.Rows(0)("PARENT_PARCEL"))) Then
                    If (Convert.ToString(tblTaxAccount.Rows(0)("PARENT_PARCEL")) <> String.Empty) Then
                        lblParentParcel.Visible = True
                        '  lblParentParcel.Text = "Parent Parcel: " & Convert.ToString(tblTaxAccount.Rows(0)("PARENT_PARCEL"))
                        btnParentParcel.Text = Convert.ToString(tblTaxAccount.Rows(0)("PARENT_PARCEL"))
                        AccountParentBal = 1
                    End If
                End If

                If (Not IsDBNull(tblTaxAccount.Rows(0)("ACCOUNT_ALERT"))) Then
                    AccountAlert = Convert.ToInt32(tblTaxAccount.Rows(0)("ACCOUNT_ALERT"))
                End If

                If (Not IsDBNull(tblTaxAccount.Rows(0)("ACCOUNT_SUSPEND"))) Then
                    AccountSuspend = Convert.ToInt32(tblTaxAccount.Rows(0)("ACCOUNT_SUSPEND"))
                End If

                If (Not IsDBNull(tblTaxAccount.Rows(0)("ACCOUNT_STATUS"))) Then
                    AccountStatus = Convert.ToInt32(tblTaxAccount.Rows(0)("ACCOUNT_STATUS"))
                End If

                If (Not IsDBNull(tblTaxAccount.Rows(0)("ACCOUNT_BANKRUPTCY"))) Then
                    AccountBankruptcy = Convert.ToInt32(tblTaxAccount.Rows(0)("ACCOUNT_BANKRUPTCY"))
                End If
            Else
                AccountAlert = 0
                AccountSuspend = 0
                AccountStatus = 0
                ' AccountParentBal = 0
                AccountBankruptcy = 1000
                btnBankruptcyLight.Enabled = False

            End If
        End Using

        Dim row As GridViewRow
        Dim dtaSummaryTaxYear As String = String.Empty

        row = dtaSummary.SelectedRow
        dtaSummaryTaxYear = row.Cells(1).Text


        Dim SQL2 As String = String.Format("SELECT taxrollnumber,STATUS,BOARD_ORDER,isnull(FLAG_CONFIDENTIAL,0) as FLAG_CONFIDENTIAL,isnull(FLAG_MAIL_RETURNED,0)AS FLAG_MAIL_RETURNED " & _
                                          "FROM genii_user.TR " & _
                                          "WHERE taxrollnumber={0} and taxyear ={1}", Me.TaxRollMaster.TaxRollNumber, dtaSummaryTaxYear)


        Using adt2 As New OleDbDataAdapter(SQL2, Me.ConnectString)
            Dim tblTRStatus As New DataTable()

            adt2.Fill(tblTRStatus)

            If tblTRStatus.Rows.Count > 0 Then
                If (Not IsDBNull(tblTRStatus.Rows(0)("STATUS"))) Then
                    TRStatus = Convert.ToInt32(tblTRStatus.Rows(0)("STATUS"))
                End If

                If (Not IsDBNull(tblTRStatus.Rows(0)("BOARD_ORDER"))) Then
                    TRBoardOrder = Convert.ToInt32(tblTRStatus.Rows(0)("BOARD_ORDER"))
                End If

                If (Not IsDBNull(tblTRStatus.Rows(0)("FLAG_CONFIDENTIAL"))) Then
                    TRConfidential = Convert.ToInt32(tblTRStatus.Rows(0)("FLAG_CONFIDENTIAL"))
                End If

                If (Not IsDBNull(tblTRStatus.Rows(0)("FLAG_MAIL_RETURNED"))) Then
                    TRMailReturned = Convert.ToInt32(tblTRStatus.Rows(0)("FLAG_MAIL_RETURNED"))
                End If

            Else
                TRStatus = 1000
                TRBoardOrder = 1000
                TRConfidential = 0
                TRMailReturned = 0

                btnAccountStatusLight.Enabled = False
                btnBoardOrderLight.Enabled = False
            End If
        End Using


        Dim SQL3 As String = String.Format("SELECT taxrollnumber, cp_status " & _
                                          "FROM genii_user.TR_CP " & _
                                          "WHERE taxrollnumber={0} and taxyear ={1}", Me.TaxRollMaster.TaxRollNumber, Me.TaxRollMaster.TaxYear)


        Using adt3 As New OleDbDataAdapter(SQL3, Me.ConnectString)
            Dim tblTRCP As New DataTable()

            adt3.Fill(tblTRCP)

            If tblTRCP.Rows.Count > 0 Then
                If (Not IsDBNull(tblTRCP.Rows(0)("CP_STATUS"))) Then
                    TRCP = Convert.ToInt32(tblTRCP.Rows(0)("CP_STATUS"))
                End If

            Else
                ' TRStatus = 1000
                btnAccountStatusLight.Enabled = False
                btnBoardOrderLight.Enabled = False
            End If
        End Using


        Me.txtAPN.Text = Me.TaxRollMaster.APN
        Me.txtTaxID.Text = Me.TaxRollMaster.TaxIDNumber
        Me.txtRegSSAN.Text = Me.TaxRollMaster.LastName



    End Sub



    ''' <summary>
    ''' Prepare TaxRollMasterClass object with user entered search parameters.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub LoadTaxInfo()
        Me.TaxRollMaster = Nothing
        Dim balanceOnly As Boolean
        Dim trm As TaxRollMasterClass = Me.TaxRollMaster
        Dim searchBy As String = String.Empty

        trm.TaxYear = Me.ddlTaxYear.Text

        If Me.rdoTaxRollNumber.Checked Then
            '  Me.txtAPN.Enabled = False
            '  Me.txtTaxID.Enabled = False
            Me.txtTaxRollNumber.Text = (Me.txtTaxRollNumber.Text).Replace("-", String.Empty)
            trm.TaxRollNumber = Me.txtTaxRollNumber.Text
            searchBy = "taxrollnumber"
        ElseIf Me.rdoAPN.Checked Then
            ' Me.txtTaxID.Enabled = False
            '  Me.txtTaxRollNumber.Enabled = False
            trm.APN = Me.txtAPN.Text.Replace("_"c, String.Empty)
            searchBy = "apn"
        ElseIf Me.rdoTaxID.Checked Then
            '  Me.txtTaxRollNumber.Enabled = False
            '  Me.txtAPN.Enabled = False
            Me.txtTaxID.Text = (Me.txtTaxID.Text).Replace("-", String.Empty)
            trm.TaxIDNumber = Me.txtTaxID.Text

            searchBy = "taxid"
        ElseIf (Not Me.txtRegSSAN.Text = String.Empty) Then
            '   trm.LastName 
            searchBy = "ssan"
        End If

        'If (chkBalanceOnly.Checked) Then
        '    balanceOnly = True
        'Else
        '    balanceOnly = False
        'End If

        If (Me.chkTaxYear.Checked = True) Then
            trm.LoadData(searchBy)
        Else
            trm.LoadDataNoYear(searchBy)
        End If



        Me.TaxRollMaster = trm
    End Sub

    Public Sub chkTaxYear_checkChanged()
        If (Me.chkTaxYear.Checked) Then
            ddlTaxYear.Enabled = True
        ElseIf (chkTaxYear.Checked = False) Then
            ddlTaxYear.Enabled = False
        End If

    End Sub

    ''' <summary>
    ''' Fills in tax information controls for transaction.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub LoadData()
        Dim paymentDate As Date, firstHalfDue As Date, secondHalfDue As Date

        ' paymentDate = CDate(Me.txtPaymentDate.Text)
        Me.TaxRollMaster.GetDelinquentDates(firstHalfDue, secondHalfDue)
        '  Me.TaxRollMaster.GetMailAddress()
        Dim taxes As Decimal, interest As Decimal, fee As Decimal, payments As Decimal

        Dim currentTax As Decimal

        ' Dim SQL3 As String = String.Format("Select sum(account_balance)as account_balance from genii_user.TAX_ACCOUNT where ParcelOrTaxID='{0}' ", TaxRollMaster.TaxIDNumber)

        Dim SQL3 As String = String.Format("select a.parcelOrTaxID, " & _
                                            " case  " & _
                                            " when a.account_balance = b.currentBalance then a.account_balance " & _
                                            " when a.account_balance > b.currentBalance then a.account_balance " & _
                                            " when a.account_balance < b.currentBalance then b.currentBalance " & _
                                            "         End 'Balance' " & _
                                            " from genii_user.tax_account a, genii_user.TR b " & _
                                            " where a.parcelOrTaxID =b.TaxIDNumber and a.ParcelOrTaxID='{0}' " & _
                                            " group by a.parcelOrTaxID, a.account_Balance, b.currentBalance " & _
                                            " order by  'Balance' desc", TaxRollMaster.TaxIDNumber)

        Using adt3 As New OleDbDataAdapter(SQL3, Me.ConnectString)
            Dim tblTotalTax As New DataTable()

            adt3.Fill(tblTotalTax)

            If tblTotalTax.Rows.Count > 0 Then
                If (Not IsDBNull(tblTotalTax.Rows(0)("Balance"))) Then
                    taxes = Convert.ToDecimal(tblTotalTax.Rows(0)("Balance"))
                End If
            Else
                taxes = 0.0
            End If
        End Using

        Me.txtTotalTaxes.Text = Decimal.Round(taxes, 2)

        With Me.TaxRollMaster
            '.RecalculateFees(Me.txtPaymentDate.Text)
            '  taxes = .GetTaxes()

            '  .GetInterestAndFee(Me.txtPaymentDate.Text, interest, fee)
            payments = .GetTotalPayments()

            currentTax = .GetTaxes
            taxes = Decimal.Round(taxes, 2)
            interest = Decimal.Round(interest, 2)
            fee = Decimal.Round(fee, 2)
            '   payments = Decimal.Round(payments, 2)

            Me.txtTaxRollNumber.Text = .TaxRollNumber
            Me.txtAPN.Text = .APN

            If (Me.txtAPN.Text <> String.Empty) Then
                Me.txtTaxID.Text = .APN.Replace("-", String.Empty)

            Else
                Me.txtTaxID.Text = .TaxIDNumber
            End If


            Me.txtTotalTaxes.Text = taxes
            '    Me.txtTotalInterest.Text = interest
            '    Me.txtTotalFees.Text = fee
            '    Me.txtTotalPayments.Text = payments
            '   Me.txtCalculatedBalance.Text = taxes '+ interest + fee - payments
            Me.txtAmountPaid.Text = taxes + interest + fee
            Me.hdnTxtRequiredAmount.Text = taxes + interest + fee ' - payments
            ' Me.txtAmountDueNow.Text = taxes + interest + fee
            '  Me.txtGrandTotal.Text = taxes + interest + fee + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
            'Me.txtCalculatedBalance.Text = .CurrentBalance
            Dim firstHalfDueDate As Date = "10/1" & "/" & ddlTaxYear.Text
            Dim secondHalfDueDate As Date = "03/1"
            Dim forgiveDate As Date = "12/31" & "/" & ddlTaxYear.Text
            Dim forgiveDateRange As Date = "12/15" & "/" & ddlTaxYear.Text
            Dim yearNow As Integer = Year(Now) - 1
            ' firstHalfDueDate = Date.FromOADate(firstHalfDueDate + yearNow)
            ' secondHalfDueDate = Date.FromOADate(secondHalfDueDate.ToString() + yearNow)

            '   firstHalfDueDate = firstHalfDueDate + "" & _ yearNow
            'RULES MTA 04162013
            'if paybothhalves.checked=true 
            'rule 1:  payment_effective_date> first half due date then tax due is equal to total tax
            'rule 2: payment_effective_date>first half due date and total tax< 100 then tax due = total tax
            'rule 3: payment effective date between first half and second half due date and total tax >=100 then tax due = 0.5*(total tax)
            'rule 4: payment effective date and total tax paid <= forgive date then delete the interest, tax due =total tax

            'forgive date =dec 31
            '
            'first half due date = october 1
            'first half delinquent=nov1

            'second half due date=march 1
            'second half delinquent= may 1


            If (paymentDate < firstHalfDueDate) Then
                'rule0
                Me.txtPriorYears.Text = 0.0
                ' Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text
                Me.txtAmountPaid.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
                '  Me.txtGrandTotal.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)

            ElseIf ((chkPayBothHalves.Checked = True) And (paymentDate > firstHalfDueDate) AndAlso (paymentDate < secondHalfDueDate)) Then
                'rule1
                Me.txtPriorYears.Text = currentTax
                '  Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text + interest + fee
                Me.txtAmountPaid.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
                '    Me.txtGrandTotal.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)

            ElseIf ((chkPayBothHalves.Checked = False) And (paymentDate > firstHalfDueDate) AndAlso (paymentDate < secondHalfDueDate)) Then
                'rule1
                Me.txtPriorYears.Text = currentTax / 2
                '   Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text
                Me.txtAmountPaid.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
                '   Me.txtGrandTotal.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)

            ElseIf ((paymentDate > firstHalfDueDate) AndAlso (paymentDate < secondHalfDueDate) And (taxes < 100)) Then
                'rule2
                Me.txtPriorYears.Text = currentTax + interest + fee '- payments
                '    Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text + interest + fee
                Me.txtAmountPaid.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
                '   Me.txtGrandTotal.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)

            ElseIf (((paymentDate > firstHalfDueDate) AndAlso (paymentDate < forgiveDate) Or ((paymentDate < secondHalfDueDate) AndAlso (paymentDate > forgiveDate))) And taxes >= 100) Then
                'rule3
                Me.txtPriorYears.Text = currentTax / 2
                '  Me.txtAmountDueNow.Text = (Me.txtCalculatedBalance.Text + interest + fee)
                Me.txtAmountPaid.Text = (Me.txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
                '   Me.txtGrandTotal.Text = (Me.txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)

            ElseIf ((paymentDate <= forgiveDate) AndAlso (paymentDate > firstHalfDueDate)) Then
                'rule4
                Me.txtPriorYears.Text = currentTax
                '   Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text
                Me.txtAmountPaid.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
                '    Me.txtGrandTotal.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)

            ElseIf ((paymentDate > secondHalfDueDate AndAlso paymentDate < forgiveDate)) Then
                'rule5
                Me.txtPriorYears.Text = currentTax + interest + fee - payments
                '   Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text + interest + fee
                Me.txtAmountPaid.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
                '   Me.txtGrandTotal.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)

                'ElseIf ((chkPayBothHalves.Checked = True) And (paymentDate > secondHalfDueDate AndAlso paymentDate < forgiveDate)) Then
                '    'rule5
                '    Me.txtCalculatedBalance.Text = taxes + interest + fee - payments
                ''    Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text
                '   Me.txtAmountPaid.Text = Me.txtCalculatedBalance.Text

            Else
                Me.txtPriorYears.Text = currentTax + interest + fee - payments
                '  Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text + interest + fee
                Me.txtAmountPaid.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
                '    Me.txtGrandTotal.Text = Me.txtPriorYears.Text + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
            End If






            '  If Me.chkPayBothHalves.Checked Or taxes <= .MaxTaxDueBothHalves Or paymentDate > firstHalfDue Then
            '      ' Full taxes due if taxes is less than $100 or first half due date has passed.
            '     Me.txtAmountDueNow.Text = Me.txtCalculatedBalance.Text
            '      Me.txtAmountPaid.Text = Me.txtCalculatedBalance.Text
            '  Else
            '     ' First half due at this time.
            '      Me.txtAmountDueNow.Text = taxes / 2 + interest + fee - payments
            '     Me.txtAmountPaid.Text = taxes / 2 + interest + fee - payments
            '  End If

            'Me.txtPayerName.Text = .OwnerName
            'If (.MailAddress1.Trim() = "") Then
            '    Me.txtMailToAddress.Text = .OwnerName + vbNewLine + .MailAddress2 + vbNewLine + .MailCityStateCode
            'Else
            '    Me.txtMailToAddress.Text = .OwnerName + vbNewLine + .MailAddress1 + vbNewLine + .MailAddress2 + vbNewLine + .MailCityStateCode
            'End If




        End With

        ' Difference
        If IsNumeric(Me.txtAmountPaid.Text) Then 'IsNumeric(Me.txtGrandTotal.Text) And
            'Integer diff=((txtAmountDueNow .Text)-(txtTotalPayments .Text))-(txtAmountPaid .text)
            Dim tx As String = (taxes + interest + fee - payments - txtAmountPaid.Text).ToString()
            '(CDbl(Me.txtAmountDueNow.Text) - txtTotalPayments.Text - (CDbl(Me.txtAmountPaid.Text)).ToString())
            '      Me.txtDifference.Text = "( " & tx & " )"

            '  Me.txtDifference.ForeColor = Drawing.Color.Red


        End If


        txtTotalTaxes.Text = FormatNumber(txtTotalTaxes.Text, 2, , , TriState.True)
        'txtCalculatedBalance.Text = FormatNumber(txtCalculatedBalance.Text, 2, , , TriState.True)
        'txtTotalInterest.Text = FormatNumber(txtTotalInterest.Text, 2, , , TriState.True)
        'txtTotalFees.Text = FormatNumber(txtTotalFees.Text, 2, , , TriState.True)
        txtPriorYears.Text = FormatNumber(txtPriorYears.Text, 2, , , TriState.True)
        txtAddCP.Text = FormatNumber(txtAddCP.Text, 2, , , TriState.True)
        txtAddCPState.Text = FormatNumber(txtAddCPState.Text, 2, , , TriState.True)
        ' txtTotalPayments.Text = FormatNumber(txtTotalPayments.Text, 2, , , TriState.True)
        '   txtGrandTotal.Text = FormatNumber(txtGrandTotal.Text, 2, , , TriState.True)
        txtAmountPaid.Text = FormatNumber(txtAmountPaid.Text, 2, , , TriState.True)
        txtPriorYears.Text = FormatNumber(txtPriorYears.Text, 2, , , TriState.True)

        ' Control preparation.
        '  Me.btnPrintReceipt.Visible = False
    End Sub


    ''' <summary>
    ''' Binds tax roll information grids.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub BindTaxRollInfoGrids()
        ' Account History tab
        '  lblTaxHistoryAccount.Text = Me.TaxRollMaster.APN
        Me.txtTaxID.Text = Me.TaxRollMaster.APN.Replace("-", String.Empty)

        Dim accountSQL As String = String.Format("SELECT TA.ParcelOrTaxID, TA.APN, TR.TaxYear, TR.TaxRollNumber, " & _
                                                 " ISNULL(CONVERT(varchar(10),NULLIF(TA.GIS_HOUSE_ADDRESS, 0)), '') +  " & _
                                                 "  ' ' + ISNULL(TA.PHYSICAL_CITY, '') + ' ' + ISNULL(TA.PHYSICAL_ZIP, '') AS PARCEL_ADDRESS, " & _
                                                 " ISNULL(TR.MAIL_ADDRESS_2, '') + ' ' + ISNULL(TR.MAIL_CITY, '') + ' ' + ISNULL(TR.MAIL_STATE, '') + ' ' + ISNULL(TR.MAIL_CODE, '') AS MAILING_ADDRESS," & _
                                                 " ISNULL(TR.MAIL_ADDRESS_1,'') as MAIL1,ISNULL(TR.MAIL_ADDRESS_2,'') as MAIL2,ISNULL(TR.MAIL_CITY, '') + ' ' + ISNULL(TR.MAIL_STATE, '') + ' ' + ISNULL(TR.MAIL_CODE, '') as MAIL3," & _
                                                 " CASE ACCOUNT_STATUS WHEN 1 THEN 'Secured - Active' WHEN 2 THEN 'Secured - Merged' " & _
                                                 " WHEN 3 THEN 'Secured - Split' WHEN 4 THEN 'Unsecured - Active' WHEN 5 THEN 'Unsecured - Closed' " & _
                                                 " WHEN 6 THEN 'Unsecured - Abated' END AS ACCOUNT_STATUS " & _
                                                 " FROM genii_user.TAX_ACCOUNT AS TA " & _
                                                 " JOIN genii_user.TR AS TR ON TA.APN = TR.APN " & _
                                                 " WHERE (TA.ParcelOrTaxID = '{0}')", Me.TaxRollMaster.APN.Replace("-", String.Empty))

        '' ' + ISNULL(RMF.STREET_NAME, '') + ' ' + ISNULL(RMF.STREET_TYPE, '') +
        '  " LEFT OUTER JOIN NCIS.dbo.ROAD_MASTER_FILE AS RMF ON TA.ROAD_NUMBER = RMF.ROAD_NUMBER " & _

        Using adt As New OleDbDataAdapter(accountSQL, Me.ConnectString)
            Dim tblAccount As New DataTable()

            adt.Fill(tblAccount)

            If tblAccount.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblAccount)
                dv.RowFilter = "TaxYear = " & Me.TaxRollMaster.TaxYear & " AND TaxRollNumber = " & Me.TaxRollMaster.TaxRollNumber

                If (Not IsDBNull(dv(0)("TaxYear"))) Then
                    '   lblTaxHistoryStatus.Text = dv(0)("ACCOUNT_STATUS").ToString()
                    '   lblTaxHistoryAddress.Text = dv(0)("PARCEL_ADDRESS").ToString()
                    '   lblTaxHistoryMailingAddress.Text = dv(0)("MAILING_ADDRESS").ToString()
                    Dim mail1 As String = dv(0)("MAIL1").ToString()
                    '     If mail1 = String.Empty Then
                    'Me.txtMailToAddress.Text = TaxRollMaster.OwnerName + vbNewLine + dv(0)("MAIL2").ToString() + vbNewLine + dv(0)("MAIL3").ToString()
                    '  Else
                    '     Me.txtMailToAddress.Text = TaxRollMaster.OwnerName + vbNewLine + dv(0)("MAIL1").ToString() + vbNewLine + dv(0)("MAIL2").ToString() + vbNewLine + dv(0)("MAIL3").ToString()
                    ' End If

                    'If (txtMailToAddress.Text = String.Empty) Then
                    'txtMailToAddress.Text = TaxRollMaster.MailAddress1
                    '  End If

                End If

            End If
        End Using

        Dim SitusSQL As String = String.Format("select * from genii_user.Tax_account a left join genii_user.TR b on a.ParcelOrTaxID=b.TaxIDNumber where a.parcelortaxid='" + TaxRollMaster.TaxIDNumber + "' order by a.create_date desc ")


        Using adt As New OleDbDataAdapter(SitusSQL, Me.ConnectString)
            Dim tblSitus As New DataTable()

            adt.Fill(tblSitus)

            If tblSitus.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblSitus)
                If (Not IsDBNull(dv(0)("physical_address"))) Then
                    lblPhysicalAddress.Text = dv(0)("physical_address").ToString()
                Else
                    lblPhysicalAddress.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("physical_city"))) Then
                    lblPhysicalCity.Text = dv(0)("physical_city").ToString()
                    'Else
                    '    lblPhysicalCity.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("physical_zip"))) Then
                    lblPhysicalZip.Text = dv(0)("physical_zip").ToString()
                    'Else
                    '    lblPhysicalZip.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("GIS_HOUSE_ADDRESS"))) Then
                    lblGISHouseNumber.Text = dv(0)("GIS_HOUSE_ADDRESS").ToString()
                    'Else
                    '    lblGISHouseNumber.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("GIS_Road"))) Then
                    lblGIS_Road.Text = dv(0)("GIS_Road").ToString()
                    'Else
                    '    lblGIS_Road.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("PP_PARCEL"))) Then
                    lblPPParcel.Text = dv(0)("PP_PARCEL").ToString()
                Else
                    lblPPParcel.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("PP_SPACE"))) Then
                    lblPPSpace.Text = dv(0)("PP_SPACE").ToString()
                Else
                    lblPPSpace.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("ACCOUNT_CLASS"))) Then
                    If (dv(0)("ACCOUNT_CLASS").ToString() = "R") Then
                        lblAccountClass.Text = "Residential"
                    ElseIf (dv(0)("ACCOUNT_CLASS").ToString() = "M") Then
                        lblAccountClass.Text = "Manufactured Home"
                    ElseIf (dv(0)("ACCOUNT_CLASS").ToString() = "P") Then
                        lblAccountClass.Text = "Personal Property"
                    End If
                    '  lblAccountClass.Text = dv(0)("ACCOUNT_CLASS").ToString()
                Else
                    lblAccountClass.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("ACCOUNT_TYPE"))) Then
                    lblAccountType.Text = dv(0)("ACCOUNT_TYPE").ToString()
                Else
                    lblAccountType.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("PP_VIN"))) Then
                    lblVIN.Text = dv(0)("PP_VIN").ToString()
                Else
                    lblAccountType.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("PHYSICAL_ACRES"))) Then
                    lblAcreage.Text = dv(0)("PHYSICAL_ACRES").ToString()
                Else
                    lblAccountType.Text = "N/A"
                End If

                If (Not IsDBNull(dv(0)("LEGAL"))) Then
                    lblLegal.Text = dv(0)("LEGAL").ToString()
                Else
                    lblLegal.Text = "N/A"
                End If
            End If
        End Using


        '   Dim taxHistorySQL As String = String.Format("SELECT TaxYear AS 'Tax Year', TaxRollNumber AS 'Tax Roll', Status, " & _
        '                                              "ChargeAmount AS 'Taxes', NumPayments AS 'Payments', TotalPaymentAmount AS 'Remitted', " & _
        '                                              "ChargeAmount - TotalPaymentAmount AS 'Balance' " & _
        '                                              "FROM vTaxHistory " & _
        '                                             "WHERE APN = '{0}' " & _
        '                                             "ORDER BY 'Tax Year' DESC", Me.TaxRollMaster.APN)


        '     Dim taxHistorySQL As String = String.Format("SELECT TaxYear AS 'Tax Year',  " & _
        '                                                 " TaxRollNumber AS 'Tax Roll', Status,  " & _
        '                                               " ChargeAmount AS 'Taxes',  " & _
        '                                                 " NumPayments AS 'Payments',  " & _
        '                                            " TotalPaymentAmount AS 'Remitted',  " & _
        '                                            " ChargeAmount - TotalPaymentAmount AS 'Balance' " & _
        '                                            " FROM " & _
        '                                            " vTaxHistory WHERE TaxRollNumber = '{0}' and TaxYear = {1}  " & _
        '                                           " ORDER BY 'Tax Year' DESC", Me.TaxRollMaster.TaxRollNumber, Me.TaxRollMaster.TaxYear)


        'Dim taxHistorySQL As String = String.Format("SELECT TaxYear AS 'Tax Year',  " & _
        '                                                 " TaxRollNumber AS 'Tax Roll', Status,  " & _
        '                                               " isnull(ChargeAmount,0) AS 'Taxes',  " & _
        '                                                 " isnull(NumPayments,0) AS 'Payments',  " & _
        '                                            " isnull(TotalPaymentAmount,0) AS 'Remitted',  " & _
        '                                            " ChargeAmount - ISNULL(TotalPaymentAmount,0) AS 'Balance' " & _
        '                                            " FROM " & _
        '                                            " vTaxHistory WHERE TaxIDNumber = '{0}' " & _
        '                                            " ORDER BY 'Tax Year' DESC", Me.TaxRollMaster.TaxIDNumber, Me.TaxRollMaster.TaxYear)
        ''and TaxYear = {1}  " & _

        '' with above query can also use --Me.TaxRollMaster.APN.Replace("-", String.Empty)-- if taxrollmaster.taxidnumber is null MTA changed05022013


        ''  BindGrid(Me.grdTaxHistory, taxHistorySQL)

        'BindGrid(Me.dtaSummary, taxHistorySQL)
        Dim taxHistorySQL As String = String.Format("select * from dbo.vTaxHistory WHERE TaxIDNumber = '{0}' " & _
                                                           " ORDER BY TaxYear DESC", Me.TaxRollMaster.TaxIDNumber, Me.TaxRollMaster.TaxYear)

        BindGrid(Me.dtaSummary, taxHistorySQL)


        If (dtaSummary.Rows.Count <> 0) Then
            lblHdrAcctHist.Visible = True
        End If

        If (dtaSummary.Rows.Count = 1) Then
            dtaSummary.SelectedIndex = 0
        End If


        'Dim priorYearsSQL As String = String.Format("SELECT TaxYear AS 'Tax Year',  " & _
        '                                                 " TaxRollNumber AS 'Tax Roll', Status,  " & _
        '                                               " isnull(ChargeAmount,0) AS 'Taxes',  " & _
        '                                                 " isnull(NumPayments,0) AS 'Payments',  " & _
        '                                            " isnull(TotalPaymentAmount,0) AS 'Remitted',  " & _
        '                                            " ChargeAmount - ISNULL(TotalPaymentAmount,0) AS 'Balance' " & _
        '                                            " FROM " & _
        '                                            " vTaxHistory WHERE TaxIDNumber = '{0}' and ((chargeamount <> totalpaymentamount) or (totalpaymentamount is null)) " & _
        '                                            " and taxyear not in (" + ddlTaxYear.SelectedValue + ") " & _
        '                                            " ORDER BY 'Tax Year' DESC", Me.TaxRollMaster.TaxIDNumber, Me.TaxRollMaster.TaxYear)
        'and TaxYear = {1}  " & _

        ' with above query can also use --Me.TaxRollMaster.APN.Replace("-", String.Empty)-- if taxrollmaster.taxidnumber is null MTA 05022013


       ' Dim priorYearsSQL As String = String.Format("SELECT * FROM dbo.vPriorYearsOwed where taxIDNumber='{0}'  order by taxyear desc", Me.TaxRollMaster.TaxIDNumber) 'and taxYear not in (" + ddlTaxYear.SelectedValue + ")
        ',case when taxyear=" + ddlTaxYear.SelectedValue + " then '1' else '0' end as 'enabled'

        Dim priorYearsSQL As String = String.Format("SELECT a.*, isnull(b.numPayments,0) as numPayments, b.TotalPaymentAmount as Payments FROM dbo.vPriorYearsOwed a, dbo.vTaxHistory b " & _
                                                        "        WHERE a.taxrollnumber = b.taxrollnumber " & _
                                                        " and a.taxyear=b.taxyear " & _
                                                        " and a.TAXIDNUMBER='{0}' order by a.taxYear desc", Me.TaxRollMaster.TaxIDNumber)

        BindGrid(Me.grdPriorYears, priorYearsSQL)
        If Not (grdPriorYears.Rows.Count = 0) Then
            '  Me.tabContainer.ActiveTabIndex = 6
            Me.lblPriorYearsHeader.Visible = True
            '    Me.btnComputePriorYears.Visible = True
        End If

        Dim x As Integer = grdPriorYears.Rows.Count

        For i = 0 To (x - 1)
            Dim ddlInterestList As DropDownList = New DropDownList
            ddlInterestList = grdPriorYears.Rows(i).Cells(4).FindControl("ddlInterest")

            Dim txRoll As String = grdPriorYears.Rows(i).Cells(2).Text
            Dim txYr As String = grdPriorYears.Rows(i).Cells(1).Text

            Dim ddlInterestSQL As String = String.Format("select interest from dbo.vPriorYearsOwed " & _
                                           " WHERE TaxRollNumber='{0}'  and taxYear={1} ", txRoll, txYr)


            Using adt As New OleDbDataAdapter(ddlInterestSQL, Me.ConnectString)
                Dim tblInterest As New DataTable()
                Dim interest As String


                adt.Fill(tblInterest)

                If tblInterest.Rows.Count > 0 Then
                    If (Not IsDBNull(tblInterest.Rows(0)("interest"))) Then
                        interest = Convert.ToString(tblInterest.Rows(0)("interest"))
                        ddlInterestList.Items.Add("Aged I (" + interest + ")")
                    End If
                End If
            End Using

            Dim ddlPriorInterestSQL As String = String.Format("select priot_interest from dbo.vPriorYearsOwed " & _
                                                " WHERE TaxRollNumber='{0}'  and taxYear={1} ", txRoll, txYr)


            Using adt As New OleDbDataAdapter(ddlPriorInterestSQL, Me.ConnectString)
                Dim tblInterest As New DataTable()
                Dim interest As String

                adt.Fill(tblInterest)

                If tblInterest.Rows.Count > 0 Then
                    If (Not IsDBNull(tblInterest.Rows(0)("priot_interest"))) Then
                        interest = Convert.ToString(tblInterest.Rows(0)("priot_interest"))
                        ddlInterestList.Items.Add("Prior I (" + interest + ")")
                    End If
                End If
            End Using

            ddlInterestList.Items.Add("No I (0.00)")
        Next

       

        'Dim dataSummarySQL As String = String.Format("SELECT * FROM dbo.vPriorYearsOwed where taxIDNumber='{0}' order by taxyear desc", Me.TaxRollMaster.TaxIDNumber) '

        'BindGrid(Me.dtaSummary, dataSummarySQL)

        ' Payment History tab
        'Dim paymentsSQL As String = String.Format("SELECT TC.TASK_DATE AS PaymentDate, TCP.PaymentEffectiveDate, TCP.Pertinent1, " & _
        '                                          "TCP.Pertinent2, TCP.PaymentAmount FROM genii_user.TR_CALENDAR TC " & _
        '                                          "INNER JOIN genii_user.TR_PAYMENTS TCP ON TC.RECORD_ID = TCP.RECORD_ID " & _
        '                                          "WHERE TC.TaxYear = '{0}' AND TC.TaxRollNumber = '{1}'", _
        '                                          Me.TaxRollMaster.TaxYear, Me.TaxRollMaster.TaxRollNumber)


        'Dim row As GridViewRow
        'Dim taxRollNumber As String = String.Empty

        'If (dtaSummary.SelectedIndex <> 0) Then
        '    dtaSummary.SelectedIndex = 0
        '    row = dtaSummary.SelectedRow
        '    taxRollNumber = row.Cells(2).Text

        'Else
        '    row = dtaSummary.SelectedRow
        '    taxRollNumber = row.Cells(2).Text
        'End If

        Dim gvr As GridViewRow
        Dim taxRollnumber As String = String.Empty
        If (dtaSummary.Rows.Count = 1) Then
            dtaSummary.SelectedIndex = 0
            gvr = dtaSummary.SelectedRow
            taxRollnumber = gvr.Cells(2).Text
        ElseIf (dtaSummary.Rows.Count < 1) Then
            ' gvr = dtaSummary.SelectedRow
            taxRollnumber = String.Empty
        ElseIf (dtaSummary.Rows.Count > 1) Then

            If (dtaSummary.SelectedIndex < 0) Then
                dtaSummary.SelectedIndex = 0
            End If

            gvr = dtaSummary.SelectedRow
            taxRollnumber = gvr.Cells(2).Text
        Else
            taxRollnumber = String.Empty
        End If

        Dim paymentsSQL As String = String.Format("SELECT TP.PaymentEffectiveDate AS PaymentDate, TP.PaymentEffectiveDate, TP.Pertinent1, " & _
                                                  "TP.Pertinent2, TP.PaymentAmount FROM genii_user.TR_PAYMENTS TP " & _
                                                  "WHERE TP.TaxYear = '{0}' AND TP.TaxRollNumber = '{1}' ", _
                                                  ddlTaxYear.SelectedValue, taxRollnumber) ' taxRollNumber) 'Me.TaxRollMaster.TaxYear 'Me.TaxRollMaster.TaxYear


        BindGrid(Me.grdPaymentHistory, paymentsSQL)

        ' Tax Calculation tab
        '   Me.TaxRollMaster.TaxCalculationTable.DefaultView.Sort = "TaxYear DESC, TaxChargeCodeID ASC, TaxTypeID ASC"
        '  Me.TaxRollMaster.TaxCalculationTable.DefaultView.RowFilter = "TaxTypeID <= " & Me.TaxRollMaster.MaxTaxTypeID & " and taxYear =" & ddlTaxYear.SelectedValue

        'Dim dt As DataTable = Me.TaxRollMaster.TaxCalculationTable.DefaultView.ToTable(False, "TaxYear", "TaxChargeCodeID", "TaxTypeID", "ChargeAmount")

        'dt.Columns("ChargeAmount").ColumnName = "Tax"


        'Dim taxCalcSQL As String = String.Format("SELECT * from genii_user.TR_CHARGES " & _
        '                                         " WHERE TaxYear = '{0}' AND TaxRollNumber = '{1}' ", _
        '                                         ddlTaxYear.SelectedValue, taxRollNumber) ' taxRollNumber) 'Me.TaxRollMaster.TaxYear 'Me.TaxRollMaster.TaxYear


        Dim taxCalcSQL As String = String.Format("SELECT genii_user.TR_CHARGES.TaxYear, " & _
                                                    " genii_user.TR_CHARGES.TaxRollNumber, " & _
                                                    "  genii_user.TR_CHARGES.TaxChargeCodeID AS 'Auth CD',  " & _
                                                    "  genii_user.LEVY_AUTHORITY.TaxChargeDescription AS 'Authority', " & _
                                                    "  genii_user.TR_CHARGES.TaxTypeID AS 'Type', " & _
                                                    "  genii_user.LEVY_TAX_TYPES.TaxCodeDescription AS 'Type Description', " & _
                                                    "  genii_user.TR_CHARGES.ChargeAmount " & _
                                                    " FROM         genii_user.TR_CHARGES INNER JOIN " & _
                                                    "                       genii_user.LEVY_AUTHORITY ON genii_user.TR_CHARGES.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID INNER JOIN " & _
                                                    "                       genii_user.LEVY_TAX_TYPES ON genii_user.TR_CHARGES.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID " & _
                                                    " WHERE TaxYear = '{0}' AND TaxRollNumber = '{1}' ", ddlTaxYear.SelectedValue, taxRollnumber)

        BindGrid(Me.grdTaxCalc, taxCalcSQL)
        Dim v As Integer = grdTaxCalc.Rows.Count

        For z = 0 To (v - 1)
            ' Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
            'chkCP.Checked = True
            Dim totalCharges As Double
            totalCharges = totalCharges + grdTaxCalc.Rows(z).Cells(4).Text
            lblTaxesTotal.Text = totalCharges

        Next



        'With Me.grdTaxCalc
        '    .DataSource = dt
        '    .DataBind()
        'End With

        ' Late Charges tab
        Dim FeesSQL As String = String.Format("SELECT * from genii_user.TR_CHARGES " & _
                                                        " WHERE taxtypeID>(select parameter from genii_user.ST_PARAMETER where Parameter_name='MaxTaxTypeID') and TaxYear = '{0}' AND TaxRollNumber = '{1}' ", _
                                                        ddlTaxYear.SelectedValue, taxRollnumber) ' taxRollNumber) 'Me.TaxRollMaster.TaxYear 'Me.TaxRollMaster.TaxYear


        BindGrid(Me.grdCharges, FeesSQL)
        'With Me.grdCharges
        '    .DataSource = Me.TaxRollMaster.GetChargesTable()
        '    .DataBind()
        'End With

        Dim paymentDistSQL As String = String.Format("SELECT * from dbo.vApportion " & _
                                                        " WHERE TaxYear = '{0}' AND TaxRollNumber = '{1}' ORDER BY 'AUTH_CD'", _
                                                        ddlTaxYear.SelectedValue, taxRollnumber) ' taxRollNumber) 'Me.TaxRollMaster.TaxYear 'Me.TaxRollMaster.TaxYear
        'Dim paymentDistSQL As String = String.Format("SELECT DISTINCT GENII_USER.CASHIER_APPORTION.TAXCHARGECODEID,  " & _
        '                                                    " GENII_USER.CASHIER_APPORTION.TAXROLLNUMBER,  " & _
        '                                                    " GENII_USER.CASHIER_APPORTION.TAXYEAR, " & _
        '                                                    " CONVERT(varchar(10), genii_user.CASHIER_APPORTION.PaymentDate, 101) as 'PaymentDate', " & _
        '                                                    " genii_user.LEVY_AUTHORITY.TaxChargeDescription + ' (' + genii_user.LEVY_AUTHORITY.TaxChargeCodeID + ')' as 'Levy Authority', " & _
        '                                                    " genii_user.LEVY_TAX_TYPES.TaxCodeDescription + ' (' + genii_user.LEVY_TAX_TYPES.TaxTypeID + ')' as 'TaxType', " & _
        '                                                    " genii_user.CASHIER_APPORTION.GLAccount,  " & _
        '                                                    " genii_user.CASHIER_APPORTION.DateApportioned, " & _
        '                                                    " SUM(GENII_USER.CASHIER_APPORTION.DOLLARAMOUNT) AS AMOUNT  " & _
        '                                                    "         FROM GENII_USER.CASHIER_APPORTION " & _
        '                                                    " INNER JOIN GENII_USER.LEVY_AUTHORITY " & _
        '                                                    " ON genii_user.CASHIER_APPORTION.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID " & _
        '                                                    " INNER JOIN genii_user.LEVY_TAX_TYPES  " & _
        '                                                    " ON genii_user.CASHIER_APPORTION.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID  " & _
        '                                                    "         WHERE TAXYEAR = '{0}' And taxRollnumber = '{1}' " & _
        '                                                    " GROUP BY GENII_USER.CASHIER_APPORTION.TAXCHARGECODEID, " & _
        '                                                    " GENII_USER.CASHIER_APPORTION.TAXROLLNUMBER,  " & _
        '                                                    " GENII_USER.CASHIER_APPORTION.TAXYEAR, " & _
        '                                                    " genii_user.CASHIER_APPORTION.PaymentDate, " & _
        '                                                    " genii_user.LEVY_AUTHORITY.TaxChargeDescription, " & _
        '                                                    " genii_user.LEVY_AUTHORITY.TaxChargeCodeID, " & _
        '                                                    " genii_user.LEVY_TAX_TYPES.TaxCodeDescription, " & _
        '                                                    " genii_user.LEVY_TAX_TYPES.TaxTypeID, " & _
        '                                                    " genii_user.CASHIER_APPORTION.GLAccount, " & _
        '                                                    " genii_user.CASHIER_APPORTION.DateApportioned", ddlTaxYear.SelectedValue, taxRollnumber)

        BindGrid(Me.grdPaymentDist, paymentDistSQL)
        Dim a As Integer = grdPaymentDist.Rows.Count
        For z = 0 To (a - 1)
            ' Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
            'chkCP.Checked = True
            Dim totalCharges As Double
            totalCharges = totalCharges + grdPaymentDist.Rows(z).Cells(6).Text
            lblTotalApportion.Text = totalCharges

        Next
        Dim MailToSQL As String = String.Empty

        If (rdoTaxID.Checked And txtTaxID.Text <> String.Empty) Then
            MailToSQL = String.Format("select  top 1 * from genii_user.TR  where  taxIDNumber='" + TaxRollMaster.TaxIDNumber + "'  order by TaxYear desc")
        ElseIf (rdoAPN.Checked And txtAPN.Text <> String.Empty) Then
            MailToSQL = String.Format("select  top 1 * from genii_user.TR  where  taxIDNumber='" + TaxRollMaster.TaxIDNumber + "'  order by TaxYear desc")
        ElseIf (rdoTaxRollNumber.Checked And txtTaxRollNumber.Text <> String.Empty And chkTaxYear.Checked) Then
            MailToSQL = String.Format("select  top 1 * from genii_user.TR  where  taxIDNumber='" + TaxRollMaster.TaxIDNumber + "' and taxYear = '" + ddlTaxYear.SelectedValue + "'  order by TaxYear desc")
            '  MailToSQL = String.Format("select  top 1 * from genii_user.TR  where taxYear='" + ddlTaxYear.SelectedValue + "' and taxRollNumber=" + TaxRollMaster.TaxRollNumber + "  order by TaxYear desc")
        Else
            MailToSQL = String.Format("select  top 1 * from genii_user.TR  where  taxIDNumber='" + TaxRollMaster.TaxIDNumber + "'  order by TaxYear desc")
        End If


        'taxYear='" + ddlTaxYear.SelectedValue + "' and

        Using adt As New OleDbDataAdapter(MailToSQL, Me.ConnectString)
            Dim tblMailTo As New DataTable()
            Dim ownerName As String = String.Empty
            Dim mailAddress2 As String = String.Empty
            Dim mailAddress1 As String = String.Empty
            Dim mailCity As String = String.Empty
            Dim mailState As String = String.Empty
            Dim mailCode As String = String.Empty
            Dim mailCityStateCode As String = String.Empty
            Dim firstHalfDelinquent As Date
            Dim secondHalfDelinquent As Date

            adt.Fill(tblMailTo)

            If tblMailTo.Rows.Count > 0 Then
                Dim dv As DataView = New DataView(tblMailTo)

                If (Not IsDBNull(dv(0)("owner_name_1"))) Then
                    ownerName = dv(0)("owner_name_1").ToString()

                    If (Not IsDBNull(dv(0)("owner_name_2"))) Then
                        ownerName = dv(0)("owner_name_2").ToString() & " " & ownerName
                    End If

                    If (Not IsDBNull(dv(0)("owner_name_3"))) Then
                        ownerName = dv(0)("owner_name_3").ToString() & " " & ownerName
                    End If
                End If

                If (Not IsDBNull(dv(0)("mail_address_1"))) Then
                    mailAddress1 = dv(0)("mail_address_1").ToString()
                End If

                If (Not IsDBNull(dv(0)("mail_address_2"))) Then
                    mailAddress2 = dv(0)("mail_address_2").ToString()
                End If

                If (Not IsDBNull(dv(0)("mail_city"))) Then
                    mailCity = dv(0)("mail_city").ToString()
                End If

                If (Not IsDBNull(dv(0)("mail_state"))) Then
                    mailState = dv(0)("mail_state").ToString()
                End If

                If (Not IsDBNull(dv(0)("mail_code"))) Then
                    mailCode = dv(0)("mail_code").ToString()
                End If

                If (Not IsDBNull(dv(0)("firstHalfDelinquent"))) Then
                    firstHalfDelinquent = dv(0)("firstHalfDelinquent").ToString()
                    firstHalfDelinquent = firstHalfDelinquent.ToString("d")
                End If

                If (Not IsDBNull(dv(0)("secondHalfDelinquent"))) Then
                    secondHalfDelinquent = dv(0)("secondHalfDelinquent").ToString()
                    secondHalfDelinquent = secondHalfDelinquent.ToString("d")
                End If

                mailCityStateCode = mailCity & " " & mailState & " " & mailCode
                txtPayerName.Text = ownerName ' TaxRollMaster.OwnerName
                If (mailAddress1.Trim() = "") Then
                    Me.txtMailToAddress.Text = ownerName + vbNewLine + mailAddress2 + vbNewLine + mailCityStateCode
                Else
                    Me.txtMailToAddress.Text = ownerName + vbNewLine + mailAddress1 + vbNewLine + mailAddress2 + vbNewLine + mailCityStateCode
                End If

                txtFirstHalfDelinquent.Text = firstHalfDelinquent
                txtSecondHalfDelinquent.Text = secondHalfDelinquent

            Else
                mailCityStateCode = TaxRollMaster.MailCityStateCode
                txtPayerName.Text = TaxRollMaster.LastName  ' TaxRollMaster.OwnerName
                If (mailAddress1.Trim() = "") Then
                    Me.txtMailToAddress.Text = TaxRollMaster.MailAddress1
                Else
                    Me.txtMailToAddress.Text = TaxRollMaster.MailAddress1 + vbNewLine + TaxRollMaster.MailAddress2
                End If

                txtFirstHalfDelinquent.Text = String.Empty
                txtSecondHalfDelinquent.Text = String.Empty
            End If
        End Using


        Dim checkCPSQL As String = "select investorID from genii_user.TR_CP where APN='" + Me.TaxRollMaster.APN + "'"
        Dim investorIDtxt As Integer
        Dim cpSQL2 As String
        Dim cpSQL1 As String

        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()
            Dim cmd As New OleDbCommand()
            cmd.Connection = conn

            cmd.CommandText = "select investorID from genii_user.TR_CP where APN='" + Me.TaxRollMaster.APN + "'"
            cmd = New OleDbCommand(checkCPSQL, conn)

            Dim dr As OleDbDataReader = cmd.ExecuteReader()
            dr.Read()

            If dr.Read() Then
                investorIDtxt = dr.GetInt32(0)
            End If
        End Using



        '   If (investorIDtxt <= 1) Then
        'cpSQL1 = String.Format("SELECT CertificateNumber AS 'Certificat', " & _
        '                       " CP_STATUS AS 'CP_STATUS', " & _
        '                        " TaxYear AS 'Tax Year',  " & _
        '                        " TaxRollNumber As 'Roll Number', " & _
        '                        " genii_user.ST_INVESTOR.LastName AS 'Investor', " & _
        '                        " CONVERT(varchar(10), MonthlyRateOfInterest*100) + '%' AS 'Rate of Interest', " & _
        '                        " CONVERT(varchar(10), DateOfSale, 101) AS 'DateSaleOrPurchase', " & _
        '                        "           '$' + CONVERT(varchar, FaceValueOfCP, 1) AS 'FaceOrPurchase Value', " & _
        '                        " DATEDIFF(mm, DateOfSale, GETDATE()) AS 'Months', " & _
        '                        "           '$' + CONVERT(varchar, CAST((MonthlyRateOfInterest/12)*FaceValueOfCP*DATEDIFF(mm, DateOfSale, GETDATE()) AS DECIMAL(18,2))) AS 'Investor Interest', " & _
        '                        "           '$10.00' AS 'Redemption', " & _
        '                        "           '$' + CONVERT(varchar, CAST(FaceValueOfCP + (MonthlyRateOfInterest/12)*FaceValueOfCP*DATEDIFF(mm, DateOfSale, GETDATE()) + 10 AS DECIMAL(18,2))) AS 'Total' " & _
        '                        "            FROM genii_user.TR_CP " & _
        '                        " INNER JOIN genii_user.ST_INVESTOR " & _
        '                        "     ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " & _
        '                        " WHERE APN = '" + Me.TaxRollMaster.APN + "' and DATE_REDEEMED is null and genii_user.TR_CP.InvestorID = 1  " & _
        '                        " and Taxyear not in (" + ddlTaxYear.SelectedValue + ") order by TaxYear")


        cpSQL1 = String.Format("select *, (taxes+interest+fees + redeemfees - payments)as total from dbo.vCPredeemState where APN='" + Me.TaxRollMaster.APN + "' order by taxyear")
        'taxYear not in (" + ddlTaxYear.SelectedValue + ") and 



        ''  Me.grdCPs.Columns(2).HeaderText = "MIA222"
        'Me.grdCPsState.Columns(5).HeaderText = "Rate of Interest"
        'Me.grdCPsState.Columns(6).HeaderText = "Date of Sale"
        'Me.grdCPsState.Columns(7).HeaderText = "Face Value"

        BindGrid(Me.grdCPsState, cpSQL1)

        If Not (grdCPsState.Rows.Count = 0) Then
            '  Me.tabContainer.ActiveTabIndex = 6
            Me.lblActiveCPHeaderState.Visible = True
        End If

        '  ElseIf (investorIDtxt > 1) Then
        'cpSQL2 = String.Format("SELECT CertificateNumber AS 'Certificat', " & _
        '                        " CP_STATUS AS 'CP_STATUS', " & _
        '                          " TaxYear AS 'Tax Year', " & _
        '                          " TaxRollNumber As 'Roll Number', " & _
        '                          " genii_user.ST_INVESTOR.LastName AS 'Investor', " & _
        '                          " CONVERT(varchar(10), MonthlyRateOfInterest*100) + '%' AS 'Rate of Interest', " & _
        '                          " CONVERT(varchar(10), DateCPPurchased, 101) AS 'DateSaleOrPurchase', " & _
        '                          "          '$' + CONVERT(varchar, PurchaseValue, 1) AS 'FaceOrPurchase Value', " & _
        '                          " DATEDIFF(mm, DateCPPurchased, GETDATE()) AS 'Months', " & _
        '                          "           '$' + CONVERT(varchar, CAST((MonthlyRateOfInterest/12)*PurchaseValue*DATEDIFF(mm, DateCPPurchased, GETDATE()) AS DECIMAL(18,2))) AS 'Investor Interest', " & _
        '                          "           '$10.00' AS 'Redemption', " & _
        '                          "           '$' + CONVERT(varchar, CAST(PurchaseValue + (MonthlyRateOfInterest/12)*PurchaseValue*DATEDIFF(mm, DateCPPurchased, GETDATE()) + 10 AS DECIMAL(18,2))) AS 'Total' " & _
        '                          "           FROM genii_user.TR_CP " & _
        '                          " INNER JOIN genii_user.ST_INVESTOR " & _
        '                          "   ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " & _
        '                          " WHERE APN = '" + Me.TaxRollMaster.APN + "' and DATE_REDEEMED is null and genii_user.TR_CP.InvestorID = 1  " & _
        '                          " and Taxyear not in (" + ddlTaxYear.SelectedValue + ") order by TaxYear")

        cpSQL2 = String.Format("select *, (value+interest + redeemfee)as total from dbo.vCPredeeminvest where APN='" + Me.TaxRollMaster.APN + "' order by taxyear,Certificate")
        'taxyear not in (" + ddlTaxYear.SelectedValue + ") and

        ' Me.grdCPs.Columns(2).HeaderText = "MIA"
        'Me.grdCPsInvestor.Columns(5).HeaderText = "Rate of Interest"
        'Me.grdCPsInvestor.Columns(6).HeaderText = "Date of Purchase"
        'Me.grdCPsInvestor.Columns(7).HeaderText = "Purchase Value"

        BindGrid(Me.grdCPsInvestor, cpSQL2)

        If Not (grdCPsInvestor.Rows.Count = 0) Then
            '  Me.tabContainer.ActiveTabIndex = 6
            Me.lblActiveCPHeader.Visible = True
        End If

        '  End If


        '' CPs tab
        'Dim cpSQL As String = String.Format("SELECT TaxYear, TaxRollNumber, InvestorID, InvestorName, CertificateNumber, " & _
        '                                    "MonthlyRateOfInterest, FaceValueOfCP, PurchaseValue, CP_STATUS " & _
        '                                    "FROM dbo.vTaxCPsIssued WHERE APN = '{0}' and CP_STATUS <> 5 ORDER BY TaxYear", Me.TaxRollMaster.APN)
        'error here ......




        ''Bind Apportion Payments

        'Dim ApportionPaymentsSQL As String = String.Format("SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS 'TaxYear', " & _
        '                                                  " genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS 'TaxRollNumber', " & _
        '                                                  " genii_user.LEVY_AUTHORITY.TaxChargeDescription + ' (' + genii_user.LEVY_AUTHORITY.TaxChargeCodeID + ')' as 'Levy Authority', " & _
        '                                                  " genii_user.LEVY_TAX_TYPES.TaxCodeDescription + ' (' + genii_user.LEVY_TAX_TYPES.TaxTypeID + ')' as 'TaxType', " & _
        '                                                  " CONVERT(varchar(10), genii_user.CASHIER_APPORTION.PaymentDate, 101) as 'PaymentDate', " & _
        '                                                  "       genii_user.CASHIER_APPORTION.GLAccount, " & _
        '                                                  "       genii_user.CASHIER_APPORTION.DateApportioned, " & _
        '                                                  "       '$' + CONVERT(varchar, genii_user.CASHIER_APPORTION.DollarAmount, 1) AS 'Amount' " & _
        '                                                  "      FROM genii_user.CASHIER_TRANSACTIONS  " & _
        '                                                  " INNER JOIN genii_user.CASHIER_APPORTION " & _
        '                                                  "   ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID " & _
        '                                                  " INNER JOIN genii_user.LEVY_AUTHORITY " & _
        '                                                  "   ON genii_user.CASHIER_APPORTION.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID " & _
        '                                                  " INNER JOIN genii_user.LEVY_TAX_TYPES " & _
        '                                                  "   ON genii_user.CASHIER_APPORTION.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID " & _
        '                                                  "       WHERE genii_user.CASHIER_TRANSACTIONS.SESSION_ID = {0} " & _
        '                                                  " ORDER BY genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER, genii_user.LEVY_AUTHORITY.TaxChargeCodeID ", Me.SessionRecordID)

        Dim ApportionPaymentsSQL As String = String.Format("SELECT DISTINCT GENII_USER.CASHIER_APPORTION.TAXCHARGECODEID,  " & _
                                                            " GENII_USER.CASHIER_APPORTION.TAXROLLNUMBER,  " & _
                                                            " GENII_USER.CASHIER_APPORTION.TAXYEAR, " & _
                                                            " CONVERT(varchar(10), genii_user.CASHIER_APPORTION.PaymentDate, 101) as 'PaymentDate', " & _
                                                            " genii_user.LEVY_AUTHORITY.TaxChargeDescription + ' (' + genii_user.LEVY_AUTHORITY.TaxChargeCodeID + ')' as 'Levy Authority', " & _
                                                            " genii_user.LEVY_TAX_TYPES.TaxCodeDescription + ' (' + genii_user.LEVY_TAX_TYPES.TaxTypeID + ')' as 'TaxType', " & _
                                                            " genii_user.CASHIER_APPORTION.GLAccount,  " & _
                                                            " genii_user.CASHIER_APPORTION.DateApportioned, " & _
                                                            " SUM(GENII_USER.CASHIER_APPORTION.DOLLARAMOUNT) AS AMOUNT  " & _
                                                            "         FROM GENII_USER.CASHIER_APPORTION " & _
                                                            " INNER JOIN GENII_USER.LEVY_AUTHORITY " & _
                                                            " ON genii_user.CASHIER_APPORTION.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID " & _
                                                            " INNER JOIN genii_user.LEVY_TAX_TYPES  " & _
                                                            " ON genii_user.CASHIER_APPORTION.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID  " & _
                                                            "         WHERE TAXYEAR = '{0}' And taxRollnumber = '{1}' " & _
                                                            " GROUP BY GENII_USER.CASHIER_APPORTION.TAXCHARGECODEID, " & _
                                                            " GENII_USER.CASHIER_APPORTION.TAXROLLNUMBER,  " & _
                                                            " GENII_USER.CASHIER_APPORTION.TAXYEAR, " & _
                                                            " genii_user.CASHIER_APPORTION.PaymentDate, " & _
                                                            " genii_user.LEVY_AUTHORITY.TaxChargeDescription, " & _
                                                            " genii_user.LEVY_AUTHORITY.TaxChargeCodeID, " & _
                                                            " genii_user.LEVY_TAX_TYPES.TaxCodeDescription, " & _
                                                            " genii_user.LEVY_TAX_TYPES.TaxTypeID, " & _
                                                            " genii_user.CASHIER_APPORTION.GLAccount, " & _
                                                            " genii_user.CASHIER_APPORTION.DateApportioned", ddlTaxYear.SelectedValue, taxRollnumber)

        BindGrid(Me.grdApportionPayments, ApportionPaymentsSQL)


        '' Tax Account Remarks Grid
        Dim taxAccountRemarksSQL As String = String.Format("SELECT RECORD_ID, REMARKS, IMAGE, TASK_DATE, FILE_TYPE " & _
                                                           "FROM genii_user.TAX_ACCOUNT_CALENDAR " & _
                                                           "WHERE ParcelOrTaxID = '{0}' " & _
                                                           "AND YEAR(TASK_DATE) >= '{1}' ORDER BY TASK_DATE ", _
                                                           Me.TaxRollMaster.APN.Replace("-", String.Empty), (DateTime.Now.Year - 1))
        ''"WHERE ParcelOrTaxID = '{0}' ORDER BY TASK_DATE", Me.TaxRollMaster.APN.Replace("-", String.Empty))

        BindGrid(Me.gvAccountRemarks, taxAccountRemarksSQL)


        '' Tax Roll Remarks Grid
        'Dim taxRollRemarksSQL As String = String.Format("SELECT RECORD_ID, REMARKS, IMAGE, TASK_DATE, FILE_TYPE " & _
        '                                                "FROM genii_user.TR_CALENDAR " & _
        '                                                "WHERE TaxRollNumber = {0} ORDER BY TASK_DATE", Me.TaxRollMaster.TaxRollNumber)

        'BindGrid(Me.gvTaxRollRemarks, taxRollRemarksSQL)

        '' Other Year Remarks Grid
        'Dim otherYearRemarksSQL As String = String.Format("SELECT RECORD_ID, REMARKS, IMAGE, TASK_DATE, FILE_TYPE " & _
        '                                                  "FROM genii_user.TAX_ACCOUNT_CALENDAR " & _
        '                                                  "WHERE ParcelOrTaxID = '{0}' " & _
        '                                                  "AND YEAR(TASK_DATE) < '{1}' ORDER BY TASK_DATE ", Me.TaxRollMaster.APN.Replace("-", String.Empty), DateTime.Now.Year - 1)

        'BindGrid(Me.gvOtherYearRemarks, otherYearRemarksSQL)
    End Sub

    Private Sub GridView1_RowCreated(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles dtaSummary.RowCreated
        If e.Row.RowType = DataControlRowType.DataRow Then
            e.Row.Attributes("onmouseover") = "this.style.cursor='pointer';this.style.textDecoration='underline';"
            e.Row.Attributes("onmouseout") = "this.style.textDecoration='none';"
            e.Row.ToolTip = "Click to select row"
            e.Row.Attributes("onclick") = Me.Page.ClientScript.GetPostBackClientHyperlink(Me.dtaSummary, "Select$" & e.Row.RowIndex)
        End If

    End Sub
    'Private Sub GridView2_RowCreated(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles grdPriorYears.RowCreated
    '    If e.Row.RowType = DataControlRowType.DataRow Then
    '        e.Row.Attributes("onmouseover") = "this.style.cursor='pointer';this.style.textDecoration='underline';"
    '        e.Row.Attributes("onmouseout") = "this.style.textDecoration='none';"
    '        e.Row.ToolTip = "Click to select row"
    '        e.Row.Attributes("onclick") = Me.Page.ClientScript.GetPostBackClientHyperlink(Me.grdPriorYears, "Select$" & e.Row.RowIndex)
    '    End If

    '    'Dim row As GridViewRow = dtaSummary.SelectedRow
    '    'Dim taxyear As String = row.Cells(1).Text ' dtaSummary.Rows(row.Cells).Cells(1).Text
    '    'ddlTaxYear.Text = taxyear
    '    'btnFindTaxInfo_Click(Me, EventArgs.Empty)
    'End Sub
    'Public Sub GridView2_SelectedIndexChanged()

    '    For Each gvr As GridViewRow In grdPriorYears.Rows
    '        Dim cb As CheckBox = grdPriorYears.SelectedRow.FindControl("chkPriorYears")
    '        If (cb.Checked) Then
    '            cb.AutoPostBack = False
    '        End If
    '    Next

    '    Dim row As GridViewRow = grdPriorYears.SelectedRow
    '    Dim taxyear As String = row.Cells(2).Text ' dtaSummary.Rows(row.Cells).Cells(1).Text
    '    ddlTaxYear.Text = taxyear
    '    btnFindTaxInfo_Click(Me, EventArgs.Empty)

    'End Sub
    Public Sub GridView1_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        Dim row As GridViewRow = dtaSummary.SelectedRow
        Dim taxyear As String = row.Cells(1).Text
        Dim taxRollNumber As String = row.Cells(2).Text ' dtaSummary.Rows(row.Cells).Cells(1).Text
        ddlTaxYear.Text = taxyear
        TaxRollMaster.TaxRollNumber = taxRollNumber
        'txtTaxRollNumber.Text = taxRollNumber
        'rdoTaxRollNumber.Checked = True

        btnFindTaxInfo_Click(Me, EventArgs.Empty)

    End Sub

    'Public Sub GridView3_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
    '    Dim row As GridViewRow = grdPriorYears.SelectedRow
    '    Dim taxyear As String = row.Cells(2).Text ' dtaSummary.Rows(row.Cells).Cells(1).Text
    '    Dim rdo As RadioButton = Me

    '    ddlTaxYear.Text = taxyear
    '    btnFindTaxInfo_Click(Me, EventArgs.Empty)

    'End Sub
    'Protected Sub chkRedeemOnly_Click(sender As Object, e As EventArgs)
    '    If (chkRedeemOnly.Checked = True) Then
    '        Me.txtGrandTotal.Text = CDec(txtPriorYears.Text) '- CDec(txtCalculatedBalance.Text) - CDec(txtTotalInterest.Text) - CDec(txtTotalFees.Text)
    '        Me.txtAmountPaid.Text = Me.txtPriorYears.Text
    '        ' Me.txtTotalTaxes.Text = "0.00"
    '        '  Me.txtCalculatedBalance.Text = "0.00"
    '        '   Me.txtTotalInterest.Text = "0.00"
    '        '   Me.txtTotalFees.Text = "0.00"

    '    Else
    '        btnFindTaxInfo_Click(Me, EventArgs.Empty)
    '    End If

    'End Sub


    Public Sub checkCPAll()
        Dim v As Integer = grdCPsInvestor.Rows.Count
        Dim chk As CheckBox
        chk = grdCPsInvestor.HeaderRow.FindControl("chkCPSelectAll")

        If (chk.Checked) Then
            For z = 0 To (v - 1)
                Dim chkCP As CheckBox = grdCPsInvestor.Rows(z).FindControl("chkCP")
                chkCP.Checked = True
                chkCP_CheckedChanged(Me, EventArgs.Empty)
            Next
        Else
            For z = 0 To (v - 1)
                Dim chkCP As CheckBox = grdCPsInvestor.Rows(z).FindControl("chkCP")
                chkCP.Checked = False
            Next
        End If
    End Sub

    Protected Sub chkCP_CheckedChanged(sender As Object, e As EventArgs)

        '  Dim chk As CheckBox = grdCPsInvestor.HeaderRow.FindControl("chkCPSelectAll")
        Dim total As Double = 0

        Dim v As Integer = grdCPsInvestor.Rows.Count
        Dim x As Integer = 0
        Dim y As Integer = grdCPsInvestor.Rows.Count
        Dim z As Integer = 0
        Dim ctr As Integer = 0

        Dim CertNumberCount As Integer = 0
        Dim disableCertificateNumber As Boolean

        '  If (chk.Checked) Then
        'For z = 0 To (v - 1)
        '    Dim chkCP As CheckBox = grdCPsInvestor.Rows(z).FindControl("chkCP")

        '    chkCP.Checked = True

        'Next

        '  Dim idx As Integer = grdCPsInvestor.SelectedIndex


        For z = 0 To (v - 1)
            Dim chkCP As CheckBox = grdCPsInvestor.Rows(z).FindControl("chkCP")
            If (chkCP.Checked = True) Then

                Dim certNumber As String = grdCPsInvestor.Rows(z).Cells(3).Text

                For a = 0 To (v - 1)
                    Dim certNumbersCheck As String = grdCPsInvestor.Rows(a).Cells(3).Text
                    Dim chkCPa As CheckBox = grdCPsInvestor.Rows(a).FindControl("chkCP")
                    If (certNumbersCheck.Equals(certNumber)) Then
                        chkCPa.Checked = True

                    End If
                Next
                total = total + grdCPsInvestor.Rows(z).Cells(10).Text
                ctr = ctr + 1
            ElseIf (chkCP.Checked = False) Then

                Dim certNumber As String = grdCPsInvestor.Rows(z).Cells(3).Text

                For a = 0 To (v - 1)
                    Dim certNumbersCheck As String = grdCPsInvestor.Rows(a).Cells(3).Text
                    Dim chkCPa As CheckBox = grdCPsInvestor.Rows(a).FindControl("chkCP")
                    If (certNumbersCheck.Equals(certNumber)) Then
                        chkCPa.Checked = False

                    End If
                Next

            End If


        Next
        '  ElseIf (chk.Checked = False) Then

        'For z = 0 To (v - 1)
        '    Dim chkCP As CheckBox = grdCPsInvestor.Rows(z).FindControl("chkCP")

        '    chkCP.Checked = False

        'Next

        'For z = 0 To (v - 1)
        '    Dim chkCP As CheckBox = grdCPsInvestor.Rows(z).FindControl("chkCP")
        '    If (chkCP.Checked) Then
        '        total = total + grdCPsInvestor.Rows(z).Cells(10).Text
        '        ctr = ctr + 1
        '    Else
        '        chkCP.Checked = False
        '    End If


        'Next

        'End If




        'For z = 0 To (v - 1)
        '    Dim chkCP As CheckBox = grdCPsInvestor.Rows(z).FindControl("chkCP")
        '    If (chkCP.Checked) Then
        '        total = total + grdCPsInvestor.Rows(z).Cells(10).Text
        '        ctr = ctr + 1
        '    Else
        '        chkCP.Checked = False
        '    End If


        'Next



        'For x = 0 To (y - 1)
        '    Dim chkCP As CheckBox = grdCPsInvestor.Rows(x).FindControl("chkCP")
        '    If (disableCertificateNumber = True) Then
        '        chkCP.Checked = True
        '        ' chkCP.Enabled = False
        '    End If
        '    If (chkCP.Checked) Then
        '        total = total + grdCPsInvestor.Rows(x).Cells(10).Text
        '        ctr = ctr + 1
        '    End If

        'Next

        If (ctr > 0) Then
            '   Me.txtAmountPaid.Enabled = False
            'chkPartialPayment.Visible = True
            ' chkPartialPayment.Enabled = True
            Me.btnSavePayment.Enabled = True
            Me.btnRejectPayment.Enabled = True
        Else
            ' Me.txtAmountPaid.Enabled = True
            '  chkPartialPayment.Visible = False
            '  chkPartialPayment.Enabled = False
        End If

        Me.txtAddCP.Text = total
        '   Me.lblCPCollectionsInvestor.Text = total
        Dim amountdue As Double ' = CDec(Me.txtCalculatedBalance.Text)

        amountdue = amountdue + total
        Me.txtAmountPaid.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) '- CDec(txtTotalPayments.Text)
        Me.hdnTxtRequiredAmount.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
        '   Me.txtGrandTotal.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) ' - CDec(txtTotalPayments.Text)
        ' Me.txtDifference.Text = Me.txtGrandTotal.Text - Me.txtAmountPaid.Text
        '  End If
        '  End If

        txtTotalTaxes.Text = FormatNumber(txtTotalTaxes.Text, 2, , , TriState.True)
        '    txtCalculatedBalance.Text = FormatNumber(txtCalculatedBalance.Text, 2, , , TriState.True)
        '   txtTotalInterest.Text = FormatNumber(txtTotalInterest.Text, 2, , , TriState.True)
        '   txtTotalFees.Text = FormatNumber(txtTotalFees.Text, 2, , , TriState.True)
        txtPriorYears.Text = FormatNumber(txtPriorYears.Text, 2, , , TriState.True)
        txtAddCP.Text = FormatNumber(txtAddCP.Text, 2, , , TriState.True)
        txtAddCPState.Text = FormatNumber(txtAddCPState.Text, 2, , , TriState.True)
        '    txtTotalPayments.Text = FormatNumber(txtTotalPayments.Text, 2, , , TriState.True)
        '  txtGrandTotal.Text = FormatNumber(txtGrandTotal.Text, 2, , , TriState.True)
        txtAmountPaid.Text = FormatNumber(txtAmountPaid.Text, 2, , , TriState.True)





        '  System.Web.HttpContext.Current.Response.Write("<SCRIPT LANGUAGE='JavaScript'>")
        ' System.Web.HttpContext.Current.Response.Write("alert(' CP Amount has been added to Total Amount Due')")
        '  System.Web.HttpContext.Current.Response.Write("</SCRIPT>")

        ' Response.Write("<script>")
        '  Response.Write("$(this).dialog('destroy');")
        '  Response.Write("</script>")


        ' For Each eRow As GridViewRow In grdCPs.Rows
        'chkCP = CType(grdCPs.Rows(eRow.RowIndex).FindControl("chkCP"), CheckBox).Checked
        'txtAddCP

        '  Dim gvRow As GridViewRow = CType(CType(sender, Control).Parent.Parent,  _
        '                              GridViewRow)
        '  Dim index As Integer = gvRow.RowIndex
        '  Dim a As String = gvRow.Cells(index).Controls.ToString

        '  Dim chk As CheckBox = sender

        '   Dim bool As Boolean = chk.Checked
        '    Dim r As GridViewRow = chk.NamingContainer
        '   Dim t As TextBox = r.FindControl("Redemption")
        '   Dim tx As String = r.FindControl("Redemption").ToString




        '  Me.txtAddCP.Text = "100"

        '  Next
    End Sub

    Public Sub chkCPStateAll()
        Dim v As Integer = grdCPsState.Rows.Count
        Dim chk As CheckBox
        chk = grdCPsState.HeaderRow.FindControl("chkCPStateSelectAll")

        If (chk.Checked) Then
            For z = 0 To (v - 1)
                Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
                chkCP.Checked = True
                chkCPState_CheckedChanged(Me, EventArgs.Empty)
            Next
        Else
            For z = 0 To (v - 1)
                Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
                chkCP.Checked = False
            Next
        End If
    End Sub

    Protected Sub chkCPState_CheckedChanged(sender As Object, e As EventArgs)

        Dim chk As CheckBox = grdCPsState.HeaderRow.FindControl("chkCPStateSelectAll")
        Dim total As Double = 0

        Dim v As Integer = grdCPsState.Rows.Count
        Dim x As Integer = 0
        Dim y As Integer = grdCPsState.Rows.Count
        Dim z As Integer = 0
        Dim ctr As Integer = 0
        ' If (chk.Checked) Then
        'For x = 0 To (y - 1)
        'Dim chkRow As CheckBox = grdCPs.Rows(x).FindControl("chkCP")
        '  chkRow.Checked = True

        '  Next x
        '  End If

        'If (chk.Checked) Then
        '    For z = 0 To (v - 1)
        '        Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
        '        chkCP.Checked = True

        '    Next
        '    For z = 0 To (v - 1)
        '        Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
        '        If (chkCP.Checked) Then
        '            total = total + grdCPsState.Rows(z).Cells(9).Text
        '            ctr = ctr + 1
        '        Else
        '            chkCP.Checked = False
        '        End If


        '    Next
        'ElseIf (chk.Checked = False) Then
        '    'For z = 0 To (v - 1)
        '    '    Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
        '    '    chkCP.Checked = False
        '    'Next

        '    For z = 0 To (v - 1)
        '        Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
        '        If (chkCP.Checked) Then
        '            total = total + grdCPsState.Rows(z).Cells(9).Text
        '            ctr = ctr + 1
        '        Else
        '            chkCP.Checked = False
        '        End If


        '    Next
        'End If

        For z = 0 To (v - 1)
            Dim chkCP As CheckBox = grdCPsState.Rows(z).FindControl("chkCPState")
            If (chkCP.Checked) Then

                Dim certNumber As String = grdCPsState.Rows(z).Cells(3).Text

                For a = 0 To (v - 1)
                    Dim certNumbersCheck As String = grdCPsState.Rows(a).Cells(3).Text
                    Dim chkCPa As CheckBox = grdCPsState.Rows(a).FindControl("chkCPState")
                    If (certNumbersCheck.Equals(certNumber)) Then
                        chkCPa.Checked = True

                    End If
                Next

                total = total + grdCPsState.Rows(z).Cells(9).Text
                ctr = ctr + 1
            Else
                chkCP.Checked = False
            End If


        Next



        'For x = 0 To (y - 1)
        '    Dim chkCP As CheckBox = grdCPsState.Rows(x).FindControl("chkCPState")

        '    If (chkCP.Checked) Then
        '        total = total + grdCPsState.Rows(x).Cells(9).Text
        '        ctr = ctr + 1
        '    End If

        'Next

        If (ctr > 0) Then
            '  Me.txtAmountPaid.Enabled = False
            '  chkPartialPayment.Visible = True
            ' chkPartialPayment.Enabled = True
            Me.btnSavePayment.Enabled = True
            Me.btnRejectPayment.Enabled = True
        Else
            '  Me.txtAmountPaid.Enabled = True
            '  chkPartialPayment.Visible = False
            '  chkPartialPayment.Enabled = False
        End If

        Me.txtAddCPState.Text = total
        '   Me.lblCPCollections.Text = total
        Dim amountdue As Double = CDec(Me.txtPriorYears.Text)

        amountdue = amountdue + total
        ' Me.txtAmountDueNow.Text = amountdue
        Me.txtAmountPaid.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) '- CDec(txtTotalPayments.Text)
        Me.hdnTxtRequiredAmount.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text)
        '  Me.txtGrandTotal.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) '- CDec(txtTotalPayments.Text)
        ' Me.txtDifference.Text = Me.txtGrandTotal.Text - Me.txtAmountPaid.Text
        '  End If


        'Me.txtPriorYears.Text = total
        ''  Me.lblCPCollections.Text = total
        'Dim amountdue As Double '= CDec(Me.txtCalculatedBalance.Text)

        'amountdue = amountdue + total
        '' Me.txtAmountDueNow.Text = amountdue
        'Me.txtAmountPaid.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) - CDec(txtTotalPayments.Text)
        'Me.txtGrandTotal.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) - CDec(txtTotalPayments.Text)
        'Me.txtDifference.Text = Me.txtGrandTotal.Text - Me.txtAmountPaid.Text
        ''  End If


        txtTotalTaxes.Text = FormatNumber(txtTotalTaxes.Text, 2, , , TriState.True)
        '     txtCalculatedBalance.Text = FormatNumber(txtCalculatedBalance.Text, 2, , , TriState.True)
        '      txtTotalInterest.Text = FormatNumber(txtTotalInterest.Text, 2, , , TriState.True)
        '      txtTotalFees.Text = FormatNumber(txtTotalFees.Text, 2, , , TriState.True)
        txtPriorYears.Text = FormatNumber(txtPriorYears.Text, 2, , , TriState.True)
        txtAddCP.Text = FormatNumber(txtAddCP.Text, 2, , , TriState.True)
        txtAddCPState.Text = FormatNumber(txtAddCPState.Text, 2, , , TriState.True)
        ' txtTotalPayments.Text = FormatNumber(txtTotalPayments.Text, 2, , , TriState.True)
        '   txtGrandTotal.Text = FormatNumber(txtGrandTotal.Text, 2, , , TriState.True)
        txtAmountPaid.Text = FormatNumber(txtAmountPaid.Text, 2, , , TriState.True)

    End Sub

    'Public Sub chkFGI_CheckChanged()
    '    Dim v As Integer = grdPriorYears.Rows.Count
    '    'Dim txtInterest(v - 1) As String
    '    '_txtInterest() = _txtInterest(v - 1)

    '    Array.Resize(_txtInterest, v - 1)
    '    For z = 0 To (v - 1)
    '        Dim chkFGI As CheckBox = grdPriorYears.Rows(z).FindControl("chkFGI")
    '        '  Dim hdnInterest As HiddenField = grdPriorYears.Rows(z).FindControl("hdnInterest")
    '        '   Dim txtInterest As String = grdPriorYears.Rows(z).Cells(5).Text
    '        ' _txtInterest(z) = grdPriorYears.Rows(z).Cells(5).Text
    '        If (chkFGI.Checked) Then
    '            '    Dim txtInterest As Double = CDec(grdPriorYears.Rows(z).Cells(4).Text)
    '            Dim oldBalance As Double = CDec(grdPriorYears.Rows(z).Cells(7).Text)
    '            Dim newBalance As Double = oldBalance - CDec(grdPriorYears.Rows(z).Cells(6).Text) 'CDec(hdnInterest.Value)
    '            grdPriorYears.Rows(z).Cells(7).Text = newBalance.ToString()
    '            grdPriorYears.Rows(z).Cells(8).Text = "0.00"


    '        ElseIf (chkFGI.Checked = False) Then


    '            If (CDec(grdPriorYears.Rows(z).Cells(7).Text) <> CDec(grdPriorYears.Rows(z).Cells(8).Text)) Then
    '                grdPriorYears.Rows(z).Cells(8).Text = "$" & CDec(grdPriorYears.Rows(z).Cells(7).Text)
    '                Dim oldBalance As Double = CDec(grdPriorYears.Rows(z).Cells(3).Text)
    '                Dim newBalance As Double = oldBalance + CDec(grdPriorYears.Rows(z).Cells(7).Text)
    '                grdPriorYears.Rows(z).Cells(10).Text = newBalance.ToString()
    '            End If

    '            ' grdPriorYears.Rows(z).Cells(5).Text = grdPriorYears.Rows(z).Cells(6).

    '            'grdPriorYears.Rows(z).Cells(5).Text = _txtInterest(z)
    '            'Dim priorYearsSQL As String = String.Format("SELECT * FROM dbo.vPriorYearsOwed where taxIDNumber='{0}'  order by taxyear desc", Me.TaxRollMaster.TaxIDNumber) 'and taxYear not in (" + ddlTaxYear.SelectedValue + ")                

    '            'BindGrid(Me.grdPriorYears, priorYearsSQL)
    '            'If Not (grdPriorYears.Rows.Count = 0) Then
    '            '    Me.lblPriorYearsHeader.Visible = True
    '            'End If
    '        End If
    '    Next
    '    chkPriorYears_CheckedChanged2()
    'End Sub

    'Public Sub chkPM_CheckChanged()
    '    Dim v As Integer = grdPriorYears.Rows.Count
    '    'Dim txtInterest(v - 1) As String
    '    '_txtInterest() = _txtInterest(v - 1)

    '    Array.Resize(_txtInterest, v - 1)
    '    For z = 0 To (v - 1)
    '        Dim chkPM As CheckBox = grdPriorYears.Rows(z).FindControl("chkPM")
    '        '  Dim hdnInterest As HiddenField = grdPriorYears.Rows(z).FindControl("hdnInterest")
    '        '   Dim txtInterest As String = grdPriorYears.Rows(z).Cells(5).Text
    '        ' _txtInterest(z) = grdPriorYears.Rows(z).Cells(5).Text
    '        If (chkPM.Checked) Then
    '            '    Dim txtInterest As Double = CDec(grdPriorYears.Rows(z).Cells(4).Text)
    '            Dim oldBalance As Double = CDec(grdPriorYears.Rows(z).Cells(3).Text)
    '            Dim newBalance As Double = oldBalance + CDec(grdPriorYears.Rows(z).Cells(6).Text) 'CDec(hdnInterest.Value)
    '            grdPriorYears.Rows(z).Cells(10).Text = newBalance.ToString()
    '            grdPriorYears.Rows(z).Cells(8).Text = (grdPriorYears.Rows(z).Cells(6).Text)


    '        ElseIf (chkPM.Checked = False) Then


    '            If (CDec(grdPriorYears.Rows(z).Cells(7).Text) <> CDec(grdPriorYears.Rows(z).Cells(8).Text)) Then
    '                grdPriorYears.Rows(z).Cells(8).Text = "$" & CDec(grdPriorYears.Rows(z).Cells(7).Text)
    '                Dim oldBalance As Double = CDec(grdPriorYears.Rows(z).Cells(3).Text)
    '                Dim newBalance As Double = oldBalance + CDec(grdPriorYears.Rows(z).Cells(7).Text)
    '                grdPriorYears.Rows(z).Cells(10).Text = newBalance.ToString()
    '            End If

    '            ' grdPriorYears.Rows(z).Cells(5).Text = grdPriorYears.Rows(z).Cells(6).

    '            'grdPriorYears.Rows(z).Cells(5).Text = _txtInterest(z)
    '            'Dim priorYearsSQL As String = String.Format("SELECT * FROM dbo.vPriorYearsOwed where taxIDNumber='{0}'  order by taxyear desc", Me.TaxRollMaster.TaxIDNumber) 'and taxYear not in (" + ddlTaxYear.SelectedValue + ")                

    '            'BindGrid(Me.grdPriorYears, priorYearsSQL)
    '            'If Not (grdPriorYears.Rows.Count = 0) Then
    '            '    Me.lblPriorYearsHeader.Visible = True
    '            'End If
    '        End If
    '    Next
    '    chkPriorYears_CheckedChanged2()
    'End Sub
    Public Sub checkPriorYearsAll()
        Dim v As Integer = grdPriorYears.Rows.Count
        Dim chk As CheckBox
        chk = grdPriorYears.HeaderRow.FindControl("chkPriorYearsSelectAll")

        If (chk.Checked) Then
            For z = 0 To (v - 1)
                Dim chkPriorYears As CheckBox = grdPriorYears.Rows(z).FindControl("chkPriorYears")
                chkPriorYears.Checked = True
                chkPriorYears_CheckedChanged(Me, EventArgs.Empty)
            Next
        Else
            For z = 0 To (v - 1)
                Dim chkPriorYears As CheckBox = grdPriorYears.Rows(z).FindControl("chkPriorYears")
                chkPriorYears.Checked = False
            Next
        End If
    End Sub

    Public Sub chkPriorYears_CheckedChanged(sender As Object, e As EventArgs)

        Dim chk As CheckBox
        '  If (grdPriorYears.HeaderRow.FindControl("chkPriorYearsSelectAll") = IsNothing()) Then
        chk = grdPriorYears.HeaderRow.FindControl("chkPriorYearsSelectAll")
        ' Else
        'do nothing
        '   End If

        Dim total As Double = 0

        Dim v As Integer = grdPriorYears.Rows.Count
        Dim x As Integer = 0
        Dim y As Integer = grdPriorYears.Rows.Count
        Dim z As Integer = 0
        Dim ctr As Integer = 0

        If (chk.Checked) Then
            For z = 0 To (v - 1)
                Dim chkPriorYears As CheckBox = grdPriorYears.Rows(z).FindControl("chkPriorYears")
                Dim ddlInterestList As DropDownList = grdPriorYears.Rows(z).Cells(4).FindControl("ddlInterest")
                Dim stringArray() As String = Split(ddlInterestList.SelectedValue, "I")
                Dim interestType As String = Trim(stringArray(0))
                Dim taxYear As String = grdPriorYears.Rows(z).Cells(1).Text
                Dim taxRoll As String = grdPriorYears.Rows(z).Cells(2).Text
                '  Dim interestValue As Double = Trim(stringArray(0))

                Dim currentInterest As Double
                Dim priorInterest As Double

                Dim SQL8 As String = String.Format("SELECT * from dbo.vPriorYearsOwed " & _
                                         "         where taxYear = " + taxYear + " And taxRollNumber = " + taxRoll + " ")

                Using adt As New OleDbDataAdapter(SQL8, Me.ConnectString)
                    Dim tblReceiptDetails As New DataTable()

                    adt.Fill(tblReceiptDetails)

                    If tblReceiptDetails.Rows.Count > 0 Then
                        Dim dv As DataView = New DataView(tblReceiptDetails)
                        If (Not IsDBNull(dv(0)("Interest"))) Then
                            currentInterest = Convert.ToDouble(dv(0)("Interest"))
                        End If
                        If (Not IsDBNull(dv(0)("PRIOT_INTEREST"))) Then
                            priorInterest = Convert.ToDouble(dv(0)("PRIOT_INTEREST"))
                        End If

                    End If
                End Using

                Dim balance As Double = CDec(grdPriorYears.Rows(z).Cells(3).Text) + CDec(grdPriorYears.Rows(z).Cells(5).Text) - CDec(grdPriorYears.Rows(z).Cells(6).Text)
                If (interestType = "Aged") Then
                    balance = balance + currentInterest
                ElseIf (interestType = "Prior") Then
                    balance = (balance) + priorInterest
                ElseIf (interestType = "No") Then
                    balance = (balance)
                End If

                grdPriorYears.Rows(z).Cells(7).Text = balance

                chkPriorYears.Checked = True
                If (chkPriorYears.Checked) Then
                    ddlInterestList.Enabled = True
                    '  Dim txroll As String = grdPriorYears.Rows(x).Cells(2).Text
                    ' total = total + grdPriorYears.Rows(x).Cells(6).Text
                    ctr = ctr + 1

                    Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(z).FindControl("txtPriorYearAmount")
                    txtPriorYearAmount.Enabled = True
                    txtPriorYearAmount.Text = CDec(grdPriorYears.Rows(z).Cells(7).Text)
                    Dim amount = txtPriorYearAmount.Text
                    total = total + amount
                Else
                    ddlInterestList.Enabled = False
                    chkPriorYears.Checked = False
                    Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(z).FindControl("txtPriorYearAmount")
                    txtPriorYearAmount.Enabled = False
                    txtPriorYearAmount.Text = "0.00"
                End If

            Next
        ElseIf (chk.Checked = False) Then
            For z = 0 To (v - 1)
                Dim chkPriorYears As CheckBox = grdPriorYears.Rows(z).FindControl("chkPriorYears")
                Dim ddlInterestList As DropDownList = grdPriorYears.Rows(z).Cells(4).FindControl("ddlInterest")
                Dim stringArray() As String = Split(ddlInterestList.SelectedValue, "I")
                Dim interestType As String = Trim(stringArray(0))
                Dim taxYear As String = grdPriorYears.Rows(z).Cells(1).Text
                Dim taxRoll As String = grdPriorYears.Rows(z).Cells(2).Text
                '  Dim interestValue As Double = Trim(stringArray(0))

                Dim currentInterest As Double
                Dim priorInterest As Double

                Dim SQL8 As String = String.Format("SELECT * from dbo.vPriorYearsOwed " & _
                                         "         where taxYear = " + taxYear + " And taxRollNumber = " + taxRoll + " ")

                Using adt As New OleDbDataAdapter(SQL8, Me.ConnectString)
                    Dim tblReceiptDetails As New DataTable()

                    adt.Fill(tblReceiptDetails)

                    If tblReceiptDetails.Rows.Count > 0 Then
                        Dim dv As DataView = New DataView(tblReceiptDetails)
                        If (Not IsDBNull(dv(0)("Interest"))) Then
                            currentInterest = Convert.ToDouble(dv(0)("Interest"))
                        End If
                        If (Not IsDBNull(dv(0)("PRIOT_INTEREST"))) Then
                            priorInterest = Convert.ToDouble(dv(0)("PRIOT_INTEREST"))
                        End If

                    End If
                End Using

                Dim balance As Double = CDec(grdPriorYears.Rows(z).Cells(3).Text) + CDec(grdPriorYears.Rows(z).Cells(5).Text) - CDec(grdPriorYears.Rows(z).Cells(6).Text)
                If (interestType = "Aged") Then
                    balance = balance + currentInterest
                ElseIf (interestType = "Prior") Then
                    balance = (balance) + priorInterest
                ElseIf (interestType = "No") Then
                    balance = (balance)
                End If

                grdPriorYears.Rows(z).Cells(7).Text = balance


                If (chkPriorYears.Checked) Then
                    ddlInterestList.Enabled = True
                    '  Dim txroll As String = grdPriorYears.Rows(x).Cells(2).Text
                    ' total = total + grdPriorYears.Rows(x).Cells(6).Text
                    ctr = ctr + 1

                    Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(z).FindControl("txtPriorYearAmount")
                    txtPriorYearAmount.Enabled = True
                    txtPriorYearAmount.Text = CDec(grdPriorYears.Rows(z).Cells(7).Text)
                    Dim amount = txtPriorYearAmount.Text
                    total = total + amount
                Else
                    ddlInterestList.Enabled = False
                    chkPriorYears.Checked = False
                    Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(z).FindControl("txtPriorYearAmount")
                    txtPriorYearAmount.Enabled = False
                    txtPriorYearAmount.Text = "0.00"
                End If


            Next
        End If

        'For x = 0 To (y - 1)
        '    Dim chkPriorYears As CheckBox = grdPriorYears.Rows(x).FindControl("chkPriorYears")
        '    If (chkPriorYears.Checked) Then
        '        '  Dim txroll As String = grdPriorYears.Rows(x).Cells(2).Text
        '        ' total = total + grdPriorYears.Rows(x).Cells(6).Text
        '        ctr = ctr + 1

        '        Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(x).FindControl("txtPriorYearAmount")
        '        txtPriorYearAmount.Enabled = True
        '        txtPriorYearAmount.Text = CDec(grdPriorYears.Rows(x).Cells(6).Text)
        '        Dim amount = txtPriorYearAmount.Text
        '        total = total + amount
        '    End If

        'Next

        If (ctr > 0) Then
            ' Me.txtAmountPaid.Enabled = False
            '    chkPartialPayment.Visible = True
            '  chkPartialPayment.Enabled = True
            Me.btnSavePayment.Enabled = True
            Me.btnRejectPayment.Enabled = True

        Else
            '  Me.txtAmountPaid.Enabled = True
            '  chkPartialPayment.Visible = False
            '    chkPartialPayment.Enabled = False
        End If

        Me.txtPriorYears.Text = total
        '  Me.lblCPCollections.Text = total
        Dim amountdue As Double '= CDec(Me.txtCalculatedBalance.Text)

        amountdue = amountdue + total
        ' Me.txtAmountDueNow.Text = amountdue
        Me.txtAmountPaid.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) ' - CDec(txtTotalPayments.Text)
        Me.hdnTxtRequiredAmount.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) ' Me.txtAmountPaid.Text
        ' Me.txtGrandTotal.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) ' - CDec(txtTotalPayments.Text)
        '   Me.txtDifference.Text = Me.txtGrandTotal.Text - Me.txtAmountPaid.Text
        '  End If

        txtTotalTaxes.Text = FormatNumber(txtTotalTaxes.Text, 2, , , TriState.True)
        '    txtCalculatedBalance.Text = FormatNumber(txtCalculatedBalance.Text, 2, , , TriState.True)
        '    txtTotalInterest.Text = FormatNumber(txtTotalInterest.Text, 2, , , TriState.True)
        '    txtTotalFees.Text = FormatNumber(txtTotalFees.Text, 2, , , TriState.True)
        txtPriorYears.Text = FormatNumber(txtPriorYears.Text, 2, , , TriState.True)
        txtAddCP.Text = FormatNumber(txtAddCP.Text, 2, , , TriState.True)
        '  txtTotalPayments.Text = FormatNumber(txtTotalPayments.Text, 2, , , TriState.True)
        '  txtGrandTotal.Text = FormatNumber(txtGrandTotal.Text, 2, , , TriState.True)
        txtAmountPaid.Text = FormatNumber(txtAmountPaid.Text, 2, , , TriState.True)
        '   txtAmountPaid.Enabled = False
    End Sub

    Public Sub chkPriorYears_CheckedChanged2()

        ' Dim chk As CheckBox = New CheckBox
        'chk = grdPriorYears.HeaderRow.FindControl("chkPriorYearsSelectAll")
        ' ' Else
        'do nothing
        '   End If

        Dim total As Double = 0

        Dim v As Integer = grdPriorYears.Rows.Count
        Dim x As Integer = 0
        Dim y As Integer = grdPriorYears.Rows.Count
        Dim z As Integer = 0
        Dim ctr As Integer = 0

        ' If (chk.Checked) Then
        'For z = 0 To (v - 1)
        'Dim chkPriorYears As CheckBox = grdPriorYears.Rows(z).FindControl("chkPriorYears")

        '  chkPriorYears.Checked = True

        '  Next
        '  ElseIf (chk.Checked = False) Then
        For z = 0 To (v - 1)
            Dim chkPriorYears As CheckBox = grdPriorYears.Rows(z).FindControl("chkPriorYears")
            If (chkPriorYears.Checked) Then
                '  Dim txroll As String = grdPriorYears.Rows(x).Cells(2).Text
                ' total = total + grdPriorYears.Rows(x).Cells(6).Text
                ctr = ctr + 1

                Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(z).FindControl("txtPriorYearAmount")
                txtPriorYearAmount.Enabled = True
                txtPriorYearAmount.Text = CDec(grdPriorYears.Rows(z).Cells(7).Text)
                Dim amount = txtPriorYearAmount.Text
                total = total + amount
            Else
                chkPriorYears.Checked = False
                Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(z).FindControl("txtPriorYearAmount")
                txtPriorYearAmount.Enabled = False
                txtPriorYearAmount.Text = "0.00"
            End If


        Next
        ' End If

        'For x = 0 To (y - 1)
        '    Dim chkPriorYears As CheckBox = grdPriorYears.Rows(x).FindControl("chkPriorYears")
        '    If (chkPriorYears.Checked) Then
        '        '  Dim txroll As String = grdPriorYears.Rows(x).Cells(2).Text
        '        ' total = total + grdPriorYears.Rows(x).Cells(6).Text
        '        ctr = ctr + 1

        '        Dim txtPriorYearAmount As TextBox = grdPriorYears.Rows(x).FindControl("txtPriorYearAmount")
        '        txtPriorYearAmount.Enabled = True
        '        txtPriorYearAmount.Text = CDec(grdPriorYears.Rows(x).Cells(6).Text)
        '        Dim amount = txtPriorYearAmount.Text
        '        total = total + amount
        '    End If

        'Next

        If (ctr > 0) Then
            '    Me.txtAmountPaid.Enabled = False
            '    chkPartialPayment.Visible = True
            '  chkPartialPayment.Enabled = True
            Me.btnSavePayment.Enabled = True
            Me.btnRejectPayment.Enabled = True

        Else
            '   Me.txtAmountPaid.Enabled = True
            '  chkPartialPayment.Visible = False
            '    chkPartialPayment.Enabled = False
        End If

        Me.txtPriorYears.Text = total
        '  Me.lblCPCollections.Text = total
        Dim amountdue As Double '= CDec(Me.txtCalculatedBalance.Text)

        amountdue = amountdue + total
        ' Me.txtAmountDueNow.Text = amountdue
        Me.txtAmountPaid.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) '- CDec(txtTotalPayments.Text)
        Me.hdnTxtRequiredAmount.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) ' Me.txtAmountPaid.Text
        ' Me.txtGrandTotal.Text = CDec(txtPriorYears.Text) + CDec(txtAddCP.Text) + CDec(txtAddCPState.Text) '- CDec(txtTotalPayments.Text)
        ' Me.txtDifference.Text = Me.txtGrandTotal.Text - Me.txtAmountPaid.Text
        '  End If

        txtTotalTaxes.Text = FormatNumber(txtTotalTaxes.Text, 2, , , TriState.True)
        '    txtCalculatedBalance.Text = FormatNumber(txtCalculatedBalance.Text, 2, , , TriState.True)
        '    txtTotalInterest.Text = FormatNumber(txtTotalInterest.Text, 2, , , TriState.True)
        '    txtTotalFees.Text = FormatNumber(txtTotalFees.Text, 2, , , TriState.True)
        txtPriorYears.Text = FormatNumber(txtPriorYears.Text, 2, , , TriState.True)
        txtAddCP.Text = FormatNumber(txtAddCP.Text, 2, , , TriState.True)
        '   txtTotalPayments.Text = FormatNumber(txtTotalPayments.Text, 2, , , TriState.True)
        '  txtGrandTotal.Text = FormatNumber(txtGrandTotal.Text, 2, , , TriState.True)
        txtAmountPaid.Text = FormatNumber(txtAmountPaid.Text, 2, , , TriState.True)
        '  txtAmountPaid.Enabled = False
    End Sub
    'Protected Sub chkPartialPayment_Click(sender As Object, e As EventArgs)
    '    ' Dim chk As CheckBox = grdCPs.HeaderRow.FindControl("chkPartialPayment")
    '    ' Dim x As Integer = 0
    '    ' Dim y As Integer = grdCPs.Rows.Count
    '    If (Me.chkPartialPayment.Checked) Then
    '        txtAmountPaid.Enabled = True
    '    Else
    '        txtAmountPaid.Enabled = False
    '    End If


    'End Sub
    Protected Sub chkAllCP_CheckedChanged(sender As Object, e As EventArgs)
        Dim chk As CheckBox = grdCPsInvestor.HeaderRow.FindControl("chkCPSelectAll")
        Dim x As Integer = 0
        Dim y As Integer = grdCPsInvestor.Rows.Count
        If (chk.Checked) Then
            For x = 0 To (y - 1)
                Dim chkRow As CheckBox = grdCPsInvestor.Rows(x).FindControl("chkCP")

                chkRow.Checked = True

            Next x
        ElseIf (chk.Checked = False) Then
            For x = 0 To (y - 1)
                Dim chkRow As CheckBox = grdCPsInvestor.Rows(x).FindControl("chkCP")
                chkRow.Checked = False

            Next x

        End If


    End Sub
    'Protected Sub grdTaxCalc_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles grdTaxCalc.RowDataBound


    'End Sub

    Protected Sub grdApportionPayments_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles grdApportionPayments.RowDataBound

        '   Dim total As Double = 0

        ' If (e.Row.RowType = DataControlRowType.DataRow) Then
        'Dim sNum As String = e.Row.Cells(7).Text

        '  Dim sum As Double

        '  If (Double.TryParse(sNum, sum)) Then
        'total += sum
        '   End If

        '  Me.lblTotalApportionmentPayment.Text = total.ToString()

        '  End If
        '  BindPendingPaymentsGrids()

        Dim objTotalApportions As Object = If(Me.ApportionDetailsTable.Columns.Contains("Amount"), _
                                              Me.ApportionDetailsTable.Compute("SUM(Amount)", String.Empty), _
                                              Nothing)

        If IsNumeric(objTotalApportions) Then
            Me.lblTotalApportionmentPayment.Text = CDec(objTotalApportions).ToString("C") 'lblTotalPendingPayments.Text
            Me.lblAsApportioned.Text = CDec(objTotalApportions).ToString("C") 'lblTotalPendingPayments.Text
            '   Me.btnSaveAll.Visible = True
        Else
            Me.lblTotalApportionmentPayment.Text = String.Empty
            Me.lblAsApportioned.Text = String.Empty
            '  Me.btnSaveAll.Visible = False
        End If

        '  BindPendingPaymentsGrids()

    End Sub


    ''' <summary>
    ''' Binds grid with given select command.
    ''' </summary>
    ''' <param name="grid"></param>
    ''' <param name="commandText"></param>
    ''' <remarks>Helper function</remarks>
    Private Sub BindGrid(grid As GridView, commandText As String)
        Dim dt As New DataTable()

        Using adt As New OleDbDataAdapter(commandText, Me.ConnectString)
            adt.SelectCommand.CommandTimeout = 300
            adt.Fill(dt)
        End Using

        With grid
            .DataSource = dt
            .DataBind()
        End With
    End Sub

    Private Sub BindDataGrid(grid As DataGrid, commandText As String)
        Dim dt As New DataTable()

        Using adt As New OleDbDataAdapter(commandText, Me.ConnectString)
            adt.SelectCommand.CommandTimeout = 300
            adt.Fill(dt)
        End Using

        With grid
            .DataSource = dt
            .DataBind()
        End With
    End Sub


    ''' <summary>
    ''' Binds the Cashier Session Activity tab.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub BindPendingPaymentsGrids()
        ' Summary
        ' Username and login time are bound in LoadLogin()
        LoadLogoutInfo()
        '  BindApportionsGrid()
        Dim totalPayments As Decimal, totalTax As Decimal, totalRefunds As Decimal, totalKittyFund As Decimal
        Dim totalCash As Decimal, totalChecks As Decimal, totalCreditCard As Decimal, totalCreditron As Decimal
        Dim totalMoneyOrder As Decimal, totalOtherPaid As Decimal
        Dim totalDeclined As Decimal

        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(PAYMENT_AMT)", String.Empty).ToString(), totalPayments)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(PAYMENT_AMT)", "PAYMENT_TYPE = 2").ToString(), totalCash)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(PAYMENT_AMT)", "PAYMENT_TYPE = 1").ToString(), totalChecks)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(PAYMENT_AMT)", "PAYMENT_TYPE = 10").ToString(), totalCreditCard)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(PAYMENT_AMT)", "PAYMENT_TYPE = 4").ToString(), totalCreditron)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(PAYMENT_AMT)", "PAYMENT_TYPE = 3").ToString(), totalMoneyOrder)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(PAYMENT_AMT)", "PAYMENT_TYPE = 5").ToString(), totalOtherPaid)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(TAX_AMT)", String.Empty).ToString(), totalTax)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(REFUND_AMT)", String.Empty).ToString(), totalRefunds)
        Decimal.TryParse(Me.CashierTransactionsTable.Compute("SUM(KITTY_AMT)", String.Empty).ToString(), totalKittyFund)
        Decimal.TryParse(Me.DeclinedPaymentsTable.Compute("SUM(DECLINED_AMT)", String.Empty).ToString(), totalDeclined)

        Me.lblPendTransNum.Text = Me.CashierTransactionsTable.Rows.Count + Me.DeclinedPaymentsTable.Rows.Count
        Me.lblPendPayments.Text = totalPayments.ToString("C")
        Me.lblPendCash.Text = totalCash.ToString("C")
        Me.lblPendChecks.Text = totalChecks.ToString("C")
        Me.lblPendCreditCard.Text = totalCreditCard.ToString("C")
        Me.lblPendCreditron.Text = totalCreditron.ToString("C")
        Me.lblPendMoneyOrder.Text = totalMoneyOrder.ToString("C")
        Me.lblPendOtherPaid.Text = totalOtherPaid.ToString("C")
        Me.lblPendTax.Text = totalTax.ToString("C")
        Me.lblPendRefunds.Text = totalRefunds.ToString("C")
        Me.lblPendKittyFund.Text = totalKittyFund.ToString("C")
        Me.lblPendDeclined.Text = totalDeclined.ToString("C")

        Me.lblPendAllocatedTotal.Text = (totalTax + totalRefunds + totalKittyFund + totalDeclined).ToString("C")

        ' Pending payments
        Me.grdPendingPayments.DataSource = Me.CashierTransactionsTable()
        Me.grdPendingPayments.DataBind()
        Me.lblTotalPendingPayments.Text = totalPayments.ToString("C")
        '    Me.lblTotalApportionmentPayment.Text = totalPayments.ToString("C")
        '  Me.lblAsApportioned.Text = totalPayments.ToString("C")

        Dim startCash As Decimal = CDec(lblPendCashBoxBalance.Text)
        Me.lblRequiredCash.Text = startCash + CDec(lblPendCash.Text) '(startCash + totalTax + totalRefunds + totalKittyFund + totalDeclined).ToString()
        ' Me.lblPendDifference.Text = (totalPayments - (totalTax + totalRefunds + totalKittyFund + totalDeclined)).ToString("C")
        Dim asApportioned As Decimal
        If (lblAsApportioned.Text = String.Empty) Then
            asApportioned = 0.0
            Me.lblAsApportioned.Text = "0.00"
        Else
            asApportioned = CDec(lblAsApportioned.Text)
        End If

        Me.lblPendDifference.Text = (CDec(lblPendPayments.Text)) - (asApportioned)
        Me.txtLogoutRequiredCash.Text = totalCash.ToString("C") + startCash 'lblPendPayments.Text + startCash

        ' Declined payments
        Me.grdDeclinedPayments.DataSource = Me.DeclinedPaymentsTable
        Me.grdDeclinedPayments.DataBind()

        ' Tax payments
        Dim TaxView As New DataView(Me.CashierTransactionsTable())
        TaxView.RowFilter = "TAX_AMT <> 0"


        Me.grdPendingTax.DataSource = TaxView
        Me.grdPendingTax.DataBind()

        ' Refunds
        Dim RefundsView As New DataView(Me.CashierTransactionsTable())
        RefundsView.RowFilter = "REFUND_AMT <> 0"
        Me.grdRefunds.DataSource = RefundsView
        Me.grdRefunds.DataBind()

        ' Kitty Fund
        Dim KittyView As New DataView(Me.CashierTransactionsTable())
        KittyView.RowFilter = "KITTY_AMT <> 0"
        Me.grdKittyFunds.DataSource = KittyView
        Me.grdKittyFunds.DataBind()
    End Sub


    ''' <summary>
    ''' Binds the Closeout tab.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub BindApportionsGrid()
        '  Me.grdApportionPayments.DataSource = Me.ApportionDetailsTable
        '  Me.grdApportionPayments.DataBind()


        ' Me.grdApportionPayments.DataSource = Me.ApportionDetailsTable
        ' Me.grdApportionPayments.DataBind()

        Dim ApportionPaymentsSQL As String = String.Format("SELECT genii_user.CASHIER_APPORTION.TAXYEAR AS 'TaxYear', " & _
                                                         " genii_user.CASHIER_APPORTION.TAXROLLNUMBER AS 'TaxRollNumber', " & _
                                                         " genii_user.LEVY_AUTHORITY.TaxChargeDescription + ' (' + genii_user.LEVY_AUTHORITY.TaxChargeCodeID + ')' as 'Levy Authority', " & _
                                                         " genii_user.LEVY_TAX_TYPES.TaxCodeDescription + ' (' + genii_user.LEVY_TAX_TYPES.TaxTypeID + ')' as 'TaxType', " & _
                                                         " CONVERT(varchar(10), genii_user.CASHIER_APPORTION.PaymentDate, 101) as 'PaymentDate', " & _
                                                         "       genii_user.CASHIER_APPORTION.GLAccount, " & _
                                                         "       genii_user.CASHIER_APPORTION.DateApportioned, " & _
                                                         "       '$' + CONVERT(varchar, genii_user.CASHIER_APPORTION.DollarAmount, 1) AS 'Amount' " & _
                                                         "      FROM genii_user.CASHIER_TRANSACTIONS  " & _
                                                         " INNER JOIN genii_user.CASHIER_APPORTION " & _
                                                         "   ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID " & _
                                                         " INNER JOIN genii_user.LEVY_AUTHORITY " & _
                                                         "   ON genii_user.CASHIER_APPORTION.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID " & _
                                                         " INNER JOIN genii_user.LEVY_TAX_TYPES " & _
                                                         "   ON genii_user.CASHIER_APPORTION.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID " & _
                                                         "       WHERE genii_user.CASHIER_TRANSACTIONS.SESSION_ID = {0} " & _
                                                         " ORDER BY genii_user.CASHIER_APPORTION.TAXYEAR,genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER, genii_user.LEVY_AUTHORITY.TaxChargeCodeID ", Me.SessionRecordID)

        BindGrid(Me.grdApportionPayments, ApportionPaymentsSQL)

        Dim objTotalApportions As Object = If(Me.ApportionDetailsTable.Columns.Contains("Amount"), _
                                              Me.ApportionDetailsTable.Compute("SUM(Amount)", String.Empty), _
                                              Nothing)

        'If IsNumeric(objTotalApportions) Then
        '    Me.lblTotalApportionmentPayment.Text = CDec(objTotalApportions).ToString("C") 'lblTotalPendingPayments.Text '
        '    '   Me.btnSaveAll.Visible = True
        '    Me.lblAsApportioned.Text = CDec(objTotalApportions).ToString("C") ' lblTotalPendingPayments.Text '
        'Else
        '    Me.lblTotalApportionmentPayment.Text = String.Empty
        '    Me.lblAsApportioned.Text = String.Empty
        '    '  Me.btnSaveAll.Visible = False
        'End If

        Dim sum As Decimal = 0
        Dim amount As String = String.Empty
        For i = 0 To (grdApportionPayments.Rows.Count) - 1
            amount = grdApportionPayments.Rows(i).Cells(7).Text
            sum = sum + Convert.ToDecimal(Decimal.Parse(amount.Substring(1)))
        Next
        Me.lblTotalApportionmentPayment.Text = sum
        Me.lblAsApportioned.Text = sum

        '   BindPendingPaymentsGrids()

        'Dim sqlGetCharges As String = "SELECT SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS CHARGES " & _
        '                                   " FROM genii_user.CASHIER_TRANSACTIONS " & _
        '                             " INNER JOIN genii_user.CASHIER_APPORTION  " & _
        '                             "   ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID  " & _
        '                             " INNER JOIN genii_user.LEVY_AUTHORITY  " & _
        '                             "   ON genii_user.CASHIER_APPORTION.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID  " & _
        '                             " INNER JOIN genii_user.LEVY_TAX_TYPES  " & _
        '                             "  ON genii_user.CASHIER_APPORTION.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID  " & _
        '                             "        WHERE genii_user.CASHIER_TRANSACTIONS.SESSION_ID = ?  " & _
        '                             "       and genii_user.CASHIER_APPORTION.TaxChargeCodeID  IN (99940,99930) "

        'Using adt As New OleDbDataAdapter(sqlGetCharges, Me.ConnectString)
        '    adt.SelectCommand.Parameters.AddWithValue("@SESSION_ID", Me.SessionRecordID)

        '    Dim dt As New DataTable()

        '    adt.Fill(dt)
        '    Dim amount As Decimal = dt.Rows(0)("CHARGES")
        '    Dim amountString As String = Convert.ToString(amount)
        '    Me.lblCharges.Text = "$" & _ amountString

        'End Using


        'Dim SQL As String = String.Format("SELECT SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS CHARGES " & _
        '                                   " FROM genii_user.CASHIER_TRANSACTIONS " & _
        '                             " INNER JOIN genii_user.CASHIER_APPORTION  " & _
        '                             "   ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID  " & _
        '                             " INNER JOIN genii_user.LEVY_AUTHORITY  " & _
        '                             "   ON genii_user.CASHIER_APPORTION.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID  " & _
        '                             " INNER JOIN genii_user.LEVY_TAX_TYPES  " & _
        '                             "  ON genii_user.CASHIER_APPORTION.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID  " & _
        '                             "        WHERE genii_user.CASHIER_TRANSACTIONS.SESSION_ID = {0}  " & _
        '                             "       and genii_user.CASHIER_APPORTION.TaxChargeCodeID  IN (99940,99930) ", Me.SessionRecordID)

        'Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
        '    Dim tblCharges As New DataTable()
        '    Dim chargeAmount As Decimal
        '    adt.Fill(tblCharges)

        '    If tblCharges.Rows.Count > 0 Then
        '        If (Not IsDBNull(tblCharges(0)("CHARGES"))) Then
        '            chargeAmount = Convert.ToDecimal(tblCharges(0)("CHARGES"))
        '        End If

        '    Else
        '        chargeAmount = 0
        '    End If

        '    Me.lblCharges.Text = chargeAmount.ToString()
        'End Using

    End Sub


    ''' <summary>
    ''' Loads system parameter values from tblTaxSystemParameters.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub LoadParameters()
        Using conn As New OleDbConnection(Me.ConnectString)
            ' Minimum Amount to Refund
            Dim cmd As New OleDbCommand("SELECT FieldData FROM genii_user.tblTaxSystemParameters WHERE FieldName = 'MinimumAmountToRefund'", conn)

            conn.Open()

            Dim minimumRefundAmount As Object = cmd.ExecuteScalar()

            If IsNumeric(minimumRefundAmount) Then
                Me.hdnMinimumRefundAmount.Value = CDec(minimumRefundAmount)
            Else
                Me.hdnMinimumRefundAmount.Value = Short.MaxValue
            End If
        End Using
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

        Me.btnAccountStatusLight.Enabled = False
        Me.btnRollStatusLight.Enabled = False
        Me.btnSuspendLight.Enabled = False
        Me.btnBoardOrderLight.Enabled = False
        Me.btnBankruptcyLight.Enabled = False
        Me.btnAlertLight.Enabled = False
        Me.btnCPLight.Enabled = False
        Me.btnConfLight.Enabled = False
        Me.btnRetMailLight.Enabled = False

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
        Dim sessID As Integer
        Dim sqlGetSession As String = "SELECT * FROM genii_user.CASHIER_SESSION WHERE CASHIER = ? AND END_TIME IS NULL ORDER BY START_TIME DESC"

        Using adt As New OleDbDataAdapter(sqlGetSession, Me.ConnectString)
            adt.SelectCommand.Parameters.AddWithValue("@CASHIER", userName)

            Dim dt As New DataTable()

            adt.Fill(dt)

            If dt.Rows.Count = 0 Then
                StartNewSession()
            Else
                SessionRecordID = dt.Rows(0)("RECORD_ID")
                loginTime = dt.Rows(0)("START_TIME")
                startCash = dt.Rows(0)("START_CASH")
                sessID = dt.Rows(0)("RECORD_ID")
                ' Header
                Me.lblOperatorName.Text = userName
                Me.lblCurrentDate.Text = Date.Today.ToShortDateString()
                Me.lblLoginTime.Text = loginTime.ToString("g")
                Me.lblStartCash.Text = startCash.ToString("C")
                Me.lblLogoutUsername.Text = (System.Web.HttpContext.Current.User.Identity.Name).Trim()
                lblSessionID.Text = sessID

                ' Pending payments tab
                Me.lblPendCashier.Text = (System.Web.HttpContext.Current.User.Identity.Name).Trim()
                Me.lblPendLogin.Text = loginTime.ToString()
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

        Dim cashTransactions As Object = Me.CashierTransactionsTable().Compute("SUM(PAYMENT_AMT)", "PAYMENT_TYPE = 2")

        If IsNumeric(cashTransactions) Then
            startCash = startCash + CDec(cashTransactions)
        End If

        Me.txtLogoutEndCash.Text = startCash.ToString()
        Me.lblPendCashBoxBalance.Text = startCash.ToString("C")
        ' Me.lblRequiredCash.Text = startCash.ToString("C")

    End Sub


    ''' <summary>
    ''' Prepares login dialog by filling in cash left in register in last session.
    ''' Login dialog is then opened.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub StartNewSession()
        ' Get cash in register.
        Dim userName As String = (System.Web.HttpContext.Current.User.Identity.Name).Trim()

        Using conn As New OleDbConnection(Me.ConnectString)
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


    ''' <summary>
    ''' Creates a new user session by adding a row in CASHIER_SESSION table.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub CreateNewSession()
        Dim userName As String = (System.Web.HttpContext.Current.User.Identity.Name).Trim()
        Dim startCash As Decimal = CDec(Me.txtLoginStartCash.Text)

        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                ' Get new record id.
                Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_SESSION", conn, trans)


                ' Create new record.
                Dim cmdNewRec As New OleDbCommand("INSERT INTO genii_user.CASHIER_SESSION " & _
                                                  "(RECORD_ID, CASHIER, COMPUTER_ID, START_TIME, START_CASH, CREATE_USER, CREATE_DATE,EDIT_USER,EDIT_DATE) " & _
                                                  " VALUES (?,?,?,?,?,?,?,?,?)")

                cmdNewRec.Connection = conn
                cmdNewRec.Transaction = trans

                With cmdNewRec.Parameters
                    .AddWithValue("@RECORD_ID", recordID)
                    .AddWithValue("@CASHIER", userName)
                    .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@START_TIME", Date.Now)
                    .AddWithValue("@START_CASH", startCash)
                    .AddWithValue("@CREATE_USER", userName)
                    .AddWithValue("@CREATE_DATE", Date.Now)
                    .AddWithValue("@EDIT_USER", userName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                End With

                cmdNewRec.ExecuteNonQuery()

                SessionRecordID = recordID

                Me.lblOperatorName.Text = userName
                Me.lblCurrentDate.Text = Date.Today.ToShortDateString()
                Me.lblLoginTime.Text = Date.Now.ToString()
                Me.lblStartCash.Text = startCash.ToString("C")
                Me.lblLogoutUsername.Text = userName
                Me.lblSessionID.Text = SessionRecordID

                trans.Commit()

                conn.Close()
            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
        End Using
    End Sub


    ''' <summary>
    ''' Logs out user by adding END_TIME in the CASHIER_SESSION table.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub DoLogout()
        If (Me.txtLogoutRequiredCash.Text = String.Empty) Then
            Me.txtLogoutRequiredCash.Text = "0.00"
        End If

        Dim endCash As Decimal = Me.txtLogoutEndCash.Text
        Dim requiredCash As Decimal = Me.txtLogoutRequiredCash.Text

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("UPDATE genii_user.CASHIER_SESSION SET END_TIME = ?, END_CASH = ?, REQUIRED_CASH = ?, EDIT_USER=?, EDIT_DATE=? WHERE RECORD_ID = ?")

            cmd.Connection = conn

            cmd.Parameters.AddWithValue("@END_TIME", Date.Now)
            cmd.Parameters.AddWithValue("@END_CASH", endCash)
            cmd.Parameters.AddWithValue("@REQUIRED_CASH", requiredCash)
            cmd.Parameters.AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
            cmd.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
            cmd.Parameters.AddWithValue("@RECORD_ID", Me.SessionRecordID)

            conn.Open()
            cmd.ExecuteNonQuery()
            Me.SessionRecordID = 0

            ' Prompt to start new session?
            StartNewSession()
        End Using
    End Sub


    ''' <summary>
    ''' Gets new value for column from datatable.
    ''' </summary>
    ''' <param name="columnName"></param>
    ''' <param name="table"></param>
    ''' <param name="rowFilter"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetNewID(columnName As String, table As DataTable, Optional rowFilter As String = Nothing) As Integer
        Dim newID As Object = table.Compute(String.Format("MAX({0})", columnName), rowFilter)

        If IsNumeric(newID) Then
            Return CInt(newID) + 1
        Else
            Return 1
        End If
    End Function


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
            connection = New OleDbConnection(Me.ConnectString)
        End If

        Dim cmd As New OleDbCommand(String.Format("SELECT MAX({0}) FROM {1}", columnName, tableName))

        cmd.Connection = connection

        If transaction IsNot Nothing Then
            cmd.Transaction = transaction
        End If

        If (Not connection.State = ConnectionState.Open) Then
            cmd.Connection.Open()
        End If


        Dim newID As Object = cmd.ExecuteScalar()

        If IsNumeric(newID) Then
            Return CInt(newID) + 1
        Else
            Return 1
        End If
    End Function


    Private Sub UpdateRecordIds(table As DataTable, _
                                tableName As String, _
                                columnName As String, _
                                ByVal connection As OleDbConnection, _
                                ByVal transaction As OleDbTransaction)

        Dim recordID As Integer = GetNewID(columnName, tableName, connection, transaction)

        For Each row As DataRow In table.Select(String.Empty, String.Empty, DataViewRowState.Added)
            row(columnName) = recordID
            recordID += 1
        Next
    End Sub


    Private Sub CommitTable(table As DataTable, _
                            tableName As String, _
                            ByVal connection As OleDbConnection, _
                            ByVal transaction As OleDbTransaction)

        Using adt As New OleDbDataAdapter(String.Format("SELECT * FROM {0}", tableName), connection)
            adt.SelectCommand.Transaction = transaction

            Dim bld As New OleDbCommandBuilder(adt)

            If (tableName <> "genii_user.CASHIER_REJECTED_CHECK" And tableName <> "genii_user.TAX_ACCOUNT_CALENDAR") Then
                adt.UpdateCommand = bld.GetUpdateCommand()
                adt.DeleteCommand = bld.GetDeleteCommand()
                adt.UpdateCommand.Transaction = transaction
                adt.DeleteCommand.Transaction = transaction
            End If

            adt.InsertCommand = bld.GetInsertCommand()

            adt.InsertCommand.Transaction = transaction


            adt.Update(table)
        End Using
    End Sub

    Private Sub CommitDataset()
        Using conn As New OleDbConnection(Me.ConnectString)

            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                ' Update record ids of new rows to avoid concurrency issues.

                '  If (rdoAmountOver.SelectedValue = "decline" Or rdoAmountUnder.SelectedValue = "decline") Then
                ' UpdateRecordIds(Me.DeclinedPaymentsTable, "genii_user.CASHIER_REJECTED_CHECK", "RECORD_ID", conn, trans)
                ' CommitTable(Me.DeclinedPaymentsTable, "genii_user.CASHIER_REJECTED_CHECK", conn, trans)
                '   Else
                UpdateRecordIds(Me.CashierTransactionsTable, "genii_user.CASHIER_TRANSACTIONS", "RECORD_ID", conn, trans)
                UpdateRecordIds(Me.ApportionDetailsTable, "genii_user.CASHIER_APPORTION", "RECORD_ID", conn, trans)

                CommitTable(Me.CashierTransactionsTable, "genii_user.CASHIER_TRANSACTIONS", conn, trans)
                CommitTable(Me.ApportionDetailsTable, "genii_user.CASHIER_APPORTION", conn, trans)

                '   End If



                trans.Commit()
            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex

            End Try
        End Using
    End Sub


    ''' <summary>
    ''' Loads session, transactions and apportion tables for current session.
    ''' </summary>
    ''' <param name="reload">
    ''' If true, all tables are reloaded from database.
    ''' </param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Property SessionDataset(Optional reload As Boolean = False) As DataSet
        Get
            If reload OrElse _sessionDataset Is Nothing Then
                _sessionDataset = New DataSet
            End If

            ' Load tables.
            If reload OrElse _sessionDataset.Tables("CASHIER_SESSION") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_SESSION", "SELECT * FROM genii_user.CASHIER_SESSION WHERE RECORD_ID = " & Me.SessionRecordID)
            End If

            If reload OrElse _sessionDataset.Tables("CASHIER_REJECTED_CHECK") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_REJECTED_CHECK", "SELECT * FROM genii_user.CASHIER_REJECTED_CHECK WHERE SESSION_ID = " & Me.SessionRecordID)
            End If

            If reload OrElse _sessionDataset.Tables("CASHIER_TRANSACTIONS") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_TRANSACTIONS", "SELECT * FROM genii_user.CASHIER_TRANSACTIONS WHERE SESSION_ID = " & Me.SessionRecordID)
            End If

            If reload OrElse _sessionDataset.Tables("CASHIER_APPORTION") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_APPORTION", _
                          "SELECT TA.* FROM genii_user.CASHIER_APPORTION TA, genii_user.CASHIER_TRANSACTIONS CT " & _
                          "WHERE TA.TRANS_ID = CT.RECORD_ID AND CT.SESSION_ID = " & Me.SessionRecordID)
            End If

            ' Set relations.
            AddRelation(_sessionDataset, "CASHIER_SESSION", "RECORD_ID", "CASHIER_REJECTED_CHECK", "SESSION_ID")
            AddRelation(_sessionDataset, "CASHIER_SESSION", "RECORD_ID", "CASHIER_TRANSACTIONS", "SESSION_ID")
            AddRelation(_sessionDataset, "CASHIER_TRANSACTIONS", "RECORD_ID", "CASHIER_APPORTION", "TRANS_ID")

            Return _sessionDataset
        End Get

        Set(value As DataSet)
            _sessionDataset = value
        End Set
    End Property


    ''' <summary>
    ''' Adds relation between two tables. Helper function for <see cref="SessionDataset">SessionDataset</see>.
    ''' </summary>
    ''' <param name="container"></param>
    ''' <param name="parentTable"></param>
    ''' <param name="parentColumn"></param>
    ''' <param name="childTable"></param>
    ''' <param name="childColumn"></param>
    ''' <remarks></remarks>
    Private Sub AddRelation(container As DataSet, _
                            parentTable As String, _
                            parentColumn As String, _
                            childTable As String, _
                            childColumn As String)

        Dim relName As String = String.Format("{0}-{1}", parentTable, childTable)

        If Not container.Relations.Contains(relName) Then
            Dim rel As New DataRelation(relName, container.Tables(parentTable).Columns(parentColumn), _
                                        container.Tables(childTable).Columns(childColumn))

            container.Relations.Add(rel)

            rel.ChildKeyConstraint.DeleteRule = Rule.None
            rel.ChildKeyConstraint.UpdateRule = Rule.Cascade
        End If
    End Sub


    ''' <summary>
    ''' Loads table from database into dataset. Helper function for <see cref="SessionDataset">SessionDataset</see>.
    ''' </summary>
    ''' <param name="container"></param>
    ''' <param name="tableName"></param>
    ''' <param name="query"></param>
    ''' <remarks></remarks>
    Private Sub LoadTable(container As DataSet, tableName As String, query As String)
        Using adt As New OleDbDataAdapter(query, Me.ConnectString)
            adt.Fill(container, tableName)
        End Using
    End Sub


    ''' <summary>
    ''' Returns CASHIER_SESSION table with row for current session.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property SessionTable As DataTable
        Get
            Return SessionDataset().Tables("CASHIER_SESSION")
        End Get
    End Property


    ''' <summary>
    ''' Returns CASHIER_SESSION row for current session.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property SessionRow As DataRow
        Get
            If Me.SessionTable.Rows.Count >= 1 Then
                Return Me.SessionTable.Rows(0)
            Else
                Throw New InvalidOperationException("Session not started")
            End If
        End Get
    End Property


    ''' <summary>
    ''' CASHIER_TRANSACTIONS table.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property CashierTransactionsTable() As DataTable
        Get
            Return SessionDataset().Tables("CASHIER_TRANSACTIONS")
        End Get
    End Property


    ''' <summary>
    ''' CASHIER_REJECTED_CHECK table.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property DeclinedPaymentsTable As DataTable
        Get
            Return SessionDataset().Tables("CASHIER_REJECTED_CHECK")
        End Get
    End Property


    ''' <summary>
    ''' Gets or sets current session id.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Property SessionRecordID As Integer
        Get
            Return GetSessionVariable("SessionRecordID")
        End Get

        Set(value As Integer)
            SetSessionVariable("SessionRecordID", value)
            Me.hdnSessionRecordID.Value = value
            Me.SessionDataset = Nothing
        End Set
    End Property


    ''' <summary>
    ''' Gets connection string for NCIS_TREASURY database.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property ConnectString As String
        Get
            Return ConfigurationManager.ConnectionStrings("ConnString").ConnectionString
        End Get
    End Property


    Private Shared ReadOnly Property CurrentUserName As String
        Get
            Dim UserName As String
            Dim SlashPos As Integer

            SlashPos = InStr((System.Web.HttpContext.Current.User.Identity.Name).Trim(), "\")

            If SlashPos > 0 Then
                UserName = Mid((System.Web.HttpContext.Current.User.Identity.Name).Trim(), SlashPos + 1)
            Else
                UserName = (System.Web.HttpContext.Current.User.Identity.Name).Trim()
            End If

            Return UserName
        End Get
    End Property


    ''' <summary>
    ''' Gets or sets TaxRollMasterClass object for tax roll that was last searched for.
    ''' Object is stored in ASP .NET session.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Property TaxRollMaster As TaxRollMasterClass
        Get
            Dim obj As Object = Session(TAX_ROLL_MASTER_SESS_ID)

            If obj Is Nothing OrElse Not TypeOf obj Is TaxRollMasterClass Then
                Dim trm As New TaxRollMasterClass(Me.ConnectString)

                Session.Add(TAX_ROLL_MASTER_SESS_ID, trm)

                Return trm
            Else
                Return DirectCast(obj, TaxRollMasterClass)
            End If
        End Get

        Set(value As TaxRollMasterClass)
            If Session(TAX_ROLL_MASTER_SESS_ID) IsNot Nothing Then
                Session.Remove(TAX_ROLL_MASTER_SESS_ID)
            End If

            If value IsNot Nothing Then
                Session.Add(TAX_ROLL_MASTER_SESS_ID, value)
            End If
        End Set
    End Property


    ''' <summary>
    ''' CASHIER_APPORTION table.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private ReadOnly Property ApportionDetailsTable As DataTable
        Get
            Return SessionDataset.Tables("CASHIER_APPORTION")
        End Get
    End Property


    Private Function GetSessionVariable(variableName As String) As Object
        Return Session(variableName)
    End Function


    Private Sub SetSessionVariable(variableName As String, value As Object)
        Session.Add(variableName, value)
    End Sub


    Private ReadOnly Property PaymentTypeTable As DataTable
        Get
            If _tblPaymentType Is Nothing Then
                _tblPaymentType = New DataTable()

                Using adt As New OleDbDataAdapter("SELECT * FROM genii_user.ST_PAYMENT_INSTRUMENT", Me.ConnectString)
                    adt.Fill(_tblPaymentType)
                End Using
            End If

            Return _tblPaymentType
        End Get
    End Property


    Private Function GetPaymentTypeCode(paymentType As PaymentTypeEnum) As Integer
        Dim paymentDescription As String = String.Empty

        Select Case paymentType
            Case PaymentTypeEnum.Cash
                paymentDescription = "Cash"
            Case PaymentTypeEnum.Check
                paymentDescription = "Check"
            Case PaymentTypeEnum.CreditCard
                paymentDescription = "Credit Card"
            Case PaymentTypeEnum.Creditron
                paymentDescription = "Scanned Payment"
        End Select

        Dim rows As DataRow() = Me.PaymentTypeTable.Select(String.Format("PaymentDescription = '{0}'", paymentDescription), String.Empty)

        If rows.Length >= 1 Then
            Return rows(0)("PaymentTypeCode")
        Else
            Return 0
        End If
    End Function


    Protected Function CanRedeemCP(status As Object) As Boolean
        If IsNumeric(status) Then
            Select Case CInt(status)
                Case 0, 5, 6, 8
                    Return False
                Case 1, 2, 3, 4, 7
                    Return True
                Case Else
                    Return False
            End Select
        Else
            Return False
        End If
    End Function


    Protected Function GetCPStatus(status As Object) As String
        If IsNumeric(status) Then
            Select Case CInt(status)
                Case 0
                    Return "Preparation"
                Case 1
                    Return "Purchased"
                Case 2
                    Return "Assigned to State"
                Case 3
                    Return "Purchased from State"
                Case 4
                    Return "Reassigned"
                Case 5
                    Return "Redeemed"
                Case 6
                    Return "Closed by Deed"
                Case 7
                    Return "Expiring"
                Case 8
                    Return "Expired"
                Case Else
                    Return status.ToString()
            End Select
        Else
            Return status.ToString()
        End If
    End Function


    ''' <summary>
    ''' Gets payment type (cash, check, credit card, etc.).
    ''' </summary>
    ''' <param name="paymentType"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Protected Function GetPaymentType(paymentType As Object) As String
        If IsNumeric(paymentType) Then
            Dim rows As DataRow() = Me.PaymentTypeTable.Select("PaymentTypeCode = " & paymentType.ToString())

            If rows.Length >= 1 Then
                Return rows(0)("PaymentDescription")
            Else
                Return paymentType.ToString()
            End If
        Else
            Return String.Empty
        End If
    End Function


    ''' <summary>
    ''' Gets tax charge code from LEVY_AUTHORITY table.
    ''' </summary>
    ''' <param name="taxChargeCodeID"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Protected Function GetChargeCode(taxChargeCodeID As String) As String
        If _tblTaxAuthority Is Nothing Then
            Using adt As New OleDbDataAdapter("SELECT TaxChargeCodeID, TaxChargeDescription + ' (' + genii_user.LEVY_AUTHORITY.TaxChargeCodeID + ')' AS TaxChargeDescription FROM genii_user.LEVY_AUTHORITY", Me.ConnectString)
                _tblTaxAuthority = New DataTable()
                adt.Fill(_tblTaxAuthority)
            End Using
        End If

        Dim rows As DataRow() = _tblTaxAuthority.Select(String.Format("TaxChargeCodeID = '{0}'", taxChargeCodeID))

        If rows.Length > 0 Then
            Return rows(0)("TaxChargeDescription").ToString()
        Else
            Return taxChargeCodeID
        End If
    End Function


    ''' <summary>
    ''' Gets tax type from LEVY_TAX_TYPES table.
    ''' </summary>
    ''' <param name="taxTypeID"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Protected Function GetTaxType(taxTypeID As String) As String
        If _tblTaxType Is Nothing Then
            Using adt As New OleDbDataAdapter("SELECT TaxTypeID, TaxCodeDescription + ' (' + genii_user.LEVY_TAX_TYPES.TaxTypeID + ')' AS TaxCodeDescription FROM genii_user.LEVY_TAX_TYPES", Me.ConnectString)
                _tblTaxType = New DataTable()
                adt.Fill(_tblTaxType)
            End Using
        End If

        Dim rows As DataRow() = _tblTaxType.Select(String.Format("TaxTypeID = '{0}'", taxTypeID))

        If rows.Length > 0 Then
            Return rows(0)("TaxCodeDescription").ToString()
        Else
            Return taxTypeID
        End If
    End Function


    ''' <summary>
    ''' Returns formatted tax area code.
    ''' </summary>
    ''' <param name="areaCode"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Protected Function GetTaxArea(areaCode As String) As String
        If _tblTaxDistrict Is Nothing Then
            Using adt As New OleDbDataAdapter("SELECT DistrictCode, DistrictName FROM genii_user.LEVY_TAX_DISTRICT", Me.ConnectString)
                _tblTaxDistrict = New DataTable()
                adt.Fill(_tblTaxDistrict)
            End Using
        End If

        If areaCode.Length >= 4 Then
            Dim districtCode As String = areaCode.Substring(0, 2)
            Dim rows As DataRow() = _tblTaxDistrict.Select(String.Format("DistrictCode = '{0}'", districtCode))

            If rows.Length > 0 Then
                Dim area As String = areaCode.Substring(2)
                Return String.Format("{0} ({1})", rows(0)("DistrictName"), area)
            End If
        End If

        Return areaCode
    End Function


    ''' <summary>
    ''' Saves declined payment in database.
    ''' </summary>
    ''' <remarks>CASHIER_REJECTED_CHECK table.</remarks>
    Private Sub SaveDeclinedPayment(declineReason As String)
        ' Add payment information to CASHIER_REJECTED_CHECK table.
        Dim row As DataRow = Me.DeclinedPaymentsTable.NewRow()

        row("SESSION_ID") = Me.SessionRecordID
        row("TAX_YEAR") = Me.ddlTaxYear.Text
        row("TAX_ROLL_NUMBER") = Me.txtTaxRollNumber.Text
        row("PAYMENT_DATE") = Date.Now 'Me.txtPaymentDate.Text
        row("PAYMENT_TYPE") = Me.ddlPaymentType.SelectedValue

        If Me.ddlPaymentType.SelectedValue = 2 Then
            row("CHECK_NUMBER") = Me.txtCheckNumber.Text
        End If

        row("DECLINED_AMT") = Utilities.GetDecimalOrDBNull(Me.txtAmountPaid.Text)
        row("DECLINE_REASON") = Me.txtDeclineReason.Text 'declineReason
        row("PAYOR_NAME") = Me.txtPayerName.Text
        row("BARCODE") = Me.txtBarcode.Text
        row("EDIT_USER") = TaxPayments.CurrentUserName
        row("EDIT_DATE") = Date.Now
        row("CREATE_USER") = TaxPayments.CurrentUserName
        row("CREATE_DATE") = Date.Now

        Me.DeclinedPaymentsTable.Rows.Add(row)

        '   CommitDataset()

        Using conn As New OleDbConnection(Me.ConnectString)

            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                ' Update record ids of new rows to avoid concurrency issues.

                '  If (rdoAmountOver.SelectedValue = "decline" Or rdoAmountUnder.SelectedValue = "decline") Then
                UpdateRecordIds(Me.DeclinedPaymentsTable, "genii_user.CASHIER_REJECTED_CHECK", "RECORD_ID", conn, trans)
                CommitTable(Me.DeclinedPaymentsTable, "genii_user.CASHIER_REJECTED_CHECK", conn, trans)
                '   Else

                '   End If
                trans.Commit()
            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex

            End Try
        End Using


    End Sub
    Private Sub SaveAcceptedPaymentInvestorState(taxAmount As Decimal, kittyAmount As Decimal, refundAmount As Decimal, txRollNumber As String, txYear As String, grpKey As Integer, applyTo As Integer, refundTag As Integer)
        ' Add payment information to CASHIER_TRANSACTION table.

        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)
            '       Dim trans2 As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)
            Try

                Dim cmdNewRecCashierTrans As New OleDbCommand("INSERT INTO genii_user.CASHIER_TRANSACTIONS " & _
                                               "(RECORD_ID,SESSION_ID,GROUP_KEY, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_DATE, " & _
                                               " PAYMENT_TYPE,APPLY_TO, LETTER_TAG, REFUND_TAG, PAYOR_NAME,CHECK_NUMBER, " & _
                                               " PAYMENT_AMT, TAX_AMT,KITTY_AMT, REFUND_AMT, " & _
                                               " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                               " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecCashierTrans.Connection = conn
                cmdNewRecCashierTrans.Transaction = trans

                Dim recordIDCashierTrans As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_TRANSACTIONS", conn, trans)
                ' taxrollnumber = row2("TaxRollNumber")
                ' Dim isApportioned As String = 1

                With cmdNewRecCashierTrans.Parameters
                    .AddWithValue("@RECORD_ID", recordIDCashierTrans)
                    .AddWithValue("@SESSION_ID", Me.lblSessionID.Text)
                    .AddWithValue("@GROUP_KEY", grpKey)
                    .AddWithValue("@TAX_YEAR", txYear)
                    .AddWithValue("@TAX_ROLL_NUMBER", txRollNumber)
                    .AddWithValue("@PAYMENT_DATE", Date.Now)
                    .AddWithValue("@PAYMENT_TYPE", Me.ddlPaymentType.SelectedValue)
                    .AddWithValue("@APPLY_TO", applyTo)
                    .AddWithValue("@LETTER_TAG", 0)
                    .AddWithValue("@REFUND_TAG", refundTag)
                    .AddWithValue("@PAYOR_NAME", Me.txtPayerName.Text)
                    .AddWithValue("@CHECK_NUMBER", Me.txtCheckNumber.Text)
                    .AddWithValue("@PAYMENT_AMT", taxAmount)
                    .AddWithValue("@TAX_AMT", taxAmount)
                    .AddWithValue("@KITTY_AMT", kittyAmount)
                    .AddWithValue("@REFUND_AMT", refundAmount)

                    .AddWithValue("@EDIT_USER", Me.lblOperatorName.Text)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", Me.lblOperatorName.Text)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCashierTrans.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using
        'Dim row As DataRow = Me.CashierTransactionsTable.NewRow()

        'row("SESSION_ID") = Me.SessionRecordID
        'row("GROUP_KEY") = grpKey
        ''  row("TRANSACTION_STATUS") = 1
        'row("TAX_YEAR") = txYear
        'row("TAX_ROLL_NUMBER") = txRollNumber
        'row("PAYMENT_DATE") = Date.Now 'Me.txtPaymentDate.Text
        'row("PAYMENT_TYPE") = Me.ddlPaymentType.SelectedValue
        'row("APPLY_TO") = applyTo
        'row("LETTER_TAG") = 0
        'row("REFUND_TAG") = refundTag
        'row("PAYOR_NAME") = Me.txtPayerName.Text

        '' If Me.ddlPaymentType.SelectedValue = 2 Then
        'row("CHECK_NUMBER") = Me.txtCheckNumber.Text
        ''  End If
        'row("BARCODE") = Me.txtBarcode.Text
        'row("PAYMENT_AMT") = taxAmount 'Utilities.GetDecimalOrDBNull(Me.txtAmountPaid.Text)
        'row("TAX_AMT") = taxAmount
        'row("KITTY_AMT") = kittyAmount
        'row("REFUND_AMT") = refundAmount
        'row("EDIT_USER") = TaxPayments.CurrentUserName
        'row("EDIT_DATE") = Date.Now
        'row("CREATE_USER") = TaxPayments.CurrentUserName
        'row("CREATE_DATE") = Date.Now

        'Me.CashierTransactionsTable.Rows.Add(row)

        'CommitDataset()
    End Sub


    ''' <summary>
    ''' Saves accepted payment in database.
    ''' </summary>
    ''' <param name="taxAmount"></param>
    ''' <param name="kittyAmount"></param>
    ''' <param name="refundAmount"></param>
    ''' <remarks>CASHIER_TRANSACTIONS table.</remarks>
    Private Sub SaveAcceptedPayment(taxAmount As Decimal, kittyAmount As Decimal, refundAmount As Decimal, txRollNumber As String, txYear As String, grpKey As Integer, applyTo As Integer)
        ' Add payment information to CASHIER_TRANSACTION table.


        Dim row As DataRow = Me.CashierTransactionsTable.NewRow()

        row("SESSION_ID") = Me.SessionRecordID
        row("GROUP_KEY") = grpKey
        '  row("TRANSACTION_STATUS") = 1
        row("TAX_YEAR") = txYear
        row("TAX_ROLL_NUMBER") = txRollNumber
        row("PAYMENT_DATE") = Date.Now 'Me.txtPaymentDate.Text
        row("PAYMENT_TYPE") = Me.ddlPaymentType.SelectedValue
        row("APPLY_TO") = applyTo
        row("LETTER_TAG") = 0
        row("REFUND_TAG") = 1
        row("PAYOR_NAME") = Me.txtPayerName.Text

        ' If Me.ddlPaymentType.SelectedValue = 2 Then
        row("CHECK_NUMBER") = Me.txtCheckNumber.Text
        '  End If
        row("BARCODE") = Me.txtBarcode.Text
        row("PAYMENT_AMT") = taxAmount + refundAmount  'Utilities.GetDecimalOrDBNull(Me.txtAmountPaid.Text)
        row("TAX_AMT") = taxAmount
        row("KITTY_AMT") = kittyAmount
        row("REFUND_AMT") = refundAmount
        row("EDIT_USER") = TaxPayments.CurrentUserName
        row("EDIT_DATE") = Date.Now
        row("CREATE_USER") = TaxPayments.CurrentUserName
        row("CREATE_DATE") = Date.Now

        Me.CashierTransactionsTable.Rows.Add(row)

        CommitDataset()
    End Sub

    Private Sub SaveAcceptedPaymentWriteOff(taxAmount As Decimal, kittyAmount As Decimal, refundAmount As Decimal, txRollNumber As String, txYear As String, grpKey As Integer, applyTo As Integer)
        ' Add payment information to CASHIER_TRANSACTION table.
        Dim WithKittyAmount As Boolean
        Dim totKittyAmount As Decimal
        Dim SQL As String = String.Format("select sum(kitty_amt) as totKittyAmt from genii_user.cashier_transactions", kittyAmount)

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblKittyAmt As New DataTable()

            adt.Fill(tblKittyAmt)

            If tblKittyAmt.Rows.Count > 0 Then
                If (Not IsDBNull(tblKittyAmt.Rows(0)("totKittyAmt"))) Then
                    totKittyAmount = Convert.ToDecimal(tblKittyAmt.Rows(0)("totKittyAmt"))
                    WithKittyAmount = True
                Else
                    totKittyAmount = 0.0
                    WithKittyAmount = False
                End If
            End If
        End Using

        'If (TotalKittyAmount > 0 And TotalKittyAmount >= kittyAmount) Then
        '    kittyAmount = kittyAmount * -1
        'ElseIf (TotalKittyAmount < kittyAmount) Then
        '    kittyAmount = kittyAmount
        'End If

        'If (WithKittyAmount = True) Then
        '    Using conn As New OleDbConnection(Me.ConnectString)
        '        conn.Open()

        '        Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

        '        Try
        '            Dim cmdUpdateTRCharges As New OleDbCommand("UPDATE genii_user.cashier_transactions " & _
        '                                                             " SET kitty_amt = ?, EDIT_USER=?, EDIT_DATE=? " & _
        '                                                             " WHERE TAX_YEAR=? AND TAX_ROLL_NUMBER=? and session_id= ? and kitty_amt >= ?")

        '            cmdUpdateTRCharges.Connection = conn
        '            cmdUpdateTRCharges.Transaction = trans

        '            With cmdUpdateTRCharges.Parameters

        '                .AddWithValue("@kitty_amt", kittyAmount * (-1))
        '                .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
        '                .AddWithValue("@EDIT_DATE", Date.Now)
        '                .AddWithValue("@TaxYear", txYear) 'currentTaxYear)
        '                .AddWithValue("@TaxRollNumber", txRollNumber)
        '                .AddWithValue("@session_id", lblSessionID.Text)
        '                .AddWithValue("@kitty_amt", kittyAmount)

        '            End With

        '            cmdUpdateTRCharges.ExecuteNonQuery()
        '            trans.Commit()

        '        Catch ex As Exception
        '            trans.Rollback()
        '            ' Response.Redirect("ErrorPage.aspx")
        '            Throw ex
        '        End Try
        '        conn.Close()
        '    End Using
        'End If

        Dim computedKitty As Decimal
        If (WithKittyAmount = True And totKittyAmount >= kittyAmount) Then
            computedKitty = kittyAmount * -1
        Else
            computedKitty = 0.0
        End If

        Dim row As DataRow = Me.CashierTransactionsTable.NewRow()

        row("SESSION_ID") = Me.SessionRecordID
        row("GROUP_KEY") = grpKey
        '  row("TRANSACTION_STATUS") = 1
        row("TAX_YEAR") = txYear
        row("TAX_ROLL_NUMBER") = txRollNumber
        row("PAYMENT_DATE") = Date.Now 'Me.txtPaymentDate.Text
        row("PAYMENT_TYPE") = Me.ddlPaymentType.SelectedValue
        row("APPLY_TO") = applyTo
        row("LETTER_TAG") = 0
        row("REFUND_TAG") = 1
        row("PAYOR_NAME") = Me.txtPayerName.Text

        ' If Me.ddlPaymentType.SelectedValue = 2 Then
        row("CHECK_NUMBER") = Me.txtCheckNumber.Text
        '  End If
        row("BARCODE") = Me.txtBarcode.Text
        row("PAYMENT_AMT") = taxAmount  'Utilities.GetDecimalOrDBNull(Me.txtAmountPaid.Text)
        row("TAX_AMT") = taxAmount + kittyAmount
        row("KITTY_AMT") = computedKitty
        row("REFUND_AMT") = refundAmount
        row("EDIT_USER") = TaxPayments.CurrentUserName
        row("EDIT_DATE") = Date.Now
        row("CREATE_USER") = TaxPayments.CurrentUserName
        row("CREATE_DATE") = Date.Now

        Me.CashierTransactionsTable.Rows.Add(row)

        CommitDataset()

    End Sub

    Private Sub SaveAcceptedPaymentState(taxAmount As Decimal, kittyAmount As Decimal, refundAmount As Decimal, txRollNumber As String, txYear As String, grpKey As Integer, applyTo As Integer)
        ' Add payment information to CASHIER_TRANSACTION table.


        Dim row As DataRow = Me.CashierTransactionsTable.NewRow()

        row("SESSION_ID") = Me.SessionRecordID
        row("GROUP_KEY") = grpKey
        '  row("TRANSACTION_STATUS") = 1
        row("TAX_YEAR") = txYear
        row("TAX_ROLL_NUMBER") = txRollNumber
        row("PAYMENT_DATE") = Date.Now 'Me.txtPaymentDate.Text
        row("PAYMENT_TYPE") = Me.ddlPaymentType.SelectedValue
        row("APPLY_TO") = applyTo
        row("LETTER_TAG") = 0
        row("REFUND_TAG") = 0
        row("PAYOR_NAME") = Me.txtPayerName.Text

        ' If Me.ddlPaymentType.SelectedValue = 2 Then
        row("CHECK_NUMBER") = Me.txtCheckNumber.Text
        '  End If
        row("BARCODE") = Me.txtBarcode.Text
        row("PAYMENT_AMT") = taxAmount 'Utilities.GetDecimalOrDBNull(Me.txtAmountPaid.Text)
        row("TAX_AMT") = taxAmount
        row("KITTY_AMT") = kittyAmount
        row("REFUND_AMT") = refundAmount
        row("EDIT_USER") = TaxPayments.CurrentUserName
        row("EDIT_DATE") = Date.Now
        row("CREATE_USER") = TaxPayments.CurrentUserName
        row("CREATE_DATE") = Date.Now

        Me.CashierTransactionsTable.Rows.Add(row)

        CommitDataset()
    End Sub

    ''' <summary>
    ''' Calculates apportion detail for every payment
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub CalculateApportionmentsOnSavePayment(taxRollNumber As String, taxYear As String, paymentAmount As Decimal)


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            ' Call GetApportionment SQL function for each payment.
            Dim paymentDate As Date 'check payment... how to .. if payment payed or payment due  ''''paymentAmount As Decimal,
            ' taxYear As Integer, taxRollNumber As String, 

            Me.ApportionDetailsTable.Clear()

            For Each payRow As DataRow In Me.CashierTransactionsTable.Select("TAX_YEAR=" + _priorMonthTaxYear + " AND TAX_ROLL_NUMBER = " + _priorMonthTaxRoll + " AND TRANSACTION_STATUS IS NULL ") 'MTA 04052013 change sql; add " and roll id, tax year, payment date, payment amount = ?,?,?,?"  AND TRANSACTION_STATUS IS NULL OR TRANSACTION_STATUS NOT IN (1,2,3,4)
                'For Each payRow As DataRow In Me.CashierTransactionsTable.Select("IS_APPORTIONED IS NULL OR IS_APPORTIONED <> 1 AND TAX_ROLL_NUMBER=" & _ +" AND TAX_YEAR=" & _ +" AND ")
                taxYear = payRow("TAX_YEAR") ' MTA change this to current tax year
                taxRollNumber = payRow("TAX_ROLL_NUMBER") ' MTA change this to current tax roll number
                'paymentAmount = payRow("TAX_AMT") ' MTA change this to current tax amount
                paymentDate = payRow("PAYMENT_DATE") ' MTA change this to current date


                Dim cmd As New OleDbCommand("SELECT * FROM dbo.GetApportionment(?,?,?,?)", conn)

                cmd.Parameters.AddWithValue("@TaxYear", taxYear)
                cmd.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber)
                cmd.Parameters.AddWithValue("@PaymentAmount", paymentAmount)
                cmd.Parameters.AddWithValue("@PaymentDate", paymentDate)

                Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                While rdr.Read()
                    Dim row As DataRow = Me.ApportionDetailsTable.NewRow()

                    'row("RECORD_ID") = GetNewID("RECORD_ID", Me.ApportionDetailsTable)
                    row("TRANS_ID") = payRow("RECORD_ID")
                    row("TaxYear") = rdr.Item("TaxYear")
                    row("TaxRollNumber") = rdr.Item("TaxRollNumber")
                    row("AreaCode") = rdr.Item("AreaCode")
                    row("TaxChargeCodeID") = rdr.Item("TaxChargeCodeID")
                    row("TaxTypeID") = rdr.Item("TaxTypeID")
                    row("PaymentDate") = rdr.Item("PaymentDate")
                    row("GLAccount") = rdr.Item("GLAccount")
                    row("SentToOtherSystem") = rdr.Item("SentToOtherSystem")
                    row("ReceiptNumber") = rdr.Item("ReceiptNumber")
                    row("DateApportioned") = rdr.Item("DateApportioned")
                    row("DollarAmount") = rdr.Item("DollarAmount")
                    row("EDIT_USER") = Me.SessionRow("CASHIER").ToString()
                    row("EDIT_DATE") = Date.Now
                    row("CREATE_USER") = Me.SessionRow("CASHIER").ToString()
                    row("CREATE_DATE") = Date.Now

                    Me.ApportionDetailsTable.Rows.Add(row)
                End While

                payRow("TRANSACTION_STATUS") = 1
            Next
        End Using

        CommitDataset()
        BindPendingPaymentsGrids()
        BindApportionsGrid()
    End Sub

    ''' <summary>
    ''' Calculates apportion detail for every non-apportioned session transaction.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub CalculateApportionments()
        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            ' Call GetApportionment SQL function for each payment.
            Dim taxYear As Integer, taxRollNumber As String, paymentAmount As Decimal, paymentDate As Date

            Me.ApportionDetailsTable.Clear()

            For Each payRow As DataRow In Me.CashierTransactionsTable.Select("AND TRANSACTION_STATUS IS NULL OR TRANSACTION_STATUS NOT IN (1,2,3,4)") 'MTA 04052013 change sql; add " and roll id, tax year, payment date, payment amount = ?,?,?,?"
                'For Each payRow As DataRow In Me.CashierTransactionsTable.Select("IS_APPORTIONED IS NULL OR IS_APPORTIONED <> 1 AND TAX_ROLL_NUMBER=" & _ +" AND TAX_YEAR=" & _ +" AND ")
                taxYear = payRow("TAX_YEAR") ' MTA change this to current tax year
                taxRollNumber = payRow("TAX_ROLL_NUMBER") ' MTA change this to current tax roll number
                paymentAmount = payRow("TAX_AMT") ' MTA change this to current tax amount
                paymentDate = payRow("PAYMENT_DATE") ' MTA change this to current date

                Dim cmd As New OleDbCommand("SELECT * FROM dbo.GetApportionment(?,?,?,?)", conn)

                cmd.Parameters.AddWithValue("@TaxYear", taxYear)
                cmd.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber)
                cmd.Parameters.AddWithValue("@PaymentAmount", paymentAmount)
                cmd.Parameters.AddWithValue("@PaymentDate", paymentDate)

                Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                While rdr.Read()
                    Dim row As DataRow = Me.ApportionDetailsTable.NewRow()

                    row("RECORD_ID") = GetNewID("RECORD_ID", Me.ApportionDetailsTable)
                    row("TRANS_ID") = payRow("RECORD_ID")
                    row("TaxYear") = rdr.Item("TaxYear")
                    row("TaxRollNumber") = rdr.Item("TaxRollNumber")
                    row("AreaCode") = rdr.Item("AreaCode")
                    row("TaxChargeCodeID") = rdr.Item("TaxChargeCodeID")
                    row("TaxTypeID") = rdr.Item("TaxTypeID")
                    row("PaymentDate") = rdr.Item("PaymentDate")
                    row("GLAccount") = rdr.Item("GLAccount")
                    row("SentToOtherSystem") = rdr.Item("SentToOtherSystem")
                    row("ReceiptNumber") = rdr.Item("ReceiptNumber")
                    row("DateApportioned") = rdr.Item("DateApportioned")
                    row("DollarAmount") = rdr.Item("DollarAmount")
                    row("EDIT_USER") = Me.SessionRow("CASHIER").ToString()
                    row("EDIT_DATE") = Date.Now
                    row("CREATE_USER") = Me.SessionRow("CASHIER").ToString()
                    row("CREATE_DATE") = Date.Now

                    Me.ApportionDetailsTable.Rows.Add(row)
                End While

                payRow("TRANSACTION_STATUS") = 1
            Next
        End Using

        CommitDataset()
        BindPendingPaymentsGrids()
        BindApportionsGrid()
    End Sub


    Protected Sub btnClearPendingPayments_Click(sender As Object, e As System.EventArgs) Handles btnClearPendingPayments.Click
        ' ClearPendingPayments()
    End Sub


    Protected Sub btnApportionment_Click(sender As Object, e As System.EventArgs) Handles btnCreateApportionment.Click
        CalculateApportionments()
        ' CalculateApportionmentsOnSavePayment(Me.ddlTaxYear.SelectedValue, Me.txtTaxRollNumber.Text)
    End Sub


    <Obsolete("Is this used?", False)> _
    Private Sub ClearPaymentTable(tableName As String, connection As OleDbConnection, transaction As OleDbTransaction)
        Dim cmd As New OleDbCommand(String.Format("DELETE FROM {0} WHERE SESSION_ID = ?", tableName), connection)

        If transaction IsNot Nothing Then
            cmd.Transaction = transaction
        End If

        cmd.Parameters.AddWithValue("@SESSION_ID", Me.SessionRecordID)
        cmd.ExecuteNonQuery()
    End Sub


    <Obsolete("Is this used?", True)> _
    Private Sub ClearPendingPayments()
        ' Delete from database.
        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction

            Try
                ClearPaymentTable("genii_user.CASHIER_TRANSACTIONS", conn, trans)
                ClearPaymentTable("genii_user.PAYMENT_ADJUST", conn, trans)
                ClearPaymentTable("genii_user.CASHIER_REJECTED_CHECK", conn, trans)
                ClearPaymentTable("genii_user.PAYMENT_REFUND", conn, trans)

                trans.Commit()
            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
        End Using

        ' Clear variables.
        Me.SessionDataset = Nothing

        ' Bind grid.
        BindPendingPaymentsGrids()
    End Sub



#Region "Letter Queuer in Payments Tab"
    'Private Sub ShowLetterQueuer(balance As Decimal)
    '    Dim showQueuePanel As Boolean = False
    '    Dim conn As New OleDbConnection(Me.ConnectString)

    '    conn.Open()

    '    Try
    '        ' Letter 1 - Payment Accepted - Outstanding CP
    '        ' Check previous years on parcel for CPs.
    '        Dim cmd As New OleDbCommand()

    '        cmd.Connection = conn
    '        cmd.CommandText = "SELECT count(*) FROM vTaxCPsIssued WHERE APN = ? AND CP_STATUS IN (1,2,3,4,7)"
    '        cmd.Parameters.AddWithValue("@APN", Me.TaxRollMaster.APN)

    '        If CInt(cmd.ExecuteScalar()) > 0 Then
    '            ' Parcel has CPs.
    '            showQueuePanel = True
    '            Me.chkQueueLetter1.Visible = True
    '            Me.chkQueueLetter1.Checked = True
    '        Else
    '            Me.chkQueueLetter1.Visible = False
    '        End If
    '    Finally
    '        If conn IsNot Nothing AndAlso conn.State = ConnectionState.Open Then
    '            conn.Close()
    '        End If
    '    End Try

    '    ' Balance is remaining.
    '    Me.chkQueueLetter2.Visible = False
    '    Me.chkQueueLetter3.Visible = False

    '    If balance > 0 Then
    '        Dim latePayment As Boolean = False

    '        ' Payment is late if delinquent fee is due.
    '        For Each row As DataRow In Me.TaxRollMaster.TaxCalculationTable.Rows
    '            If Me.TaxRollMaster.IsDelinquentFee(row("TaxTypeID")) Then
    '                latePayment = True
    '            End If
    '        Next

    '        If latePayment Then
    '            ' Letter 3 - Payment Late - Outstanding Balance
    '            ' Payment date is after due data. Balance is remaining.
    '            showQueuePanel = True
    '            Me.chkQueueLetter2.Visible = True
    '            Me.chkQueueLetter2.Checked = True
    '        Else
    '            ' Letter 2 - Payment Early - Outstanding Balance
    '            ' Payment date is before due date. Balance is remaining.
    '            showQueuePanel = True
    '            Me.chkQueueLetter3.Visible = True
    '            Me.chkQueueLetter3.Checked = True
    '        End If
    '    End If

    '    ' Letter 4 - CP Redeemed - Letter to Investor
    '    'TODO: Not yet implemented.
    '    Me.chkQueueLetter4.Visible = False

    '    ' Show queue panel if one or more letters have to be queued.
    '    Me.pnlLetterQueuer.Visible = showQueuePanel
    'End Sub


    'Protected Sub btnQueueLetters_Click(sender As Object, e As System.EventArgs) Handles btnQueueLetters.Click
    '    Dim paymentAmount As Decimal = Utilities.GetDecimalOrZero(Me.txtAmountPaid.Text)
    '    Dim taxAmount As Decimal = Utilities.GetDecimalOrZero(Me.txtGrandTotal.Text)
    '    Dim balanceDue As Decimal = taxAmount - paymentAmount
    '    Dim firstHalfDue As Date, secondHalfDue As Date, payBothHalves As Boolean

    '    Me.TaxRollMaster.GetDelinquentDates(firstHalfDue, secondHalfDue)
    '    payBothHalves = Me.chkPayBothHalves.Checked

    '    If Me.chkQueueLetter1.Visible And Me.chkQueueLetter1.Checked Then
    '        ' Letter 1 - Payment Accepted - Outstanding CP
    '        Using conn As New OleDbConnection(Me.ConnectString)
    '            ' Get CP purchase price.
    '            conn.Open()

    '            Dim cmd As New OleDbCommand()

    '            cmd.Connection = conn
    '            cmd.CommandText = "SELECT SUM(PurchaseValue) FROM vTaxCPsIssued WHERE APN = ?"
    '            cmd.Parameters.AddWithValue("@APN", Me.TaxRollMaster.APN)

    '            Dim objCPValue As Object = cmd.ExecuteScalar()

    '            If IsNumeric(objCPValue) Then
    '                Dim purchaseValue As Decimal = CDec(objCPValue)

    '                QueueLetter(Me.TaxRollMaster.TaxYear, Me.TaxRollMaster.TaxRollNumber, 1, _
    '                            paymentAmount, purchaseValue, purchaseValue, 0, secondHalfDue, secondHalfDue)
    '            End If
    '        End Using
    '    End If

    '    If Me.chkQueueLetter2.Visible And Me.chkQueueLetter2.Checked Then
    '        ' Letter 2 - Payment Early - Outstanding Balance
    '        QueueLetter(Me.TaxRollMaster.TaxYear, Me.TaxRollMaster.TaxRollNumber, 2, _
    '                    paymentAmount, balanceDue, balanceDue, 0, secondHalfDue, secondHalfDue)
    '    End If

    '    If Me.chkQueueLetter3.Visible And Me.chkQueueLetter3.Checked Then
    '        ' Letter 3 - Payment Late - Outstanding Balance
    '        QueueLetter(Me.TaxRollMaster.TaxYear, Me.TaxRollMaster.TaxRollNumber, 3, _
    '                    paymentAmount, balanceDue, balanceDue, 0, secondHalfDue, secondHalfDue)
    '    End If

    '    If Me.chkQueueLetter4.Visible And Me.chkQueueLetter4.Checked Then
    '        ' Letter 4 - CP Redeemed - Letter to Investor
    '        'TODO: Not yet implemented.
    '    End If

    '    Me.pnlLetterQueuer.Visible = False
    '    '  Me.BindLettersGrid()
    'End Sub


    'Private Sub QueueLetter(taxYear As Integer, taxRollNumber As Integer, letterType As Integer, _
    '                        amount1 As Decimal, amount2 As Decimal, amount3 As Decimal, amount4 As Decimal, _
    '                        date1 As DateTime, date2 As DateTime)

    '    Dim conn As New OleDbConnection(Me.ConnectString)

    '    conn.Open()

    '    Dim trans As OleDbTransaction = conn.BeginTransaction()

    '    Try
    '        ' Get new record id.
    '        Dim newRecordID As Integer

    '        Using cmdRecordID As New OleDbCommand("SELECT MAX(RECORD_ID) FROM genii_user.CASHIER_LETTERS", conn)
    '            Dim objRecordID As Object

    '            cmdRecordID.Transaction = trans
    '            objRecordID = cmdRecordID.ExecuteScalar()

    '            If (Not IsDBNull(objRecordID)) AndAlso IsNumeric(objRecordID) Then
    '                newRecordID = CInt(objRecordID) + 1
    '            Else
    '                newRecordID = 1
    '            End If
    '        End Using

    '        ' Insert new record.
    '        Using cmd As New OleDbCommand()
    '            cmd.CommandText = "INSERT INTO genii_user.CASHIER_LETTERS (" & _
    '                              "RECORD_ID, CASHIER, TAX_YEAR, TAX_ROLL_NUMBER, LETTER_TYPE, LETTER_DATE, " & _
    '                              "AMOUNT_1, AMOUNT_2, AMOUNT_3, AMOUNT_4, DATE_1, DATE_2, " & _
    '                              "CREATE_USER, CREATE_DATE, EDIT_USER, EDIT_DATE" & _
    '                              ") VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)"

    '            cmd.Connection = conn
    '            cmd.Transaction = trans

    '            cmd.Parameters.AddWithValue("@RECORD_ID", newRecordID)
    '            cmd.Parameters.AddWithValue("@CASHIER", TaxPayments.CurrentUserName)
    '            cmd.Parameters.AddWithValue("@TAX_YEAR", taxYear)
    '            cmd.Parameters.AddWithValue("@TAX_ROLL_NUMBER", taxRollNumber)
    '            cmd.Parameters.AddWithValue("@LETTER_TYPE", letterType)
    '            cmd.Parameters.AddWithValue("@LETTER_DATE", DateTime.Now)
    '            cmd.Parameters.AddWithValue("@AMOUNT_1", amount1)
    '            cmd.Parameters.AddWithValue("@AMOUNT_2", amount2)
    '            cmd.Parameters.AddWithValue("@AMOUNT_3", amount3)
    '            cmd.Parameters.AddWithValue("@AMOUNT_4", amount4)
    '            cmd.Parameters.AddWithValue("@DATE_1", date1)
    '            cmd.Parameters.AddWithValue("@DATE_2", date2)
    '            cmd.Parameters.AddWithValue("@CREATE_USER", TaxPayments.CurrentUserName)
    '            cmd.Parameters.AddWithValue("@CREATE_DATE", DateTime.Now)
    '            cmd.Parameters.AddWithValue("@EDIT_USER", TaxPayments.CurrentUserName)
    '            cmd.Parameters.AddWithValue("@EDIT_DATE", DateTime.Now)

    '            cmd.ExecuteNonQuery()
    '        End Using

    '        trans.Commit()
    '    Catch ex As Exception
    '        trans.Rollback()
    '    Finally
    '        If conn IsNot Nothing AndAlso conn.State = ConnectionState.Open Then
    '            conn.Close()
    '        End If
    '    End Try

    'End Sub
#End Region


    '#Region "Letters Tab"
    '    Private Sub BindLettersGrid()
    '        Dim sql As String = "SELECT RECORD_ID, [DESCRIPTION], LETTERS_COUNT FROM vCashierLetters ORDER BY RECORD_ID"

    '        Using adp As New OleDbDataAdapter(sql, Me.ConnectString)
    '            Dim dt As New DataTable()

    '            adp.Fill(dt)

    '            Me.grdLetters.DataSource = dt
    '            Me.grdLetters.DataBind()
    '        End Using

    '        If grdLetters.Rows.Count > 0 Then
    '            btnLettersPrint.Enabled = True
    '        Else
    '            btnLettersPrint.Enabled = False
    '        End If
    '    End Sub


    '    Private Sub GenerateLetters()
    '        Dim reports As New Generic.Dictionary(Of Integer, Byte())()

    '        ' Generate reports.
    '        For Each row As GridViewRow In Me.grdLetters.Rows
    '            If row.RowType = DataControlRowType.DataRow Then
    '                Dim chkLettersSelect As CheckBox = DirectCast(row.FindControl("chkLettersSelect"), CheckBox)
    '                Dim hdnLetterType As HiddenField = DirectCast(row.FindControl("hdnLetterType"), HiddenField)

    '                If chkLettersSelect.Checked Then
    '                    reports.Add(CInt(hdnLetterType.Value), GenerateLetter(CInt(hdnLetterType.Value)))
    '                End If
    '            End If
    '        Next

    '        ' Output to response.
    '        If reports.Count > 0 Then
    '            With HttpContext.Current.Response
    '                .ClearContent()
    '                .ClearHeaders()
    '                .AddHeader("Content-Disposition", "inline; filename=Reports.zip")
    '                .ContentType = "application/zip"
    '                ExportReports(reports, .OutputStream)
    '                .End()
    '            End With

    '        End If

    '    End Sub


    '    Private Sub ExportReports(reports As Generic.Dictionary(Of Integer, Byte()), output As Stream)
    '        ' Get letter type descriptions.
    '        Dim dt As New DataTable()

    '        Using adp As New OleDbDataAdapter("SELECT RECORD_ID, [DESCRIPTION] FROM genii_user.CASHIER_LETTER_TYPES", Me.ConnectString)
    '            adp.Fill(dt)
    '        End Using

    '        ' Add reports to zip file.
    '        Dim crc As New ICSharpCode.SharpZipLib.Checksums.Crc32
    '        Dim zip As New ZipOutputStream(output)

    '        zip.SetLevel(3)

    '        For Each report As Generic.KeyValuePair(Of Integer, Byte()) In reports
    '            Dim description As String = dt.Select("RECORD_ID = " & report.Key)(0).Item("DESCRIPTION")
    '            Dim entry As New ZipEntry(description & ".pdf")

    '            entry.DateTime = DateTime.Now
    '            entry.Size = report.Value.Length
    '            crc.Reset()
    '            crc.Update(report.Value)
    '            entry.Crc = crc.Value
    '            zip.PutNextEntry(entry)
    '            zip.Write(report.Value, 0, report.Value.Length)
    '            zip.CloseEntry()

    '            UpdateLetterPrinted(report.Key)
    '        Next

    '        zip.Close()
    '    End Sub


    '    Private Function GenerateLetter(letterType As Integer) As Byte()
    '        ' Load report.
    '        Dim rpt As New ReportDocument()
    '        Dim folder As String = Path.Combine(Path.GetDirectoryName(Request.PhysicalPath), "Letters")

    '        Select Case letterType
    '            Case 1
    '                ' Payment Accepted - Outstanding CP
    '                rpt.Load(Path.Combine(folder, "ltrPaymentAcceptedOutstandingCP.rpt"))
    '            Case 2
    '                ' Early Payment - Outstanding Balance
    '                rpt.Load(Path.Combine(folder, "ltrEarlyPaymentOutstandingBalance.rpt"))
    '            Case 3
    '                ' Late Payment - Outstanding Balance
    '                rpt.Load(Path.Combine(folder, "ltrLatePaymentOutstandingBalance.rpt"))
    '            Case 4
    '                ' CP Redeemed - Letter to Investor
    '                rpt.Load(Path.Combine(folder, "ltrCPRedeemedInvestorLetter.rpt"))
    '        End Select

    '        ' Database authentication.
    '        Dim userName As String = (Environment.UserName).Trim()
    '        Dim password As String = util.GetDatabasePassword(Me.ConnectString)

    '        rpt.SetDatabaseLogon(userName, password)

    '        ' Generate report.
    '        Dim str As Stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat)

    '        rpt.Close()

    '        Dim bytes(str.Length - 1) As Byte

    '        str.Read(bytes, 0, bytes.Length)

    '        Return bytes
    '    End Function

    '    Private Sub UpdateLetterPrinted(letterType As Integer)
    '        Using conn As New OleDbConnection(Me.ConnectString)
    '            conn.Open()

    '            ' Prepare where clause.
    '            Dim whereClause As String = String.Empty

    '            Select Case letterType
    '                Case 1
    '                    whereClause = "LETTER_TYPE = 1 AND (PRINTED IS NULL OR PRINTED <> 1)"
    '                Case 2
    '                    whereClause = "LETTER_TYPE = 2 AND (PRINTED IS NULL OR PRINTED <> 1)"
    '                Case 3
    '                    whereClause = "LETTER_TYPE = 3 AND (PRINTED IS NULL OR PRINTED <> 1)"
    '                Case 4
    '                    whereClause = "LETTER_TYPE = 4 AND APPROVED = 1 AND (PRINTED IS NULL OR PRINTED <> 1)"
    '            End Select

    '            ' Create command.
    '            Dim cmd As New OleDbCommand("UPDATE genii_user.CASHIER_LETTERS SET PRINTED = 1, EDIT_USER = ?, EDIT_DATE = ? WHERE " & whereClause, conn)

    '            cmd.Parameters.AddWithValue("@EDIT_USER", TaxPayments.CurrentUserName)
    '            cmd.Parameters.AddWithValue("@EDIT_DATE", DateTime.Now)
    '            cmd.ExecuteNonQuery()

    '            conn.Close()
    '        End Using
    '    End Sub


    '    Protected Sub btnLettersPrint_Click(sender As Object, e As System.EventArgs) Handles btnLettersPrint.Click
    '        GenerateLetters()
    '    End Sub


    '    Protected Sub grdLetters_RowCommand(sender As Object, e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles grdLetters.RowCommand
    '        Select Case e.CommandName
    '            Case "LetterDetail"
    '                Dim letterType As Integer = CInt(e.CommandArgument)
    '                Dim letterTypeDesc As String = String.Empty

    '                Dim sql As String = "SELECT LTR.CASHIER, LTR.TAX_YEAR, LTR.TAX_ROLL_NUMBER, LTR.LETTER_DATE, LTR.APPROVED, ISNULL(TR.MAIL_ADDRESS_1, '') AS MAIL_ADDRESS_1, " & _
    '                                    "ISNULL(TR.OWNER_NAME_3, '') + ' ' + ISNULL(TR.OWNER_NAME_2, '') + ' ' + ISNULL(TR.OWNER_NAME_1, '') AS OWNER_NAME " & _
    '                                    "FROM genii_user.CASHIER_LETTERS AS LTR " & _
    '                                    "INNER JOIN genii_user.TR AS TR ON LTR.TAX_YEAR = TR.TaxYear " & _
    '                                    "AND LTR.TAX_ROLL_NUMBER = TR.TaxRollNumber WHERE LTR.LETTER_TYPE = ?"

    '                Using conn As New OleDbConnection(Me.ConnectString)
    '                    conn.Open()

    '                    ' Bind letter details grid.
    '                    Dim cmdLetters As New OleDbCommand(sql, conn)

    '                    cmdLetters.Parameters.AddWithValue("@LETTER_TYPE", letterType)
    '                    Me.grdLettersDetail.DataSource = cmdLetters.ExecuteReader()
    '                    Me.grdLettersDetail.DataBind()

    '                    ' Get letter type description.
    '                    Dim cmdLetterType As New OleDbCommand("SELECT [DESCRIPTION] FROM genii_user.CASHIER_LETTER_TYPES WHERE RECORD_ID = ?", conn)

    '                    cmdLetterType.Parameters.AddWithValue("@RECORD_ID", letterType)
    '                    letterTypeDesc = cmdLetterType.ExecuteScalar().ToString()
    '                End Using

    '                ClientScript.RegisterStartupScript(Me.GetType(), "LetterDetailsDialog", _
    '                                                   String.Format("openLetterDetailsDialog('{0}');", letterTypeDesc), _
    '                                                   True)
    '        End Select
    '    End Sub

    '#End Region


#Region "Account Remarks Tab"

    ''' <summary>
    ''' btnAddNewAccountRemarks_Click - Click event for btnAddNewAccountRemarks
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Protected Sub btnAddNewAccountRemarks_Click(sender As Object, e As System.EventArgs) Handles btnAddNewAccountRemarks.Click
        Dim fileName As String = String.Empty

        Dim row As DataRow = Me.TaxAccountCalendarTable.NewRow()

        'row.Item("RECORD_ID") = GetNewID("RECORD_ID", "genii_user.TAX_ACCOUNT_CALENDAR")
        row.Item("ParcelOrTaxID") = Me.TaxRollMaster.APN.Replace("-", String.Empty)
        row.Item("REMARKS") = Me.txtRemarkText.Text
        row.Item("IMAGE") = Me.uplRemarkImage.FileBytes
        row.Item("TASK_DATE") = Me.txtRemarkDate.Text
        row.Item("CREATE_USER") = TaxPayments.CurrentUserName
        row.Item("CREATE_DATE") = Date.Now
        row.Item("EDIT_USER") = TaxPayments.CurrentUserName
        row.Item("EDIT_DATE") = Date.Now
        row.Item("FILE_TYPE") = util.GetUploadFileType(Me.uplRemarkImage.FileName)

        Me.TaxAccountCalendarTable.Rows.Add(row)

        ''CommitDataset()
        Using conn As New OleDbConnection(Me.ConnectString)

            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                ' Commit tables.
                CommitTable(Me.TaxAccountCalendarTable, "genii_user.TAX_ACCOUNT_CALENDAR", conn, trans)

                trans.Commit()
            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
        End Using

        Me.AccountRemarksDataset = Nothing
        Me.OtherYearRemarksDataset = Nothing

        BindAccountRemarksGrid()
        '  BindOtherYearsRemarksGrid()
    End Sub

    'Protected Sub btnAddNewTaxRollRemarks_Click(sender As Object, e As System.EventArgs) Handles btnAddNewTaxRollRemarks.Click
    '    Dim row As DataRow = Me.TaxRollCalendarTable.NewRow()
    '    row.Item("RECORD_ID") = GetNewID("RECORD_ID", "genii_user.TR_CALENDAR")
    '    row.Item("TAXYEAR") = Me.TaxRollMaster.TaxYear
    '    row.Item("TaxRollNumber") = Me.TaxRollMaster.TaxRollNumber
    '    row.Item("REMARKS") = Me.txtRemarkText.Text
    '    row.Item("IMAGE") = Me.uplRemarkImage.FileBytes
    '    row.Item("TASK_DATE") = Me.txtRemarkDate.Text
    '    row.Item("TASK_ID") = 0
    '    row.Item("ADMIN_REVIEW") = 0
    '    row.Item("CREATE_USER") = TaxPayments.CurrentUserName
    '    row.Item("CREATE_DATE") = Date.Now
    '    row.Item("EDIT_USER") = TaxPayments.CurrentUserName
    '    row.Item("EDIT_DATE") = Date.Now
    '    row.Item("FILE_TYPE") = util.GetUploadFileType(Me.uplRemarkImage.FileName)

    '    Me.TaxRollCalendarTable.Rows.Add(row)

    '    ''CommitDataset()
    '    Using conn As New OleDbConnection(Me.ConnectString)

    '        conn.Open()

    '        Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

    '        Try
    '            ' Commit tables.
    '            CommitTable(Me.TaxRollCalendarTable, "genii_user.TR_CALENDAR", conn, trans)

    '            trans.Commit()
    '        Catch ex As Exception
    '            trans.Rollback()
    '            Throw ex
    '        End Try
    '    End Using

    '    Me.TaxRollRemarksDataset = Nothing

    '    BindTaxRollRemarksGrid()
    'End Sub

    Private Sub BindAccountRemarksGrid()
        Me.gvAccountRemarks.DataSource = Me.AccountRemarksDataset.Tables(0)
        Me.gvAccountRemarks.DataBind()
    End Sub

    'Private Sub BindTaxRollRemarksGrid()
    '    Me.gvTaxRollRemarks.DataSource = Me.TaxRollRemarksDataset.Tables(0)
    '    Me.gvTaxRollRemarks.DataBind()
    'End Sub

    'Private Sub BindOtherYearsRemarksGrid()
    '    Me.gvOtherYearRemarks.DataSource = Me.OtherYearRemarksDataset.Tables(0)
    '    Me.gvOtherYearRemarks.DataBind()
    'End Sub

#Region "Datasets For Account Remarks Tab"

    Public Property AccountRemarksDataset(Optional reload As Boolean = False) As DataSet
        Get
            If reload OrElse _AccountRemarksDataset Is Nothing Then
                _AccountRemarksDataset = New DataSet
            End If

            '' For Account Remarks
            If reload OrElse _AccountRemarksDataset.Tables("TAX_ACCOUNT_CALENDAR") Is Nothing Then
                Dim accountRemarksSQL As String = String.Format("SELECT * " & _
                                                                "FROM genii_user.TAX_ACCOUNT_CALENDAR " & _
                                                                "WHERE ParcelOrTaxID = '{0}' " & _
                                                                "AND YEAR(TASK_DATE) >= '{1}' ORDER BY TASK_DATE ", _
                                                                Me.TaxRollMaster.APN.Replace("-", String.Empty), (DateTime.Now.Year - 1))

                LoadTable(_AccountRemarksDataset, "TAX_ACCOUNT_CALENDAR", accountRemarksSQL)
            End If

            Return _AccountRemarksDataset
        End Get
        Set(value As DataSet)
            _AccountRemarksDataset = value
        End Set
    End Property

    'Public Property TaxRollRemarksDataset(Optional reload As Boolean = False) As DataSet
    '    Get
    '        If reload OrElse _taxRollRemarksDataset Is Nothing Then
    '            _taxRollRemarksDataset = New DataSet
    '        End If

    '        '' Load Table

    '        '' For Tax Roll Remarks
    '        If reload OrElse _taxRollRemarksDataset.Tables("TR_CALENDAR") Is Nothing Then
    '            LoadTable(_taxRollRemarksDataset, "TR_CALENDAR", "SELECT * FROM genii_user.TR_CALENDAR WHERE TaxRollNumber = " & Me.TaxRollMaster.TaxRollNumber)
    '        End If

    '        Return _taxRollRemarksDataset
    '    End Get
    '    Set(value As DataSet)
    '        _taxRollRemarksDataset = value
    '    End Set
    'End Property

    Public Property OtherYearRemarksDataset(Optional reload As Boolean = False) As DataSet
        Get
            If reload OrElse _otherYearRemarksDataset Is Nothing Then
                _otherYearRemarksDataset = New DataSet
            End If

            '' For Other Year Account Remarks Grid
            If reload OrElse _otherYearRemarksDataset.Tables("TAX_ACCOUNT_CALENDAR") Is Nothing Then
                Dim otherYearRemarksSQL As String = String.Format("SELECT * " & _
                                                                  "FROM genii_user.TAX_ACCOUNT_CALENDAR " & _
                                                                  "WHERE ParcelOrTaxID = '{0}' " & _
                                                                  "AND YEAR(TASK_DATE) < '{1}' ORDER BY TASK_DATE ", _
                                                                  Me.TaxRollMaster.APN.Replace("-", String.Empty), DateTime.Now.Year - 1)

                LoadTable(_otherYearRemarksDataset, "TAX_ACCOUNT_CALENDAR", otherYearRemarksSQL)
            End If

            Return _otherYearRemarksDataset
        End Get
        Set(value As DataSet)
            _otherYearRemarksDataset = value
        End Set
    End Property


    ''' <summary>
    ''' Return TAX_ACCOUNT_CALENDAR table from dataset.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property TaxAccountCalendarTable As DataTable
        Get
            Return Me.AccountRemarksDataset.Tables("TAX_ACCOUNT_CALENDAR")
        End Get
    End Property

    ''' <summary>
    ''' Return TR_CALENDAR table from dataset.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    'Public ReadOnly Property TaxRollCalendarTable As DataTable
    '    Get
    '        Return Me.TaxRollRemarksDataset.Tables("TR_CALENDAR")
    '    End Get
    'End Property

    ''' <summary>
    ''' Return TAX_ACCOUNT_CALENDAR table for prior tax years from dataset.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property OtherYearsTaxAccountCalendarTable As DataTable
        Get
            Return Me.OtherYearRemarksDataset.Tables("TAX_ACCOUNT_CALENDAR")
        End Get
    End Property
#End Region
#End Region


    Public Sub SetAlertMessage()
        Me.btnAlertLight.Enabled = True
        Select Case Me.AccountAlert
            Case 0
                Me.btnAlertLight.Text = "Standard Care"
                ' Me.btnAlertLight.BorderColor = Drawing.Color.Yellow
                '  Me.btnAlertLight.BorderWidth = 4
                ' Me.btnAlertLight.BorderStyle = BorderStyle.Groove
                '  Me.btnAlertLight.ForeColor = Drawing.Color.Black
                Me.btnAlertLight.ForeColor = Drawing.Color.Black
                Me.btnAlertLight.BackColor = Drawing.Color.LightGreen
            Case 1
                Me.btnAlertLight.Text = "Alert"
                ' Me.btnAlertLight.BorderColor = Drawing.Color.Yellow
                '  Me.btnAlertLight.BorderWidth = 4
                ' Me.btnAlertLight.BorderStyle = BorderStyle.Groove
                '  Me.btnAlertLight.ForeColor = Drawing.Color.Black
                Me.btnAlertLight.ForeColor = Drawing.Color.Black
                Me.btnAlertLight.BackColor = Drawing.Color.Red

            Case Else
                Me.btnAlertLight.Text = "Standard Care"
                Me.btnAlertLight.BackColor = Drawing.Color.LightGreen
                Me.btnAlertLight.ForeColor = Drawing.Color.Black

                '  Case 3
                '      Me.btnAlertLight.Text = "Investor Foreclosure"
                '    Me.btnAlertLight.ForeColor = Drawing.Color.Black
                '    Me.btnAlertLight.BackColor = Drawing.Color.Yellow
        End Select
    End Sub

    Public Sub SetSuspendMessage()
        ' lblSuspendMessage.Text = "Suspend Transaction"
        Me.btnSuspendLight.Enabled = True

        Select Case Me.AccountSuspend
            Case 1
                Me.btnSuspendLight.Enabled = True
                Me.btnSuspendLight.Text = "Account Suspended"
                Me.btnSuspendLight.BackColor = Drawing.Color.Red
                Me.btnSuspendLight.ForeColor = Drawing.Color.Black
            Case Else
                Me.btnSuspendLight.Enabled = True
                Me.btnSuspendLight.Text = "Account Active"
                Me.btnSuspendLight.BackColor = Drawing.Color.LightGreen
                Me.btnSuspendLight.ForeColor = Drawing.Color.Black
        End Select

    End Sub
    '()
    Public Sub SetParentBalAlert()
        ' lblSuspendMessage.Text = "Suspend Transaction"
        Me.btnParentBal.Enabled = True

        Select Case Me.AccountParentBal
            Case 1
                Me.btnParentBal.Enabled = True
                Me.btnParentBal.Text = "Parent Parcel Balance"
                Me.btnParentBal.BackColor = Drawing.Color.Red
                Me.btnParentBal.ForeColor = Drawing.Color.Black
            Case Else
                Me.btnParentBal.Enabled = True
                Me.btnParentBal.Text = "Genealogy OK"
                Me.btnParentBal.BackColor = Drawing.Color.LightGreen
                Me.btnParentBal.ForeColor = Drawing.Color.Black
        End Select

    End Sub

    Public Sub SetTRConfidentialMessage()
        ' lblSuspendMessage.Text = "Suspend Transaction"
        Me.btnConfLight.Enabled = True

        Select Case Me.TRConfidential
            Case 1
                Me.btnConfLight.Enabled = True
                Me.btnConfLight.Text = "Confidential"
                Me.btnConfLight.BackColor = Drawing.Color.Red
                Me.btnConfLight.ForeColor = Drawing.Color.Black
            Case Else
                Me.btnConfLight.Enabled = True
                Me.btnConfLight.Text = "Public Information"
                Me.btnConfLight.BackColor = Drawing.Color.LightGreen
                Me.btnConfLight.ForeColor = Drawing.Color.Black
        End Select

    End Sub

    Public Sub SetTRMailReturnedMessage()
        ' lblSuspendMessage.Text = "Suspend Transaction"
        Me.btnRetMailLight.Enabled = True

        Select Case Me.TRMailReturned
            Case 1
                Me.btnRetMailLight.Enabled = True
                Me.btnRetMailLight.Text = "Returned Mail"
                Me.btnRetMailLight.BackColor = Drawing.Color.Red
                Me.btnRetMailLight.ForeColor = Drawing.Color.Black
            Case Else
                Me.btnRetMailLight.Enabled = True
                Me.btnRetMailLight.Text = "No Returned Mail"
                Me.btnRetMailLight.BackColor = Drawing.Color.LightGreen
                Me.btnRetMailLight.ForeColor = Drawing.Color.Black
        End Select

    End Sub

    Public Sub SetStatusMessage()
        Me.btnAccountStatusLight.Enabled = True

        Select Case Me.AccountStatus
            Case 1
                Me.btnAccountStatusLight.Text = "Secured-Active"
                Me.btnAccountStatusLight.BackColor = Drawing.Color.LightGreen
                Me.btnAccountStatusLight.ForeColor = Drawing.Color.Black
            Case 2
                Me.btnAccountStatusLight.Text = "Secured-Merged"
                Me.btnAccountStatusLight.BackColor = Drawing.Color.Yellow
                Me.btnAccountStatusLight.ForeColor = Drawing.Color.Black
            Case 3
                Me.btnAccountStatusLight.Text = "Secured-Split"
                Me.btnAccountStatusLight.BackColor = Drawing.Color.Yellow
                Me.btnAccountStatusLight.ForeColor = Drawing.Color.Black
            Case 4
                Me.btnAccountStatusLight.Text = "Unsecured-Active"
                Me.btnAccountStatusLight.BackColor = Drawing.Color.LightGreen
                Me.btnAccountStatusLight.ForeColor = Drawing.Color.Black
            Case 5
                Me.btnAccountStatusLight.Text = "Unsecured-Closed"
                Me.btnAccountStatusLight.BackColor = Drawing.Color.LightBlue
                Me.btnAccountStatusLight.ForeColor = Drawing.Color.Black
            Case 6
                Me.btnAccountStatusLight.Text = "Unsecured-Abated"
                Me.btnAccountStatusLight.BackColor = Drawing.Color.LightBlue
                Me.btnAccountStatusLight.ForeColor = Drawing.Color.Black
            Case 7
                Me.btnAccountStatusLight.Text = "Unsecured-Sheriff"
                Me.btnAccountStatusLight.BackColor = Drawing.Color.Red
                Me.btnAccountStatusLight.ForeColor = Drawing.Color.Black
            Case Else
                btnAccountStatusLight.Enabled = False
        End Select
    End Sub

    Public Sub SetTRCPMessage()
        Me.btnCPLight.Enabled = True
        Select Case Me.TRCP
            Case 1, 2, 3, 4
                Me.btnCPLight.Enabled = True
                Me.btnCPLight.Text = "Active CP"
                Me.btnCPLight.ForeColor = Drawing.Color.Black
                Me.btnCPLight.BackColor = Drawing.Color.Red
            Case Else
                Me.btnCPLight.Enabled = True
                Me.btnCPLight.Text = "No Tax Lien"
                Me.btnCPLight.ForeColor = Drawing.Color.Black
                Me.btnCPLight.BackColor = Drawing.Color.LightGreen

        End Select
    End Sub

    Public Sub SetBankruptcyMessage()
        Me.btnBankruptcyLight.Enabled = True
        Select Case Me.AccountBankruptcy
            Case 0
                Me.btnBankruptcyLight.Enabled = True
                Me.btnBankruptcyLight.Text = "Account Solvent"
                Me.btnBankruptcyLight.ForeColor = Drawing.Color.Black
                Me.btnBankruptcyLight.BackColor = Drawing.Color.LightGreen
            Case 1
                Me.btnBankruptcyLight.Enabled = True
                Me.btnBankruptcyLight.Text = "Account in Bankruptcy"
                Me.btnBankruptcyLight.ForeColor = Drawing.Color.Black
                Me.btnBankruptcyLight.BackColor = Drawing.Color.Red
            Case 1000
                Me.btnBankruptcyLight.Enabled = False
            Case Else
                Me.btnBankruptcyLight.Enabled = True
                Me.btnBankruptcyLight.Text = "Account Solvent"
                Me.btnBankruptcyLight.ForeColor = Drawing.Color.Black
                Me.btnBankruptcyLight.BackColor = Drawing.Color.LightGreen

        End Select
    End Sub

    Public Sub SetTRStatusMessage()

        Select Case Me.TRStatus
            Case 0
                Me.btnRollStatusLight.Enabled = True
                Me.btnRollStatusLight.Text = "New Roll"
                Me.btnRollStatusLight.ForeColor = Drawing.Color.Black
                Me.btnRollStatusLight.BackColor = Drawing.Color.White
            Case 1
                Me.btnRollStatusLight.Enabled = True
                Me.btnRollStatusLight.Text = "Balance Due"
                Me.btnRollStatusLight.ForeColor = Drawing.Color.Black
                Me.btnRollStatusLight.BackColor = Drawing.Color.Red
            Case 2
                Me.btnRollStatusLight.Enabled = True
                Me.btnRollStatusLight.Text = "Paid in Full"
                Me.btnRollStatusLight.ForeColor = Drawing.Color.Black
                Me.btnRollStatusLight.BackColor = Drawing.Color.LightGreen
            Case 3
                Me.btnRollStatusLight.Enabled = True
                Me.btnRollStatusLight.Text = "Paid by Investor"
                Me.btnRollStatusLight.ForeColor = Drawing.Color.Black
                Me.btnRollStatusLight.BackColor = Drawing.Color.LightBlue
            Case 4
                Me.btnRollStatusLight.Enabled = True
                Me.btnRollStatusLight.Text = "State-Owned CP"
                Me.btnRollStatusLight.ForeColor = Drawing.Color.Black
                Me.btnRollStatusLight.BackColor = Drawing.Color.Yellow
            Case 5
                Me.btnRollStatusLight.Enabled = True
                Me.btnRollStatusLight.Text = "Redeemed"
                Me.btnRollStatusLight.ForeColor = Drawing.Color.Black
                Me.btnRollStatusLight.BackColor = Drawing.Color.Green
            Case 6
                Me.btnRollStatusLight.Enabled = True
                Me.btnRollStatusLight.Text = "Deeded"
                Me.btnRollStatusLight.ForeColor = Drawing.Color.Black
                Me.btnRollStatusLight.BackColor = Drawing.Color.LightBlue
            Case 7
                Me.btnRollStatusLight.Enabled = True
                Me.btnRollStatusLight.Text = "Charged Off"
                Me.btnRollStatusLight.ForeColor = Drawing.Color.Black
                Me.btnRollStatusLight.BackColor = Drawing.Color.Yellow
            Case 1000
                Me.btnRollStatusLight.Enabled = False
        End Select
    End Sub

    Public Sub SetTRBoardOrderMessage()
        ' Me.btnBoardOrderLight.Enabled = False
        Select Case Me.TRBoardOrder
            Case 0
                Me.btnBoardOrderLight.Enabled = True
                Me.btnBoardOrderLight.Text = "As Published"
                Me.btnBoardOrderLight.ForeColor = Drawing.Color.Black
                Me.btnBoardOrderLight.BackColor = Drawing.Color.LightGreen
                '   Case 1
                '        Me.btnBoardOrderLight.Enabled = True
                '       Me.btnBoardOrderLight.Text = "Change in Tax"
                '      Me.btnBoardOrderLight.ForeColor = Drawing.Color.Black
                '      Me.btnBoardOrderLight.BackColor = Drawing.Color.Yellow
                '  Case 2
                '      Me.btnBoardOrderLight.Enabled = True
                '      Me.btnBoardOrderLight.Text = "Change of Party or Address"
                '      Me.btnBoardOrderLight.ForeColor = Drawing.Color.Black
                '      Me.btnBoardOrderLight.BackColor = Drawing.Color.LightGray
                '  Case 3
                '      Me.btnBoardOrderLight.Enabled = True
                '      Me.btnBoardOrderLight.Text = "Change of Property Type"
                ''      Me.btnBoardOrderLight.ForeColor = Drawing.Color.Black
                '     Me.btnBoardOrderLight.BackColor = Drawing.Color.LightGray
            Case 4
                Me.btnBoardOrderLight.Enabled = True
                Me.btnBoardOrderLight.Text = "Roll Abated"
                Me.btnBoardOrderLight.ForeColor = Drawing.Color.Black
                Me.btnBoardOrderLight.BackColor = Drawing.Color.Red
            Case 1000
                Me.btnBoardOrderLight.Enabled = False
            Case Else
                Me.btnBoardOrderLight.Enabled = True
                Me.btnBoardOrderLight.Text = "Change in Roll"
                Me.btnBoardOrderLight.ForeColor = Drawing.Color.Black
                Me.btnBoardOrderLight.BackColor = Drawing.Color.Yellow
        End Select
    End Sub

    <Serializable()> _
    Private MustInherit Class RowClass
        Private _dataTable As DataTable
        Protected _dataRow As DataRow
        Protected _connectString As String

        Protected Sub New(connectString As String)
            _connectString = connectString
            _dataTable = New DataTable()
            _dataRow = _dataTable.NewRow()
        End Sub


        Public MustOverride ReadOnly Property TableName As String
        Public MustOverride ReadOnly Property ColumnNames As String
        Public MustOverride ReadOnly Property SortOrder As String


        Protected Function LoadData(whereClause As String) As Integer
            Dim cmd As New OleDbCommand()

            cmd.Connection = Me.Connection
            cmd.CommandText = String.Format("SELECT {0} FROM {1} WHERE {2}  ORDER BY {3}", _
                                        Me.ColumnNames, Me.TableName, whereClause, Me.SortOrder)

            Dim adt As New OleDbDataAdapter(cmd)

            _dataTable = New DataTable()
            adt.Fill(_dataTable)

            If _dataTable.Rows.Count >= 1 Then
                _dataRow = _dataTable.Rows(0)
                Return 1
            Else
                _dataRow = _dataTable.NewRow()
                Return 0
            End If
        End Function


        <NonSerialized()> _
        Private _connection As OleDbConnection


        Protected ReadOnly Property Connection As OleDbConnection
            Get
                If _connection Is Nothing Then
                    _connection = New OleDbConnection(_connectString)
                End If

                If _connection.State <> ConnectionState.Open Then
                    _connection.Open()
                End If

                Return _connection
            End Get
        End Property

        Protected Overrides Sub Finalize()
            'SyncLock _connection
            '    If _connection IsNot Nothing AndAlso _connection.State = ConnectionState.Open Then
            '        _connection.Close()
            '    End If
            'End SyncLock
        End Sub

#Region "Row Accessors"
        Private Function GetValue(columnName As String, defaultValue As Object) As Object
            If Not _dataTable.Columns.Contains(columnName) Then
                Return defaultValue
            End If

            If _dataRow.IsNull(columnName) Then
                Return defaultValue
            Else
                Return _dataRow.Item(columnName)
            End If
        End Function


        Private Sub SetValue(Of T)(columnName As String, value As T)
            ' Add column if required.
            If Not _dataTable.Columns.Contains(columnName) Then
                _dataTable.Columns.Add(columnName, GetType(T))
            End If

            ' Set value or null.
            If value Is Nothing Then
                _dataRow.Item(columnName) = DBNull.Value
            Else
                _dataRow.Item(columnName) = value
            End If
        End Sub


        Protected Function GetInt(columnName As String) As Integer
            Return GetValue(columnName, 0)
        End Function


        Protected Sub SetInt(columnName As String, value As Integer)
            SetValue(Of Integer)(columnName, value)
        End Sub


        Protected Function GetString(columnName As String) As String
            Return GetValue(columnName, String.Empty)
        End Function


        Protected Sub SetString(columnName As String, value As String)
            If String.IsNullOrEmpty(value) Then
                SetValue(Of String)(columnName, Nothing)
            Else
                SetValue(Of String)(columnName, value)
            End If
        End Sub


        Protected Function GetDate(columnName As String) As Date
            Return GetValue(columnName, Nothing)
        End Function


        Protected Sub SetDate(columnName As String, value As Date)
            SetValue(Of Date)(columnName, value)
        End Sub


        Protected Function GetBoolean(columnName As String) As Boolean
            Dim val As Object = GetValue(columnName, False)

            If IsNumeric(val) Then
                If val = 0 Then
                    Return False
                Else
                    Return True
                End If
            Else
                ' Should be boolean.
                Return CBool(val)
            End If
        End Function


        Protected Sub SetBoolean(columnName As String, value As Boolean)
            SetValue(Of Boolean)(columnName, value)
        End Sub


        Protected Function GetDecimal(columnName As String) As Decimal
            Return GetValue(columnName, 0)
        End Function


        Protected Sub SetDecimal(columnName As String, value As Decimal)
            SetValue(Of Decimal)(columnName, value)
        End Sub


        Protected Function GetShort(columnName As String) As Short
            Return GetValue(columnName, 0)
        End Function


        Protected Sub SetShort(columnName As String, value As Short)
            SetValue(Of Short)(columnName, value)
        End Sub


        Protected Function GetSingle(columnName As String) As Single
            Return GetValue(columnName, 0)
        End Function


        Protected Sub SetSingle(columnName As String, value As Single)
            SetValue(Of Single)(columnName, value)
        End Sub
#End Region
    End Class


    Private Class TaxRollMasterClass
        Inherits RowClass

        Private _taxRollValuesRow As DataRow
        Private _taxCalculationTable As DataTable
        Private _taxNamesTable As DataTable
        Private _tblTaxAuthChargeTypes As DataTable

#Region "Properties"
        Dim CityStateCode As String

        Public Property TaxYear As Integer
            Get
                Return MyBase.GetInt("TaxYear")

            End Get
            Set(value As Integer)
                MyBase.SetInt("TaxYear", value)
            End Set
        End Property

        Public Property TaxRollNumber As Integer
            Get
                Return MyBase.GetInt("TaxRollNumber")
            End Get
            Set(value As Integer)
                MyBase.SetInt("TaxRollNumber", value)
            End Set
        End Property

        Public Property Secured As Boolean
            Get
                Return MyBase.GetBoolean("SecuredUnsecured")
            End Get
            Set(value As Boolean)
                MyBase.SetBoolean("SecuredUnsecured", value)
            End Set
        End Property

        Public Property APN As String
            Get
                Return MyBase.GetString("APN")
            End Get
            Set(value As String)
                MyBase.SetString("APN", value)
            End Set
        End Property

        Public Property Latitude As Single
            Get
                Return MyBase.GetSingle("LATITUDE")
            End Get
            Set(value As Single)
                MyBase.SetSingle("LATITUDE", value)
            End Set
        End Property

        Public Property Longitude As Single
            Get
                Return MyBase.GetSingle("LONGITUDE")
            End Get
            Set(value As Single)
                MyBase.SetSingle("LONGITUDE", value)
            End Set
        End Property

        'Public Property BankruptcyAlert As Boolean
        '    Get
        '        Return MyBase.GetBoolean("BANKRUPTCY_ALERT")
        '    End Get
        '    Set(value As Boolean)
        '        MyBase.SetBoolean("BANKRUPTCY_ALERT", value)
        '    End Set
        'End Property

        'Public Property AlertStatus As Short
        '    Get
        '        Return MyBase.GetShort("ALERT_STATUS")
        '    End Get
        '    Set(value As Short)
        '        MyBase.SetShort("ALERT_STATUS", value)
        '    End Set
        'End Property

        Public Property Alert As Short
            Get
                Return MyBase.GetShort("ALERT")
            End Get
            Set(value As Short)
                MyBase.SetShort("ALERT", value)
            End Set
        End Property

        Public Property Suspend As Short
            Get
                Return MyBase.GetShort("SUSPEND")
            End Get
            Set(value As Short)
                MyBase.SetShort("SUSPEND", value)
            End Set
        End Property



        Public Property TaxIDNumber As String
            Get
                Return MyBase.GetString("TaxIDNumber")
            End Get
            Set(value As String)
                MyBase.SetString("TaxIDNumber", value)
            End Set
        End Property

        Public Property TaxPayerID As Integer
            Get
                Return MyBase.GetInt("TaxPayerID")
            End Get
            Set(value As Integer)
                MyBase.SetInt("TaxPayerID", value)
            End Set
        End Property

        Public Property CurrentBalance As Decimal
            Get
                Return MyBase.GetDecimal("CurrentBalance")
            End Get
            Set(value As Decimal)
                MyBase.SetDecimal("CurrentBalance", value)
            End Set
        End Property

        Public Property EditUser As String
            Get
                Return MyBase.GetString("EDIT_USER")
            End Get
            Set(value As String)
                MyBase.SetString("EDIT_USER", value)
            End Set
        End Property

        Public Property EditDate As Date
            Get
                Return MyBase.GetDate("EDIT_DATE")
            End Get
            Set(value As Date)
                MyBase.SetDate("EDIT_DATE", value)
            End Set
        End Property

        Public Property CreateUser As String
            Get
                Return MyBase.GetString("CREATE_USER")
            End Get
            Set(value As String)
                MyBase.SetString("CREATE_USER", value)
            End Set
        End Property

        Public Property CreateDate As Date
            Get
                Return MyBase.GetDate("CREATE_DATE")
            End Get
            Set(value As Date)
                MyBase.SetDate("CREATE_DATE", value)
            End Set
        End Property

        Public ReadOnly Property FirstName As String
            Get
                'If Me.TaxNamesTable.Rows.Count > 0 Then
                '    Return Me.TaxNamesTable.Rows(0)("FirstName").ToString()
                'Else
                Return _dataRow("OWNER_NAME_3").ToString()
                'End If
            End Get
        End Property

        Public ReadOnly Property MiddleName As String
            Get
                'If Me.TaxNamesTable.Rows.Count > 0 Then
                'Return Me.TaxNamesTable.Rows(0)("MiddleName").ToString()
                'Else
                Return _dataRow("OWNER_NAME_2").ToString()
                ' End If
            End Get
        End Property

        Public ReadOnly Property LastName As String
            Get
                'If Me.TaxNamesTable.Rows.Count > 0 Then
                'Return Me.TaxNamesTable.Rows(0)("LastName").ToString()
                'Else
                Return _dataRow("OWNER_NAME_1").ToString()
                ' End If
            End Get
        End Property

        Public ReadOnly Property MailAddress1 As String
            Get
                '  If Me.TaxNamesTable.Rows.Count > 0 Then
                'Return Me.TaxNamesTable.Rows(0)("LastName").ToString()
                ' Else
                Return _dataRow("MAIL_ADDRESS_1").ToString()
                '  End If
            End Get
        End Property

        Public ReadOnly Property MailAddress2 As String
            Get
                Return _dataRow("MAIL_ADDRESS_2").ToString()
            End Get
        End Property

        Public ReadOnly Property MailCityStateCode As String
            Get
                Dim CityStateCode = _dataRow("MAIL_CITY").ToString() + " " + _dataRow("MAIL_STATE").ToString() + " " + _dataRow("MAIL_CODE").ToString()
                Return CityStateCode
            End Get
        End Property

        Public ReadOnly Property OwnerName As String
            Get
                OwnerName = LastName
                If (Not String.IsNullOrEmpty(OwnerName)) AndAlso (Not String.IsNullOrEmpty(MiddleName)) Then
                    OwnerName = MiddleName & " " & OwnerName
                End If
                If (Not String.IsNullOrEmpty(OwnerName)) AndAlso (Not String.IsNullOrEmpty(FirstName)) Then
                    OwnerName = FirstName & " " & OwnerName
                End If
            End Get
        End Property

        Private _maxTaxTypeID As Integer?
        Public ReadOnly Property MaxTaxTypeID As Integer
            Get
                If _maxTaxTypeID Is Nothing Then
                    Using cmd As New OleDbCommand()
                        cmd.Connection = Me.Connection
                        cmd.CommandText = "select FieldData from genii_user.tblTaxSystemParameters where FieldName='MaxTaxTypeID'"
                        Dim objResult As Object = cmd.ExecuteScalar()
                        If IsNumeric(objResult) Then
                            _maxTaxTypeID = CInt(objResult)
                        Else
                            ' Default
                            _maxTaxTypeID = 40
                        End If
                    End Using
                End If

                Return _maxTaxTypeID
            End Get
        End Property

        Private _maxTaxDueBothHalves As Decimal?
        Public ReadOnly Property MaxTaxDueBothHalves As Decimal
            Get
                If _maxTaxDueBothHalves Is Nothing Then
                    Using cmd As New OleDbCommand()
                        cmd.Connection = Me.Connection
                        cmd.CommandText = "select FieldData from genii_user.tblTaxSystemParameters where FieldName='MaxTaxDueBothHalves'"
                        Dim objResult As Object = cmd.ExecuteScalar()
                        If IsNumeric(objResult) Then
                            _maxTaxDueBothHalves = CDec(objResult)
                        Else
                            ' Default
                            _maxTaxDueBothHalves = 100
                        End If
                    End Using
                End If

                Return _maxTaxDueBothHalves
            End Get
        End Property
#End Region


        Public Sub New(connectString As String)
            MyBase.New(connectString)
        End Sub


        Public Overrides ReadOnly Property TableName As String
            Get
                Return "genii_user.TR"
            End Get
        End Property


        Public Overrides ReadOnly Property ColumnNames As String
            Get
                Return "*"
            End Get
        End Property


        Public Overrides ReadOnly Property SortOrder As String
            Get
                Return "TaxYear desc"
            End Get
        End Property


        Public Function IsLoaded() As Boolean
            If _dataRow Is Nothing Then
                Return False
            End If
            If IsDBNull(_dataRow("TaxRollNumber")) Then
                Return False
            End If

            Return True
        End Function


        Public Overloads Sub LoadData(ByVal searchBy As String)
            Dim wc As String = String.Empty

            Select Case searchBy.ToLower().Trim()
                Case "apn"
                    wc = String.Format("APN = '{0}' AND TaxYear = {1}", Me.APN, Me.TaxYear)
                Case "taxrollnumber"
                    wc = String.Format("TaxRollNumber = {0} AND TaxYear = {1}", Me.TaxRollNumber, Me.TaxYear)
                Case "taxid"
                    wc = String.Format("TaxIDNumber ='{0}' AND TaxYear = {1}", Me.TaxIDNumber, Me.TaxYear)
                    ' wc = "taxIDNumber='" & _ Me.TaxIDNumber.ToString() + "' and TaxYear='" & _ Me.TaxYear + "' "
                Case "ssan"
                    '  wc = String.Format("")
            End Select



            'If String.IsNullOrEmpty(Me.APN) Then
            '    wc = String.Format("TaxRollNumber = {0} AND TaxYear = {1}", Me.TaxRollNumber, Me.TaxYear)
            'ElseIf String.IsNullOrEmpty(Me.TaxRollNumber) Then
            '    wc = String.Format("APN = '{0}' AND TaxYear = {1}", Me.APN, Me.TaxYear)
            'End If





            MyBase.LoadData(wc)
        End Sub

        Public Overloads Sub LoadDataNoYear(ByVal searchBy As String)
            Dim wc As String = String.Empty

            Select Case searchBy.ToLower().Trim()
                Case "apn"
                    wc = String.Format("APN = '{0}' ", Me.APN)
                Case "taxrollnumber"
                    wc = String.Format("TaxRollNumber = {0} ", Me.TaxRollNumber)
                Case "taxid"
                    wc = String.Format("TaxIDNumber ='{0}' ", Me.TaxIDNumber)
                    ' wc = "taxIDNumber='" & _ Me.TaxIDNumber.ToString() + "' and TaxYear='" & _ Me.TaxYear + "' "
                Case "ssan"
                    '  wc = String.Format("")
                    '  wc = String.Format("Owner_Name_1 like '%{0}%' ", Me.txtRegSSAN.text)
            End Select

            'If String.IsNullOrEmpty(Me.APN) Then
            '    wc = String.Format("TaxRollNumber = {0} AND TaxYear = {1}", Me.TaxRollNumber, Me.TaxYear)
            'ElseIf String.IsNullOrEmpty(Me.TaxRollNumber) Then
            '    wc = String.Format("APN = '{0}' AND TaxYear = {1}", Me.APN, Me.TaxYear)
            'End If





            MyBase.LoadData(wc)
        End Sub


        Public ReadOnly Property TaxRollValuesRow() As DataRow
            Get
                If _taxRollValuesRow Is Nothing Then
                    Using cmd As New OleDbCommand()
                        cmd.Connection = Me.Connection
                        '  cmd.CommandText = "SELECT * FROM genii_user.TR_VALUES WHERE TaxYear = ? AND TaxRollNumber = ?" 'CHANGED TO GENII_USER.TR MTA.. OBSOLETE TABLE..
                        cmd.CommandText = "SELECT * FROM genii_user.TR WHERE TaxYear = ? AND TaxRollNumber = ?"
                        cmd.Parameters.AddWithValue("@TaxYear", Me.TaxYear)
                        cmd.Parameters.AddWithValue("@TaxRollNumber", Me.TaxRollNumber)

                        Dim adt As New OleDbDataAdapter(cmd)
                        Dim dt As New DataTable()

                        adt.Fill(dt)

                        If dt.Rows.Count >= 1 Then
                            _taxRollValuesRow = dt.Rows(0)
                        End If
                    End Using
                End If
                Return _taxRollValuesRow
            End Get
        End Property


        Private Function GetParameter(name As String, type As OleDbType, value As Object) As OleDbParameter
            Dim param As New OleDbParameter(name, type)
            param.Value = value
            Return param
        End Function


        Public ReadOnly Property TaxCalculationTable As DataTable

            Get
                If _taxCalculationTable Is Nothing Then
                    Dim cmd As New OleDbCommand()
                    cmd.Connection = Me.Connection

                    cmd.CommandText = "SELECT * FROM genii_user.TR_CHARGES WHERE TaxYear = ? AND TaxRollNumber = ?"
                    'cmd.CommandText = "SELECT * FROM genii_user.TR_CHARGES"
                    cmd.Parameters.Add(GetParameter("@TaxYear", OleDbType.VarChar, Me.TaxYear))
                    cmd.Parameters.Add(GetParameter("@TaxRollNumber", OleDbType.VarChar, Me.TaxRollNumber))

                    Dim adt As New OleDbDataAdapter(cmd)

                    _taxCalculationTable = New DataTable()
                    adt.Fill(_taxCalculationTable)
                End If

                Return _taxCalculationTable
            End Get
        End Property


        'Public ReadOnly Property TaxNamesTable() As DataTable
        '    Get
        '        If _taxNamesTable Is Nothing Then
        '            Dim cmd As New OleDbCommand()

        '            cmd.Connection = Me.Connection
        '            ''cmd.CommandText = "SELECT * FROM genii_user.tblTaxNames WHERE TaxYear = ? AND TaxRollNumber = ?"
        '            cmd.Parameters.AddWithValue("@TaxYear", Me.TaxYear)
        '            cmd.Parameters.AddWithValue("@TaxRollNumber", Me.TaxRollNumber)

        '            Dim adt As New OleDbDataAdapter(cmd)

        '            _taxNamesTable = New DataTable()
        '            adt.Fill(_taxNamesTable)
        '        End If

        '        Return _taxNamesTable
        '    End Get
        'End Property


        Public Sub RecalculateFees(paymentDate As Date)
            Using cmd As New OleDbCommand()
                cmd.Connection = Me.Connection
                cmd.CommandText = "SetDelinquentFees"
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.Add("RETURN_VALUE", OleDbType.Integer).Direction = ParameterDirection.ReturnValue
                cmd.Parameters.AddWithValue("@taxYear", Me.TaxYear)
                cmd.Parameters.AddWithValue("@taxRollNumber", Me.TaxRollNumber)
                cmd.Parameters.AddWithValue("@paymentDate", paymentDate)
                cmd.Parameters.AddWithValue("@userName", TaxPayments.CurrentUserName)

                cmd.ExecuteNonQuery()
            End Using
        End Sub


        Public Function GetTaxes() As Decimal
            Dim tax As Object = Me.TaxCalculationTable.Compute("SUM(ChargeAmount)", "ChargeAmount IS NOT NULL AND TaxTypeID <= " & Me.MaxTaxTypeID)
            If IsNumeric(tax) Then
                Return CDec(tax)
            Else
                Return 0
            End If
        End Function


        Public Function GetChargesTable() As DataView
            Dim vw As New DataView(Me.TaxCalculationTable)
            vw.RowFilter = "TaxTypeID > " & Me.MaxTaxTypeID
            Return vw
        End Function


        Public Sub GetDelinquentDates(ByRef firstHalfDelinquent As Date, ByRef secondHalfDelinquent As Date)
            If Me.TaxRollValuesRow.IsNull("FirstHalfDelinquent") Then
                firstHalfDelinquent = Nothing
            Else
                firstHalfDelinquent = Me.TaxRollValuesRow("FirstHalfDelinquent")
            End If

            If Me.TaxRollValuesRow.IsNull("SecondHalfDelinquent") Then
                secondHalfDelinquent = Nothing
            Else
                secondHalfDelinquent = Me.TaxRollValuesRow("SecondHalfDelinquent")
            End If
        End Sub

        Public Sub GetInterestAndFee(paymentDate As Date, ByRef interest As Decimal, ByRef fee As Decimal)
            interest = 0
            fee = 0

            For Each row As DataRow In Me.TaxCalculationTable.Rows
                Dim taxTypeID As Integer = CInt(row("TaxTypeID"))
                Dim chargeAmount As Decimal = If(row.IsNull("ChargeAmount"), 0, CDec(row("ChargeAmount")))

                If IsInterestFee(taxTypeID) Then
                    interest += chargeAmount
                ElseIf IsDelinquentFee(taxTypeID) Then
                    fee += chargeAmount
                End If
            Next
        End Sub


        Public Function GetTotalPayments() As Decimal
            Dim cmd As New OleDbCommand()

            cmd.Connection = Me.Connection
            'cmd.CommandText = "SELECT SUM([TCP].PaymentAmount) FROM genii_user.TR_CALENDAR TC " & _
            '                  "INNER JOIN genii_user.TR_PAYMENTS [TCP] ON TC.RECORD_ID = [TCP].RECORD_ID " & _
            '                  "WHERE TC.TaxYear = ? AND TC.TaxRollNumber = ?"
            cmd.CommandText = "SELECT SUM(PaymentAmount) FROM genii_user.TR_PAYMENTS " & _
                              "WHERE TaxYear = ? AND TaxRollNumber = ? "

            cmd.Parameters.AddWithValue("@TaxYear", Me.TaxYear)
            cmd.Parameters.AddWithValue("@TaxRollNumber", Me.TaxRollNumber)

            Dim payments As Object = cmd.ExecuteScalar()

            If payments IsNot Nothing AndAlso IsNumeric(payments) Then
                Return CDec(payments)
            Else
                Return 0
            End If
        End Function

        Public Function GetTotalPayments2(Taxyear As String, TaxRollNumber As String) As Decimal
            Dim cmd As New OleDbCommand()

            cmd.Connection = Me.Connection
            'cmd.CommandText = "SELECT SUM([TCP].PaymentAmount) FROM genii_user.TR_CALENDAR TC " & _
            '                  "INNER JOIN genii_user.TR_PAYMENTS [TCP] ON TC.RECORD_ID = [TCP].RECORD_ID " & _
            '                  "WHERE TC.TaxYear = ? AND TC.TaxRollNumber = ?"
            cmd.CommandText = "SELECT SUM(PaymentAmount) FROM genii_user.TR_PAYMENTS " & _
                              "WHERE TaxYear = ? AND TaxRollNumber = ? "

            cmd.Parameters.AddWithValue("@TaxYear", Taxyear)
            cmd.Parameters.AddWithValue("@TaxRollNumber", TaxRollNumber)

            Dim payments As Object = cmd.ExecuteScalar()

            If payments IsNot Nothing AndAlso IsNumeric(payments) Then
                Return CDec(payments)
            Else
                Return 0
            End If
        End Function


        Public Function IsInterestFee(taxTypeID As Integer) As Boolean
            If _tblTaxAuthChargeTypes Is Nothing Then
                _tblTaxAuthChargeTypes = New DataTable()

                Using adt As New OleDbDataAdapter("SELECT * FROM genii_user.LEVY_TAX_TYPES", Me.Connection)
                    adt.Fill(_tblTaxAuthChargeTypes)
                End Using
            End If

            Dim rows As DataRow() = _tblTaxAuthChargeTypes.Select("TaxTypeID = " & taxTypeID)

            If rows.Length >= 1 Then
                Return rows(0)("DelinquentInterest") = 1
            Else
                Throw New ApplicationException("No matching AuthType:" & taxTypeID)
            End If
        End Function


        Public Function IsDelinquentFee(taxTypeID As Integer) As Boolean
            If _tblTaxAuthChargeTypes Is Nothing Then
                _tblTaxAuthChargeTypes = New DataTable()

                Using adt As New OleDbDataAdapter("SELECT * FROM genii_user.tblTaxAuthChargeTypes", Me.Connection)
                    adt.Fill(_tblTaxAuthChargeTypes)
                End Using
            End If

            Dim rows As DataRow() = _tblTaxAuthChargeTypes.Select("TaxTypeID = " & taxTypeID)

            If rows.Length >= 1 Then
                Return rows(0)("DelinquentFee") = 1
            Else
                Throw New ApplicationException("No matching AuthType:" & taxTypeID)
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

        <System.Web.Services.WebMethod()> _
        Public Shared Function btnPopupResult() As Boolean
            'Me.divViewForeclosures.Attributes.Add("style", "display:block")
            ' ClientScript.RegisterStartupScript(Me."",""[GetType](), "ShowDiv", "document.getElementById('divViewForeclosures').style.display=''", True)

            Dim a As String = "mia"




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

        <System.Web.Services.WebMethod()> _
        Public Shared Function GetInvestorTR(InvestorIDorSSAN As String, TaxYear As String) As Generic.Dictionary(Of String, String)
            ' Extract numerals from ssan.
            Dim ssn As String = ExtractSSANNumerals(InvestorIDorSSAN)
            Dim SQL As String = String.Empty
            Dim cmd As New OleDbCommand()
            Dim result As New Generic.Dictionary(Of String, String)
            ' Dim TaxYear As String = GetTaxYear()

            Dim myUtil As New Utilities()

            ' Search database.
            Using conn As New OleDbConnection(myUtil.ConnectString)

                If (Not String.IsNullOrEmpty(ssn)) Then
                    SQL = "SELECT TOP 25 TaxRollNumber, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2, OWNER_NAME_1, TaxYear,mail_address_1,mail_address_2 FROM genii_user.TR " & _
                          "WHERE TaxYear=? and TaxRollNumber = ? OR REPLACE(TaxIDNumber,'-','') LIKE ? "

                    cmd = New OleDbCommand(SQL)
                    cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                    cmd.Parameters.AddWithValue("@TaxRollNumber", ssn)
                    cmd.Parameters.AddWithValue("@TaxIDNumber", ssn & "%")
                Else
                    '' SSAN is empty try to search by lastname
                    If (Not String.IsNullOrEmpty(InvestorIDorSSAN.Trim())) Then
                        SQL = "SELECT TOP 25 TaxRollNumber, TaxIDNumber, OWNER_NAME_3, OWNER_NAME_2, OWNER_NAME_1, TaxYear,mail_address_1,mail_address_2 FROM genii_user.TR " & _
                         "WHERE TaxYear=? and OWNER_NAME_1 LIKE ? or mail_address_1 like ? or mail_address_2 like ?"

                        cmd = New OleDbCommand(SQL)
                        cmd.Parameters.AddWithValue("@TaxYear", TaxYear)
                        cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" & InvestorIDorSSAN & "%")
                        cmd.Parameters.AddWithValue("@mail_address_1", "%" & InvestorIDorSSAN & "%")
                        cmd.Parameters.AddWithValue("@mail_address_2", "%" & InvestorIDorSSAN & "%")
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
                        Dim taxRollNumber As Integer = rdr.GetInt32(0)
                        Dim taxIDNumber As Object = rdr.GetValue(1)
                        Dim firstName As Object = rdr.GetValue(2)
                        Dim middleName As Object = rdr.GetValue(3)
                        Dim lastName As Object = rdr.GetValue(4)
                        Dim mail1 As Object = rdr.GetValue(6)
                        Dim mail2 As Object = rdr.GetValue(7)
                        '   Dim taxYear As Object = rdr.GetValue(5)

                        Dim resultString As New StringBuilder()

                        resultString.AppendFormat("{0}, ", taxRollNumber)

                        If Not IsDBNull(taxIDNumber) Then
                            resultString.AppendFormat("{0}, ", taxIDNumber)
                        End If

                        If Not IsDBNull(firstName) Then
                            resultString.AppendFormat("{0} ", firstName)
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


                        result.Add(taxRollNumber.ToString(), resultString.ToString())
                    End While

                    If result.Count = 0 Then
                        result.Add("0", "Add New")
                    End If

                End If

                Return result
            End Using
        End Function

    End Class



End Class



