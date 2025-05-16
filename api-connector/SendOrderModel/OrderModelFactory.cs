using System;
using System.Collections.Generic;
using TradecloudService.Models;

namespace Com.Tradecloud1.SDK.Client
{
    public class OrderModelFactory
    {
        // Default values
        public static class Defaults
        {
            // Common defaults
            public const string OrderType = "Purchase";
            public const string DefaultCurrency = "EUR";
            public const string DefaultUnitOfMeasure = "PCE"; // Piece
            public const string DefaultLocationType = "DeliveryAddress";
            public const string DefaultDeliveryPosition = "1";
            public const decimal DefaultPriceUnitQuantity = 1;
            public const string DefaultIncoterms = "DAP"; // Delivered At Place
            public const string DefaultIncotermsCode = "DAP";

            // Country defaults
            public const string DefaultCountryCode = "NL";
            public const string DefaultCity = "Amsterdam";

            // Time defaults
            public static DateTime DefaultDeliveryDate => DateTime.UtcNow.AddDays(30);

            // Date formats
            public const string DateFormat = "yyyy-MM-dd";
            public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

            // Create default address lines
            public static List<string> DefaultAddressLines => new List<string> { "Default Address" };
        }

        /// <summary>
        /// Creates a simple purchase order request with minimal required fields and defaults
        /// </summary>
        public static TradecloudPurchaseOrderRequestModel CreateBasicOrder(
            string companyId,
            string purchaseOrderNumber,
            string supplierAccountNumber,
            string description = null)
        {
            return new TradecloudPurchaseOrderRequestModel
            {
                Order = new TradecloudOrder
                {
                    CompanyId = companyId,
                    PurchaseOrderNumber = purchaseOrderNumber,
                    SupplierAccountNumber = supplierAccountNumber,
                    Description = description ?? $"Order {purchaseOrderNumber}",
                    OrderType = Defaults.OrderType,
                    Indicators = new TradecloudIndicators()
                },
                Lines = new List<TradecloudOrderLine>(),
                ErpIssueDateTime = DateTime.Now
            };
        }

        /// <summary>
        /// Creates a complete purchase order with default values
        /// </summary>
        public static TradecloudPurchaseOrderRequestModel CreateCompleteOrder(
            string companyId,
            string purchaseOrderNumber,
            string supplierAccountNumber,
            string buyerEmail = null,
            string supplierEmail = null,
            string description = null)
        {
            var order = CreateBasicOrder(companyId, purchaseOrderNumber, supplierAccountNumber, description);

            // Add defaults
            AddDestination(order.Order,
                code: $"LOC-{purchaseOrderNumber}",
                city: Defaults.DefaultCity,
                countryCodeIso2: Defaults.DefaultCountryCode);

            if (!string.IsNullOrEmpty(buyerEmail))
                AddContact(order.Order, buyerEmail);

            if (!string.IsNullOrEmpty(supplierEmail))
                AddSupplierContact(order.Order, supplierEmail);

            AddTerms(order.Order,
                incoterms: Defaults.DefaultIncoterms,
                incotermsCode: Defaults.DefaultIncotermsCode);

            return order;
        }

        /// <summary>
        /// Creates a complete sample order for testing with multiple line items and all fields populated
        /// </summary>
        public static TradecloudPurchaseOrderRequestModel CreateSampleOrder(
            string companyId)
        {
            // Generate a unique PO number with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string purchaseOrderNumber = $"PO-TEST-{timestamp}";

            // Create the base order
            var order = CreateCompleteOrder(
                companyId: companyId,
                purchaseOrderNumber: purchaseOrderNumber,
                supplierAccountNumber: "540830",
                buyerEmail: "test.buyer@example.com",
                supplierEmail: "test.supplier@example.com",
                description: "Automated Sample Test Order");

            // Add additional properties
            order.Order.Properties = new List<Property>
            {
                new Property { Key = "ReferenceNumber", Value = $"REF-{timestamp}" },
                new Property { Key = "Department", Value = "Testing" },
                new Property { Key = "ProjectCode", Value = "TEST-PROJECT-001" }
            };

            order.Order.Notes = new List<string>
            {
                "This is a sample order created for testing",
                "Contact test@example.com for any questions"
            };

            order.Order.Labels = new List<string> { "TEST", "SAMPLE", "AUTOMATED" };

            // Add destination with full details
            AddDestination(
                order.Order,
                code: "WAREHOUSE-TEST",
                city: "Amsterdam",
                countryCodeIso2: "NL",
                postalCode: "1000AA",
                addressLines: new List<string> { "Test Street 123", "Test Building 45" },
                names: new List<string> { "Main Test Warehouse", "Distribution Center" }
            );

            // Add sample order lines
            AddOrderLine(
                order,
                position: "10",
                itemNumber: "TEST-ITEM-001",
                itemName: "Test Widget Alpha",
                quantity: 10,
                deliveryDate: DateTime.Now.AddDays(14).ToString(Defaults.DateFormat),
                unitOfMeasureIso: "PCE",
                price: 99.95m,
                currencyIso: "EUR"
            );

            AddOrderLine(
                order,
                position: "20",
                itemNumber: "TEST-ITEM-002",
                itemName: "Test Widget Beta",
                quantity: 5,
                deliveryDate: DateTime.Now.AddDays(21).ToString(Defaults.DateFormat),
                unitOfMeasureIso: "PCE",
                price: 149.99m,
                currencyIso: "EUR"
            );

            AddOrderLine(
                order,
                position: "30",
                itemNumber: "TEST-ITEM-003",
                itemName: "Test Widget Gamma",
                quantity: 25,
                deliveryDate: DateTime.Now.AddDays(30).ToString(Defaults.DateFormat),
                unitOfMeasureIso: "PCE",
                price: 45.50m,
                currencyIso: "EUR"
            );

            return order;
        }

        /// <summary>
        /// Add a destination to an order
        /// </summary>
        public static TradecloudOrder AddDestination(
            TradecloudOrder order,
            string code,
            string city = null,
            string countryCodeIso2 = null,
            string postalCode = null,
            List<string> addressLines = null,
            List<string> names = null)
        {
            order.Destination = new TradecloudDestination
            {
                Code = code,
                City = city ?? Defaults.DefaultCity,
                CountryCodeIso2 = countryCodeIso2 ?? Defaults.DefaultCountryCode,
                PostalCode = postalCode,
                AddressLines = addressLines ?? Defaults.DefaultAddressLines,
                Names = names,
                LocationType = Defaults.DefaultLocationType
            };
            return order;
        }

        /// <summary>
        /// Add a contact to an order
        /// </summary>
        public static TradecloudOrder AddContact(TradecloudOrder order, string email)
        {
            order.Contact = new Contact { Email = email };
            return order;
        }

        /// <summary>
        /// Add supplier contact to an order
        /// </summary>
        public static TradecloudOrder AddSupplierContact(TradecloudOrder order, string email)
        {
            order.SupplierContact = new Contact { Email = email };
            return order;
        }

        /// <summary>
        /// Add an order line to the purchase order
        /// </summary>
        public static TradecloudPurchaseOrderRequestModel AddOrderLine(
            TradecloudPurchaseOrderRequestModel purchaseOrder,
            string position,
            string itemNumber,
            string itemName,
            decimal quantity,
            string deliveryDate = null,
            string unitOfMeasureIso = null,
            decimal? price = null,
            string currencyIso = null)
        {
            var orderLine = new TradecloudOrderLine
            {
                Position = position,
                Description = itemName,
                Item = new TradecloudItem
                {
                    Number = itemNumber,
                    Name = itemName,
                    PurchaseUnitOfMeasureIso = unitOfMeasureIso ?? Defaults.DefaultUnitOfMeasure
                },
                DeliverySchedule = new List<TradecloudDeliverySchedule>
                {
                    new TradecloudDeliverySchedule
                    {
                        Position = Defaults.DefaultDeliveryPosition,
                        Date = deliveryDate ?? Defaults.DefaultDeliveryDate.ToString(Defaults.DateFormat),
                        Quantity = quantity
                    }
                },
                Indicators = new TradecloudOrderIndicators()
            };

            // Add prices if provided
            if (price.HasValue)
            {
                orderLine.Prices = new TradecloudPrices
                {
                    GrossPrice = new TradecloudPrice
                    {
                        PriceInTransactionCurrency = new TradecloudPriceCurrency
                        {
                            Value = price.Value,
                            CurrencyIso = currencyIso ?? Defaults.DefaultCurrency
                        }
                    },
                    PriceUnitOfMeasureIso = unitOfMeasureIso ?? Defaults.DefaultUnitOfMeasure,
                    PriceUnitQuantity = Defaults.DefaultPriceUnitQuantity
                };
            }

            purchaseOrder.Lines.Add(orderLine);
            return purchaseOrder;
        }

        /// <summary>
        /// Add multiple order lines in one call
        /// </summary>
        public static TradecloudPurchaseOrderRequestModel AddOrderLines(
            TradecloudPurchaseOrderRequestModel purchaseOrder,
            List<(string position, string itemNumber, string itemName, decimal quantity, decimal price)> lines,
            string deliveryDate = null,
            string unitOfMeasureIso = null,
            string currencyIso = null)
        {
            int positionCounter = 10;
            foreach (var line in lines)
            {
                string position = line.position;
                if (string.IsNullOrEmpty(position))
                {
                    position = positionCounter.ToString();
                    positionCounter += 10;
                }

                AddOrderLine(
                    purchaseOrder,
                    position: position,
                    itemNumber: line.itemNumber,
                    itemName: line.itemName,
                    quantity: line.quantity,
                    price: line.price,
                    deliveryDate: deliveryDate ?? DateTime.Now.AddDays(30).ToString(Defaults.DateFormat),
                    unitOfMeasureIso: unitOfMeasureIso,
                    currencyIso: currencyIso
                );
            }

            return purchaseOrder;
        }

        /// <summary>
        /// Set terms to an order
        /// </summary>
        public static TradecloudOrder AddTerms(
            TradecloudOrder order,
            string incoterms = null,
            string incotermsCode = null,
            string paymentTerms = null,
            string paymentTermsCode = null)
        {
            order.Terms = new TradecloudTerms
            {
                Incoterms = incoterms ?? Defaults.DefaultIncoterms,
                IncotermsCode = incotermsCode ?? Defaults.DefaultIncotermsCode,
                PaymentTerms = paymentTerms,
                PaymentTermsCode = paymentTermsCode
            };
            return order;
        }

        /// <summary>
        /// Set indicators on an order
        /// </summary>
        public static TradecloudOrder SetOrderIndicators(
            TradecloudOrder order,
            bool confirmed = false,
            bool shipped = false,
            bool delivered = false,
            bool completed = false,
            bool cancelled = false,
            bool cancelLineWhenMissing = false,
            bool autoConfirm = false)
        {
            order.Indicators = new TradecloudIndicators
            {
                Confirmed = confirmed,
                Shipped = shipped,
                Delivered = delivered,
                Completed = completed,
                Cancelled = cancelled,
                CancelLineWhenMissing = cancelLineWhenMissing,
                AutoConfirm = autoConfirm
            };

            return order;
        }
    }
}