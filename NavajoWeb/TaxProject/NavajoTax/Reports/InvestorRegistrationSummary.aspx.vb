Imports System.Data
Imports System.Data.OleDb

Partial Class Reports_InvestorRegistrationSummary
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

            HeaderValue = GetHeaderValue()
            SignatureValue = GetSignatureValue()

            ''InvestorID = 155

            Dim SQL As String = String.Format("SELECT FirstName, MiddleName, LastName, Address1, Address2, City, State, PostalCode, " & _
                                              "SocialSecNum, Active, PhoneNumber, NW_Vendor, ConfidentialFlag, EMailAddress " & _
                                              "FROM genii_user.ST_INVESTOR WHERE InvestorID = {0} ", InvestorID)

            LoadTable(ReportParameterDS, "ST_INVESTOR", SQL)

            Dim row As DataRow = ReportParameterDS.Tables(0).Rows(0)

            lblHeader.Text = HeaderValue.Replace("%DATETIME%", Date.Today)
            lblSignature.Text = SignatureValue
            lblReportDate.Text = Date.Today()
            lblFirstName.Text = IIf(IsDBNull(row("FirstName")), String.Empty, row("FirstName"))
            lblMiddleName.Text = IIf(IsDBNull(row("MiddleName")), String.Empty, row("MiddleName"))
            lblLastName.Text = IIf(IsDBNull(row("LastName")), String.Empty, row("LastName"))
            lblAddress1.Text = IIf(IsDBNull(row("Address1")), String.Empty, row("Address1"))
            lblAddress2.Text = IIf(IsDBNull(row("Address2")), String.Empty, row("Address2"))
            lblCity.Text = IIf(IsDBNull(row("City")), String.Empty, row("City"))
            lblState.Text = IIf(IsDBNull(row("State")), String.Empty, row("State"))
            lblPostalCode.Text = IIf(IsDBNull(row("PostalCode")), String.Empty, row("PostalCode"))
            lblSSN.Text = IIf(IsDBNull(row("SocialSecNum")), String.Empty, row("SocialSecNum"))
            lblVendorNum.Text = IIf(IsDBNull(row("NW_Vendor")), String.Empty, row("NW_Vendor"))
            lblPhone.Text = IIf(IsDBNull(row("PhoneNumber")), String.Empty, row("PhoneNumber"))
            lblEMail.Text = IIf(IsDBNull(row("EMailAddress")), String.Empty, row("EMailAddress"))
            lblConfidential.Text = IIf(row("ConfidentialFlag") = 1, "Yes", "No")
            lblActive.Text = IIf(row("Active") = 1, "Yes", "No")


        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try

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


