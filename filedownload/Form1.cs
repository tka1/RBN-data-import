using System;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Npgsql;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;


namespace filedownload
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

      dloadPath.Text= @"e:\rbndata\" ;
            unzip_folder.Text = @"e:\temp\rbndata\";

        }

        public static DataTable RetrieveSourceData(String filename)


        {
            //connection string changes depending on the operation  
            //system you are running  
            string sourceConnString = @"Provider=Microsoft.Jet.OLEDB.4.0; 
                                        Data Source=e:\; 
                                        Extended Properties=text;";
            DataTable sourceData = new DataTable();
            using (OleDbConnection conn =
                           new OleDbConnection(sourceConnString))
            {
                conn.Open();
                // Get the data from the source table as a SqlDataReader. 
                OleDbCommand command = new OleDbCommand(
                                                        @"SELECT * from " + filename, conn);
                OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                sourceData.Dispose();
                sourceData.AcceptChanges();
                adapter.Fill(sourceData);

                conn.Close();
            }
            return sourceData;
        }

        public static void CopyData(DataTable sourceData)

        {
            string destConnString = @"Password=123456;  
                                     Persist Security Info=True; 
                                     User ID=etl;Initial Catalog=RBN; 
                                     Data Source=INTEL\OH2BBT";

            // Set up the bulk copy object.  
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(destConnString))
            {
                bulkCopy.DestinationTableName = "dbo.rbn_data";
                // Guarantee that columns are mapped correctly by 
                // defining the column mappings for the order. 
                bulkCopy.ColumnMappings.Add("callsign", "callsign");
                bulkCopy.ColumnMappings.Add("de_pfx", "de_pfx");
                bulkCopy.ColumnMappings.Add("de_cont", "de_cont");
                bulkCopy.ColumnMappings.Add("freq", "freq");
                bulkCopy.ColumnMappings.Add("band", "band");
                bulkCopy.ColumnMappings.Add("dx", "dx");
                bulkCopy.ColumnMappings.Add("dx_pfx", "dx_pfx");
                bulkCopy.ColumnMappings.Add("dx_cont", "dx_cont");
                bulkCopy.ColumnMappings.Add("mode", "mode");
                bulkCopy.ColumnMappings.Add("db", "db");
                bulkCopy.ColumnMappings.Add("date", "date");
                bulkCopy.ColumnMappings.Add("speed", "speed");
                bulkCopy.ColumnMappings.Add("tx_mode", "tx_mode");
                //  Write from the source to the destination. 
                bulkCopy.WriteToServer(sourceData);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String RbnPath;
            RbnPath = dloadPath.Text;
            //string[] filenames = Directory.GetFiles(@"e:\rbndata");
            string[] filenames = Directory.GetFiles(RbnPath);
            //string extractPath = @"e:\temp\rbndata";
            string extractPath = unzip_folder.Text;
            foreach (string name in filenames)
            {
                string newfilename = name.Substring(0, name.Length - 3) + "txt";

                //MessageBox.Show(newfilename);
                try {
                    ZipFile.ExtractToDirectory(name, extractPath);
                    textBox1.AppendText(name + "\r\n");
                }
                catch (Exception eee)
                {
                    MessageBox.Show(eee.ToString());
                }

            }

        }
     

      

     

        private void button2_Click(object sender, EventArgs e)
        {
            string dbserver = System.Configuration.ConfigurationManager.AppSettings["dbserver"];
            string database = System.Configuration.ConfigurationManager.AppSettings["database"];
            string userid = System.Configuration.ConfigurationManager.AppSettings["userid"];
           
            NpgsqlConnection conn = new NpgsqlConnection("Server=" + dbserver + ";User Id=" + userid + ";Password=Saturnus1!" + ";Database=" + database + ";");

            char splitchar = ',';
            string freq_orig = "";
            decimal freq = 0;
            decimal db = 0;
            string rbndatafolder = unzip_folder.Text;
           

            using (StreamWriter ww = File.AppendText(@"e:\temp\rbndata\temppi\new\log.txt"))
            {
                 ww.WriteLine("started  " + DateTime.Now);
            }
            string[] files = Directory.GetFiles(rbndatafolder);





            foreach (string name in files)
            {

                using (StreamWriter ww = File.AppendText(@"e:\temp\rbndata\log.txt"))
                {
                    ww.WriteLine(DateTime.Now + "  " + name);
                }
                textBox1.AppendText(name + "\r\n");
                string[] rows = File.ReadAllLines(name);
                for (int i = 0; i <= rows.Length - 1; i++)

                {

                    string[] newrow = rows[i].Split(splitchar);
                    if (newrow[0] != "callsign" && newrow.Length > 3)
                    {
                        // for (int col=0; col <= newrow.Length-1; col++)
                        // {
                        // string colum = newrow[col];
                        // }
                        DateTime dt = Convert.ToDateTime("1900-03-03");
                        //string date = newrow[10];
                        try
                        {
                            // dt = Convert.ToDateTime(date);
                            dt = DateTime.Parse(newrow[10]);
                            // dt = DateTime.ParseExact(newrow[10], "YYYY-DD-MM HH:MI:SS", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat);
                        }
                        catch (Exception de)
                        {
                            textBox1.AppendText(rows[i] + "  " + i + "  " + de + "\r\n");
                        }
                        // DateTime dt = Convert.ToDateTime(date);
                        // textBox1.AppendText(newrow[10] + " "  +dt.Month  + "\r\n");
                        freq_orig = newrow[3];
                        freq_orig = freq_orig.Replace(".", ",");

                        try
                        {
                            freq = Decimal.Parse(freq_orig);
                            db = Decimal.Parse(newrow[9]);
                        }
                        catch (FormatException fe)
                        {
                            using (StreamWriter ww = File.AppendText(@"e:\temp\rbndata\temppi\new\log.txt"))

                                ww.WriteLine(DateTime.Now + "  " + fe.ToString());
                        }
                        string speed = "";
                        string tx_mode = "";
                        if (newrow.Length > 11)
                        {
                            speed = newrow[11];
                            tx_mode = newrow[12];
                        }

                        // textBox1.AppendText(freq + "\r\n");
                        /* using (StreamWriter w = File.AppendText(@"e:\temp\rbndata\temppi\new\rbn.csv"))
                             if (newrow.Length > 2)
                             {
                                 w.WriteLine(newrow[0] + ";" + newrow[1] + ";" + newrow[2] + ";" + newrow[3] + ";" + newrow[4] + ";" + newrow[5] + ";" + newrow[6] + ";" + newrow[7] + ";" + newrow[8] + ";" + newrow[9] + ";" + newrow[10]);
                             }*/
                        try
                        {
                            conn.Open();
                            NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO cluster.rbn(callsign, de_pfx, de_cont, freq, band, dx, dx_pfx, dx_cont, mode, db, date,speed,tx_mode) values ( :value1 ,:value2,:value3,:value4,:value5,:value6,:value7,:value8,:value9,:value10,:value11,:value12,:value13)", conn);
                            cmd.Parameters.Add(new NpgsqlParameter("value1", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value2", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value3", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value4", NpgsqlTypes.NpgsqlDbType.Numeric));
                            cmd.Parameters.Add(new NpgsqlParameter("value5", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value6", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value7", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value8", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value9", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value10", NpgsqlTypes.NpgsqlDbType.Numeric));
                            cmd.Parameters.Add(new NpgsqlParameter("value11", NpgsqlTypes.NpgsqlDbType.Timestamp));
                            cmd.Parameters.Add(new NpgsqlParameter("value12", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value13", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters[0].Value = newrow[0];
                            cmd.Parameters[1].Value = newrow[1];
                            cmd.Parameters[2].Value = newrow[2];
                            cmd.Parameters[3].Value = freq;
                            cmd.Parameters[4].Value = newrow[4];
                            cmd.Parameters[5].Value = newrow[5];
                            cmd.Parameters[6].Value = newrow[6];
                            cmd.Parameters[7].Value = newrow[7];
                            cmd.Parameters[8].Value = newrow[8];
                            cmd.Parameters[9].Value = db;
                            cmd.Parameters[10].Value = dt;
                            cmd.Parameters[11].Value = speed;
                            cmd.Parameters[12].Value = tx_mode;

                            NpgsqlDataReader dr = cmd.ExecuteReader();
                            conn.Close();




                        }
                        catch (Exception eeee)
                        {
                            using (StreamWriter ww = File.AppendText(@"e:\temp\rbndata\temppi\new\log.txt"))

                                ww.WriteLine(DateTime.Now + "  " + name + " rivi: " + rows[i] + "  " + eeee.ToString());
                            //     MessageBox.Show(name+" "+rows[i]+"  " + eeee.ToString());
                            conn.Close();

                        }





                    }

                }
            }
            textBox1.AppendText("done" + "\r\n");
            using (StreamWriter ww = File.AppendText(@"e:\temp\rbndata\temppi\new\log.txt"))
            {
                ww.WriteLine("stopped  " + DateTime.Now);
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            HtmlWeb web = new HtmlWeb();
           HtmlAgilityPack.HtmlDocument document2 = web.Load("http://www.reversebeacon.net/raw_data/");
           HtmlNode[] nodes = document2.DocumentNode.SelectNodes("//td").ToArray();
            
            Regex regex = new Regex(@"href=");
            int kountti = 0;
            String RbnPath;
           // RbnPath = @"e:\rbndata\";
            RbnPath = dloadPath.Text;
            textBox1.AppendText(RbnPath + "\r\n");

            String RBNYear;
            RBNYear = textBox_year.Text;
            foreach (HtmlNode item in nodes)
            {

                Match match = regex.Match(item.OuterHtml);
                if (match.Success)
                {
                    // textBox1.AppendText(item.OuterHtml + "\r\n");
                    string hreflink = item.OuterHtml;
                    string link = hreflink.Substring(13, 17);
                    WebClient webClient = new WebClient();
                    string year = link.Substring(link.Length - 8, 4);
                    //textBox1.AppendText(year + "\r\n");
                   // string downloadYear = RBNYear;
                    
                    if (year.Equals(RBNYear))
                    { 
                    try
                    {
                            webClient.DownloadFile("http://www.reversebeacon.net/raw_data/" + link, RbnPath + link.Substring(link.Length - 8, 8) + ".zip");
                            textBox1.AppendText("http://www.reversebeacon.net/raw_data/" + link + " " + link.Substring(link.Length - 8, 8) + "\r\n");
                        }
                    catch (Exception ee)
                    { MessageBox.Show(ee.ToString()); }
                    
                

                   }
                    kountti++;
                }

              //  textBox1.AppendText(item.OuterHtml + "\r\n");
               
                //textBox1.AppendText(att + "\r\n");
                // Console.WriteLine(item.InnerHtml);
            }

           

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (StreamWriter ww = File.AppendText(@"e:\temp\rbndata\temppi\new\log.txt"))
            {
                ww.WriteLine("started export to sql server  " + DateTime.Now);
            }
            string[] files = Directory.GetFiles(@"e:\temp\rbndata\temppi");
           

            foreach (string name in files)
            {
                FileInfo f = new FileInfo(name);
               long fileSize = f.Length/1000000;
            // MessageBox.Show(name + " "+ fileSize +" M");
                // DataTable data = RetrieveSourceData(name);

                string sourceConnString = @"Provider=Microsoft.Jet.OLEDB.4.0; 
                                        Data Source=e:\; 
                                        Extended Properties=text;";
                DataTable sourceData = new DataTable();
                using (OleDbConnection conn =
                               new OleDbConnection(sourceConnString))
                {
                    conn.Open();
                    // Get the data from the source table as a SqlDataReader. 
                    OleDbCommand command = new OleDbCommand(
                                                            @"SELECT * from " + name, conn);
                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                  
                    //sourceData.AcceptChanges();
                    adapter.Fill(sourceData);
                    adapter.Dispose();
                    conn.Close();
                }


                textBox1.AppendText("start to load "+name  + " " + fileSize + " M" + "\r\n");
                try {

                    //  CopyData(sourceData);

                    textBox1.AppendText("done" + "\r\n");
                }
                catch (Exception eeee)
                {
                    using (StreamWriter ww = File.AppendText(@"e:\temp\rbndata\temppi\new\log.txt"))

                        ww.WriteLine(DateTime.Now + "  " + name +  eeee.ToString());
                         //MessageBox.Show(name  + eeee.ToString());
                }
                finally
                {
                    sourceData.Dispose();
                    sourceData = null;
                
                }
                using (StreamWriter ww = File.AppendText(@"e:\temp\rbndata\temppi\new\log.txt"))
                {
                    ww.WriteLine(name +" export to sql server done  " + DateTime.Now);
                }

            }



        }

        private void dloadPath_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox_year_MouseClick(object sender, MouseEventArgs e)
        {
            textBox_year.Text = "";
        }

        private void dloadPath_MouseClick(object sender, MouseEventArgs e)
        {
           // dloadPath.Text = "";
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void unzip_folder_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
