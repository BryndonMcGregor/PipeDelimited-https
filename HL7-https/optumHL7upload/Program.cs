using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.Linq;
using System.Data.Odbc;
using System.Net;
using HL7Messaging;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Web;

namespace hl7HTTPS
{
    class Program
    {
        static void Main(string[] args)
        {


            //GET DATA TO SEND   //DATABASE CONNECTIONS

            string connectionString = "connection";
            OdbcConnection conn = new OdbcConnection(connectionString);
            conn.Open();
            DataSet data = new DataSet();

            string querystr = "select * from DATABASE WHERE " + args[0];

            OdbcDataAdapter avtrda = new OdbcDataAdapter(querystr, conn);
            avtrda.Fill(data);

            conn.Close();

            string output = "";

            foreach (DataRow dataR in data.Tables[0].Rows)
            {
             
                var Response = new Message();
                string uniqueID = Guid.NewGuid().ToString();


                var msh = new Segment("MSH");
                msh.Field(2, "^~\\&");
                msh.Field(3, "COMPANY"); //Application
                msh.Field(4, dataR["report_header_1"].ToString() + "-OK"); //sending facility 
                msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmm"));
                msh.Field(9, "ORM^O01");
                msh.Field(10, uniqueID);
                msh.Field(11, "T");
                msh.Field(12, "2.3");

                Response.Add(msh);

                //create now for formatting when adding
                var DOB = Convert.ToDateTime(dataR["date_of_birth"].ToString());

                var pid = new Segment("PID");
                pid.Field(1, "1");
                pid.Field(3, dataR["member_id"].ToString());
                pid.Field(5, dataR["patient_name"].ToString()); //add in data rows *****
                pid.Field(13, dataR["patient_cell_phone"].ToString());
                pid.Field(14, dataR["patient_work_phone"].ToString());
                pid.Field(7, DOB.ToString("yyyyMMddhhmm"));
                pid.Field(8, dataR["patient_sex_value"].ToString());
                pid.Field(11, dataR["patient_add_street_1"].ToString() + "," + dataR["patient_add_city"].ToString() + "^OK^" + dataR["patient_add_zipcode"].ToString());

                Response.Add(pid);

                var obr = new Segment("OBR");
                obr.Field(2, uniqueID);
                obr.Field(4, "Universal Service ID");

                Response.Add(obr);

                var orc = new Segment("ORC");
                orc.Field(12, dataR["provider_name"].ToString());
                orc.Field(23, dataR["provider_phone"].ToString());
                orc.Field(13, dataR["provider_name"].ToString());
                orc.Field(22, dataR["prov_address_str"].ToString());

                Response.Add(orc);

                var obx = new Segment("OBX");
                obx.Field(1, "1");
                obx.Field(2, "ST");
                obx.Field(3, "BCT^Best Contact Time");
                obx.Field(5, dataR["best_time"].ToString());

                Response.Add(obx);


                var obx2 = new Segment("OBX");
                obx2.Field(1, "2");
                obx2.Field(2, "ST");
                obx2.Field(3, "PPH^Preferred Phone Number");
                obx2.Field(5, dataR["best_contact"].ToString());

                Response.Add(obx2);

                output = Response.Serialize();

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"log.txt", true))
                {
                    file.WriteLine(DateTime.Now.ToShortDateString() + ":" + "created " + dataR["member_id"].ToString());
                }

            }
            
            //add POST endpoint url
            string URI = "https://endpointURL";


            //the certificate location can be specified below
            X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certCollection = certStore.Certificates.Find(
                X509FindType.FindByIssuerName, "CERTIFICATE ISSUER", false);

            X509Certificate2 cert;

            if (certCollection.Count > 0)
            {
                Console.WriteLine("Certificate being used");
                 cert = certCollection[0];
            }
            else
            {
                Console.WriteLine("Certificate not being used");
                cert = new X509Certificate2();
            }

            var request = (HttpWebRequest)WebRequest.Create(URI);
            request.Method = "POST";
            request.ClientCertificates.Add(cert);


            //WRAP PIPE DELIMITED MESSAGE IN CDATA TAGS
            byte[] stringData;
            string cDataString = "<![CDATA[" + output + "]]>";
            stringData = Encoding.UTF8.GetBytes(cDataString);

            //REQUEST HEADERS
            request.ContentType = "application/hl7-v2 HL7v2 pipe delimited";
            request.ContentLength = stringData.Length;

            //POST TO URL
            Stream datastream = request.GetRequestStream();
            datastream.Write(stringData, 0, stringData.Length);
            WebResponse response = request.GetResponse();
            

            //WRITE RESPONSE TO FILE
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"log.txt", true))
            {
                file.WriteLine(DateTime.Now.ToShortDateString() + ":" + "uploaded with message " + ((HttpWebResponse)response).StatusDescription);
                datastream = response.GetResponseStream();
                StreamReader reader = new StreamReader(datastream);
                string responsefromserver = reader.ReadToEnd();
                file.WriteLine(responsefromserver);
                reader.Close();
                datastream.Close();
                response.Close();
            }

            Console.ReadKey();
        }
      

        
    }
 
    
}
