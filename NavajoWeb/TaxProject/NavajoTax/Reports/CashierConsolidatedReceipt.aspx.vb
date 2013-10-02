Imports System.Data
Imports System.Data.OleDb

Partial Class ConsolidatedCashierReceipt
    Inherits System.Web.UI.Page

    Dim SessionTaxID As String
    Dim SessionTaxYear As String
    Dim SessionTaxRollNumber As String
    Dim SessionTransDate As String
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
            SessionTaxID = Request.QueryString("TaxIDNumber")
            SessionTaxYear = Request.QueryString("TaxYear")
            SessionTaxRollNumber = Request.QueryString("TaxRollNumber")
            SessionTransDate = Request.QueryString("TransDate")

            If (SessionTransDate = String.Empty) Then
                SessionTransDate = Date.Now
            End If
            '  p = Request.QueryString("p")

            LoadReceiptRecord(SessionTaxRollNumber)
            '  LoadReceiptDetails()

            '  If (p = 1) Then
            'Response.Write("<script>")
            '   Response.Write("window.print()")
            '   Response.Write("</script>")
            ' End If





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
    Private Sub LoadReceiptRecord(TaxRollNumber As String)
        '  If Not Request.QueryString("SessionID") Is Nothing And Not String.IsNullOrEmpty(Request.QueryString("SessionID")) Then
        'SessionID = Request.QueryString("SessionID").Trim()
        '   End If

        Dim TransID As Integer
        Dim GroupKey As Integer
        Dim TransDate As Date

        HeaderValue = GetHeaderValue()
        lblHeader.Text = HeaderValue.Replace("%DATETIME%", Date.Today)

        TaxIDNumber.Text = SessionTaxID


        Dim SQL As String = String.Format("SELECT genii_user.TR_PAYMENTS.trans_id,genii_user.TR_PAYMENTS.Pertinent1, " & _
                                             " genii_user.TR_PAYMENTS.Pertinent2, " & _
                                             "  genii_user.ST_WHO_PAID.DescriptionOfPayer + ' - ' + genii_user.ST_PAYMENT_INSTRUMENT.PaymentDescription as 'Description', genii_user.TR_PAYMENTS.PaymentEffectiveDate " & _
                                             "            FROM genii_user.TR_PAYMENTS  " & _
                                             "  INNER JOIN genii_user.ST_PAYMENT_INSTRUMENT " & _
                                             "    ON genii_user.TR_PAYMENTS.PaymentTypeCode = genii_user.ST_PAYMENT_INSTRUMENT.PaymentTypeCode " & _
                                             "  INNER JOIN genii_user.ST_WHO_PAID " & _
                                             "    ON genii_user.TR_PAYMENTS.PaymentMadeByCode = genii_user.ST_WHO_PAID.PaymentMadeByCode " & _
                                             "            WHERE genii_user.TR_PAYMENTS.TRANS_ID > 1 and genii_user.TR_PAYMENTS.TaxRollNumber='{0}' and genii_user.TR_PAYMENTS.TaxYear={1} ", SessionTaxRollNumber, SessionTaxYear)


        Using adt3 As New OleDbDataAdapter(SQL, util.ConnectString)
            Dim tblTRCP As New DataTable()

            adt3.Fill(tblTRCP)

            If tblTRCP.Rows.Count > 0 Then
                If (Not IsDBNull(tblTRCP.Rows(0)("trans_id"))) Then
                    TransID = Convert.ToString(tblTRCP.Rows(0)("trans_id"))
                End If
                If (Not IsDBNull(tblTRCP.Rows(0)("Pertinent1"))) Then
                    Me.Pert1.Text = Convert.ToString(tblTRCP.Rows(0)("Pertinent1"))
                End If
                If (Not IsDBNull(tblTRCP.Rows(0)("Pertinent2"))) Then
                    Me.Pert2.Text = Convert.ToString(tblTRCP.Rows(0)("Pertinent2"))
                Else
                    Me.Pert2.Text = "N/A"
                End If
                If (Not IsDBNull(tblTRCP.Rows(0)("Description"))) Then
                    Me.Desc.Text = Convert.ToString(tblTRCP.Rows(0)("Description"))
                End If

                If (Not IsDBNull(tblTRCP.Rows(0)("PaymentEffectiveDate"))) Then
                    GETDATE.Text = Convert.ToDateTime(tblTRCP.Rows(0)("PaymentEffectiveDate"))
                    TransDate = Convert.ToString(tblTRCP.Rows(0)("PaymentEffectiveDate"))
                    PaymentDate.Text = TransDate.ToString("U")
                End If
            End If
        End Using

        Dim SQL1 As String = String.Format("select Parameter from genii_user.ST_PARAMETER where Parameter_name='COUNTY_NAME'")


        Using adt As New OleDbDataAdapter(SQL1, util.ConnectString)
            Dim tblCashierTrans As New DataTable()

            adt.Fill(tblCashierTrans)

            If tblCashierTrans.Rows.Count > 0 Then
                If (Not IsDBNull(tblCashierTrans.Rows(0)("Parameter"))) Then
                    CountyName.Text = Convert.ToString(tblCashierTrans.Rows(0)("Parameter"))
                End If
            End If
        End Using

        Dim SQL2 As String = String.Format("select group_key from genii_user.cashier_transactions where record_id={0} ", TransID)


        Using adt As New OleDbDataAdapter(SQL2, util.ConnectString)
            Dim tblCashierTrans As New DataTable()

            adt.Fill(tblCashierTrans)

            If tblCashierTrans.Rows.Count > 0 Then
                If (Not IsDBNull(tblCashierTrans.Rows(0)("group_key"))) Then
                    GroupKey = Convert.ToString(tblCashierTrans.Rows(0)("group_key"))
                End If
            End If
        End Using

        Dim totalAmount As String = String.Format("select sum(payment_amt) as totalAmount from genii_user.CASHIER_TRANSACTIONS " & _
                                             "        WHERE GROUP_KEY ={0} ", GroupKey)


        Using adt As New OleDbDataAdapter(totalAmount, util.ConnectString)
            Dim tblCashierTrans As New DataTable()

            adt.Fill(tblCashierTrans)

            If tblCashierTrans.Rows.Count > 0 Then
                If (Not IsDBNull(tblCashierTrans.Rows(0)("totalAmount"))) Then
                    Amount.Text = Convert.ToString(tblCashierTrans.Rows(0)("totalAmount"))
                End If
            End If
        End Using

        Dim SQL3 As String = String.Format("SELECT TAX_YEAR AS 'Tax Year', " & _
                                             " TAX_ROLL_NUMBER AS 'Roll', " & _
                                             "  CONVERT(varchar(10), PAYMENT_DATE, 101) AS 'Date', " & _
                                             "  PAYMENT_AMT AS 'Payment Applied', " & _
                                             "  APPLY_TO AS 'Applied To' " & _
                                             "       FROM genii_user.CASHIER_TRANSACTIONS " & _
                                             "        WHERE GROUP_KEY ={0} ", GroupKey)

        BindGrid(Me.grdTransactionData, SQL3)


        Dim SQL4 As String = String.Format("SELECT  distinct   a.CertificateNumber, a.CP_STATUS, a.TaxYear, a.TaxRollNumber,  a.APN, a.DateOfSale, a.DATE_REDEEMED " & _
                                           "  From genii_user.TR_CP a, genii_user.TR b " & _
                                            " WHERE     a.APN=b.APN and a.CP_STATUS = 5 AND b.TaxIDNumber='{0}' AND a.DATE_REDEEMED > convert(datetime,'{1}')", SessionTaxID, SessionTransDate)

        BindGrid(Me.grdCPRedeemed, SQL4)





    End Sub

    Private Sub BindGrid(grid As GridView, commandText As String)
        Dim dt As New DataTable()

        Using adt As New OleDbDataAdapter(commandText, util.ConnectString)
            adt.SelectCommand.CommandTimeout = 300
            adt.Fill(dt)
        End Using

        With grid
            .DataSource = dt
            .DataBind()
        End With
    End Sub

    Private Sub LoadReceiptDetails()
        '  If Not Request.QueryString("SessionID") Is Nothing And Not String.IsNullOrEmpty(Request.QueryString("SessionID")) Then
        'SessionID = Request.QueryString("SessionID").Trim()
        '   End If

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
                                             "            FROM genii_user.CASHIER_SESSION  " & _
                                             " INNER JOIN genii_user.CASHIER_TRANSACTIONS " & _
                                             "   ON genii_user.CASHIER_SESSION.RECORD_ID = genii_user.CASHIER_TRANSACTIONS.SESSION_ID " & _
                                             " INNER JOIN genii_user.CASHIER_APPORTION " & _
                                             "   ON genii_user.CASHIER_TRANSACTIONS.RECORD_ID = genii_user.CASHIER_APPORTION.TRANS_ID " & _
                                             " INNER JOIN genii_user.CASHIER_POSTING_GL " & _
                                             "   ON genii_user.CASHIER_APPORTION.GLAccount = genii_user.CASHIER_POSTING_GL.GLAccount " & _
                                            " WHERE  genii_user.CASHIER_SESSION.RECORD_ID = {0} " & _
                                            " GROUP BY genii_user.CASHIER_APPORTION.GLAccount, " & _
                                             "            genii_user.CASHIER_APPORTION.ReceiptNumber, " & _
                                             "            genii_user.CASHIER_POSTING_GL.Description" & _
                                             "            ORDER BY 'ACCOUNT' ", TaxIDNumber)


            cmd.Connection = conn

            conn.Open()
            '  Me.gvDepositDetails.DataSource = cmd.ExecuteReader()
            '   Me.gvDepositDetails.DataBind()
        End Using
    End Sub

    Public Function GetHeaderValue() As String
        Dim myHeaderValue As String = String.Empty

        '   If (Not (TaxYear > 0)) Then
        Dim SQL As String = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE PARAMETER_NAME = 'Header'")

        LoadTable(ReportHeaderDS, "ST_PARAMETER", SQL)

        Dim row As DataRow = ReportHeaderDS.Tables(0).Rows(0)

        myHeaderValue = IIf(IsDBNull(row("PARAMETER")), String.Empty, row("PARAMETER"))
        '  End If

        Return myHeaderValue
    End Function


    'Public Function GetSignatureValue() As String
    '    Dim mySigValue As String = String.Empty

    '    '  If (SessionID > 0) Then
    '    Dim SQL As String = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE PARAMETER_NAME = 'Signature'")

    '    LoadTable(ReportSignatureDS, "ST_PARAMETER", SQL)

    '    Dim row As DataRow = ReportSignatureDS.Tables(0).Rows(0)

    '    mySigValue = IIf(IsDBNull(row("PARAMETER")), String.Empty, row("PARAMETER"))
    '    ' End If

    '    Return mySigValue
    'End Function


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


