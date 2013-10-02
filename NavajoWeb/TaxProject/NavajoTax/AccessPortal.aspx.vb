Imports System.Data
Imports System.Data.OleDb
Imports System.Web.Services
Imports System.Data.SqlClient

Namespace AccessPortal
    Partial Class AccessPortal
        Inherits System.Web.UI.Page

        'Dim ConnectString As String = ConfigurationManager.ConnectionStrings("ConnString").ConnectionString
        Dim ParcelOrTaxID As String = String.Empty
        Dim APN As String = String.Empty
        Dim TaxAccountDS As DataSet = New DataSet()
        Dim TrDS As DataSet = New DataSet()
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
        ' Private _accountAlert As Integer = 0
        '  Private _accountSuspend As Integer = 0
        Private _accountStatus As Integer = 0
        Private _accountBankruptcy As Integer = 0
        Private _trStatus As Integer = 0
        Private _trBoardOrder As Integer = 0
        Private _trCP As Integer = 0
        Private _trConfidential As Integer = 0
        Private _trMailReturned As Integer = 0
        Private _TRPaymentRule As Integer = 0




        Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
            If Not Me.IsPostBack Then
                PopulateLookupYears()
                FillTaxYearDropDown()
                ResetCautionLights()
                LoadCountyInfo()

            End If
            ' Dim sql As String
            '  sql = "select image from genii_user.ST_PARAMETER where Parameter_name='Logo'"
            'Using conn As New OleDbConnection(Me.ConnectString)
            'conn.Open()
            ' Dim cmd As New OleDbCommand()
            ' cmd.Connection = conn

            ' cmd.CommandText = "select image from genii_user.ST_PARAMETER where Parameter_name='Logo'"
            ' cmd = New OleDbCommand(sql, conn)

            ' Dim dr As OleDbDataReader = cmd.ExecuteReader()
            ' dr.Read()
            ' Context.Response.BinaryWrite(dr("image"))
            ' dr.Close()

            ' End Using

        End Sub
        Public Property AccountSuspend As Integer
            Get
                Return _accountSuspend
            End Get
            Set(ByVal value As Integer)
                _accountSuspend = value
            End Set
        End Property
        Public Property AccountAlert As Integer
            Get
                Return _accountAlert
            End Get
            Set(ByVal value As Integer)
                _accountAlert = value
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
        Private Sub FillTaxYearDropDown()
            For i As Integer = 40 To 0 Step -1
                Dim myYear As Integer = i + 1980
                Dim newItem As ListItem = New ListItem(myYear, myYear)

                ddlTaxYear.Items.Add(newItem)
            Next

            ddlTaxYear.SelectedValue = Date.Today.Year - 1
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
        Public Sub btnTaxIDSearch_Click(sender As Object, e As System.EventArgs) Handles btnTaxIDSearch.Click
            '  txtParcelSearch.Text = String.Empty
            txtLastNameSearch.Text = String.Empty
            '  Dim a As String
            Dim searchText As String, a = GetParcelOrTaxID(txtTaxIDSearch.Text)

            ParcelOrTaxID = a.Keys(0)


            If Not (ParcelOrTaxID = String.Empty) Then
                If (Not (String.IsNullOrEmpty(txtTaxIDSearch.Text.Trim()))) Then

                    ' Bind grids with APN or ParcelOrTaxID
                    '   ParcelOrTaxID = txtLastNameSearch.Text.Trim().Replace("-", String.Empty)

                    '  txtLastNameSearch.Text = a.Values(0)
                    ParcelOrTaxID = ParcelOrTaxID.Replace("-", String.Empty)
                    LoadTaxAccountValues(ParcelOrTaxID)

                    ' CHECK FOR APN:
                    'If (Not (String.IsNullOrEmpty(txtAPN.Text.Trim()))) Then
                    '
                    APN = txtAPN.Text.Trim()

                    ' IF WE HAVE APN, BIND ALL OTHER CONTROLS THAT DEPEND ON IT
                    BindTaxRollGrid(APN)
                    BindDeedsGrid(APN)
                    BindRemarksGrid(APN)
                    'BindLossGrid(APN)


                    'BindCPGrid(APN)

                    '  Me.btnShowAccountRemarksPopup.Enabled = True



                    'End If


                    txtAPN.Text = String.Empty

                End If
            End If

            ' txtParcelSearch.Text = String.Empty



        End Sub
        Private Sub CheckAccountStatus()
            Dim apn As String

            'If (Me.TaxRollMaster.APN.Replace("-", String.Empty) = String.Empty) Then
            '    If (txtTaxID.Text = String.Empty) Then
            '        apn = Me.TaxRollMaster.TaxIDNumber
            '    Else
            '        apn = txtTaxID.Text
            '    End If

            'Else
            '    apn = Me.TaxRollMaster.APN.Replace("-", String.Empty)
            'End If
            Dim SQL As String = String.Format("SELECT ACCOUNT_ALERT, ACCOUNT_SUSPEND,ACCOUNT_STATUS, isnull(ACCOUNT_BANKRUPTCY,0) as ACCOUNT_BANKRUPTCY " & _
                                              "FROM genii_user.TAX_ACCOUNT " & _
                                              "WHERE ParcelOrTaxID = '{0}' ", txtTaxIDSearch2.Text)

            Using adt As New OleDbDataAdapter(SQL, Me.ConnectString)
                Dim tblTaxAccount As New DataTable()

                adt.Fill(tblTaxAccount)

                If tblTaxAccount.Rows.Count > 0 Then

                End If
                If (Not IsDBNull(tblTaxAccount.Rows(0)("ACCOUNT_ALERT"))) Then
                    AccountAlert = Convert.ToInt32(tblTaxAccount.Rows(0)("ACCOUNT_ALERT"))
                End If

                If (Not IsDBNull(tblTaxAccount.Rows(0)("ACCOUNT_SUSPEND"))) Then
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
                    AccountBankruptcy = 1000
                    btnBankruptcyLight.Enabled = False

                End If
            End Using


            Dim SQL2 As String = String.Format("SELECT taxrollnumber,STATUS,BOARD_ORDER,isnull(FLAG_CONFIDENTIAL,0) as FLAG_CONFIDENTIAL,isnull(FLAG_MAIL_RETURNED,0)AS FLAG_MAIL_RETURNED " & _
                                              "FROM genii_user.TR " & _
                                              "WHERE taxrollnumber={0} and taxyear ={1}", txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)


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
                                              "WHERE taxrollnumber={0} and taxyear ={1}", txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)


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

        End Sub
        'GetTaxRollNumber
        Public Sub btnTaxRollNumber_Click(sender As Object, e As System.EventArgs) Handles btnTaxRollSearch.Click
            Dim TaxYear As String = ddlTaxYear.SelectedValue
            ' txtParcelSearch2.Text = String.Empty
            Dim apn As String
            txtLastNameSearch2.Text = String.Empty
            Dim searchText As String, a = GetTaxRollNumber(txtTabOuterTaxRoll.Text, TaxYear)
            ' txtParcelSearch.Text = String.Empty
            apn = a.Keys(0)

            If Not (apn = String.Empty) Then
                If (Not (String.IsNullOrEmpty(txtTabOuterTaxRoll.Text.Trim()))) Then

                    ' Bind grids with APN or ParcelOrTaxID
                    '   ParcelOrTaxID = txtLastNameSearch.Text.Trim().Replace("-", String.Empty)

                    '     txtLastNameSearch.Text = a.Values(0)
                    ParcelOrTaxID = apn.Replace("-", String.Empty)
                    txtTaxIDSearch2.Text = ParcelOrTaxID
                    txtLastNameSearch2.Text = a.Values(0)
                    txtLastNameSearch.Text = a.Values(0)
                    '  LoadTaxAccountValues(ParcelOrTaxID)
                    'LoadOuterTaxRollValues(ParcelOrTaxID)
                    BindTaxRollOuterGrid(ParcelOrTaxID, TaxYear)
                    BindTaxRollChargesGrid(txtTabOuterTaxRoll.Text, TaxYear)
                    BindTaxRollPaymentsGrid(txtTabOuterTaxRoll.Text, TaxYear)
                    BindTaxRollCPGrid(txtTabOuterTaxRoll.Text, TaxYear)
                    BindTaxRollRemarksGrid(txtTabOuterTaxRoll.Text, TaxYear)

                    LoadTaxAccountValues(ParcelOrTaxID)

                    BindTaxRollGrid(apn)
                    BindDeedsGrid(apn)
                    BindRemarksGrid(apn)
                    ' CHECK FOR APN:
                    If (Not (String.IsNullOrEmpty(txtAPN2.Text.Trim()))) Then

                        apn = txtAPN2.Text.Trim()

                        ' IF WE HAVE APN, BIND ALL OTHER CONTROLS THAT DEPEND ON IT
                        '    BindTaxRollGrid(APN)
                        '   BindDeedsGrid(APN)
                        '   BindRemarksGrid(APN)
                        'BindLossGrid(APN)
                        'BindCPGrid(APN)
                    End If

                    txtAPN2.Text = String.Empty
                End If

                CheckAccountStatus()

                If (Me.AccountAlert >= 0) Then
                    SetAlertMessage()

                    Me.btnAlertLight.Enabled = True
                Else
                    Me.btnAlertLight.Enabled = False
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

                ' Else
                '    ClientScript.RegisterStartupScript(Me.GetType(), "TaxRollNotFound", "showMessage('Tax roll not found.', 'Not Found');", True)
            End If

            
        End Sub

        Public Sub btnTaxIDSearch2_Click(sender As Object, e As System.EventArgs) Handles btnTaxIDSearch2.Click
            Dim TaxYear As String = ddlTaxYear.SelectedValue
            Dim apn As String
            ' txtParcelSearch2.Text = String.Empty
            txtLastNameSearch2.Text = String.Empty
            Dim searchText As String, a = GetParcelOrTaxID2(txtTaxIDSearch2.Text)
            ' txtParcelSearch.Text = String.Empty
            txtLastNameSearch2.Text = a.Values(0)
            apn = a.Keys(0)

            If Not (apn = String.Empty) Then
                If (Not (String.IsNullOrEmpty(txtTaxIDSearch2.Text.Trim()))) Then

                    ' Bind grids with APN or ParcelOrTaxID
                    '   ParcelOrTaxID = txtLastNameSearch.Text.Trim().Replace("-", String.Empty)

                    '     txtLastNameSearch.Text = a.Values(0)
                    ParcelOrTaxID = apn.Replace("-", String.Empty)
                    '  LoadTaxAccountValues(ParcelOrTaxID)
                    'LoadOuterTaxRollValues(ParcelOrTaxID)
                    BindTaxRollOuterGrid(ParcelOrTaxID, TaxYear)
                    BindTaxRollChargesGrid(txtTabOuterTaxRoll.Text, TaxYear)
                    BindTaxRollPaymentsGrid(txtTabOuterTaxRoll.Text, TaxYear)
                    BindTaxRollCPGrid(txtTabOuterTaxRoll.Text, TaxYear)
                    BindTaxRollRemarksGrid(txtTabOuterTaxRoll.Text, TaxYear)

                    LoadTaxAccountValues(ParcelOrTaxID)

                    BindTaxRollGrid(apn)
                    BindDeedsGrid(apn)
                    BindRemarksGrid(apn)

                    ' CHECK FOR APN:
                    If (Not (String.IsNullOrEmpty(txtAPN2.Text.Trim()))) Then

                        apn = txtAPN2.Text.Trim()

                        ' IF WE HAVE APN, BIND ALL OTHER CONTROLS THAT DEPEND ON IT
                        '    BindTaxRollGrid(APN)
                        '   BindDeedsGrid(APN)
                        '   BindRemarksGrid(APN)
                        'BindLossGrid(APN)
                        'BindCPGrid(APN)
                    End If

                    txtAPN2.Text = String.Empty
                End If
                CheckAccountStatus()

                If (Me.AccountAlert >= 0) Then
                    SetAlertMessage()

                    Me.btnAlertLight.Enabled = True
                Else
                    Me.btnAlertLight.Enabled = False
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
            End If

      

        End Sub

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
                    Me.btnAccountStatusLight.BackColor = Drawing.Color.Blue
                    Me.btnAccountStatusLight.ForeColor = Drawing.Color.Black
                Case 6
                    Me.btnAccountStatusLight.Text = "Unsecured-Abated"
                    Me.btnAccountStatusLight.BackColor = Drawing.Color.Blue
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
                    Me.btnRollStatusLight.BackColor = Drawing.Color.Blue
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
                    Me.btnRollStatusLight.BackColor = Drawing.Color.Blue
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

        '  Public Sub BindCPGrid(myAPN As String)
        'Dim sql As String
        'Dim conn As OleDbConnection

        '       sql = String.Format("SELECT TaxYear, TaxRollNumber, " +
        '                        "CP_Status, APN " +
        '                       "FROM genii_user.TR_CP WHERE APN = '{0}' ORDER BY TaxYear DESC", myAPN)



        '       Using conn As New OleDbConnection(Me.ConnectString)
        '           conn.Open()
        '   Dim cmd As New OleDbCommand()
        '          cmd.Connection = conn
        '
        '            cmd.CommandText = "SELECT TaxYear, TaxRollNumber, " +
        '                "CP_Status, APN " +
        '            "FROM genii_user.TR_CP WHERE APN =" + myAPN + " ORDER BY TaxYear DESC"
        '


        '  OleDbCommand(cmd  as New OleDbCommand(sql, conn))
        '          this.grdCP.DataSource = cmd.ExecuteReader()
        '         Me.grdc()

        '        this.grdCP.DataBind()
        '    End Using

        '  End Sub

        Public Sub BindTaxRollGrid(myAPN As String)
            Dim sql As String
            'Dim conn As OleDbConnection

            sql = String.Format("SELECT TaxYear, " +
                                            "TaxRollNumber, " +
                                          "SecuredUnsecured = CASE SecuredUnsecured WHEN 'S' THEN 'Secured' ELSE 'Unsecured' END, " +
                                         "CurrentBalance, " +
                                         "Status = CASE CurrentBalance WHEN '0' THEN 'Paid In Full' ELSE 'Balance Due' END, " +
                                         "APN " +
                                           "FROM genii_user.TR WHERE TaxIDNumber = '{0}' ORDER BY TaxYear DESC", txtTaxIDSearch.Text)

            Using conn As New OleDbConnection(Me.ConnectString)
                conn.Open()
                Dim cmd As New OleDbCommand()
                cmd.Connection = conn
                cmd.CommandText = "SELECT TaxYear, " +
                                          "TaxRollNumber, " +
                                         "SecuredUnsecured = CASE SecuredUnsecured WHEN 'S' THEN 'Secured' ELSE 'Unsecured' END, " +
                                         "CurrentBalance, " +
                                         "Status = CASE CurrentBalance WHEN '0' THEN 'Paid In Full' ELSE 'Balance Due' END, " +
                                         "APN " +
                                         "FROM genii_user.TR WHERE TaxIDNumber = '" + txtTaxIDSearch.Text + "' ORDER BY TaxYear DESC"

                ' OleDbCommand(cmd = New OleDbCommand(sql, conn))
                cmd = New OleDbCommand(sql, conn)

                Me.grdTaxRoll.DataSource = cmd.ExecuteReader()

                Me.grdTaxRoll.DataBind()
            End Using

        End Sub


        Public Sub BindDeedsGrid(myAPN As String)
            Dim sql As String
            sql = String.Format("SELECT genii_user.TAX_ACCOUNT_DEED.APN AS 'Parcel', genii_user.TAX_ACCOUNT_DEED.DEED_YEAR AS 'Deed Year', " +
                               " ISNULL(CONVERT(varchar(15), genii_user.TAX_ACCOUNT_DEED.Initiated, 101), 'Not Completed') as 'Initiated', " +
                                 "  ISNULL(CONVERT(varchar(15), genii_user.TAX_ACCOUNT_DEED.Completed, 101), 'Not Completed') as 'Completed', " +
                                 "  CASE genii_user.TAX_ACCOUNT_DEED.Cancelled " +
                                 "    WHEN 0 THEN 'Not Cancelled' " +
                                 "    WHEN 1 THEN 'Cancelled' END AS 'Status', " +
                                "   genii_user.ST_INVESTOR.LastName AS 'Foreclosing Party', " +
                                 " case  " +
                                " when  ISNULL(CONVERT(varchar(15), genii_user.TAX_ACCOUNT_DEED.Completed, 101), 'Not Completed')='Not Completed' then  " +
                                " ''  " +
                                " when  ISNULL(CONVERT(varchar(15), genii_user.TAX_ACCOUNT_DEED.Completed, 101), 'Not Completed')<> 'Not Completed' then " +
                                " 'LOSS' " +
                                " End 'WithLoss' " +
                                "             FROM genii_user.TAX_ACCOUNT_DEED " +
                                "   INNER JOIN genii_user.ST_INVESTOR " +
                                "     ON genii_user.TAX_ACCOUNT_DEED.InvestorID = genii_user.ST_INVESTOR.InvestorID " +
                                " WHERE (genii_user.TAX_ACCOUNT_DEED.APN = '{0}') order by genii_user.TAX_ACCOUNT_DEED.DEED_YEAR", myAPN)

            Using conn As New OleDbConnection(Me.ConnectString)
                conn.Open()
                Dim cmd As New OleDbCommand()
                cmd.Connection = conn
                cmd.CommandText = "SELECT genii_user.TAX_ACCOUNT_DEED.APN AS 'Parcel', genii_user.TAX_ACCOUNT_DEED.DEED_YEAR AS 'Deed Year', " +
                               " ISNULL(CONVERT(varchar(15), genii_user.TAX_ACCOUNT_DEED.Initiated, 101), 'Not Completed') as 'Initiated', " +
                                 "  ISNULL(CONVERT(varchar(15), genii_user.TAX_ACCOUNT_DEED.Completed, 101), 'Not Completed') as 'Completed', " +
                                 "  CASE genii_user.TAX_ACCOUNT_DEED.Cancelled " +
                                 "    WHEN 0 THEN 'Not Cancelled' " +
                                 "    WHEN 1 THEN 'Cancelled' END AS 'Status', " +
                                "   genii_user.ST_INVESTOR.LastName AS 'Foreclosing Party', " +
                                " case  " +
                                " when  ISNULL(CONVERT(varchar(15), genii_user.TAX_ACCOUNT_DEED.Completed, 101), 'Not Completed')='Not Completed' then  " +
                                " ''  " +
                                " when  ISNULL(CONVERT(varchar(15), genii_user.TAX_ACCOUNT_DEED.Completed, 101), 'Not Completed')<> 'Not Completed' then " +
                                " 'LOSS' " +
                                " End 'WithLoss' " +
                                "             FROM genii_user.TAX_ACCOUNT_DEED " +
                                "   INNER JOIN genii_user.ST_INVESTOR " +
                                "     ON genii_user.TAX_ACCOUNT_DEED.InvestorID = genii_user.ST_INVESTOR.InvestorID " +
                                " WHERE genii_user.TAX_ACCOUNT_DEED.APN = " + myAPN + " order by genii_user.TAX_ACCOUNT_DEED.DEED_YEAR"

                ' OleDbCommand(cmd = New OleDbCommand(sql, conn))
                cmd = New OleDbCommand(sql, conn)

                Me.grdDeeds.DataSource = cmd.ExecuteReader()
                Me.grdDeeds.DataBind()

                grdLoss.Visible = True
                lblLoss.Visible = True
            End Using


        End Sub

        Public Sub BindRemarksGrid(myAPN As String)
            Dim sql As String
            sql = String.Format("SELECT RECORD_ID, REMARKS, IMAGE, TASK_DATE, FILE_TYPE " +
                                "FROM genii_user.TAX_ACCOUNT_CALENDAR " +
                                "WHERE ParcelOrTaxID = '{0}' ORDER BY TASK_DATE", Me.ParcelOrTaxID)

            Using conn As New OleDbConnection(Me.ConnectString)
                conn.Open()
                Dim cmd As New OleDbCommand()
                cmd.Connection = conn
                cmd.CommandText = "SELECT RECORD_ID, REMARKS, IMAGE, TASK_DATE, FILE_TYPE " +
                                "FROM genii_user.TAX_ACCOUNT_CALENDAR " +
                                "WHERE ParcelOrTaxID = " + Me.ParcelOrTaxID + " ORDER BY TASK_DATE"

                ' OleDbCommand(cmd = New OleDbCommand(sql, conn))
                cmd = New OleDbCommand(sql, conn)

                Me.gvAccountRemarks.DataSource = cmd.ExecuteReader()
                Me.gvAccountRemarks.DataBind()
            End Using


        End Sub

        Public Sub BindTaxRollRemarksGrid(taxRollNumber As String, taxyear As String)
            Dim sql As String
            sql = String.Format("SELECT RECORD_ID, REMARKS, IMAGE, CHANGE_TYPE " +
                                "FROM genii_user.ST_BOARD_ORDER " +
                                "WHERE TaxRollNumber = '{0}' and TaxYear=" + taxyear + " ORDER BY TaxRollNumber", taxRollNumber)

            Using conn As New OleDbConnection(Me.ConnectString)
                conn.Open()
                Dim cmd As New OleDbCommand()
                cmd.Connection = conn
                cmd.CommandText = "SELECT RECORD_ID, REMARKS, IMAGE, TASK_DATE, FILE_TYPE " +
                                "FROM genii_user.ST_BOARD_ORDER " +
                                "WHERE TaxRollNumber = '" + taxRollNumber + "' and TaxYear=" + taxyear + "  ORDER BY TaxRollNumber"

                ' OleDbCommand(cmd = New OleDbCommand(sql, conn))
                cmd = New OleDbCommand(sql, conn)

                Me.gvTaxRollRemarks.DataSource = cmd.ExecuteReader()
                Me.gvTaxRollRemarks.DataBind()
            End Using


        End Sub



        Public Sub BindLossGrid(myAPN As String)
            Dim sql As String
            sql = String.Format("SELECT genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxYear AS 'Tax Year',  " +
                                " genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxRollNumber AS 'Roll', " +
                                " '$' + CONVERT(varchar, SUM(genii_user.TAX_ACCOUNT_DEED_LOSS.ChargeAmount), 1) AS 'Revenue Loss' " +
                                " FROM genii_user.TAX_ACCOUNT_DEED_TR_CLEAR " +
                                " INNER JOIN genii_user.TAX_ACCOUNT_DEED_LOSS " +
                                " ON genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxYear = genii_user.TAX_ACCOUNT_DEED_LOSS.TaxYear " +
                                " AND genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxRollNumber = genii_user.TAX_ACCOUNT_DEED_LOSS.TaxRollNumber " +
                                " WHERE (genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.APN = '{0}') " +
                                " GROUP BY genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxYear,  " +
                                " genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxRollNumber ORDER BY genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxYear", myAPN)

            Using conn As New OleDbConnection(Me.ConnectString)
                conn.Open()
                Dim cmd As New OleDbCommand()
                cmd.Connection = conn
                cmd.CommandText = "SELECT genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxYear AS 'Tax Year',  " +
                                " genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxRollNumber AS 'Roll', " +
                                " '$' + CONVERT(varchar, SUM(genii_user.TAX_ACCOUNT_DEED_LOSS.ChargeAmount), 1) AS 'Revenue Loss' " +
                                " FROM genii_user.TAX_ACCOUNT_DEED_TR_CLEAR " +
                                " INNER JOIN genii_user.TAX_ACCOUNT_DEED_LOSS " +
                                " ON genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxYear = genii_user.TAX_ACCOUNT_DEED_LOSS.TaxYear " +
                                " AND genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxRollNumber = genii_user.TAX_ACCOUNT_DEED_LOSS.TaxRollNumber " +
                                " WHERE (genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.APN = " + myAPN + ") " +
                                " GROUP BY genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxYear,  " +
                                " genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxRollNumber  ORDER BY genii_user.TAX_ACCOUNT_DEED_TR_CLEAR.TaxYear"

                ' OleDbCommand(cmd = New OleDbCommand(sql, conn))
                cmd = New OleDbCommand(sql, conn)

                Me.grdLoss.DataSource = cmd.ExecuteReader()
                Me.grdLoss.DataBind()

                Me.lblLoss.Text = "Loss from " + myAPN + ":"
                '  Me.lblLoss.Visible = False
                ' Me.lblLoss.Visible = False
                '

            End Using


        End Sub
        Protected Sub grdDeeds_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles grdDeeds.RowCommand
            If (e.CommandName = "withLoss") Then
                Dim myApn As String = Me.txtTaxIDSearch.Text
                Dim apn As String = Me.ParcelOrTaxID
                Dim apnlabel As String = Me.lblParcelNumber.Text
                'BindLossGrid(Me.ParcelOrTaxID)
                BindLossGrid(apnlabel)
                'Me.grdLoss.Visible = True
                ' Me.lblLoss.Visible = True


            End If
        End Sub


        Protected Sub grdTaxRoll_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles grdTaxRoll.RowCommand


            If (e.CommandName = "gotoOuterTaxRoll") Then
                'bind grid in grdtaxroll Outer tab..
                Dim APN As String = Me.txtAPN.Text
                Dim index As Integer = Convert.ToInt32(e.CommandArgument)
                Dim selectedRow As GridViewRow = grdTaxRoll.Rows(index)
                Dim taxYear As String = selectedRow.Cells(1).Text
                Dim taxRollNumber As String = selectedRow.Cells(2).Text
                ParcelOrTaxID = Me.txtTaxIDSearch.Text.Trim()
                BindTaxRollOuterGrid(ParcelOrTaxID, taxYear)
                BindTaxRollChargesGrid(taxRollNumber, taxYear)
                BindTaxRollPaymentsGrid(taxRollNumber, taxYear)
                BindTaxRollCPGrid(taxRollNumber, taxYear)
                BindTaxRollRemarksGrid(taxRollNumber, taxYear)

                ' TabContainer2.ActiveTab = TabContainer2.Tabs(0)
                'Response.Redirect("~/InformationPortal.aspx#tabOuterTaxRoll", False)
            End If
        End Sub

        Public Sub BindTaxRollCPGrid(taxRollNumber As String, taxyear As String)
            Dim sql As String
            'taxyear = taxyear + 1
            sql = String.Format("SELECT genii_user.TR_CP.CertificateNumber AS 'Certificate', " +
                                 " genii_user.TR_CP.TaxYear AS 'Tax Year',  " +
                                 "  genii_user.TR_CP.TaxRollNumber As 'Roll Number', " +
                                 "  CONVERT(varchar, genii_user.TR_CP.MonthlyRateOfInterest*100) + '%' AS 'Interest', " +
                                 "            '$' + CONVERT(varchar, genii_user.TR_CP.FaceValueOfCP, 1) AS 'Face Value',  " +
                                 "            '$' + CONVERT(varchar, genii_user.TR_CP.PurchaseValue, 1) AS 'Purchse Value', " +
                                 "  genii_user.ST_INVESTOR.LastName AS 'Investor', " +
                                 "  CASE genii_user.TR_CP.CP_STATUS " +
                                 "    WHEN 0 THEN 'Preparation' " +
                                 "    WHEN 1 THEN 'Purchased' " +
                                 "    WHEN 2 THEN 'Assigned to State' " +
                                 "    WHEN 3 THEN 'Purchased from State' " +
                                 "    WHEN 4 THEN 'Reassigned' " +
                                 "    WHEN 5 THEN 'Redeemed' " +
                                 "    WHEN 6 THEN 'Deeded' " +
                                 "    WHEN 7 THEN 'Expiring' " +
                                 "    WHEN 8 THEN 'Expired' END AS 'Current Status', " +
                                 "      ISNULL(CONVERT(varchar(10), genii_user.TR_CP.DateOfSale, 101), '') as 'Date of Sale', " +
                                 "  ISNULL(CONVERT(varchar(10), genii_user.TR_CP.DateCPPurchased, 101), '') as 'Purchase Date', " +
                                 "  ISNULL(CONVERT(varchar(10), genii_user.TR_CP.DATE_REDEEMED, 101), '') as 'Date Dedeemed', " +
                                 "  ISNULL('$' + CONVERT(varchar, genii_user.TR_CP.INTEREST_EARNED, 1), '') AS 'Interest Earned' " +
                                 "            FROM genii_user.TR_CP " +
                                 "  INNER JOIN genii_user.ST_INVESTOR " +
                                 "    ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " +
                                " WHERE      genii_user.TR_CP.TaxYear = " + taxyear + "  AND  genii_user.TR_CP.TaxRollNumber = '{0}' ", taxRollNumber)

            Using conn As New OleDbConnection(Me.ConnectString)
                conn.Open()
                Dim cmd As New OleDbCommand()
                cmd.Connection = conn
                cmd.CommandText = "SELECT genii_user.TR_CP.CertificateNumber AS 'Certificate', " +
                                 " genii_user.TR_CP.TaxYear AS 'Tax Year',  " +
                                 "  genii_user.TR_CP.TaxRollNumber As 'Roll Number', " +
                                 "  CONVERT(varchar, genii_user.TR_CP.MonthlyRateOfInterest*100) + '%' AS 'Interest', " +
                                 "            '$' + CONVERT(varchar, genii_user.TR_CP.FaceValueOfCP, 1) AS 'Face Value',  " +
                                 "            '$' + CONVERT(varchar, genii_user.TR_CP.PurchaseValue, 1) AS 'Purchase Value', " +
                                 "  genii_user.ST_INVESTOR.LastName AS 'Investor', " +
                                 "  CASE genii_user.TR_CP.CP_STATUS " +
                                 "    WHEN 0 THEN 'Preparation' " +
                                 "    WHEN 1 THEN 'Purchased' " +
                                 "    WHEN 2 THEN 'Assigned to State' " +
                                 "    WHEN 3 THEN 'Purchased from State' " +
                                 "    WHEN 4 THEN 'Reassigned' " +
                                 "    WHEN 5 THEN 'Redeemed' " +
                                 "    WHEN 6 THEN 'Deeded' " +
                                 "    WHEN 7 THEN 'Expiring' " +
                                 "    WHEN 8 THEN 'Expired' END AS 'Current Status', " +
                                 "      ISNULL(CONVERT(varchar(10), genii_user.TR_CP.DateOfSale, 101), '') as 'Date of Sale', " +
                                 "  ISNULL(CONVERT(varchar(10), genii_user.TR_CP.DateCPPurchased, 101), '') as 'Purchase Date', " +
                                 "  ISNULL(CONVERT(varchar(10), genii_user.TR_CP.DATE_REDEEMED, 101), '') as 'Date Dedeemed', " +
                                 "  ISNULL('$' + CONVERT(varchar, genii_user.TR_CP.INTEREST_EARNED, 1), '') AS 'Interest Earned' " +
                                 "            FROM genii_user.TR_CP " +
                                 "  INNER JOIN genii_user.ST_INVESTOR " +
                                 "    ON genii_user.TR_CP.InvestorID = genii_user.ST_INVESTOR.InvestorID " +
                                " WHERE      genii_user.TR_CP.TaxYear = " + taxyear + "  AND  genii_user.TR_CP.TaxRollNumber = " + taxRollNumber + " "

                ' OleDbCommand(cmd = New OleDbCommand(sql, conn))
                cmd = New OleDbCommand(sql, conn)

                Me.grdTaxRollCP.DataSource = cmd.ExecuteReader()
                Me.grdTaxRollCP.DataBind()
            End Using


        End Sub

        Public Sub BindTaxRollPaymentsGrid(taxRollNumber As String, taxYear As String)
            Dim sql As String
            sql = String.Format("SELECT genii_user.TR_PAYMENTS.TaxYear AS 'Tax Year', " +
                                  " genii_user.TR_PAYMENTS.TaxRollNumber AS 'Roll Number', " +
                                  "          '$' + CONVERT(varchar, genii_user.TR_PAYMENTS.PaymentAmount, 1) AS 'Payment Amount',  " +
                                  " CONVERT(varchar(10), genii_user.TR_PAYMENTS.PaymentEffectiveDate, 101) as 'Effective Date',  " +
                                  " genii_user.TR_PAYMENTS.Pertinent1 AS 'Name on Instrument', " +
                                  " genii_user.TR_PAYMENTS.Pertinent2 AS 'Instrument Note', " +
                                  " genii_user.ST_WHO_PAID.DescriptionOfPayer AS 'Payor Description', " +
                                  " genii_user.ST_PAYMENT_INSTRUMENT.PaymentDescription AS 'Payment Description' " +
                                  "          FROM genii_user.TR_PAYMENTS  " +
                                 " INNER JOIN genii_user.ST_WHO_PAID " +
                                  "  ON genii_user.TR_PAYMENTS.PaymentMadeByCode = genii_user.ST_WHO_PAID.PaymentMadeByCode " +
                                 " INNER JOIN genii_user.ST_PAYMENT_INSTRUMENT " +
                                 "   ON genii_user.TR_PAYMENTS.PaymentTypeCode = genii_user.ST_PAYMENT_INSTRUMENT.PaymentTypeCode " +
                                " WHERE genii_user.TR_PAYMENTS.TaxYear = " + taxYear + " AND genii_user.TR_PAYMENTS.TaxRollNumber = '{0}'", taxRollNumber)

            Using conn As New OleDbConnection(Me.ConnectString)
                conn.Open()
                Dim cmd As New OleDbCommand()
                cmd.Connection = conn
                cmd.CommandText = "SELECT genii_user.TR_PAYMENTS.TaxYear AS 'Tax Year', " +
                                  " genii_user.TR_PAYMENTS.TaxRollNumber AS' Roll Number', " +
                                  "          '$' + CONVERT(varchar, genii_user.TR_PAYMENTS.PaymentAmount, 1) AS 'Payment Amount',  " +
                                  " CONVERT(varchar(10), genii_user.TR_PAYMENTS.PaymentEffectiveDate, 101) as 'Effective Date',  " +
                                  " genii_user.TR_PAYMENTS.Pertinent1 AS 'Name on Instrument', " +
                                  " genii_user.TR_PAYMENTS.Pertinent2 AS 'Instrument Note', " +
                                  " genii_user.ST_WHO_PAID.DescriptionOfPayer AS 'Payor Description', " +
                                  " genii_user.ST_PAYMENT_INSTRUMENT.PaymentDescription AS 'Payment Description' " +
                                  "          FROM genii_user.TR_PAYMENTS  " +
                                 " INNER JOIN genii_user.ST_WHO_PAID " +
                                  "  ON genii_user.TR_PAYMENTS.PaymentMadeByCode = genii_user.ST_WHO_PAID.PaymentMadeByCode " +
                                 " INNER JOIN genii_user.ST_PAYMENT_INSTRUMENT " +
                                 "   ON genii_user.TR_PAYMENTS.PaymentTypeCode = genii_user.ST_PAYMENT_INSTRUMENT.PaymentTypeCode " +
                                " WHERE genii_user.TR_PAYMENTS.TaxYear = " + taxYear + " AND genii_user.TR_PAYMENTS.TaxRollNumber = " + taxRollNumber + ""

                ' OleDbCommand(cmd = New OleDbCommand(sql, conn))
                cmd = New OleDbCommand(sql, conn)

                Me.grdTaxRollPayments.DataSource = cmd.ExecuteReader()
                Me.grdTaxRollPayments.DataBind()
            End Using


        End Sub


        Public Sub BindTaxRollChargesGrid(taxRollNumber As String, taxYear As String)
            Dim sql As String
            sql = String.Format("SELECT genii_user.TR_CHARGES.TaxYear AS 'Tax Year', " +
                                 " genii_user.TR_CHARGES.TaxRollNumber AS 'Roll Number', " +
                                 " genii_user.LEVY_AUTHORITY.TaxChargeDescription + ' (' + genii_user.TR_CHARGES.TaxChargeCodeID + ')' AS 'Authority', " +
                                 " genii_user.LEVY_TAX_TYPES.TaxCodeDescription + ' (' +  genii_user.TR_CHARGES.TaxTypeID + ')' AS 'Tax Type', " +
                                 " ISNULL('$' + CONVERT(varchar, genii_user.TR_CHARGES.ChargeAmount, 1), '') AS 'Charge', " +
                                 " ISNULL('$' + CONVERT(varchar, genii_user.TR_CHARGES.OriginalLevyAmount, 1), '') AS 'Original Value' " +
                                 "           FROM genii_user.TR_CHARGES  " +
                                 "  INNER JOIN genii_user.LEVY_TAX_TYPES " +
                                 "   ON genii_user.TR_CHARGES.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID " +
                                 "      INNER JOIN genii_user.LEVY_AUTHORITY " +
                                 "      ON genii_user.TR_CHARGES.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID " +
                                 " WHERE (genii_user.TR_CHARGES.TaxYear = " + taxYear + ") AND (genii_user.TR_CHARGES.TaxRollNumber = '{0}') " +
                                 " ORDER BY genii_user.TR_CHARGES.TaxChargeCodeID", taxRollNumber)

            Using conn As New OleDbConnection(Me.ConnectString)
                conn.Open()
                Dim cmd As New OleDbCommand()
                cmd.Connection = conn
                cmd.CommandText = "SELECT genii_user.TR_CHARGES.TaxYear AS 'Tax Year', " +
                                 " genii_user.TR_CHARGES.TaxRollNumber AS 'Roll Number', " +
                                 " genii_user.LEVY_AUTHORITY.TaxChargeDescription + ' (' + genii_user.TR_CHARGES.TaxChargeCodeID + ')' AS 'Authority', " +
                                 " genii_user.LEVY_TAX_TYPES.TaxCodeDescription + ' (' +  genii_user.TR_CHARGES.TaxTypeID + ')' AS 'Tax Type', " +
                                 " ISNULL('$' + CONVERT(varchar, genii_user.TR_CHARGES.ChargeAmount, 1), '') AS 'Charge', " +
                                 " ISNULL('$' + CONVERT(varchar, genii_user.TR_CHARGES.OriginalLevyAmount, 1), '') AS 'Original Value' " +
                                 "           FROM genii_user.TR_CHARGES  " +
                                 "  INNER JOIN genii_user.LEVY_TAX_TYPES " +
                                 "   ON genii_user.TR_CHARGES.TaxTypeID = genii_user.LEVY_TAX_TYPES.TaxTypeID " +
                                 "      INNER JOIN genii_user.LEVY_AUTHORITY " +
                                 "      ON genii_user.TR_CHARGES.TaxChargeCodeID = genii_user.LEVY_AUTHORITY.TaxChargeCodeID " +
                                 " WHERE (genii_user.TR_CHARGES.TaxYear = " + taxYear + ") AND (genii_user.TR_CHARGES.TaxRollNumber = " + taxRollNumber + ") " +
                                 " ORDER BY genii_user.TR_CHARGES.TaxChargeCodeID"

                ' OleDbCommand(cmd = New OleDbCommand(sql, conn))
                cmd = New OleDbCommand(sql, conn)

                Me.grdTaxRollCharges.DataSource = cmd.ExecuteReader()
                Me.grdTaxRollCharges.DataBind()
            End Using


        End Sub

        Public Sub BindTaxRollOuterGrid(myAPN As String, taxYear As String)
            Dim sql As String
            '   Dim parcelOrTaxID = txtParcelSearch.Text
            sql = String.Format("SELECT TR.*,LPS.LPS_NAME FROM genii_user.TR TR, genii_user.ST_LENDER_PROCESSING_SERVICES LPS  WHERE TR.TaxPayerID=LPS.Record_id and TAXIDNUMBER='{0}' AND TAXYEAR= " + taxYear + " ", myAPN) 'parcelOrTaxID) 'Me.ParcelOrTaxID)

            LoadTable(TrDS, "TR", sql)
            Dim row As DataRow
            row = TrDS.Tables(0).Rows(0)

            If (IsDBNull(row("TaxYear"))) Then
                Me.lblTaxYear.Text = String.Empty
                Me.ddlTaxYear.Text = String.Empty
            Else
                Me.lblTaxYear.Text = row("TaxYear")
                Me.ddlTaxYear.Text = row("TaxYear")
            End If

            If (IsDBNull(row("TaxRollNumber"))) Then
                Me.lblTaxRollNumber.Text = String.Empty
                Me.txtTabOuterTaxRoll.Text = String.Empty
            Else
                Me.lblTaxRollNumber.Text = row("TaxRollNumber")
                Me.txtTabOuterTaxRoll.Text = row("TaxRollNumber")
            End If

            If (IsDBNull(row("TaxIDNumber"))) Then
                Me.txtTaxIDSearch2.Text = String.Empty
            Else
                Me.txtTaxIDSearch.Text = row("TaxIDNumber")
                Me.txtTaxIDSearch2.Text = row("TaxIDNumber")
            End If
            'lblLenderProcessingService
            If (IsDBNull(row("LPS_NAME"))) Then
                Me.lblLenderProcessingService.Text = String.Empty
            Else
                Me.lblLenderProcessingService.Text = row("LPS_NAME")
            End If

            If (IsDBNull(row("FirstHalfDelinquent"))) Then
                Me.lblFirstHalfDelinquent.Text = String.Empty
            Else
                Me.lblFirstHalfDelinquent.Text = row("FirstHalfDelinquent")
            End If

            If (IsDBNull(row("SecondHalfDelinquent"))) Then
                Me.lblSecondHalfDelinquent.Text = String.Empty
            Else
                Me.lblSecondHalfDelinquent.Text = row("SecondHalfDelinquent")
            End If
            'If (IsDBNull(row("APN"))) Then
            '    Me.lblParcelNumberAPN.Text = String.Empty
            'Else
            '    Me.lblParcelNumberAPN.Text = row("APN")
            'End If

            'If (IsDBNull(row("SecuredUnsecured"))) Then
            '    Me.lblSecuredOuter.Text = String.Empty
            'Else
            '    Me.lblSecuredOuter.Text = row("SecuredUnsecured")
            'End If

            'If (IsDBNull(row("STATUS"))) Then
            '    Me.lblStatus.Text = String.Empty
            'Else
            '    Me.lblStatus.Text = row("STATUS")
            'End If

            'If (IsDBNull(row("BOARD_ORDER"))) Then
            '    Me.lblBoardOrder.Text = String.Empty
            'Else
            '    Me.lblBoardOrder.Text = row("BOARD_ORDER")
            'End If

            If (IsDBNull(row("TaxArea"))) Then
                Me.lblTaxArea.Text = String.Empty
            Else
                Me.lblTaxArea.Text = row("TaxArea")
            End If

            If (IsDBNull(row("AREA"))) Then
                Me.lblDistrict.Text = String.Empty
            Else
                Me.lblDistrict.Text = row("AREA")
            End If

            'If (IsDBNull(row("SUBAREA"))) Then
            '    Me.lblDistrictArea.Text = String.Empty
            'Else
            '    Me.lblDistrictArea.Text = row("SUBAREA")
            'End If

            'If (IsDBNull(row("LATITUDE"))) Then
            '    Me.lblLat.Text = String.Empty
            'Else
            '    Me.lblLat.Text = row("LATITUDE")
            'End If

            'If (IsDBNull(row("LONGITUDE"))) Then
            '    Me.lblLong.Text = String.Empty
            'Else
            '    Me.lblLong.Text = row("LONGITUDE")
            'End If

            'If (IsDBNull(row("TaxPayerID"))) Then
            'Me.lbltax.Text = String.Empty
            'Else
            'Me.lblTaxYear.Text = row("TaxPayerID")
            'End If

            If (IsDBNull(row("CurrentBalance"))) Then
                Me.lblCurrentBalance.Text = String.Empty
            Else
                Me.lblCurrentBalance.Text = row("CurrentBalance")
            End If

            'If (IsDBNull(row("OWNER_GROUP"))) Then
            '    Me.lblOwnerGroupOuter.Text = String.Empty
            'Else
            '    Me.lblOwnerGroupOuter.Text = row("OWNER_GROUP")
            'End If

            If (IsDBNull(row("OWNER_NAME_3"))) Then
                Me.lblFirstNameOuter.Text = String.Empty
            Else
                Me.lblFirstNameOuter.Text = row("OWNER_NAME_3")
            End If

            If (IsDBNull(row("OWNER_NAME_2"))) Then
                Me.lblMiddleNameOuter.Text = String.Empty
            Else
                Me.lblMiddleNameOuter.Text = row("OWNER_NAME_2")
            End If

            If (IsDBNull(row("OWNER_NAME_1"))) Then
                Me.lblLastNameOuter.Text = String.Empty
                Me.txtLastNameSearch2.Text = String.Empty
            Else
                Me.lblLastNameOuter.Text = row("OWNER_NAME_1")
                Me.txtLastNameSearch.Text = row("OWNER_NAME_1")
                Me.txtLastNameSearch2.Text = row("OWNER_NAME_1")
            End If

            If (IsDBNull(row("MAIL_ADDRESS_1"))) Then
                Me.lblCOAddressOuter.Text = String.Empty
            Else
                Me.lblCOAddressOuter.Text = row("MAIL_ADDRESS_1")
            End If

            If (IsDBNull(row("MAIL_ADDRESS_2"))) Then
                Me.lblMailingAddressOuter.Text = String.Empty
            Else
                Me.lblMailingAddressOuter.Text = row("MAIL_ADDRESS_2")
            End If

            If (IsDBNull(row("MAIL_CITY"))) Then
                Me.lblMailingCityOuter.Text = String.Empty
            Else
                Me.lblMailingCityOuter.Text = row("MAIL_CITY")
            End If

            If (IsDBNull(row("MAIL_STATE"))) Then
                Me.lblMailingStateOuter.Text = String.Empty
            Else
                Me.lblMailingStateOuter.Text = row("MAIL_STATE")
            End If

            'If (IsDBNull(row("MAIL_CODE"))) Then
            '    Me.lblPostalCodeOuter.Text = String.Empty
            'Else
            '    Me.lblPostalCodeOuter.Text = row("MAIL_CODE")
            'End If

            'If (IsDBNull(row("EMAIL_ADDRESS"))) Then
            '    Me.lblEmailOuter.Text = String.Empty
            'Else
            '    Me.lblEmailOuter.Text = row("EMAIL_ADDRESS")
            'End If

            'If (IsDBNull(row("FLAG_MAIL_RETURNED"))) Then
            '    Me.lblMailReturnFlagOuter.Text = String.Empty
            'Else
            '    Me.lblMailReturnFlagOuter.Text = row("FLAG_MAIL_RETURNED")
            'End If

            'If (IsDBNull(row("FLAG_CONFIDENTIAL"))) Then
            '    Me.lblConfidentialFlagOuter.Text = String.Empty
            'Else
            '    Me.lblConfidentialFlagOuter.Text = row("FLAG_CONFIDENTIAL")
            'End If




            '            Using conn As New OleDbConnection(Me.ConnectString)
            'conn.Open()
            'Dim cmd As New OleDbCommand()
            'cmd.Connection = conn
            'cmd.CommandText = "SELECT * FROM genii_user.TR WHERE TAXIDNUMBER=" + myAPN + " AND TAXYEAR= " + taxYear + " "

            '' OleDbCommand(cmd = New OleDbCommand(sql, conn))
            '            cmd = New OleDbCommand(sql, conn)

            'Me.gvAccountRemarks.DataSource = cmd.ExecuteReader()
            'e.gvAccountRemarks.DataBind()
            'End Using


        End Sub

        Public Sub LoadTaxAccountValues(myParcelOrTaxID As String)
            Dim sql As String
            sql = String.Format("SELECT * FROM genii_user.Tax_Account WHERE ParcelOrTaxID = '{0}'", myParcelOrTaxID)

            LoadTable(TaxAccountDS, "Tax_Account", sql)

            Dim row As DataRow
            row = TaxAccountDS.Tables(0).Rows(0)

            ' Fill in Labels with data from TAX_ACCOUNT
            If (IsDBNull(row("ParcelOrTaxID"))) Then
                Me.lblParcelTaxID.Text = String.Empty        
            Else
                Me.lblParcelTaxID.Text = row("ParcelOrTaxID")
                Me.txtTaxIDSearch.Text = row("ParcelOrTaxID")
            End If

            If (IsDBNull(row("APN"))) Then
                Me.lblParcelNumber.Text = String.Empty
            Else
                Me.lblParcelNumber.Text = row("APN")
            End If

            If (IsDBNull(row("SecuredUnsecured"))) Then
                Me.lblSecured.Text = String.Empty
            Else
                Me.lblSecured.Text = row("SecuredUnsecured")
            End If

            If (IsDBNull(row("Account_Status"))) Then
                Me.lblAccountStatus.Text = String.Empty
            Else
                Me.lblAccountStatus.Text = row("Account_Status")
            End If

            If (IsDBNull(row("Account_Alert"))) Then
                Me.lblAlertLevel.Text = String.Empty
            Else
                Me.lblAlertLevel.Text = row("Account_Alert")
            End If

            If (IsDBNull(row("Account_Suspend"))) Then
                Me.lblAccountSuspend.Text = String.Empty
            Else
                Me.lblAccountSuspend.Text = row("Account_Suspend")
            End If

            If (IsDBNull(row("Collection_Deputy"))) Then
                Me.lblCollectionsDeputy.Text = String.Empty
            Else
                Me.lblCollectionsDeputy.Text = row("Collection_Deputy")
            End If

            If (IsDBNull(row("Latitude"))) Then
                Me.lblLatitude.Text = String.Empty
            Else
                Me.lblLatitude.Text = row("Latitude")
            End If

            If (IsDBNull(row("Longitude"))) Then
                Me.lblLongitude.Text = String.Empty
            Else
                Me.lblLongitude.Text = row("Longitude")
            End If

            If (IsDBNull(row("GIS_HOUSE_ADDRESS"))) Then
                Me.lblHouseNumber.Text = String.Empty
            Else
                Me.lblHouseNumber.Text = row("GIS_HOUSE_ADDRESS")
            End If

            'If (IsDBNull(row("StreetName"))) Then
            '    Me.lblStreet.Text = String.Empty
            'Else
            '    Me.lblStreet.Text = row("StreetName")
            'End If

            If (IsDBNull(row("PHYSICAL_CITY"))) Then
                Me.lblLocationCity.Text = String.Empty
            Else
                Me.lblLocationCity.Text = row("PHYSICAL_CITY")
            End If

            If (IsDBNull(row("Physical_Address"))) Then
                Me.lblPhysicalAddress.Text = String.Empty
            Else
                Me.lblPhysicalAddress.Text = row("Physical_Address")
            End If

            'If (IsDBNull(row("CityLocation"))) Then
            '    Me.lblCity.Text = String.Empty
            'Else
            '    Me.lblCity.Text = row("CityLocation")
            'End If

            If (IsDBNull(row("PHYSICAL_ZIP"))) Then
                Me.lblPostalCode.Text = String.Empty
            Else
                Me.lblPostalCode.Text = row("PHYSICAL_ZIP")
            End If


        End Sub
        'Public Sub LoadOuterTaxRollValues(myParcelOrTaxID As String)
        '    Dim sql As String
        '    sql = String.Format("SELECT * FROM genii_user.TR WHERE TaxIDNumber = '{0}' and TaxYear= {1}", myParcelOrTaxID, ddlTaxYear.SelectedValue)

        '    LoadTable(TrDS, "TR", sql)

        '    Dim row As DataRow
        '    row = TrDS.Tables(0).Rows(0)

        '    ' Fill in Labels with data from TAX_ACCOUNT
        '    If (IsDBNull(row("TaxYear"))) Then
        '        Me.lblParcelTaxID.Text = String.Empty
        '    Else
        '        Me.lblParcelTaxID.Text = row("TaxYear")
        '    End If

        '    If (IsDBNull(row("TaxRollNumber"))) Then
        '        Me.lblParcelNumber.Text = String.Empty
        '    Else
        '        Me.lblParcelNumber.Text = row("TaxRollNumber")
        '    End If

        '    If (IsDBNull(row("TaxIDNumber"))) Then
        '        Me.lblSecured.Text = String.Empty
        '    Else
        '        Me.lblSecured.Text = row("TaxIDNumber")
        '    End If

        '    If (IsDBNull(row("APN"))) Then
        '        Me.lblAccountStatus.Text = String.Empty
        '    Else
        '        Me.lblAccountStatus.Text = row("APN")
        '    End If

        '    If (IsDBNull(row("Account_Alert"))) Then
        '        Me.lblAlertLevel.Text = String.Empty
        '    Else
        '        Me.lblAlertLevel.Text = row("Account_Alert")
        '    End If

        '    If (IsDBNull(row("Account_Suspend"))) Then
        '        Me.lblAccountSuspend.Text = String.Empty
        '    Else
        '        Me.lblAccountSuspend.Text = row("Account_Suspend")
        '    End If

        '    If (IsDBNull(row("Collection_Deputy"))) Then
        '        Me.lblCollectionsDeputy.Text = String.Empty
        '    Else
        '        Me.lblCollectionsDeputy.Text = row("Collection_Deputy")
        '    End If

        '    If (IsDBNull(row("StreetName"))) Then
        '        Me.lblStreetName.Text = String.Empty
        '    Else
        '        Me.lblStreetName.Text = row("StreetName")
        '    End If

        '    If (IsDBNull(row("Latitude"))) Then
        '        Me.lblLatitude.Text = String.Empty
        '    Else
        '        Me.lblLatitude.Text = row("Latitude")
        '    End If

        '    If (IsDBNull(row("Longitude"))) Then
        '        Me.lblLongitude.Text = String.Empty
        '    Else
        '        Me.lblLongitude.Text = row("Longitude")
        '    End If

        '    If (IsDBNull(row("StreetNo"))) Then
        '        Me.lblHouseNumber.Text = String.Empty
        '    Else
        '        Me.lblHouseNumber.Text = row("StreetNo")
        '    End If

        '    If (IsDBNull(row("StreetName"))) Then
        '        Me.lblStreet.Text = String.Empty
        '    Else
        '        Me.lblStreet.Text = row("StreetName")
        '    End If

        '    If (IsDBNull(row("LOCCITY"))) Then
        '        Me.lblLocationCity.Text = String.Empty
        '    Else
        '        Me.lblLocationCity.Text = row("LOCCITY")
        '    End If

        '    If (IsDBNull(row("Physical_Address"))) Then
        '        Me.lblPhysicalAddress.Text = String.Empty
        '    Else
        '        Me.lblPhysicalAddress.Text = row("Physical_Address")
        '    End If

        '    If (IsDBNull(row("CityLocation"))) Then
        '        Me.lblCity.Text = String.Empty
        '    Else
        '        Me.lblCity.Text = row("CityLocation")
        '    End If

        '    If (IsDBNull(row("Postal_Code"))) Then
        '        Me.lblPostalCode.Text = String.Empty
        '    Else
        '        Me.lblPostalCode.Text = row("Postal_Code")
        '    End If


        'End Sub
        Public Sub LoadTable(container As DataSet, tableName As String, query As String)
            'Dim adt As OleDbDataAdapter


            Using adt As New OleDbDataAdapter(query, Me.ConnectString)
                adt.Fill(container, tableName)
            End Using


        End Sub

        'Public Sub btnParcelSearch_Click(sender As Object, e As EventArgs) Handles btnParcelSearch.Click
        '    txtTaxIDSearch.Text = String.Empty
        '    txtLastNameSearch.Text = String.Empty
        '    If (Not (String.IsNullOrEmpty(txtParcelSearch.Text.Trim()))) Then

        '        ' Bind grids with APN or ParcelOrTaxID
        '        ParcelOrTaxID = txtParcelSearch.Text
        '        LoadTaxAccountValues(ParcelOrTaxID)


        '        ' CHECK FOR APN:
        '        If (Not (String.IsNullOrEmpty(txtAPN.Text.Trim()))) Then

        '            APN = txtAPN.Text.Trim()

        '            ' IF WE HAVE APN, BIND ALL OTHER CONTROLS THAT DEPEND ON IT
        '            BindTaxRollGrid(APN)
        '            BindDeedsGrid(APN)
        '            BindRemarksGrid(APN)
        '            'BindLossGrid(APN)

        '            ' BindTaxRollChargesGrid(APN)
        '            'BindCPGrid(APN)
        '        End If

        '        txtAPN.Text = String.Empty

        '    End If
        'End Sub

        'Public Sub btnParcelSearch2_Click(sender As Object, e As EventArgs) Handles btnParcelSearch2.Click
        '    txtTaxIDSearch2.Text = String.Empty
        '    txtLastNameSearch2.Text = String.Empty
        '    If (Not (String.IsNullOrEmpty(txtParcelSearch2.Text.Trim()))) Then

        '        ' Bind grids with APN or ParcelOrTaxID
        '        ParcelOrTaxID = txtParcelSearch2.Text
        '        'LoadTaxAccountValues(ParcelOrTaxID)
        '        BindTaxRollOuterGrid(ParcelOrTaxID, ddlTaxYear.SelectedValue)
        '        BindTaxRollOuterGrid(ParcelOrTaxID, ddlTaxYear.SelectedValue)
        '        BindTaxRollChargesGrid(txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)
        '        BindTaxRollPaymentsGrid(txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)
        '        BindTaxRollCPGrid(txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)
        '        BindTaxRollRemarksGrid(txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)

        '        ' CHECK FOR APN:
        '        If (Not (String.IsNullOrEmpty(txtAPN2.Text.Trim()))) Then

        '            APN = txtAPN2.Text.Trim()

        '            ' IF WE HAVE APN, BIND ALL OTHER CONTROLS THAT DEPEND ON IT
        '            '  BindTaxRollGrid(APN)
        '            '  BindDeedsGrid(APN)
        '            '  BindRemarksGrid(APN)
        '            'BindLossGrid(APN)

        '            ' BindTaxRollChargesGrid(APN)
        '            'BindCPGrid(APN)
        '        End If

        '        txtAPN2.Text = String.Empty

        '    End If
        'End Sub

        Public Sub btnLastNameSearch_Click(sender As Object, e As EventArgs) Handles btnLastNameSearch.Click
            txtTaxIDSearch.Text = String.Empty
            Dim apn As String
            '  Dim a As String
            Dim searchText As String, a = GetNameID(txtLastNameSearch.Text)
            apn = a.Keys(0)

            If Not (apn = String.Empty) Then

                If (Not (String.IsNullOrEmpty(txtLastNameSearch.Text.Trim()))) Then

                    ' Bind grids with APN or ParcelOrTaxID
                    '   ParcelOrTaxID = txtLastNameSearch.Text.Trim().Replace("-", String.Empty)

                    txtLastNameSearch.Text = a.Values(0)
                    txtTaxIDSearch.Text = apn.Replace("-", String.Empty)
                    ParcelOrTaxID = apn.Replace("-", String.Empty)

                    LoadTaxAccountValues(ParcelOrTaxID)
                    BindTaxRollGrid(apn)
                    BindDeedsGrid(apn)
                    BindRemarksGrid(apn)
                    'BindLossGrid(APN)

                    'BindTaxRollChargesGrid(APN)
                    ' BindCPGrid(APN)

                End If

            End If

            ' txtParcelSearch.Text = String.Empty
           
            txtAPN.Text = String.Empty
        End Sub

        Public Sub btnLastNameSearch2_Click(sender As Object, e As EventArgs) Handles btnLastNameSearch2.Click
            Dim apn As String
            txtTaxIDSearch2.Text = String.Empty
            Dim searchText As String, a = GetNameID(txtLastNameSearch2.Text)
            ' txtParcelSearch.Text = String.Empty
            apn = a.Keys(0)

            If Not (apn = String.Empty) Then
                If (Not (String.IsNullOrEmpty(txtLastNameSearch2.Text.Trim()))) Then

                    ' Bind grids with APN or ParcelOrTaxID
                    '   ParcelOrTaxID = txtLastNameSearch.Text.Trim().Replace("-", String.Empty)

                    txtLastNameSearch2.Text = a.Values(0)
                    ParcelOrTaxID = apn.Replace("-", String.Empty)
                    txtTaxIDSearch2.Text = ParcelOrTaxID
                    BindTaxRollOuterGrid(ParcelOrTaxID, ddlTaxYear.SelectedValue)
                    BindTaxRollChargesGrid(txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)
                    BindTaxRollPaymentsGrid(txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)
                    BindTaxRollCPGrid(txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)
                    BindTaxRollRemarksGrid(txtTabOuterTaxRoll.Text, ddlTaxYear.SelectedValue)

                    LoadTaxAccountValues(ParcelOrTaxID)

                    '  apn = txtLastNameSearch.Text.Trim()
                    BindTaxRollGrid(apn)
                    BindDeedsGrid(apn)
                    BindRemarksGrid(apn)
                    'BindTaxRollGrid(APN)
                    'BindDeedsGrid(APN)
                    'BindRemarksGrid(APN)
                    'BindLossGrid(APN)

                    'BindTaxRollChargesGrid(APN)
                    ' BindCPGrid(APN)

                End If

                txtAPN2.Text = String.Empty

                CheckAccountStatus()

                If (Me.AccountAlert >= 0) Then
                    SetAlertMessage()

                    Me.btnAlertLight.Enabled = True
                Else
                    Me.btnAlertLight.Enabled = False
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



        <System.Web.Services.WebMethod()> _
        Public Shared Function GetParcelOrTaxID(ByVal parcelOrTaxID As String) As Dictionary(Of String, String)
            Dim myUtils As Utilities = New Utilities()

            Dim SQL As String = String.Empty
            Dim APN As String = String.Empty
            Dim myParcelOrTaxID = String.Empty

            Dim cmd As OleDbCommand = New OleDbCommand()

            Dim result As Dictionary(Of String, String) = New Dictionary(Of String, String)

            'search database
            Using conn As New OleDbConnection(myUtils.ConnectString)
                'Check to make sure taxID is not null before doing query
                If (Not (String.IsNullOrEmpty(parcelOrTaxID.Trim()))) Then

                    'SETUP YOUR SQL QUERY HERE
                    SQL = "SELECT TOP 1 ParcelOrTaxID, APN FROM genii_user.TAX_ACCOUNT WHERE ParcelOrTaxID = '" + parcelOrTaxID + "' ;"

                    cmd = New OleDbCommand(SQL)
                    '  cmd.Parameters.AddWithValue("@ParcelOrTaxID", parcelOrTaxID)
                    cmd.Connection = conn
                    conn.Open()

                    Dim reader As OleDbDataReader = cmd.ExecuteReader()

                    While (reader.Read())
                        Dim resultString As StringBuilder = New StringBuilder()

                        'ParcelOrTaxID
                        If (Not (Convert.IsDBNull(reader.GetValue(0)))) Then
                            myParcelOrTaxID = reader.GetValue(0).ToString()
                            ' resultString.AppendFormat("{0}, ", myParcelOrTaxID)
                        End If

                        'APN
                        If (Not (Convert.IsDBNull(reader.GetValue(1)))) Then
                            APN = reader.GetValue(1).ToString()
                            resultString.AppendFormat("{0}, ", APN)
                        End If

                        result.Add(myParcelOrTaxID, resultString.ToString())
                    End While

                End If
            End Using

            Return result
        End Function
        <System.Web.Services.WebMethod()> _
        Public Shared Function GetParcelOrTaxID2(ByVal parcelOrTaxID As String) As Dictionary(Of String, String)
            Dim myUtils As Utilities = New Utilities()

            Dim SQL As String = String.Empty
            Dim lastname As String = String.Empty
            Dim myParcelOrTaxID = String.Empty

            Dim cmd As OleDbCommand = New OleDbCommand()

            Dim result As Dictionary(Of String, String) = New Dictionary(Of String, String)

            'search database
            Using conn As New OleDbConnection(myUtils.ConnectString)
                'Check to make sure taxID is not null before doing query
                If (Not (String.IsNullOrEmpty(parcelOrTaxID.Trim()))) Then

                    'SETUP YOUR SQL QUERY HERE
                    SQL = "SELECT TOP 1 TaxIDNumber, APN, OWNER_NAME_1 FROM genii_user.TR WHERE TaxIDNumber = '" + parcelOrTaxID + "' ;"

                    cmd = New OleDbCommand(SQL)
                    ' cmd.Parameters.AddWithValue("@TaxIDNumber", parcelOrTaxID)
                    cmd.Connection = conn
                    conn.Open()

                    Dim reader As OleDbDataReader = cmd.ExecuteReader()

                    While (reader.Read())
                        Dim resultString As StringBuilder = New StringBuilder()

                        'ParcelOrTaxID
                        If (Not (Convert.IsDBNull(reader.GetValue(0)))) Then
                            myParcelOrTaxID = reader.GetValue(0).ToString()
                            ' resultString.AppendFormat("{0}, ", myParcelOrTaxID)
                        End If

                        'APN
                        If (Not (Convert.IsDBNull(reader.GetValue(1)))) Then
                            lastname = reader.GetValue(2).ToString()
                            resultString.AppendFormat("{0}, ", lastname)
                        End If

                        result.Add(myParcelOrTaxID, resultString.ToString())
                    End While

                End If
            End Using

            Return result
        End Function

        <System.Web.Services.WebMethod()> _
        Public Shared Function GetNameID(ByVal NameID As String) As Dictionary(Of String, String)
            Dim myUtils As Utilities = New Utilities()

            Dim SQL As String = String.Empty
            Dim TaxIDNumber As String = String.Empty
            Dim lastName = String.Empty

            Dim cmd As OleDbCommand = New OleDbCommand()

            Dim result As Dictionary(Of String, String) = New Dictionary(Of String, String)

            'search database
            Using conn As New OleDbConnection(myUtils.ConnectString)
                'Check to make sure taxID is not null before doing query
                If (Not (String.IsNullOrEmpty(NameID.Trim()))) Then

                    'SETUP YOUR SQL QUERY HERE
                    SQL = "SELECT top 1 APN, MAX(OWNER_NAME_1) AS OWNER_NAME_1 " +
                              "FROM genii_user.TR " +
                              "WHERE OWNER_NAME_1 LIKE ? " +
                              "GROUP BY APN"

                    '                              "AND APN IS NOT NULL " +

                    cmd = New OleDbCommand(SQL)
                    cmd.Parameters.AddWithValue("@OWNER_NAME_1", "%" + NameID + "%")
                    cmd.Connection = conn
                    conn.Open()

                    Dim reader As OleDbDataReader = cmd.ExecuteReader()

                    While (reader.Read())
                        Dim resultString As StringBuilder = New StringBuilder()

                        'TaxIDNumber
                        If (Not (Convert.IsDBNull(reader.GetValue(0)))) Then
                            TaxIDNumber = reader.GetValue(0).ToString()
                            ' resultString.AppendFormat("{0}, ", TaxIDNumber)
                            '   txtTaxIDSearch.text = TaxIDNumber
                        End If

                        'APN
                        If (Not (Convert.IsDBNull(reader.GetValue(1)))) Then
                            lastName = reader.GetValue(1).ToString()
                            resultString.AppendFormat("{0} ", lastName)
                        End If
                        resultString.AppendFormat("{0}", lastName)
                        result.Add(TaxIDNumber.ToString(), resultString.ToString())
                    End While

                End If
            End Using

            Return result
        End Function

        <System.Web.Services.WebMethod()> _
        Public Shared Function GetTaxRollNumber(ByVal TaxRollNumber As String, ByVal taxYear As String) As Dictionary(Of String, String)
            Dim myUtils As Utilities = New Utilities()

            Dim SQL As String = String.Empty
            Dim TaxIDNumber As String = String.Empty
            Dim lastName = String.Empty

            Dim cmd As OleDbCommand = New OleDbCommand()

            Dim result As Dictionary(Of String, String) = New Dictionary(Of String, String)

            'search database
            Using conn As New OleDbConnection(myUtils.ConnectString)
                'Check to make sure taxID is not null before doing query
                If (Not (String.IsNullOrEmpty(TaxRollNumber.Trim()))) Then

                    'SETUP YOUR SQL QUERY HERE
                    SQL = "SELECT TaxIDNumber, TaxRollNumber, OWNER_NAME_1 " +
                              "FROM genii_user.TR " +
                              "WHERE TaxRollNumber = ?  and taxyear= " + taxYear + " " +
                              "GROUP BY TaxIDNumber,TaxRollNumber,OWNER_NAME_1"

                    '                              "AND APN IS NOT NULL " +

                    cmd = New OleDbCommand(SQL)
                    cmd.Parameters.AddWithValue("@TaxRollNumber", TaxRollNumber)
                    '  cmd.Parameters.AddWithValue("@taxyear", taxYear)
                    cmd.Connection = conn
                    conn.Open()

                    Dim reader As OleDbDataReader = cmd.ExecuteReader()

                    While (reader.Read())
                        Dim resultString As StringBuilder = New StringBuilder()

                        'TaxIDNumber
                        If (Not (Convert.IsDBNull(reader.GetValue(0)))) Then
                            TaxIDNumber = reader.GetValue(0).ToString()
                            ' resultString.AppendFormat("{0}, ", TaxIDNumber)
                            '   txtTaxIDSearch.text = TaxIDNumber
                        End If

                        'APN
                        If (Not (Convert.IsDBNull(reader.GetValue(1)))) Then
                            lastName = reader.GetValue(2).ToString()
                            resultString.AppendFormat("{0} ", lastName)
                        End If
                        ' resultString.AppendFormat("{0}", lastName)
                        result.Add(TaxIDNumber.ToString(), resultString.ToString())
                    End While

                End If
            End Using

            Return result
        End Function


    End Class

End Namespace
