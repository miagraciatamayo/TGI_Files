Imports System.Data
Imports System.Data.OleDb
Imports System.Drawing.Printing.PrintDocument
Imports System.Drawing.Printing
Imports System.Drawing

Partial Class TaxSupervisor
    Inherits System.Web.UI.Page


    Private _trCount As Generic.Dictionary(Of Integer, Integer)
    Private _tblPaymentType As DataTable
    Private _tblTaxAuthority As DataTable
    Private Shared newReceiptNumber As Integer
    Private Shared _GRPKEY As Integer = 0
    ' Private _isSessionEnded As Boolean = True

#Region "Common Properties"
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

    Protected ReadOnly Property SalePrepInitialMessage As String
        Get
            Return "<small>&lt;Select Year and click on 'Go'&gt;</small>"
        End Get
    End Property

    Private ReadOnly Property BankConnectString As String
        Get
            Return ConfigurationManager.ConnectionStrings("BankConnString").ConnectionString
        End Get
    End Property


    Private ReadOnly Property CurrentUserName As String
        Get
            Dim UserName As String
            Dim SlashPos As Integer

            SlashPos = InStr(System.Web.HttpContext.Current.User.Identity.Name, "\")

            If SlashPos > 0 Then
                UserName = Mid(System.Web.HttpContext.Current.User.Identity.Name, SlashPos + 1)
            Else
                UserName = System.Web.HttpContext.Current.User.Identity.Name
            End If

            Return UserName
        End Get
    End Property


    Private ReadOnly Property PaymentTypeTable As DataTable
        Get
            If _tblPaymentType Is Nothing Then
                _tblPaymentType = New DataTable()
                Using adt As New OleDbDataAdapter("select * from genii_user.ST_PAYMENT_INSTRUMENT", Me.ConnectString)
                    adt.Fill(_tblPaymentType)
                End Using
            End If

            Return _tblPaymentType
        End Get
    End Property

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="list"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    '  Public Shared Function CreateIDsString(ByVal list As List(Of String)) As String
    Public Shared Function CreateIDsString(ByVal list As Generic.List(Of String)) As String
        Dim rslt As New StringBuilder
        rslt.Append("(")
        Dim i As Integer

        For i = 0 To list.Count - 1
            rslt.Append(list.Item(i) & ",")
        Next

        If (rslt.Length > 0) Then
            rslt.Remove(rslt.Length - 1, 1)
        End If

        rslt.Append(")")

        Return rslt.ToString
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Duration"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function TimeSpan(ByVal Duration As DateTime) As Integer
        Dim date1 As DateTime = Duration
        Dim date2 As DateTime = DateTime.Now
        Dim ts As TimeSpan = date2.Subtract(date1)
        Return ts.Days
    End Function


#End Region

#Region "Event Handlers"
    Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
        If Not Me.IsPostBack Then
            Me.txtPosDate.Text = Date.Today.ToShortDateString()
            ' BindLettersGrid()
            PopulateSalePrepYears()
            LoadCountyInfo()
            'ViewRefunds2()
            'LoadLPS2()
            'LoadCAD2()
            'LoadDailyLetters()
            ''   chkProcessRefunds(Me, EventArgs.Empty)
            '' TODO: FIX THIS
            '' BindForeclosuresGrid()
        End If
    End Sub

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

            'If IsNumeric(startCash) Then
            '    Me.txtLoginStartCash.Text = startCash
            'Else
            '    Me.txtLoginStartCash.Text = String.Empty
            'End If
        End Using

        Me.lblLoginUsername.Text = userName

        ClientScript.RegisterStartupScript(Me.GetType, "Login", "$(document).ready(function() { showLoginDialog(); });", True)
    End Sub

    Protected Sub btnLogin_Click(sender As Object, e As System.EventArgs) Handles btnLogin.Click
        CreateNewSession()
    End Sub

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

    Private Sub CreateNewSession()
        Dim userName As String = (System.Web.HttpContext.Current.User.Identity.Name).Trim()
        Dim startCash As Decimal ' = CDec(Me.txtLoginStartCash.Text)
        'startcash must be cash paid by user...

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

                'SessionRecordID = recordID

                'Me.lblOperatorName.Text = userName
                'Me.lblCurrentDate.Text = Date.Today.ToShortDateString()
                'Me.lblLoginTime.Text = Date.Now.ToString()
                'Me.lblStartCash.Text = startCash.ToString("C")
                'Me.lblLogoutUsername.Text = userName
                'Me.lblSessionID.Text = SessionRecordID

                trans.Commit()

                conn.Close()
            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
        End Using
    End Sub
#End Region



#Region "Posting Tab"

#Region "Properties"
    Private Const SESS_VAR_SelectedSessionID As String = "TaxSupervisor_Posting_SelectedSessionID"
    Private Property SelectedSessionID As Integer
        Get
            If IsNumeric(Session(SESS_VAR_SelectedSessionID)) Then
                Return CInt(Session(SESS_VAR_SelectedSessionID))
            Else
                Return 0
            End If
        End Get
        Set(ByVal value As Integer)
            Session(SESS_VAR_SelectedSessionID) = value
        End Set
    End Property

    ''' <summary>
    ''' Gets payment type (cash, check, credit card, etc.).
    ''' </summary>
    ''' <param name="paymentType"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Protected Function GetPaymentType(ByVal paymentType As Object) As String
        If IsNumeric(paymentType) Then
            Dim rows As DataRow() = Me.PaymentTypeTable.Select("PaymentTypeCode=" & paymentType.ToString())
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
    Protected Function GetChargeCode(ByVal taxChargeCodeID As String) As String
        If _tblTaxAuthority Is Nothing Then
            Using adt As New OleDbDataAdapter("select TaxChargeCodeID, TaxChargeDescription from genii_user.LEVY_AUTHORITY", Me.ConnectString)
                _tblTaxAuthority = New DataTable()
                adt.Fill(_tblTaxAuthority)
            End Using
        End If

        Dim rows As DataRow() = _tblTaxAuthority.Select(String.Format("TaxChargeCodeID='{0}'", taxChargeCodeID))
        If rows.Length > 0 Then
            Return rows(0)("TaxChargeDescription").ToString()
        Else
            Return taxChargeCodeID
        End If
    End Function

    Private _tblTaxType As DataTable
    ''' <summary>
    ''' Gets tax type from LEVY_TAX_TYPES table.
    ''' </summary>
    ''' <param name="taxTypeID"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Protected Function GetTaxType(ByVal taxTypeID As String) As String
        If _tblTaxType Is Nothing Then
            Using adt As New OleDbDataAdapter("select TaxTypeID, TaxCodeDescription from genii_user.LEVY_TAX_TYPES", Me.ConnectString)
                _tblTaxType = New DataTable()
                adt.Fill(_tblTaxType)
            End Using
        End If

        Dim rows As DataRow() = _tblTaxType.Select(String.Format("TaxTypeID='{0}'", taxTypeID))
        If rows.Length > 0 Then
            Return rows(0)("TaxCodeDescription").ToString()
        Else
            Return taxTypeID
        End If
    End Function
#End Region


#Region "Event Handlers"
    Protected Sub btnPosLoadPosting_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPosLoadPosting.Click
        grdPosTransactions.DataSource = Nothing

        LoadDeposits()
        Me.btnPost.Enabled = False
        Me.btnPostLoadSession.Enabled = True
        If (Me.rdoPosDate.Checked = True) Then
            Me.btnPost.Enabled = False

        End If
    End Sub

    ' Public Sub GridView1_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles grdPosDeposits.RowDataBound

    'Dim depositsTable As New DataTable()
    '    'grdposdeposits
    '    Dim x As Integer = 0
    '    Dim y As Integer = grdPosDeposits.Rows.Count
    '   Dim this As String
    '
    '   For x = 0 To (y - 1)
    '    Dim btnPost As Button = grdPosDeposits.Rows(x).FindControl("btnPost")
    '
    '       If (btnPost.OnClientClick) Then
    '         this = grdPosDeposits.Rows(x).Cells(1).Text
    ''
    '       End If
    '
    '   Next

    '    Using adt As New OleDbDataAdapter(String.Empty, Me.ConnectString)
    'Dim sql As String = "SELECT * FROM vCashierSession WHERE "

    '           If Me.rdoPosDate.Checked Then
    '              sql &= String.Format("START_TIME BETWEEN '{0}' AND DATEADD(d, 1, '{0}')", Me.txtPosDate.Text)
    '         Else
    '            sql &= "POSTED = 0"
    '       End If
    '
    '       adt.SelectCommand.CommandText = sql

    '        adt.SelectCommand.Connection.Open()
    '
    'Dim depositsDataAdapter As New OleDbDataAdapter(adt.SelectCommand)

    '            depositsDataAdapter.Fill(depositsTable)
    '       End Using




    'End Sub

    ' Protected Sub grdPosTransactions_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles grdPosTransactions.RowCommand
    '     If (e.CommandName = "DeleteTransaction") Then
    '   Dim index As Integer = Convert.ToInt32(e.CommandArgument)
    '  Dim gvRow As GridViewRow = grdPosTransactions.Rows(index)
    '   Dim TransactionAmount As Double = grdPosTransactions.Rows(index).Cells(4).Text
    '   Dim TransactionPayor As String = grdPosTransactions.Rows(index).Cells(5).Text

    '           lblDeleteDivPayorName.Text = TransactionPayor
    '          lblDeleteDivAmount.Text = TransactionAmount

    '          ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Delete Transaction", "showDeleteTrans('Delete Transaction')", True)
    '     End If



    '  End Sub

    Public Sub callBtnPostLoadSession()
        btnPostLoadSession_Click(Me, EventArgs.Empty)
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

    Protected Sub btnPostLoadSession_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPostLoadSession.Click


        If IsNumeric(Me.hdnPosSessionID.Value) Then
            Me.SelectedSessionID = CInt(Me.hdnPosSessionID.Value)
            'here..MTA
            ' LoadSessionTransactions(Me.SelectedSessionID)
            LoadSessionApportionments(Me.SelectedSessionID)
            LoadSessionRefunds(Me.SelectedSessionID)
            LoadSessionKittyAmounts(Me.SelectedSessionID)
            LoadSessionDeclinedAmounts(Me.SelectedSessionID)

            '  btnPostLoadSession.Enabled = False
            If (Me.rdoPosDate.Checked = True) Then
                btnPost.Enabled = False
            Else
                btnPost.Enabled = True
            End If

            '  Me.lblDeleteDivAmount .Text=

            'load values into popup
            Dim idx As Integer = Me.hdnPosSessionID.Value
            Dim cashierSessionDetails As DataSet = New DataSet()
            Dim isSessionEnded As Boolean
            Dim sql As String = String.Format("Select * from vcashierSession where record_id= {0}", idx)

            LoadTable(cashierSessionDetails, "CASHIER_SESSION", sql)

            Dim row As DataRow
            row = cashierSessionDetails.Tables(0).Rows(0)



            If (IsDBNull(row("END_TIME"))) Then
                isSessionEnded = False
                btnPost.Enabled = False                
            Else
                isSessionEnded = True                
            End If

            If (IsDBNull(row("POSTED")) Or (row("POSTED") = 0)) Then
                grdPosTransactionsReverse.Visible = False
                grdPosTransactions.Visible = True
                LoadSessionTransactions(Me.SelectedSessionID)
            Else
                grdPosTransactions.Visible = False
                grdPosTransactionsReverse.Visible = True
                LoadSessionTransactionsReverse(Me.SelectedSessionID)
            End If

            'load values into popup
            'Dim transactionSessionDetails As DataSet = New DataSet()          
            'Dim sql2 As String = String.Format("Select * from genii_user.CASHIER_TRANSACTIONS where SESSION_id= {0} ", idx)

            'LoadTable(transactionSessionDetails, "CASHIER_TRANSACTIONS", sql2)

            ' Dim row2 As DataRow
            ' If (Not transactionSessionDetails.Tables(0).Rows(0) Is Nothing) Then
            'row2 = transactionSessionDetails.Tables(0).Rows(0)
            'End If


            'Dim x As Integer = 0
            'For Each row2 As DataRow In transactionSessionDetails.Tables(0).Rows()
            '    Dim row3 As DataRow = transactionSessionDetails.Tables(0).Rows(x)
            '    If (IsDBNull(row3("PAYOR_NAME"))) Then
            '        Me.lblDeleteDivPayorName.Text = String.Empty
            '    Else
            '        Me.lblDeleteDivPayorName.Text = row3("PAYOR_NAME")
            '    End If

            '    If (IsDBNull(row3("PAYMENT_AMT"))) Then
            '        Me.lblDeleteDivAmount.Text = String.Empty
            '    Else
            '        Me.lblDeleteDivAmount.Text = "$" & row3("PAYMENT_AMT")
            '    End If

            '    If (IsDBNull(row3("RECORD_ID"))) Then
            '        Me.lblDeleteDivRecordID.Text = String.Empty
            '    Else
            '        Me.lblDeleteDivRecordID.Text = row3("RECORD_ID")
            '    End If
            '    x = x + 1
            'Next





            '  idx = idx - 1
            '     Dim recordID As String = grdPosDeposits.Rows(idx).Cells(0).Text
            '  Dim a As String = grdPosDeposits.Rows(idx).Cells(1).Text
            '  Dim b As String = grdPosDeposits.Rows(idx).Cells(2).Text
            '  Dim c As String = grdPosDeposits.Rows(idx).Cells(3).Text
            ' Dim d As String = grdPosDeposits.Rows(idx).Cells(4).Text

            ' Dim i As String = grdPosDeposits.Rows(idx).Cells(5).Text
            ' Dim f As String = grdPosDeposits.Rows(idx).Cells(6).Text
            ' Dim g As String = grdPosDeposits.Rows(idx).Cells(7).Text
            ' Dim h As String = grdPosDeposits.Rows(idx).Cells(8).Text

            '  For Each row As GridViewRow In Me.grdPosDeposits.Rows
            'Dim btnPost2 As Button = row.FindControl("btnPost")
            '  btnPost2.Enabled = False

            '  Next


            '  Dim btnPost As Button = grdPosDeposits.Rows(selectedIndex).FindControl("btnPost") 'this is wrong. must be row number
            '  btnPost.Enabled = True

            LoadSessionValues(idx)
            '  LoadTransactionValues(idx)

        End If
    End Sub

    ' Public Sub LoadTransactionValues(recordID As String)

    ' End Sub

    Protected Sub rdoSelectDeposit(ByVal sender As Object, ByVal e As System.EventArgs)
        LoadDeposits()
        Me.btnPost.Enabled = False
        Me.btnPostLoadSession.Enabled = True
        If (Me.rdoPosDate.Checked = True) Then
            Me.btnPost.Enabled = False

        End If
    End Sub

    Protected Sub rdoLoadDetails(ByVal sender As Object, ByVal e As System.EventArgs)
        If IsNumeric(Me.hdnPosSessionID.Value) Then
            Me.SelectedSessionID = CInt(Me.hdnPosSessionID.Value)
            LoadSessionTransactions(Me.SelectedSessionID)
            LoadSessionApportionments(Me.SelectedSessionID)
            LoadSessionRefunds(Me.SelectedSessionID)
            LoadSessionKittyAmounts(Me.SelectedSessionID)
            LoadSessionDeclinedAmounts(Me.SelectedSessionID)
            'load values into popup
            Dim idx As Integer = Me.hdnPosSessionID.Value
            Dim cashierSessionDetails As DataSet = New DataSet()
            Dim isSessionEnded As Boolean
            Dim sql As String = String.Format("Select * from vcashierSession where record_id= " + idx + " ")

            LoadTable(cashierSessionDetails, "CASHIER_SESSION", sql)

            Dim row As DataRow
            row = cashierSessionDetails.Tables(0).Rows(0)

            If (IsDBNull(row("END_TIME"))) Then
                isSessionEnded = False
                btnPost.Enabled = False
            Else
                isSessionEnded = True

            End If

            LoadSessionValues(idx)

        End If
    End Sub
    Private Sub BindGrid(grid As GridView, commandText As String)
        Dim dt As New DataTable()
        Dim myUtil As New Utilities()

        Using adt As New OleDbDataAdapter(commandText, myUtil.ConnectString)
            adt.SelectCommand.CommandTimeout = 300
            adt.Fill(dt)
        End Using
        grid.DataSource = dt
        grid.DataBind()
        'With grid
        '    .DataSource = dt
        '    .DataBind()
        'End With
    End Sub
    Public Sub ViewSendUnsecured()
        Dim ViewSendUnsecuredSQL As String = String.Format("SELECT * from  genii_user.TAX_ACCOUNT " & _
                                                        " WHERE TAX_ACCOUNT.SecuredUnsecured = 'U' AND ACCOUNT_BALANCE > 0 AND ACCOUNT_STATUS <> 7")

        BindGrid(Me.grdViewSendUnsecured, ViewSendUnsecuredSQL)

        Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewSendUnsecuredActions('Testing');", True)
    End Sub
    Public Sub ViewReturnedChecks()
        'Dim ViewReturnedChecksSQL As String = "SELECT * FROM genii_user.CASHIER_TRANSACTIONS" ' WHERE transaction_status=1"

        'BindGrid(Me.grdReturnedChecks, ViewReturnedChecksSQL)

        Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewReturnedChecksActions('Testing');", True)
    End Sub

    'Public Sub ViewRefunds()
    '    Dim ViewRefundsSQL As String = String.Format("SELECT CONVERT(varchar, genii_user.CASHIER_TRANSACTIONS.RECORD_ID) + ' (' " & _
    '                                                "    + Convert(varchar, genii_user.CASHIER_TRANSACTIONS.GROUP_KEY) " & _
    '                                                "     + ')' AS 'Transaction', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR + ' ('  " & _
    '                                                "     + genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER " & _
    '                                                "     + ')' AS 'Year (Roll)', " & _
    '                                                "   CASE genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS " & _
    '                                                "     WHEN 1 THEN 'Not Posted' " & _
    '                                                "     WHEN 2 THEN 'Posted' " & _
    '                                                "     WHEN 3 THEN 'Canceled Prior' " & _
    '                                                "     WHEN 4 THEN 'Reversed' " & _
    '                                                "       END AS 'Status', " & _
    '                                                "   CONVERT(varchar(10), genii_user.CASHIER_TRANSACTIONS.PAYMENT_DATE, 101) AS 'Date', " & _
    '                                                "   genii_user.ST_APPLY_PAYMENT_TO.APPLY_TO AS 'Apply To', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYOR_NAME AS 'Name', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT AS 'Payment', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_AMT AS 'Tax', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.REFUND_AMT AS 'Refund' " & _
    '                                                "         FROM genii_user.CASHIER_TRANSACTIONS " & _
    '                                                "   INNER JOIN genii_user.ST_APPLY_PAYMENT_TO " & _
    '                                                "     ON genii_user.CASHIER_TRANSACTIONS.APPLY_TO = genii_user.ST_APPLY_PAYMENT_TO.RECORD_ID " & _
    '                                                " WHERE     REFUND_TAG = 1 AND REFUND_AMT > 0")

    '    BindGrid(Me.grdProcessRefunds, ViewRefundsSQL)


    '    'Dim ViewRefundsSQL2 As String = String.Format("WITH PAYMENTS (TAX_YEAR, TAX_ROLL_NUMBER, Payments) AS " & _
    '    '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER, " & _
    '    '                                                "   SUM(genii_user.TR_PAYMENTS.PaymentAmount) AS 'Payment' " & _
    '    '                                                " FROM  " & _
    '    '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
    '    '                                                " 		INNER JOIN genii_user.TR_PAYMENTS " & _
    '    '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_PAYMENTS.TaxYear  " & _
    '    '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_PAYMENTS.TaxRollNumber " & _
    '    '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
    '    '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER), " & _
    '    '                                                " APPORTION (RECORD_ID, GROUP_KEY, TRANSACTION_STATUS, APPLY_TO, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_AMT, Apportioned) AS " & _
    '    '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.RECORD_ID, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.GROUP_KEY, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.APPLY_TO, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT, " & _
    '    '                                                "   SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS 'Apportioned' " & _
    '    '                                                " FROM  " & _
    '    '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
    '    '                                                " 	INNER JOIN genii_user.CASHIER_APPORTION  " & _
    '    '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.CASHIER_APPORTION.TaxYear  " & _
    '    '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.CASHIER_APPORTION.TaxRollNumber " & _
    '    '                                                " WHERE     (genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1) " & _
    '    '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.RECORD_ID, genii_user.CASHIER_TRANSACTIONS.GROUP_KEY,  " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.APPLY_TO, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT), " & _
    '    '                                                " CHARGES (TAX_YEAR, TAX_ROLL_NUMBER, Charged) AS " & _
    '    '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
    '    '                                                "   SUM(genii_user.TR_CHARGES.ChargeAmount) AS 'Charged' " & _
    '    '                                                " FROM  " & _
    '    '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
    '    '                                                " 	INNER JOIN genii_user.TR_CHARGES  " & _
    '    '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CHARGES.TaxYear  " & _
    '    '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CHARGES.TaxRollNumber " & _
    '    '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
    '    '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER), " & _
    '    '                                                " REFUND (TAX_YEAR, TAX_ROLL_NUMBER, Refund) AS " & _
    '    '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
    '    '                                                "   SUM(genii_user.TR_CHARGES.ChargeAmount) AS 'Refund' " & _
    '    '                                                " FROM  " & _
    '    '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
    '    '                                                " 	INNER JOIN genii_user.TR_CHARGES  " & _
    '    '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CHARGES.TaxYear  " & _
    '    '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CHARGES.TaxRollNumber " & _
    '    '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
    '    '                                                "   AND genii_user.TR_CHARGES.TaxChargeCodeID in (99922,99932) " & _
    '    '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER) " & _
    '    '                                                " SELECT CONVERT(varchar, APPORTION.RECORD_ID) + ' ('  " & _
    '    '                                                "     + CONVERT(varchar, APPORTION.GROUP_KEY) + ')' AS 'Trans (Group)', " & _
    '    '                                                "   APPORTION.TAX_YEAR + ' (' + " & _
    '    '                                                "   APPORTION.TAX_ROLL_NUMBER + ')' AS 'Year (Roll)', " & _
    '    '                                                "                             Case APPORTION.TRANSACTION_STATUS " & _
    '    '                                                "     WHEN 1 THEN 'Not Posted' " & _
    '    '                                                "     WHEN 2 THEN 'Posted' " & _
    '    '                                                "     WHEN 3 THEN 'Canceled Prior' " & _
    '    '                                                "     WHEN 4 THEN 'Reversed' " & _
    '    '                                                "       END AS 'Status', " & _
    '    '                                                "   genii_user.ST_APPLY_PAYMENT_TO.APPLY_TO AS 'Apply To', " & _
    '    '                                                "   APPORTION.PAYMENT_AMT AS 'Redeem Payment', " & _
    '    '                                                "   CHARGES.Charged AS 'Charges', " & _
    '    '                                                "   PAYMENTS.Payments AS 'Payments', " & _
    '    '                                                "   APPORTION.Apportioned AS 'Apportioned', " & _
    '    '                                                "   REFUND.Refund AS 'Refund' " & _
    '    '                                                "         from APPORTION " & _
    '    '                                                "    INNER JOIN PAYMENTS " & _
    '    '                                                "      ON APPORTION.Tax_YEAR=PAYMENTS.Tax_YEAR " & _
    '    '                                                "        AND APPORTION.TAX_ROLL_NUMBER=PAYMENTS.TAX_ROLL_NUMBER " & _
    '    '                                                "    INNER JOIN CHARGES " & _
    '    '                                                "      ON APPORTION.Tax_YEAR=CHARGES.Tax_YEAR " & _
    '    '                                                "        AND APPORTION.TAX_ROLL_NUMBER=CHARGES.TAX_ROLL_NUMBER " & _
    '    '                                                "    INNER JOIN REFUND " & _
    '    '                                                "      ON APPORTION.Tax_YEAR=REFUND.Tax_YEAR " & _
    '    '                                                "        AND APPORTION.TAX_ROLL_NUMBER=REFUND.TAX_ROLL_NUMBER " & _
    '    '                                                "    INNER JOIN genii_user.ST_APPLY_PAYMENT_TO " & _
    '    '                                                "      ON APPORTION.APPLY_TO = genii_user.ST_APPLY_PAYMENT_TO.RECORD_ID;")

    '    'BindGrid(Me.grdCPRefunds, ViewRefundsSQL2)


    '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewRefundsActions('Testing');", True)
    'End Sub


    'Public Sub ViewRefunds2()
    '    Dim ViewRefundsSQL As String = String.Format("SELECT CONVERT(varchar, genii_user.CASHIER_TRANSACTIONS.RECORD_ID) + ' (' " & _
    '                                                "    + Convert(varchar, genii_user.CASHIER_TRANSACTIONS.GROUP_KEY) " & _
    '                                                "     + ')' AS 'Transaction', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR + ' ('  " & _
    '                                                "     + genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER " & _
    '                                                "     + ')' AS 'Year (Roll)', " & _
    '                                                "   CASE genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS " & _
    '                                                "     WHEN 1 THEN 'Not Posted' " & _
    '                                                "     WHEN 2 THEN 'Posted' " & _
    '                                                "     WHEN 3 THEN 'Canceled Prior' " & _
    '                                                "     WHEN 4 THEN 'Reversed' " & _
    '                                                "       END AS 'Status', " & _
    '                                                "   CONVERT(varchar(10), genii_user.CASHIER_TRANSACTIONS.PAYMENT_DATE, 101) AS 'Date', " & _
    '                                                "   genii_user.ST_APPLY_PAYMENT_TO.APPLY_TO AS 'Apply To', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYOR_NAME AS 'Name', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT AS 'Payment', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_AMT AS 'Tax', " & _
    '                                                "   genii_user.CASHIER_TRANSACTIONS.REFUND_AMT AS 'Refund' " & _
    '                                                "         FROM genii_user.CASHIER_TRANSACTIONS " & _
    '                                                "   INNER JOIN genii_user.ST_APPLY_PAYMENT_TO " & _
    '                                                "     ON genii_user.CASHIER_TRANSACTIONS.APPLY_TO = genii_user.ST_APPLY_PAYMENT_TO.RECORD_ID " & _
    '                                                " WHERE     REFUND_TAG = 1 AND REFUND_AMT > 0")


    '    BindGrid(Me.grdProcessRefunds2, ViewRefundsSQL)

    '    'Dim ViewRefundsSQL2 As String = String.Format("WITH PAYMENTS (TAX_YEAR, TAX_ROLL_NUMBER, Payments) AS " & _
    '    '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER, " & _
    '    '                                                "   SUM(genii_user.TR_PAYMENTS.PaymentAmount) AS 'Payment' " & _
    '    '                                                " FROM  " & _
    '    '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
    '    '                                                " 		INNER JOIN genii_user.TR_PAYMENTS " & _
    '    '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_PAYMENTS.TaxYear  " & _
    '    '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_PAYMENTS.TaxRollNumber " & _
    '    '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
    '    '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER), " & _
    '    '                                                " APPORTION (RECORD_ID, GROUP_KEY, TRANSACTION_STATUS, APPLY_TO, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_AMT, Apportioned) AS " & _
    '    '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.RECORD_ID, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.GROUP_KEY, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.APPLY_TO, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT, " & _
    '    '                                                "   SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS 'Apportioned' " & _
    '    '                                                " FROM  " & _
    '    '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
    '    '                                                " 	INNER JOIN genii_user.CASHIER_APPORTION  " & _
    '    '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.CASHIER_APPORTION.TaxYear  " & _
    '    '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.CASHIER_APPORTION.TaxRollNumber " & _
    '    '                                                " WHERE     (genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1) " & _
    '    '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.RECORD_ID, genii_user.CASHIER_TRANSACTIONS.GROUP_KEY,  " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.APPLY_TO, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT), " & _
    '    '                                                " CHARGES (TAX_YEAR, TAX_ROLL_NUMBER, Charged) AS " & _
    '    '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
    '    '                                                "   SUM(genii_user.TR_CHARGES.ChargeAmount) AS 'Charged' " & _
    '    '                                                " FROM  " & _
    '    '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
    '    '                                                " 	INNER JOIN genii_user.TR_CHARGES  " & _
    '    '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CHARGES.TaxYear  " & _
    '    '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CHARGES.TaxRollNumber " & _
    '    '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
    '    '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER), " & _
    '    '                                                " REFUND (TAX_YEAR, TAX_ROLL_NUMBER, Refund) AS " & _
    '    '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
    '    '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
    '    '                                                "   SUM(genii_user.TR_CHARGES.ChargeAmount) AS 'Refund' " & _
    '    '                                                " FROM  " & _
    '    '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
    '    '                                                " 	INNER JOIN genii_user.TR_CHARGES  " & _
    '    '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CHARGES.TaxYear  " & _
    '    '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CHARGES.TaxRollNumber " & _
    '    '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
    '    '                                                "   AND genii_user.TR_CHARGES.TaxChargeCodeID in (99922,99932) " & _
    '    '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER) " & _
    '    '                                                " SELECT CONVERT(varchar, APPORTION.RECORD_ID) + ' ('  " & _
    '    '                                                "     + CONVERT(varchar, APPORTION.GROUP_KEY) + ')' AS 'Trans (Group)', " & _
    '    '                                                "   APPORTION.TAX_YEAR + ' (' + " & _
    '    '                                                "   APPORTION.TAX_ROLL_NUMBER + ')' AS 'Year (Roll)', " & _
    '    '                                                "                             Case APPORTION.TRANSACTION_STATUS " & _
    '    '                                                "     WHEN 1 THEN 'Not Posted' " & _
    '    '                                                "     WHEN 2 THEN 'Posted' " & _
    '    '                                                "     WHEN 3 THEN 'Canceled Prior' " & _
    '    '                                                "     WHEN 4 THEN 'Reversed' " & _
    '    '                                                "       END AS 'Status', " & _
    '    '                                                "   genii_user.ST_APPLY_PAYMENT_TO.APPLY_TO AS 'Apply To', " & _
    '    '                                                "   APPORTION.PAYMENT_AMT AS 'Redeem Payment', " & _
    '    '                                                "   CHARGES.Charged AS 'Charges', " & _
    '    '                                                "   PAYMENTS.Payments AS 'Payments', " & _
    '    '                                                "   APPORTION.Apportioned AS 'Apportioned', " & _
    '    '                                                "   REFUND.Refund AS 'Refund' " & _
    '    '                                                "         from APPORTION " & _
    '    '                                                "    INNER JOIN PAYMENTS " & _
    '    '                                                "      ON APPORTION.Tax_YEAR=PAYMENTS.Tax_YEAR " & _
    '    '                                                "        AND APPORTION.TAX_ROLL_NUMBER=PAYMENTS.TAX_ROLL_NUMBER " & _
    '    '                                                "    INNER JOIN CHARGES " & _
    '    '                                                "      ON APPORTION.Tax_YEAR=CHARGES.Tax_YEAR " & _
    '    '                                                "        AND APPORTION.TAX_ROLL_NUMBER=CHARGES.TAX_ROLL_NUMBER " & _
    '    '                                                "    INNER JOIN REFUND " & _
    '    '                                                "      ON APPORTION.Tax_YEAR=REFUND.Tax_YEAR " & _
    '    '                                                "        AND APPORTION.TAX_ROLL_NUMBER=REFUND.TAX_ROLL_NUMBER " & _
    '    '                                                "    INNER JOIN genii_user.ST_APPLY_PAYMENT_TO " & _
    '    '                                                "      ON APPORTION.APPLY_TO = genii_user.ST_APPLY_PAYMENT_TO.RECORD_ID")


    '    'BindGrid(Me.grdCPRefunds2, ViewRefundsSQL2)


    'End Sub

    Public Sub ViewForeclosures()
        Dim ViewForeclosuresSQL As String = String.Format("SELECT genii_user.TR_CP.APN as APN, " & _
                                                       "  genii_user.TR_CP.TaxYear AS TaxYear, " & _
                                                       "  genii_user.TR.OWNER_NAME_1 AS OWNER_NAME, " & _
                                                       "  ISNULL(genii_user.TR.MAIL_ADDRESS_1, '') " & _
                                                       "    + ' ' + ISNULL(genii_user.TR.MAIL_ADDRESS_2, '') " & _
                                                       "    + ', ' + genii_user.TR.MAIL_CITY + ' ' + genii_user.TR.MAIL_STATE + ' ' + genii_user.TR.MAIL_CODE AS OWNER_ADDRESS, " & _
                                                       "  ISNULL(genii_user.TR.MAIL_ADDRESS_1, '') + ' ' + ISNULL(genii_user.TR.MAIL_ADDRESS_2, '') AS MAIL_ADDRESS, " & _
                                                       "  genii_user.TR.MAIL_CITY AS MAIL_CITY, " & _
                                                       "        genii_user.TR.MAIL_STATE, " & _
                                                       "  genii_user.TR.MAIL_CODE AS MAIL_ZIP, " & _
                                                       "  0 AS TITLE_STATUS, 0 AS CERTIFIED_LETTER_STATUS,  " & _
                                                       "  0 AS FORECLOSURE_STATUS, " & _
                                                       "        'genii_user' AS CREATE_USER, " & _
                                                       "  GETDATE() AS CREATE_DATE, " & _
                                                       "        'genii_user' AS EDIT_USER, " & _
                                                       "  GETDATE() AS EDIT_DATE " & _
                                                       " FROM         genii_user.TR_CP INNER JOIN " & _
                                                       "                      genii_user.TR ON genii_user.TR_CP.TaxYear = genii_user.TR.TaxYear AND  " & _
                                                       "                      genii_user.TR_CP.TaxRollNumber = genii_user.TR.TaxRollNumber INNER JOIN " & _
                                                       "                      genii_user.TAX_ACCOUNT ON genii_user.TR_CP.APN = genii_user.TAX_ACCOUNT.APN " & _
                                                       " WHERE     genii_user.TR_CP.TaxYear = DATEPART(yyyy, GETDATE()) - 7 AND genii_user.TR_CP.CP_STATUS = 2")




        BindGrid(Me.grdViewForeclosures, ViewForeclosuresSQL)

        Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewForeclosuresActions('Testing');", True)

        'Me.txtForeclosures.Text = "22222222222"
        'Dim dtForeclosures As New DataTable()
        'Dim myUtil As New Utilities()
        '' Dim ds As New DataSet()

        'Using adt As New OleDbDataAdapter(ViewForeclosuresSQL, myUtil.ConnectString)
        '    adt.SelectCommand.CommandTimeout = 300
        '    adt.Fill(dtForeclosures)
        '    ' adt.Fill(ds)
        'End Using

        'Dim page As Page = HttpContext.Current.Handler
        ''Dim lblFdBk As Label = page.FindControl("lblFeedback")
        'Dim myGrid As New GridView
        'myGrid = FindControl("grdViewForeclosures")


        ''If (dtForeclosures.Rows.Count > 0) Then
        ''    ' If (myGrid.DataSource = Nothing) Then
        ''    myGrid.DataSource = dtForeclosures
        ''    myGrid.DataBind()
        ''    'End If

        ''    ' myGrid.DataSource = dtForeclosures
        ''    '  myGrid.DataBind()
        ''End If

        'With Me.grdViewForeclosures
        '    Me.grdViewForeclosures.DataSource = dtForeclosures
        '    Me.grdViewForeclosures.DataBind()
        'End With




    End Sub

    <System.Web.Services.WebMethod()> _
    Public Shared Function btnViewForeclosures_click() As Boolean
        'Me.divViewForeclosures.Attributes.Add("style", "display:block")
        ' ClientScript.RegisterStartupScript(Me."",""[GetType](), "ShowDiv", "document.getElementById('divViewForeclosures').style.display=''", True)

        Dim thisPage As New TaxSupervisor()
        thisPage.ViewForeclosures()

        Return True

    End Function

    '
    <System.Web.Services.WebMethod()> _
    Public Shared Function btnCommitSendUnsecured_click() As Boolean

        Dim myUtil As New Utilities()
        Dim page As Page = HttpContext.Current.Handler
        '   Dim lblFdBk As Label = page.FindControl("lblFeedback")

        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdTaxAccount As New OleDbCommand("UPDATE genii_user.TAX_ACCOUNT SET ACCOUNT_STATUS = 7 WHERE TAX_ACCOUNT.SecuredUnsecured = 'U' AND ACCOUNT_BALANCE > 0")

                cmdTaxAccount.Connection = conn
                cmdTaxAccount.Transaction = trans

                'With cmdTaxAccount.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdTaxAccount.ExecuteNonQuery()
                '    lblFdBk.Text = "Unsecured Delinquent accounts sent to the Sheriff."


                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
            conn.Close()
        End Using

        Return True

    End Function
    'Public Sub chkProcessRefunds(sender As Object, e As EventArgs)

    '    Dim total As Double = 0

    '    Dim v As Integer = grdProcessRefunds.Rows.Count
    '    Dim x As Integer = 0
    '    Dim y As Integer = grdProcessRefunds.Rows.Count
    '    Dim z As Integer = 0
    '    Dim ctr As Integer = 0

    '    'For z = 0 To (v - 1)
    '    '    Dim chkRefunds As CheckBox = grdProcessRefunds.HeaderRow.FindControl("chkRefundsSelectAll") 'grdProcessRefunds.Rows(z).FindControl("chkRefunds")
    '    '    If (chkRefunds.Checked) Then
    '    '        ctr = ctr + 1

    '    '        For x = 0 To (y - 1)
    '    '            Dim chk As CheckBox = grdProcessRefunds.Rows(x).FindControl("chkRefunds")
    '    '            chk.Checked = True
    '    '        Next


    '    '    Else
    '    '        For x = 0 To (y - 1)
    '    '            Dim chk As CheckBox = grdProcessRefunds.Rows(x).FindControl("chkRefunds")
    '    '            chk.Checked = False
    '    '        Next

    '    '    End If
    '    'Next

    '    'For x = 0 To (y - 1)
    '    '    Dim chk As CheckBox = grdProcessRefunds.Rows(x).FindControl("chkBoxRefunds")
    '    '    If (chk.Checked) Then
    '    '        Dim a As String = "aa"
    '    '        Dim b As String = "BB"
    '    '    End If
    '    'Next

    'End Sub

    'Public Sub LocateReturnedChecksSearch()
    '    If (txtTaxID.Text <> String.Empty) Then
    '        rdoTaxID.Checked = True

    '    End If

    'End Sub


    'Public Sub searchReturnedChecks_click(ByVal sender As Object, ByVal e As System.EventArgs) Handles searchReturnedChecks.Click
    '    Dim where_clause As String = String.Empty
    '    Dim assignedIDs As String = Request.Form("hdnTxtValue")


    '    ''   rdoPayor.Checked = True
    '    'If (Me.rdoTaxID.Checked = True) Then
    '    '    If (txtTaxID.Text <> String.Empty) Then
    '    '        where_clause = "where TaxIDNumber= '" + hdnTextValueTaxID.Value + "' "
    '    '    End If

    '    'ElseIf (Me.rdoCheckNum.Checked = True) Then
    '    '    If (txtCheckNum.Text <> String.Empty) Then
    '    '        where_clause = "where Check_number like '%" + hdnTextValueCheckNum.Value + "%' "
    '    '    End If
    '    'ElseIf (Me.rdoPayor.Checked = True) Then
    '    '    If (txtPayor.Text <> String.Empty) Then
    '    '        where_clause = "where payor_name like '%" + hdnTextValuePayor.Value + "%' "
    '    '    End If
    '    'Else
    '    '    where_clause = ""
    '    'End If

    '    If (hdnTextValueTaxID.Value <> String.Empty) Then
    '        where_clause = "where TaxIDNumber= '" + hdnTextValueTaxID.Value + "' "
    '    ElseIf (hdnTextValueCheckNum.Value <> String.Empty) Then
    '        where_clause = "where Check_number like '%" + hdnTextValueCheckNum.Value + "%' "
    '    ElseIf (hdnTextValuePayor.Value <> String.Empty) Then
    '        where_clause = "where payor_name like '%" + hdnTextValuePayor.Value + "%' "
    '    Else
    '        where_clause = ""
    '    End If


    '    'where_clause = " where  TaxIDNumber= '" + hdnTextValueTaxID.Value + "' or Check_number like '%" + hdnTextValueCheckNum.Value + "%' or payor_name like '%" + hdnTextValuePayor.Value + "%'"

    '    Dim ViewReturnedChecksSQL As String = "SELECT * FROM genii_user.CASHIER_TRANSACTIONS " & where_clause  ' WHERE transaction_status=1"

    '    BindGrid(Me.grdReturnedChecks, ViewReturnedChecksSQL)

    '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewReturnedChecksActions('Testing');", True)
    'End Sub

    'Public Sub searchReturnedChecks_click2()
    '    Dim where_clause As String = String.Empty
    '    Dim assignedIDs As String = Request.Form("hdnTxtValue")


    '    '   rdoPayor.Checked = True
    '    If (Me.radioTaxIDSearch.Checked = True) Then
    '        If (txtTaxIDSearch.Text <> String.Empty) Then
    '            where_clause = "where TaxIDNumber= '" + txtTaxIDSearch.Text + "' "
    '        End If

    '    ElseIf (Me.radioCheckNumberSearch.Checked = True) Then
    '        If (txtCheckNumberSearch.Text <> String.Empty) Then
    '            where_clause = "where Check_number like '%" + txtCheckNumberSearch.Text + "%' "
    '        End If
    '    ElseIf (Me.radioPayorSearch.Checked = True) Then
    '        If (txtPayorSearch.Text <> String.Empty) Then
    '            where_clause = "where payor_name like '%" + txtPayorSearch.Text + "%' "
    '        End If
    '    Else
    '        where_clause = ""
    '    End If

    '    Dim ViewReturnedChecksSQL As String = "SELECT * FROM genii_user.CASHIER_TRANSACTIONS " & where_clause  ' WHERE transaction_status=1"

    '    BindGrid(Me.grdReturnedChecks2, ViewReturnedChecksSQL)

    '    Me.tabContainer.ActiveTabIndex = 0


    '    '   Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewReturnedChecksActions('Testing');", True)
    'End Sub

    'Public Sub ProcessReturnedChecks()
    '    Dim myUtil As New Utilities()
    '    Dim y As Integer = grdProcessRefunds.Rows.Count
    '    Dim z As Integer = grdCPRefunds.Rows.Count
    '    Dim transID As Integer
    '    Dim taxYear As String
    '    Dim taxRollNumber As String
    '    Dim paymentAmount As Double

    '    For Each gvr As GridViewRow In grdCPRefunds.Rows
    '        Dim chk As HtmlInputCheckBox = grdCPRefunds.FindControl("chkCPRefunds")

    '        Dim cbox As HtmlInputCheckBox = grdCPRefunds.Rows(gvr.RowIndex).Cells(0).FindControl("chkCPRefunds")

    '        If (cbox.Value <> String.Empty) Then
    '            Dim a As String = "a"
    '            Dim b As String = "B"

    '        End If
    '    Next


    '    For x = 0 To (y - 1)
    '        Dim cbox As HtmlInputCheckBox = grdProcessRefunds.Rows(x).Cells(0).FindControl("chkRefunds")




    '        Dim chk As HtmlInputCheckBox = grdProcessRefunds.Rows(x).FindControl("chkRefunds")
    '        '   chk = grdProcessRefunds.Controls(x).FindControl("chkRefunds")
    '        Dim a As HtmlInputCheckBox = grdProcessRefunds.Controls.Item(x).FindControl("chkRefunds")
    '        Dim j As Integer = grdProcessRefunds.Controls.Count

    '        '  chk = grdProcessRefunds.Rows(x).FindControl("chkRefunds")
    '        'chk.FindControl("chkRefunds")
    '        'Dim a As String = Request.Form("chkRefunds")
    '        If (chk.Checked) Then
    '            transID = grdProcessRefunds.Rows(x).Cells(1).Text
    '            paymentAmount = CDec(grdProcessRefunds.Rows(x).Cells(5).Text)
    '            taxYear = grdProcessRefunds.Rows(x).Cells(2).Text
    '            taxRollNumber = grdProcessRefunds.Rows(x).Cells(3).Text

    '            Using conn As New OleDbConnection(myUtil.ConnectString)
    '                conn.Open()

    '                Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

    '                Try
    '                    Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.TR_PAYMENTS " & _
    '                                                  "(TRANS_ID, TaxYear, TaxRollNumber, PaymentEffectiveDate, " & _
    '                                                  " PaymentTypeCode,PaymentMadeByCode,Pertinent1, " & _
    '                                                  " Pertinent2, PaymentAmount,  " & _
    '                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
    '                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

    '                    cmdNewRecPayments.Connection = conn
    '                    cmdNewRecPayments.Transaction = trans


    '                    With cmdNewRecPayments.Parameters
    '                        .AddWithValue("@TRANS_ID", transID)
    '                        .AddWithValue("@TaxYear", taxYear)
    '                        .AddWithValue("@TaxRollNumber", taxRollNumber)
    '                        .AddWithValue("@PaymentEffectiveDate", Date.Now)
    '                        .AddWithValue("@PaymentTypeCode", 6)
    '                        .AddWithValue("@PaymentMadeByCode", 5)
    '                        .AddWithValue("@Pertinent1", "CP/Investor Refund.")
    '                        .AddWithValue("@Pertinent2", "Scheduled by Banking System.")
    '                        .AddWithValue("@PaymentAmount", paymentAmount)

    '                        '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
    '                        .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
    '                        .AddWithValue("@EDIT_DATE", Date.Now)
    '                        .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                        .AddWithValue("@CREATE_DATE", Date.Now)

    '                    End With

    '                    cmdNewRecPayments.ExecuteNonQuery()


    '                    Dim cmdNewRecCharges As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
    '                                                                     "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
    '                                                                     " TaxTypeID,ChargeAmount, " & _
    '                                                                     " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
    '                                                                     " VALUES (?,?,?,?,?,?,?,?,?)")

    '                    cmdNewRecCharges.Connection = conn
    '                    cmdNewRecCharges.Transaction = trans

    '                    With cmdNewRecCharges.Parameters
    '                        .AddWithValue("@TaxYear", taxYear)
    '                        .AddWithValue("@TaxRollNumber", taxRollNumber)
    '                        .AddWithValue("@TaxChargeCodeID", 99932)
    '                        .AddWithValue("@TaxTypeID", 99)
    '                        .AddWithValue("@ChargeAmount", paymentAmount)

    '                        .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                        .AddWithValue("@EDIT_DATE", Date.Now)
    '                        .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                        .AddWithValue("@CREATE_DATE", Date.Now)

    '                    End With

    '                    cmdNewRecCharges.ExecuteNonQuery()

    '                    trans.Commit()

    '                Catch ex As Exception
    '                    trans.Rollback()
    '                    Throw ex
    '                End Try
    '                conn.Close()
    '            End Using
    '        End If

    '    Next

    '    For x = 0 To (z - 1)

    '    Next





    'End Sub

    'Public Sub ProcessReturnedChecks2()
    '    Dim myUtil As New Utilities()
    '    Dim y As Integer = grdProcessRefunds2.Rows.Count
    '    Dim z As Integer = grdCPRefunds2.Rows.Count
    '    Dim transID As String
    '    Dim taxYear As String
    '    Dim taxRollNumber As String
    '    Dim paymentAmount As Double


    '    For x = 0 To (y - 1)
    '        '  Dim cbox As HtmlInputCheckBox = grdProcessRefunds.Rows(x).Cells(0).FindControl("chkRefunds2")
    '        Dim chk As CheckBox = grdProcessRefunds2.Rows(x).FindControl("chkRefunds2")

    '        If (chk.Checked) Then
    '            transID = grdProcessRefunds2.Rows(x).Cells(1).Text
    '            transID = transID.Substring(transID.Length - 3, 2)
    '            paymentAmount = CDec(grdProcessRefunds2.Rows(x).Cells(9).Text)
    '            'taxYear = grdProcessRefunds2.Rows(x).Cells(2).Text
    '            'taxYear = taxYear.Substring(0, 4)
    '            'taxRollNumber = taxYear.Substring(taxYear.Length - 1, )

    '            Using conn As New OleDbConnection(myUtil.ConnectString)
    '                conn.Open()

    '                Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

    '                Try
    '                    Dim refundDetails As DataSet = New DataSet()

    '                    Dim Sql = String.Format("SELECT genii_user.CASHIER_TRANSACTIONS.tax_year, genii_user.CASHIER_TRANSACTIONS.tax_roll_number,GETDATE() AS QUEUE_DATE, " & _
    '                                            "  genii_user.CASHIER_TRANSACTIONS.REFUND_AMT AS REFUND_AMOUNT, " & _
    '                                            "   'Payment Refund ' + genii_user.CASHIER_TRANSACTIONS.TAX_YEAR + ' - ' + genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS 'MEMO', " & _
    '                                            "   genii_user.TR.OWNER_NAME_1 AS 'VENDOR_NAME', " & _
    '                                            "   genii_user.TR.MAIL_ADDRESS_1 AS ADDRESS_1, " & _
    '                                            "   genii_user.TR.MAIL_ADDRESS_2, AS ADDRESS_2" & _
    '                                            "   genii_user.TR.MAIL_CITY AS 'CITY', " & _
    '                                            "   genii_user.TR.MAIL_STATE AS 'STATE', " & _
    '                                            "   genii_user.TR.MAIL_CODE AS 'ZIP', " & _
    '                                            "   0 AS SENT_TO_OTHER_SYSTEM, " & _
    '                                            "  '" + System.Web.HttpContext.Current.User.Identity.Name + "' AS CREATE_USER, " & _
    '                                            "   GETDATE() AS CREATE_DATE, " & _
    '                                            "   '" + System.Web.HttpContext.Current.User.Identity.Name + "' AS EDIT_USER, " & _
    '                                            "   GETDATE() AS EDIT_DATE " & _
    '                                            "   FROM genii_user.CASHIER_TRANSACTIONS " & _
    '                                            "  INNER JOIN genii_user.TR " & _
    '                                            "     ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR.TaxYear " & _
    '                                            "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR.TaxRollNumber " & _
    '                                            "                         WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
    '                                            "   AND genii_user.CASHIER_TRANSACTIONS.REFUND_AMT > 0 AND genii_user.CASHIER_TRANSACTIONS.GROUP_KEY=" + transID + " )")

    '                    LoadTable(refundDetails, "CASHIER_QUEUE_CHECK", Sql)

    '                    Dim row As DataRow
    '                    row = refundDetails.Tables(0).Rows(0)


    '                    Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_CHECK_QUEUE", conn, trans)

    '                    Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.CASHIER_CHECK_QUEUE " & _
    '                                                                     "(record_id, group_key, queue_date, " & _
    '                                                                     " refund_amount,memo,vendor_name, address_1, " & _
    '                                                                     " address_2,city,state, zip, sent_to_other_system, " & _
    '                                                                     " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
    '                                                                     " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")
    '                    cmdNewRecPayments.Connection = conn
    '                    cmdNewRecPayments.Transaction = trans


    '                    With cmdNewRecPayments.Parameters
    '                        .AddWithValue("@record_id", recordID)
    '                        .AddWithValue("@group_key", transID)
    '                        .AddWithValue("@queue_date", row("QUEUE_DATE"))
    '                        .AddWithValue("@refund_amount", row("REFUND_AMOUNT"))
    '                        .AddWithValue("@memo", row("memo"))
    '                        .AddWithValue("@vendor_name", row("vendor_name"))
    '                        .AddWithValue("@address_1", row("address_1"))
    '                        .AddWithValue("@address_2", row("address_2"))
    '                        .AddWithValue("@city", row("city"))
    '                        .AddWithValue("@state", row("state"))
    '                        .AddWithValue("@zip", row("zip"))
    '                        .AddWithValue("@sent_to_other_system", row("sent_to_other_system"))

    '                        '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
    '                        .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
    '                        .AddWithValue("@EDIT_DATE", Date.Now)
    '                        .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                        .AddWithValue("@CREATE_DATE", Date.Now)

    '                    End With

    '                    cmdNewRecPayments.ExecuteNonQuery()


    '                    trans.Commit()

    '                Catch ex As Exception
    '                    trans.Rollback()
    '                    Throw ex
    '                End Try
    '                conn.Close()
    '            End Using
    '        End If

    '    Next

    '    For x = 0 To (z - 1)
    '        Dim chk As CheckBox = grdCPRefunds2.Rows(x).FindControl("chkCPRefunds2")

    '        If (chk.Checked) Then
    '            transID = grdCPRefunds2.Rows(x).Cells(1).Text
    '            transID = transID.Substring(transID.Length - 3, 2)
    '            paymentAmount = CDec(grdCPRefunds2.Rows(x).Cells(9).Text)
    '            'taxYear = grdCPRefunds2.Rows(x).Cells(2).Text
    '            'taxRollNumber = grdCPRefunds2.Rows(x).Cells(3).Text

    '            Using conn As New OleDbConnection(myUtil.ConnectString)
    '                conn.Open()

    '                Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)


    '                Dim refundDetails As DataSet = New DataSet()

    '                Dim Sql = String.Format("SELECT genii_user.CASHIER_TRANSACTIONS.tax_year,genii_user.CASHIER_TRANSACTIONS.tax_roll_number,GETDATE() AS QUEUE_DATE, " & _
    '                                        "  genii_user.TR_CP.PurchaseValue + ISNULL(genii_user.TR_CP.INTEREST_EARNED, 0) AS 'REFUND_AMOUNT', " & _
    '                                        "                     'CP Redeem ' + genii_user.TR_CP.APN + ' - ' + CONVERT(varchar, genii_user.TR_CP.TaxYear) AS 'MEMO', " & _
    '                                        "   ISNULL(genii_user.ST_INVESTOR.FirstName, '') + ' ' " & _
    '                                        "   + ISNULL(genii_user.ST_INVESTOR.MiddleName, '') + ' ' " & _
    '                                        "   + genii_user.ST_INVESTOR.LastName AS 'VENDOR_NAME', " & _
    '                                        "   ISNULL(genii_user.ST_INVESTOR.Address1, '') AS 'ADDRESS_1', " & _
    '                                        "   ISNULL(genii_user.ST_INVESTOR.Address2, '') AS 'ADDRESS_2', " & _
    '                                        "   ISNULL(genii_user.ST_INVESTOR.City, '') AS 'CITY', " & _
    '                                        "   ISNULL(genii_user.ST_INVESTOR.State, '') AS 'STATE', " & _
    '                                        "   ISNULL(genii_user.ST_INVESTOR.PostalCode, '') AS 'ZIP', " & _
    '                                        "   0 AS SENT_TO_OTHER_SYSTEM, " & _
    '                                        "                     'genii_user' AS CREATE_USER, " & _
    '                                        "   GETDATE() AS CREATE_DATE, " & _
    '                                        "   'genii_user' AS EDIT_USER, " & _
    '                                        "   GETDATE() AS EDIT_DATE " & _
    '                                        "                     FROM genii_user.CASHIER_TRANSACTIONS " & _
    '                                        "   INNER JOIN genii_user.TR_CP " & _
    '                                        "     ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CP.TaxYear " & _
    '                                        "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CP.TaxRollNumber " & _
    '                                        "   INNER JOIN genii_user.ST_INVESTOR " & _
    '                                        "     ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " & _
    '                                        "   INNER JOIN genii_user.TR_CHARGES " & _
    '                                        "     ON genii_user.CASHIER_TRANSACTIONS.Tax_Year = genii_user.TR_CHARGES.TaxYear  " & _
    '                                        "       AND genii_user.CASHIER_TRANSACTIONS.Tax_ROLL_NUMBER = genii_user.TR_CHARGES.TaxRollNumber  " & _
    '                                        "                     WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
    '                                        "   AND genii_user.CASHIER_TRANSACTIONS.APPLY_TO = 2 " & _
    '                                        "   AND genii_user.TR_CHARGES.TaxChargeCodeID IN (99922,99932) and genii_user.CASHIER_TRANSACTIONS.group_key=" + transID + " ")

    '                LoadTable(refundDetails, "CASHIER_QUEUE_CHECK", Sql)

    '                Dim row As DataRow
    '                row = refundDetails.Tables(0).Rows(0)


    '                Try
    '                    Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.TR_PAYMENTS " & _
    '                                                  "(TRANS_ID, TaxYear, TaxRollNumber, PaymentEffectiveDate, " & _
    '                                                  " PaymentTypeCode,PaymentMadeByCode,Pertinent1, " & _
    '                                                  " Pertinent2, PaymentAmount,  " & _
    '                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
    '                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

    '                    cmdNewRecPayments.Connection = conn
    '                    cmdNewRecPayments.Transaction = trans


    '                    With cmdNewRecPayments.Parameters
    '                        .AddWithValue("@TRANS_ID", transID)
    '                        .AddWithValue("@TaxYear", row("tax_year"))
    '                        .AddWithValue("@TaxRollNumber", row("tax_roll_number"))
    '                        .AddWithValue("@PaymentEffectiveDate", Date.Now)
    '                        .AddWithValue("@PaymentTypeCode", 6)
    '                        .AddWithValue("@PaymentMadeByCode", 5)
    '                        .AddWithValue("@Pertinent1", "CP/Investor Refund.")
    '                        .AddWithValue("@Pertinent2", "Scheduled by Banking System.")
    '                        .AddWithValue("@PaymentAmount", paymentAmount)

    '                        '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
    '                        .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
    '                        .AddWithValue("@EDIT_DATE", Date.Now)
    '                        .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                        .AddWithValue("@CREATE_DATE", Date.Now)

    '                    End With

    '                    cmdNewRecPayments.ExecuteNonQuery()


    '                    Dim cmdNewRecCharges As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
    '                                                                     "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
    '                                                                     " TaxTypeID,ChargeAmount, " & _
    '                                                                     " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
    '                                                                     " VALUES (?,?,?,?,?,?,?,?,?)")

    '                    cmdNewRecCharges.Connection = conn
    '                    cmdNewRecCharges.Transaction = trans

    '                    With cmdNewRecCharges.Parameters
    '                        .AddWithValue("@TaxYear", row("tax_year"))
    '                        .AddWithValue("@TaxRollNumber", row("tax_roll_number"))
    '                        .AddWithValue("@TaxChargeCodeID", 99932)
    '                        .AddWithValue("@TaxTypeID", 99)
    '                        .AddWithValue("@ChargeAmount", paymentAmount)

    '                        .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                        .AddWithValue("@EDIT_DATE", Date.Now)
    '                        .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                        .AddWithValue("@CREATE_DATE", Date.Now)

    '                    End With

    '                    cmdNewRecCharges.ExecuteNonQuery()


    '                    Dim cmdUpdateTrans As New OleDbCommand("Update genii_user.Cashier_transactions set refund_tag =2, edit_user='" + System.Web.HttpContext.Current.User.Identity.Name + "', edit_date='" + Date.Now + "' where tax_year =" + row("tax_year") + " and tax_roll_Number= " + row("tax_roll_number") + " ")

    '                    cmdUpdateTrans.Connection = conn
    '                    cmdUpdateTrans.Transaction = trans

    '                    'With cmdUpdateTrans.Parameters
    '                    '    .AddWithValue("@TaxYear", taxYear)
    '                    '    .AddWithValue("@TaxRollNumber", taxRollNumber)
    '                    '    .AddWithValue("@TaxChargeCodeID", 99932)
    '                    '    .AddWithValue("@TaxTypeID", 99)
    '                    '    .AddWithValue("@ChargeAmount", paymentAmount)

    '                    '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                    '    .AddWithValue("@EDIT_DATE", Date.Now)
    '                    '    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                    '    .AddWithValue("@CREATE_DATE", Date.Now)

    '                    'End With

    '                    cmdUpdateTrans.ExecuteNonQuery()


    '                    Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_CHECK_QUEUE", conn, trans)

    '                    Dim cmdNewRecPayments2 As New OleDbCommand("INSERT INTO genii_user.CASHIER_CHECK_QUEUE " & _
    '                                                                     "(record_id, group_key, queue_date, " & _
    '                                                                     " refund_amount,memo,vendor_name, address_1, " & _
    '                                                                     " address_2,city,state, zip, sent_to_other_system, " & _
    '                                                                     " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
    '                                                                     " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")
    '                    cmdNewRecPayments2.Connection = conn
    '                    cmdNewRecPayments2.Transaction = trans


    '                    With cmdNewRecPayments2.Parameters
    '                        .AddWithValue("@record_id", recordID)
    '                        .AddWithValue("@group_key", transID)
    '                        .AddWithValue("@queue_date", row("QUEUE_DATE"))
    '                        .AddWithValue("@refund_amount", row("REFUND_AMOUNT"))
    '                        .AddWithValue("@memo", row("memo"))
    '                        .AddWithValue("@vendor_name", row("vendor_name"))
    '                        .AddWithValue("@address_1", row("address_1"))
    '                        .AddWithValue("@address_2", row("address_2"))
    '                        .AddWithValue("@city", row("city"))
    '                        .AddWithValue("@state", row("state"))
    '                        .AddWithValue("@zip", row("zip"))
    '                        .AddWithValue("@sent_to_other_system", row("sent_to_other_system"))

    '                        .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
    '                        .AddWithValue("@EDIT_DATE", Date.Now)
    '                        .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
    '                        .AddWithValue("@CREATE_DATE", Date.Now)

    '                    End With

    '                    cmdNewRecPayments2.ExecuteNonQuery()



    '                    trans.Commit()

    '                Catch ex As Exception
    '                    trans.Rollback()
    '                    Throw ex
    '                End Try
    '                conn.Close()
    '            End Using
    '        End If

    '    Next

    'End Sub

    'btnCommitForeclosures_click

    <System.Web.Services.WebMethod()> _
    Public Shared Function btnCommitForeclosures_click() As Boolean

        Dim myUtil As New Utilities()
        Dim page As Page = HttpContext.Current.Handler
        '    Dim lblFdBk As Label = page.FindControl("lblFeedback")

        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdDeeds As New OleDbCommand("INSERT INTO genii_user.DEED(APN,TaxYear,Owner_name,Owner_address,Mail_address,mail_city,Mail_state,Mail_zip,title_status, " & _
                                                        " certified_letter_status,foreclosure_status,create_user,create_date,edit_user,edit_date) " & _
                                                        " SELECT NULL AS RECORD_ID, " & _
                                                        "   genii_user.TR_CP.APN as APN, " & _
                                                        "   genii_user.TR_CP.TaxYear AS TaxYear, " & _
                                                        "   genii_user.TR.OWNER_NAME_1 AS OWNER_NAME, " & _
                                                        "   ISNULL(genii_user.TR.MAIL_ADDRESS_1, '') " & _
                                                        "     + ' ' + ISNULL(genii_user.TR.MAIL_ADDRESS_2, '') " & _
                                                        "     + ', ' + genii_user.TR.MAIL_CITY + ' ' + genii_user.TR.MAIL_STATE + ' ' + genii_user.TR.MAIL_CODE AS OWNER_ADDRESS, " & _
                                                        "   ISNULL(genii_user.TR.MAIL_ADDRESS_1, '') + ' ' + ISNULL(genii_user.TR.MAIL_ADDRESS_2, '') AS MAIL_ADDRESS, " & _
                                                        "   genii_user.TR.MAIL_CITY AS MAIL_CITY, " & _
                                                        "                 genii_user.TR.MAIL_STATE, " & _
                                                        "   genii_user.TR.MAIL_CODE AS MAIL_ZIP, " & _
                                                        "   genii_user.TAX_ACCOUNT.LEGAL AS MAIL_STATE, " & _
                                                        "   0 AS TITLE_STATUS, 0 AS CERTIFIED_LETTER_STATUS,  " & _
                                                        "   0 AS FORECLOSURE_STATUS, " & _
                                                        "                 'genii_user' AS CREATE_USER, " & _
                                                        "   GETDATE() AS CREATE_DATE, " & _
                                                        "                 'genii_user' AS EDIT_USER, " & _
                                                        "   GETDATE() AS EDIT_DATE " & _
                                                        " FROM         genii_user.TR_CP INNER JOIN " & _
                                                        "                       genii_user.TR ON genii_user.TR_CP.TaxYear = genii_user.TR.TaxYear AND genii_user.TR_CP.TaxRollNumber = genii_user.TR.TaxRollNumber INNER JOIN " & _
                                                        "                       genii_user.TAX_ACCOUNT ON genii_user.TR_CP.APN = genii_user.TAX_ACCOUNT.APN " & _
                                                        " WHERE     (genii_user.TR_CP.TaxYear = DATEPART(yyyy, GETDATE()) - 7) AND (genii_user.TR_CP.CP_STATUS = 2) ")

                cmdDeeds.Connection = conn
                cmdDeeds.Transaction = trans

                'With cmdTaxAccount.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdDeeds.ExecuteNonQuery()
                '  lblFdBk.Text = "Foreclosures Loaded."


                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
            conn.Close()
        End Using

        Return True
    End Function

    <System.Web.Services.WebMethod()> _
    Public Shared Function btnUpdateWebValues_click() As Boolean
        Dim myUtil As New Utilities()
        Dim page As Page = HttpContext.Current.Handler
        '  Dim lblFdBk As Label = page.FindControl("lblFeedback")

        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdTaxWebSpeed As New OleDbCommand("DROP TABLE genii_user.ST_WEB_DATA")

                cmdTaxWebSpeed.Connection = conn
                cmdTaxWebSpeed.Transaction = trans

                'With cmdTaxWebSpeed.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdTaxWebSpeed.ExecuteNonQuery()


                Dim cmdTaxWebSpeed2 As New OleDbCommand("SELECT * INTO genii_user.ST_WEB_DATA FROM NCIS_TREASURY.dbo.vWebData")

                cmdTaxWebSpeed2.Connection = conn
                cmdTaxWebSpeed2.Transaction = trans

                'With cmdTaxWebSpeed2.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdTaxWebSpeed2.ExecuteNonQuery()


                Dim cmdTaxWebSpeed3 As New OleDbCommand("DELETE FROM genii_user.ST_WEB_DATA WHERE Tax Year < 2007 AND Balance = '$0.00'")

                cmdTaxWebSpeed3.Connection = conn
                cmdTaxWebSpeed3.Transaction = trans

                'With cmdTaxWebSpeed3.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdTaxWebSpeed3.ExecuteNonQuery()

                Dim cmdTaxWebSpeed4 As New OleDbCommand("CREATE CLUSTERED INDEX CI_TaxIDNumber ON genii_user.ST_WEB_DATA (TaxIDNumber)")

                cmdTaxWebSpeed4.Connection = conn
                cmdTaxWebSpeed4.Transaction = trans

                'With cmdTaxWebSpeed4.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdTaxWebSpeed4.ExecuteNonQuery()
                '   lblFdBk.Text = "Updated Tax Web Speed Tables."


                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
            conn.Close()
        End Using

        Return True

    End Function

    <System.Web.Services.WebMethod()> _
    Public Shared Function btnCaptureLevy_click() As Boolean
        Dim myUtil As New Utilities()
        Dim page As Page = HttpContext.Current.Handler
        '  Dim lblFdBk As Label = page.FindControl("lblFeedback")

        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdCaptureLevy As New OleDbCommand("SELECT TaxYear, CaptureYear, CaptureMonth, CaptureType, TaxChargeCodeID,  " & _
                                                      " TaxTypeID, TotalLevyAmount, TotalPaidAmount, TotalOutstanding,  " & _
                                                      "  'genii_user' AS CREATE_USER, GETDATE() AS CREATE_DATE, " & _
                                                      "  'genii_user' AS EDIT_USER, GETDATE() AS EDIT_DATE INTO dbo.LEVY_TRANSFER " & _
                                                      "  FROM dbo.vLevyCapture ")

                cmdCaptureLevy.Connection = conn
                cmdCaptureLevy.Transaction = trans

                'With cmdCaptureLevy.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdCaptureLevy.ExecuteNonQuery()

                Dim cmdCaptureLevy2 As New OleDbCommand("INSERT INTO genii_user.LEVY_TOTALS SELECT * FROM dbo.LEVY_TRANSFER")

                cmdCaptureLevy2.Connection = conn
                cmdCaptureLevy2.Transaction = trans

                'With cmdCaptureLevy2.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdCaptureLevy2.ExecuteNonQuery()

                Dim cmdCaptureLevy3 As New OleDbCommand("DROP TABLE dbo.LEVY_TRANSFER")

                cmdCaptureLevy3.Connection = conn
                cmdCaptureLevy3.Transaction = trans

                'With cmdCaptureLevy3.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdCaptureLevy3.ExecuteNonQuery()



                ' lblFdBk.Text = "Capture Levy Done."


                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
            conn.Close()
        End Using

        Return True
    End Function

    'btnUpdateNightlyFunction_click

    <System.Web.Services.WebMethod()> _
    Public Shared Function btnUpdateNightlyFunction_click() As Boolean ''transID As String,

        Dim myUtil As New Utilities()
        Dim page As Page = HttpContext.Current.Handler
        '   Dim lblFdBk As Label = page.FindControl("lblFeedback")

        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdUptateTR1 As New OleDbCommand("UPDATE genii_user.TR " & _
                                                  " SET CurrentBalance = ?")

                cmdUptateTR1.Connection = conn
                cmdUptateTR1.Transaction = trans

                With cmdUptateTR1.Parameters

                    .AddWithValue("@CurrentBalance", 0.0)

                End With

                cmdUptateTR1.ExecuteNonQuery()

                '     lblFdBk.Text = "Step 1: Tax Roll Current has been reset to zero."

                Dim cmdUptateTR2 As New OleDbCommand("UPDATE genii_user.TR SET CurrentBalance = vCurrent_TR_Balance.Balance " & _
                                                               " FROM genii_user.TR  " & _
                                                       "  INNER JOIN vCurrent_TR_Balance " & _
                                                       "     ON genii_user.TR.TaxYear = vCurrent_TR_Balance.TaxYear " & _
                                                       "      AND genii_user.TR.TaxRollNumber = vCurrent_TR_Balance.TaxRollNumber")

                cmdUptateTR2.Connection = conn
                cmdUptateTR2.Transaction = trans

                'With cmdUptateTR2.Parameters

                '    .AddWithValue("@CurrentBalance", 0.0)

                'End With

                cmdUptateTR2.ExecuteNonQuery()
                '  lblFdBk.Text = "Step 2: Tax Roll Current Balance has been set to the current value."

                Dim cmdTRStatus As New OleDbCommand("Update genii_user.TR set genii_user.TR.STATUS=? ")

                cmdTRStatus.Connection = conn
                cmdTRStatus.Transaction = trans

                With cmdTRStatus.Parameters

                    .AddWithValue("@STATUS", 1)

                End With

                cmdTRStatus.ExecuteNonQuery()


                Dim cmdTR As New OleDbCommand("update genii_user.TR set genii_user.TR.STATUS = ? WHERE genii_user.TR.CurrentBalance = ?")

                cmdTR.Connection = conn
                cmdTR.Transaction = trans

                With cmdTR.Parameters
                    .AddWithValue("@STATUS", 2)
                    .AddWithValue("@CurrentBalance", 0.0)

                End With

                cmdTR.ExecuteNonQuery()


                Dim cmdTR2 As New OleDbCommand("update genii_user.TR set genii_user.TR.STATUS = ?  " & _
                                             " FROM genii_user.TR INNER JOIN genii_user.TR_CP  " & _
                                             "    ON genii_user.TR.TaxYear = genii_user.TR_CP.TaxYear  " & _
                                             "      AND genii_user.TR.TaxRollNumber = genii_user.TR_CP.TaxRollNumber " & _
                                             "            WHERE genii_user.TR_CP.CertificateNumber Is Not NULL ")

                cmdTR2.Connection = conn
                cmdTR2.Transaction = trans

                With cmdTR2.Parameters
                    .AddWithValue("@STATUS", 3)

                End With

                cmdTR2.ExecuteNonQuery()


                Dim cmdTR3 As New OleDbCommand("update genii_user.TR set genii_user.TR.STATUS = ?  " & _
                                             " FROM genii_user.TR INNER JOIN genii_user.TR_CP  " & _
                                             "    ON genii_user.TR.TaxYear = genii_user.TR_CP.TaxYear  " & _
                                             "       AND genii_user.TR.TaxRollNumber = genii_user.TR_CP.TaxRollNumber " & _
                                             "            WHERE genii_user.TR_CP.InvestorID = ?")

                cmdTR3.Connection = conn
                cmdTR3.Transaction = trans

                With cmdTR3.Parameters
                    .AddWithValue("@STATUS", 4)
                    .AddWithValue("@InvestorID", 1)

                End With

                cmdTR3.ExecuteNonQuery()

                Dim cmdTR4 As New OleDbCommand("update genii_user.TR set genii_user.TR.STATUS = ? " & _
                                                 " FROM genii_user.TR INNER JOIN genii_user.TR_CP  " & _
                                                 "    ON genii_user.TR.TaxYear = genii_user.TR_CP.TaxYear  " & _
                                                 "      AND genii_user.TR.TaxRollNumber = genii_user.TR_CP.TaxRollNumber " & _
                                                 "            WHERE genii_user.TR_CP.CP_STATUS = ?")

                cmdTR4.Connection = conn
                cmdTR4.Transaction = trans

                With cmdTR4.Parameters
                    .AddWithValue("@STATUS", 5)
                    .AddWithValue("@CP_STATUS", 5)

                End With

                cmdTR4.ExecuteNonQuery()


                Dim cmdTR5 As New OleDbCommand("update genii_user.TR " & _
                                            "         set genii_user.TR.STATUS = ? " & _
                                            "   FROM genii_user.TR INNER JOIN genii_user.TAX_ACCOUNT_DEED_CP_CLEAR " & _
                                            "     ON genii_user.TR.TaxYear = genii_user.TAX_ACCOUNT_DEED_CP_CLEAR.TaxYear  " & _
                                            "       AND genii_user.TR.TaxRollNumber = genii_user.TAX_ACCOUNT_DEED_CP_CLEAR.TaxRollNumber " & _
                                            "           WHERE genii_user.TAX_ACCOUNT_DEED_CP_CLEAR.CertificateNumber Is Not NULL")

                cmdTR5.Connection = conn
                cmdTR5.Transaction = trans

                With cmdTR5.Parameters
                    .AddWithValue("@STATUS", 6)

                End With

                cmdTR5.ExecuteNonQuery()


                Dim cmdTR6 As New OleDbCommand("update genii_user.TR " & _
                                                "           set genii_user.TR.STATUS = ? " & _
                                                "   FROM genii_user.TR INNER JOIN genii_user.TAX_ACCOUNT_DEED_LOSS  " & _
                                                "     ON genii_user.TR.TaxRollNumber = genii_user.TAX_ACCOUNT_DEED_LOSS.TaxRollNumber  " & _
                                                "       AND genii_user.TR.TaxYear = genii_user.TAX_ACCOUNT_DEED_LOSS.TaxYear " & _
                                                "            WHERE genii_user.TAX_ACCOUNT_DEED_LOSS.ChargeAmount > 0")

                cmdTR6.Connection = conn
                cmdTR6.Transaction = trans

                With cmdTR6.Parameters
                    .AddWithValue("@STATUS", 7)

                End With

                cmdTR6.ExecuteNonQuery()

                '   lblFdBk.Text = "Step 3: Tax Roll Status has been recalculated"


                Dim cmdTaxAccount As New OleDbCommand("    UPDATE genii_user.TAX_ACCOUNT SET ACCOUNT_BANKRUPTCY = ? ")

                cmdTaxAccount.Connection = conn
                cmdTaxAccount.Transaction = trans

                With cmdTaxAccount.Parameters
                    .AddWithValue("@ACCOUNT_BANKRUPTCY", 0)

                End With

                cmdTaxAccount.ExecuteNonQuery()

                Dim cmdTaxAccount2 As New OleDbCommand("  UPDATE genii_user.TAX_ACCOUNT SET ACCOUNT_BANKRUPTCY = ? " & _
                                                      "     WHERE ParcelOrTaxID IN  " & _
                                                      "       (SELECT genii_user.BK_ACCOUNT.ACCOUNT " & _
                                                      "               FROM genii_user.BK_PARENT " & _
                                                      "           INNER JOIN genii_user.BK_ACCOUNT " & _
                                                      "             ON genii_user.BK_PARENT.RECORD_ID = genii_user.BK_ACCOUNT.PARENT_ID " & _
                                                      "         WHERE genii_user.BK_PARENT.RECORD_ID " & _
                                                      "           NOT IN " & _
                                                      "             (SELECT PARENT_ID FROM genii_user.BK_EVENT WHERE EVENT_TYPE = 6)) ")

                cmdTaxAccount2.Connection = conn
                cmdTaxAccount2.Transaction = trans

                With cmdTaxAccount2.Parameters
                    .AddWithValue("@ACCOUNT_BANKRUPTCY", 1)

                End With

                cmdTaxAccount2.ExecuteNonQuery()
                '  lblFdBk.Text = "Step 4: The Tax Account Bankruptcy Flag has been synchronized"



                Dim cmdTaxAccount3 As New OleDbCommand("UPDATE genii_user.TAX_ACCOUNT SET ACCOUNT_BALANCE = ?")

                cmdTaxAccount3.Connection = conn
                cmdTaxAccount3.Transaction = trans

                With cmdTaxAccount3.Parameters
                    .AddWithValue("@ACCOUNT_BALANCE", 0)

                End With

                cmdTaxAccount3.ExecuteNonQuery()
                '    lblFdBk.Text = "Step5: Tax Account Current has been reset to zero."


                Dim cmdTaxAccount4 As New OleDbCommand("UPDATE genii_user.TAX_ACCOUNT " & _
                                                  "         set ACCOUNT_BALANCE = vTotalAccountBalance.TOTAL_ACCOUNT_BALANCE " & _
                                                  "   FROM genii_user.TAX_ACCOUNT INNER JOIN vTotalAccountBalance " & _
                                                  "     ON genii_user.TAX_ACCOUNT.ParcelOrTaxID = vTotalAccountBalance.TaxIDNumber ")

                cmdTaxAccount4.Connection = conn
                cmdTaxAccount4.Transaction = trans

                'With cmdTaxAccount4.Parameters
                '    .AddWithValue("@ACCOUNT_BALANCE", 0.0)

                'End With

                cmdTaxAccount4.ExecuteNonQuery()
                '  lblFdBk.Text = "Step 6: Tax Account Current Balance has been set to the current value"


                Dim cmdTaxAccount5 As New OleDbCommand("UPDATE genii_user.TAX_ACCOUNT SET PARENT_BALANCE = ?")

                cmdTaxAccount5.Connection = conn
                cmdTaxAccount5.Transaction = trans

                With cmdTaxAccount5.Parameters
                    .AddWithValue("@PARENT_BALANCE", 0)

                End With

                cmdTaxAccount5.ExecuteNonQuery()


                Dim cmdTaxAccount6 As New OleDbCommand("UPDATE genii_user.TAX_ACCOUNT SET PARENT_BALANCE = ? " & _
                                                       "  WHERE PARENT_PARCEL IN (SELECT PARENT_PARCEL FROM vParentParcelBalance)")

                cmdTaxAccount6.Connection = conn
                cmdTaxAccount6.Transaction = trans

                With cmdTaxAccount6.Parameters
                    .AddWithValue("@PARENT_BALANCE", 1)

                End With

                cmdTaxAccount6.ExecuteNonQuery()
                '  lblFdBk.Text = "Step 7: Tax Account Parent Balance has been set to the current value."


                'Dim cmdTaxWebSpeed As New OleDbCommand("DROP TABLE genii_user.ST_WEB_DATA")

                'cmdTaxWebSpeed.Connection = conn
                'cmdTaxWebSpeed.Transaction = trans

                ''With cmdTaxWebSpeed.Parameters
                ''    .AddWithValue("@PARENT_BALANCE", 1)

                ''End With

                'cmdTaxWebSpeed.ExecuteNonQuery()


                'Dim cmdTaxWebSpeed2 As New OleDbCommand("SELECT * INTO genii_user.ST_WEB_DATA FROM dbo.vWebData")

                'cmdTaxWebSpeed2.Connection = conn
                'cmdTaxWebSpeed2.Transaction = trans

                ''With cmdTaxWebSpeed2.Parameters
                ''    .AddWithValue("@PARENT_BALANCE", 1)

                ''End With

                'cmdTaxWebSpeed2.ExecuteNonQuery()


                'Dim cmdTaxWebSpeed3 As New OleDbCommand("DELETE FROM genii_user.ST_WEB_DATA WHERE Tax Year < 2007 AND Balance = '$0.00'")

                'cmdTaxWebSpeed3.Connection = conn
                'cmdTaxWebSpeed3.Transaction = trans

                ''With cmdTaxWebSpeed3.Parameters
                ''    .AddWithValue("@PARENT_BALANCE", 1)

                ''End With

                'cmdTaxWebSpeed3.ExecuteNonQuery()

                'Dim cmdTaxWebSpeed4 As New OleDbCommand("CREATE CLUSTERED INDEX CI_TaxIDNumber ON genii_user.ST_WEB_DATA (TaxIDNumber)")

                'cmdTaxWebSpeed4.Connection = conn
                'cmdTaxWebSpeed4.Transaction = trans

                ''With cmdTaxWebSpeed4.Parameters
                ''    .AddWithValue("@PARENT_BALANCE", 1)

                ''End With

                'cmdTaxWebSpeed4.ExecuteNonQuery()
                ''   lblFdBk.Text = "Step 8: Update Tax Web Speed Tables."


                Dim cmdTRCP As New OleDbCommand("UPDATE genii_user.TR_CP " & _
                                              "        set  INITIAL_CP_YEAR = vInitialCPYear.INITIAL_CP_YEAR " & _
                                              "           FROM genii_user.TR_CP " & _
                                              "   INNER JOIN vInitialCPYear " & _
                                              "     ON genii_user.TR_CP.CertificateNumber = vInitialCPYear.CertificateNumber")

                cmdTRCP.Connection = conn
                cmdTRCP.Transaction = trans

                'With cmdTRCP.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdTRCP.ExecuteNonQuery()
                ' lblFdBk.Text = "Step 9: Update Initial CP Year."


                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
            conn.Close()
        End Using
        '  lblFdBk.Text = "Nightly Script Done."

        Return True

    End Function
    <System.Web.Services.WebMethod()> _
    Public Shared Function btnReverseTransaction_Click(grpKey As String) As Boolean


        Dim myUtil As New Utilities()

        ' Update rows.
        Using conn3 As New OleDbConnection(myUtil.ConnectString)
            conn3.Open()
            Dim trans3 As OleDbTransaction = conn3.BeginTransaction()

            Dim sql1 As String = "Update genii_user.cashier_transactions set transaction_status=4, edit_user='" + System.Web.HttpContext.Current.User.Identity.Name + "', edit_date='" + Date.Now + "' where group_key=" + grpKey + " "

            Dim sql2 As String = "insert into genii_user.TR_Payments(trans_id, " & _
                                   " taxYear, " & _
                                   "  TaxRollNumber, " & _
                                   "  PaymentEffectiveDate, " & _
                                   "  PaymentTypeCode, " & _
                                   "  PaymentMadeByCode," & _
                                   "  Pertinent1, " & _
                                   "  Pertinent2, " & _
                                   "  PaymentAmount, " & _
                                   "  Payment_rule, " & _
                                   "  CalcPaydate, " & _
                                   "  create_user, " & _
                                   "  create_date, " & _
                                   "  edit_user, " & _
                                   "  edit_date) " & _
                                   "  (select trans_id, " & _
                                   "  taxYear, " & _
                                   "  TaxRollNumber, " & _
                                   "  PaymentEffectiveDate, " & _
                                   "  12 as PaymentTypeCode, " & _
                                   "  6 as PaymentMadeByCode, " & _
                                   "  Pertinent1, " & _
                                   "  'Payment Reversal' as Pertinent2, " & _
                                   "  (PaymentAmount* (-1)) as PaymentAmount, " & _
                                   "  Payment_rule, " & _
                                   "  CalcPaydate, " & _
                                   "  '" + System.Web.HttpContext.Current.User.Identity.Name + "', " & _
                                    "  '" + Date.Now + "', " & _
                                     "  '" + System.Web.HttpContext.Current.User.Identity.Name + "', " & _
                                     "  '" + Date.Now + "' " & _
                                   "  from genii_user.TR_Payments " & _
                         " where trans_id in " & _
                        " (select record_id from genii_user.Cashier_transactions where group_key=" + grpKey + "))"

            Dim sql3 As String = "insert into genii_user.TR_CHARGES(taxyear,taxrollnumber,taxchargecodeid,taxtypeid,chargeamount,originallevyamount,create_user,create_date,edit_user,edit_date) " & _
                                    " (select distinct taxyear, " & _
                                    "  taxRollNumber, " & _
                                    " 99973 as TaxChargeCodeID,  " & _
                                    "  99 as TaxTypeID, " & _
                                    " (select parameter from genii_user.ST_PARAMETER   " & _
                                    " where record_id=99973) as ChargeAmount, " & _
                                    " 0.00 as OriginalLevyAmount, " & _
                                    "  '" + System.Web.HttpContext.Current.User.Identity.Name + "', " & _
                                    "  '" + Date.Now + "', " & _
                                     "  '" + System.Web.HttpContext.Current.User.Identity.Name + "', " & _
                                     "  '" + Date.Now + "' " & _
                                    "  from genii_user.TR_CHARGES where taxrollnumber in  " & _
                                    " (select tax_roll_number from genii_user.Cashier_transactions where group_key =" + grpKey + ") " & _
                                    " and taxyear in (select tax_year from genii_user.Cashier_transactions where group_key=" + grpKey + ")) "

            Dim sql4 As String = "insert into genii_user.Cashier_apportion(taxchargecodeID,trans_id,taxyear, " & _
                                    " taxrollnumber,areacode,taxtypeid,paymentDate,glaccount,senttoothersystem,receiptnumber,dateApportioned,dollarAmount,create_user,create_date,edit_user,edit_date) " & _
                                    " (select distinct taxchargecodeid,  " & _
                                    " trans_id, " & _
                                    " taxyear, " & _
                                    " taxrollnumber, " & _
                                    " areacode, " & _
                                    " taxtypeID, " & _
                                    " paymentDate, " & _
                                    " glAccount, " & _
                                    "  0 as SenttootherSystem,   " & _
                                    " ReceiptNumber, " & _
                                    " DateApportioned, " & _
                                    " DollarAmount * (-1) as DollarAmount, " & _
                                    "  '" + System.Web.HttpContext.Current.User.Identity.Name + "', " & _
                                    "  '" + Date.Now + "', " & _
                                     "  '" + System.Web.HttpContext.Current.User.Identity.Name + "', " & _
                                     "  '" + Date.Now + "' " & _
                                    "             from genii_user.cashier_apportion " & _
                                    " where trans_id in (select record_id from genii_user.Cashier_transactions where group_key=" + grpKey + "))"


            Dim sql5 As String = "UPDATE genii_user.TR SET CurrentBalance= genii_user.cashier_transactions.payment_amt " & _
                                " from genii_user.TR INNER JOIN genii_user.cashier_transactions " & _
                                " on genii_user.TR.TaxYear= genii_user.cashier_transactions.tax_year " & _
                                " and genii_user.TR.Taxrollnumber=genii_user.cashier_transactions.tax_roll_number " & _
                                " and genii_user.cashier_transactions.group_key=" + grpKey + " "

            Dim sql6 As String = "UPDATE genii_user.Tax_Account SET account_balance= genii_user.cashier_transactions.payment_amt " & _
                                " from genii_user.Tax_Account INNER JOIN genii_user.TR " & _
                                " on genii_user.Tax_Account.ParcelOrTaxID=genii_user.TR.TaxIDNumber " & _
                                " inner join genii_user.cashier_transactions " & _
                                " on genii_user.TR.TaxYear= genii_user.cashier_transactions.tax_year " & _
                                " and genii_user.TR.Taxrollnumber=genii_user.cashier_transactions.tax_roll_number " & _
                                " and genii_user.cashier_transactions.group_key=" + grpKey + " "

            Dim cmdUpdateCashierTransaction As New OleDbCommand(sql1, conn3)
            Dim cmdInsertTRPaymentRecord As New OleDbCommand(sql2, conn3)
            Dim cmdInsertTRChargesRecord As New OleDbCommand(sql3, conn3)
            Dim cmdInsertCashierApportionRecord As New OleDbCommand(sql4, conn3)
            Dim cmdUpdateTR As New OleDbCommand(sql5, conn3)
            Dim cmdUpdateTaxAccount As New OleDbCommand(sql6, conn3)

            cmdUpdateCashierTransaction.Transaction = trans3
            cmdInsertTRPaymentRecord.Transaction = trans3
            cmdInsertTRChargesRecord.Transaction = trans3
            cmdInsertCashierApportionRecord.Transaction = trans3
            cmdUpdateTR.Transaction = trans3
            cmdUpdateTaxAccount.Transaction = trans3

            cmdUpdateCashierTransaction.ExecuteNonQuery()
            cmdInsertTRPaymentRecord.ExecuteNonQuery()
            cmdInsertTRChargesRecord.ExecuteNonQuery()
            cmdInsertCashierApportionRecord.ExecuteNonQuery()
            cmdUpdateTR.ExecuteNonQuery()
            cmdUpdateTaxAccount.ExecuteNonQuery()

            trans3.Commit()
            conn3.Close()

        End Using

        _GRPKEY = grpKey

        Dim print_document As Printing.PrintDocument
        print_document = PreparePrintDocument()
        print_document.Print()

       



       


        '============================================


        Dim sessID As Integer = 0
        Dim SQLSessID As String = String.Format("select distinct session_id from genii_user.cashier_transactions where group_key = " + grpKey + " ")

        Using adt As New OleDbDataAdapter(SQLSessID, myUtil.ConnectString)
            Dim tblSessID As New DataTable()

            adt.Fill(tblSessID)

            If tblSessID.Rows.Count > 0 Then
                If (Not IsDBNull(tblSessID.Rows(0)("session_id"))) Then
                    sessID = Convert.ToInt32(tblSessID.Rows(0)("session_id"))
                End If

            End If
        End Using

        Dim sql As String = String.Empty
        ' Dim sql2 As String
        Dim depositDetails As DataSet = New DataSet()
        Dim receiptDetails As DataSet = New DataSet()
        sql = String.Format("SELECT genii_user.CASHIER_SESSION.RECORD_ID AS 'Cashier Session', " & _
                                            "  GETDATE() AS 'ENTRY_DATE', " & _
                                            "   GETDATE() AS 'RECORD_DATE', " & _
                                            "             'xxx' AS 'MEMO', " & _
                                            "   5 AS 'MEMO_NUMBER', " & _
                                            "   GETDATE() AS 'TAX_RECEIPT_OPEN_DATE', " & _
                                            "             'JE-REVDep' + CONVERT(varchar, DATEPART(dd, GETDATE())) AS 'REFERENCE', " & _
                                            "             genii_user.CASHIER_SESSION.POSTED_DATE, " & _
                                            "             genii_user.CASHIER_SESSION.POSTED, " & _
                                            "   SUM(genii_user.CASHIER_APPORTION.DollarAmount)*(-1) AS 'AMOUNT', " & _
                                            "   GETDATE() AS 'FROM_DATE', " & _
                                            "   GETDATE() AS 'TO_DATE', " & _
                                            "             '' AS 'ENTITY', " & _
                                            "   2 AS 'STATUS', " & _
                                            "             'CFB' AS 'ACCOUNT', " & _
                                            "             'Unknown' AS 'ENTRY', " & _
                                            "   DATEPART(mm, GETDATE()) AS DEPOSIT, " & _
                                            "   0 AS 'USR_RECORD_NUMBER', " & _
                                            "             'False' AS 'PRINT_RECEIPT', " & _
                                            "             'True' AS 'TAXRECEIPT', " & _
                                            "             'genii_user' AS 'CREATE_USER', " & _
                                            "   GETDATE() AS 'CREATE_DATE', " & _
                                            "             'genii_user' AS 'EDIT_USER', " & _
                                            "   GETDATE() AS 'EDIT_DATE'                  " & _
                                            "             FROM genii_user.CASHIER_SESSION " & _
                                            "  INNER JOIN genii_user.CASHIER_TRANSACTIONS " & _
                                            "    ON genii_user.CASHIER_SESSION.RECORD_ID = genii_user.CASHIER_TRANSACTIONS.SESSION_ID " & _
                                            "  INNER JOIN genii_user.CASHIER_APPORTION " & _
                                            "    ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID " & _
                                            " WHERE genii_user.CASHIER_TRANSACTIONS.GROUP_KEY = '" + grpKey.ToString() + "' " & _
                                            " GROUP BY genii_user.CASHIER_SESSION.RECORD_ID, " & _
                                            "             genii_user.CASHIER_SESSION.POSTED_DATE,genii_user.CASHIER_SESSION.POSTED ")


        '     LoadTable(depositDetails, "CASHIER_SESSION", sql)

        Using adt As New OleDbDataAdapter(sql, myUtil.ConnectString)
            adt.Fill(depositDetails, "CASHIER_SESSION")
        End Using

        Dim row As DataRow
        row = depositDetails.Tables(0).Rows(0)

        Dim cashierSession As Integer
        Dim receiptNumber As String
        Dim entryDate As Date
        Dim recordDate As Date
        Dim memo As String
        Dim memoNumber As Integer
        Dim taxReceiptOpenDate As Date
        Dim reference As String
        Dim amount As Double
        Dim fromDate As DateTime
        Dim toDate As DateTime
        Dim entity As String
        Dim status As Integer
        Dim account As String
        Dim entry As String
        Dim deposit As Integer
        Dim usrRecordNumber As Integer
        Dim printReceipt As Boolean
        Dim taxreceipt As Boolean
        Dim postedDate As DateTime
        Dim isPosted As Integer

        ' Fill in Labels with data from CASHIER_SESSION
        If (IsDBNull(row("Cashier Session"))) Then
            cashierSession = String.Empty
        Else
            cashierSession = row("Cashier Session")
        End If

        If (IsDBNull(row("MEMO"))) Then
            memo = String.Empty
        Else
            memo = row("MEMO")
        End If

        If (IsDBNull(row("MEMO_NUMBER"))) Then
            memoNumber = String.Empty
        Else
            memoNumber = row("MEMO_NUMBER")
        End If

        If (IsDBNull(row("REFERENCE"))) Then
            reference = String.Empty
        Else
            reference = row("REFERENCE")
        End If

        If (IsDBNull(row("AMOUNT"))) Then
            amount = String.Empty
        Else
            amount = row("AMOUNT")
        End If

        If (IsDBNull(row("ENTITY"))) Then
            entity = String.Empty
        Else
            entity = row("ENTITY")
        End If

        If (IsDBNull(row("STATUS"))) Then
            status = String.Empty
        Else
            status = row("STATUS")
        End If

        If (IsDBNull(row("ACCOUNT"))) Then
            account = String.Empty
        Else
            account = row("ACCOUNT")
        End If

        If (IsDBNull(row("ENTRY"))) Then
            entry = String.Empty
        Else
            entry = row("ENTRY")
        End If

        If (IsDBNull(row("DEPOSIT"))) Then
            deposit = String.Empty
        Else
            deposit = row("DEPOSIT")
        End If

        If (IsDBNull(row("USR_RECORD_NUMBER"))) Then
            usrRecordNumber = String.Empty
        Else
            usrRecordNumber = row("USR_RECORD_NUMBER")
        End If

        If (IsDBNull(row("PRINT_RECEIPT"))) Then
            printReceipt = String.Empty
        Else
            printReceipt = row("PRINT_RECEIPT")
        End If

        If (IsDBNull(row("TAXRECEIPT"))) Then
            taxreceipt = String.Empty
        Else
            taxreceipt = row("TAXRECEIPT")
        End If


        Dim conn1 As New OleDbConnection(myUtil.ConnectString)

        conn1.Open()

        Dim receiptDetailsAdapter As OleDbDataAdapter = New OleDbDataAdapter( _
                "SELECT genii_user.CASHIER_APPORTION.ReceiptNumber AS 'RECEIPT_NUMBER', " & _
                                             " SUM(genii_user.CASHIER_APPORTION.DollarAmount)*(-1) AS 'AMOUNT',  " & _
                                             "  genii_user.CASHIER_APPORTION.GLAccount AS 'ACCOUNT', " & _
                                             "  genii_user.CASHIER_POSTING_GL.Description AS 'MEMO', " & _
                                             "            'genii_user' AS 'CREATE_USER', " & _
                                             "  GETDATE() AS 'CREATE_DATE', " & _
                                             "            'genii_user' AS 'EDIT_USER', " & _
                                             "  GETDATE() AS 'EDIT_DATE'              " & _
                                             "            FROM genii_user.CASHIER_SESSION  " & _
                                             " INNER JOIN genii_user.CASHIER_TRANSACTIONS " & _
                                             "   ON genii_user.CASHIER_SESSION.RECORD_ID = genii_user.CASHIER_TRANSACTIONS.SESSION_ID " & _
                                             " INNER JOIN genii_user.CASHIER_APPORTION " & _
                                             "   ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID " & _
                                             " INNER JOIN genii_user.CASHIER_POSTING_GL " & _
                                             "   ON genii_user.CASHIER_APPORTION.GLAccount = genii_user.CASHIER_POSTING_GL.GLAccount " & _
                                            " WHERE  genii_user.CASHIER_SESSION.RECORD_ID = '" + sessID.ToString() + "' " & _
                                            " GROUP BY genii_user.CASHIER_APPORTION.GLAccount, " & _
                                             "            genii_user.CASHIER_APPORTION.ReceiptNumber, " & _
                                             "            genii_user.CASHIER_POSTING_GL.Description" & _
                                             "            ORDER BY  'ACCOUNT' ", conn1)


        Dim receiptDetailsDS As DataSet = New DataSet()
        receiptDetailsAdapter.Fill(receiptDetailsDS, "receiptDetails")

        Dim receiptDetailsRow As DataRow




        Dim conn As New OleDbConnection(myUtil.BankConnectString)

        conn.Open()

        Dim trans As OleDbTransaction = conn.BeginTransaction()
        ' Dim newReceiptNumber As Integer
        Try
            ' Get new record id.
            Dim newRecordID As Integer



            Using cmdRecordID As New OleDbCommand("SELECT MAX(RECORD_ID) FROM dbo.RECEIPT_PARENT", conn)
                Dim objRecordID As Object

                cmdRecordID.Transaction = trans
                objRecordID = cmdRecordID.ExecuteScalar()

                If (Not IsDBNull(objRecordID)) AndAlso IsNumeric(objRecordID) Then
                    newRecordID = CInt(objRecordID) + 1
                Else
                    newRecordID = 1
                End If
            End Using

            Using cmdRecordID As New OleDbCommand("SELECT MAX(RECEIPT_NUMBER) FROM dbo.RECEIPT_PARENT", conn)
                Dim objRecordID As Object

                cmdRecordID.Transaction = trans
                objRecordID = cmdRecordID.ExecuteScalar()

                If (Not IsDBNull(objRecordID)) AndAlso IsNumeric(objRecordID) Then
                    newReceiptNumber = CInt(objRecordID) + 1
                Else
                    newReceiptNumber = 1
                End If
            End Using


            ' Insert new record.
            Using cmd As New OleDbCommand()
                cmd.CommandText = "INSERT INTO dbo.RECEIPT_PARENT (" & _
                                    " RECORD_ID, RECEIPT_NUMBER, ENTRY_DATE, RECORD_DATE, MEMO, MEMO_NUMBER, " & _
                                    " TAX_RECEIPT_OPEN_DATE, REFERENCE, AMOUNT, FROM_DATE, TO_DATE, ENTITY, " & _
                                    " STATUS, ACCOUNT, ENTRY, DEPOSIT, USR_RECORD_NUMBER, PRINT_RECEIPT, " & _
                                    " TAXRECEIPT, " & _
                                    " CREATE_USER, CREATE_DATE, EDIT_USER, EDIT_DATE" & _
                                    ") VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)"
                cmd.Connection = conn
                cmd.Transaction = trans

                cmd.Parameters.AddWithValue("@RECORD_ID", newRecordID)
                cmd.Parameters.AddWithValue("@RECEIPT_NUMBER", newReceiptNumber)
                cmd.Parameters.AddWithValue("@ENTRY_DATE", DateTime.Now.ToShortDateString)
                cmd.Parameters.AddWithValue("@RECORD_DATE", DateTime.Now.ToShortDateString)
                cmd.Parameters.AddWithValue("@MEMO", memo)
                cmd.Parameters.AddWithValue("@MEMO_NUMBER", memoNumber)
                cmd.Parameters.AddWithValue("@TAX_RECEIPT_OPEN_DATE", DateTime.Now.ToShortDateString)
                cmd.Parameters.AddWithValue("@REFERENCE", reference)
                cmd.Parameters.AddWithValue("@AMOUNT", amount)
                cmd.Parameters.AddWithValue("@FROM_DATE", DateTime.Now)
                cmd.Parameters.AddWithValue("@TO_DATE", DateTime.Now)
                cmd.Parameters.AddWithValue("@ENTITY", entity)
                cmd.Parameters.AddWithValue("@STATUS", status)
                cmd.Parameters.AddWithValue("@ACCOUNT", account)
                cmd.Parameters.AddWithValue("@ENTRY", entry)
                cmd.Parameters.AddWithValue("@DEPOSIT", deposit)
                cmd.Parameters.AddWithValue("@USR_RECORD_NUMBER", usrRecordNumber)
                cmd.Parameters.AddWithValue("@PRINT_RECEIPT", printReceipt)
                cmd.Parameters.AddWithValue("@TAXRECEIPT", taxreceipt)
                cmd.Parameters.AddWithValue("@CREATE_USER", "genii_user")
                cmd.Parameters.AddWithValue("@CREATE_DATE", DateTime.Now.ToShortDateString)
                cmd.Parameters.AddWithValue("@EDIT_USER", "genii_user")
                cmd.Parameters.AddWithValue("@EDIT_DATE", DateTime.Now.ToShortDateString)

                cmd.ExecuteNonQuery()

            End Using

            trans.Commit()

            '   LoadDeposits()

        Catch ex As Exception
            trans.Rollback()
        Finally
            If conn IsNot Nothing AndAlso conn.State = ConnectionState.Open Then
                conn.Close()
            End If
        End Try




        Dim conn2 As New OleDbConnection(myUtil.BankConnectString)

        conn2.Open()

        Dim trans2 As OleDbTransaction = conn2.BeginTransaction()
        Try
            ' Get new record id.
            Dim newRecordID As Integer

            Using cmdRecordID As New OleDbCommand("SELECT MAX(RECORD_ID) FROM dbo.RECEIPT_DETAIL", conn2)
                Dim objRecordID As Object

                cmdRecordID.Transaction = trans2
                objRecordID = cmdRecordID.ExecuteScalar()

                If (Not IsDBNull(objRecordID)) AndAlso IsNumeric(objRecordID) Then
                    newRecordID = CInt(objRecordID) + 1
                Else
                    newRecordID = 1
                End If
            End Using


            For Each receiptDetailsRow In receiptDetailsDS.Tables("receiptDetails").Rows
                'receiptDetailsRow("RECEIPT_NUMBER").toString()
                'SAVE TO BELOW
                'cmd.Parameters.AddWithValue("@RECEIPT_NUMBER", newReceiptNumber)


                Using cmd As New OleDbCommand()
                    cmd.CommandText = "INSERT INTO dbo.RECEIPT_DETAIL (" & _
                                        " RECORD_ID, RECEIPT_NUMBER, MEMO, " & _
                                        " AMOUNT, ACCOUNT,  " & _
                                        " CREATE_USER, CREATE_DATE, EDIT_USER, EDIT_DATE" & _
                                        ") VALUES (?,?,?,?,?,?,?,?,?)"
                    cmd.Connection = conn2
                    cmd.Transaction = trans2

                    cmd.Parameters.AddWithValue("@RECORD_ID", newRecordID + 1)
                    cmd.Parameters.AddWithValue("@RECEIPT_NUMBER", newReceiptNumber)
                    cmd.Parameters.AddWithValue("@MEMO", receiptDetailsRow("MEMO"))
                    cmd.Parameters.AddWithValue("@AMOUNT", receiptDetailsRow("AMOUNT"))
                    cmd.Parameters.AddWithValue("@ACCOUNT", receiptDetailsRow("ACCOUNT"))
                    cmd.Parameters.AddWithValue("@CREATE_USER", "genii_user")
                    cmd.Parameters.AddWithValue("@CREATE_DATE", DateTime.Now.ToShortDateString)
                    cmd.Parameters.AddWithValue("@EDIT_USER", "genii_user")
                    cmd.Parameters.AddWithValue("@EDIT_DATE", DateTime.Now.ToShortDateString)

                    cmd.ExecuteNonQuery()
                End Using


            Next
            trans2.Commit()

        Catch ex As Exception
            trans2.Rollback()
        Finally
            If conn2 IsNot Nothing AndAlso conn2.State = ConnectionState.Open Then
                conn2.Close()
            End If
        End Try



        ' TaxSupervisor.btnReverseTransaction_Click(grpKey)



        Return True
    End Function

    Public Shared Function PreparePrintDocument() As Printing.PrintDocument
        ' Make the PrintDocument object.
        Dim print_document As New Printing.PrintDocument

        AddHandler print_document.PrintPage, AddressOf DrawStringPointF_PRINTHEADER
        AddHandler print_document.PrintPage, AddressOf DrawStringPointF

        ' Return the object.
        Return print_document
    End Function

    Public Shared Function DrawStringPointF_PRINTHEADER(ByVal sender As Object, ByVal e As PrintPageEventArgs) As Boolean
        Dim myUtil As New Utilities()

        Dim SQLa As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='SIGNATURE_BLOCK_TITLE' ")
        Dim sigBlockTitle As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQLa, myUtil.ConnectString)
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

        Dim SQLb As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='SIGNATURE_BLOCK_NAME' ")
        Dim sigBlockName As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQLb, myUtil.ConnectString)
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

        Dim SQLc As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='ADDRESS' ")
        Dim sigBlockAddress As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQLc, myUtil.ConnectString)
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

        Dim SQLd As String = String.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='CITY_STATE_ZIP' ")
        Dim sigBlockCityStateZip As String = String.Empty

        Using adt2 As New OleDbDataAdapter(SQLd, myUtil.ConnectString)
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


        '  Dim y As String = String.Empty

        ' Dim defaultHeader As String() = {"---------------------------------------", sigBlockTitle, sigBlockName, sigBlockAddress, sigBlockCityStateZip, Date.Now, "Operator - "}
        Dim defaultHeader As String() = {"-----------------------------------------------------", "Operator - " & System.Web.HttpContext.Current.User.Identity.Name, Date.Now, sigBlockCityStateZip, sigBlockAddress, sigBlockName, sigBlockTitle, "-----------------------------------------------------"}
        Dim s As String = String.Empty
        For i = 0 To defaultHeader.Count - 1
            s = s & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(defaultHeader(i) & s, printFont10B, Brushes.Black, rect1, stringFormat)
        Next

        Return True
    End Function

    Public Shared Function DrawStringPointF(ByVal sender As Object, ByVal e As PrintPageEventArgs) As Boolean

        Dim myUtil As New Utilities()

        Dim SQLe As String = String.Format("select a.Tax_Roll_Number,a.tax_year, a.payment_amt,a.record_id as transID,b.APN from genii_user.CASHIER_TRANSACTIONS a, genii_user.TR b where a.Tax_Roll_Number=b.TaxRollNumber and a.Tax_Year =b.taxYear and a.group_key='" + _GRPKEY.ToString() + "' and a.transaction_status=4 ")
        'transaction_status=4  equals to reversed...
        Dim taxRollNumber As String = String.Empty
        Dim taxyYear As String = String.Empty
        Dim taxID As String = String.Empty
        Dim amountReversed As Decimal = 0
        Dim transID As Integer

        Using adt2 As New OleDbDataAdapter(SQLe, myUtil.ConnectString)
            Dim tblParameter4 As New DataTable()

            adt2.Fill(tblParameter4)

            If tblParameter4.Rows.Count > 0 Then

                For x = 0 To (tblParameter4.Rows.Count - 1)
                    If (Not IsDBNull(tblParameter4.Rows(x)("TAX_ROLL_NUMBER"))) Then
                        taxRollNumber = Convert.ToString(tblParameter4.Rows(x)("TAX_ROLL_NUMBER"))
                    End If

                    If (Not IsDBNull(tblParameter4.Rows(x)("TAX_YEAR"))) Then
                        taxyYear = Convert.ToString(tblParameter4.Rows(x)("TAX_YEAR"))
                    End If

                    If (Not IsDBNull(tblParameter4.Rows(x)("payment_amt"))) Then
                        amountReversed = Convert.ToDecimal(tblParameter4.Rows(x)("payment_amt"))
                    End If

                    If (Not IsDBNull(tblParameter4.Rows(x)("transID"))) Then
                        transID = Convert.ToInt32(tblParameter4.Rows(x)("transID"))
                    End If

                    If (Not IsDBNull(tblParameter4.Rows(x)("APN"))) Then
                        taxID = Convert.ToString(tblParameter4.Rows(x)("APN"))
                    End If
                Next
            End If
        End Using


        Dim printFont10B = New Font("Arial", 9, FontStyle.Bold)
        Dim printFont9R = New Font("Arial", 9, FontStyle.Regular)
        Dim rect1 As New Rectangle(10, 10, 270, 250)

        Dim rect2a As New Rectangle(10, 30, 270, 250)

        Dim rect2 As New Rectangle(10, 90, 270, 250)

        Dim rect3 As New Rectangle(10, 150, 270, 250)

        Dim rect4 As New Rectangle(10, 160, 270, 400)

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

        Dim paymentDetails As String() = {"Payment Reversal "}
        z = String.Empty
        For i = 0 To paymentDetails.Count - 1
            z = z & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails(i) & z, printFont9R, Brushes.Black, rect2a, StringFormat)
        Next

        Dim paymentDetails2 As String() = {"Transaction :" & transID, "Group Number: " & _GRPKEY, "Amount Reversed: $" & amountReversed}
        a = String.Empty
        For i = 0 To paymentDetails2.Count - 1
            a = a & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails2(i) & a, printFont9R, Brushes.Black, rect2, stringFormat)
        Next

        Dim paymentDetails1 As String() = {"- - - ", "Tax ID: " & taxID, "Roll Number :" & taxRollNumber, "Tax Year: " & taxyYear}
        z = String.Empty
        For i = 0 To paymentDetails1.Count - 1
            z = z & " " & vbNewLine & vbNewLine
            e.Graphics.DrawString(paymentDetails1(i) & z, printFont9R, Brushes.Black, rect3, StringFormat)
        Next



        ''-------------------------------------------------------------------------------------------------------------
        Return True
    End Function

    <System.Web.Services.WebMethod()> _
    Public Shared Function btnUpdateQuickPayments_Click(transID As String, idx As Integer, balance As Decimal, taxes As Decimal, interest As String, payment As Decimal, difference As Decimal, chkPM As Boolean, chkBI As Boolean, chkFG As Boolean) As Boolean ''transID As String,

        Dim myUtil As New Utilities()

        ' Dim balance As Decimal
        Dim fees As Decimal
        '  Dim interest As Decimal
        Dim paidAmount As Decimal
        Dim taxAmount As Decimal
        Dim TaxYear As String
        Dim TaxRollNumber As String
        Dim pm As Integer
        Dim bi As Integer
        Dim fg As Integer
        Dim kittyAmount As Decimal
        Dim refundAmount As Decimal
        ' Dim difference As Decimal
        Dim MaxKittyAmount As Decimal
        Dim recordID As Integer

        Dim SQL As String = String.Format("SELECT parameter FROM genii_user.st_parameter WHERE parameter_name = 'MAX_KITTY_AMOUNT'")

        Using adt As New OleDbDataAdapter(SQL, myUtil.ConnectString)
            Dim tblMaxKittyAmount As New DataTable()

            adt.Fill(tblMaxKittyAmount)

            If tblMaxKittyAmount.Rows.Count > 0 Then
                If (Not IsDBNull(tblMaxKittyAmount.Rows(0)("parameter"))) Then
                    MaxKittyAmount = Convert.ToDecimal(tblMaxKittyAmount.Rows(0)("parameter"))
                End If

            End If
        End Using


        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            ' For Each gvr As GridViewRow In grdQuickPayments.Rows
            '  Dim gvr As GridViewRow
            'gvr = grdQuickPayments.SelectedRow
            ' Dim idx As GridViewRow = grdQuickPayments.SelectedRow
            ' Dim cb As CheckBox = grdQuickPayments.SelectedRow.FindControl("chkPriorYears")
            'Dim txtPayment As HtmlInputText '= grdQuickPayments.Rows(idx).cells(8) 'gvr.Cells(8).FindControl("txtQuickPayment")
            'Dim txtDifference As Label = gvr.Cells(9).FindControl("lblQuickPaymentRemainder")
            'Dim chkpm As CheckBox = gvr.Cells(10).FindControl("chkPM")
            'Dim chkbi As CheckBox = gvr.Cells(11).FindControl("chkBI")
            'Dim chkfgi As CheckBox = gvr.Cells(12).FindControl("chkFG")

            'paidAmount = txtPayment.Value
            'difference = txtDifference.Text
            'If (chkpm.Checked = True) Then
            '    pm = 1
            'Else
            '    pm = 0
            'End If

            'If (chkbi.Checked = True) Then
            '    bi = 1
            'Else
            '    bi = 0
            'End If

            'If (chkfgi.Checked = True) Then
            '    fg = 1
            'Else
            '    fg = 0
            'End If

            'recordID = gvr.Cells(0).Text
            'TaxYear = gvr.Cells(1).Text
            'TaxRollNumber = gvr.Cells(2).Text
            'interest = gvr.Cells(7).Text
            'taxAmount = gvr.Cells(4).Text
            'balance = gvr.Cells(4).Text - paidAmount
            If (difference <= MaxKittyAmount And difference <> 0) Then
                kittyAmount = difference
                refundAmount = 0.0
            ElseIf (difference > MaxKittyAmount) Then
                refundAmount = difference
                kittyAmount = 0.0
            ElseIf (difference = 0) Then
                refundAmount = 0.0
                kittyAmount = 0.0
            End If

            Try
                Dim cmdNewRecPayments As New OleDbCommand("UPDATE genii_user.CASHIER_QUICK_PAYMENTS " & _
                                                          " SET QP_STATUS = ?, BALANCE = ?, INTEREST=?, PAYMENT_AMT=?, TAX_AMT=?, KITTY_AMT=?, REFUND_AMT=?, PM=?, BI=?, FGI=?, PBH=? " & _
                                                          " WHERE RECORD_ID =? and QP_STATUS is null")

                cmdNewRecPayments.Connection = conn
                cmdNewRecPayments.Transaction = trans

                'BALANCE, INTEREST, PAYMENT_AMT, TAX_AMT, KITTY_AMT, REFUND_AMT, PM, BI, FGI, PBH


                With cmdNewRecPayments.Parameters
                    '    .AddWithValue("@RECORD_ID", recordID)
                    '    .AddWithValue("@SESSION_ID", lblSessionID.Text)
                    .AddWithValue("@QP_STATUS", 1)
                    .AddWithValue("@BALANCE", balance)
                    '   .AddWithValue("@FEES", fees)
                    .AddWithValue("@INTEREST", interest)
                    .AddWithValue("@PAYMENT_AMT", paidAmount)
                    .AddWithValue("@TAX_AMT", taxAmount)
                    .AddWithValue("@KITTY_AMT", kittyAmount)
                    .AddWithValue("@REFUND_AMT", refundAmount)
                    .AddWithValue("@PM", pm)
                    .AddWithValue("@BI", bi)
                    .AddWithValue("@FGI", fg)
                    .AddWithValue("@PBH", 0)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)
                    .AddWithValue("@RECORD_ID", recordID)
                    '  .AddWithValue("@TAX_YEAR", TaxYear)
                    '  .AddWithValue("@TAX_ROLL_NUMBER", TaxRollNumber)

                End With

                cmdNewRecPayments.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                '  Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            ' Next
            conn.Close()
        End Using


        Dim success = True
        Return success




    End Function

    '   Public Sub btnDeleteTransaction_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnDeleteTransaction.Click
    <System.Web.Services.WebMethod()> _
    Public Shared Function btnDeleteTransaction_Click(grpKey As String) As Boolean ''transID As String,

        Dim myUtil As New Utilities()

        ' Update rows.
        Using conn3 As New OleDbConnection(myUtil.ConnectString)
            conn3.Open()
            Dim trans3 As OleDbTransaction = conn3.BeginTransaction()

            Dim sql1 As String = "Update genii_user.cashier_transactions set transaction_status=3, edit_user='" + System.Web.HttpContext.Current.User.Identity.Name + "', edit_date='" + Date.Now + "' where group_key=" + grpKey + " "

            '  Dim sql1 As String = "DELETE FROM genii_user.CASHIER_TRANSACTIONS  WHERE  GROUP_KEY= " + grpKey + " " 'RECORD_ID=" + transID + " AND
            '  Dim sql2 As String = "DELETE FROM genii_user.CASHIER_APPORTION  WHERE TRANS_ID=" + transID + " "
            '  Dim sql3 As String = "DELETE FROM genii_user.TR_PAYMENTS  WHERE TRANS_ID=" + transID + " "

            '  Dim sql2 As String = "delete from genii_user.CASHIER_APPORTION where trans_id in(select trans_id from genii_user.CASHIER_TRANSACTIONS A, genii_user.CASHIER_APPORTION B where a.record_id=b.trans_id and group_key=" + grpKey + ")"

            Dim sql2 As String = " delete from genii_user.CASHIER_APPORTION where record_id in( " & _
                                " select genii_user.CASHIER_APPORTION.record_id from genii_user.CASHIER_APPORTION  " & _
                                "  inner join  " & _
                                "  genii_user.CASHIER_TRANSACTIONS " & _
                                "  on genii_user.CASHIER_APPORTION.trans_id = genii_user.CASHIER_TRANSACTIONS.record_id " & _
                                "  and genii_user.CASHIER_TRANSACTIONS.group_key=" + grpKey + ")"

            Dim sql3 As String = "delete from genii_user.TR_PAYMENTS where trans_id in(select trans_id from genii_user.CASHIER_TRANSACTIONS A, genii_user.TR_PAYMENTS B where a.record_id=b.trans_id and group_key=" + grpKey + ")"

            Dim sql4 As String = "UPDATE genii_user.TR SET CurrentBalance= genii_user.cashier_transactions.payment_amt " & _
                                " from genii_user.TR INNER JOIN genii_user.cashier_transactions " & _
                                " on genii_user.TR.TaxYear= genii_user.cashier_transactions.tax_year " & _
                                " and genii_user.TR.Taxrollnumber=genii_user.cashier_transactions.tax_roll_number " & _
                                " and genii_user.cashier_transactions.group_key=" + grpKey + " "

            Dim sql5 As String = "UPDATE genii_user.Tax_Account SET account_balance= genii_user.cashier_transactions.payment_amt " & _
                                " from genii_user.Tax_Account INNER JOIN genii_user.TR " & _
                                " on genii_user.Tax_Account.ParcelOrTaxID=genii_user.TR.TaxIDNumber " & _
                                " inner join genii_user.cashier_transactions " & _
                                " on genii_user.TR.TaxYear= genii_user.cashier_transactions.tax_year " & _
                                " and genii_user.TR.Taxrollnumber=genii_user.cashier_transactions.tax_roll_number " & _
                                " and genii_user.cashier_transactions.group_key=" + grpKey + " "

            Dim cmdUpdateCashierTransaction As New OleDbCommand(sql1, conn3)

            Dim cmdDeleteCashierApportion As New OleDbCommand(sql2, conn3)

            Dim cmdDeleteCashierTRPayments As New OleDbCommand(sql3, conn3)
            Dim cmdUpdateTR As New OleDbCommand(sql4, conn3)
            Dim cmdUpdateTaxAccount As New OleDbCommand(sql5, conn3)

            cmdUpdateCashierTransaction.Transaction = trans3
            cmdDeleteCashierApportion.Transaction = trans3
            cmdDeleteCashierTRPayments.Transaction = trans3
            cmdUpdateTR.Transaction = trans3
            cmdUpdateTaxAccount.Transaction = trans3

            cmdUpdateCashierTransaction.ExecuteNonQuery()
            cmdDeleteCashierApportion.ExecuteNonQuery()
            cmdDeleteCashierTRPayments.ExecuteNonQuery()
            cmdUpdateTR.ExecuteNonQuery()
            cmdUpdateTaxAccount.ExecuteNonQuery()

            trans3.Commit()
            conn3.Close()

        End Using


        Dim success = True
        Return success




    End Function

    Public Sub btnViewDeposit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnViewDeposit.Click


        '  Dim url As String = "/www.dotnetcurry.com"
        '  Dim url As String = "Reports/PostingReport.aspx?SessionID=" & _ Me.lblSession.Text + "&p=0"

        '  ClientScript.RegisterStartupScript(Me.GetType(), "OpenWin", "<script>openNewWin('" & url & "','_blank',height=1000,width=2000)</script>")

        Response.Write("<script>")
        Response.Write("window.open('Reports/PostingReport.aspx?SessionID=" + Me.lblSession.Text + "','_blank')")
        '  Response.Write("window.onfocus = function () { window.close(); }")
        Response.Write("</script>")




    End Sub

    Public Sub btnPrintDeposit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPrintDeposit.Click


        '  Dim url As String = "/www.dotnetcurry.com"
        Dim url As String = "Reports/PostingReport.aspx?SessionID=" + Me.lblSession.Text + "&p=1"

        ClientScript.RegisterStartupScript(Me.GetType(), "OpenWin", "<script>openNewWin('" & url & "','_blank',height=1000,width=2000)</script>")

        '   Response.Write("<script>")
        '   Response.Write("window.open('Reports/PostingReport.aspx?ReceiptNumber=" & newReceiptNumber & "')")
        '   Response.Write("</script>")


        Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewPostDetails('Post Cashier Session');", True)

    End Sub

    '<System.Web.Services.WebMethod()> _
    Public Sub btnCommitDeposit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCommitDeposit.Click

        Dim sql As String
        Dim sql2 As String
        Dim depositDetails As DataSet = New DataSet()
        Dim receiptDetails As DataSet = New DataSet()
        sql = String.Format("SELECT genii_user.CASHIER_SESSION.RECORD_ID AS 'Cashier Session', " & _
                                            "  GETDATE() AS 'ENTRY_DATE', " & _
                                            "   GETDATE() AS 'RECORD_DATE', " & _
                                            "             'xxx' AS 'MEMO', " & _
                                            "   5 AS 'MEMO_NUMBER', " & _
                                            "   GETDATE() AS 'TAX_RECEIPT_OPEN_DATE', " & _
                                            "             'JE-REVDep' + CONVERT(varchar, DATEPART(dd, GETDATE())) AS 'REFERENCE', " & _
                                            "             genii_user.CASHIER_SESSION.POSTED_DATE, " & _
                                            "             genii_user.CASHIER_SESSION.POSTED, " & _
                                            "   SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS 'AMOUNT', " & _
                                            "   GETDATE() AS 'FROM_DATE', " & _
                                            "   GETDATE() AS 'TO_DATE', " & _
                                            "             '' AS 'ENTITY', " & _
                                            "   2 AS 'STATUS', " & _
                                            "             'CFB' AS 'ACCOUNT', " & _
                                            "             'Unknown' AS 'ENTRY', " & _
                                            "   DATEPART(mm, GETDATE()) AS DEPOSIT, " & _
                                            "   0 AS 'USR_RECORD_NUMBER', " & _
                                            "             'False' AS 'PRINT_RECEIPT', " & _
                                            "             'True' AS 'TAXRECEIPT', " & _
                                            "             'genii_user' AS 'CREATE_USER', " & _
                                            "   GETDATE() AS 'CREATE_DATE', " & _
                                            "             'genii_user' AS 'EDIT_USER', " & _
                                            "   GETDATE() AS 'EDIT_DATE'                  " & _
                                            "             FROM genii_user.CASHIER_SESSION " & _
                                            "  INNER JOIN genii_user.CASHIER_TRANSACTIONS " & _
                                            "    ON genii_user.CASHIER_SESSION.RECORD_ID = genii_user.CASHIER_TRANSACTIONS.SESSION_ID " & _
                                            "  INNER JOIN genii_user.CASHIER_APPORTION " & _
                                            "    ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID " & _
                                            " WHERE genii_user.CASHIER_SESSION.RECORD_ID = " + Me.lblSession.Text + " " & _
                                            " GROUP BY genii_user.CASHIER_SESSION.RECORD_ID, " & _
                                            "             genii_user.CASHIER_SESSION.POSTED_DATE,genii_user.CASHIER_SESSION.POSTED ")

        LoadTable(depositDetails, "CASHIER_SESSION", sql)

        Dim row As DataRow
        row = depositDetails.Tables(0).Rows(0)

        Dim cashierSession As Integer
        Dim receiptNumber As String
        Dim entryDate As Date
        Dim recordDate As Date
        Dim memo As String
        Dim memoNumber As Integer
        Dim taxReceiptOpenDate As Date
        Dim reference As String
        Dim amount As Double
        Dim fromDate As DateTime
        Dim toDate As DateTime
        Dim entity As String
        Dim status As Integer
        Dim account As String
        Dim entry As String
        Dim deposit As Integer
        Dim usrRecordNumber As Integer
        Dim printReceipt As Boolean
        Dim taxreceipt As Boolean
        Dim postedDate As DateTime
        Dim isPosted As Integer

        ' Fill in Labels with data from CASHIER_SESSION
        If (IsDBNull(row("Cashier Session"))) Then
            cashierSession = String.Empty
        Else
            cashierSession = row("Cashier Session")
        End If

        If (IsDBNull(row("MEMO"))) Then
            memo = String.Empty
        Else
            memo = row("MEMO")
        End If

        If (IsDBNull(row("MEMO_NUMBER"))) Then
            memoNumber = String.Empty
        Else
            memoNumber = row("MEMO_NUMBER")
        End If

        If (IsDBNull(row("REFERENCE"))) Then
            reference = String.Empty
        Else
            reference = row("REFERENCE")
        End If

        If (IsDBNull(row("AMOUNT"))) Then
            amount = String.Empty
        Else
            amount = row("AMOUNT")
        End If

        If (IsDBNull(row("ENTITY"))) Then
            entity = String.Empty
        Else
            entity = row("ENTITY")
        End If

        If (IsDBNull(row("STATUS"))) Then
            status = String.Empty
        Else
            status = row("STATUS")
        End If

        If (IsDBNull(row("ACCOUNT"))) Then
            account = String.Empty
        Else
            account = row("ACCOUNT")
        End If

        If (IsDBNull(row("ENTRY"))) Then
            entry = String.Empty
        Else
            entry = row("ENTRY")
        End If

        If (IsDBNull(row("DEPOSIT"))) Then
            deposit = String.Empty
        Else
            deposit = row("DEPOSIT")
        End If

        If (IsDBNull(row("USR_RECORD_NUMBER"))) Then
            usrRecordNumber = String.Empty
        Else
            usrRecordNumber = row("USR_RECORD_NUMBER")
        End If

        If (IsDBNull(row("PRINT_RECEIPT"))) Then
            printReceipt = String.Empty
        Else
            printReceipt = row("PRINT_RECEIPT")
        End If

        If (IsDBNull(row("TAXRECEIPT"))) Then
            taxreceipt = String.Empty
        Else
            taxreceipt = row("TAXRECEIPT")
        End If

        ''   sql2 = "SELECT genii_user.CASHIER_APPORTION.ReceiptNumber AS 'RECEIPT_NUMBER', " & _
        '            "  SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS 'AMOUNT',  " & _
        '         "  genii_user.CASHIER_APPORTION.GLAccount AS 'ACCOUNT', " & _
        ''            "  genii_user.CASHIER_POSTING_GL.Description AS 'MEMO', " & _
        '          "        'genii_user' AS 'CREATE_USER', " & _
        '         "  GETDATE() AS 'CREATE_DATE', " & _
        '        "        'genii_user' AS 'EDIT_USER', " & _
        '          "  GETDATE() AS 'EDIT_DATE' " & _
        '         "        FROM genii_user.CASHIER_APPORTION  " & _
        '          "  INNER JOIN genii_user.CASHIER_POSTING_GL " & _
        '         "    ON genii_user.CASHIER_APPORTION.GLAccount = genii_user.CASHIER_POSTING_GL.GLAccount " & _
        '         "        WHERE genii_user.CASHIER_APPORTION.TRANS_ID = " & _ Me.lblSession.Text + " " & _
        '        " GROUP BY genii_user.CASHIER_APPORTION.GLAccount, " & _
        '         "        genii_user.CASHIER_APPORTION.ReceiptNumber, " & _
        '         "        genii_user.CASHIER_POSTING_GL.Description " & _
        '         "        ORDER BY  'ACCOUNT' "

        ' LoadTable(receiptDetails, "CASHIER_APPORTION", sql)

        Dim conn1 As New OleDbConnection(Me.ConnectString)

        conn1.Open()

        Dim receiptDetailsAdapter As OleDbDataAdapter = New OleDbDataAdapter( _
                "SELECT genii_user.CASHIER_APPORTION.ReceiptNumber AS 'RECEIPT_NUMBER', " & _
                                             " SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS 'AMOUNT',  " & _
                                             "  genii_user.CASHIER_APPORTION.GLAccount AS 'ACCOUNT', " & _
                                             "  genii_user.CASHIER_POSTING_GL.Description AS 'MEMO', " & _
                                             "            'genii_user' AS 'CREATE_USER', " & _
                                             "  GETDATE() AS 'CREATE_DATE', " & _
                                             "            'genii_user' AS 'EDIT_USER', " & _
                                             "  GETDATE() AS 'EDIT_DATE'              " & _
                                             "            FROM genii_user.CASHIER_SESSION  " & _
                                             " INNER JOIN genii_user.CASHIER_TRANSACTIONS " & _
                                             "   ON genii_user.CASHIER_SESSION.RECORD_ID = genii_user.CASHIER_TRANSACTIONS.SESSION_ID " & _
                                             " INNER JOIN genii_user.CASHIER_APPORTION " & _
                                             "   ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID " & _
                                             " INNER JOIN genii_user.CASHIER_POSTING_GL " & _
                                             "   ON genii_user.CASHIER_APPORTION.GLAccount = genii_user.CASHIER_POSTING_GL.GLAccount " & _
                                            " WHERE  genii_user.CASHIER_SESSION.RECORD_ID = " + Me.lblSession.Text + " " & _
                                            " GROUP BY genii_user.CASHIER_APPORTION.GLAccount, " & _
                                             "            genii_user.CASHIER_APPORTION.ReceiptNumber, " & _
                                             "            genii_user.CASHIER_POSTING_GL.Description" & _
                                             "            ORDER BY  'ACCOUNT' ", conn1)


        Dim receiptDetailsDS As DataSet = New DataSet()
        receiptDetailsAdapter.Fill(receiptDetailsDS, "receiptDetails")

        Dim receiptDetailsRow As DataRow





        Dim conn As New OleDbConnection(Me.BankConnectString)

        conn.Open()

        Dim trans As OleDbTransaction = conn.BeginTransaction()
        ' Dim newReceiptNumber As Integer
        Try
            ' Get new record id.
            Dim newRecordID As Integer



            Using cmdRecordID As New OleDbCommand("SELECT MAX(RECORD_ID) FROM dbo.RECEIPT_PARENT", conn)
                Dim objRecordID As Object

                cmdRecordID.Transaction = trans
                objRecordID = cmdRecordID.ExecuteScalar()

                If (Not IsDBNull(objRecordID)) AndAlso IsNumeric(objRecordID) Then
                    newRecordID = CInt(objRecordID) + 1
                Else
                    newRecordID = 1
                End If
            End Using

            Using cmdRecordID As New OleDbCommand("SELECT MAX(RECEIPT_NUMBER) FROM dbo.RECEIPT_PARENT", conn)
                Dim objRecordID As Object

                cmdRecordID.Transaction = trans
                objRecordID = cmdRecordID.ExecuteScalar()

                If (Not IsDBNull(objRecordID)) AndAlso IsNumeric(objRecordID) Then
                    newReceiptNumber = CInt(objRecordID) + 1
                Else
                    newReceiptNumber = 1
                End If
            End Using

            lblReceiptNumber.Text = newReceiptNumber

            ' Insert new record.
            Using cmd As New OleDbCommand()
                cmd.CommandText = "INSERT INTO dbo.RECEIPT_PARENT (" & _
                                    " RECORD_ID, RECEIPT_NUMBER, ENTRY_DATE, RECORD_DATE, MEMO, MEMO_NUMBER, " & _
                                    " TAX_RECEIPT_OPEN_DATE, REFERENCE, AMOUNT, FROM_DATE, TO_DATE, ENTITY, " & _
                                    " STATUS, ACCOUNT, ENTRY, DEPOSIT, USR_RECORD_NUMBER, PRINT_RECEIPT, " & _
                                    " TAXRECEIPT, " & _
                                    " CREATE_USER, CREATE_DATE, EDIT_USER, EDIT_DATE" & _
                                    ") VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)"
                cmd.Connection = conn
                cmd.Transaction = trans

                cmd.Parameters.AddWithValue("@RECORD_ID", newRecordID)
                cmd.Parameters.AddWithValue("@RECEIPT_NUMBER", newReceiptNumber)
                cmd.Parameters.AddWithValue("@ENTRY_DATE", DateTime.Now.ToShortDateString)
                cmd.Parameters.AddWithValue("@RECORD_DATE", DateTime.Now.ToShortDateString)
                cmd.Parameters.AddWithValue("@MEMO", memo)
                cmd.Parameters.AddWithValue("@MEMO_NUMBER", memoNumber)
                cmd.Parameters.AddWithValue("@TAX_RECEIPT_OPEN_DATE", DateTime.Now.ToShortDateString)
                cmd.Parameters.AddWithValue("@REFERENCE", reference)
                cmd.Parameters.AddWithValue("@AMOUNT", amount)
                cmd.Parameters.AddWithValue("@FROM_DATE", DateTime.Now)
                cmd.Parameters.AddWithValue("@TO_DATE", DateTime.Now)
                cmd.Parameters.AddWithValue("@ENTITY", entity)
                cmd.Parameters.AddWithValue("@STATUS", status)
                cmd.Parameters.AddWithValue("@ACCOUNT", account)
                cmd.Parameters.AddWithValue("@ENTRY", entry)
                cmd.Parameters.AddWithValue("@DEPOSIT", deposit)
                cmd.Parameters.AddWithValue("@USR_RECORD_NUMBER", usrRecordNumber)
                cmd.Parameters.AddWithValue("@PRINT_RECEIPT", printReceipt)
                cmd.Parameters.AddWithValue("@TAXRECEIPT", taxreceipt)
                cmd.Parameters.AddWithValue("@CREATE_USER", "genii_user")
                cmd.Parameters.AddWithValue("@CREATE_DATE", DateTime.Now.ToShortDateString)
                cmd.Parameters.AddWithValue("@EDIT_USER", "genii_user")
                cmd.Parameters.AddWithValue("@EDIT_DATE", DateTime.Now.ToShortDateString)

                cmd.ExecuteNonQuery()

            End Using

            trans.Commit()

            '   LoadDeposits()

        Catch ex As Exception
            trans.Rollback()
        Finally
            If conn IsNot Nothing AndAlso conn.State = ConnectionState.Open Then
                conn.Close()
            End If
        End Try


        Dim conn2 As New OleDbConnection(Me.BankConnectString)

        conn2.Open()

        Dim trans2 As OleDbTransaction = conn2.BeginTransaction()
        Try
            ' Get new record id.
            Dim newRecordID As Integer

            Using cmdRecordID As New OleDbCommand("SELECT MAX(RECORD_ID) FROM dbo.RECEIPT_DETAIL", conn2)
                Dim objRecordID As Object

                cmdRecordID.Transaction = trans2
                objRecordID = cmdRecordID.ExecuteScalar()

                If (Not IsDBNull(objRecordID)) AndAlso IsNumeric(objRecordID) Then
                    newRecordID = CInt(objRecordID) + 1
                Else
                    newRecordID = 1
                End If
            End Using


            For Each receiptDetailsRow In receiptDetailsDS.Tables("receiptDetails").Rows
                'receiptDetailsRow("RECEIPT_NUMBER").toString()
                'SAVE TO BELOW
                'cmd.Parameters.AddWithValue("@RECEIPT_NUMBER", newReceiptNumber)


                Using cmd As New OleDbCommand()
                    cmd.CommandText = "INSERT INTO dbo.RECEIPT_DETAIL (" & _
                                        " RECORD_ID, RECEIPT_NUMBER, MEMO, " & _
                                        " AMOUNT, ACCOUNT,  " & _
                                        " CREATE_USER, CREATE_DATE, EDIT_USER, EDIT_DATE" & _
                                        ") VALUES (?,?,?,?,?,?,?,?,?)"
                    cmd.Connection = conn2
                    cmd.Transaction = trans2

                    cmd.Parameters.AddWithValue("@RECORD_ID", newRecordID + 1)
                    cmd.Parameters.AddWithValue("@RECEIPT_NUMBER", newReceiptNumber)
                    cmd.Parameters.AddWithValue("@MEMO", receiptDetailsRow("MEMO"))
                    cmd.Parameters.AddWithValue("@AMOUNT", receiptDetailsRow("AMOUNT"))
                    cmd.Parameters.AddWithValue("@ACCOUNT", receiptDetailsRow("ACCOUNT"))
                    cmd.Parameters.AddWithValue("@CREATE_USER", "genii_user")
                    cmd.Parameters.AddWithValue("@CREATE_DATE", DateTime.Now.ToShortDateString)
                    cmd.Parameters.AddWithValue("@EDIT_USER", "genii_user")
                    cmd.Parameters.AddWithValue("@EDIT_DATE", DateTime.Now.ToShortDateString)

                    cmd.ExecuteNonQuery()
                End Using


            Next
            trans2.Commit()



            '   LoadDeposits()


            ' Dim memo2 As String
            '  Dim amount2 As Double
            '  Dim account2 As String
            ' Dim row2 As DataRow
            ' Dim x As Integer
            ' For x = 0 To receiptDetails.Tables("CASHIER_APPORTION").Rows().Count - 1
            'row2 = receiptDetails.Tables(0).Rows(x)

            ' If (IsDBNull(row2("MEMO"))) Then
            'memo2 = String.Empty
            '  Else
            ' memo2 = row2("MEMO")
            ' End If

            ' If (IsDBNull(row2("AMOUNT"))) Then
            'amount2 = String.Empty
            ' Else
            '  amount2 = row2("AMOUNT")
            '  End If

            '  If (IsDBNull(row2("ACCOUNT"))) Then
            'account2 = String.Empty
            '  Else
            ' account2 = row2("ACCOUNT")
            ' End If


            ' Insert new record.

            ' Next



        Catch ex As Exception
            trans2.Rollback()
        Finally
            If conn2 IsNot Nothing AndAlso conn2.State = ConnectionState.Open Then
                conn2.Close()
            End If
        End Try


        ''''''''''''''''''''

        ' Update rows.
        Using conn3 As New OleDbConnection(Me.ConnectString)
            conn3.Open()
            Dim trans3 As OleDbTransaction = conn3.BeginTransaction()

            ' Approvals.
            ' If approveIDs.Count > 0 Then
            Dim cmdCashierSession As New OleDbCommand("UPDATE genii_user.CASHIER_SESSION SET RECEIPT_NUMBER=?,POSTED_DATE = ?, POSTED=?, " & _
                                               "EDIT_USER = ?, EDIT_DATE = ? WHERE RECORD_ID= " + Me.lblSession.Text + " ", conn3)

            cmdCashierSession.Transaction = trans3
            cmdCashierSession.Parameters.AddWithValue("@RECEIPT_NUMBER", newReceiptNumber)
            cmdCashierSession.Parameters.AddWithValue("@POSTED_DATE", Date.Now)
            cmdCashierSession.Parameters.AddWithValue("@POSTED", 1)
            cmdCashierSession.Parameters.AddWithValue("@EDIT_USER", Me.CurrentUserName)
            cmdCashierSession.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
            cmdCashierSession.ExecuteNonQuery()
            ' End If

            trans3.Commit()
            conn3.Close()
        End Using

        LoadDeposits()

        btnPost.Enabled = False
        btnPostLoadSession.Enabled = True

        'Response.Write("<script>")
        'Response.Write("setInterval(function () { document.getElementById('btnPosLoadPosting').click(); }, 200);")
        'Response.Write("</script>")

    End Sub

    Public Sub LoadSessionValues(recordID As String)
        Dim sql As String
        Dim cashierSession As DataSet = New DataSet()
        sql = String.Format("SELECT * FROM vCashierSession WHERE record_id = '{0}'", recordID)

        LoadTable(cashierSession, "vCashierSession", sql)

        Dim row As DataRow
        row = cashierSession.Tables(0).Rows(0)

        ' Fill in Labels with data from CASHIER_SESSION
        If (IsDBNull(row("RECORD_ID"))) Then
            Me.lblSession.Text = String.Empty
        Else
            Me.lblSession.Text = row("RECORD_ID")
        End If

        If (IsDBNull(row("CASHIER"))) Then
            Me.lblOperator.Text = String.Empty
        Else
            Me.lblOperator.Text = row("CASHIER")
        End If

        If (IsDBNull(row("START_TIME"))) Then
            Me.lblDepositOpened.Text = String.Empty
        Else
            Me.lblDepositOpened.Text = row("START_TIME")
        End If

        If (IsDBNull(row("END_TIME"))) Then
            Me.lblDepositClosed.Text = String.Empty
        Else
            Me.lblDepositClosed.Text = row("END_TIME")
        End If

        If (IsDBNull(row("END_TIME"))) Then
            Me.lblDepositClosed.Text = String.Empty
        Else
            Me.lblDepositClosed.Text = row("END_TIME")
        End If

        If (IsDBNull(row("TRANS_COUNT"))) Then
            Me.lblTransactions.Text = String.Empty
        Else
            Me.lblTransactions.Text = row("TRANS_COUNT")
        End If

        If (IsDBNull(row("PAYMENT_AMT"))) Then
            Me.lblAmount.Text = String.Empty
        Else
            Me.lblAmount.Text = "$" & row("PAYMENT_AMT")
        End If


    End Sub


    Public Sub LoadTable(container As DataSet, tableName As String, query As String)
        'Dim adt As OleDbDataAdapter


        Using adt As New OleDbDataAdapter(query, Me.ConnectString)
            adt.Fill(container, tableName)
        End Using


    End Sub

#End Region


    ''' <summary>
    ''' LoadDeposits - Load Deposit Data from vCashierSession
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub LoadDeposits()
        Dim depositsTable As New DataTable()

        Using adt As New OleDbDataAdapter(String.Empty, Me.ConnectString)

            Dim sql As String = "SELECT a.*,b.receipt_number,b.posted_date FROM dbo.vCashierSession a, genii_user.CASHIER_SESSION b WHERE a.record_id=b.record_id and   "

            If Me.rdoPosDate.Checked Then
                sql &= String.Format("b.posted_date BETWEEN '{0}' AND DATEADD(d, 2, '{0}') AND a.POSTED = 1 order by b.record_id", Me.txtPosDate.Text) 'a.END_TIME BETWEEN '{0}' AND DATEADD(d, 10, '{0}') AND
                ' sql &= "POSTED = 1"
            Else
                sql &= "a.POSTED = 0 order by b.record_id"
            End If

            adt.SelectCommand.CommandText = sql

            adt.SelectCommand.Connection.Open()

            Dim depositsDataAdapter As New OleDbDataAdapter(adt.SelectCommand)

            depositsDataAdapter.Fill(depositsTable)
        End Using

        If depositsTable.Rows.Count > 0 Then
            Me.grdPosDeposits.DataSource = depositsTable
            Me.grdPosDeposits.DataBind()
            lblNoDepositData.Visible = False
        Else
            lblNoDepositData.Visible = True
        End If
    End Sub


    ''' <summary>
    ''' LoadSessionTransactions
    ''' </summary>
    ' ''' <param name="sessionID"></param>
    ''' <remarks></remarks>
    Private Sub LoadSessionTransactions(ByVal sessionID As Integer)
        Dim sessionTransactionsTable As New DataTable()

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("SELECT RECORD_ID,GROUP_KEY,ISNULL(TRANSACTION_STATUS,0)AS TRANSACTION_STATUS,TAX_YEAR,TAX_ROLL_NUMBER,PAYMENT_DATE, PAYMENT_TYPE, PAYMENT_AMT,PAYOR_NAME, CHECK_NUMBER,TAX_AMT,REFUND_AMT,KITTY_AMT,CASE  " & _
                                        " WHEN TRANSACTION_STATUS is null THEN 'STATUS IS NULL' " & _
                                        " WHEN TRANSACTION_STATUS=1 THEN 'NOT POSTED' " & _
                                        " WHEN TRANSACTION_STATUS=2 THEN 'POSTED' " & _
                                        " WHEN TRANSACTION_STATUS=3 THEN 'CANCELED PRIOR' " & _
                                        " WHEN TRANSACTION_STATUS=4 THEN 'REVERSED AFTER' " & _
                                        " END AS TRANS_STATUS FROM genii_user.CASHIER_TRANSACTIONS WHERE SESSION_ID = " & sessionID & "")

            cmd.Connection = conn

            conn.Open()

            Dim sessionTransactionsDataAdapter As New OleDbDataAdapter(cmd)

            sessionTransactionsDataAdapter.Fill(sessionTransactionsTable)
        End Using

        If sessionTransactionsTable.Rows.Count > 0 Then
            Me.grdPosTransactions.DataSource = sessionTransactionsTable
            Me.grdPosTransactions.DataBind()
            lblNoPosTransactionData.Visible = False
        Else
            Me.grdPosTransactions.DataSource = sessionTransactionsTable
            Me.grdPosTransactions.DataBind()
            lblNoPosTransactionData.Visible = True
        End If
    End Sub

    Private Sub LoadSessionTransactionsReverse(ByVal sessionID As Integer)
        Dim sessionTransactionsTable As New DataTable()

        Dim myUtil As New Utilities()
        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdCashierTrans As New OleDbCommand("UPDATE genii_user.CASHIER_TRANSACTIONS SET TRANSACTION_STATUS = 1 WHERE TRANSACTION_STATUS IS NULL")

                cmdCashierTrans.Connection = conn
                cmdCashierTrans.Transaction = trans

                'With cmdTaxAccount.Parameters
                '    .AddWithValue("@PARENT_BALANCE", 1)

                'End With

                cmdCashierTrans.ExecuteNonQuery()
                '    lblFdBk.Text = "Unsecured Delinquent accounts sent to the Sheriff."


                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
            conn.Close()
        End Using

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("SELECT *,CASE " & _
                                        " WHEN TRANSACTION_STATUS=1 THEN 'NOT POSTED' " & _
                                        " WHEN TRANSACTION_STATUS=2 THEN 'POSTED' " & _
                                        " WHEN TRANSACTION_STATUS=3 THEN 'CANCELED PRIOR' " & _
                                        " WHEN TRANSACTION_STATUS=4 THEN 'REVERSED AFTER' " & _
                                        " END AS TRANS_STATUS FROM genii_user.CASHIER_TRANSACTIONS WHERE SESSION_ID = " & sessionID & "")

            cmd.Connection = conn

            conn.Open()

            Dim sessionTransactionsDataAdapter As New OleDbDataAdapter(cmd)

            sessionTransactionsDataAdapter.Fill(sessionTransactionsTable)
        End Using

        If sessionTransactionsTable.Rows.Count > 0 Then
            Me.grdPosTransactionsReverse.DataSource = sessionTransactionsTable
            Me.grdPosTransactionsReverse.DataBind()
            lblNoPosTransactionData.Visible = False
        Else
            Me.grdPosTransactionsReverse.DataSource = sessionTransactionsTable
            Me.grdPosTransactionsReverse.DataBind()
            lblNoPosTransactionData.Visible = True
        End If
    End Sub

    ''' <summary>
    ''' LoadSessionApportionments
    ''' </summary>
    ''' <param name="sessionID"></param>
    ''' <remarks></remarks>
    Private Sub LoadSessionApportionments(ByVal sessionID As Integer)
        Dim sessionApportionmentsTable As New DataTable()

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("SELECT TA.* FROM genii_user.CASHIER_APPORTION TA, genii_user.CASHIER_TRANSACTIONS CT WHERE TA.TRANS_ID = CT.RECORD_ID AND CT.SESSION_ID = " & sessionID)

            cmd.Connection = conn

            conn.Open()

            Dim sessionApportionmentsDataAdapter As New OleDbDataAdapter(cmd)

            sessionApportionmentsDataAdapter.Fill(sessionApportionmentsTable)
        End Using

        If sessionApportionmentsTable.Rows.Count > 0 Then
            Me.grdPosApportionments.DataSource = sessionApportionmentsTable
            Me.grdPosApportionments.DataBind()
            lblNoPosApportionmentData.Visible = False
        Else
            lblNoPosApportionmentData.Visible = True
        End If

    End Sub


    ''' <summary>
    ''' LoadSessionRefunds
    ''' </summary>
    ''' <param name="sessionID"></param>
    ''' <remarks></remarks>
    Private Sub LoadSessionRefunds(ByVal sessionID As Integer)
        Dim sessionRefundsTable As New DataTable()

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("select * from genii_user.CASHIER_TRANSACTIONS where REFUND_AMT <> 0 and SESSION_ID = " & sessionID)
            cmd.Connection = conn

            conn.Open()

            Dim sessionRefundsDataAdapter As New OleDbDataAdapter(cmd)

            sessionRefundsDataAdapter.Fill(sessionRefundsTable)
        End Using

        If sessionRefundsTable.Rows.Count > 0 Then
            Me.grdPosRefunds.DataSource = sessionRefundsTable
            Me.grdPosRefunds.DataBind()
            lblNoPosRefundData.Visible = False
        Else
            lblNoPosRefundData.Visible = True
        End If
    End Sub


    ''' <summary>
    ''' LoadSessionKittyAmounts
    ''' </summary>
    ''' <param name="sessionID"></param>
    ''' <remarks></remarks>
    Private Sub LoadSessionKittyAmounts(ByVal sessionID As Integer)
        Dim sessionKittyTable As New DataTable()

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("SELECT * FROM genii_user.CASHIER_TRANSACTIONS WHERE KITTY_AMT <> 0 AND SESSION_ID = " & sessionID)

            cmd.Connection = conn

            conn.Open()

            Dim sessionKittyDataAdapter As New OleDbDataAdapter(cmd)

            sessionKittyDataAdapter.Fill(sessionKittyTable)
        End Using

        If sessionKittyTable.Rows.Count > 0 Then
            Me.grdPosKittyFunds.DataSource = sessionKittyTable
            Me.grdPosKittyFunds.DataBind()
            lblNoPosKittyData.Visible = False
        Else
            lblNoPosKittyData.Visible = True
        End If
    End Sub


    ''' <summary>
    ''' LoadSessionDeclinedAmounts
    ''' </summary>
    ''' <param name="sessionID"></param>
    ''' <remarks></remarks>
    Private Sub LoadSessionDeclinedAmounts(ByVal sessionID As Integer)
        Dim sessionDeclinedTable As New DataTable()

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("SELECT * FROM genii_user.CASHIER_REJECTED_CHECK WHERE SESSION_ID = " & sessionID)

            cmd.Connection = conn

            conn.Open()

            Dim sessionDeclinedDataAdapter As New OleDbDataAdapter(cmd)

            sessionDeclinedDataAdapter.Fill(sessionDeclinedTable)
        End Using

        If sessionDeclinedTable.Rows.Count > 0 Then
            Me.grdPosDeclinedPayments.DataSource = sessionDeclinedTable
            Me.grdPosDeclinedPayments.DataBind()
            lblNoPosDeclinedData.Visible = False
        Else
            lblNoPosDeclinedData.Visible = True
        End If
    End Sub
#End Region


    '#Region "Refunds Tab"
    '    Protected Sub btnLoadRefund_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLoadRefund.Click
    '        LoadRefunds()
    '    End Sub

    '    Protected Sub btnSaveRefund_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSaveRefund.Click

    '    End Sub


    '    ''' <summary>
    '    ''' LoadRefunds - Load Refund Data from Cashier_Transactions
    '    ''' </summary>
    '    ''' <remarks></remarks>
    '    Private Sub LoadRefunds()
    '        Dim refundsTable As New DataTable()

    '        Using conn As New OleDbConnection(Me.ConnectString)
    '            '  Dim sql As String = "SELECT * FROM genii_user.CASHIER_TRANSACTIONS WHERE REFUND_AMT <> 0 AND (IS_REFUNDED <> 1 OR IS_REFUNDED IS NULL) ORDER BY PAYMENT_DATE"
    '            Dim sql As String = "SELECT * FROM genii_user.CASHIER_TRANSACTIONS WHERE REFUND_AMT <> 0 AND (REFUND_TAG IN (1,2)) ORDER BY PAYMENT_DATE"
    '            Dim cmd As New OleDbCommand(sql, conn)

    '            conn.Open()
    '            Dim refundsDataAdapter As New OleDbDataAdapter(cmd)

    '            refundsDataAdapter.Fill(refundsTable)
    '        End Using

    '        If refundsTable.Rows.Count > 0 Then
    '            Me.grdRefunds.DataSource = refundsTable
    '            Me.grdRefunds.DataBind()
    '            lblNoRefundData.Visible = False
    '        Else
    '            lblNoRefundData.Visible = True
    '        End If
    '    End Sub

    '    ''' <summary>
    '    ''' GetRefundDay - 
    '    ''' </summary>
    '    ''' <param name="paymentDate"></param>
    '    ''' <returns></returns>
    '    ''' <remarks></remarks>
    '    Protected Function GetRefundDays(ByVal paymentDate As Object) As Integer
    '        If IsDate(paymentDate) Then
    '            Dim dt As DateTime = CDate(paymentDate)
    '            Return Date.Now.Subtract(dt).Days
    '        Else
    '            Return 0
    '        End If
    '    End Function
    '#End Region

#Region "Lender Processing Services"
    'Protected Sub btnLPSLoad_Click() ' ByVal sender As Object, ByVal e As System.EventArgs Handles btnLPSLoad.Click
    '    LoadLPS2()
    'End Sub


    ''' <summary>
    ''' LoadLPS - Load all Lender Processing Services data from ST_LENDER_PROCESSIG_SERVICES
    ''' </summary>
    ''' <remarks></remarks>
    'Public Sub LoadLPS()
    '    Dim lpsTable As New DataTable()

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand("SELECT * FROM genii_user.ST_LENDER_PROCESSING_SERVICES")

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

    '        lpsDataAdapter.Fill(lpsTable)
    '    End Using

    '    If lpsTable.Rows.Count > 0 Then
    '        Me.grdLPS.DataSource = lpsTable
    '        Me.grdLPS.DataBind()
    '        lblLPSNoData.Visible = False
    '    Else
    '        lblLPSNoData.Visible = True
    '    End If

    '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCheckLPSActions('LPS');", True)

    'End Sub

    'Public Sub LoadDailyLetters()
    '    Dim dailyLettersTable As New DataTable()
    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand("SELECT CONVERT(varchar, genii_user.CASHIER_TRANSACTIONS.RECORD_ID) + ' (' " & _
    '                                    "    + Convert(varchar, genii_user.CASHIER_TRANSACTIONS.GROUP_KEY) " & _
    '                                    "     + ')' AS 'Trans (Group)', " & _
    '                                    "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR + ' ('  " & _
    '                                    "     + genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER " & _
    '                                    "     + ')' AS 'Year (Roll)', " & _
    '                                    "   CONVERT(varchar(10), genii_user.CASHIER_TRANSACTIONS.PAYMENT_DATE, 101) AS 'Date', " & _
    '                                    "   CASE genii_user.CASHIER_TRANSACTIONS.LETTER_TAG " & _
    '                                    "     WHEN 1 THEN 'Payment with Balance' " & _
    '                                    "     WHEN 2 THEN 'Payment with CP' " & _
    '                                    "     WHEN 3 THEN 'Both Balance and CP' " & _
    '                                    "       END AS 'Letter Reason', " & _
    '                                    "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT AS 'Payment', " & _
    '                                    "   genii_user.TAX_ACCOUNT.ACCOUNT_BALANCE AS 'Account Balance', " & _
    '                                    "  (SELECT COUNT(*) FROM genii_user.TR_CP WHERE genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CP.TaxYear " & _
    '                                    "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CP.TaxRollNumber) AS 'CP Count' " & _
    '                                    "             FROM genii_user.CASHIER_TRANSACTIONS " & _
    '                                    "   INNER JOIN genii_user.TR " & _
    '                                    "     ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR.TaxYear " & _
    '                                    "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR.TaxRollNumber " & _
    '                                    "   INNER JOIN genii_user.TAX_ACCOUNT " & _
    '                                    "     ON genii_user.TR.TaxIDNumber = genii_user.TAX_ACCOUNT.ParcelOrTaxID " & _
    '                                    "   LEFT OUTER JOIN genii_user.TR_CP " & _
    '                                    "     ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CP.TaxYear " & _
    '                                    "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CP.TaxRollNumber " & _
    '                                    "  WHERE     LETTER_TAG IN (1,2,3)")

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim dailyLettersDataAdapter As New OleDbDataAdapter(cmd)

    '        dailyLettersDataAdapter.Fill(dailyLettersTable)
    '    End Using

    '    If dailyLettersTable.Rows.Count > 0 Then
    '        Me.grdDailyLetters.DataSource = dailyLettersTable
    '        Me.grdDailyLetters.DataBind()
    '    End If

    'End Sub

    'Public Sub LoadLPS2()
    '    Dim lpsTable As New DataTable()

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand("SELECT * FROM genii_user.ST_LENDER_PROCESSING_SERVICES")

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

    '        lpsDataAdapter.Fill(lpsTable)
    '    End Using

    '    If lpsTable.Rows.Count > 0 Then
    '        Me.grdLPS2.DataSource = lpsTable
    '        Me.grdLPS2.DataBind()
    '    End If

    '    ' Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCheckLPSActions('LPS');", True)

    'End Sub

    'Public Sub LoadCAD()
    '    Dim lpsTable As New DataTable()

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand("SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
    '                                    "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
    '                                    "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
    '                                    "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
    '                                    "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
    '                                    "   genii_user.TR.CurrentBalance AS 'Balance', " & _
    '                                    "   CASE " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid = 0 THEN 'Match' " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid > 0 THEN 'Partial' " & _
    '                                    "     ELSE 'Refund' END AS 'Status' " & _
    '                                    "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
    '                                    "   INNER JOIN genii_user.TR " & _
    '                                    "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
    '                                    "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
    '                                    "             WHERE genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.Paid <> 0 " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
    '                                    " ORDER BY STATUS, genii_user.CASHIER_WEB_PAYMENTS.DatePosted")

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

    '        lpsDataAdapter.Fill(lpsTable)
    '    End Using

    '    If lpsTable.Rows.Count > 0 Then
    '        Me.grdCAD.DataSource = lpsTable
    '        Me.grdCAD.DataBind()
    '    End If

    '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    'End Sub

    'Public Sub LoadCAD2()
    '    Dim lpsTable As New DataTable()

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand("SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
    '                                    "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
    '                                    "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
    '                                    "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
    '                                    "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
    '                                    "   genii_user.TR.CurrentBalance AS 'Balance', " & _
    '                                    "   CASE " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid = 0 THEN 'Match' " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid > 0 THEN 'Partial' " & _
    '                                    "     ELSE 'Refund' END AS 'Status' " & _
    '                                    "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
    '                                    "   INNER JOIN genii_user.TR " & _
    '                                    "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
    '                                    "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
    '                                    "             WHERE genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.Paid <> 0 " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
    '                                    " ORDER BY STATUS, genii_user.CASHIER_WEB_PAYMENTS.DatePosted")

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

    '        lpsDataAdapter.Fill(lpsTable)
    '    End Using

    '    If lpsTable.Rows.Count > 0 Then
    '        Me.grdCAD2.DataSource = lpsTable
    '        Me.grdCAD2.DataBind()
    '    End If

    '    '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    'End Sub

    'Public Sub LoadCADMatch()
    '    Dim lpsTable As New DataTable()

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand("SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
    '                                    "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
    '                                    "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
    '                                    "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
    '                                    "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
    '                                    "   genii_user.TR.CurrentBalance AS 'Balance', " & _
    '                                    "   CASE " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid = 0 THEN 'Match' " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid > 0 THEN 'Partial' " & _
    '                                    "     ELSE 'Refund' END AS 'Status' " & _
    '                                    "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
    '                                    "   INNER JOIN genii_user.TR " & _
    '                                    "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
    '                                    "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
    '                                    "             WHERE genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.Paid <> 0 " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
    '                                    "   AND genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid = 0" & _
    '                                    " ORDER BY STATUS, genii_user.CASHIER_WEB_PAYMENTS.DatePosted")

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

    '        lpsDataAdapter.Fill(lpsTable)
    '    End Using

    '    If lpsTable.Rows.Count > 0 Then
    '        Me.grdCAD2.DataSource = lpsTable
    '        Me.grdCAD2.DataBind()
    '    End If

    '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    'End Sub

    'Public Sub LoadCADPartial()
    '    Dim lpsTable As New DataTable()

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand("SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
    '                                    "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
    '                                    "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
    '                                    "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
    '                                    "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
    '                                    "   genii_user.TR.CurrentBalance AS 'Balance', " & _
    '                                    "   CASE " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid = 0 THEN 'Match' " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid > 0 THEN 'Partial' " & _
    '                                    "     ELSE 'Refund' END AS 'Status' " & _
    '                                    "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
    '                                    "   INNER JOIN genii_user.TR " & _
    '                                    "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
    '                                    "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
    '                                    "             WHERE genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.Paid <> 0 " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
    '                                    "   AND genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid > 0" & _
    '                                    " ORDER BY STATUS, genii_user.CASHIER_WEB_PAYMENTS.DatePosted")

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

    '        lpsDataAdapter.Fill(lpsTable)
    '    End Using

    '    If lpsTable.Rows.Count > 0 Then
    '        Me.grdCAD2.DataSource = lpsTable
    '        Me.grdCAD2.DataBind()
    '    End If

    '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    'End Sub

    'Public Sub LoadCADRefund()
    '    Dim lpsTable As New DataTable()

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand("SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
    '                                    "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
    '                                    "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
    '                                    "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
    '                                    "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
    '                                    "   genii_user.TR.CurrentBalance AS 'Balance', " & _
    '                                    "   CASE " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid = 0 THEN 'Match' " & _
    '                                    "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid > 0 THEN 'Partial' " & _
    '                                    "     ELSE 'Refund' END AS 'Status' " & _
    '                                    "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
    '                                    "   INNER JOIN genii_user.TR " & _
    '                                    "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
    '                                    "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
    '                                    "             WHERE genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.Paid <> 0 " & _
    '                                    "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
    '                                    "   AND genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid < 0" & _
    '                                    " ORDER BY STATUS, genii_user.CASHIER_WEB_PAYMENTS.DatePosted")

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

    '        lpsDataAdapter.Fill(lpsTable)
    '    End Using

    '    If lpsTable.Rows.Count > 0 Then
    '        Me.grdCAD2.DataSource = lpsTable
    '        Me.grdCAD2.DataBind()
    '    End If

    '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    'End Sub
    Private Sub DoLogout()
        'Dim endCash As Decimal = Me.txtLogoutEndCash.Text
        'Dim requiredCash As Decimal = Me.txtLogoutRequiredCash.Text

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("UPDATE genii_user.CASHIER_SESSION SET END_TIME = ?, END_CASH = ?, REQUIRED_CASH = ?, EDIT_USER=?, EDIT_DATE=? WHERE RECORD_ID = ?")

            cmd.Connection = conn

            'cmd.Parameters.AddWithValue("@END_TIME", Date.Now)
            'cmd.Parameters.AddWithValue("@END_CASH", endCash)
            'cmd.Parameters.AddWithValue("@REQUIRED_CASH", requiredCash)
            'cmd.Parameters.AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
            'cmd.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
            'cmd.Parameters.AddWithValue("@RECORD_ID", Me.SessionRecordID)

            conn.Open()
            cmd.ExecuteNonQuery()
            ' Me.SessionRecordID = 0

            ' Prompt to start new session?
            StartNewSession()
        End Using
    End Sub

    Public Sub ProcessCAD()
        StartNewSession()
        ''process CAD


        ''Logout
        DoLogout()
    End Sub


    ''' <summary>
    ''' GetTRCountByYear - Get TR Counts from DB by Year
    ''' </summary>
    ''' <param name="taxYear"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetTRCountByYear(ByVal taxYear As Integer) As Integer
        If _trCount Is Nothing Then
            _trCount = New Generic.Dictionary(Of Integer, Integer)()
        End If

        If Not _trCount.ContainsKey(taxYear) Then
            Using conn As New OleDbConnection(Me.ConnectString)
                Dim cmd As New OleDbCommand("SELECT COUNT(*) FROM genii_user.TR WHERE TaxPayerID > 0 AND TaxYear = " & taxYear)
                cmd.Connection = conn

                conn.Open()
                _trCount.Add(taxYear, cmd.ExecuteScalar())
            End Using
        End If

        Return _trCount(taxYear)
    End Function

    ''' <summary>
    ''' GetLPSData - Get LPS Data from Database
    ''' </summary>
    ''' <param name="lpsID"></param>
    ''' <param name="taxYear"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Protected Function GetLPSData(ByVal lpsID As Integer, ByVal taxYear As Integer) As String
        ' Get LPS count for year.
        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand()
            cmd.CommandText = String.Format("select count(*) from genii_user.TR where TaxYear = {0} and TaxPayerID = {1}", taxYear, lpsID)
            cmd.Connection = conn

            conn.Open()
            Dim lpsCount As Integer = cmd.ExecuteScalar()
            Return String.Format("{0:###.##}% ({1}) <input type='hidden' value='{1}' />", Divide(lpsCount, GetTRCountByYear(taxYear)) * 100, lpsCount)
        End Using
    End Function
#End Region

#Region "Letters"
    ''' <summary>
    ''' BindLettersGrid - Bind grdLetters with data
    ''' </summary>
    ''' <remarks></remarks>
    'Private Sub BindLettersGrid()
    '    Dim lettersTable As New DataTable()

    '    Dim sql As String = "SELECT LTR.RECORD_ID, LTR.LETTER_TYPE, CLT.[DESCRIPTION], LTR.CASHIER, LTR.TAX_YEAR, LTR.TAX_ROLL_NUMBER, LTR.LETTER_DATE, LTR.APPROVED, " & _
    '                        "ISNULL(TR.OWNER_NAME_3, '') + ' ' + ISNULL(TR.OWNER_NAME_2, '') + ' ' + ISNULL(TR.OWNER_NAME_1, '') AS OWNER_NAME  " & _
    '                        "FROM genii_user.CASHIER_LETTERS AS LTR  " & _
    '                        "INNER JOIN genii_user.TR AS TR ON LTR.TAX_YEAR = TR.TaxYear AND LTR.TAX_ROLL_NUMBER = TR.TaxRollNumber  " & _
    '                        "INNER JOIN genii_user.CASHIER_LETTER_TYPES CLT ON LTR.LETTER_TYPE = CLT.RECORD_ID " & _
    '                        "WHERE LTR.PRINTED IS NULL OR LTR.PRINTED = 0"

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand(sql)

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim lettersDataAdapter As New OleDbDataAdapter(cmd)

    '        lettersDataAdapter.Fill(lettersTable)
    '    End Using

    '    If lettersTable.Rows.Count > 0 Then
    '        Me.grdLetters.DataSource = lettersTable
    '        Me.grdLetters.DataBind()
    '        lblNoLetterData.Visible = False
    '    Else
    '        lblNoLetterData.Visible = True
    '    End If
    'End Sub

    'Protected Sub btnLettersSave_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLettersSave.Click
    '    ' Get record ids of rows to update.
    '    Dim approveIDs As New Generic.List(Of String)()
    '    Dim nonApprovedIDs As New Generic.List(Of String)()

    '    For Each row As GridViewRow In Me.grdLetters.Rows
    '        Dim chkLetterApproved As CheckBox = row.FindControl("chkLetterApproved")
    '        Dim hdnRecordID As HiddenField = row.FindControl("hdnRecordID")

    '        If chkLetterApproved.Checked Then
    '            approveIDs.Add(hdnRecordID.Value)
    '        Else
    '            nonApprovedIDs.Add(hdnRecordID.Value)
    '        End If
    '    Next

    '    ' Update rows.
    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        conn.Open()
    '        Dim trans As OleDbTransaction = conn.BeginTransaction()

    '        ' Approvals.
    '        If approveIDs.Count > 0 Then
    '            Dim cmdApprove As New OleDbCommand("UPDATE genii_user.CASHIER_LETTERS SET APPROVED = 1, " & _
    '                                               "EDIT_USER = ?, EDIT_DATE = ? WHERE RECORD_ID IN " & TaxSupervisor.CreateIDsString(approveIDs), conn)

    '            cmdApprove.Transaction = trans
    '            cmdApprove.Parameters.AddWithValue("@EDIT_USER", Me.CurrentUserName)
    '            cmdApprove.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
    '            cmdApprove.ExecuteNonQuery()
    '        End If


    '        ' Non-Approvals.
    '        If nonApprovedIDs.Count > 0 Then
    '            Dim cmdUnapprove As New OleDbCommand("UPDATE genii_user.CASHIER_LETTERS SET APPROVED = 0, " & _
    '                                                 "EDIT_USER = ?, EDIT_DATE = ? WHERE RECORD_ID IN " & TaxSupervisor.CreateIDsString(nonApprovedIDs), conn)

    '            cmdUnapprove.Transaction = trans
    '            cmdUnapprove.Parameters.AddWithValue("@EDIT_USER", Me.CurrentUserName)
    '            cmdUnapprove.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
    '            cmdUnapprove.ExecuteNonQuery()
    '        End If

    '        trans.Commit()
    '    End Using

    '    BindLettersGrid()
    'End Sub
#End Region

#Region "Foreclosures"
    'Private Sub BindForeclosuresGrid()
    '    Dim foreclosuresTable As New DataTable()

    '    Dim sql As String = "SELECT TAD.apn, investorid, initiated, completed, deedtype, TAD.taxyear, TAD.taxrollnumber, " & _
    '                        "cancelled = CASE Cancelled WHEN '0' THEN 'No' ELSE 'Yes' END " & _
    '                        "FROM genii_user.TAX_ACCOUNT_DEED as TAD  " & _
    '                        "INNER JOIN genii_user.TR as TR on TAD.TaxYear = TR.TaxYear and TAD.TaxRollNumber = TR.TaxRollNumber  "

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        Dim cmd As New OleDbCommand(sql)

    '        cmd.Connection = conn

    '        conn.Open()

    '        Dim foreclosuresDataAdapter As New OleDbDataAdapter(cmd)

    '        foreclosuresDataAdapter.Fill(foreclosuresTable)
    '    End Using

    '    If foreclosuresTable.Rows.Count > 0 Then
    '        Me.grdForeclosures.DataSource = foreclosuresTable
    '        Me.grdForeclosures.DataBind()
    '        lblNoForeclosuresData.Visible = False
    '    Else
    '        lblNoForeclosuresData.Visible = True
    '    End If
    'End Sub

    'Private Sub BindViewCPOwnedGrid()

    '    Dim sql As String = "SELECT CertificateNumber AS 'CP', " & _
    '                             "CONVERT(varchar(10), DateCPPurchased, 101) AS 'Purchase Date', FaceValueOfCP AS 'Face Value', " & _
    '                             "PurchaseValue AS 'Purchase Value' " & _
    '                             "FROM genii_user.TR_CP AS TRCP " & _
    '                             "INNER JOIN genii_user.TAX_ACCOUNT_DEED AS TAD ON TAD.TaxYear = TRCP.TaxYear AND TAD.TaxRollNumber = TRCP.TaxRollNumber"

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        conn.Open()

    '        Dim cmd As New OleDbCommand(sql, conn)
    '        Me.grdViewCPOwned.DataSource = cmd.ExecuteReader()
    '        Me.grdViewCPOwned.DataBind()
    '    End Using
    'End Sub

    'Private Sub BindViewCPHelpGrid()
    '    Dim sql As String = "SELECT CertificateNumber AS 'CP', " & _
    '                             "CONVERT(varchar(10), DateCPPurchased, 101) AS 'Purchase Date', FaceValueOfCP AS 'Face Value', " & _
    '                             "PurchaseValue AS 'Purchase Value' " & _
    '                             "FROM genii_user.TR_CP " & _
    '                             "INNER JOIN genii_user.TAX_ACCOUNT_DEED AS TAD ON TAD.TaxYear = TRCP.TaxYear AND TAD.TaxRollNumber = TRCP.TaxRollNumber"

    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        conn.Open()

    '        Dim cmd As New OleDbCommand(sql, conn)
    '        Me.grdViewCPHelp.DataSource = cmd.ExecuteReader()
    '        Me.grdViewCPHelp.DataBind()
    '    End Using
    'End Sub



#End Region

    Private Function Divide(ByVal numerator As Decimal, ByVal denominator As Decimal) As Decimal
        If denominator = 0 Then
            Return 0
        Else
            Return numerator / denominator
        End If
    End Function

#Region "Sale Preparation"
    Private Sub PopulateSalePrepYears()
        Dim dt As New DataTable()

        Using adp As New OleDbDataAdapter("SELECT DISTINCT TaxYear FROM genii_user.TR ORDER BY TaxYear DESC", Me.ConnectString)
            adp.Fill(dt)

            With Me.ddlSalePrepYear
                .DataTextField = "TaxYear"
                .DataValueField = "TaxYear"
                .DataSource = dt
                .DataBind()
            End With
        End Using

        ' Select previous year.
        Dim selectedYear As Integer

        If Date.Today.Month >= 3 Then
            selectedYear = Date.Today.Year - 1
        Else
            selectedYear = Date.Today.Year - 2
        End If

        Dim item As ListItem = Me.ddlSalePrepYear.Items.FindByValue(selectedYear)

        If item IsNot Nothing Then
            Me.ddlSalePrepYear.SelectedItem.Text = item.Text
            ''item.Selected = True
        End If

        ' Initialize label text.
        Me.lblSalePrepNumCandidates.Text = Me.SalePrepInitialMessage
        Me.lblSalePrepNumAdvFee.Text = Me.SalePrepInitialMessage
        Me.lblSalePrepNumCPShell.Text = Me.SalePrepInitialMessage
        Me.lblSalePrepNumSoldAtAuction.Text = Me.SalePrepInitialMessage
        Me.lblSalePrepUnassignedCPs.Text = Me.SalePrepInitialMessage
    End Sub

    Protected Sub btnSalePrepGo_Click(sender As Object, e As System.EventArgs) Handles btnSalePrepGo.Click
        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()
            Dim cmd As New OleDbCommand()
            cmd.Connection = conn
            Dim taxYear As Integer = CInt(Me.ddlSalePrepYear.SelectedValue)

            ' Tax sale candidates.
            cmd.CommandText = "select count(*) from genii_user.TR where SecuredUnsecured='S' and CurrentBalance>0 and TaxYear=" & taxYear
            Dim numCandidates As Integer = CInt(cmd.ExecuteScalar())
            Me.lblSalePrepNumCandidates.Text = numCandidates.ToString()

            ' Candidates not assigned advertisement fee.
            cmd.CommandText = "SELECT COUNT(*) FROM genii_user.TR AS TR " & _
                              "LEFT OUTER JOIN (SELECT TaxYear, TaxRollNumber FROM genii_user.TR_CHARGES WHERE TaxChargeCodeID = 99902) AS AdvFees " & _
                              "ON TR.TaxYear = AdvFees.TaxYear AND TR.TaxRollNumber = AdvFees.TaxRollNumber WHERE TR.CurrentBalance > 0 AND TR.SecuredUnsecured = 'S' " & _
                              "AND AdvFees.TaxYear IS NULL AND TR.TaxYear = " & taxYear

            Dim numNoAdvFee As Integer = CInt(cmd.ExecuteScalar())
            Me.lblSalePrepNumAdvFee.Text = numNoAdvFee.ToString()

            ' Date fees posted.
            cmd.CommandText = "SELECT MAX(EDIT_DATE) FROM genii_user.TR_CHARGES WHERE TaxChargeCodeID = '99902' AND TaxYear = " & taxYear
            Dim maxFeeDate As Object = cmd.ExecuteScalar()
            If IsDBNull(maxFeeDate) Then
                Me.lblSalePrepDateFeesPosted.Text = String.Empty
            Else
                Me.lblSalePrepDateFeesPosted.Text = CDate(maxFeeDate).ToShortDateString() & "<br />"
            End If
            Me.btnSalePrepPostFees.Enabled = (numNoAdvFee > 0)

            ' Print candidate CSV
            Me.btnSalePrepCSV.Enabled = (numCandidates > 0)

            ' Rolls assigned CP shell
            cmd.CommandText = "SELECT COUNT(*) FROM genii_user.TR_CP WHERE TaxYear = " & taxYear
            Me.lblSalePrepNumCPShell.Text = cmd.ExecuteScalar().ToString()

            ' Create CP shell
            Me.btnSalePrepCreateCPShell.Enabled = True

            ' CPs sold at auctions
            cmd.CommandText = "SELECT COUNT(*) FROM genii_user.TR_CP WHERE CP_STATUS = 1 AND TaxYear = " & taxYear
            Me.lblSalePrepNumSoldAtAuction.Text = cmd.ExecuteScalar().ToString()

            ' Unassigned CPs
            cmd.CommandText = "SELECT COUNT(*) FROM genii_user.TR_CP WHERE (InvestorID = 0 OR InvestorID IS NULL) AND TaxYear = " & taxYear
            Dim numUnassignedCPs As Integer = CInt(cmd.ExecuteScalar())
            Me.lblSalePrepUnassignedCPs.Text = numUnassignedCPs.ToString()

            ' Assign unsold CPs to state.
            Me.btnSalePrepAssignToState.Enabled = (numUnassignedCPs > 0)
        End Using
    End Sub

#End Region
    '#Region "Lookup - Year"
    '    Protected Sub btnLookupYearGo_Click(sender As Object, e As System.EventArgs) Handles btnLookupYearGo.Click
    '        ' Lookup CP by Year
    '        Dim where As String = "CP.TaxYear = " & Me.ddlLookupYear.SelectedItem.Value
    '        Dim dt As DataTable = LoadCP(where)

    '        Me.grdLookupYear.DataSource = dt
    '        Me.grdLookupYear.DataBind()
    '    End Sub
    '#End Region

End Class
