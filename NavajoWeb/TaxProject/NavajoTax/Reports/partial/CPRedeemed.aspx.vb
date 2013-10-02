
Imports System.Data
Imports System.Data.OleDb

Partial Class Reports_CPRedeemed
    Inherits System.Web.UI.Page

    Dim ReportID As Integer = 0
    Dim InvestorID As Integer = 0
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
            LoadReportValues()

            LoadCertificateNoticeGridData(InvestorID)

        End If
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub LoadReportValues()
        Try
            If Not Request.QueryString("ReportID") Is Nothing And Not String.IsNullOrEmpty(Request.QueryString("ReportID")) Then
                ReportID = Request.QueryString("ReportID").Trim()
            End If

            If Not Request.QueryString("InvestorID") Is Nothing And Not String.IsNullOrEmpty(Request.QueryString("InvestorID")) Then
                InvestorID = Convert.ToInt32(Request.QueryString("InvestorID").Trim())
            End If

            InvestorID = 155

            HeaderValue = GetHeaderValue()
            SignatureValue = GetSignatureValue()


            Dim SQL As String = String.Format("SELECT FirstName, MiddleName, LastName, Address1, Address2, City, State, " & _
                                              "PostalCode FROM genii_user.ST_INVESTOR WHERE InvestorID = {0}; ", InvestorID)

            LoadTable(ReportParameterDS, "ST_INVESTOR", SQL)

            Dim row As DataRow = ReportParameterDS.Tables(0).Rows(0)

            lblHeader.Text = HeaderValue.Replace("%DATETIME%", Date.Today)
            lblSignature.Text = SignatureValue
            lblReportDate.Text = Date.Today()
            lblInvestor.Text = IIf(IsDBNull(row("FirstName")), row("LastName"), row("FirstName") & " " & row("LastName"))
            lblAddress1.Text = IIf(IsDBNull(row("Address1")), String.Empty, row("Address1"))
            lblAddress2.Text = IIf(IsDBNull(row("Address2")), String.Empty, row("Address2"))
            lblCity.Text = IIf(IsDBNull(row("City")), String.Empty, row("City"))
            lblState.Text = IIf(IsDBNull(row("State")), String.Empty, row("State"))
            lblPostalCode.Text = IIf(IsDBNull(row("PostalCode")), String.Empty, row("PostalCode"))



        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try

    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="InvestorID"></param>
    ''' <remarks></remarks>
    Private Sub LoadCertificateNoticeGridData(ByVal InvestorID As Integer)
        ''InvestorID = 155
        Using conn As New OleDbConnection(util.ConnectString)
            Dim cmd As New OleDbCommand()

            cmd.CommandText = String.Format("SELECT CertificateNumber AS CP, ISNULL(APN,'') AS APNPARCEL, ISNULL(DateofSale,'') AS SaleDate, DATEADD(year, 10, DateofSale) AS Expiration, " & _
                                            "FaceValueOfCP,PurchaseValue FROM genii_user.TR_CP WHERE DATEADD(MONTH, +114, DateofSale) >= DATEADD(Month,-6,GETDATE()) " & _
                                            "AND DATEADD(MONTH, +114, DateofSale) < GETDATE() AND InvestorID = {0}; ", InvestorID)


            cmd.Connection = conn

            conn.Open()
            Me.gvCertificateNotice.DataSource = cmd.ExecuteReader()
            Me.gvCertificateNotice.DataBind()
        End Using
    End Sub


    Public Function GetHeaderValue() As String
        Dim myHeaderValue As String = String.Empty

        If (ReportID > 0) Then
            Dim SQL As String = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE PARAMETER_NAME = 'Header'")

            LoadTable(ReportHeaderDS, "ST_PARAMETER", SQL)

            Dim row As DataRow = ReportHeaderDS.Tables(0).Rows(0)

            myHeaderValue = IIf(IsDBNull(row("PARAMETER")), String.Empty, row("PARAMETER"))
        End If

        Return myHeaderValue
    End Function


    Public Function GetSignatureValue() As String
        Dim mySigValue As String = String.Empty

        If (ReportID > 0) Then
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


