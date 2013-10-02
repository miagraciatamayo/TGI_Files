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
//using System.Windows.Forms;


public partial class UserControls_CPPurchaseFromState : System.Web.UI.UserControl
{
    int mInvestorID = 0;
    static  DataSet mStateOwnedCPs = new DataSet();
    static DataSet mSelectedCPs = new DataSet();

    static Utilities util = new Utilities();
    private string _CPTaxYear;
    private string _CPTaxRoll;
    private string _CPTaxID;

    private Font printFont;
   //' private StreamReader streamToPrint;

    #region Properties
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



    public static DataSet StateOwnedCPs
    {
        get 
        {
            if (mStateOwnedCPs.Tables.Count == 0)
            {
                string SQL = "SELECT * FROM genii_user.TR_CP " +
                     "WHERE InvestorID = 1 " +
                     "AND cp_status=2 " +
                    "ORDER BY TaxYear DESC";

               util.LoadTable(mStateOwnedCPs, "TR_CP", SQL);
              // Utilities.LoadTable(mStateOwnedCPs, "TR_CP", SQL);
            }

            return mStateOwnedCPs; 
        
        }
        set { mStateOwnedCPs = value; }
    }

    

    public static DataSet SelectedCPs
    {
        get
        {
            return mSelectedCPs;
        }
        set
        {
            mSelectedCPs = value;
        }
    }

    #endregion


    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            StateOwnedCPs = new DataSet();
            SelectedCPs = new DataSet();
            LoadStateOwnedCPs();
            PrepareControls();
        }
    }



    protected void btnCPFromStateSSANSearch_Click(object sender, EventArgs e)
    {
        // Get investor info
        DataSet ds = new DataSet();
        DataRow dr;
        string investorName = string.Empty;

        try
        {
            if (!String.IsNullOrEmpty(txtCPFromStateInvestorSSAN.Text))
            {
                InvestorID = Convert.ToInt32(txtCPFromStateInvestorSSAN.Text);
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

                lblCPFromStateInvestorName.Text = investorName.Trim();
                txtPayorName.Text = investorName.Trim();
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public void PrepareControls()
    {
        DataSet ds = new DataSet();

        util.LoadTable(ds, "ST_PAYMENT_INSTRUMENT", "SELECT PaymentTypeCode, PaymentDescription FROM genii_user.ST_PAYMENT_INSTRUMENT WHERE SHOW_CASHIER = 1");
        //  ddlPaymentType .Items.Add(new ListItem("Select","0"));
        for (int i = 0; i < ds.Tables["ST_PAYMENT_INSTRUMENT"].Rows.Count; i++)
        {
          //  ddlPaymentType.Items.Add(new ListItem());
            ddlPaymentType.Items.Add(new ListItem(Convert.ToString(ds.Tables["ST_PAYMENT_INSTRUMENT"].Rows[i][1]).Trim(),Convert.ToString(ds.Tables["ST_PAYMENT_INSTRUMENT"].Rows[i][0]).Trim()));
        }

        //      Object dsData=ds.Tables["ST_PAYMENT_INSTRUMENT"].Rows[0]["PaymentDescription"];
        // return ds;

    }

    protected void btnAddParcel_Click(object sender, EventArgs e)
    {
        string parcelNumber = string.Empty;

       // if (gvCPFromStateParcelGrid.DataSource = null)
      //  {
          //  gvCPFromStateParcelGrid.DataSource = null;
           // gvCPFromStateParcelGrid.DataBind();
        //}

        StateOwnedCPs = new DataSet();
        SelectedCPs = new DataSet();
        LoadStateOwnedCPs();
        PrepareControls();

        // Get DataSet of State Owned CPs
        if (!string.IsNullOrEmpty(txtCPFromStateParcelNumber.Text))
        {
            parcelNumber = txtCPFromStateParcelNumber.Text.Trim();
        }

        // Create DataTable of distinct parcels
        DataTable dtDistinctParcels = new DataView(StateOwnedCPs.Tables[0]).ToTable(true, "APN");

        string SearchExpression = string.Format("APN = '{0}'", parcelNumber);
        int taxYearCount;
        string taxYearRange;
        double parcelTotal = 0.00;
        if (dtDistinctParcels.Select(SearchExpression).Length > 0)
        {
           // if (!SelectedCPs.Tables[0] != DBNull.value)
          //  {
                foreach (DataRow dr in dtDistinctParcels.Select(SearchExpression))
                {
                    DataRow newRow = SelectedCPs.Tables[0].NewRow();

                    string APN = dr["APN"].ToString();
                    string taxes=GetCPTaxes(APN).ToString();
                   // taxes = taxes.Replace("@",System.Environment.NewLine );

                    string fees = GetCPFees(APN).ToString();
                    string transFees = GetCPtransactionFees(APN).ToString();
                    string intrst = GetCPInterest(APN).ToString();
                    string txYear = GetCPTaxYear(APN).ToString();
                    string total = GetCPTotalAmount(APN).ToString();
                    string certNum = GetCPCertificateNumber(APN).ToString();
                   // taxYearCount = GetCPTaxYear(APN);
                    taxYearRange = GetCPTaxYearRange(APN);

                    newRow["APN"] = APN;
                    newRow["TAXES"] = taxes;//Convert.ToDecimal(GetCPTaxes(APN));
                    newRow["FEES"] = fees;
                    newRow["TRANSACTIONFEES"] = transFees;
                    //GetCPCertificateNumber
                    newRow["CERTIFICATENUMBER"] = certNum;
                    newRow["INTEREST"] = intrst;
                    newRow["TAXYEAR"] = txYear;
                    newRow["TOTAL"] = total;
                    newRow["TAXYEARRANGE"] = taxYearRange;

                //    lblTaxYearCount.Text = taxYearCount.ToString();
               //     lblTaxYearRange.Text = taxYearRange;
                    double CPPurchaseTotalAmount = double.Parse(total) + double.Parse(total);
                    SelectedCPs.Tables[0].Rows.Add(newRow);


                    parcelTotal =  newRow["TOTAL"] == DBNull.Value ? 0.0 : Convert.ToDouble(newRow["TOTAL"].ToString());
                  //  GetTotalsForParcels(APN);
                    double totalCPPurchase = double.Parse(lblTotalAmountCPPurchase.Text) + parcelTotal;
                    lblTotalAmountCPPurchase.Text = totalCPPurchase.ToString();//CPPurchaseTotalAmount.ToString();
                }
            }
            else
            {
                // We need to let users know that no Parcels were found with the specified parcel number
                Control Caller = this;
                ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPPurchaseFromState), "ParcelNotFound", "showMessage('Parcel not found.', 'Not Found');", true);
            }            
      //  }
      //  else
      //  {
            // We need to let users know that no Parcels were found with the specified parcel number
      //      Control Caller = this;
      //      ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPPurchaseFromState), "ParcelNotFound", "showMessage('Parcel not found.', 'Not Found');", true);
      //  }

        // parcel 105-34-242
        gvCPFromStateParcelGrid.DataSource = SelectedCPs;
        gvCPFromStateParcelGrid.DataBind();
      
    }

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

    protected void btnCommitData_Click(object sender, EventArgs e)
    {
        int transID = 0;
        // Upon Commit do the following:
        // 1) Update TR_CP
        // 1a) Update InvestorID to new InvestorID
        // 1b) Update CP_STATUS to 3 (purchased from state) from 2 (assigned to state)
        // 1c) Update DateOfPurchase
        // 1d) Update DateCPReassigned
        // 1e) Update Purchase value: set equal to sum of tax charges, interest(99901), publication fees (99902), 
        //     CP formulation (99920), and reassignment fee (99940).
        ///    Interest is derived directly from dates of delinquency
        ///    

        string paymentType = ddlPaymentType.SelectedValue;

        if ((paymentType == "1") || (paymentType == "3"))
        {
            if (txtCheckNumber.Text == string.Empty)
            {
                Control Caller = this;
                ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPPurchaseFromState), "Check Number", "showMessage('Check Number cannot be null', 'Check Number');", true);
                return;
            }
        }

                try
                {
                    string taxRollNumber = string.Empty;
                    string taxYear = string.Empty;
                    string parcelNumber = string.Empty;
                    //   double paymentAmount = 0.0;

                    LoadLoginInfo();

                    double parcelTaxes = 0.0;
                    double parcelFees = 0.0;
                    double parcelInterest = 0.0;
                    double parcelTotal = 0.0;
                    double parcelTransactionFees = 0.0;
                    //string parcelNumber = string.Empty;


                    double groupKey;

                    string SQL1 = String.Format("select isnull(max(group_key),0)+1  as group_key from genii_user.cashier_transactions ");

                    using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
                    {
                        conn.Open();

                        OleDbCommand cmd = new OleDbCommand(SQL1, conn);

                        groupKey = Convert.ToDouble(cmd.ExecuteScalar());

                    }


                    foreach (DataRow dr in SelectedCPs.Tables[0].Rows)
                    {
                        //   parcelTaxes = dr["TAXES"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["TAXES"].ToString());
                        // parcelFees = dr["FEES"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["FEES"].ToString());
                        //    parcelTransactionFees = dr["TRANSACTIONFEES"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["TRANSACTIONFEES"].ToString());
                        //    parcelInterest = dr["INTEREST"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["INTEREST"].ToString());
                        //    parcelTotal = parcelTaxes + parcelFees + parcelTransactionFees + parcelInterest;
                        parcelNumber = dr["APN"] == DBNull.Value ? string.Empty : Convert.ToString(dr["APN"].ToString());
                        //     dr["TOTAL"] =Math.Round(parcelTotal,2);

                        // Get DataSet of State Owned CPs
                        //parcelTotal = double.Parse(GetCPTotalAmount(parcelNumber));
                        //taxYear = GetCPTaxYearParameter();
                        //taxRollNumber = GetTaxRollNumber(parcelNumber, taxYear);
                        //string certificateNumber = GetCPCertificateNumber(parcelNumber);

                        //string certNumber = GetCPFirstCertificateNumber(parcelNumber);// certificateNumber.Substring(1, 5);


                        string TaxYear = String.Empty;
                        string TaxRollNumber;
                        double TotalAmount;
                        string CertificateNumber;


                        string SQL = String.Format("select *,sum(taxes + interest + Fees + TransferFees)as total from dbo.vState_Owned_CP where APN = '{0}' and cp_status=2 " +
                                                    " group by taxyear, taxrollnumber,APN,Taxes,Interest,Fees, TransferFees, CertificateNumber,cp_status,dateOfSale order by TaxYear", parcelNumber);

                        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
                        {
                            conn.Open();

                            OleDbCommand cmd = new OleDbCommand(SQL, conn);

                            using (OleDbDataReader dataRow = cmd.ExecuteReader())
                            {
                                if (dataRow.HasRows)
                                {
                                    while (dataRow.Read())
                                    {
                                        TaxYear = Convert.ToString(dataRow["TaxYear"]);
                                        TaxRollNumber = Convert.ToString(dataRow["TaxRollNumber"]);
                                        TotalAmount = Convert.ToDouble(dataRow["Total"]);
                                        CertificateNumber = Convert.ToString(dataRow["CertificateNumber"]);

                                        _CPTaxRoll = TaxRollNumber;
                                        _CPTaxYear = TaxYear;
                                        PreparePrintDocument();

                                        Update_TR_CP_Table(Convert.ToInt32(txtCPFromStateInvestorSSAN.Text), TotalAmount, parcelNumber, TaxYear, TaxRollNumber, CertificateNumber);

                                        // 2) Update TR_CP_OWNER
                                        // 2a) Update TO_DATE with date of sale (this will "close" current owned record
                                        Update_TR_CP_OWNER_Table("1", txtCPFromStateInvestorSSAN.Text, CertificateNumber);

                                        // 3) Create new record in TR_CP_OWNER
                                        // 3a) Set CertificateNumber to Certificate of old record
                                        // 3b) Set InvestorID to new Investor
                                        // 3c) Set FROM_DATE to day following sale date

                                        //already included in "UPdate Tr CP owner" method
                                        //   create_TR_CP_OWNER_Record();

                                        // 4) Create new record in TR_CHARGES
                                        // 4a) Set TaxChargeCodeID = 99940 (CP Reassignment fee)
                                        // 4b) Set all other fields
                                        create_TR_CHARGES_Record(TaxYear, TaxRollNumber);

                                        // 6) Create new record in CASHIER_TRANSACTIONS
                                        // 6a) Create and record cashier transaction record

                                        transID = create_CASHIER_TRANSACTIONS_Record(TaxYear, TaxRollNumber, Convert.ToDecimal(TotalAmount), groupKey);
                                        // 5) Create new records in CASHIER_APPORTION
                                        // 5a) Add CP Reassignment fee (99940)
                                        // 5b) Apportion taxes to respective accounts
                                        // 5c) Add Delinquent interest (99901)
                                        // 5d) Add Publication fee (99902)
                                        // 5e) Add CP Formulation Fee (99920)
                                        // 5f) Add Treasurer's fee (99921)
                                        // 5g) Add CP Transfer fee (99940)

                                        //Removed because of excess amounts...
                                        //    create_CASHIER_APPORTION_Record(transID, TaxYear, TaxRollNumber, 10.00, TotalAmount);

                                        create_GET_APPORTION_Record(transID, TaxYear, TaxRollNumber, 10.00, TotalAmount);
                                        // 7) Create new record in TR_PAYMENTS MTA

                                        //check amount being written...
                                        create_TR_PAYMENTS_Record(TaxYear, TaxRollNumber, TotalAmount, transID);

                                        Update_TR_Table(TaxYear, TaxRollNumber);

                                    }
                                }

                            }
                        }





                    }

                    gvCPFromStateParcelGrid.DataSource = null;
                    gvCPFromStateParcelGrid.DataBind();
                    txtCPFromStateParcelNumber.Text = string.Empty;
                    lblTotalAmountCPPurchase.Text = string.Empty;
                    txtCheckNumber.Text = string.Empty;

                    Control Caller = this;
                    ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPPurchaseFromState), "CP Purchase from State Complete", "showMessage('CP Purchase from State completed successfully', 'CP Purchase from State Complete');", true);
                  //  Response.Redirect("TaxInvestors.aspx#tabInvestors"); 
                    //StateOwnedCPs = new DataSet();
                    //SelectedCPs = new DataSet();
                    //LoadStateOwnedCPs();
                    //PrepareControls();
                }

                catch (Exception ex)
                {
                    gvCPFromStateParcelGrid.DataSource = null;
                    gvCPFromStateParcelGrid.DataBind();
                    txtCPFromStateParcelNumber.Text = string.Empty;
                    lblTotalAmountCPPurchase.Text = string.Empty;
                    // let users know there was an error during transfer
                    Control Caller = this;
                    ScriptManager.RegisterStartupScript(Caller, typeof(UserControls_CPPurchaseFromState), "CP Purchase from State Error", "showMessage('There was an error during the CP Purchase from State process.', 'CP Purchase from State Error');", true);
                }

            
        
    }

    protected void PreparePrintDocument() 
    {
        try
        {
          //  streamToPrint = new StreamReader(filePath);
            try
            {
            //    printFont = new Font("Arial", 10);
                //PrintDocument pd = new PrintDocument();
                //pd.PrintPage += new PrintPageEventHandler(printDocument1_PrintPage);               
                //// Print the document.
                //pd.Print();

                PrintDocument Document = new PrintDocument();
                Document.PrintPage += new PrintPageEventHandler(printDocument1_PrintHeader);
                Document.PrintPage += new PrintPageEventHandler(printDocument1_PrintPage);
                Document.Print();

                //return pd;

            }
            finally
            {
               // streamToPrint.Close();
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

        string[] defaultHeader = { "-----------------------------------------------------", "Operator - " + System.Web.HttpContext.Current.User.Identity.Name, (DateTime.Today).ToString()  , sigBlockCityStateZip, sigBlockAddress, sigBlockName, sigBlockTitle, "-----------------------------------------------------" };

        a = string.Empty;
        for (int i = 0; i < defaultHeader.Length ;i++ )
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
        decimal transferFees = 0;
        string certNumber = string.Empty;
        string dateOfSale = string.Empty;
        string bidRate = string.Empty;
        decimal totalAmount = 0;
        string APN = string.Empty;

        decimal totalInvestment = 0;
        string sql = string.Format("select *,sum(taxes + interest + Fees + TransferFees)as total from dbo.vState_Owned_CP where TaxRollNumber = '{0}' and TaxYear= '{1}' and cp_status=2 " +
                                    " group by taxyear, taxrollnumber,APN,Taxes,Interest,Fees, TransferFees, CertificateNumber,cp_status,dateOfSale order by TaxYear", _CPTaxRoll, _CPTaxYear);

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
                    certNumber  = (dv[0]["CertificateNumber"]).ToString ();
                }
                else
                {
                    certNumber = "N/A";
                }

                if (!DBNull.Value.Equals(dv[0]["Taxes"]))
                {
                    taxes = Convert.ToDecimal(dv[0]["Taxes"]);
                }
                else
                {
                    taxes = 0;
                }

                if (!DBNull.Value.Equals(dv[0]["Interest"]))
                {
                    totalInterest = Convert.ToDecimal(dv[0]["Interest"]);
                }
                else
                {
                    totalInterest = 0;
                }

                if (!DBNull.Value.Equals(dv[0]["Fees"]))
                {
                    totalFees = Convert.ToDecimal(dv[0]["Fees"]);
                }
                else
                {
                    totalFees = 0;
                }

                if (!DBNull.Value.Equals(dv[0]["TransferFees"]))
                {
                    transferFees = Convert.ToDecimal(dv[0]["TransferFees"]);
                }
                else
                {
                    transferFees = 0;
                }

                if (!DBNull.Value.Equals(dv[0]["total"]))
                {
                    totalAmount = Convert.ToDecimal(dv[0]["total"]);
                }
                else
                {
                    totalAmount = 0;
                }

                if (!DBNull.Value.Equals(dv[0]["DateOfSale"]))
                {
                    dateOfSale   = (dv[0]["DateOfSale"]).ToString();
                }
                else
                {
                    dateOfSale =string.Empty;
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
        Rectangle rect3 = new Rectangle(10, 90, 270, 250);
        Rectangle rect4 = new Rectangle(10, 140, 270, 250);
        Rectangle rect5 = new Rectangle(10, 180, 270, 250);
        Rectangle rect5b = new Rectangle(10,220, 270, 250);
        Rectangle rect6 = new Rectangle(10, 220, 270, 400);

        StringFormat stringFormat = new StringFormat();
        stringFormat.Alignment = StringAlignment.Center;
        stringFormat.LineAlignment = StringAlignment.Center;

        StringFormat stringFormatNear = new StringFormat();
        stringFormatNear.Alignment = StringAlignment.Near;
        stringFormatNear.LineAlignment = StringAlignment.Center;

        //Dim stringFormatNear As New StringFormat()
        //stringFormatNear.Alignment = StringAlignment.Near
        //stringFormatNear.LineAlignment = StringAlignment.Center
        //Dim tabs As Single() = {100}
        //stringFormatNear.SetTabStops(0, tabs)


        string a = string.Empty;

        string[] paymentDetails = {"Purchase from State","Receipt for CP"};

        a = string.Empty;
        for (int i = 0; i < paymentDetails.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentDetails[i] + a, printFont9R, Brushes.Black, rect2, stringFormat);
        }

        string[] paymentDetails1 = { "Certificate of Purchase: " + certNumber, "Purchaser: " +  txtCPFromStateInvestorSSAN.Text  + " - " + txtPayorName.Text };

        a = string.Empty;
        for (int i = 0; i < paymentDetails1.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentDetails1[i] + a, printFont9R, Brushes.Black, rect3, stringFormat);
        }

        string[] paymentDetails2 = { "Payment applied to the " + _CPTaxYear  + " Tax Year", "Thank you for your Payment of: $" + (totalAmount) };

        a = string.Empty;
        for (int i = 0; i < paymentDetails2.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentDetails2[i] + a, printFont9R, Brushes.Black, rect4, stringFormat);
        }

        string[] paymentDetails3 = { "Parcel / Tax ID: " + APN, "Tax Roll: " + _CPTaxRoll , "Tax Year: " + _CPTaxYear  };

        a = string.Empty;
        for (int i = 0; i < paymentDetails3.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentDetails3[i] + a, printFont9R, Brushes.Black, rect5, stringFormat);
        }

        string[] paymentDetails3B = { "Rate: "  , "Original Date of Sale: " + dateOfSale };

        a = string.Empty;
        for (int i = 0; i < paymentDetails3B.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentDetails3B[i] + a, printFont9R, Brushes.Black, rect5b, stringFormat);
        }


        string[] paymentReceipt1 = { "- - -", "Total Paid: " + "             " + "$" + totalAmount, "Investor Fee: " + "          " + "$" + totalFees, "Investment: " + "            " + "$" + totalInvestment };

        a = string.Empty;
        for (int i = 0; i < paymentReceipt1.Length; i++)
        {
            a = a + Environment.NewLine + Environment.NewLine;
            e.Graphics.DrawString(paymentReceipt1[i] + a, printFont9R, Brushes.Black, rect6, stringFormatNear);
        }


        //string b = string.Empty;
        //string[] paymentReceipt1 = { "- - -", "Total Paid: " + "          " + "$" + totalAmount, "Investor Fee: " + "          " + "$" + totalFees + transferFees, "Investment: " + "          " + "$" + taxes + totalInterest };
        //for (int i = 0; i < paymentReceipt1.Length; i++)
        //{
        //    b = b + Environment.NewLine + Environment.NewLine + Environment.NewLine;
        //    e.Graphics.DrawString(paymentReceipt1[i] + b, printFont9R, Brushes.Black, rect6, stringFormatNear);
        //}


    }

    protected void btnPrintReceipt_Click(object sender, EventArgs e)
    {
        

        //addHandler print_document.PrintPageEventArgs ,addressOf 
    }




    #region Database Methods
    private void Update_TR_CP_Table(int parmInvestorID, double purchaseValue,string parcelNumber,string taxYear, string taxRollNumber, string certificateNumber)
    {
        DateTime saleDate = DateTime.Today;
      //  string parcelNumber = string.Empty;
        DateTime currDate = DateTime.Today;
        string currUser =  System.Web.HttpContext.Current.User.Identity.Name;

        //System.Environment .UserDomainName 
      //  double purchaseValue = 0.0;
        string SQL = string.Empty;

        // Iterate SelectedCPs dataset and update TR_CP for each parcel
        // 1) Update TR_CP
        // 1a) Update InvestorID to new InvestorID
        // 1b) Update CP_STATUS to 3 (purchased from state) from 2 (assigned to state)
        // 1c) Update DateOfPurchase
        // 1d) Update DateCPReassigned
        // 1e) Update Purchase value: set equal to sum of tax charges, interest(99901), publication fees (99902), 
        //     CP formulation (99920), and reassignment fee (99940).
        ///    Interest is derived directly from dates of delinquency
      //  foreach (DataRow dr in SelectedCPs.Tables[0].Rows)
      //  {
           // parcelNumber = dr["APN"] == DBNull.Value ? String.Empty : dr["APN"].ToString();

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
                                            " SET " +
                                            " DateCPReassigned = '{0}', " +
                                            " DateCPPurchased = '{8}', " +
                                            " CP_STATUS = 4, " +
                                            " EDIT_USER = '{1}', " +
                                            " EDIT_DATE = '{2}', " +
                                            " InvestorID = {3}, " +
                                            " PurchaseValue = '{4}' " +
                                            " WHERE APN = '{5}' " +
                                            " AND TaxRollNumber = '{6}' " +
                                            " AND TaxYear = '{7}' ",
                                            currDate,
                                            currUser,
                                            currDate,
                                            parmInvestorID,
                                            purchaseValue,
                                            parcelNumber,
                                            taxRollNumber,
                                            taxYear,
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
                // return recordID;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
       // }
    }

    private void Update_TR_Table(string taxYear, string taxRollNumber)
    {
        DateTime saleDate = DateTime.Today;
        //  string parcelNumber = string.Empty;
        DateTime currDate = DateTime.Today;
        string currUser = util.CurrentUserName;
        //  double purchaseValue = 0.0;
        string SQL = string.Empty;

        // Iterate SelectedCPs dataset and update TR_CP for each parcel
        // 1) Update TR_CP
        // 1a) Update InvestorID to new InvestorID
        // 1b) Update CP_STATUS to 3 (purchased from state) from 2 (assigned to state)
        // 1c) Update DateOfPurchase
        // 1d) Update DateCPReassigned
        // 1e) Update Purchase value: set equal to sum of tax charges, interest(99901), publication fees (99902), 
        //     CP formulation (99920), and reassignment fee (99940).
        ///    Interest is derived directly from dates of delinquency
        //  foreach (DataRow dr in SelectedCPs.Tables[0].Rows)
        //  {
        // parcelNumber = dr["APN"] == DBNull.Value ? String.Empty : dr["APN"].ToString();

        try
        {
            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    // Update record in TR_CP
                    SQL = string.Format("UPDATE genii_user.TR " +
                                        " SET " +
                                        " CurrentBalance = '{0}', " +
                                        " STATUS = 3, " +
                                        " EDIT_USER = '{1}', " +
                                        " EDIT_DATE = '{2}' " +
                                        " WHERE TaxRollNumber = '{3}' " +
                                        " AND TaxYear = '{4}' ",
                                        0.0,
                                        System.Web.HttpContext.Current.User.Identity.Name,
                                        currDate,                                        
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
        // }
    }


    private void Update_TR_CP_OWNER_Table(string currInvestorID, string newInvestorID, string certificateNumber)
    {
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
                                        "EDIT_USER = '{1}', " +
                                        "EDIT_DATE = '{2}' " +
                                        "WHERE InvestorID = {3} " +
                                        "AND CertificateNumber = '{4}' ",
                                        currDate,
                                        System.Web.HttpContext.Current.User.Identity.Name,
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
                                        System.Web.HttpContext.Current.User.Identity.Name,
                                        currDate,
                                        System.Web.HttpContext.Current.User.Identity.Name,
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


   // private void create_TR_CP_OWNER_Record()
  ////  {
//
 //   }


    private void create_TR_CHARGES_Record(string taxYear, string taxRollNumber)
    {
        try
        {
          //  int recordID = util.GetNewID("Record_ID", "TR_CHARGES");
            string sessionID = lblSessionID.Text;
            DateTime currentDate = DateTime.Today;
            int paymentType = 7;
            string payorName = this.lblCPFromStateInvestorName.Text;
            string currentUserName = System.Web.HttpContext.Current.User.Identity.Name;
            double charge99921 = 0;
            double charge99920 = 0;
            double charge99940 = 0;

            string SQL = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE RECORD_ID = '99921'");

            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbCommand cmd = new OleDbCommand(SQL, conn);

                charge99921 = Convert.ToDouble (cmd.ExecuteScalar());
            }

            string SQL2 = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE RECORD_ID = '99920'");

            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbCommand cmd = new OleDbCommand(SQL2, conn);

                charge99920 = Convert.ToDouble(cmd.ExecuteScalar());
            }

            string SQL3 = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE RECORD_ID = '99940'");

            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbCommand cmd = new OleDbCommand(SQL3, conn);

                charge99940 = Convert.ToDouble(cmd.ExecuteScalar());
            }



            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {                    
                    OleDbCommand cmdNewRec = new OleDbCommand("INSERT INTO genii_user.TR_CHARGES " +
                                                              "(TAXYEAR, " +
                                                              "TAXROLLNUMBER, " +
                                                              "TAXCHARGECODEID, " +
                                                              "TAXTYPEID, " +
                                                              "CHARGEAMOUNT, " +  
                                                              "EDIT_USER, " +
                                                              "EDIT_DATE, " +
                                                              "CREATE_USER, " +
                                                              "CREATE_DATE ) " +
                                                              "VALUES (?,?,?,?,?,?,?,?,?) ");

                    cmdNewRec.Connection = conn;
                    cmdNewRec.Transaction = trans;

                    // Set Parameter Values
                                      
                    cmdNewRec.Parameters.AddWithValue("@TAXYEAR", taxYear);
                    cmdNewRec.Parameters.AddWithValue("@TAXROLLNUMBER", taxRollNumber);
                    cmdNewRec.Parameters.AddWithValue("@TAXCHARGECODEID", 99920);
                    cmdNewRec.Parameters.AddWithValue("@TAXTYPEID", 75);
                    cmdNewRec.Parameters.AddWithValue("@CHARGEAMOUNT", charge99920 );                   
                    cmdNewRec.Parameters.AddWithValue("@EDIT_USER", currentUserName);
                    cmdNewRec.Parameters.AddWithValue("@EDIT_DATE", currentDate);
                    cmdNewRec.Parameters.AddWithValue("@CREATE_USER", currentUserName);
                    cmdNewRec.Parameters.AddWithValue("@CREATE_DATE", currentDate);

                    cmdNewRec.ExecuteNonQuery();


                       OleDbCommand cmdNewRec2 = new OleDbCommand("INSERT INTO genii_user.TR_CHARGES " +
                                                                "(TAXYEAR, " +
                                                                "TAXROLLNUMBER, " +
                                                                "TAXCHARGECODEID, " +
                                                                "TAXTYPEID, " +
                                                                "CHARGEAMOUNT, " +
                                                                "EDIT_USER, " +
                                                                "EDIT_DATE, " +
                                                                "CREATE_USER, " +
                                                                "CREATE_DATE ) " +
                                                                "VALUES (?,?,?,?,?,?,?,?,?) ");

                       cmdNewRec2.Connection = conn;
                       cmdNewRec2.Transaction = trans;



                       cmdNewRec2.Parameters.AddWithValue("@TAXYEAR", taxYear);
                       cmdNewRec2.Parameters.AddWithValue("@TAXROLLNUMBER", taxRollNumber);
                       cmdNewRec2.Parameters.AddWithValue("@TAXCHARGECODEID", 99921);
                       cmdNewRec2.Parameters.AddWithValue("@TAXTYPEID", 75);
                       cmdNewRec2.Parameters.AddWithValue("@CHARGEAMOUNT", charge99921);
                       cmdNewRec2.Parameters.AddWithValue("@EDIT_USER", currentUserName);
                       cmdNewRec2.Parameters.AddWithValue("@EDIT_DATE", currentDate);
                       cmdNewRec2.Parameters.AddWithValue("@CREATE_USER", currentUserName);
                       cmdNewRec2.Parameters.AddWithValue("@CREATE_DATE", currentDate);

                       cmdNewRec2.ExecuteNonQuery();
                    
    
                       OleDbCommand cmdNewRec3 = new OleDbCommand("INSERT INTO genii_user.TR_CHARGES " +
                                                                "(TAXYEAR, " +
                                                                "TAXROLLNUMBER, " +
                                                                "TAXCHARGECODEID, " +
                                                                "TAXTYPEID, " +
                                                                "CHARGEAMOUNT, " +
                                                                "EDIT_USER, " +
                                                                "EDIT_DATE, " +
                                                                "CREATE_USER, " +
                                                                "CREATE_DATE ) " +
                                                                "VALUES (?,?,?,?,?,?,?,?,?) ");

                       cmdNewRec3.Connection = conn;
                       cmdNewRec3.Transaction = trans;


                       cmdNewRec3.Parameters.AddWithValue("@TAXYEAR", taxYear);
                       cmdNewRec3.Parameters.AddWithValue("@TAXROLLNUMBER", taxRollNumber);
                       cmdNewRec3.Parameters.AddWithValue("@TAXCHARGECODEID", 99940);
                       cmdNewRec3.Parameters.AddWithValue("@TAXTYPEID", 75);
                       cmdNewRec3.Parameters.AddWithValue("@CHARGEAMOUNT", charge99940);
                       cmdNewRec3.Parameters.AddWithValue("@EDIT_USER", currentUserName);
                       cmdNewRec3.Parameters.AddWithValue("@EDIT_DATE", currentDate);
                       cmdNewRec3.Parameters.AddWithValue("@CREATE_USER", currentUserName);
                       cmdNewRec3.Parameters.AddWithValue("@CREATE_DATE", currentDate);

                       cmdNewRec3.ExecuteNonQuery();
                       
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

    private void create_GET_APPORTION_Record(int transID, string taxYear, string taxRollNumber, double dollarAmount, double totalAmount)
    
    {

        try
        {
            //  int recordID = util.GetNewID("Record_ID", "TR_CHARGES");
            string sessionID = lblSessionID.Text;
            DateTime currentDate = DateTime.Today;
            string paymentType = ddlPaymentType.SelectedValue;
            string payorName = this.lblCPFromStateInvestorName.Text;
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
                                        taxRollNumber ,
                                        taxYear);

                    OleDbCommand cmdUpdateRecord = new OleDbCommand(SQL);

                    cmdUpdateRecord.Connection = conn;
                    cmdUpdateRecord.Transaction = trans;

                    cmdUpdateRecord.ExecuteNonQuery();

                    trans.Commit();



                 //   DataSet ds = new DataSet();

               //     util.LoadTable(ds, "GetApportionment", "SELECT * FROM NCIS_TREASURY.dbo.GetApportionment('" + taxYear + "','" + taxRollNumber + "'," + totalAmount + ",'" + currentDate + "')");

               //     OleDbDataReader rdr = cmdGetRecord.ExecuteReader();
                    
                    //while (rdr.Read())
                    //{

                    //    int recordIDCashierApportion = util.GetNewID("RECORD_ID", "CASHIER_APPORTION");

                    //    string SQL4 = string.Format("INSERT INTO genii_user.CASHIER_APPORTION " +
                    //                                  "(Record_ID, TRANS_ID, TaxYear, TaxRollNumber, AreaCode, TaxChargeCodeID, " +
                    //                                  " TaxTypeID,PaymentDate,GLAccount, " +
                    //                                  " DateApportioned, DollarAmount,  " +
                    //                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " +
                    //                                  " VALUES ({0},{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}',{10},'{11}','{12}','{13}','{14}')",
                    //                                  recordIDCashierApportion,
                    //                                  transID,
                    //                                  rdr.GetString(0),
                    //                                  rdr.GetString(1),
                    //                                  rdr.GetString(2),
                    //                                  rdr.GetString(3),
                    //                                  rdr.GetString(4),
                    //                                  currentDate,
                    //                                  rdr.GetString(6),
                    //                                  currentDate,
                    //                                  rdr.GetDecimal(10),
                    //                                  currentUserName,
                    //                                  currentDate,
                    //                                  currentUserName,
                    //                                  currentDate);

                    //    OleDbCommand cmdNewRecApportion = new OleDbCommand(SQL4, conn);
                    //    cmdNewRecApportion.Transaction = trans2;                       
                    //    cmdNewRecApportion.ExecuteNonQuery();
                    //  //  cmdNewRecApportion.InitializeLifetimeService();
                    //}

                //    rdr.Close();


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


    private void create_CASHIER_APPORTION_Record(int transID, string taxYear, string taxRollNumber, double dollarAmount, double totalAmount)
    {
        string taxChargeCode = "99940";
        string taxChargeCode2 = "99930";
        string GLAccount = "N00100547180";
        string GLAccount2 = "N00100547180";
        DateTime currDate = DateTime.Today;
        string currUser = System.Web.HttpContext.Current.User.Identity.Name;
        string SQL = string.Empty;
        string SQL2 = string.Empty;
        int recordID = 0;
        int taxTypeID = 75;

        // Add record with CP Reassignment fee (99940)
        // ONE RECORD PER TaxRollNumber and TaxYear
        // record_id, trans_id (cashier_transactions record_id), taxyear, taxrollnumber, taxchargecodeID (99940), taxtypeID (75), glaccount(??), receiptnumber(??), dollarAmount
   //     recordID = util.GetNewID("RECORD_ID", "CASHIER_APPORTION");

        try
        {
            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {                    
                    SQL = string.Format("INSERT INTO genii_user.CASHIER_APPORTION " +
                                        "(TRANS_ID, " +
                                        "TaxYear, " +
                                        "TaxRollNumber, " +
                                        "TaxChargeCodeID, " +
                                        "TaxTypeID, " +
                                        "PaymentDate, " +
                                        "GLAccount, " +
                                        "DollarAmount, " +
                                        "EDIT_USER, " +
                                        "EDIT_DATE, " +
                                        "CREATE_USER, " +
                                        "CREATE_DATE) " +
                                        "VALUES ( {0}, {1}, '{2}', '{3}', '{4}', '{5}', '{6}', {7},'{8}', '{9}', '{10}', '{11}') ",
                                        transID,
                                        taxYear,
                                        taxRollNumber,
                                        taxChargeCode,
                                        taxTypeID,
                                        currDate,
                                        GLAccount,
                                        dollarAmount,
                                        System.Web.HttpContext.Current.User.Identity.Name,
                                        currDate,
                                        System.Web.HttpContext.Current.User.Identity.Name,
                                        currDate);


                    OleDbCommand cmdUpdateRecord = new OleDbCommand(SQL);

                    cmdUpdateRecord.Connection = conn;
                    cmdUpdateRecord.Transaction = trans;

                    cmdUpdateRecord.ExecuteNonQuery();

                    //for 99930
                    //int recordID2 = util.GetNewID("RECORD_ID", "CASHIER_APPORTION");
                    SQL2 = string.Format("INSERT INTO genii_user.CASHIER_APPORTION " +
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
                                       "VALUES ( {0}, {1}, '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}','{12}') ",
                                       transID,
                                       taxYear,
                                       taxRollNumber,
                                       taxChargeCode2,
                                       taxTypeID,
                                       currDate,
                                       GLAccount2,
                                       currDate,
                                       dollarAmount,
                                       System.Web.HttpContext.Current.User.Identity.Name,
                                       currDate,
                                       System.Web.HttpContext.Current.User.Identity.Name,
                                       currDate);


                    OleDbCommand cmdUpdateRecord2 = new OleDbCommand(SQL2);

                    cmdUpdateRecord2.Connection = conn;
                    cmdUpdateRecord2.Transaction = trans;

                    cmdUpdateRecord2.ExecuteNonQuery();
                    
                    //////////////////////////////////////////

                    trans.Commit();

                    
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

  


    private int create_CASHIER_TRANSACTIONS_Record(string taxYear, string taxRollNumber, decimal paymentAmount, double group_key)
    {            

        try
        {
            int recordID = util.GetNewID("Record_ID", "CASHIER_TRANSACTIONS");
            string sessionID = lblSessionID.Text;
            DateTime currentDate = DateTime.Today;
            string paymentType = ddlPaymentType.SelectedValue;
            string payorName = this.lblCPFromStateInvestorName.Text;
            string currentUserName = System.Web.HttpContext.Current.User.Identity.Name;
//            paymentAmount = paymentAmount + Convert.ToDecimal(20.00);

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
                                                              "TRANSACTION_STATUS, " +
                                                              "TAX_YEAR, " +
                                                              "TAX_ROLL_NUMBER, " +
                                                              "PAYMENT_DATE, " +
                                                              "PAYMENT_TYPE, " +
                                                              "APPLY_TO, " +
                                                              "PAYOR_NAME, " +
                                                              "CHECK_NUMBER, " +
                                                              "PAYMENT_AMT, " +
                                                              "TAX_AMT, " +
                                                              "EDIT_USER, " +
                                                              "EDIT_DATE, " +
                                                              "CREATE_USER, " +
                                                              "CREATE_DATE ) " +
                                                              "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?) ");

                    cmdNewRec.Connection = conn;
                    cmdNewRec.Transaction = trans;

                    // Set Parameter Values
                    cmdNewRec.Parameters.AddWithValue("@RECORD_ID", recordID);
                    cmdNewRec.Parameters.AddWithValue("@SESSION_ID", sessionID);
                    cmdNewRec.Parameters.AddWithValue("@GROUP_KEY", group_key);
                    cmdNewRec.Parameters.AddWithValue("@TRANSACTION_STATUS", 1);
                    cmdNewRec.Parameters.AddWithValue("@TAX_YEAR", taxYear);
                    cmdNewRec.Parameters.AddWithValue("@TAX_ROLL_NUMBER", taxRollNumber);
                    cmdNewRec.Parameters.AddWithValue("@PAYMENT_DATE", currentDate);
                    cmdNewRec.Parameters.AddWithValue("@PAYMENT_TYPE", paymentType);
                    cmdNewRec.Parameters.AddWithValue("@APPLY_TO", 6);
                    cmdNewRec.Parameters.AddWithValue("@PAYOR_NAME", payorName);
                    cmdNewRec.Parameters.AddWithValue("@CHECK_NUMBER", txtCheckNumber.Text);
                    cmdNewRec.Parameters.AddWithValue("@PAYMENT_AMT", paymentAmount);
                    cmdNewRec.Parameters.AddWithValue("@TAX_AMT", paymentAmount);
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

    private void create_TR_PAYMENTS_Record(string taxYear, string taxRollNumber, double paymentAmount, int transID)
    {
        double charge99921 = 0.0;
        double charge99920 = 0.0;
        double charge99940 = 0.0;

        string SQL = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE RECORD_ID = '99921'");

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            charge99921 = Convert.ToDouble(cmd.ExecuteScalar());
        }

        string SQL2 = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE RECORD_ID = '99920'");

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL2, conn);

            charge99920 = Convert.ToDouble(cmd.ExecuteScalar());
        }

        string SQL3 = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE RECORD_ID = '99940'");

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL3, conn);

            charge99940 = Convert.ToDouble(cmd.ExecuteScalar());
        }




        try
        {
            string sessionID = lblSessionID.Text;
            DateTime currentDate = DateTime.Today;
            string payorName = this.lblCPFromStateInvestorName.Text;
            string currentUserName = System.Web.HttpContext.Current.User.Identity.Name;

            using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
            {
                conn.Open();

                OleDbTransaction trans = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    // Insert new record into CASHIER_TRANSACTIONS.
                    OleDbCommand cmdNewRec = new OleDbCommand("INSERT INTO genii_user.TR_PAYMENTS  " +
                                                  " (TRANS_ID, TaxYear, TaxRollNumber, PaymentEffectiveDate,  " +
                                                  " PaymentTypeCode,PaymentMadeByCode,Pertinent1,  " +
                                                  " Pertinent2, PaymentAmount,   " +
                                                  " EDIT_USER,EDIT_DATE,CREATE_USER,CREATE_DATE) " +
                                                  " VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)");

                    cmdNewRec.Connection = conn;
                    cmdNewRec.Transaction = trans;

                    // Set Parameter Values
                    cmdNewRec.Parameters.AddWithValue("@TRANS_ID", transID); // payRow("Record_ID"));
                    cmdNewRec.Parameters.AddWithValue("@TaxYear", taxYear);
                    cmdNewRec.Parameters.AddWithValue("@TaxRollNumber", taxRollNumber);
                    cmdNewRec.Parameters.AddWithValue("@PaymentEffectiveDate", currentDate);
                    cmdNewRec.Parameters.AddWithValue("@PaymentTypeCode", ddlPaymentType .SelectedValue);
                    cmdNewRec.Parameters.AddWithValue("@PaymentMadeByCode", 2);
                    cmdNewRec.Parameters.AddWithValue("@Pertinent1", this.txtPayorName.Text);
                    cmdNewRec.Parameters.AddWithValue("@Pertinent2", "CP Purchased - " + currentDate);
                    cmdNewRec.Parameters.AddWithValue("@PaymentAmount", paymentAmount);
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
          //  return recordID;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }


    }
    #endregion




    #region Misc Methods

    /// <summary>
    /// GetInvestorInfo - Get investor information from ST_INVESTOR table
    /// </summary>
    /// <param name="InvestorID"></param>
    /// <returns></returns>
    public DataSet GetInvestorInfo(int InvestorID)
    {
        DataSet ds = new DataSet();

        util.LoadTable(ds, "ST_INVESTOR", "SELECT * FROM genii_user.ST_INVESTOR WHERE InvestorID = " + InvestorID);

        return ds;
    }


    /// <summary>
    /// LoatStateOwnedCps - Load all State Owned CP's from database and put them into a dataset to use later.
    /// </summary>
    private void LoadStateOwnedCPs()
    {
        string SQL = "SELECT *, '0.0' AS TAXES, '0.0' AS INTEREST, '0.0' AS FEES,'0.0' AS TRANSACTIONFEES, '0.0' AS TOTAL, '' AS TAXYEARRANGE " +
                     "FROM genii_user.TR_CP " +
                     "WHERE InvestorID = 1 " +
                     "AND DATE_REDEEMED IS NULL " +
                    // "AND CP_STATUS = 2 " +
                     "ORDER BY TaxYear DESC";

        util.LoadTable(StateOwnedCPs, "TR_CP", SQL);

        SelectedCPs.Tables.Add(StateOwnedCPs.Tables[0].Clone());
    }


    

    /// <summary>
    /// GetCPTaxes - Get Tax charges for a given parcel number
    /// </summary>
    /// <param name="parcelNumber"></param>
    /// <returns></returns>
    private string GetCPTaxes(string parcelNumber)
    {
        string taxAmount = string.Empty;

       // string SQL = String.Format("SELECT SUM(CHARGEAMOUNT) AS Taxes " +
        //string SQL = String.Format("SELECT taxyear, sum(CHARGEAMOUNT) AS Taxes  " +
        //                           " FROM genii_user.TR_CHARGES " +
        //                           " WHERE SUBSTRING(TaxChargeCodeID, 1, 1) <> 9 " +
        //                           " AND TaxRollNumber IN " +
        //                           " (SELECT TaxRollNumber FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) " +
        //                           " AND TAXYEAR IN " +
        //                           "(SELECT TAXYEAR FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) group by taxYear", parcelNumber);


        string SQL = String.Format("SELECT     genii_user.TR_CP.APN, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CHARGES.TaxRollNumber, " +
                                                     "  SUM(CASE WHEN taxtypeid <= 40 THEN chargeamount ELSE 0 END) AS Taxes, SUM(CASE WHEN taxtypeid = 80 THEN chargeamount ELSE 0 END) AS Interest, " +
                                                     "  SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN chargeamount ELSE 0 END) AS Fees, " +
                                                     "  (select parameter from genii_user.ST_PARAMETER where parameter_name='CURRENT_TXN_FEE') AS 'TransferFees'" +
                                " FROM         genii_user.TR_CHARGES INNER JOIN" +
                                                     "  genii_user.TR_CP ON genii_user.TR_CHARGES.TaxYear = genii_user.TR_CP.TaxYear AND " +
                                                     "  genii_user.TR_CHARGES.TaxRollNumber = genii_user.TR_CP.TaxRollNumber" +
                                " WHERE     genii_user.TR_CP.CP_STATUS = 2 AND genii_user.TR_CP.APN = '{0}'" +
                                " GROUP BY genii_user.TR_CHARGES.TaxRollNumber, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CP.APN", parcelNumber);

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            using (OleDbDataReader dr = cmd.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        taxAmount=taxAmount + '\n' + '$' + dr["Taxes"];
                    }
                }
            }


           // taxAmount = Convert.ToString(cmd.ExecuteScalar());
        }

        return taxAmount;
    }

    private string GetCPTotalAmount(string parcelNumber)
    {
        string taxAmount = string.Empty;

        // string SQL = String.Format("SELECT SUM(CHARGEAMOUNT) AS Taxes " +
        //string SQL = String.Format("SELECT sum(CHARGEAMOUNT) AS Taxes  " +
        //                           " FROM genii_user.TR_CHARGES " +
        //                           " WHERE SUBSTRING(TaxChargeCodeID, 1, 4) <> 9993 and SUBSTRING(TaxChargeCodeID, 1, 5) <> 99922 " +
        //                           " AND TaxRollNumber IN " +
        //                           " (SELECT TaxRollNumber FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) " +
        //                           " AND TAXYEAR IN " +
        //                           "(SELECT TAXYEAR FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL)", parcelNumber);


        string SQL = String.Format("SELECT     sum(genii_user.TR_CHARGES.chargeamount) + (count( distinct genii_user.TR_CP.TaxYear) *(select parameter from genii_user.ST_PARAMETER where parameter_name='CURRENT_TXN_FEE')) " +
                                " FROM         genii_user.TR_CHARGES INNER JOIN" +
                                                     "  genii_user.TR_CP ON genii_user.TR_CHARGES.TaxYear = genii_user.TR_CP.TaxYear AND " +
                                                     "  genii_user.TR_CHARGES.TaxRollNumber = genii_user.TR_CP.TaxRollNumber" +
                                " WHERE     genii_user.TR_CP.CP_STATUS = 2 AND genii_user.TR_CP.APN = '{0}'", parcelNumber);


        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

             taxAmount = Convert.ToString(cmd.ExecuteScalar());
        }

        return taxAmount;
    }

    private string GetCPTaxYear(string parcelNumber)
    {
        string taxYear = string.Empty;

        //string SQL = String.Format("SELECT COUNT(taxYear) AS TAXYEAR FROM genii_user.TR_CP WHERE APN  = '{0}' " +
        //                           " AND TaxRollNumber IN " +
        //                           " (SELECT TaxRollNumber FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) " +
        //                           " AND TAXYEAR IN " +
        //                           " (SELECT TAXYEAR FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL)", parcelNumber);

        string SQL = String.Format("SELECT    count(distinct genii_user.TR_CHARGES.TaxYear) " +
                                " FROM         genii_user.TR_CHARGES INNER JOIN " +
                                                     "  genii_user.TR_CP ON genii_user.TR_CHARGES.TaxYear = genii_user.TR_CP.TaxYear AND " +
                                                     "  genii_user.TR_CHARGES.TaxRollNumber = genii_user.TR_CP.TaxRollNumber " +
                                " WHERE     genii_user.TR_CP.CP_STATUS = 2 AND genii_user.TR_CP.APN = '{0}' ", parcelNumber);

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            taxYear = Convert.ToString(cmd.ExecuteScalar());
        }

        return taxYear;
    }

    private string GetCPTaxYearParameter()
    {
        string taxYear = string.Empty ;

        string SQL = String.Format("SELECT PARAMETER FROM genii_user.ST_PARAMETER WHERE PARAMETER_NAME = 'CURRENT_TAXYEAR'");

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            taxYear = Convert.ToString (cmd.ExecuteScalar());
        }

        return taxYear;
    }

    private string GetTaxRollNumber(string parcelNumber,string taxYear)
    {
        string taxRollNumber = string.Empty;

        //string SQL = String.Format("SELECT top 1 MIN(TaxRollNumber) AS TaxRollNumber  " +
        //                            " FROM genii_user.TR_CP  " +
        //                            " WHERE InvestorID = 1  " +
        //                            " and apn='{0}' " +
        //                            " AND DateCPReassigned is NULL  " +
        //                            " GROUP BY taxyear  " +
        //                            " ORDER BY taxYear desc",parcelNumber ); 

        string SQL = String.Format("SELECT top 1 MIN(TaxRollNumber) AS TaxRollNumber  " +
                                    " FROM genii_user.TR  " +
                                    " where apn='{0}' " +
                                    " and taxyear='{1}' " +
                                    " GROUP BY taxyear  " +
                                    " ORDER BY taxYear desc", parcelNumber,taxYear); 

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            taxRollNumber = Convert.ToString(cmd.ExecuteScalar());
        }

        return taxRollNumber;
    }

    private string GetCPCertificateNumber(string parcelNumber)
    {
        string certificateNumber = string.Empty;

        //string SQL = String.Format("SELECT top 1 CertificateNumber  " +
        //                            " FROM genii_user.TR_CP  " +
        //                            " WHERE InvestorID = 1  " +
        //                            " and apn='{0}' " +
        //                            " AND DateCPReassigned is NULL  " +
        //                            " GROUP BY  taxYear, CertificateNumber  " +
        //                            " ORDER BY taxYear desc", parcelNumber);

        string SQL = String.Format("SELECT     genii_user.TR_CP.CertificateNumber " +
                               " FROM         genii_user.TR_CHARGES INNER JOIN" +
                                                    "  genii_user.TR_CP ON genii_user.TR_CHARGES.TaxYear = genii_user.TR_CP.TaxYear AND " +
                                                    "  genii_user.TR_CHARGES.TaxRollNumber = genii_user.TR_CP.TaxRollNumber" +
                               " WHERE     genii_user.TR_CP.CP_STATUS = 2 AND genii_user.TR_CP.APN = '{0}'" +
                               " GROUP BY genii_user.TR_CHARGES.TaxRollNumber, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CP.APN,genii_user.TR_CP.CertificateNumber order by genii_user.TR_CHARGES.TaxYear", parcelNumber);

        //using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        //{
        //    conn.Open();

        //    OleDbCommand cmd = new OleDbCommand(SQL, conn);

        //    certificateNumber = Convert.ToString(cmd.ExecuteScalar());
        //}

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            using (OleDbDataReader dr = cmd.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        certificateNumber = certificateNumber + '\n' + dr["CertificateNumber"];
                    }
                }
            }

            //  taxYearRange = Convert.ToString (cmd.ExecuteScalar());
        }


        return certificateNumber;
    }


    private string GetCPFirstCertificateNumber(string parcelNumber)
    {
        string certificateNumber = string.Empty;

        string SQL = String.Format("SELECT top 1 CertificateNumber  " +
                                    " FROM genii_user.TR_CP  " +
                                    " WHERE InvestorID = 1  " +
                                    " and apn='{0}' " +
                                    " AND DateCPReassigned is NULL  " +
                                    " GROUP BY  taxYear, CertificateNumber  " +
                                    " ORDER BY taxYear desc", parcelNumber);



        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            certificateNumber = Convert.ToString(cmd.ExecuteScalar());
        }      

        return certificateNumber;
    }


    private string GetCPTaxYearRange(string parcelNumber)
    {
        string taxYearRange = string.Empty; 

      //  string SQL = String.Format("SELECT (CONVERT(NVARCHAR(50),MIN(taxYear)) +' - '+ CONVERT(NVARCHAR(50),MAX(TAXYEAR))) AS TAXYEARRANGE FROM genii_user.TR_CP WHERE APN = '{0}'", parcelNumber);

        //string SQL = string.Format("SELECT taxyear " +
        //                           " FROM genii_user.TR_CHARGES " +
        //                           " WHERE taxRollNumber IN " +
        //                           " (SELECT TaxRollNumber FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) " +
        //                           " AND TAXYEAR IN " +
        //                           " (SELECT TAXYEAR FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) group by taxyear", parcelNumber);

        string SQL = String.Format("SELECT     genii_user.TR_CP.APN, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CHARGES.TaxRollNumber, " +
                                                     "  SUM(CASE WHEN taxtypeid <= 40 THEN chargeamount ELSE 0 END) AS Taxes, SUM(CASE WHEN taxtypeid = 80 THEN chargeamount ELSE 0 END) AS Interest, " +
                                                     "  SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN chargeamount ELSE 0 END) AS Fees, " +
                                                     "  (select parameter from genii_user.ST_PARAMETER where parameter_name='CURRENT_TXN_FEE') AS 'TransferFees'" +
                                " FROM         genii_user.TR_CHARGES INNER JOIN" +
                                                     "  genii_user.TR_CP ON genii_user.TR_CHARGES.TaxYear = genii_user.TR_CP.TaxYear AND " +
                                                     "  genii_user.TR_CHARGES.TaxRollNumber = genii_user.TR_CP.TaxRollNumber" +
                                " WHERE     genii_user.TR_CP.CP_STATUS = 2 AND genii_user.TR_CP.APN = '{0}'" +
                                " GROUP BY genii_user.TR_CHARGES.TaxRollNumber, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CP.APN order by genii_user.TR_CHARGES.TaxYear", parcelNumber);

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            using (OleDbDataReader dr = cmd.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        taxYearRange = taxYearRange + '\n' + dr["taxyear"];
                    }
                }
            }

          //  taxYearRange = Convert.ToString (cmd.ExecuteScalar());
        }

        return taxYearRange;
    }

    /// <summary>
    /// GetCPFees - Get All fees for a given parcel number
    /// </summary>
    /// <param name="parcelNumber"></param>
    /// <returns></returns>
    private string GetCPFees(string parcelNumber)
    {
        string feeAmount = string.Empty;

        //string SQL = string.Format("SELECT taxyear,ISNULL(SUM(CHARGEAMOUNT),0) AS FEES" +
        //                           " FROM genii_user.TR_CHARGES " +
        //                           " WHERE TaxChargeCodeID IN (99902) " + //99921,99920, 99940, not to be included in fees accdg to max.. MTA 05092013
        //                           " AND TaxRollNumber IN " +
        //                           " (SELECT TaxRollNumber FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) " +
        //                           " AND TAXYEAR IN " +
        //                           " (SELECT TAXYEAR FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) group by taxyear", parcelNumber);

        string SQL = String.Format("SELECT     genii_user.TR_CP.APN, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CHARGES.TaxRollNumber, " +
                                                     "  SUM(CASE WHEN taxtypeid <= 40 THEN chargeamount ELSE 0 END) AS Taxes, SUM(CASE WHEN taxtypeid = 80 THEN chargeamount ELSE 0 END) AS Interest, " +
                                                     "  SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN chargeamount ELSE 0 END) AS Fees, " +
                                                     "  (select parameter from genii_user.ST_PARAMETER where parameter_name='CURRENT_TXN_FEE') AS 'TransferFees'" +
                                " FROM         genii_user.TR_CHARGES INNER JOIN" +
                                                     "  genii_user.TR_CP ON genii_user.TR_CHARGES.TaxYear = genii_user.TR_CP.TaxYear AND " +
                                                     "  genii_user.TR_CHARGES.TaxRollNumber = genii_user.TR_CP.TaxRollNumber" +
                                " WHERE     genii_user.TR_CP.CP_STATUS = 2 AND genii_user.TR_CP.APN = '{0}'" +
                                " GROUP BY genii_user.TR_CHARGES.TaxRollNumber, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CP.APN", parcelNumber);

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {

            //99901 IS SUPPOSED TO BE THE INTEREST
            //99920 AND 99940 NOT TO BE INCLUDED IN FEES...
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);


            using (OleDbDataReader dr = cmd.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        if (dr["Fees"] != DBNull.Value)
                        {
                            feeAmount = feeAmount + '\n' + '$' + dr["Fees"];                            
                        }else{
                            feeAmount = feeAmount + '\n' + "$0.00"; 
                        }
                        
                    }
                }
            }


          //  feeAmount = Convert.ToString(cmd.ExecuteScalar());
        }

        return feeAmount;
    }

    private string GetCPtransactionFees(string parcelNumber)
    {
        string feeAmount = string.Empty;

        //string SQL = string.Format("SELECT taxyear, SUM(CHARGEAMOUNT) AS Fees" +
        //                            " FROM genii_user.TR_CHARGES " +
        //                            " WHERE TaxChargeCodeID IN (99921,99920, 99940) AND TaxRollNumber IN " +
        //                            " (SELECT TaxRollNumber FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) " +
        //                            " AND TAXYEAR IN " +
        //                            " (SELECT TAXYEAR FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL)" +
        //                            " group by taxYear", parcelNumber);


        string SQL = String.Format("SELECT     genii_user.TR_CP.APN, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CHARGES.TaxRollNumber, " +
                                                     "  SUM(CASE WHEN taxtypeid <= 40 THEN chargeamount ELSE 0 END) AS Taxes, SUM(CASE WHEN taxtypeid = 80 THEN chargeamount ELSE 0 END) AS Interest, " +
                                                     "  SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN chargeamount ELSE 0 END) AS Fees, " +
                                                     "  (select parameter from genii_user.ST_PARAMETER where parameter_name='CURRENT_TXN_FEE') AS 'TransferFees'" +
                                " FROM         genii_user.TR_CHARGES INNER JOIN" +
                                                     "  genii_user.TR_CP ON genii_user.TR_CHARGES.TaxYear = genii_user.TR_CP.TaxYear AND " +
                                                     "  genii_user.TR_CHARGES.TaxRollNumber = genii_user.TR_CP.TaxRollNumber" +
                                " WHERE     genii_user.TR_CP.CP_STATUS = 2 AND genii_user.TR_CP.APN = '{0}'" +
                                " GROUP BY genii_user.TR_CHARGES.TaxRollNumber, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CP.APN", parcelNumber);

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {

            //99901 IS SUPPOSED TO BE THE INTEREST
            //99920 AND 99940 NOT TO BE INCLUDED IN FEES...
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            using (OleDbDataReader dr = cmd.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                      //  if (dr["Fees"] != DBNull.Value)
                      //  {
                            feeAmount = feeAmount + '\n' + '$' + dr["TransferFees"];
                     //   }
                     //   else
                     //   {
                       //     feeAmount = feeAmount + '\n' + "$0.00";
                     //   }
                    }
                }
               // if (feeAmount==string.Empty)
              //  {
              //      feeAmount = "$30.00";
              //  }
            }

           // feeAmount = Convert.ToString(cmd.ExecuteScalar());
        }

        return feeAmount;
    }


    /// <summary>
    /// GetCPInterest - Get all interest for a given parcel number
    /// </summary>
    /// <param name="parcelNumber"></param>
    /// <returns></returns>
    private string GetCPInterest(string parcelNumber)
    {
        string interestAmount = string.Empty;      

        //string SQL = string.Format("SELECT taxyear, SUM(CHARGEAMOUNT) AS Fees " +
        //                           "FROM genii_user.TR_CHARGES " +
        //                           " WHERE TaxChargeCodeID IN (99901) " +
        //                           " AND TaxRollNumber IN " +
        //                           "(SELECT TaxRollNumber FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) " +
        //                           " AND TAXYEAR IN " +
        //                           " (SELECT TAXYEAR FROM genii_user.TR_CP WHERE APN = '{0}' AND DATE_REDEEMED IS NULL) group by taxyear", parcelNumber);

        string SQL = String.Format("SELECT     genii_user.TR_CP.APN, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CHARGES.TaxRollNumber, " +
                                                    "  SUM(CASE WHEN taxtypeid <= 40 THEN chargeamount ELSE 0 END) AS Taxes, SUM(CASE WHEN taxtypeid = 80 THEN chargeamount ELSE 0 END) AS Interest, " +
                                                    "  SUM(CASE WHEN taxtypeid IN (70, 75, 76, 90, 91, 92, 93, 99) THEN chargeamount ELSE 0 END) AS Fees, " +
                                                    "  (select parameter from genii_user.ST_PARAMETER where parameter_name='CURRENT_TXN_FEE') AS 'TransferFees'" +
                               " FROM         genii_user.TR_CHARGES INNER JOIN" +
                                                    "  genii_user.TR_CP ON genii_user.TR_CHARGES.TaxYear = genii_user.TR_CP.TaxYear AND " +
                                                    "  genii_user.TR_CHARGES.TaxRollNumber = genii_user.TR_CP.TaxRollNumber" +
                               " WHERE     genii_user.TR_CP.CP_STATUS = 2 AND genii_user.TR_CP.APN = '{0}'" +
                               " GROUP BY genii_user.TR_CHARGES.TaxRollNumber, genii_user.TR_CHARGES.TaxYear, genii_user.TR_CP.APN", parcelNumber);

        using (OleDbConnection conn = new OleDbConnection(util.ConnectString))
        {

            //99901 IS SUPPOSED TO BE THE INTEREST
            //99920 AND 99940 NOT TO BE INCLUDED IN FEES...
            conn.Open();

            OleDbCommand cmd = new OleDbCommand(SQL, conn);

            using (OleDbDataReader dr = cmd.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        interestAmount = interestAmount + '\n' + '$' + dr["Interest"];
                    }
                }
            }

           // interestAmount = Convert.ToString(cmd.ExecuteScalar());
        }


        return interestAmount;
    }


    /// <summary>
    /// GetTotalsForParcels - Calculate all totals for all parcels in dataset and update TOTAL field for each row.
    /// </summary>
    private void GetTotalsForParcels( string parcelNumber)
    {
        double parcelTaxes = 0.0;
        double parcelFees = 0.0;
        double parcelInterest = 0.0;
        double parcelTotal = 0.0;
        double parcelTransactionFees = 0.0;

        foreach (DataRow dr in SelectedCPs.Tables[0].Rows)
        {
          //  parcelTaxes = dr["TAXES"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["TAXES"].ToString());
          //  parcelFees = dr["FEES"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["FEES"].ToString());
          //  parcelTransactionFees = dr["TRANSACTIONFEES"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["TRANSACTIONFEES"].ToString());
          //  parcelInterest = dr["INTEREST"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["INTEREST"].ToString());
          //  parcelTotal = parcelTaxes + parcelFees + parcelTransactionFees + parcelInterest;

         //   dr["TOTAL"] = Math.Round(parcelTotal,2);
           // dr["TAXYEARRANGE"] = GetCPTaxYearRange(parcelNumber);

         //   lblTotalAmount.Text = (Math.Round(parcelTotal, 2)).ToString();

        }
    }
    #endregion



   


    #region Grid Events

    protected void gvCPFromStateParcelGrid_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        //if (e.CommandName == "RemoveParcel")
        //{

        //    int index = Convert.ToInt32(e.CommandArgument);   
        //}
    }

    protected void gvCPFromStateParcelGrid_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        SelectedCPs.Tables[0].Rows.RemoveAt(e.RowIndex);
        gvCPFromStateParcelGrid.DataSource = SelectedCPs;
        gvCPFromStateParcelGrid.DataBind();
    }

    #endregion



}