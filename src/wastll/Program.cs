using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Client;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient.Protocol;
using System.Net;
using Common.Logging;
using NDesk.Options;
using System.IO;

namespace wastll
{
    partial class Program
    {
        static readonly ILog logger = LogManager.GetCurrentClassLogger();

        private StreamReader GetTableStream(CloudStorageAccount account, string tableName, string query)
        {
            var requestUri = new StringBuilder();

            requestUri.AppendFormat("{0}{1}()", account.TableEndpoint.ToString(), tableName);
            if (!String.IsNullOrWhiteSpace(query))
            {
                requestUri.AppendFormat("?{0}", query);
            }

            // create Http Request
            var request = (HttpWebRequest)HttpWebRequest.Create(requestUri.ToString());

            // signs request using the specified credentials under the Shared Key Lite authentication
            account.Credentials.SignRequestLite(request);

            var response = (HttpWebResponse)request.GetResponse();

            var sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(response.CharacterSet));

            return sr;
        }


        void Query(CloudStorageAccount account)
        {
            using (var stream = GetTableStream(account, "EntityOne", @"$filter=(PartitionKey eq 'p0000') and (RowKey eq 'r0000')"))
            {
                var rb = new Char[1024 * 4];

                int count = stream.Read(rb, 0, rb.Length);

                while (count > 0)
                {
                    var s = new String(rb, 0, count);
                    logger.Info(s);
                    count = stream.Read(rb, 0, rb.Length);
                }

                stream.Close();
            }
        }


        public static void Main(string[] args)
        {

            var showHelp = false;
            CloudStorageAccount account = null;

            var program = new Program();

            var p = new OptionSet() {
            {
                "c|connection=", "connection string", 
                v => account = CloudStorageAccount.Parse(v)
            },
            {
                "h|help",  "show this message and exit",
                v => showHelp = v != null 
            }
            };

            try
            {
                var extra = p.Parse(args);

                if (showHelp)
                {
                    ShowHelp(p);
                }
                else
                {
                    program.Query(account);
                }
            }
            catch (OptionException e)
            {
                System.Console.Write("wasmetrics: ");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine("Try `wasmetrics --help' for more information.");
            }

        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: wasmetrics [OPTIONS]+ message");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

    }
}
