using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TradecloudService.Models;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrderModel
    {
        const string accessToken = "";
        static readonly HttpClient httpClient = new HttpClient();

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderByBuyerRoute
        const string sendOrderUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order";

        // Custom date time converter for ErpIssueDateTime fields
        class ErpDateTimeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                string value = reader.Value?.ToString();
                if (string.IsNullOrEmpty(value))
                    return null;

                return DateTime.Parse(value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                DateTime dateTime = (DateTime)value;
                writer.WriteValue(dateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order model example.");

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Parse command line args
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "basic":
                        await SendBasicOrderExample();
                        break;
                    case "complete":
                        await SendCompleteOrderExample();
                        break;
                    case "batch":
                        await SendOrderWithBatchLinesExample();
                        break;
                    case "sample":
                        await SendSampleOrderExample();
                        break;
                    default:
                        Console.WriteLine("Unknown example type. Available options: basic, complete, batch, sample");
                        break;
                }
            }
            else
            {
                // No arguments provided, run sample order example
                await SendSampleOrderExample();
            }
        }

        static async Task SendBasicOrderExample()
        {
            // Create a sample purchase order using the factory
            var purchaseOrder = OrderModelFactory.CreateBasicOrder(
                companyId: "f56aa4ce-8ec8-5197-bc26-77716a58add7",
                purchaseOrderNumber: "PO-12345",
                supplierAccountNumber: "540830",
                description: "Test Order");

            // Add more order details
            OrderModelFactory.AddDestination(
                purchaseOrder.Order,
                code: "MAIN-WAREHOUSE",
                city: "Amsterdam",
                countryCodeIso2: "NL",
                postalCode: "1000AA",
                addressLines: new List<string> { "123 Main Street" });

            OrderModelFactory.AddContact(purchaseOrder.Order, "buyer@example.com");
            OrderModelFactory.AddSupplierContact(purchaseOrder.Order, "supplier@example.com");
            OrderModelFactory.AddTerms(purchaseOrder.Order, incoterms: "Delivered At Place", incotermsCode: "DAP");

            // Add order lines
            OrderModelFactory.AddOrderLine(
                purchaseOrder,
                position: "10",
                itemNumber: "ITEM-001",
                itemName: "Widget A",
                quantity: 10,
                price: 99.95m);

            OrderModelFactory.AddOrderLine(
                purchaseOrder,
                position: "20",
                itemNumber: "ITEM-002",
                itemName: "Widget B",
                quantity: 5,
                price: 149.50m);

            await SendOrderToTradecloud(purchaseOrder, "Basic Order Example");
        }

        static async Task SendCompleteOrderExample()
        {
            // Create a complete order with defaults
            var purchaseOrder = OrderModelFactory.CreateCompleteOrder(
                companyId: "f56aa4ce-8ec8-5197-bc26-77716a58add7",
                purchaseOrderNumber: "PO-67890",
                supplierAccountNumber: "540830",
                buyerEmail: "buyer@example.com",
                supplierEmail: "supplier@example.com",
                description: "Complete Order Example");

            // Add just the lines, other details already set with defaults
            OrderModelFactory.AddOrderLine(
                purchaseOrder,
                position: "10",
                itemNumber: "ITEM-003",
                itemName: "Product X",
                quantity: 15);

            await SendOrderToTradecloud(purchaseOrder, "Complete Order Example");
        }

        static async Task SendOrderWithBatchLinesExample()
        {
            // Create basic order
            var purchaseOrder = OrderModelFactory.CreateBasicOrder(
                companyId: "f56aa4ce-8ec8-5197-bc26-77716a58add7",
                purchaseOrderNumber: "PO-BATCH-001",
                supplierAccountNumber: "540830");

            // Add default shipping information
            OrderModelFactory.AddDestination(purchaseOrder.Order, code: "WAREHOUSE-B");

            // Add multiple lines at once
            var orderLines = new List<(string position, string itemNumber, string itemName, decimal quantity, decimal price)>
            {
                (position: "10", itemNumber: "BATCH-001", itemName: "Batch Item 1", quantity: 5, price: 10.99m),
                (position: "20", itemNumber: "BATCH-002", itemName: "Batch Item 2", quantity: 10, price: 25.50m),
                (position: "30", itemNumber: "BATCH-003", itemName: "Batch Item 3", quantity: 2, price: 199.99m),
                (position: "40", itemNumber: "BATCH-004", itemName: "Batch Item 4", quantity: 8, price: 55.00m)
            };

            OrderModelFactory.AddOrderLines(
                purchaseOrder,
                orderLines,
                deliveryDate: DateTime.Now.AddDays(15).ToString(OrderModelFactory.Defaults.DateFormat));

            await SendOrderToTradecloud(purchaseOrder, "Batch Lines Example");
        }

        static async Task SendSampleOrderExample()
        {
            // Create a fully populated sample order with timestamp-based order number
            var purchaseOrder = OrderModelFactory.CreateSampleOrder(
                companyId: "f56aa4ce-8ec8-5197-bc26-77716a58add7");

            // You can still modify the sample order if needed
            // For example, add an additional line item
            OrderModelFactory.AddOrderLine(
                purchaseOrder,
                position: "40",
                itemNumber: "TEST-ITEM-CUSTOM",
                itemName: "Custom Test Widget",
                quantity: 1,
                price: 999.99m,
                currencyIso: "EUR"
            );

            await SendOrderToTradecloud(purchaseOrder, "Sample Order Example");
        }

        static async Task SendOrderToTradecloud(TradecloudPurchaseOrderRequestModel purchaseOrder, string exampleName)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                FloatFormatHandling = FloatFormatHandling.String,
                FloatParseHandling = FloatParseHandling.Decimal,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new ErpDateTimeConverter()
                }
            };

            var json = JsonConvert.SerializeObject(purchaseOrder, settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[{exampleName}] Sending order {purchaseOrder.Order.PurchaseOrderNumber} to Tradecloud...");
            var response = await httpClient.PostAsync(sendOrderUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to submit purchase order. Status: {response.StatusCode}, Error: {errorContent}");
            }
            else
            {
                Console.WriteLine($"Order {purchaseOrder.Order.PurchaseOrderNumber} sent successfully.");

                // Log successful response
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {responseContent}");
            }
        }
    }
}
