Imports System.Data
Imports System.Data.OleDb

Partial Class TaxCP
    Inherits System.Web.UI.Page
    Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
        If Not Me.IsPostBack Then
            PopulateLookupYears()
            PopulateSalePrepYears()
        End If
    End Sub

#Region "Properties"
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
#End Region

#Region "Lookup"
#Region "Lookup - Number and Investor"
    Private Sub PopulateLookupYears()
        Dim dt As New DataTable()

        Using adp As New OleDbDataAdapter("SELECT DISTINCT TaxYear FROM genii_user.TR ORDER BY TaxYear DESC", Me.ConnectString)

            adp.Fill(dt)

            With Me.ddlLookupYear
                .DataTextField = "TaxYear"
                .DataValueField = "TaxYear"
                .DataSource = dt
                .DataBind()
            End With
        End Using
    End Sub

    Private Function LoadCP(whereClause As String) As DataTable
        Dim sql As String = "SELECT CP.CertificateNumber AS 'Certificate', CP.FaceValueOfCP AS 'Face Value', CP.MonthlyRateOfInterest AS 'Interest Rate', CP.TaxYear AS 'Year', " & _
            "CP.TaxRollNumber AS 'Roll', CP.APN AS 'Parcel', ISNULL(INV.FirstName, '') + ' ' + ISNULL(INV.MiddleName, '') + ' ' + ISNULL(INV.LastName, '') AS 'Investor', " & _
            "CASE CP_STATUS WHEN 0 THEN 'Preparation' WHEN 1 THEN 'Purchased' WHEN 2 THEN 'Assigned to State' WHEN 3 THEN 'Purchased from State' WHEN 4 THEN 'Reassigned' " & _
            "WHEN 5 THEN 'Redeemed' WHEN 6 THEN 'Closed by Deed' WHEN 7 THEN 'Expiring' WHEN 8 THEN 'Expired' END AS 'CP Status', CONVERT(varchar(10), " & _
            "CP.DateCPPurchased, 101) AS 'Purchased', CONVERT(varchar(10), CP.DateOfSale, 101) AS 'Sold', CONVERT(varchar(10), CP.DateCPReassigned, 101) " & _
            "AS 'Reassigned' " & _
            "FROM genii_user.TR_CP AS CP INNER JOIN " & _
            "genii_user.ST_INVESTOR AS INV ON CP.InvestorID = INV.InvestorID " & _
            "WHERE " & whereClause & _
            " ORDER BY CP.CertificateNumber"

        Using adp As New OleDbDataAdapter(sql, Me.ConnectString)
            Dim dt As New DataTable()
            adp.Fill(dt)
            Return dt
        End Using
    End Function

    Protected Sub btnLookupNumberGo_Click(sender As Object, e As System.EventArgs) Handles btnLookupNumberGo.Click
        ' Lookup CP by certificate number.
        Dim where As String = "CP.CertificateNumber = '" & Me.txtLookupNumber.Text & "'"
        Dim dt As DataTable = LoadCP(where)

        Me.dtlLookupNumber.DataSource = dt
        Me.dtlLookupNumber.DataBind()
    End Sub

    Protected Sub btnLookupInvestorGo_Click(sender As Object, e As System.EventArgs) Handles btnLookupInvestorGo.Click
        ' Lookup CP by investor id.
        Dim where As String = "CP.InvestorID = " & Me.txtLookupInvestor.Text
        Dim dt As DataTable = LoadCP(where)

        Me.grdLookupInvestor.DataSource = dt
        Me.grdLookupInvestor.DataBind()
    End Sub
#End Region

#Region "Lookup - Candidates"
    Protected Sub btnLookupCandidatesGo_Click(sender As Object, e As System.EventArgs) Handles btnLookupCandidatesGo.Click
        Dim sql As String = "select left([Parcel],3) as [Book], count(*) as [NumCandidates] from vTaxSaleCandidates group by left([Parcel],3) order by [Book]"
        Using adp As New OleDbDataAdapter(sql, Me.ConnectString)
            Dim dt As New DataTable()
            adp.Fill(dt)

            Me.grdCandidatesTop.DataSource = dt
            Me.grdCandidatesTop.DataBind()
        End Using
    End Sub

    Protected Sub grdCandidatesTop_RowCommand(sender As Object, e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles grdCandidatesTop.RowCommand
        Dim book As String = e.CommandArgument
        Dim sql As String = String.Format("SELECT * FROM vTaxSaleCandidates WHERE left([Parcel],3) = '{0}'", book)

        Using adp As New OleDbDataAdapter(sql, Me.ConnectString)
            Dim dt As New DataTable()
            adp.Fill(dt)

            Me.grdCandidatesChild.DataSource = dt
            Me.grdCandidatesChild.DataBind()
        End Using

        ClientScript.RegisterStartupScript(Me.GetType(), "TaxSaleCandidatesDialog", "openLookupCandidatesDialog();", True)
    End Sub
#End Region

#Region "Lookup - Year"
    Protected Sub btnLookupYearGo_Click(sender As Object, e As System.EventArgs) Handles btnLookupYearGo.Click
        ' Lookup CP by Year
        Dim where As String = "CP.TaxYear = " & Me.ddlLookupYear.SelectedItem.Value
        Dim dt As DataTable = LoadCP(where)

        Me.grdLookupYear.DataSource = dt
        Me.grdLookupYear.DataBind()
    End Sub
#End Region
#End Region

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

    
End Class
