using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class CleanCsvOrderLines
    {   
        const bool dryRun = true;
        const string esUrlTemplate = "http://localhost:9200/prod-order-line/_doc/{lineId}";
       
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud clean order lines based on CSV file.");
            
            HttpClient httpClient = new HttpClient();
            
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { IgnoreBlankLines = true, Encoding = Encoding.UTF8 };

            using (var reader = new StreamReader("order_lines.csv"))
            using (var csvReader = new CsvReader(reader, config))
            {
                csvReader.Context.RegisterClassMap<OrderLineMap>();
                var orderLines = csvReader.GetRecords<OrderLine>();
                foreach (var line in orderLines)
                {                  
                    await DeleteOrderLine(line.lineId.Trim());
                }
            }
            
            async Task DeleteOrderLine(string lineId)
            {                
                var url = esUrlTemplate.Replace("{lineId}", lineId);                

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();        
            
                if (dryRun)
                {
                    var response = await httpClient.GetAsync(url);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    Console.WriteLine("DeleteOrderLine dry run " + lineId + " start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase + " url=" + url);

                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode == 200) {
                        Console.WriteLine("DeleteOrderLine dry run response body=" +  responseString);                    
                    }
                } 
                else
                {
                    var response = await httpClient.DeleteAsync(url);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    Console.WriteLine("DeleteOrderLine " + lineId + "  start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200  && statusCode != 404) {
                        Console.WriteLine("DeleteOrderLine response body=" +  responseString);                    
                    }
                }
            }
        }
    }

    public class OrderLine
    {
        public string lineId { get; set; }
    }

    public sealed class OrderLineMap : ClassMap<OrderLine>
    {
        public OrderLineMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
