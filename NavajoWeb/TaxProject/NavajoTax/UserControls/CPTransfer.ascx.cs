using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Drawing.Printing;
using System.Drawing;

public partial class UserControls_CPTransfer : System.Web.UI.UserControl
{
    int mInvestorID = 0;
    Utilities util = new Utilities();
    List<SelectedCP> selectedCPCollection = new List<SelectedCP>();
    private string _CPTaxYear;
    private string _CPTaxRoll;
    private string _CPTaxID;

    public int InvestorID
    {
        get
        {
            return mInvestorID;
        }
        set
        {
            mInvestorID = value;
        }
    }

    public class SelectedCP
    {
        public string APN = string.Empty;
        public string CertificateNumber = string.Empty;
        public string Fee = string.Empty;
        public string TaxYear = string.Empty;
        public string TaxRollNumber = string.Empty;
    }

    protected void btnCurrentSSANSearch_Click(object sender, EventArgs e)
    {
        // Get investor info
        DataSet ds = new DataSet();
        DataRow dr;
        string investorName = string.Empty;
        PrepareControls();
        LoadLoginInfo();

        try
        {
            if (!String.IsNullOrEmpty(txtCurrentSSAN.Text))
            {
                InvestorID = Convert.ToInt32(txtCurrentSSAN.Text);
            }

            // Get Investor info 
            ds = GetInvestorInfo(InvestorID);

            if (ds.Tables[0].Rows.Count > 0)
            {
                dr = ds.Tables[0].Rows[0];

                string firstName = dr["FirstName"] == DBNull.Value ? String.Empty : dr["FirstName"].ToString();
                string middleName = dr["MiddleName"] == DBNull.Value ? String.Empty : dr["MiddleName"].ToString();
                string lastName = dr["LastName"] == DBNull.Value ? String.Empty : dr["LastName"].ToString();

                investorName = firstName + " " + middleName + " " + lastName;

                lblCurrentInvestor.Text = investorName.Trim();
               // txtPayorName.Text = investorName.Trim();


                // Load Investor CP Grid
                LoadInvestorCPGrid(InvestorID);
            }

            if (txtCurrentSSAN.Text == txtNewSSAN.Text)
            {
                // We need to let users know they cannot have current investor and new investor the same
                Control Caller = this;
                ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPTransfer), "Investor's are the same", "showMessage('Current Investor and New Investor cannot be the same.', 'Investor Error');", true);

                txtCurrentSSAN.Text = string.Empty;
                lblCurrentInvestor.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /*  Private Sub PrepareControls()
         ' Payment types.
         Using adt As New OleDbDataAdapter("SELECT PaymentTypeCode, PaymentDescription FROM genii_user.ST_PAYMENT_INSTRUMENT WHERE SHOW_CASHIER = 1", Me.ConnectString)
             adt.SelectCommand.Connection.Open()

             Dim rdr As OleDbDataReader = adt.SelectCommand.ExecuteReader()

             While rdr.Read()
                 Me.ddlPaymentType.Items.Add(New ListItem(rdr.Item("PaymentDescription").ToString(), rdr.Item("PaymentTypeCode")))
             End While
         End Using
     End Sub
 */






  /*  Private Sub LoadLoginInfo()
        ' Look for existing session.
        Dim userName As String = util.CurrentUserName
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
                Me.lblOperatorName.Text = userName
                Me.lblCurrentDate.Text = Date.Today.ToShortDateString()
                Me.lblLoginTime.Text = loginTime.ToString("g")
                Me.lblStartCash.Text = startCash.ToString("C")
                Me.lblLogoutUsername.Text = userName

                CashierRecordIDSessionID = dt.Rows(0)("RECORD_ID")
                Me.lblSessionID.Text = dt.Rows(0)("RECORD_ID")

                ' Pending payments tab
                ''Me.lblPendCashier.Text = userName
                '' Me.lblPendLogin.Text = loginTime.ToString()
            End If
        End Using

    End Sub
   * 
   * */

    private void LoadLoginInfo()
    {
        string currentSessionID = string.Empty;

        string SQL = string.Format("SELECT record_id FROM genii_user.CASHIER_SESSION WHERE CASHIER = '{0}' AND END_TIME IS NULL ORDER BY START_TIME DESC", System.Web.HttpContext.Current.User.Identity.Name);

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            currentSessionID = Convert.ToString(cmd.ExecuteScalar());
            lblSessionID.Text = currentSessionID;
        }

       // return feeAmount;
    }
    
    public void PrepareControls()
        {
        DataSet ds = new DataSet();

        util.LoadTable(ds, "ST_PAYMENT_INSTRUMENT", "SELECT PaymentTypeCode, PaymentDescription FROM genii_user.ST_PAYMENT_INSTRUMENT WHERE SHOW_CASHIER = 1");
          //  ddlPaymentType .Items.Add(new ListItem("Select","0"));
            for(int i = 0; i < ds.Tables["ST_PAYMENT_INSTRUMENT"].Rows.Count; i++)
            {
                ddlPaymentType.Items.Add(new ListItem(Convert.ToString(ds.Tables["ST_PAYMENT_INSTRUMENT"].Rows[i][1]).Trim(), Convert.ToString(ds.Tables["ST_PAYMENT_INSTRUMENT"].Rows[i][0]).Trim()));
            }

      //      Object dsData=ds.Tables["ST_PAYMENT_INSTRUMENT"].Rows[0]["PaymentDescription"];
       // return ds;

        }

    protected void btnNewSSANSearch_Click(object sender, EventArgs e)
    {
        DataSet ds = new DataSet();
        DataRow dr;
        string newInvestorName = string.Empty;
        int newInvestorID = 0;

        try
        {
            if (!String.IsNullOrEmpty(txtNewSSAN.Text))
            {
                newInvestorID = Convert.ToInt32(txtNewSSAN.Text);
            }

            // Get Investor info 
            ds = GetInvestorInfo(newInvestorID);

            if (ds.Tables[0].Rows.Count > 0)
            {
                dr = ds.Tables[0].Rows[0];

                string firstName = dr["FirstName"] == DBNull.Value ? String.Empty : dr["FirstName"].ToString();
                string middleName = dr["MiddleName"] == DBNull.Value ? String.Empty : dr["MiddleName"].ToString();
                string lastName = dr["LastName"] == DBNull.Value ? String.Empty : dr["LastName"].ToString();

                newInvestorName = firstName + " " + middleName + " " + lastName;

                lblNewInvestor.Text = newInvestorName.Trim();
                txtPayorName.Text = newInvestorName.Trim();
            }

            if (txtCurrentSSAN.Text == txtNewSSAN.Text)
            {
                // We need to let users know they cannot have current investor and new investor the same
                Control Caller = this;
                ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPTransfer), "Investor's are the same", "showMessage('Current Investor and New Investor cannot be the same.', 'Investor Error');", true);

                txtNewSSAN.Text = string.Empty;
                lblNewInvestor.Text = string.Empty;
                txtPayorName.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }


    protected void btnCommit_Click(object sender, EventArgs e)
    {
        int transID = 0;
        string paymentType = ddlPaymentType.SelectedValue;
        int groupKey;

        string SQL1 = String.Format("select isnull(max(group_key),0)+1  as group_key from genii_user.cashier_transactions ");

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL1, conn);

            groupKey = Convert.ToInt32(cmd.ExecuteScalar());

        }


        if ((paymentType == "1") || (paymentType == "3"))
        {
            if (txtCheckNumber.Text == string.Empty)
            {
                Control Caller = this;
                ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPTransfer), "Check Number", "showMessage('Check Number cannot be null', 'Check Number');", true);
                return;
            }
        }

        if ((!string.IsNullOrEmpty(txtCurrentSSAN.Text)) && (!string.IsNullOrEmpty(txtNewSSAN.Text)) && (!string.IsNullOrEmpty(lblNewInvestor.Text)))
        {
            try
            {
                getSelectedCPs();

                LoadLoginInfo();

                foreach (SelectedCP selectedCPItem in selectedCPCollection)
                {
                    _CPTaxRoll =selectedCPItem.TaxRollNumber;
                    _CPTaxYear = selectedCPItem.TaxYear;
                    PreparePrintDocument();

                    // Insert records into CASHIER_TRANSACTION
                    transID = createCashierTransactionRecord(selectedCPItem.TaxYear,
                                                             selectedCPItem.TaxRollNumber,
                                                             Convert.ToDecimal(selectedCPItem.Fee), groupKey);

                    // Insert records into CASHIER_APPORTIONMENT
                    createCashierApportionRecord(transID, selectedCPItem.TaxRollNumber, selectedCPItem.TaxYear, Convert.ToDecimal(selectedCPItem.Fee));

                    //   create_GET_APPORTION_Record(transID, selectedCPItem.TaxYear, selectedCPItem.TaxRollNumber, Convert.ToDouble(selectedCPItem.Fee));

                    // Update records in TR_CP_OWNER
                    updateTR_CP_OWNER_Table(txtCurrentSSAN.Text, txtNewSSAN.Text, selectedCPItem.CertificateNumber);

                    // Update records in TR_CP
                    updateTR_CP_Table(selectedCPItem.CertificateNumber,
                                      txtCurrentSSAN.Text,
                                      txtNewSSAN.Text);
                }                

                // Load Investor CP Grid
                if (!String.IsNullOrEmpty(txtCurrentSSAN.Text))
                {
                    InvestorID = Convert.ToInt32(txtCurrentSSAN.Text);
                }

                LoadInvestorCPGrid(InvestorID);

                // let users know transfer was successful
                Control Caller = this;
                ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPTransfer), "CP Transfer Complete", "showMessage('CP Transfer completed successfully', 'CP Transfer Complete');", true);
            }
            catch (Exception ex)
            {
                // let users know there was an error during transfer
                Control Caller = this;
                ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPTransfer), "CP Transfer Error", "showMessage('There was an error during the CP Transfer process.', 'CP Transfer Error');", true);
            }

        }
        else
        {
            // We need to let users know they cannot have current investor and new investor the same
            Control Caller = this;
            ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPTransfer), "Data is missing", "showMessage('Please select current and new Investors before continuing', 'Investor Error');", true);
        }
    }




    public DataSet GetInvestorInfo(int InvestorID)
    {
        DataSet ds = new DataSet();

        util.LoadTable(ds, "ST_INVESTOR", "SELECT * FROM genii_user.ST_INVESTOR WHERE InvestorID = " + InvestorID);

        return ds;
    }


    public void LoadInvestorCPGrid(int InvestorID)
    {
        DataSet ds = new DataSet();
        string SQL = string.Empty;
        double transferFee = 0.0;

        // GET TRANSFER FEE FROM DB PARAMETER 
        transferFee = getTransferFeeFromDB(DateTime.Today.Year -1);

        //SQL = string.Format("SELECT APN, CertificateNumber, {0} AS FEE , TaxYear, TaxRollNumber FROM genii_user.TR_CP WHERE InvestorID = {1} ORDER BY TaxYear DESC", transferFee, InvestorID);
        SQL = string.Format("SELECT DISTINCT(CertificateNumber), " +
                            "APN, " +
                            "{0} AS Fee, " +
                            "MIN(TaxYear) AS TaxYear, " +
                            "MIN(TaxRollNumber) AS TaxRollNumber " +
                            "FROM genii_user.TR_CP " +
                            "WHERE InvestorID = {1} " +
                            "AND DateCPReassigned is NULL " +
                            "AND CP_STATUS IN (1,3,4) " +
                            "GROUP BY CertificateNumber, APN " +
                            "ORDER BY CertificateNumber ", transferFee, InvestorID);


        util.LoadTable(ds, "TR_CP", SQL);

        if (ds.Tables[0].Rows.Count > 0)
        {            
            gvInvestorOwnedCPs.DataSource = ds;
            gvInvestorOwnedCPs.DataBind();
        }
    }


    private double getTransferFeeFromDB(int taxYear)
    {
        double feeAmount = 0.0;
        string SQL = string.Empty;

        SQL = string.Format("SELECT DefaultAmount " +
                            "FROM genii_user.tblTaxSystemCalculation " +
                            "WHERE WhenToUse = 'SecCpReassignInvestorToInvestor' " +
                            "AND TaxChargeCodeID = 99940 " +
                            "AND TaxYear = {0} ", taxYear);

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            OleDbCommand cmd = new OleDbCommand();

            cmd.CommandText = SQL;

            cmd.Connection = conn;

            conn.Open();

            feeAmount = Convert.ToDouble(cmd.ExecuteScalar());
        }

        return feeAmount;
    }


    private void getSelectedCPs()
    {
        try
        {
            // Get APN, Certificate Number, TaxYear and TaxRollNumber from grid
            foreach (GridViewRow gvRow in gvInvestorOwnedCPs.Rows)
            {
                if (((CheckBox)gvRow.FindControl("cbSelectItem")).Checked)
                {
                    SelectedCP selectedCPItem = new SelectedCP();

                    // APN
                    selectedCPItem.APN = gvRow.Cells[1].Text;

                    // CERTIFICATE NUMBER
                    selectedCPItem.CertificateNumber = gvRow.Cells[2].Text;

                    // FEE
                    selectedCPItem.Fee = gvRow.Cells[3].Text.Replace("$", "");

                    // TAX YEAR
                    selectedCPItem.TaxYear = gvRow.Cells[4].Text;

                    // TAX ROLL NUMBER
                    selectedCPItem.TaxRollNumber = gvRow.Cells[5].Text;

                    selectedCPCollection.Add(selectedCPItem);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    protected void PreparePrintDocument()
    {
        try
        {
            try
            {
                PrintDocument Document = new PrintDocument();
                Document.PrintPage += new PrintPageEventHandler(printDocument1_PrintHeader);
                Document.PrintPage += new PrintPageEventHandler(printDocument1_PrintPage);
                Document.Print();

            }
            finally
            {
               
            }


        }
        catch (Exception ex)
        {
            //   MessageBox.Show(ex.Message);
        }
    }

    private void printDocument1_PrintHeader(object sender, System.Drawing.Printing.PrintPageEventArgs e)
    {
        string sql = string.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='SIGNATURE_BLOCK_TITLE' ");
        string sigBlockTitle = string.Empty;

        using (OleDbDataAdapter adt = new OleDbDataAdapter(sql, util.ConnectString))
        {
            DataTable tblReceiptDetails = new DataTable();

            adt.Fill(tblReceiptDetails);

            if (tblReceiptDetails.Rows.Count > 0)
            {
                DataView dv = new DataView(tblReceiptDetails);
                if (!DBNull.Value.Equals(dv[0]["parameter"]))
                {
                    sigBlockTitle = (dv[0]["parameter"]).ToString();
                }
                else
                {
                    sigBlockTitle = "N/A";
                }
            }
        }

        string sql2 = string.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='SIGNATURE_BLOCK_NAME' ");
        string sigBlockName = string.Empty;

        using (OleDbDataAdapter adt = new OleDbDataAdapter(sql2, util.ConnectString))
        {
            DataTable tblReceiptDetails = new DataTable();

            adt.Fill(tblReceiptDetails);

            if (tblReceiptDetails.Rows.Count > 0)
            {
                DataView dv = new DataView(tblReceiptDetails);
                if (!DBNull.Value.Equals(dv[0]["parameter"]))
                {
                    sigBlockName = (dv[0]["parameter"]).ToString();
                }
                else
                {
                    sigBlockName = "N/A";
                }
            }
        }

        string sql3 = string.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='ADDRESS' ");
        string sigBlockAddress = string.Empty;

        using (OleDbDataAdapter adt = new OleDbDataAdapter(sql3, util.ConnectString))
        {
            DataTable tblReceiptDetails = new DataTable();

            adt.Fill(tblReceiptDetails);

            if (tblReceiptDetails.Rows.Count > 0)
            {
                DataView dv = new DataView(tblReceiptDetails);
                if (!DBNull.Value.Equals(dv[0]["parameter"]))
                {
                    sigBlockAddress = (dv[0]["parameter"]).ToString();
                }
                else
                {
                    sigBlockAddress = "N/A";
                }
            }
        }

        string sql4 = string.Format("select parameter from genii_user.ST_PARAMETER where parameter_name='CITY_STATE_ZIP' ");
        string sigBlockCityStateZip = string.Empty;

        using (OleDbDataAdapter adt = new OleDbDataAdapter(sql4, util.ConnectString))
        {
            DataTable tblReceiptDetails = new DataTable();

            adt.Fill(tblReceiptDetails);

            if (tblReceiptDetails.Rows.Count > 0)
            {
                DataView dv = new DataView(tblReceiptDetails);
                if (!DBNull.Value.Equals(dv[0]["parameter"]))
                {
                    sigBlockCityStateZip = (dv[0]["parameter"]).ToString();
                }
                else
                {
                    sigBlockCityStateZip = "N/A";
                }
            }
        }

        Font printFont9B = new Font("Arial", 9, FontStyle.Bold);
        Font printFont9R = new Font("Arial", 9, FontStyle.Regular);

        Rectangle rect1 = new Rectangle(10, 10, 270, 250);
        Rectangle rect2 = new Rectangle(10, 100, 270, 250);
        Rectangle rect3 = new Rectangle(10, 150, 270, 250);
        Rectangle rect4 = new Rectangle(10, 170, 270, 250);
        Rectangle rect5 = new Rectangle(10, 200, 270, 250);
        Rectangle rect6 = new Rectangle(10, 250, 270, 250);

        StringFormat stringFormat = new StringFormat();
        stringFormat.Alignment = StringAlignment.Center;
        stringFormat.LineAlignment = StringAlignment.Center;

        StringFormat stringFormatNear = new StringFormat();
        stringFormatNear.Alignment = StringAlignment.Near;
        stringFormatNear.LineAlignment = StringAlignment.Near;


        string a = string.Empty;

        string[] defaultHeader = { "-----------------------------------------------------", "Operator - " + System.Web.HttpContext.Current.User.Identity.Name, (DateTime.Today).ToString(), sigBlockCityStateZip, sigBlockAddress, sigBlockName, sigBlockTitle, "-----------------------------------------------------" };

        a = string.Empty;
        for (int i = 0; i < defaultHeader.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(defaultHeader[i] + a, printFont9B, Brushes.Black, rect1, stringFormat);
        }

    }

    private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
    {
        decimal taxes = 0;
        decimal totalInterest = 0;
        decimal totalFees = 0;
      //  int transferFees = 0;
        string certNumber = string.Empty;
        string dateOfSale = string.Empty;
        string bidRate = string.Empty;
        decimal totalAmount = 0;
        string APN = string.Empty;

       double transferFee = 0.0;

        // GET TRANSFER FEE FROM DB PARAMETER 
        transferFee = getTransferFeeFromDB(DateTime.Today.Year -1);

        decimal totalInvestment = 0;
        string sql = string.Format("SELECT DISTINCT CertificateNumber, " +
                                " APN, InvestorID,  " +
                                " '{0}' AS Fee,  " +
                                " MIN(TaxYear) AS TaxYear,  " +
                                " MIN(TaxRollNumber) AS TaxRollNumber  " +
                                " FROM genii_user.TR_CP  " +
                                " WHERE TaxRollNumber = '{1}' and TaxYear='{2}' " +
                                " AND DateCPReassigned is NULL  " +
                                " AND CP_STATUS IN (1,3,4)  " +
                                " GROUP BY CertificateNumber, APN, investorID " +
                                " ORDER BY CertificateNumber",transferFee, _CPTaxRoll, _CPTaxYear);

        using (OleDbDataAdapter adt = new OleDbDataAdapter(sql, util.ConnectString))
        {
            DataTable tblReceiptDetails = new DataTable();

            adt.Fill(tblReceiptDetails);

            if (tblReceiptDetails.Rows.Count > 0)
            {
                DataView dv = new DataView(tblReceiptDetails);

                if (!DBNull.Value.Equals(dv[0]["APN"]))
                {
                    APN = (dv[0]["APN"]).ToString();
                }
                else
                {
                    APN = "N/A";
                }

                if (!DBNull.Value.Equals(dv[0]["CertificateNumber"]))
                {
                    certNumber = (dv[0]["CertificateNumber"]).ToString();
                }
                else
                {
                    certNumber = "N/A";
                }

            }
        }

        // totalAmount = taxes + totalInterest + totalFees + transferFees;

        // totalFees = totalFees + transferFees;
        totalInvestment = totalInterest + taxes;

        Font printFont9B = new Font("Arial", 9, FontStyle.Bold);
        Font printFont9R = new Font("Arial", 9, FontStyle.Regular);

        Rectangle rect1 = new Rectangle(10, 10, 270, 250);
        Rectangle rect2 = new Rectangle(10, 50, 270, 250);
        Rectangle rect3 = new Rectangle(10, 100, 270, 250);
        Rectangle rect4 = new Rectangle(10, 150, 270, 250);
        Rectangle rect5 = new Rectangle(10, 180, 270, 250);
        Rectangle rect5b = new Rectangle(10, 220, 270, 250);
        Rectangle rect6 = new Rectangle(10, 220, 270, 250);

        StringFormat stringFormat = new StringFormat();
        stringFormat.Alignment = StringAlignment.Center;
        stringFormat.LineAlignment = StringAlignment.Center;

        StringFormat stringFormatNear = new StringFormat();
        stringFormatNear.Alignment = StringAlignment.Near;
        stringFormatNear.LineAlignment = StringAlignment.Center;


        string a = string.Empty;

        string[] paymentDetails = { "Between Investors", "Receipt for CP" };

        a = string.Empty;
        for (int i = 0; i < paymentDetails.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentDetails[i] + a, printFont9R, Brushes.Black, rect2, stringFormat);
        }

        string[] paymentDetails1 = { "Certificate of Purchase: " + certNumber, "Purchasing Party: " + txtNewSSAN.Text + " - " +  lblNewInvestor .Text ,"Selling Party: " + txtCurrentSSAN.Text  + " - " + lblCurrentInvestor.Text };

        a = string.Empty;
        for (int i = 0; i < paymentDetails1.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentDetails1[i] + a, printFont9R, Brushes.Black, rect3, stringFormat);
        }

        string[] paymentReceipt1 = { "- - -", "Total Paid: " + "             " + "             " + "$" + transferFee };

        a = string.Empty;
        for (int i = 0; i < paymentReceipt1.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentReceipt1[i] + a, printFont9R, Brushes.Black, rect4, stringFormatNear);
        }


        ////string b = string.Empty;
        ////string[] paymentReceipt1 = { "- - -", "Total Paid: " + "          " + "$" + totalAmount, "Investor Fee: " + "          " + "$" + totalFees + transferFees, "Investment: " + "          " + "$" + taxes + totalInterest };
        ////for (int i = 0; i < paymentReceipt1.Length; i++)
        ////{
        ////    b = b + Environment.NewLine + Environment.NewLine + Environment.NewLine;
        ////    e.Graphics.DrawString(paymentReceipt1[i] + b, printFont9R, Brushes.Black, rect6, stringFormatNear);
        ////}


    }


    /// <summary>
    /// createCashierTransactionRecord - Create new record in CASHIER_TRANSACTIONS Table
    /// </summary>
    /// <param name="taxYear"></param>
    /// <param name="taxRollNumber"></param>
    /// <param name="paymentAmount"></param>
    private int createCashierTransactionRecord(string taxYear, string taxRollNumber, decimal paymentAmount, int groupKey)
    {
        try
        {
            int recordID = util.GetNewID("Record_ID", "CASHIER_TRANSACTIONS");
            string sessionID = lblSessionID.Text;
            DateTime currentDate = DateTime.Today;
            string paymentType = ddlPaymentType.SelectedValue;
            string payorName = this.lblNewInvestor.Text;
            string currentUserName = System.Web.HttpContext.Current.User.Identity.Name;

            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    // Insert new record into CASHIER_TRANSACTIONS.
                    OleDbCommand cmdNewRec = new OleDbCommand("INSERT INTO genii_user.CASHIER_TRANSACTIONS " +
                                                              "(RECORD_ID, " +
                                                              "SESSION_ID, " +
                                                              "GROUP_KEY, " +
                                                              "TAX_YEAR, " +
                                                              "TAX_ROLL_NUMBER, " +
                                                              "PAYMENT_DATE, " +
                                                              "PAYMENT_TYPE, " +
                                                              "APPLY_TO, " +
                                                              "PAYOR_NAME, " +
                                                              "CHECK_NUMBER, " +
                                                              "PAYMENT_AMT, " +
                                                              "TAX_AMT, " +
                                                              "TRANSACTION_STATUS, " +
                                                              "EDIT_USER, " +
                                                              "EDIT_DATE, " +
                                                              "CREATE_USER, " +
                                                              "CREATE_DATE ) " +
                                                              "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?) ");

                    cmdNewRec.Connection = conn;
                    cmdNewRec.Transaction = trans;

                    string checkNumber;
                    if (this.txtCheckNumber.Text == "")
                    {
                        checkNumber = string.Empty;
                    }
                    else
                    {
                        checkNumber = this.txtCheckNumber.Text;
                    }

                    string isApportioned = "1";

                    // Set Parameter Values
                    cmdNewRec.Parameters.AddWithValue("@RECORD_ID", recordID);
                    cmdNewRec.Parameters.AddWithValue("@SESSION_ID", sessionID);
                    cmdNewRec.Parameters.AddWithValue("@GROUP_KEY", groupKey);
                    cmdNewRec.Parameters.AddWithValue("@TAX_YEAR", taxYear);
                    cmdNewRec.Parameters.AddWithValue("@TAX_ROLL_NUMBER", taxRollNumber);
                    cmdNewRec.Parameters.AddWithValue("@PAYMENT_DATE", currentDate);
                    cmdNewRec.Parameters.AddWithValue("@PAYMENT_TYPE", paymentType);
                    cmdNewRec.Parameters.AddWithValue("@APPLY_TO", 5);
                    cmdNewRec.Parameters.AddWithValue("@PAYOR_NAME", payorName);
                    cmdNewRec.Parameters.AddWithValue("@CHECK_NUMBER", checkNumber);
                    cmdNewRec.Parameters.AddWithValue("@PAYMENT_AMT", paymentAmount);
                    cmdNewRec.Parameters.AddWithValue("@TAX_AMT", paymentAmount);
                    cmdNewRec.Parameters.AddWithValue("@TRANSACTION_STATUS", isApportioned);
                    cmdNewRec.Parameters.AddWithValue("@EDIT_USER", currentUserName);
                    cmdNewRec.Parameters.AddWithValue("@EDIT_DATE", currentDate);
                    cmdNewRec.Parameters.AddWithValue("@CREATE_USER", currentUserName);
                    cmdNewRec.Parameters.AddWithValue("@CREATE_DATE", currentDate);

                    cmdNewRec.ExecuteNonQuery();

                    trans.Commit();

                    conn.Close();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw new Exception(ex.Message);
                }
            }
            return recordID;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }


    private void create_GET_APPORTION_Record(int transID, string taxYear, string taxRollNumber,double totalAmount)
    {

        try
        {
            //  int recordID = util.GetNewID("Record_ID", "TR_CHARGES");
            string sessionID = lblSessionID.Text;
            DateTime currentDate = DateTime.Today;
            string paymentType = ddlPaymentType.SelectedValue;
            string payorName = this.lblNewInvestor.Text;
            string currentUserName = System.Web.HttpContext.Current.User.Identity.Name;

            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();
                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);
                // OleDbTransaction trans2 = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {

                    string SQL3 = string.Format("INSERT INTO GENII_USER.CASHIER_APPORTION(TAXYEAR,TAXROLLNUMBER,AREACODE,TAXCHARGECODEID,TAXTYPEID,PAYMENTDATE,GLACCOUNT,SENTTOOTHERSYSTEM,RECEIPTNUMBER, " +
                                                " DATEAPPORTIONED,DOLLARAMOUNT)SELECT TAXYEAR,TAXROLLNUMBER,AREACODE,TAXCHARGECODEID,TAXTYPEID,PAYMENTDATE,GLACCOUNT,SENTTOOTHERSYSTEM,RECEIPTNUMBER, " +
                                                " DATEAPPORTIONED,DOLLARAMOUNT FROM dbo.GetApportionment(?,?,?,?)");

                    OleDbCommand cmdGetRecord = new OleDbCommand(SQL3, conn);
                    cmdGetRecord.Parameters.AddWithValue("@TaxYear", taxYear);
                    cmdGetRecord.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber);
                    cmdGetRecord.Parameters.AddWithValue("@PaymentAmount", totalAmount);
                    cmdGetRecord.Parameters.AddWithValue("@PaymentDate", currentDate);
                    // cmdGetRecord.Connection = conn;
                    cmdGetRecord.Transaction = trans;
                    cmdGetRecord.ExecuteNonQuery();



                    string SQL = string.Format("UPDATE genii_user.CASHIER_APPORTION " +
                                        "SET TRANS_ID = {0}, " +
                                        "EDIT_USER = '{1}', " +
                                        "EDIT_DATE = '{2}', " +
                                        "CREATE_USER = '{3}', " +
                                        "CREATE_DATE = '{4}' " +
                                        "WHERE taxrollnumber = '{5}' " +
                                        "AND taxyear = '{6}' ",
                                        transID,
                                        currentUserName,
                                        currentDate,
                                        currentUserName,
                                        currentDate,
                                        taxRollNumber,
                                        taxYear);

                    OleDbCommand cmdUpdateRecord = new OleDbCommand(SQL);

                    cmdUpdateRecord.Connection = conn;
                    cmdUpdateRecord.Transaction = trans;

                    cmdUpdateRecord.ExecuteNonQuery();

                    trans.Commit();

                    conn.Close();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw new Exception(ex.Message);
                }
            }
            // return recordID;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }



    }


    /// <summary>
    /// createCashierApportionmentRecord - Create new record in CASHIER_APPORTION Table
    /// </summary>
    /// <param name="transID">TransactionID (RecordID from CASHIER_TRANSACTIONS)</param>
    private void createCashierApportionRecord(int transID, string taxRollNumber, string taxYear, decimal dollarAmount)
    {
        int taxChargeCode = 99940;
        DateTime currDate = DateTime.Today;
        string currUser = System.Web.HttpContext.Current.User.Identity.Name;
        string SQL = string.Empty;
        int recordID = 0;
        int taxTypeID = 75;
        string GLAccount = "N00100547180";
        string GLAccount2 = "N00100547180";

        // Add record with CP Reassignment fee (99940)
        // ONE RECORD PER TaxRollNumber and TaxYear
        // record_id, trans_id (cashier_transactions record_id), taxyear, taxrollnumber, taxchargecodeID (99940), taxtypeID (75), glaccount(??), receiptnumber(??), dollarAmount
        //recordID = util.GetNewID("RECORD_ID", "CASHIER_APPORTION");
        
        try
        {
            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    // Insert New record into TR_CP_OWNER
                    SQL = string.Format("INSERT INTO genii_user.CASHIER_APPORTION " +
                                        "(TRANS_ID, " +
                                        "TaxYear, " +
                                        "TaxRollNumber, " +
                                        "TaxChargeCodeID, " +
                                        "TaxTypeID, " +
                                        "PaymentDate, " +
                                        "GLAccount, " +
                                        "DateApportioned, " +
                                        "DollarAmount, " +
                                        "EDIT_USER, " +
                                        "EDIT_DATE, " +
                                        "CREATE_USER, " +
                                        "CREATE_DATE) " +
                                        "VALUES ( {0}, {1}, '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}','{11}','{12}' ) ",                                        
                                        transID,
                                        taxYear,
                                        taxRollNumber,
                                        taxChargeCode,
                                        taxTypeID,
                                        currDate,
                                        GLAccount,
                                        currDate,
                                        dollarAmount,
                                        currUser,
                                        currDate,
                                        currUser,
                                        currDate);


                    OleDbCommand cmdUpdateRecord = new OleDbCommand(SQL);

                    cmdUpdateRecord.Connection = conn;
                    cmdUpdateRecord.Transaction = trans;

                    cmdUpdateRecord.ExecuteNonQuery();

                    trans.Commit();

                    conn.Close();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw new Exception(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }


    /// <summary>
    /// updateTR_CP_OWNER_Table - Update record in TR_CP_OWNER Table
    /// </summary>
    private void updateTR_CP_OWNER_Table(string currInvestorID, string newInvestorID, string certificateNumber)
    {
        byte[] letterOfAgreementBytes = new byte[]{};

        if (uplCPTransferDocument.FileBytes.Count() > 0)
        {
            letterOfAgreementBytes = uplCPTransferDocument.FileBytes;
        }
        
        string fileType = util.get_GetUploadFileType(uplCPTransferDocument.FileName);
        DateTime currDate = DateTime.Today;
        string currUser = System.Web.HttpContext.Current.User.Identity.Name;
        string SQL = string.Empty;

        // 1) Update TO_DATE with date of sale - this will "close" out the old TR_CP_OWNER record
        // 2) If document was uploaded, save it to IMAGE and FILE_TYPE columns
        try
        {
            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    // Update record in TR_CP_OWNER

                    SQL = string.Format("UPDATE genii_user.TR_CP_OWNER " +
                                        "SET TO_DATE = '{0}', " +
                                        "IMAGE = '{1}', " +
                                        "FILE_TYPE = '{2}',  " +
                                        "EDIT_USER = '{3}', " +
                                        "EDIT_DATE = '{4}' " +
                                        "WHERE InvestorID = {5} " +
                                        "AND CertificateNumber = '{6}' ",
                                        currDate,
                                        letterOfAgreementBytes,
                                        fileType,
                                        currUser, 
                                        currDate, 
                                        currInvestorID,
                                        certificateNumber);

                    OleDbCommand cmdUpdateRecord = new OleDbCommand(SQL);

                    cmdUpdateRecord.Connection = conn;
                    cmdUpdateRecord.Transaction = trans;

                    cmdUpdateRecord.ExecuteNonQuery();

                    trans.Commit();

                    conn.Close();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw new Exception(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        // 3) Create new record, set CertificateNumber, InvestorID (new investor), FROM_DATE (date of sale + 1 day), 
        //    CREATE_USER, CREATE_DATE, EDIT_USER, EDIT_DATE
        try
        {
            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    // Insert New record into TR_CP_OWNER
                    SQL = string.Format("INSERT INTO genii_user.TR_CP_OWNER " +
                                        "(CertificateNumber, " +
                                        "InvestorID, " +
                                        "FROM_DATE, " +
                                        "CREATE_USER, " +
                                        "CREATE_DATE, " +
                                        "EDIT_USER, " +
                                        "EDIT_DATE) " +
                                        "VALUES ( " +
                                        "'{0}', {1}, '{2}', '{3}', '{4}', '{5}', '{6}' ) ", 
                                        certificateNumber,
                                        newInvestorID,
                                        currDate.AddDays(1),
                                        currUser,
                                        currDate,
                                        currUser,
                                        currDate);
                                       

                    OleDbCommand cmdInsertRecord = new OleDbCommand(SQL);

                    cmdInsertRecord.Connection = conn;
                    cmdInsertRecord.Transaction = trans;

                    cmdInsertRecord.ExecuteNonQuery();

                    trans.Commit();

                    conn.Close();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw new Exception(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }


    /// <summary>
    /// updateTR_CP_Table - Updated record in TR_CP Table
    /// </summary>
    private void updateTR_CP_Table(string certificateNumber, 
                                   string currInvestorID, 
                                   string newInvestorID)
    {
        // update record in TR_CP
        DateTime currDate = DateTime.Today;
        string currUser = System.Web.HttpContext.Current.User.Identity.Name;
        string SQL = string.Empty;

        try
        {
            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    // Update record in TR_CP

                    SQL = string.Format("UPDATE genii_user.TR_CP " +
                                        "SET CP_STATUS = 4, " +
                                        "DateCPReassigned = '{0}', " +
                                        "EDIT_USER = '{1}', " +
                                        "EDIT_DATE = '{2}', " +
                                        "InvestorID = {3} " +
                                        "WHERE InvestorID = {4} " +
                                        "AND CertificateNumber = '{5}' ", 
                                        currDate,
                                        currUser, 
                                        currDate,
                                        newInvestorID, 
                                        currInvestorID, 
                                        certificateNumber);


                    OleDbCommand cmdUpdateRecord = new OleDbCommand(SQL);

                    cmdUpdateRecord.Connection = conn;
                    cmdUpdateRecord.Transaction = trans;

                    cmdUpdateRecord.ExecuteNonQuery();

                    trans.Commit();

                    conn.Close();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw new Exception(ex.Message);
                }
            }
           // return recordID;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }


        // Set investorID to new Investor number on all selected rows

        // Set DateCPReassigned to current date

        // Set CP_STATUS = 4


    }

}