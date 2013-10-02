Imports System.Data
Imports System.Data.OleDb
Imports System.Drawing.Printing.PrintDocument
Imports System.Drawing.Printing
Imports System.Drawing
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine
Imports ICSharpCode.SharpZipLib.Zip
Imports Utilities
Imports System.Data.SqlClient
Imports Microsoft.PointOfService

Partial Class MaintenanceTasks
    Inherits System.Web.UI.Page


    Private _trCount As Generic.Dictionary(Of Integer, Integer)
    Private _tblPaymentType As DataTable
    Private _tblTaxAuthority As DataTable
    Private Shared newReceiptNumber As Integer
    Public _quickPaymentSessionID As Integer
    Private Shared _GRPKEY As Integer = 0
    Private _sessionDataset As DataSet
    ' Private _isSessionEnded As Boolean = True

#Region "Common Properties"
    ''' <summary>
    ''' Gets connection string for NCIS_TREASURY database.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' 

    Private ReadOnly Property ConnectString As String
        Get
            Return ConfigurationManager.ConnectionStrings("ConnString").ConnectionString
        End Get
    End Property

    Private ReadOnly Property CashierQuickPaymentsTable() As DataTable
        Get
            Return SessionDataset().Tables("CASHIER_QUICK_PAYMENTS")
        End Get
    End Property

    Private ReadOnly Property CashierWebPaymentsTable() As DataTable
        Get
            Return SessionDataset().Tables("CASHIER_WEB_PAYMENTS")
        End Get
    End Property

    Private Property SessionDataset(Optional reload As Boolean = False) As DataSet
        Get
            If reload OrElse _sessionDataset Is Nothing Then
                _sessionDataset = New DataSet
            End If

            ' Load tables.
            If reload OrElse _sessionDataset.Tables("CASHIER_SESSION") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_SESSION", "SELECT * FROM genii_user.CASHIER_SESSION WHERE RECORD_ID = " & Me.lblSessionID.Text)
            End If

            If reload OrElse _sessionDataset.Tables("CASHIER_REJECTED_CHECK") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_REJECTED_CHECK", "SELECT * FROM genii_user.CASHIER_REJECTED_CHECK WHERE SESSION_ID = " & Me.lblSessionID.Text)
            End If

            If reload OrElse _sessionDataset.Tables("CASHIER_QUICK_PAYMENTS") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_QUICK_PAYMENTS", "SELECT * FROM genii_user.CASHIER_QUICK_PAYMENTS WHERE SESSION_ID = " & Me.lblSessionID.Text)
            End If

            If reload OrElse _sessionDataset.Tables("CASHIER_WEB_PAYMENTS") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_WEB_PAYMENTS", "SELECT * FROM genii_user.CASHIER_WEB_PAYMENTS WHERE SESSIONID = " & Me.lblSessionID.Text)
            End If

            If reload OrElse _sessionDataset.Tables("CASHIER_TRANSACTIONS") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_TRANSACTIONS", "SELECT * FROM genii_user.CASHIER_TRANSACTIONS WHERE SESSION_ID = " & Me.lblSessionID.Text)
            End If

            'If reload OrElse _sessionDataset.Tables("CASHIER_APPORTION") Is Nothing Then
            '    LoadTable(_sessionDataset, "CASHIER_APPORTION", _
            '              "SELECT TA.* FROM genii_user.CASHIER_APPORTION TA, genii_user.CASHIER_TRANSACTIONS CT " & _
            '              "WHERE TA.TRANS_ID = CT.RECORD_ID AND CT.SESSION_ID = " & Me.lblSessionID.Text)
            'End If

            If reload OrElse _sessionDataset.Tables("CASHIER_APPORTION") Is Nothing Then
                LoadTable(_sessionDataset, "CASHIER_APPORTION", _
                          "SELECT TA.* FROM genii_user.CASHIER_APPORTION TA, genii_user.CASHIER_TRANSACTIONS CT " & _
                          "WHERE TA.TRANS_ID = CT.RECORD_ID AND CT.SESSION_ID = " & Me.lblSessionID.Text)
            End If

            ' Set relations.
            AddRelation(_sessionDataset, "CASHIER_SESSION", "RECORD_ID", "CASHIER_REJECTED_CHECK", "SESSION_ID")
            AddRelation(_sessionDataset, "CASHIER_SESSION", "RECORD_ID", "CASHIER_QUICK_PAYMENTS", "SESSION_ID")
            AddRelation(_sessionDataset, "CASHIER_SESSION", "RECORD_ID", "CASHIER_TRANSACTIONS", "SESSION_ID")
            '  AddRelation(_sessionDataset, "CASHIER_TRANSACTIONS", "RECORD_ID", "CASHIER_APPORTION", "TRANS_ID")
            '  AddRelation(_sessionDataset, "CASHIER_TRANSACTIONS", "RECORD_ID", "CASHIER_APPORTION", "TRANS_ID")

            Return _sessionDataset
        End Get

        Set(value As DataSet)
            _sessionDataset = value
        End Set
    End Property

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

    Private ReadOnly Property ApportionDetailsTable As DataTable
        Get
            Return SessionDataset.Tables("CASHIER_APPORTION")
        End Get
    End Property

    Private ReadOnly Property CashierTransactionsTable() As DataTable
        Get
            Return SessionDataset().Tables("CASHIER_TRANSACTIONS")
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

            ' BindLettersGrid()
            '  PopulateSalePrepYears()
            PrepareControls()
            ViewRefunds2()
            LoadLPS2()
            LoadCAD2()
            LoadDailyLetters()
            LoadDefaultTaxYear()
            LoadCountyInfo()

            '  Me.txtPosDate.Text = Date.Today.ToShortDateString()

            ''   chkProcessRefunds(Me, EventArgs.Empty)
            '' TODO: FIX THIS
            '' BindForeclosuresGrid()
        End If
    End Sub

    Private Sub PrepareControls()
        ' Payment types.
        Using adt As New OleDbDataAdapter("SELECT Connection_Name, Record_id FROM genii_user.ST_CAP_CONNECTION", Me.ConnectString)
            adt.SelectCommand.Connection.Open()

            Dim rdr As OleDbDataReader = adt.SelectCommand.ExecuteReader()

            While rdr.Read()
                Me.ddlConnection.Items.Add(New ListItem(rdr.Item("Connection_Name").ToString(), rdr.Item("Record_id")))
            End While
        End Using

        Using adt As New OleDbDataAdapter("SELECT distinct CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date' " & _
                                            "            FROM genii_user.CASHIER_WEB_PAYMENTS " & _
                                            "   INNER JOIN genii_user.TR " & _
                                            "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
                                            "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
                                            "            WHERE genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.paid > 0", Me.ConnectString)
            adt.SelectCommand.Connection.Open()

            Dim rdr As OleDbDataReader = adt.SelectCommand.ExecuteReader()

            While rdr.Read()
                Me.ddlCADDates.Items.Add(New ListItem(rdr.Item("Date").ToString(), rdr.Item("Date")))
            End While
        End Using

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

    Private Sub QuickPaymentsCreateNewSession()
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
                _quickPaymentSessionID = recordID
                lblSessionID.Text = recordID
                Me.lblOperatorQuickPayments.Text = userName
                Me.lblOpenedQuickPayments.Text = Date.Now
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



    '#Region "Posting Tab"

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


    Public Sub grdQuickPayments_RowDataBound(sender As Object, e As GridViewRowEventArgs) Handles grdQuickPayments.RowDataBound

        Dim v As Integer = grdQuickPayments.Rows.Count
        Dim totalPayments As Decimal

        If (v > 0) Then
            For x = 0 To (v - 1)
                Dim txtPayment As HtmlInputText = grdQuickPayments.Rows(x).FindControl("txtQuickPayment")
                Dim lblRemainder As Label = grdQuickPayments.Rows(x).FindControl("lblQuickPaymentRemainder")
                txtPayment.Value = (grdQuickPayments.Rows(x).Cells(3).Text).Substring(1)

                lblRemainder.Text = CDec(grdQuickPayments.Rows(x).Cells(3).Text) - CDec(txtPayment.Value)
                totalPayments = totalPayments + CDec(txtPayment.Value)

            Next x

            txtRunningTotal.Text = totalPayments
        Else
            txtRunningTotal.Text = "0.00"
        End If
        


    End Sub

    Public Sub LoadDefaultTaxYear()
        Dim SQL As String = String.Format("SELECT parameter FROM genii_user.st_parameter WHERE parameter_name = 'CURRENT_TAXYEAR'")

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblCountyName As New DataTable()

            adt.Fill(tblCountyName)

            If tblCountyName.Rows.Count > 0 Then
                If (Not IsDBNull(tblCountyName.Rows(0)("parameter"))) Then
                    txtTaxYearScanned.Text = Convert.ToString(tblCountyName.Rows(0)("parameter"))
                End If

            End If
        End Using
    End Sub
    Public Sub CADOpenSession()

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

                Me.lblCADSessID.Text = recordID
                Me.lblCADOperator.Text = userName
                Me.lblCADSessOpen.Text = Date.Today.ToShortDateString()
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

        btnCADOpenSession.BackColor = Color.ForestGreen
        btnCADOpenSession.Text = "Cashier Session Opened"
        btnCommitCAD.Enabled = True

        Dim y As Integer = grdCAD2.Rows.Count
        'grandTotal = txtGrandTotal.Text
        If (y > 0) Then
            For x = 0 To (y - 1)
                Dim chkAllCAD As CheckBox = New CheckBox
                chkAllCAD = grdCAD2.HeaderRow.FindControl("chkALLCAD")
                Dim chkCAD As CheckBox = New CheckBox
                chkCAD = grdCAD2.Rows(x).FindControl("chkCAD")
                chkAllCAD.Enabled = True
                chkCAD.Enabled = True

            Next
        End If
        
    End Sub

    Public Sub QuickPaymentsOpenSession()

        Dim userSessionIDCount As Integer
        Dim userSessionID As Integer

        Dim checkUserSessionSQL As String = String.Format("SELECT count(*) AS COUNT,session_id from  genii_user.CASHIER_QUICK_PAYMENTS where QP_STATUS is NULL  or QP_STATUS =1 and SESSION_ID in " & _
                                                          " (SELECT SESSION_ID FROM GENII_USER.CASHIER_SESSION  " & _
                                                          "    WHERE CASHIER= '" + System.Web.HttpContext.Current.User.Identity.Name + "' AND END_TIME IS NULL) GROUP BY SESSION_ID")

        Using adt As New OleDbDataAdapter(checkUserSessionSQL, Me.ConnectString)
            Dim tblQuickPayment As New DataTable()

            adt.Fill(tblQuickPayment)

            If tblQuickPayment.Rows.Count > 0 Then
                If (Not IsDBNull(tblQuickPayment.Rows(0)("COUNT"))) Then
                    userSessionIDCount = Convert.ToInt32(tblQuickPayment.Rows(0)("COUNT"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("session_id"))) Then
                    userSessionID = Convert.ToInt32(tblQuickPayment.Rows(0)("session_id"))
                End If
            End If
        End Using

        If (userSessionIDCount > 0) Then
            btnSearchQuickPayment.Enabled = True
            btnCloseCashierSession.Enabled = True
            btnOpenCashierSession.BackColor = Drawing.Color.ForestGreen
            btnOpenCashierSession.Text = "Cashier Session Open"
            lblSessionID.Text = userSessionID

            Dim LoadQuickPaymentsSQL As String = String.Format("SELECT * from  genii_user.CASHIER_QUICK_PAYMENTS where SESSION_ID ='" + userSessionID.ToString() + "' and QP_STATUS is NULL or QP_STATUS =1 order by record_id desc")
            '("SELECT * from  genii_user.CASHIER_QUICK_PAYMENTS where QP_STATUS is NULL  or QP_STATUS =1 and SESSION_ID in " & _
            ' " (SELECT SESSION_ID FROM GENII_USER.CASHIER_SESSION  " & _
            '  "    WHERE CASHIER= '" + System.Web.HttpContext.Current.User.Identity.Name + "' AND END_TIME IS NULL) ")
            '

            BindGrid(Me.grdQuickPayments, LoadQuickPaymentsSQL)

        ElseIf (userSessionIDCount = 0) Then
            btnSearchQuickPayment.Enabled = True
            btnCloseCashierSession.Enabled = True
            btnOpenCashierSession.BackColor = Drawing.Color.ForestGreen
            btnOpenCashierSession.Text = "Cashier Session Open"
            QuickPaymentsCreateNewSession()

        End If

        Me.lblOperatorQuickPayments.Text = System.Web.HttpContext.Current.User.Identity.Name
        Me.lblOpenedQuickPayments.Text = Date.Now

    End Sub

    Public Sub QuickPaymentsCloseSession()
        btnSearchQuickPayment.Enabled = False
        btnCloseCashierSession.Enabled = False
        btnOpenCashierSession.BackColor = Drawing.Color.Yellow
        btnOpenCashierSession.Text = "Open Cashier Session"
        Dim x As Integer = grdQuickPayments.Rows.Count

        If (x > 0) Then
            Dim runningTotal As String = txtRunningTotal.Text
            'COMMIT RECORDS
            CommitQuickPayments()
            Page.ClientScript.RegisterStartupScript(Me.GetType(), "Quick Payments", "showQPCommitDetails(" + runningTotal + ");", True)
        End If
     
        QuickPaymentsDoLogout()

        txtTaxIDScanned.Text = String.Empty
        txtTaxRollScanned.Text = String.Empty
        txtQuickPaymentsPayor.Text = String.Empty

        Dim LoadQuickPaymentsSQL As String = String.Format("SELECT * from  genii_user.CASHIER_QUICK_PAYMENTS where QP_STATUS IS NULL  or QP_STATUS =1 AND SESSION_ID =" + lblSessionID.Text + " order by record_id desc")
        BindGrid(Me.grdQuickPayments, LoadQuickPaymentsSQL)

    End Sub

    Public Sub CommitQuickPayments()
        Dim myUtil As New Utilities()
        Dim groupKey As Integer
        Dim PayorName As String = String.Empty
        Dim x As Integer = grdQuickPayments.Rows.Count

    

        Dim SQL As String = String.Format("select isnull(max(group_key),0)+1  as group_key from genii_user.cashier_transactions ")

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblGRPKEY As New DataTable()

            adt.Fill(tblGRPKEY)

            If tblGRPKEY.Rows.Count > 0 Then
                If (Not IsDBNull(tblGRPKEY.Rows(0)("group_key"))) Then
                    groupKey = Convert.ToInt32(tblGRPKEY.Rows(0)("group_key"))
                End If
            End If
        End Using

        For n = 0 To (x - 1)
            Dim recordIDCashierTrans As Integer '= GetNewID("RECORD_ID", "genii_user.CASHIER_TRANSACTIONS", conn, trans)


            Dim SQLrecordID As String = String.Format("select isnull(max(record_id),0)+1  as record_id from genii_user.cashier_transactions ")

            Using adt As New OleDbDataAdapter(SQLrecordID, Me.ConnectString)
                Dim tblGRPKEY As New DataTable()

                adt.Fill(tblGRPKEY)

                If tblGRPKEY.Rows.Count > 0 Then
                    If (Not IsDBNull(tblGRPKEY.Rows(0)("record_id"))) Then
                        recordIDCashierTrans = Convert.ToInt32(tblGRPKEY.Rows(0)("record_id"))
                    End If
                End If
            End Using

            Dim transID As Integer = grdQuickPayments.Rows(n).Cells(0).Text
            Dim taxYear As String = grdQuickPayments.Rows(n).Cells(1).Text
            Dim taxRollNumber As String = grdQuickPayments.Rows(n).Cells(2).Text
            Dim newInterest As Decimal = grdQuickPayments.Rows(n).Cells(8).Text
            Dim taxes As Decimal = grdQuickPayments.Rows(n).Cells(4).Text
            ' Dim payment As Decimal = row.Cells(9).Text
            Dim payment As HtmlInputText = grdQuickPayments.Rows(n).Cells(9).FindControl("txtQuickPayment")
            Dim balance As Decimal = grdQuickPayments.Rows(n).Cells(3).Text

            Dim newBalance As Decimal = balance - payment.Value


            SaveApportionment(recordIDCashierTrans, groupKey, transID, taxYear, taxRollNumber, newInterest, taxes, payment, balance, newBalance)

        Next

    End Sub


    Public Sub SaveApportionment(recordIDCashierTrans As Integer, groupKey As Integer, transID As Integer, taxYear As String, taxRollNumber As String, newInterest As Decimal, taxes As Decimal, payment As HtmlInputText, balance As Decimal, newBalance As Decimal)
        Dim x As Integer = grdQuickPayments.Rows.Count
        Dim PayorName As String = String.Empty

        Dim SQLpayor As String = String.Format("select payor_name from genii_user.cashier_quick_payments where tax_roll_number= ?  and tax_year =?", taxRollNumber, taxYear)

        Using adt As New OleDbDataAdapter(SQLpayor, Me.ConnectString)
            Dim tblPayor As New DataTable()

            adt.Fill(tblPayor)

            If tblPayor.Rows.Count > 0 Then
                If (Not IsDBNull(tblPayor.Rows(0)("payor_name"))) Then
                    PayorName = Convert.ToInt32(tblPayor.Rows(0)("payor_name"))
                End If
            End If
        End Using

        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()
            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try

                Dim cmdNewRecCashierTrans As New OleDbCommand("INSERT INTO genii_user.CASHIER_TRANSACTIONS " & _
                                         "(RECORD_ID,SESSION_ID,GROUP_KEY, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_DATE, PAYMENT_TYPE, APPLY_TO, " & _
                                         " LETTER_TAG, REFUND_TAG, PAYOR_NAME, PAYMENT_AMT, TAX_AMT, KITTY_AMT, REFUND_AMT,  " & _
                                         " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                         " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecCashierTrans.Connection = conn
                cmdNewRecCashierTrans.Transaction = trans

                With cmdNewRecCashierTrans.Parameters
                    .AddWithValue("@RECORD_ID", recordIDCashierTrans)
                    .AddWithValue("@SESSION_ID", Me.lblSessionID.Text)
                    .AddWithValue("@GROUP_KEY", groupKey)
                    .AddWithValue("@TAX_YEAR", taxYear)
                    .AddWithValue("@TAX_ROLL_NUMBER", taxRollNumber)
                    .AddWithValue("@PAYMENT_DATE", Date.Now)
                    .AddWithValue("@PAYMENT_TYPE", 1)
                    .AddWithValue("@APPLY_TO", 1)
                    .AddWithValue("@LETTER_TAG", 0)
                    .AddWithValue("@REFUND_TAG", 0)
                    .AddWithValue("@PAYOR_NAME", PayorName)
                    .AddWithValue("@PAYMENT_AMT", payment.Value)
                    .AddWithValue("@TAX_AMT", taxes)
                    .AddWithValue("@KITTY_AMT", 0)
                    .AddWithValue("@REFUND_AMT", 0)
                    ' .AddWithValue("@BARCODE", txtBarcode.Text)

                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecCashierTrans.ExecuteNonQuery()
                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
            conn.Close()
        End Using


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            '' Call GetApportionment SQL function for each payment.
            'Dim paymentDate As Date 'check payment... how to .. if payment payed or payment due  ''''paymentAmount As Decimal,
            '' taxYear As Integer, taxRollNumber As String, 

            Me.ApportionDetailsTable.Clear()

            Dim cmd As New OleDbCommand("SELECT * FROM dbo.GetApportionment(?,?,?,?)", conn)


            cmd.Parameters.AddWithValue("@TaxYear", taxYear)
            cmd.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber)
            cmd.Parameters.AddWithValue("@PaymentAmount", ((payment.Value).ToString()))
            cmd.Parameters.AddWithValue("@PaymentDate", Today.Date.ToShortDateString)

            cmd.CommandTimeout = 500

            Dim rdr As OleDbDataReader
            ' = New OleDbDataReader(cmd.ExecuteReader())

            rdr = cmd.ExecuteReader()

            While rdr.Read()
                Dim row As DataRow = Me.ApportionDetailsTable.NewRow()

                'row("RECORD_ID") = GetNewID("RECORD_ID", Me.ApportionDetailsTable)
                row("TRANS_ID") = recordIDCashierTrans
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
                row("EDIT_USER") = System.Web.HttpContext.Current.User.Identity.Name
                row("EDIT_DATE") = Date.Now
                row("CREATE_USER") = System.Web.HttpContext.Current.User.Identity.Name
                row("CREATE_DATE") = Date.Now

                Me.ApportionDetailsTable.Rows.Add(row)
            End While

            '  payRow("TRANSACTION_STATUS") = 1
            '   Next
            conn.Close()
        End Using

        CommitDataset()


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try

                'Dim paymentDate As Date

                ''  For Each payRow As DataRow In Me.CashierTransactionsTable.Select("TAX_YEAR='" + taxYear + "' AND TAX_ROLL_NUMBER = '" + taxRollNumber + "' AND TRANSACTION_STATUS IS NULL ")
                ''taxYear = payRow("TAX_YEAR") ' 
                ''taxRollNumber = payRow("TAX_ROLL_NUMBER")
                'paymentDate = Date.Now





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
                    .AddWithValue("@TaxYear", taxYear)
                    .AddWithValue("@TaxRollNumber", taxRollNumber)
                    .AddWithValue("@PaymentEffectiveDate", Date.Now)
                    .AddWithValue("@PaymentTypeCode", 1)
                    .AddWithValue("@PaymentMadeByCode", 3)
                    .AddWithValue("@Pertinent1", PayorName)
                    .AddWithValue("@Pertinent2", "Quick Payment" & " - " & Date.Now)
                    .AddWithValue("@PaymentAmount", payment.Value)

                    '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecPayments.ExecuteNonQuery()




                ' SaveApportionment(recordIDCashierTrans)

                'Dim cmd As New OleDbCommand("INSERT INTO GENII_USER.CASHIER_APPORTION(TAXYEAR,TAXROLLNUMBER,AREACODE,TAXCHARGECODEID,TAXTYPEID,PAYMENTDATE,GLACCOUNT,SENTTOOTHERSYSTEM,RECEIPTNUMBER, " & _
                '                               " DATEAPPORTIONED,DOLLARAMOUNT)SELECT TAXYEAR,TAXROLLNUMBER,AREACODE,TAXCHARGECODEID,TAXTYPEID,PAYMENTDATE,GLACCOUNT,SENTTOOTHERSYSTEM,RECEIPTNUMBER, " & _
                '                               " DATEAPPORTIONED,DOLLARAMOUNT FROM dbo.GetApportionment(?,?,?,?)", conn)
                'cmd.Transaction = trans

                'cmd.Parameters.AddWithValue("@TaxYear", taxYear)
                'cmd.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber)
                'cmd.Parameters.AddWithValue("@PaymentAmount", payment.value)
                'cmd.Parameters.AddWithValue("@PaymentDate", Date.Now)

                'cmd.ExecuteNonQuery()

                ''Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                ''While rdr.Read()                  

                'Dim SQL3 As String = String.Format("UPDATE genii_user.CASHIER_APPORTION " & _
                '                    "SET TRANS_ID = {0}, " & _
                '                    "EDIT_USER = '{1}', " & _
                '                    "EDIT_DATE = '{2}', " & _
                '                    "CREATE_USER = '{3}', " & _
                '                    "CREATE_DATE = '{4}' " & _
                '                    "WHERE taxrollnumber = '{5}' " & _
                '                    "AND taxyear = '{6}' ",
                '                    recordIDCashierTrans,
                '                    System.Web.HttpContext.Current.User.Identity.Name,
                '                    Date.Now,
                '                    System.Web.HttpContext.Current.User.Identity.Name,
                '                    Date.Now,
                '                    taxRollNumber,
                '                    taxYear)
                'Dim cmdNewRecApportion1 As OleDbCommand = New OleDbCommand(SQL3)
                'cmdNewRecApportion1.Connection = conn
                'cmdNewRecApportion1.Transaction = trans
                'cmdNewRecApportion1.ExecuteNonQuery()



                Dim cmdUpdateTRCharges As New OleDbCommand("UPDATE genii_user.TR_CHARGES " & _
                                                            " SET CHARGEAMOUNT = ?, EDIT_USER=?, EDIT_DATE=? " & _
                                                            " WHERE TAXYEAR =? and TAXROLLNUMBER =? AND TAXCHARGECODEID=99901")

                cmdUpdateTRCharges.Connection = conn
                cmdUpdateTRCharges.Transaction = trans

                With cmdUpdateTRCharges.Parameters
                    .AddWithValue("@CHARGEAMOUNT", newInterest)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TAXYEAR", taxYear)
                    .AddWithValue("@TAXROLLNUMBER", taxRollNumber)

                End With

                cmdUpdateTRCharges.ExecuteNonQuery()

                Dim cmdUpdateTR As New OleDbCommand("UPDATE genii_user.TR " & _
                                           " SET CurrentBalance = ?,EDIT_USER=?,EDIT_DATE=? " & _
                                           " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                cmdUpdateTR.Connection = conn
                cmdUpdateTR.Transaction = trans

                With cmdUpdateTR.Parameters

                    '     .AddWithValue("@STATUS", 5)
                    .AddWithValue("@CurrentBalance", newBalance)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@TaxYear", taxYear) 'currentTaxYear)
                    .AddWithValue("@TaxRollNumber", taxRollNumber)


                End With

                cmdUpdateTR.ExecuteNonQuery()

                Dim cmdUpdateTAXACCOUNT As New OleDbCommand("UPDATE genii_user.Tax_Account " & _
                                                            " SET account_balance = ?,EDIT_USER=?,EDIT_DATE=? " & _
                                                            " WHERE ParcelOrTaxID=?  ")

                cmdUpdateTAXACCOUNT.Connection = conn
                cmdUpdateTAXACCOUNT.Transaction = trans

                With cmdUpdateTAXACCOUNT.Parameters

                    .AddWithValue("@account_balance", newBalance)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@ParcelOrTaxID", txtTaxIDScanned.Text)

                End With

                cmdUpdateTAXACCOUNT.ExecuteNonQuery()


                Dim cmdUpdateTransStatus As New OleDbCommand("UPDATE genii_user.CASHIER_TRANSACTIONS " & _
                                                          " SET TRANSACTION_STATUS = ? " & _
                                                          " WHERE tax_year =? and tax_roll_number =? or TRANSACTION_STATUS is null ")

                cmdUpdateTransStatus.Connection = conn
                cmdUpdateTransStatus.Transaction = trans

                With cmdUpdateTransStatus.Parameters
                    .AddWithValue("@TRANSACTION_STATUS", 1)
                    .AddWithValue("@tax_year", taxYear)
                    .AddWithValue("@tax_roll_number", taxRollNumber)

                End With

                cmdUpdateTransStatus.ExecuteNonQuery()

                Dim cmdUpdateQPStatus As New OleDbCommand("UPDATE genii_user.CASHIER_QUICK_PAYMENTS " & _
                                                           " SET QP_STATUS = ?, balance=?, EDIT_USER=?, EDIT_DATE=? " & _
                                                           " WHERE RECORD_ID =? and QP_STATUS =1 or QP_STATUS is null ")

                cmdUpdateQPStatus.Connection = conn
                cmdUpdateQPStatus.Transaction = trans

                With cmdUpdateQPStatus.Parameters
                    .AddWithValue("@QP_STATUS", 3)
                    .AddWithValue("@balance", newBalance)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@EDIT_DATE", Date.Today.ToShortDateString)
                    .AddWithValue("@RECORD_ID", transID)


                End With

                cmdUpdateQPStatus.ExecuteNonQuery()

                trans.Commit()



            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try



            ' Next
            conn.Close()
        End Using
        ' Dim paymentAmount As Decimal = 15.9



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
                UpdateRecordIds(Me.CashierQuickPaymentsTable, "genii_user.CASHIER_QUICK_PAYMENTS", "RECORD_ID", conn, trans)
                UpdateRecordIds(Me.ApportionDetailsTable, "genii_user.CASHIER_APPORTION", "RECORD_ID", conn, trans)

                CommitTable(Me.CashierQuickPaymentsTable, "genii_user.CASHIER_QUICK_PAYMENTS", conn, trans)
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

            'If (tableName <> "genii_user.CASHIER_REJECTED_CHECK" And tableName <> "genii_user.TAX_ACCOUNT_CALENDAR") Then
            '    adt.UpdateCommand = bld.GetUpdateCommand()
            '    adt.DeleteCommand = bld.GetDeleteCommand()
            '    adt.UpdateCommand.Transaction = transaction
            '    adt.DeleteCommand.Transaction = transaction
            'End If

            adt.InsertCommand = bld.GetInsertCommand()

            adt.InsertCommand.Transaction = transaction


            adt.Update(table)
        End Using
    End Sub

    Public Sub LoadQuickPayments()
        'Dim LoadQuickPaymentsSQL As String = String.Format("SELECT * from  genii_user.CASHIER_QUICK_PAYMENTS " & _
        '                                                " WHERE TAX_ACCOUNT.SecuredUnsecured = 'U' AND ACCOUNT_BALANCE > 0 AND ACCOUNT_STATUS <> 7")

        '  BindGrid(Me.grdQuickPayments, LoadQuickPaymentsSQL)
    End Sub

    'btnDeleteQuickPayment_Click
    <System.Web.Services.WebMethod()> _
    Public Shared Function btnDeleteQuickPayment_Click(transID As String) As Boolean
        Dim myUtil As New Utilities()
        ' Dim dateNow As DateTime
        '  dateNow = DateTime.Now
        ' dateNow = dateNow.ToString("yyyy-dd-MM hh:mm:ss")

        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try
                Dim cmdDeleteRecPayments As New OleDbCommand("UPDATE genii_user.CASHIER_QUICK_PAYMENTS " & _
                                                          " SET QP_STATUS = ?, EDIT_USER=?, EDIT_DATE=? " & _
                                                          " WHERE RECORD_ID =? and QP_STATUS is null or QP_STATUS =1")

                cmdDeleteRecPayments.Connection = conn
                cmdDeleteRecPayments.Transaction = trans

                With cmdDeleteRecPayments.Parameters
                    .AddWithValue("@QP_STATUS", 2)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Today.Date.ToShortDateString)
                    .AddWithValue("@RECORD_ID", transID)

                End With

                cmdDeleteRecPayments.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try
            conn.Close()
        End Using

        '  grdQuickPayments_RowDataBound()

        Return True


    End Function

    <System.Web.Services.WebMethod()> _
    Public Shared Function btnUpdateQuickPayments_Click(transID As String, idx As Integer, balance As String, taxes As String, interest As String, payment As String, difference As String, chkPM As Boolean, chkBI As Boolean, chkFG As Boolean) As Boolean
        ', chkPM As Boolean, chkBI As Boolean, chkFG As Boolean
        ',Integer , balance As Decimal,  ''transID As String,

        Dim myUtil As New Utilities()

        '  Dim balance As Decimal
        Dim fees As Decimal
        'Dim interest As Decimal
        Dim paidAmount As Decimal
        Dim taxAmount As Decimal
        Dim TaxYear As String
        Dim TaxRollNumber As String
        Dim pm As Integer
        Dim bi As Integer
        Dim fg As Integer
        Dim kittyAmount As Decimal
        Dim refundAmount As Decimal
        'Dim difference As Decimal
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

            If (chkPM = True) Then
                pm = 1
            Else
                pm = 0
            End If

            If (chkBI = True) Then
                bi = 1
            Else
                bi = 0
            End If

            If (chkFG = True) Then
                fg = 1
            Else
                fg = 0
            End If

            difference = (difference.Replace("(", ""))
            difference = (difference.Replace(")", ""))
            difference = Math.Abs(CDec(difference))

            interest = Decimal.Parse(interest.Replace("$", ""))
            balance = Decimal.Parse(balance.Replace("$", ""))

            taxAmount = Decimal.Parse(taxes.Replace("$", ""))
            paidAmount = CDec(payment)
            'taxAmount = taxes

            If (difference <= MaxKittyAmount And difference <> 0) Then
                kittyAmount = difference
                refundAmount = 0.0
                balance = Math.Abs(CDec(balance) - (CDec(payment) + CDec(interest)) + CDec(kittyAmount))
            ElseIf (difference > MaxKittyAmount) Then
                refundAmount = difference
                kittyAmount = 0.0
                balance = Math.Abs(CDec(balance) - (CDec(payment) + CDec(interest)) + CDec(refundAmount))
            ElseIf (difference = 0) Then
                refundAmount = 0.0
                kittyAmount = 0.0
                balance = Math.Abs(CDec(balance) - (CDec(payment) + CDec(interest)))
            End If

            Try
                Dim dateNow As DateTime
                dateNow = DateTime.Now
                dateNow = dateNow.ToString("yyyy-dd-MM hh:mm:ss")
                Dim cmdNewRecPayments As New OleDbCommand("UPDATE genii_user.CASHIER_QUICK_PAYMENTS " & _
                                                          " SET QP_STATUS = ?, BALANCE = ?, INTEREST=?, PAYMENT_AMT=?, TAX_AMT=?, KITTY_AMT=?, REFUND_AMT=?, PM=?, BI=?, FGI=?, PBH=?, EDIT_USER=?, EDIT_DATE=?, CREATE_USER=?, CREATE_DATE=? " & _
                                                          " WHERE RECORD_ID =? and QP_STATUS is null  or QP_STATUS =1")

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
                    .AddWithValue("@EDIT_DATE", dateNow.ToString("yyyy-dd-MM hh:mm:ss"))
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", dateNow.ToString("yyyy-dd-MM hh:mm:ss"))
                    .AddWithValue("@RECORD_ID", transID)
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

        'Dim LoadQuickPaymentsSQL As String = String.Format("SELECT * from  genii_user.CASHIER_QUICK_PAYMENTS where QP_STATUS IS NULL AND SESSION_ID =" + lblSessionID.Text + " ")

        'BindGrid(Me.grdQuickPayments, LoadQuickPaymentsSQL)



        Dim success = True
        Return success




    End Function

    Public Sub UpdateQuickPayments()
        Dim balance As Decimal
        Dim fees As Decimal
        Dim interest As Decimal
        Dim paidAmount As Decimal
        Dim taxAmount As Decimal
        Dim TaxYear As String
        Dim TaxRollNumber As String
        Dim pm As Integer
        Dim bi As Integer
        Dim fg As Integer
        Dim kittyAmount As Decimal
        Dim refundAmount As Decimal
        Dim difference As Decimal
        Dim MaxKittyAmount As Decimal
        Dim recordID As Integer

        Dim SQL As String = String.Format("SELECT parameter FROM genii_user.st_parameter WHERE parameter_name = 'MAX_KITTY_AMOUNT'")

        Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
            Dim tblMaxKittyAmount As New DataTable()

            adt.Fill(tblMaxKittyAmount)

            If tblMaxKittyAmount.Rows.Count > 0 Then
                If (Not IsDBNull(tblMaxKittyAmount.Rows(0)("parameter"))) Then
                    MaxKittyAmount = Convert.ToDecimal(tblMaxKittyAmount.Rows(0)("parameter"))
                End If

            End If
        End Using


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            ' For Each gvr As GridViewRow In grdQuickPayments.Rows
            Dim gvr As GridViewRow
            gvr = grdQuickPayments.SelectedRow
            Dim idx As GridViewRow = grdQuickPayments.SelectedRow
            ' Dim cb As CheckBox = grdQuickPayments.SelectedRow.FindControl("chkPriorYears")
            Dim txtPayment As HtmlInputText '= grdQuickPayments.Rows(idx).cells(8) 'gvr.Cells(8).FindControl("txtQuickPayment")
            Dim txtDifference As Label = gvr.Cells(10).FindControl("lblQuickPaymentRemainder")
            Dim chkpm As CheckBox = New CheckBox
            chkpm = gvr.Cells(111).FindControl("chkPM")
            Dim chkbi As CheckBox = New CheckBox
            chkbi = gvr.Cells(12).FindControl("chkBI")
            Dim chkfgi As CheckBox = New CheckBox
            chkfgi = gvr.Cells(13).FindControl("chkFG")

            paidAmount = txtPayment.Value
            difference = txtDifference.Text
            If (chkpm.Checked = True) Then
                pm = 1
            Else
                pm = 0
            End If

            If (chkbi.Checked = True) Then
                bi = 1
            Else
                bi = 0
            End If

            If (chkfgi.Checked = True) Then
                fg = 1
            Else
                fg = 0
            End If

            recordID = gvr.Cells(0).Text
            TaxYear = gvr.Cells(1).Text
            TaxRollNumber = gvr.Cells(2).Text
            interest = gvr.Cells(7).Text
            taxAmount = gvr.Cells(4).Text
            balance = gvr.Cells(4).Text - paidAmount
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
                Dim dateNow As DateTime
                dateNow = DateTime.Now
                dateNow = dateNow.ToString("yyyy-dd-MM hh:mm:ss")
                Dim cmdNewRecPayments As New OleDbCommand("UPDATE genii_user.CASHIER_QUICK_PAYMENTS " & _
                                                          " SET QP_STATUS = ?, BALANCE = ?, INTEREST=?, PAYMENT_AMT=?, TAX_AMT=?, KITTY_AMT=?, REFUND_AMT=?, PM=?, BI=?, FGI=?, PBH=? " & _
                                                          " WHERE RECORD_ID =? AND TAX_YEAR =? AND TAX_ROLL_NUMBER=? AND SESSION_ID = " + lblSessionID.Text + " and QP_STATUS is null  or QP_STATUS =1")

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
                    .AddWithValue("@EDIT_DATE", dateNow.ToString("yyyy-dd-MM hh:mm:ss"))
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", dateNow.ToString("yyyy-dd-MM hh:mm:ss"))
                    .AddWithValue("@RECORD_ID", recordID)
                    .AddWithValue("@TAX_YEAR", TaxYear)
                    .AddWithValue("@TAX_ROLL_NUMBER", TaxRollNumber)

                End With

                cmdNewRecPayments.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            ' Next
            conn.Close()
        End Using

    End Sub



    Public Sub SearchQuickPayments()
        Dim TaxYear As String = String.Empty
        Dim TaxRollNumber As String = String.Empty
        Dim balance As Double
        Dim taxAmount As Double
        Dim interest As Double
        Dim fees As Double
        Dim paidAmount As Double
        Dim TaxID As String = String.Empty
        Dim Payor As String = String.Empty
        Dim priorInterest As Double
        

        If ((txtTaxYearScanned.Text = String.Empty)) Then
            Dim Caller As Control = Me
            ScriptManager.RegisterStartupScript(Caller, [GetType](), "Tax Year", "showMessage('Please enter a value for Tax Year', 'Tax Year');", True)
            txtTaxYearScanned.Focus()
            Exit Sub
        End If

        Dim SearchQuickPaymentSQL As String = String.Empty

        If (txtTaxRollScanned.Text <> String.Empty) Then

            SearchQuickPaymentSQL = String.Format("SELECT * from  dbo.vprioryearsowed " & _
                                                        " WHERE TaxRollNumber = '" + txtTaxRollScanned.Text + "' and TaxYear= " + txtTaxYearScanned.Text + " ")

        ElseIf (txtTaxIDScanned.Text <> String.Empty) Then

            SearchQuickPaymentSQL = String.Format("SELECT * from  dbo.vprioryearsowed " & _
                                                        " WHERE TaxIDNumber = '" + txtTaxIDScanned.Text + "' and TaxYear= " + txtTaxYearScanned.Text + " ")
        End If


        Using adt As New OleDbDataAdapter(SearchQuickPaymentSQL, Me.ConnectString)
            Dim tblQuickPayment As New DataTable()

            adt.Fill(tblQuickPayment)

            If tblQuickPayment.Rows.Count > 0 Then
                If (Not IsDBNull(tblQuickPayment.Rows(0)("TaxYear"))) Then
                    TaxYear = Convert.ToString(tblQuickPayment.Rows(0)("TaxYear"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("TaxRollNumber"))) Then
                    TaxRollNumber = Convert.ToString(tblQuickPayment.Rows(0)("TaxRollNumber"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("TaxIDNumber"))) Then
                    TaxID = Convert.ToString(tblQuickPayment.Rows(0)("TaxIDNumber"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("CurrentBalance"))) Then
                    balance = Convert.ToDouble(tblQuickPayment.Rows(0)("CurrentBalance"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("Taxes"))) Then
                    taxAmount = Convert.ToDouble(tblQuickPayment.Rows(0)("Taxes"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("Interest"))) Then
                    interest = Convert.ToDouble(tblQuickPayment.Rows(0)("Interest"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("PRIOT_INTEREST"))) Then
                    priorInterest = Convert.ToDouble(tblQuickPayment.Rows(0)("PRIOT_INTEREST"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("fees"))) Then
                    fees = Convert.ToDouble(tblQuickPayment.Rows(0)("fees"))
                End If

                If (Not IsDBNull(tblQuickPayment.Rows(0)("owner_name_1"))) Then
                    If (Not IsDBNull(tblQuickPayment.Rows(0)("owner_name_2"))) Then
                        Payor = Convert.ToString(tblQuickPayment.Rows(0)("owner_name_1")) & " " & (tblQuickPayment.Rows(0)("owner_name_2"))

                    ElseIf (Not IsDBNull(tblQuickPayment.Rows(0)("owner_name_3"))) Then
                        Payor = Convert.ToString(tblQuickPayment.Rows(0)("owner_name_1")) & " " & (tblQuickPayment.Rows(0)("owner_name_3"))
                    Else
                        Payor = Convert.ToString(tblQuickPayment.Rows(0)("owner_name_1"))
                    End If

                End If
            ElseIf (tblQuickPayment.Rows.Count = 0) Then
                Page.ClientScript.RegisterStartupScript(Me.GetType(), "TaxRollNotFound", "showMessage('Tax roll not found.', 'Not Found');", True)
                Exit Sub
            End If
        End Using

        If (chkPayor.Checked) Then
            'do nothing
        Else
            txtQuickPaymentsPayor.Text = Payor
        End If


        txtTaxIDScanned.Text = TaxID
        txtTaxRollScanned.Text = TaxRollNumber

        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

            Try


                '        Dim row As DataRow = Me.CashierQuickPaymentsTable.NewRow()

                '        row("SESSION_ID") = lblSessionID.Text
                '        row("TAX_YEAR") = TaxYear
                '        row("TAX_ROLL_NUMBER") = TaxRollNumber
                '        row("BALANCE") = balance
                '        row("FEES") = fees
                '        row("INTEREST") = interest
                '        row("PRIOR_MONTH") = priorInterest
                '        row("PAYMENT_AMT") = balance
                '        row("TAX_AMT") = taxAmount
                '        row("KITTY_AMT") = 0
                '        row("REFUND_AMT") = 0
                '        row("PM") = 0
                '        row("BI") = 0
                '        row("FGI") = 0
                '        row("PBH") = 0
                '        row("PAYOR_NAME") = txtQuickPaymentsPayor.Text
                '        row("BARCODE") = txtBarcode.Text

                '        row("EDIT_USER") = System.Web.HttpContext.Current.User.Identity.Name
                '        row("EDIT_DATE") = Today.Date.ToShortDateString
                '        row("CREATE_USER") = System.Web.HttpContext.Current.User.Identity.Name
                '        row("CREATE_DATE") = Today.Date.ToShortDateString

                '        Me.CashierQuickPaymentsTable.Rows.Add(row)

                '        CommitDataset()

                'Dim dateNow As DateTime
                'dateNow = DateTime.Now
                'dateNow = dateNow.ToString("yyyy-dd-MM hh:mm:ss")
                Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_QUICK_PAYMENTS", conn, trans)
                Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.CASHIER_QUICK_PAYMENTS " & _
                                                          "(RECORD_ID,SESSION_ID, TAX_YEAR, TAX_ROLL_NUMBER, BALANCE, " & _
                                                          " FEES,INTEREST,PRIOR_MONTH,PAYMENT_AMT,TAX_AMT,KITTY_AMT,REFUND_AMT, " & _
                                                          " PM,BI,FGI,PBH, PAYOR_NAME, BARCODE,  " & _
                                                          " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                          " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                cmdNewRecPayments.Connection = conn
                cmdNewRecPayments.Transaction = trans


                With cmdNewRecPayments.Parameters
                    .AddWithValue("@RECORD_ID", recordID)
                    .AddWithValue("@SESSION_ID", lblSessionID.Text)
                    .AddWithValue("@TAX_YEAR", TaxYear)
                    .AddWithValue("@TAX_ROLL_NUMBER", TaxRollNumber)
                    .AddWithValue("@BALANCE", balance)
                    .AddWithValue("@FEES", fees)
                    .AddWithValue("@INTEREST", interest)
                    .AddWithValue("@PRIOR_MONTH", priorInterest)
                    .AddWithValue("@PAYMENT_AMT", balance)
                    .AddWithValue("@TAX_AMT", taxAmount)
                    .AddWithValue("@KITTY_AMT", 0)
                    .AddWithValue("@REFUND_AMT", 0)
                    .AddWithValue("@PM", 0)
                    .AddWithValue("@BI", 0)
                    .AddWithValue("@FGI", 0)
                    .AddWithValue("@PBH", 0)
                    .AddWithValue("@PAYOR_NAME", (txtQuickPaymentsPayor.Text).ToUpper())
                    .AddWithValue("@BARCODE", txtBarcode.Text)
                    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                    .AddWithValue("@EDIT_DATE", Date.Now)
                    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    .AddWithValue("@CREATE_DATE", Date.Now)

                End With

                cmdNewRecPayments.ExecuteNonQuery()

                trans.Commit()

            Catch ex As Exception
                trans.Rollback()
                Response.Redirect("ErrorPage.aspx")
                Throw ex
            End Try
            conn.Close()
        End Using

        'Dim chk As CheckBox = grdQuickPayments.HeaderRow.FindControl("chkALLFG")
        'Dim FG As Boolean = False

        'If (chk.Checked) Then
        '    FG = True
        'End If

        Dim parentFGIChecked(grdQuickPayments.Rows.Count) As Boolean
        Dim v As Integer = grdQuickPayments.Rows.Count


        Dim chkALLFGI As New CheckBox()
        Dim chkFGI As New CheckBox()
        Dim FGIisChecked As Boolean
        '//  If (grdQuickPayments.HeaderRow.FindControl("chkALLFG").Controls.Count > 0) Then


        For i = 0 To v - 1
            chkALLFGI = grdQuickPayments.HeaderRow.FindControl("chkALLFG")
            chkFGI = grdQuickPayments.Rows(i).Cells(13).FindControl("chkFG")
            parentFGIChecked(i) = chkFGI.Checked
        Next

        If (chkALLFGI.Checked) Then
            FGIisChecked = True
        End If
        ' //    End If

        Dim parentPMChecked(grdQuickPayments.Rows.Count) As Boolean
        Dim w As Integer = grdQuickPayments.Rows.Count


        Dim chkALLPM As CheckBox = New CheckBox
        Dim chkPM As CheckBox = New CheckBox


        For i = 0 To v - 1
            chkALLPM = grdQuickPayments.HeaderRow.FindControl("chkALLPM")
            chkPM = grdQuickPayments.Rows(i).Cells(13).FindControl("chkPM")
            parentPMChecked(i) = chkPM.Checked
        Next

        Dim PMisChecked As Boolean
        If (chkALLPM.Checked) Then
            PMisChecked = True
        End If


        Dim LoadQuickPaymentsSQL As String = String.Format("SELECT * from  genii_user.CASHIER_QUICK_PAYMENTS where QP_STATUS IS NULL  or QP_STATUS =1 AND SESSION_ID =" + lblSessionID.Text + " order by record_id desc")

        BindGrid(Me.grdQuickPayments, LoadQuickPaymentsSQL)

        If (FGIisChecked = True) Then
            chkALLFGI.Checked = True
        End If

        'If (chkALLFGI.Checked) Then
        '    Dim lastChkFG As CheckBox = grdQuickPayments.Rows(0).Cells(12).FindControl("chkFG")
        '    lastChkFG.Checked = True
        'End If

        For i = 0 To v - 1
            If (FGIisChecked = True) Then
                chkALLFGI.Checked = True
            End If

            If (chkALLFGI.Checked) Then
                Dim lastChkFG As CheckBox = New CheckBox
                lastChkFG = grdQuickPayments.Rows(0).Cells(13).FindControl("chkFG")
                lastChkFG.Checked = True
            End If

            Dim chkFGI2 As CheckBox = New CheckBox
            If (v > 1) Then
                chkFGI2 = grdQuickPayments.Rows(i + 1).Cells(13).FindControl("chkFG")
                chkFGI2.Checked = parentFGIChecked(i)
            End If


            If (chkFGI2.Checked = True) Then
                ' Dim modInterest As Double =
                grdQuickPayments.Rows(i + 1).Cells(8).Text = "0.00"
                'modInterest.text = 0.0

            End If

        Next

        If (FGIisChecked = True) Then
            Dim chkALLFGI2 As CheckBox = New CheckBox
            chkALLFGI2 = grdQuickPayments.HeaderRow.FindControl("chkALLFG")
            chkALLFGI2.Checked = True
        End If


        '''''''''''''
        If (PMisChecked = True) Then
            chkALLPM.Checked = True
        End If

        'If (chkALLFGI.Checked) Then
        '    Dim lastChkFG As CheckBox = grdQuickPayments.Rows(0).Cells(12).FindControl("chkFG")
        '    lastChkFG.Checked = True
        'End If

        For i = 0 To v - 1
            If (PMisChecked = True) Then
                chkALLPM.Checked = True
            End If

            If (chkALLPM.Checked) Then
                Dim lastChkPM As CheckBox = New CheckBox
                lastChkPM = grdQuickPayments.Rows(0).Cells(11).FindControl("chkPM")
                lastChkPM.Checked = True
            End If

            Dim chkPM2 As CheckBox = New CheckBox
            If (v > 1) Then
                chkPM2 = grdQuickPayments.Rows(i + 1).Cells(11).FindControl("chkPM")
                chkPM2.Checked = parentPMChecked(i)
            End If


            If (chkPM2.Checked = True) Then
                ' Dim modInterest As Double =
                '  grdQuickPayments.Rows(i + 1).Cells(7).Text = "0.00"
                'modInterest.text = 0.0

            End If

        Next

        If (PMisChecked = True) Then
            Dim chkALLPM2 As CheckBox = New CheckBox
            chkALLPM2 = grdQuickPayments.HeaderRow.FindControl("chkALLPM")
            chkALLPM2.Checked = True
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

    'Private Shared Function BindGridReturn(grid As String, commandText As String) As Boolean
    '    Dim dt As New DataTable()
    '    Dim myUtil As New Utilities()

    '    grid = grdQuickPayments

    '    Using adt As New OleDbDataAdapter(commandText, myUtil.ConnectString)
    '        adt.SelectCommand.CommandTimeout = 300
    '        adt.Fill(dt)
    '    End Using
    '    grid.DataSource = dt
    '    grid.DataBind()
    '    'With grid
    '    '    .DataSource = dt
    '    '    .DataBind()
    '    'End With
    '    Return True
    'End Function

    Public Sub ViewReturnedChecks()
        'Dim ViewReturnedChecksSQL As String = "SELECT * FROM genii_user.CASHIER_TRANSACTIONS" ' WHERE transaction_status=1"

        'BindGrid(Me.grdReturnedChecks, ViewReturnedChecksSQL)

        Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewReturnedChecksActions('Testing');", True)
    End Sub



    Public Sub ViewRefunds2()
        Dim ViewRefundsSQL As String = String.Format("SELECT CONVERT(varchar, genii_user.CASHIER_TRANSACTIONS.RECORD_ID) + ' (' " & _
                                                    "    + Convert(varchar, genii_user.CASHIER_TRANSACTIONS.GROUP_KEY) " & _
                                                    "     + ')' AS 'Transaction', " & _
                                                    "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR + ' ('  " & _
                                                    "     + genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER " & _
                                                    "     + ')' AS 'Year (Roll)', " & _
                                                    "   CASE genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS " & _
                                                    "     WHEN 1 THEN 'Not Posted' " & _
                                                    "     WHEN 2 THEN 'Posted' " & _
                                                    "     WHEN 3 THEN 'Canceled Prior' " & _
                                                    "     WHEN 4 THEN 'Reversed' " & _
                                                    "       END AS 'Status', " & _
                                                    "   CONVERT(varchar(10), genii_user.CASHIER_TRANSACTIONS.PAYMENT_DATE, 101) AS 'Date', " & _
                                                    "   genii_user.ST_APPLY_PAYMENT_TO.APPLY_TO AS 'Apply To', " & _
                                                    "   genii_user.CASHIER_TRANSACTIONS.PAYOR_NAME AS 'Name', " & _
                                                    "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT AS 'Payment', " & _
                                                    "   genii_user.CASHIER_TRANSACTIONS.TAX_AMT AS 'Tax', " & _
                                                    "   genii_user.CASHIER_TRANSACTIONS.REFUND_AMT AS 'Refund' " & _
                                                    "         FROM genii_user.CASHIER_TRANSACTIONS " & _
                                                    "   INNER JOIN genii_user.ST_APPLY_PAYMENT_TO " & _
                                                    "     ON genii_user.CASHIER_TRANSACTIONS.APPLY_TO = genii_user.ST_APPLY_PAYMENT_TO.RECORD_ID " & _
                                                    " WHERE     REFUND_TAG = 1 AND REFUND_AMT > 0")


        BindGrid(Me.grdProcessRefunds2, ViewRefundsSQL)

        'Dim ViewRefundsSQL2 As String = String.Format("WITH PAYMENTS (TAX_YEAR, TAX_ROLL_NUMBER, Payments) AS " & _
        '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER, " & _
        '                                                "   SUM(genii_user.TR_PAYMENTS.PaymentAmount) AS 'Payment' " & _
        '                                                " FROM  " & _
        '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
        '                                                " 		INNER JOIN genii_user.TR_PAYMENTS " & _
        '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_PAYMENTS.TaxYear  " & _
        '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_PAYMENTS.TaxRollNumber " & _
        '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
        '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER), " & _
        '                                                " APPORTION (RECORD_ID, GROUP_KEY, TRANSACTION_STATUS, APPLY_TO, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_AMT, Apportioned) AS " & _
        '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.RECORD_ID, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.GROUP_KEY, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.APPLY_TO, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT, " & _
        '                                                "   SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS 'Apportioned' " & _
        '                                                " FROM  " & _
        '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
        '                                                " 	INNER JOIN genii_user.CASHIER_APPORTION  " & _
        '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.CASHIER_APPORTION.TaxYear  " & _
        '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.CASHIER_APPORTION.TaxRollNumber " & _
        '                                                " WHERE     (genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1) " & _
        '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.RECORD_ID, genii_user.CASHIER_TRANSACTIONS.GROUP_KEY,  " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TRANSACTION_STATUS, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.APPLY_TO, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT), " & _
        '                                                " CHARGES (TAX_YEAR, TAX_ROLL_NUMBER, Charged) AS " & _
        '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
        '                                                "   SUM(genii_user.TR_CHARGES.ChargeAmount) AS 'Charged' " & _
        '                                                " FROM  " & _
        '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
        '                                                " 	INNER JOIN genii_user.TR_CHARGES  " & _
        '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CHARGES.TaxYear  " & _
        '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CHARGES.TaxRollNumber " & _
        '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
        '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER), " & _
        '                                                " REFUND (TAX_YEAR, TAX_ROLL_NUMBER, Refund) AS " & _
        '                                                " (SELECT genii_user.CASHIER_TRANSACTIONS.TAX_YEAR AS TR, " & _
        '                                                "   genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS TYN, " & _
        '                                                "   SUM(genii_user.TR_CHARGES.ChargeAmount) AS 'Refund' " & _
        '                                                " FROM  " & _
        '                                                " 	genii_user.CASHIER_TRANSACTIONS  " & _
        '                                                " 	INNER JOIN genii_user.TR_CHARGES  " & _
        '                                                " 	ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CHARGES.TaxYear  " & _
        '                                                " 	   AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CHARGES.TaxRollNumber " & _
        '                                                " WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
        '                                                "   AND genii_user.TR_CHARGES.TaxChargeCodeID in (99922,99932) " & _
        '                                                " GROUP BY genii_user.CASHIER_TRANSACTIONS.TAX_YEAR, genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER) " & _
        '                                                " SELECT CONVERT(varchar, APPORTION.RECORD_ID) + ' ('  " & _
        '                                                "     + CONVERT(varchar, APPORTION.GROUP_KEY) + ')' AS 'Trans (Group)', " & _
        '                                                "   APPORTION.TAX_YEAR + ' (' + " & _
        '                                                "   APPORTION.TAX_ROLL_NUMBER + ')' AS 'Year (Roll)', " & _
        '                                                "                             Case APPORTION.TRANSACTION_STATUS " & _
        '                                                "     WHEN 1 THEN 'Not Posted' " & _
        '                                                "     WHEN 2 THEN 'Posted' " & _
        '                                                "     WHEN 3 THEN 'Canceled Prior' " & _
        '                                                "     WHEN 4 THEN 'Reversed' " & _
        '                                                "       END AS 'Status', " & _
        '                                                "   genii_user.ST_APPLY_PAYMENT_TO.APPLY_TO AS 'Apply To', " & _
        '                                                "   APPORTION.PAYMENT_AMT AS 'Redeem Payment', " & _
        '                                                "   CHARGES.Charged AS 'Charges', " & _
        '                                                "   PAYMENTS.Payments AS 'Payments', " & _
        '                                                "   APPORTION.Apportioned AS 'Apportioned', " & _
        '                                                "   REFUND.Refund AS 'Refund' " & _
        '                                                "         from APPORTION " & _
        '                                                "    INNER JOIN PAYMENTS " & _
        '                                                "      ON APPORTION.Tax_YEAR=PAYMENTS.Tax_YEAR " & _
        '                                                "        AND APPORTION.TAX_ROLL_NUMBER=PAYMENTS.TAX_ROLL_NUMBER " & _
        '                                                "    INNER JOIN CHARGES " & _
        '                                                "      ON APPORTION.Tax_YEAR=CHARGES.Tax_YEAR " & _
        '                                                "        AND APPORTION.TAX_ROLL_NUMBER=CHARGES.TAX_ROLL_NUMBER " & _
        '                                                "    INNER JOIN REFUND " & _
        '                                                "      ON APPORTION.Tax_YEAR=REFUND.Tax_YEAR " & _
        '                                                "        AND APPORTION.TAX_ROLL_NUMBER=REFUND.TAX_ROLL_NUMBER " & _
        '                                                "    INNER JOIN genii_user.ST_APPLY_PAYMENT_TO " & _
        '                                                "      ON APPORTION.APPLY_TO = genii_user.ST_APPLY_PAYMENT_TO.RECORD_ID")


        'BindGrid(Me.grdCPRefunds2, ViewRefundsSQL2)


    End Sub



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

    Public Sub searchReturnedChecks_click2()
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

        Dim where_clause As String = String.Empty
        Dim assignedIDs As String = Request.Form("hdnTxtValue")
        Dim msg As String = String.Empty


        '   rdoPayor.Checked = True
        If (Me.radioTaxIDSearch.Checked = True) Then
            If (txtTaxIDSearch.Text <> String.Empty) Then
                where_clause = "where tax_roll_number in (select taxrollnumber from genii_user.TR where taxIDNumber='" + txtTaxIDSearch.Text + "') "
                msg = "Tax roll not found."
            End If

        ElseIf (Me.radioCheckNumberSearch.Checked = True) Then
            If (txtCheckNumberSearch.Text <> String.Empty) Then
                where_clause = "where Check_number like '%" + txtCheckNumberSearch.Text + "%' "
                msg = "Check number not found."
            End If
        ElseIf (Me.radioPayorSearch.Checked = True) Then
            If (txtPayorSearch.Text <> String.Empty) Then
                where_clause = "where payor_name like '%" + txtPayorSearch.Text + "%' "
                msg = "Payor name not found."
            End If
        Else
            where_clause = ""
            msg = "Tax roll not found."
        End If

        Dim ViewReturnedChecksSQL As String = "SELECT * FROM genii_user.CASHIER_TRANSACTIONS " & where_clause  ' WHERE transaction_status=1"

        BindGrid(Me.grdReturnedChecks2, ViewReturnedChecksSQL)

        Me.tabContainerFunctions.ActiveTabIndex = 0

        If (grdReturnedChecks2.Rows.Count = 0) Then
            Page.ClientScript.RegisterStartupScript(Me.GetType(), "TaxRollNotFound", "showMessage('" + msg + "', 'Not Found');", True)
        End If


        '   Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewReturnedChecksActions('Testing');", True)
    End Sub



    Public Sub ProcessReturnedChecks2()
        Dim myUtil As New Utilities()
        Dim y As Integer = grdProcessRefunds2.Rows.Count
        Dim z As Integer = grdCPRefunds2.Rows.Count
        Dim transID As String
        Dim taxYear As String
        Dim taxRollNumber As String
        Dim paymentAmount As Double


        For x = 0 To (y - 1)
            '  Dim cbox As HtmlInputCheckBox = grdProcessRefunds.Rows(x).Cells(0).FindControl("chkRefunds2")
            Dim chk As CheckBox = New CheckBox
            chk = grdProcessRefunds2.Rows(x).FindControl("chkRefunds2")

            If (chk.Checked) Then
                transID = grdProcessRefunds2.Rows(x).Cells(1).Text
                transID = transID.Substring(transID.Length - 3, 2)
                paymentAmount = CDec(grdProcessRefunds2.Rows(x).Cells(9).Text)
                'taxYear = grdProcessRefunds2.Rows(x).Cells(2).Text
                'taxYear = taxYear.Substring(0, 4)
                'taxRollNumber = taxYear.Substring(taxYear.Length - 1, )

                Using conn As New OleDbConnection(myUtil.ConnectString)
                    conn.Open()

                    Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)

                    Try
                        Dim refundDetails As DataSet = New DataSet()

                        Dim Sql = String.Format("SELECT genii_user.CASHIER_TRANSACTIONS.tax_year, genii_user.CASHIER_TRANSACTIONS.tax_roll_number,GETDATE() AS QUEUE_DATE, " & _
                                                "  genii_user.CASHIER_TRANSACTIONS.REFUND_AMT AS REFUND_AMOUNT, " & _
                                                "   'Payment Refund ' + genii_user.CASHIER_TRANSACTIONS.TAX_YEAR + ' - ' + genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER AS 'MEMO', " & _
                                                "   genii_user.TR.OWNER_NAME_1 AS 'VENDOR_NAME', " & _
                                                "   genii_user.TR.MAIL_ADDRESS_1 AS ADDRESS_1, " & _
                                                "   genii_user.TR.MAIL_ADDRESS_2, AS ADDRESS_2" & _
                                                "   genii_user.TR.MAIL_CITY AS 'CITY', " & _
                                                "   genii_user.TR.MAIL_STATE AS 'STATE', " & _
                                                "   genii_user.TR.MAIL_CODE AS 'ZIP', " & _
                                                "   0 AS SENT_TO_OTHER_SYSTEM, " & _
                                                "  '" + System.Web.HttpContext.Current.User.Identity.Name + "' AS CREATE_USER, " & _
                                                "   GETDATE() AS CREATE_DATE, " & _
                                                "   '" + System.Web.HttpContext.Current.User.Identity.Name + "' AS EDIT_USER, " & _
                                                "   GETDATE() AS EDIT_DATE " & _
                                                "   FROM genii_user.CASHIER_TRANSACTIONS " & _
                                                "  INNER JOIN genii_user.TR " & _
                                                "     ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR.TaxYear " & _
                                                "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR.TaxRollNumber " & _
                                                "                         WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
                                                "   AND genii_user.CASHIER_TRANSACTIONS.REFUND_AMT > 0 AND genii_user.CASHIER_TRANSACTIONS.GROUP_KEY=" + transID + " )")

                        LoadTable(refundDetails, "CASHIER_QUEUE_CHECK", Sql)

                        Dim row As DataRow
                        row = refundDetails.Tables(0).Rows(0)


                        Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_CHECK_QUEUE", conn, trans)

                        Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.CASHIER_CHECK_QUEUE " & _
                                                                         "(record_id, group_key, queue_date, " & _
                                                                         " refund_amount,memo,vendor_name, address_1, " & _
                                                                         " address_2,city,state, zip, sent_to_other_system, " & _
                                                                         " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                                         " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")
                        cmdNewRecPayments.Connection = conn
                        cmdNewRecPayments.Transaction = trans


                        With cmdNewRecPayments.Parameters
                            .AddWithValue("@record_id", recordID)
                            .AddWithValue("@group_key", transID)
                            .AddWithValue("@queue_date", row("QUEUE_DATE"))
                            .AddWithValue("@refund_amount", row("REFUND_AMOUNT"))
                            .AddWithValue("@memo", row("memo"))
                            .AddWithValue("@vendor_name", row("vendor_name"))
                            .AddWithValue("@address_1", row("address_1"))
                            .AddWithValue("@address_2", row("address_2"))
                            .AddWithValue("@city", row("city"))
                            .AddWithValue("@state", row("state"))
                            .AddWithValue("@zip", row("zip"))
                            .AddWithValue("@sent_to_other_system", row("sent_to_other_system"))

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
                        Throw ex
                    End Try
                    conn.Close()
                End Using
            End If

        Next

        For x = 0 To (z - 1)
            Dim chk As CheckBox = New CheckBox
            chk = grdCPRefunds2.Rows(x).FindControl("chkCPRefunds2")

            If (chk.Checked) Then
                transID = grdCPRefunds2.Rows(x).Cells(1).Text
                transID = transID.Substring(transID.Length - 3, 2)
                paymentAmount = CDec(grdCPRefunds2.Rows(x).Cells(9).Text)
                'taxYear = grdCPRefunds2.Rows(x).Cells(2).Text
                'taxRollNumber = grdCPRefunds2.Rows(x).Cells(3).Text

                Using conn As New OleDbConnection(myUtil.ConnectString)
                    conn.Open()

                    Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)


                    Dim refundDetails As DataSet = New DataSet()

                    Dim Sql = String.Format("SELECT genii_user.CASHIER_TRANSACTIONS.tax_year,genii_user.CASHIER_TRANSACTIONS.tax_roll_number,GETDATE() AS QUEUE_DATE, " & _
                                            "  genii_user.TR_CP.PurchaseValue + ISNULL(genii_user.TR_CP.INTEREST_EARNED, 0) AS 'REFUND_AMOUNT', " & _
                                            "                     'CP Redeem ' + genii_user.TR_CP.APN + ' - ' + CONVERT(varchar, genii_user.TR_CP.TaxYear) AS 'MEMO', " & _
                                            "   ISNULL(genii_user.ST_INVESTOR.FirstName, '') + ' ' " & _
                                            "   + ISNULL(genii_user.ST_INVESTOR.MiddleName, '') + ' ' " & _
                                            "   + genii_user.ST_INVESTOR.LastName AS 'VENDOR_NAME', " & _
                                            "   ISNULL(genii_user.ST_INVESTOR.Address1, '') AS 'ADDRESS_1', " & _
                                            "   ISNULL(genii_user.ST_INVESTOR.Address2, '') AS 'ADDRESS_2', " & _
                                            "   ISNULL(genii_user.ST_INVESTOR.City, '') AS 'CITY', " & _
                                            "   ISNULL(genii_user.ST_INVESTOR.State, '') AS 'STATE', " & _
                                            "   ISNULL(genii_user.ST_INVESTOR.PostalCode, '') AS 'ZIP', " & _
                                            "   0 AS SENT_TO_OTHER_SYSTEM, " & _
                                            "                     'genii_user' AS CREATE_USER, " & _
                                            "   GETDATE() AS CREATE_DATE, " & _
                                            "   'genii_user' AS EDIT_USER, " & _
                                            "   GETDATE() AS EDIT_DATE " & _
                                            "                     FROM genii_user.CASHIER_TRANSACTIONS " & _
                                            "   INNER JOIN genii_user.TR_CP " & _
                                            "     ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CP.TaxYear " & _
                                            "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CP.TaxRollNumber " & _
                                            "   INNER JOIN genii_user.ST_INVESTOR " & _
                                            "     ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " & _
                                            "   INNER JOIN genii_user.TR_CHARGES " & _
                                            "     ON genii_user.CASHIER_TRANSACTIONS.Tax_Year = genii_user.TR_CHARGES.TaxYear  " & _
                                            "       AND genii_user.CASHIER_TRANSACTIONS.Tax_ROLL_NUMBER = genii_user.TR_CHARGES.TaxRollNumber  " & _
                                            "                     WHERE genii_user.CASHIER_TRANSACTIONS.REFUND_TAG = 1 " & _
                                            "   AND genii_user.CASHIER_TRANSACTIONS.APPLY_TO = 2 " & _
                                            "   AND genii_user.TR_CHARGES.TaxChargeCodeID IN (99922,99932) and genii_user.CASHIER_TRANSACTIONS.group_key=" + transID + " ")

                    LoadTable(refundDetails, "CASHIER_QUEUE_CHECK", Sql)

                    Dim row As DataRow
                    row = refundDetails.Tables(0).Rows(0)


                    Try
                        Dim cmdNewRecPayments As New OleDbCommand("INSERT INTO genii_user.TR_PAYMENTS " & _
                                                      "(TRANS_ID, TaxYear, TaxRollNumber, PaymentEffectiveDate, " & _
                                                      " PaymentTypeCode,PaymentMadeByCode,Pertinent1, " & _
                                                      " Pertinent2, PaymentAmount,  " & _
                                                      " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                      " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)")

                        cmdNewRecPayments.Connection = conn
                        cmdNewRecPayments.Transaction = trans


                        With cmdNewRecPayments.Parameters
                            .AddWithValue("@TRANS_ID", transID)
                            .AddWithValue("@TaxYear", row("tax_year"))
                            .AddWithValue("@TaxRollNumber", row("tax_roll_number"))
                            .AddWithValue("@PaymentEffectiveDate", Date.Now)
                            .AddWithValue("@PaymentTypeCode", 6)
                            .AddWithValue("@PaymentMadeByCode", 5)
                            .AddWithValue("@Pertinent1", "CP/Investor Refund.")
                            .AddWithValue("@Pertinent2", "Scheduled by Banking System.")
                            .AddWithValue("@PaymentAmount", paymentAmount)

                            '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                            .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                            .AddWithValue("@EDIT_DATE", Date.Now)
                            .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                            .AddWithValue("@CREATE_DATE", Date.Now)

                        End With

                        cmdNewRecPayments.ExecuteNonQuery()


                        Dim cmdNewRecCharges As New OleDbCommand("INSERT INTO genii_user.TR_CHARGES " & _
                                                                         "(TaxYear, TaxRollNumber, TaxChargeCodeID, " & _
                                                                         " TaxTypeID,ChargeAmount, " & _
                                                                         " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                                         " VALUES (?,?,?,?,?,?,?,?,?)")

                        cmdNewRecCharges.Connection = conn
                        cmdNewRecCharges.Transaction = trans

                        With cmdNewRecCharges.Parameters
                            .AddWithValue("@TaxYear", row("tax_year"))
                            .AddWithValue("@TaxRollNumber", row("tax_roll_number"))
                            .AddWithValue("@TaxChargeCodeID", 99932)
                            .AddWithValue("@TaxTypeID", 99)
                            .AddWithValue("@ChargeAmount", paymentAmount)

                            .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                            .AddWithValue("@EDIT_DATE", Date.Now)
                            .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                            .AddWithValue("@CREATE_DATE", Date.Now)

                        End With

                        cmdNewRecCharges.ExecuteNonQuery()


                        Dim cmdUpdateTrans As New OleDbCommand("Update genii_user.Cashier_transactions set refund_tag =2, edit_user='" + System.Web.HttpContext.Current.User.Identity.Name + "', edit_date='" + Date.Now + "' where tax_year =" + row("tax_year") + " and tax_roll_Number= " + row("tax_roll_number") + " ")

                        cmdUpdateTrans.Connection = conn
                        cmdUpdateTrans.Transaction = trans

                        'With cmdUpdateTrans.Parameters
                        '    .AddWithValue("@TaxYear", taxYear)
                        '    .AddWithValue("@TaxRollNumber", taxRollNumber)
                        '    .AddWithValue("@TaxChargeCodeID", 99932)
                        '    .AddWithValue("@TaxTypeID", 99)
                        '    .AddWithValue("@ChargeAmount", paymentAmount)

                        '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                        '    .AddWithValue("@EDIT_DATE", Date.Now)
                        '    .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                        '    .AddWithValue("@CREATE_DATE", Date.Now)

                        'End With

                        cmdUpdateTrans.ExecuteNonQuery()


                        Dim recordID As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_CHECK_QUEUE", conn, trans)

                        Dim cmdNewRecPayments2 As New OleDbCommand("INSERT INTO genii_user.CASHIER_CHECK_QUEUE " & _
                                                                         "(record_id, group_key, queue_date, " & _
                                                                         " refund_amount,memo,vendor_name, address_1, " & _
                                                                         " address_2,city,state, zip, sent_to_other_system, " & _
                                                                         " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                                         " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")
                        cmdNewRecPayments2.Connection = conn
                        cmdNewRecPayments2.Transaction = trans


                        With cmdNewRecPayments2.Parameters
                            .AddWithValue("@record_id", recordID)
                            .AddWithValue("@group_key", transID)
                            .AddWithValue("@queue_date", row("QUEUE_DATE"))
                            .AddWithValue("@refund_amount", row("REFUND_AMOUNT"))
                            .AddWithValue("@memo", row("memo"))
                            .AddWithValue("@vendor_name", row("vendor_name"))
                            .AddWithValue("@address_1", row("address_1"))
                            .AddWithValue("@address_2", row("address_2"))
                            .AddWithValue("@city", row("city"))
                            .AddWithValue("@state", row("state"))
                            .AddWithValue("@zip", row("zip"))
                            .AddWithValue("@sent_to_other_system", row("sent_to_other_system"))

                            .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                            .AddWithValue("@EDIT_DATE", Date.Now)
                            .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                            .AddWithValue("@CREATE_DATE", Date.Now)

                        End With

                        cmdNewRecPayments2.ExecuteNonQuery()



                        trans.Commit()

                    Catch ex As Exception
                        trans.Rollback()
                        Throw ex
                    End Try
                    conn.Close()
                End Using
            End If

        Next

    End Sub

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

        Dim taxYear As String
        Dim taxRollNumber As String
        Dim SQL As String = String.Format("select * from genii_user.cashier_transactions where group_key = " + grpKey + " ")

        Using adt As New OleDbDataAdapter(SQL, myUtil.ConnectString)
            Dim tblTrans As New DataTable()

            adt.Fill(tblTrans)

            If tblTrans.Rows.Count > 0 Then

                If (Not IsDBNull(tblTrans.Rows(0)("Tax_Year"))) Then
                    taxYear = Convert.ToDouble(tblTrans.Rows(0)("Tax_Year"))
                End If

                If (Not IsDBNull(tblTrans.Rows(0)("Tax_Roll_number"))) Then
                    taxRollNumber = Convert.ToDouble(tblTrans.Rows(0)("Tax_Roll_number"))
                End If

            End If
        End Using

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

        If (sessID.ToString() = String.Empty) Then
            Exit Function

        End If

        Dim sqlX As String = String.Empty
        ' Dim sql2 As String
        Dim depositDetails As DataSet = New DataSet()
        Dim receiptDetails As DataSet = New DataSet()
        sqlX = String.Format("SELECT genii_user.CASHIER_SESSION.RECORD_ID AS 'Cashier Session', " & _
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

        Using adt As New OleDbDataAdapter(sqlX, myUtil.ConnectString)
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



    Public Sub LoadTable(container As DataSet, tableName As String, query As String)
        'Dim adt As OleDbDataAdapter


        Using adt As New OleDbDataAdapter(query, Me.ConnectString)
            adt.Fill(container, tableName)
        End Using


    End Sub

#End Region



#Region "Lender Processing Services"
    Protected Sub btnLPSLoad_Click() ' ByVal sender As Object, ByVal e As System.EventArgs Handles btnLPSLoad.Click
        LoadLPS2()
    End Sub




    Public Sub LoadDailyLetters()
        Dim dailyLettersTable As New DataTable()
        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("SELECT CONVERT(varchar, genii_user.CASHIER_TRANSACTIONS.RECORD_ID) + ' (' " & _
                                        "    + Convert(varchar, genii_user.CASHIER_TRANSACTIONS.GROUP_KEY) " & _
                                        "     + ')' AS 'Trans (Group)', " & _
                                        "   genii_user.CASHIER_TRANSACTIONS.TAX_YEAR + ' ('  " & _
                                        "     + genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER " & _
                                        "     + ')' AS 'Year (Roll)', " & _
                                        "   CONVERT(varchar(10), genii_user.CASHIER_TRANSACTIONS.PAYMENT_DATE, 101) AS 'Date', " & _
                                        "   CASE genii_user.CASHIER_TRANSACTIONS.LETTER_TAG " & _
                                        "     WHEN 1 THEN 'Payment with Balance' " & _
                                        "     WHEN 2 THEN 'Payment with CP' " & _
                                        "     WHEN 3 THEN 'Both Balance and CP' " & _
                                        "       END AS 'Letter Reason', " & _
                                        "   genii_user.CASHIER_TRANSACTIONS.PAYMENT_AMT AS 'Payment', " & _
                                        "   genii_user.TAX_ACCOUNT.ACCOUNT_BALANCE AS 'Account Balance', " & _
                                        "  (SELECT COUNT(*) FROM genii_user.TR_CP WHERE genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CP.TaxYear " & _
                                        "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CP.TaxRollNumber) AS 'CP Count' " & _
                                        "             FROM genii_user.CASHIER_TRANSACTIONS " & _
                                        "   INNER JOIN genii_user.TR " & _
                                        "     ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR.TaxYear " & _
                                        "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR.TaxRollNumber " & _
                                        "   INNER JOIN genii_user.TAX_ACCOUNT " & _
                                        "     ON genii_user.TR.TaxIDNumber = genii_user.TAX_ACCOUNT.ParcelOrTaxID " & _
                                        "   LEFT OUTER JOIN genii_user.TR_CP " & _
                                        "     ON genii_user.CASHIER_TRANSACTIONS.TAX_YEAR = genii_user.TR_CP.TaxYear " & _
                                        "       AND genii_user.CASHIER_TRANSACTIONS.TAX_ROLL_NUMBER = genii_user.TR_CP.TaxRollNumber " & _
                                        "  WHERE     LETTER_TAG IN (1,2,3)")

            cmd.Connection = conn

            conn.Open()

            Dim dailyLettersDataAdapter As New OleDbDataAdapter(cmd)

            dailyLettersDataAdapter.Fill(dailyLettersTable)
        End Using

        If dailyLettersTable.Rows.Count > 0 Then
            Me.grdDailyLetters.DataSource = dailyLettersTable
            Me.grdDailyLetters.DataBind()
        End If

    End Sub

    Public Sub LoadLPS2()
        Dim lpsTable As New DataTable()

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("SELECT * FROM genii_user.ST_LENDER_PROCESSING_SERVICES")

            cmd.Connection = conn

            conn.Open()

            Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

            lpsDataAdapter.Fill(lpsTable)
        End Using

        If lpsTable.Rows.Count > 0 Then
            Me.grdLPS2.DataSource = lpsTable
            Me.grdLPS2.DataBind()
        End If

        ' Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCheckLPSActions('LPS');", True)

    End Sub



    Public Sub LoadCAD2()
        Dim lpsTable As New DataTable()

        Dim a As String = ddlConnection.SelectedIndex

        If (ddlConnection.SelectedValue = "1") Then
            Dim andClause As String = String.Empty
            If (chkCADDates.Checked) Then
                andClause = "and CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101)= '" + ddlCADDates.SelectedValue + "' "
            Else
                andClause = String.Empty
            End If
            Using conn As New OleDbConnection(Me.ConnectString)
                Dim cmd As New OleDbCommand("SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
                                            "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
                                            "  genii_user.CASHIER_WEB_PAYMENTS.PayerFirstName + ' ' + genii_user.CASHIER_WEB_PAYMENTS.PayerLastName AS 'Payor', " & _
                                            "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
                                            "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
                                            "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
                                            "   genii_user.TR.CurrentBalance AS 'Balance', " & _
                                            "   CASE " & _
                                            "     WHEN genii_user.TR.CurrentBalance = 0 and genii_user.CASHIER_WEB_PAYMENTS.Amount = genii_user.CASHIER_WEB_PAYMENTS.Paid THEN 'Match' " & _
                                            "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid >= 0 THEN 'Partial'  " & _
                                            "     ELSE 'Refund' END AS 'Status' " & _
                                            "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
                                            "   INNER JOIN genii_user.TR " & _
                                            "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
                                            "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
                                            "             WHERE genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.paid > 0 " & andClause & " " & _
                                            " ORDER BY STATUS, genii_user.CASHIER_WEB_PAYMENTS.DatePosted")

                cmd.Connection = conn

                conn.Open()

                Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

                lpsDataAdapter.Fill(lpsTable)

                If lpsTable.Rows.Count > 0 Then
                    Me.grdCAD2.DataSource = lpsTable
                    Me.grdCAD2.DataBind()
                End If
            End Using

        Else
            Me.grdCAD2.DataSource = Nothing
            Me.grdCAD2.DataBind()
        End If




        '    Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    End Sub

    'Public Sub checkALLFG(FGchecked As Boolean)

    '    Dim chk As CheckBox = grdQuickPayments.HeaderRow.FindControl("chkALLFG")
    '    Dim chkPriorYears As CheckBox = grdPriorYears.Rows(x).FindControl("chkPriorYears")
    '    If (FGchecked = True) Then
    '        chk.Checked = True
    '    Else
    '        chk.Checked = False
    '    End If

    '    If (chk.Checked) Then
    '        Dim x As Integer = (grdQuickPayments.Rows.Count)
    '        Dim chkBI As CheckBox = grdQuickPayments.Rows(0).FindControl("chkFG")
    '        chkBI.Checked = True
    '    Else
    '        do nothing
    '    End If



    'End Sub

    Public Sub LoadCADMatch()
        Dim lpsTable As New DataTable()
        If (ddlConnection.SelectedValue = "1") Then
            Dim andClause As String = String.Empty
            If (chkCADDates.Checked) Then
                andClause = "and CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101)= '" + ddlCADDates.SelectedValue + "' "
            Else
                andClause = String.Empty
            End If
        Using conn As New OleDbConnection(Me.ConnectString)
                Dim cmd As New OleDbCommand("select * from (SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
                                            "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
                                            "  genii_user.CASHIER_WEB_PAYMENTS.PayerFirstName + ' ' + genii_user.CASHIER_WEB_PAYMENTS.PayerLastName AS 'Payor', " & _
                                            "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
                                            "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
                                            "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
                                            "   genii_user.TR.CurrentBalance AS 'Balance', " & _
                                            "   CASE " & _
                                            "     WHEN genii_user.TR.CurrentBalance = 0 and genii_user.CASHIER_WEB_PAYMENTS.Amount = genii_user.CASHIER_WEB_PAYMENTS.Paid THEN 'Match' " & _
                                            "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid >= 0 THEN 'Partial'  " & _
                                            "     ELSE 'Refund' END AS 'Status' " & _
                                            "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
                                            "   INNER JOIN genii_user.TR " & _
                                            "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
                                            "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
                                            "             WHERE genii_user.TR.CurrentBalance = 0 and genii_user.CASHIER_WEB_PAYMENTS.Amount = genii_user.CASHIER_WEB_PAYMENTS.Paid " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & andClause & " " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' ) a" & _
                                            " where a.status='Match'")

            cmd.Connection = conn

            conn.Open()

            Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

            lpsDataAdapter.Fill(lpsTable)
        End Using

     '   If lpsTable.Rows.Count > 0 Then
            Me.grdCAD2.DataSource = lpsTable
            Me.grdCAD2.DataBind()
            ''  End If

        Else
            Me.grdCAD2.DataSource = Nothing
            Me.grdCAD2.DataBind()
        End If
        '   Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    End Sub

    Public Sub LoadCADPartial()
        Dim lpsTable As New DataTable()
        If (ddlConnection.SelectedValue = "1") Then
            Dim andClause As String = String.Empty
            If (chkCADDates.Checked) Then
                andClause = "and CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101)= '" + ddlCADDates.SelectedValue + "' "
            Else
                andClause = String.Empty
            End If
        Using conn As New OleDbConnection(Me.ConnectString)
                Dim cmd As New OleDbCommand("select * from (SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
                                            "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
                                            "  genii_user.CASHIER_WEB_PAYMENTS.PayerFirstName + ' ' + genii_user.CASHIER_WEB_PAYMENTS.PayerLastName AS 'Payor', " & _
                                            "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
                                            "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
                                            "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
                                            "   genii_user.TR.CurrentBalance AS 'Balance', " & _
                                            "   CASE " & _
                                            "     WHEN genii_user.TR.CurrentBalance = 0 and genii_user.CASHIER_WEB_PAYMENTS.Amount = genii_user.CASHIER_WEB_PAYMENTS.Paid THEN 'Match' " & _
                                            "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid >= 0 THEN 'Partial'  " & _
                                            "     ELSE 'Refund' END AS 'Status' " & _
                                            "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
                                            "   INNER JOIN genii_user.TR " & _
                                            "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
                                            "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
                                            "             WHERE genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid >= 0  " & _
                                            "   and  genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.paid > 0 " & _
                                            "   AND genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid > 0 " & andClause & ") a" & _
                                            " where a.status='Partial'")

            cmd.Connection = conn

            conn.Open()

            Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

            lpsDataAdapter.Fill(lpsTable)
        End Using

            '  If lpsTable.Rows.Count > 0 Then
            Me.grdCAD2.DataSource = lpsTable
            Me.grdCAD2.DataBind()
            'End If

        Else
            Me.grdCAD2.DataSource = Nothing
            Me.grdCAD2.DataBind()
        End If
        ' Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    End Sub

    Public Sub LoadCADRefund()
        Dim lpsTable As New DataTable()
        If (ddlConnection.SelectedValue = "1") Then
            Dim andClause As String = String.Empty
            If (chkCADDates.Checked) Then
                andClause = "and CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101)= '" + ddlCADDates.SelectedValue + "' "
            Else
                andClause = String.Empty
            End If
        Using conn As New OleDbConnection(Me.ConnectString)
                Dim cmd As New OleDbCommand("Select * From (SELECT genii_user.CASHIER_WEB_PAYMENTS.TaxYear AS 'Tax Year', " & _
                                            "  genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber AS 'Roll Number', " & _
                                            "  genii_user.CASHIER_WEB_PAYMENTS.PayerFirstName + ' ' + genii_user.CASHIER_WEB_PAYMENTS.PayerLastName AS 'Payor', " & _
                                            "   CONVERT(varchar(10), genii_user.CASHIER_WEB_PAYMENTS.DateInitiated, 101) AS 'Date', " & _
                                            "             genii_user.CASHIER_WEB_PAYMENTS.Amount, " & _
                                            "   genii_user.CASHIER_WEB_PAYMENTS.Paid, " & _
                                            "   genii_user.TR.CurrentBalance AS 'Balance', " & _
                                            "   CASE " & _
                                            "     WHEN genii_user.TR.CurrentBalance = 0 and genii_user.CASHIER_WEB_PAYMENTS.Amount = genii_user.CASHIER_WEB_PAYMENTS.Paid THEN 'Match' " & _
                                            "     WHEN genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid >= 0 THEN 'Partial'  " & _
                                            "     ELSE 'Refund' END AS 'Status' " & _
                                            "             FROM genii_user.CASHIER_WEB_PAYMENTS " & _
                                            "   INNER JOIN genii_user.TR " & _
                                            "     ON genii_user.CASHIER_WEB_PAYMENTS.TaxYear = genii_user.TR.TaxYear " & _
                                            "       AND genii_user.CASHIER_WEB_PAYMENTS.TaxRollNumber = genii_user.TR.TaxRollNumber " & _
                                            "             WHERE genii_user.CASHIER_WEB_PAYMENTS.Paid Is Not NULL  " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.DatePosted IS NULL " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.Paid <> 0 " & _
                                            "   AND genii_user.CASHIER_WEB_PAYMENTS.test = 'False' " & _
                                            "   AND genii_user.TR.CurrentBalance - genii_user.CASHIER_WEB_PAYMENTS.Paid < 0" & andClause & ") a" & _
                                            " where a.status ='Refund' ")

            cmd.Connection = conn

            conn.Open()

            Dim lpsDataAdapter As New OleDbDataAdapter(cmd)

            lpsDataAdapter.Fill(lpsTable)
        End Using

            '  If lpsTable.Rows.Count > 0 Then
            Me.grdCAD2.DataSource = lpsTable
            Me.grdCAD2.DataBind()
            ' End If
        Else
        Me.grdCAD2.DataSource = Nothing
        Me.grdCAD2.DataBind()
        End If
        '  Page.ClientScript.RegisterStartupScript(Me.GetType(), "testing", "showViewCADActions('Computer Aided Reports');", True)

    End Sub
    Private Sub DoLogout()
        'Dim endCash As Decimal = Me.txtLogoutEndCash.Text
        'Dim requiredCash As Decimal = Me.txtLogoutRequiredCash.Text

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("UPDATE genii_user.CASHIER_SESSION SET END_TIME = ?, END_CASH = ?, REQUIRED_CASH = ?, EDIT_USER=?, EDIT_DATE=? WHERE RECORD_ID = ?")

            cmd.Connection = conn

            cmd.Parameters.AddWithValue("@END_TIME", Date.Now)
            cmd.Parameters.AddWithValue("@END_CASH", 0.0)
            cmd.Parameters.AddWithValue("@REQUIRED_CASH", 0.0)
            cmd.Parameters.AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
            cmd.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
            cmd.Parameters.AddWithValue("@RECORD_ID", Me.lblCADSessID.Text)

            conn.Open()
            cmd.ExecuteNonQuery()
            ' Me.SessionRecordID = 0

            ' Prompt to start new session?
            StartNewSession()
        End Using
    End Sub

    Private Sub QuickPaymentsDoLogout()
        'Dim endCash As Decimal = Me.txtLogoutEndCash.Text
        'Dim requiredCash As Decimal = Me.txtLogoutRequiredCash.Text

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand("UPDATE genii_user.CASHIER_SESSION SET END_TIME = ?, END_CASH = ?, REQUIRED_CASH = ?, EDIT_USER=?, EDIT_DATE=? WHERE RECORD_ID = ?")

            cmd.Connection = conn

            cmd.Parameters.AddWithValue("@END_TIME", Date.Now)
            cmd.Parameters.AddWithValue("@END_CASH", 0.0)
            cmd.Parameters.AddWithValue("@REQUIRED_CASH", 0.0)
            cmd.Parameters.AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
            cmd.Parameters.AddWithValue("@EDIT_DATE", Date.Now)
            cmd.Parameters.AddWithValue("@RECORD_ID", Me.lblSessionID.Text)

            conn.Open()
            cmd.ExecuteNonQuery()
            ' Me.SessionRecordID = 0

            ' Prompt to start new session?
            ' StartNewSession()
        End Using
    End Sub

    Public Sub SaveApportionmentWebPayments(transID As Integer, taxYear As String, taxRollNumber As String, dateCAD As String, amount As Decimal, payment As Decimal, balance As Decimal, payor As Decimal)

        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            '' Call GetApportionment SQL function for each payment.
            'Dim paymentDate As Date 'check payment... how to .. if payment payed or payment due  ''''paymentAmount As Decimal,
            '' taxYear As Integer, taxRollNumber As String, 

            Me.ApportionDetailsTable.Clear()

            Dim cmd As New OleDbCommand("SELECT * FROM dbo.GetApportionment(?,?,?,?)", conn)


            cmd.Parameters.AddWithValue("@TaxYear", taxYear)
            cmd.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber)
            cmd.Parameters.AddWithValue("@PaymentAmount", payment)
            cmd.Parameters.AddWithValue("@PaymentDate", Today.Date.ToShortDateString)

            cmd.CommandTimeout = 500

            Dim rdr As OleDbDataReader
            ' = New OleDbDataReader(cmd.ExecuteReader())

            rdr = cmd.ExecuteReader()

            While rdr.Read()
                Dim row As DataRow = Me.ApportionDetailsTable.NewRow()

                'row("RECORD_ID") = GetNewID("RECORD_ID", Me.ApportionDetailsTable)
                row("TRANS_ID") = transID
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
                row("EDIT_USER") = System.Web.HttpContext.Current.User.Identity.Name
                row("EDIT_DATE") = Date.Now
                row("CREATE_USER") = System.Web.HttpContext.Current.User.Identity.Name
                row("CREATE_DATE") = Date.Now

                Me.ApportionDetailsTable.Rows.Add(row)
            End While

            '  payRow("TRANSACTION_STATUS") = 1
            '   Next
            conn.Close()
        End Using

        CommitDataset()


        Using conn As New OleDbConnection(Me.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)
            Dim x As Integer = grdCAD2.Rows.Count



            Try

                'Dim cmd As New OleDbCommand("INSERT INTO GENII_USER.CASHIER_APPORTION(TAXYEAR,TAXROLLNUMBER,AREACODE,TAXCHARGECODEID,TAXTYPEID,PAYMENTDATE,GLACCOUNT,SENTTOOTHERSYSTEM,RECEIPTNUMBER, " & _
                '                               " DATEAPPORTIONED,DOLLARAMOUNT)SELECT TAXYEAR,TAXROLLNUMBER,AREACODE,TAXCHARGECODEID,TAXTYPEID,PAYMENTDATE,GLACCOUNT,SENTTOOTHERSYSTEM,RECEIPTNUMBER, " & _
                '                               " DATEAPPORTIONED,DOLLARAMOUNT FROM dbo.GetApportionment(?,?,?,?)", conn)
                'cmd.Transaction = trans

                'cmd.Parameters.AddWithValue("@TaxYear", taxYear)
                'cmd.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber)
                'cmd.Parameters.AddWithValue("@PaymentAmount", payment)
                'cmd.Parameters.AddWithValue("@PaymentDate", dateCAD)

                'cmd.ExecuteNonQuery()

                ''Dim rdr As OleDbDataReader = cmd.ExecuteReader()

                ''While rdr.Read()                  

                'Dim SQL3 As String = String.Format("UPDATE genii_user.CASHIER_APPORTION " & _
                '                    "SET TRANS_ID = {0}, " & _
                '                    "EDIT_USER = '{1}', " & _
                '                    "EDIT_DATE = '{2}', " & _
                '                    "CREATE_USER = '{3}', " & _
                '                    "CREATE_DATE = '{4}' " & _
                '                    "WHERE taxrollnumber = '{5}' " & _
                '                    "AND taxyear = '{6}' ",
                '                    transID,
                '                    System.Web.HttpContext.Current.User.Identity.Name,
                '                    Date.Now,
                '                    System.Web.HttpContext.Current.User.Identity.Name,
                '                    Date.Now,
                '                    taxRollNumber,
                '                    taxYear)
                'Dim cmdNewRecApportion1 As OleDbCommand = New OleDbCommand(SQL3)
                'cmdNewRecApportion1.Connection = conn
                'cmdNewRecApportion1.Transaction = trans
                'cmdNewRecApportion1.ExecuteNonQuery()


                Dim cmdUpdateTransStatus As New OleDbCommand("UPDATE genii_user.CASHIER_TRANSACTIONS " & _
                                                          " SET TRANSACTION_STATUS = ? " & _
                                                          " WHERE tax_year =? and tax_roll_number =? or TRANSACTION_STATUS is null ")

                cmdUpdateTransStatus.Connection = conn
                cmdUpdateTransStatus.Transaction = trans

                With cmdUpdateTransStatus.Parameters
                    .AddWithValue("@TRANSACTION_STATUS", 1)
                    .AddWithValue("@tax_year", taxYear)
                    .AddWithValue("@tax_roll_number", taxRollNumber)

                End With

                cmdUpdateTransStatus.ExecuteNonQuery()

                Dim cmdUpdateQPStatus As New OleDbCommand("UPDATE genii_user.CASHIER_WEB_PAYMENTS " & _
                                                           " SET TRANS_ID = ?, SESSIONID=?, DATEPOSTED=? " & _
                                                           " WHERE TAXYEAR =? and TAXROLLNUMBER =? and DATEPOSTED is null ")

                cmdUpdateQPStatus.Connection = conn
                cmdUpdateQPStatus.Transaction = trans

                With cmdUpdateQPStatus.Parameters
                    .AddWithValue("@TRANS_ID", transID)
                    .AddWithValue("@SESSIONID", lblCADSessID.Text)
                    .AddWithValue("@DATEPOSTED", Date.Now)
                    '.AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                    '.AddWithValue("@EDIT_DATE", Date.Today.ToShortDateString)
                    .AddWithValue("@TAXYEAR", taxYear)
                    .AddWithValue("@TAXROLLNUMBER", taxRollNumber)


                End With

                cmdUpdateQPStatus.ExecuteNonQuery()

                trans.Commit()



            Catch ex As Exception
                trans.Rollback()
                Throw ex
            End Try

            '   End If

            ' Next
            conn.Close()
        End Using


    End Sub

    Public Sub ProcessCAD()
        ' StartNewSession()
        ''process CAD
        Dim myUtil As New Utilities()

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

        Using conn As New OleDbConnection(myUtil.ConnectString)
            conn.Open()

            Dim trans As OleDbTransaction = conn.BeginTransaction(IsolationLevel.Serializable)
            Dim x As Integer = grdCAD2.Rows.Count

            For y = 0 To x - 1
                'For Each gvr As DataRow In Me.CashierWebPaymentsTable.Rows 'Select("sessionid= " + Me.lblSessionID.Text + " and QP_STATUS not in (2,3) or qp_status is null  ") '

                Dim chkCAD As CheckBox = New CheckBox
                chkCAD = grdCAD2.Rows(y).Cells(0).FindControl("chkCAD")

                If (chkCAD.Checked) Then

                    'Dim gvr As DataRow = grdCAD2.
                    'taxyear, taxroll, amount, date, paid, balance,status(match,partial,refund)
                    '      Dim transID As Integer = gvr("RECORD_ID")
                    Dim taxYear As String = grdCAD2.Rows(y).Cells(1).Text ' (gvr("TAX YEAR")).ToString()
                    Dim taxRollNumber As String = grdCAD2.Rows(y).Cells(2).Text '(gvr("ROLL NUMBER")).ToString()
                    Dim dateCAD As String = grdCAD2.Rows(y).Cells(4).Text
                    Dim amount As Decimal = grdCAD2.Rows(y).Cells(5).Text
                    Dim payment As Decimal = grdCAD2.Rows(y).Cells(6).Text
                    Dim balance As Decimal = grdCAD2.Rows(y).Cells(7).Text
                    Dim payor As String = grdCAD2.Rows(y).Cells(3).Text

                    ' Dim newBalance As Decimal = balance - payment

                    Dim taxID As String = String.Empty
                    Dim SQL2 As String = String.Format("select taxIDNumber from genii_user.TR where TaxYear={0} and TaxRollNumber={1} ", taxYear, taxRollNumber)

                    Using adt As New OleDbDataAdapter(SQL2, Me.ConnectString)
                        Dim tblTaxID As New DataTable()

                        adt.Fill(tblTaxID)

                        If tblTaxID.Rows.Count > 0 Then
                            If (Not IsDBNull(tblTaxID.Rows(0)("taxIDNumber"))) Then
                                taxID = Convert.ToDouble(tblTaxID.Rows(0)("taxIDNumber"))
                            End If
                        End If
                    End Using

                    Try



                        'Dim cmdUpdateTRCharges As New OleDbCommand("UPDATE genii_user.TR_CHARGES " & _
                        '                                          " SET CHARGEAMOUNT = ?, EDIT_USER=?, EDIT_DATE=? " & _
                        '                                          " WHERE TAXYEAR =? and TAXROLLNUMBER =? AND TAXCHARGECODEID=99901")

                        'cmdUpdateTRCharges.Connection = conn
                        'cmdUpdateTRCharges.Transaction = trans

                        'With cmdUpdateTRCharges.Parameters
                        '    .AddWithValue("@CHARGEAMOUNT", newInterest)
                        '    .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                        '    .AddWithValue("@EDIT_DATE", Date.Now)
                        '    .AddWithValue("@TAXYEAR", taxYear)
                        '    .AddWithValue("@TAXROLLNUMBER", taxRollNumber)

                        'End With

                        'cmdUpdateTRCharges.ExecuteNonQuery()

                        Dim cmdNewRecCashierTrans As New OleDbCommand("INSERT INTO genii_user.CASHIER_TRANSACTIONS " & _
                                                       "(RECORD_ID,SESSION_ID,GROUP_KEY, TAX_YEAR, TAX_ROLL_NUMBER, PAYMENT_DATE, PAYMENT_TYPE, APPLY_TO, " & _
                                                       " PAYOR_NAME, PAYMENT_AMT, TAX_AMT, BARCODE, " & _
                                                       " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " & _
                                                       " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)")

                        cmdNewRecCashierTrans.Connection = conn
                        cmdNewRecCashierTrans.Transaction = trans

                        Dim recordIDCashierTrans As Integer = GetNewID("RECORD_ID", "genii_user.CASHIER_TRANSACTIONS", conn, trans)
                        ' taxrollnumber = row2("TaxRollNumber")
                        ' Dim isApportioned As String = 1

                        With cmdNewRecCashierTrans.Parameters
                            .AddWithValue("@RECORD_ID", recordIDCashierTrans)
                            .AddWithValue("@SESSION_ID", Me.lblCADSessID.Text)
                            .AddWithValue("@GROUP_KEY", groupKey)
                            '  .AddWithValue("@TRANSACTION_STATUS", 1)
                            .AddWithValue("@TAX_YEAR", taxYear)
                            .AddWithValue("@TAX_ROLL_NUMBER", taxRollNumber)
                            .AddWithValue("@PAYMENT_DATE", dateCAD)
                            .AddWithValue("@PAYMENT_TYPE", 1)
                            .AddWithValue("@APPLY_TO", 1)
                            ' .AddWithValue("@LETTER_TAG", 0)
                            ' .AddWithValue("@REFUND_TAG", 1)
                            .AddWithValue("@PAYOR_NAME", payor)
                            .AddWithValue("@PAYMENT_AMT", payment)
                            .AddWithValue("@TAX_AMT", payment)
                            .AddWithValue("@BARCODE", "")
                            ' .AddWithValue("@IS_APPORTIONED", isApportioned)

                            .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                            .AddWithValue("@EDIT_DATE", Date.Now)
                            .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                            .AddWithValue("@CREATE_DATE", Date.Now)

                        End With

                        cmdNewRecCashierTrans.ExecuteNonQuery()

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
                            .AddWithValue("@TaxYear", taxYear)
                            .AddWithValue("@TaxRollNumber", taxRollNumber)
                            .AddWithValue("@PaymentEffectiveDate", dateCAD)
                            .AddWithValue("@PaymentTypeCode", 1)
                            .AddWithValue("@PaymentMadeByCode", 3)
                            .AddWithValue("@Pertinent1", payor)
                            .AddWithValue("@Pertinent2", "Web Payment" & " - " & dateCAD)
                            .AddWithValue("@PaymentAmount", payment)

                            '       .AddWithValue("@COMPUTER_ID", Request.UserHostName)
                            .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                            .AddWithValue("@EDIT_DATE", Date.Now)
                            .AddWithValue("@CREATE_USER", System.Web.HttpContext.Current.User.Identity.Name)
                            .AddWithValue("@CREATE_DATE", Date.Now)

                        End With

                        cmdNewRecPayments.ExecuteNonQuery()



                        Dim cmdUpdateTR As New OleDbCommand("UPDATE genii_user.TR " & _
                                                   " SET CurrentBalance = ?,EDIT_USER=?,EDIT_DATE=? " & _
                                                   " WHERE TAXYEAR=? AND TAXROLLNUMBER=? ")

                        cmdUpdateTR.Connection = conn
                        cmdUpdateTR.Transaction = trans

                        With cmdUpdateTR.Parameters

                            '     .AddWithValue("@STATUS", 5)
                            .AddWithValue("@CurrentBalance", balance)
                            .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name) 'Request.UserHostName)
                            .AddWithValue("@EDIT_DATE", Date.Now)
                            .AddWithValue("@TaxYear", taxYear) 'currentTaxYear)
                            .AddWithValue("@TaxRollNumber", taxRollNumber)


                        End With

                        cmdUpdateTR.ExecuteNonQuery()

                        Dim cmdUpdateTAXACCOUNT As New OleDbCommand("UPDATE genii_user.Tax_Account " & _
                                                                    " SET account_balance = ?,EDIT_USER=?,EDIT_DATE=? " & _
                                                                    " WHERE ParcelOrTaxID=?  ")

                        cmdUpdateTAXACCOUNT.Connection = conn
                        cmdUpdateTAXACCOUNT.Transaction = trans

                        With cmdUpdateTAXACCOUNT.Parameters

                            .AddWithValue("@account_balance", balance)
                            .AddWithValue("@EDIT_USER", System.Web.HttpContext.Current.User.Identity.Name)
                            .AddWithValue("@EDIT_DATE", Date.Now)
                            .AddWithValue("@ParcelOrTaxID", taxID)

                        End With

                        cmdUpdateTAXACCOUNT.ExecuteNonQuery()

                        trans.Commit()

                        '   System.Threading.Thread.Sleep(2000)
                        SaveApportionmentWebPayments(recordIDCashierTrans, taxYear, taxRollNumber, dateCAD, amount, payment, balance, payor)

                    Catch ex As Exception
                        'trans.Rollback()
                        Throw ex
                    End Try

                End If


               
            Next

            conn.Close()
        End Using

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



    Private Function Divide(ByVal numerator As Decimal, ByVal denominator As Decimal) As Decimal
        If denominator = 0 Then
            Return 0
        Else
            Return numerator / denominator
        End If
    End Function

#Region "Sale Preparation"
    'Private Sub PopulateSalePrepYears()
    '    Dim dt As New DataTable()

    '    Using adp As New OleDbDataAdapter("SELECT DISTINCT TaxYear FROM genii_user.TR ORDER BY TaxYear DESC", Me.ConnectString)
    '        adp.Fill(dt)

    '        With Me.ddlSalePrepYear
    '            .DataTextField = "TaxYear"
    '            .DataValueField = "TaxYear"
    '            .DataSource = dt
    '            .DataBind()
    '        End With
    '    End Using

    '    ' Select previous year.
    '    Dim selectedYear As Integer

    '    If Date.Today.Month >= 3 Then
    '        selectedYear = Date.Today.Year - 1
    '    Else
    '        selectedYear = Date.Today.Year - 2
    '    End If

    '    Dim item As ListItem = Me.ddlSalePrepYear.Items.FindByValue(selectedYear)

    '    If item IsNot Nothing Then
    '        Me.ddlSalePrepYear.SelectedItem.Text = item.Text
    '        ''item.Selected = True
    '    End If

    '    ' Initialize label text.
    '    Me.lblSalePrepNumCandidates.Text = Me.SalePrepInitialMessage
    '    Me.lblSalePrepNumAdvFee.Text = Me.SalePrepInitialMessage
    '    Me.lblSalePrepNumCPShell.Text = Me.SalePrepInitialMessage
    '    Me.lblSalePrepNumSoldAtAuction.Text = Me.SalePrepInitialMessage
    '    Me.lblSalePrepUnassignedCPs.Text = Me.SalePrepInitialMessage
    'End Sub

    'Protected Sub btnSalePrepGo_Click(sender As Object, e As System.EventArgs) Handles btnSalePrepGo.Click
    '    Using conn As New OleDbConnection(Me.ConnectString)
    '        conn.Open()
    '        Dim cmd As New OleDbCommand()
    '        cmd.Connection = conn
    '        Dim taxYear As Integer = CInt(Me.ddlSalePrepYear.SelectedValue)

    '        ' Tax sale candidates.
    '        cmd.CommandText = "select count(*) from genii_user.TR where SecuredUnsecured='S' and CurrentBalance>0 and TaxYear=" & taxYear
    '        Dim numCandidates As Integer = CInt(cmd.ExecuteScalar())
    '        Me.lblSalePrepNumCandidates.Text = numCandidates.ToString()

    '        ' Candidates not assigned advertisement fee.
    '        cmd.CommandText = "SELECT COUNT(*) FROM genii_user.TR AS TR " & _
    '                          "LEFT OUTER JOIN (SELECT TaxYear, TaxRollNumber FROM genii_user.TR_CHARGES WHERE TaxChargeCodeID = 99902) AS AdvFees " & _
    '                          "ON TR.TaxYear = AdvFees.TaxYear AND TR.TaxRollNumber = AdvFees.TaxRollNumber WHERE TR.CurrentBalance > 0 AND TR.SecuredUnsecured = 'S' " & _
    '                          "AND AdvFees.TaxYear IS NULL AND TR.TaxYear = " & taxYear

    '        Dim numNoAdvFee As Integer = CInt(cmd.ExecuteScalar())
    '        Me.lblSalePrepNumAdvFee.Text = numNoAdvFee.ToString()

    '        ' Date fees posted.
    '        cmd.CommandText = "SELECT MAX(EDIT_DATE) FROM genii_user.TR_CHARGES WHERE TaxChargeCodeID = '99902' AND TaxYear = " & taxYear
    '        Dim maxFeeDate As Object = cmd.ExecuteScalar()
    '        If IsDBNull(maxFeeDate) Then
    '            Me.lblSalePrepDateFeesPosted.Text = String.Empty
    '        Else
    '            Me.lblSalePrepDateFeesPosted.Text = CDate(maxFeeDate).ToShortDateString() & "<br />"
    '        End If
    '        Me.btnSalePrepPostFees.Enabled = (numNoAdvFee > 0)

    '        ' Print candidate CSV
    '        Me.btnSalePrepCSV.Enabled = (numCandidates > 0)

    '        ' Rolls assigned CP shell
    '        cmd.CommandText = "SELECT COUNT(*) FROM genii_user.TR_CP WHERE TaxYear = " & taxYear
    '        Me.lblSalePrepNumCPShell.Text = cmd.ExecuteScalar().ToString()

    '        ' Create CP shell
    '        Me.btnSalePrepCreateCPShell.Enabled = True

    '        ' CPs sold at auctions
    '        cmd.CommandText = "SELECT COUNT(*) FROM genii_user.TR_CP WHERE CP_STATUS = 1 AND TaxYear = " & taxYear
    '        Me.lblSalePrepNumSoldAtAuction.Text = cmd.ExecuteScalar().ToString()

    '        ' Unassigned CPs
    '        cmd.CommandText = "SELECT COUNT(*) FROM genii_user.TR_CP WHERE (InvestorID = 0 OR InvestorID IS NULL) AND TaxYear = " & taxYear
    '        Dim numUnassignedCPs As Integer = CInt(cmd.ExecuteScalar())
    '        Me.lblSalePrepUnassignedCPs.Text = numUnassignedCPs.ToString()

    '        ' Assign unsold CPs to state.
    '        Me.btnSalePrepAssignToState.Enabled = (numUnassignedCPs > 0)
    '    End Using
    'End Sub

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
