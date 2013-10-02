Imports System.Data
Imports System.Data.OleDb

Partial Class Reports_InvestorHoldingsSummary
    Inherits System.Web.UI.Page

    Dim SessionID As String
    Dim InvestorID As Integer = 0
    Dim p As String
    Dim ReportParameterDS As New DataSet
    Dim InvestorHoldingsDS As New DataSet

    Dim ReportHeaderDS As New DataSet
    Dim ReportSignatureDS As New DataSet

    Dim HeaderValue As String = String.Empty
    Dim SignatureValue As String = String.Empty

    Dim util As New Utilities()


    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            '' Get Variables from URL string
            SessionID = Request.QueryString("SessionID")
            p = Request.QueryString("p")

            LoadReceiptRecord()
            LoadReceiptDetails()

            If (p = 1) Then
                Response.Write("<script>")
                Response.Write("window.print()")
                Response.Write("</script>")
            End If





        End If
    End Sub
    Private ReadOnly Property BankConnectString As String
        Get
            Return ConfigurationManager.ConnectionStrings("BankConnString").ConnectionString
        End Get
    End Property

  

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="InvestorID"></param>
    ''' <remarks></remarks>
    Private Sub LoadReceiptRecord()
        If Not Request.QueryString("SessionID") Is Nothing And Not String.IsNullOrEmpty(Request.QueryString("SessionID")) Then
            SessionID = Request.QueryString("SessionID").Trim()
        End If

        HeaderValue = GetHeaderValue()

        Using conn As New OleDbConnection(util.ConnectString)
            Dim cmd As New OleDbCommand()

            lblHeader.Text = HeaderValue.Replace("%DATETIME%", Date.Today)

            cmd.CommandText = String.Format("SELECT genii_user.CASHIER_SESSION.RECORD_ID AS 'Cashier Session', " & _
                                            "  GETDATE() AS 'ENTRY_DATE', " & _
                                            "   GETDATE() AS 'RECORD_DATE', " & _
                                            "             'xxx' AS 'MEMO', " & _
                                            "   5 AS 'MEMO_NUMBER', " & _
                                            "   GETDATE() AS 'TAX_RECEIPT_OPEN_DATE', " & _
                                            "             'JE-REVDep' + CONVERT(varchar, DATEPART(dd, GETDATE())) AS 'REFERENCE', " & _
                                            "             genii_user.CASHIER_SESSION.POSTED_DATE, " & _
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
                                            " WHERE genii_user.CASHIER_SESSION.RECORD_ID = {0} " & _
                                            " GROUP BY genii_user.CASHIER_SESSION.RECORD_ID, " & _
                                            "             genii_user.CASHIER_SESSION.POSTED_DATE ", SessionID)


            cmd.Connection = conn

            conn.Open()
            Me.gvDepositRecord.DataSource = cmd.ExecuteReader()
            Me.gvDepositRecord.DataBind()
        End Using
    End Sub


    Private Sub LoadReceiptDetails()
        If Not Request.QueryString("SessionID") Is Nothing And Not String.IsNullOrEmpty(Request.QueryString("SessionID")) Then
            SessionID = Request.QueryString("SessionID").Trim()
        End If

        HeaderValue = GetHeaderValue()

        Using conn As New OleDbConnection(util.ConnectString)
            Dim cmd As New OleDbCommand()

            cmd.CommandText = String.Format("SELECT genii_user.CASHIER_APPORTION.ReceiptNumber AS 'RECEIPT_NUMBER', " & _
                                             " SUM(genii_user.CASHIER_APPORTION.DollarAmount) AS 'AMOUNT',  " & _
                                             "  genii_user.CASHIER_APPORTION.GLAccount AS 'ACCOUNT', " & _
                                             "  genii_user.CASHIER_POSTING_GL.Description AS 'MEMO', " & _
                                             "            'genii_user' AS 'CREATE_USER', " & _
                                             "  GETDATE() AS 'CREATE_DATE', " & _
                                             "            'genii_user' AS 'EDIT_USER', " & _
                                             "  GETDATE() AS 'EDIT_DATE'              " & _
                                             "            FROM genii_user.CASHIER_SESSION " & _
                                             " INNER JOIN genii_user.CASHIER_TRANSACTIONS " & _
                                             "   ON genii_user.CASHIER_SESSION.RECORD_ID = genii_user.CASHIER_TRANSACTIONS.SESSION_ID " & _
                                             " INNER JOIN genii_user.CASHIER_APPORTION " & _
                                             "   ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID " & _
                                             " INNER JOIN genii_user.CASHIER_POSTING_GL " & _
                                             "   ON genii_user.CASHIER_APPORTION.GLAccount = genii_user.CASHIER_POSTING_GL.GLAccount " & _
                                            " WHERE  genii_user.CASHIER_SESSION.RECORD_ID = {0} " & _
                                            " GROUP BY genii_user.CASHIER_APPORTION.GLAccount, " & _
                                             "            genii_user.CASHIER_APPORTION.ReceiptNumber, " & _
                                             "            genii_user.CASHIER_POSTING_GL.Description " & _
                                             "            ORDER BY 'ACCOUNT' ", SessionID)


            cmd.Connection = conn

            conn.Open()
            Me.gvDepositDetails.DataSource = cmd.ExecuteReader()
            Me.gvDepositDetails.DataBind()
        End Using
    End Sub

    Public Function GetHeaderValue() As String
        Dim myHeaderValue As String = String.Empty

        If (SessionID > 0) Then
            Dim SQL As String = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE PARAMETER_NAME = 'Header'")

            LoadTable(ReportHeaderDS, "ST_PARAMETER", SQL)

            Dim row As DataRow = ReportHeaderDS.Tables(0).Rows(0)

            myHeaderValue = IIf(IsDBNull(row("PARAMETER")), String.Empty, row("PARAMETER"))
        End If

        Return myHeaderValue
    End Function


    Public Function GetSignatureValue() As String
        Dim mySigValue As String = String.Empty

        If (SessionID > 0) Then
            Dim SQL As String = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE PARAMETER_NAME = 'Signature'")

            LoadTable(ReportSignatureDS, "ST_PARAMETER", SQL)

            Dim row As DataRow = ReportSignatureDS.Tables(0).Rows(0)

            mySigValue = IIf(IsDBNull(row("PARAMETER")), String.Empty, row("PARAMETER"))
        End If

        Return mySigValue
    End Function


    ''' <summary>
    ''' Loads table from database into dataset.
    ''' </summary>
    ''' <param name="container"></param>
    ''' <param name="tableName"></param>
    ''' <param name="query"></param>
    ''' <remarks></remarks>
    Private Sub LoadTable(ByVal container As DataSet, ByVal tableName As String, ByVal query As String)
        Using adt As New OleDbDataAdapter(query, util.ConnectString)
            adt.Fill(container, tableName)
        End Using
    End Sub
End Class


