namespace TradecloudService.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json;

    public class TradecloudPurchaseOrderRequestModel
    {
        [Required]
        [JsonProperty("order")]
        public TradecloudOrder Order { get; set; }

        [Required]
        [JsonProperty("lines")]
        public List<TradecloudOrderLine> Lines { get; set; }

        [JsonProperty("erpIssueDateTime")]
        public DateTime? ErpIssueDateTime { get; set; }

        [JsonProperty("erpIssuedBy")]
        public Contact ErpIssuedBy { get; set; }

        [JsonProperty("erpLastChangeDateTime")]
        public DateTime? ErpLastChangeDateTime { get; set; }

        [JsonProperty("erpLastChangedBy")]
        public Contact ErpLastChangedBy { get; set; }
    }

    public class TradecloudOrder
    {
        [Required]
        [JsonProperty("companyId")]
        public string CompanyId { get; set; }

        [Required]
        [JsonProperty("supplierAccountNumber")]
        public string SupplierAccountNumber { get; set; }

        [Required]
        [JsonProperty("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("destination")]
        public TradecloudDestination Destination { get; set; }

        [JsonProperty("terms")]
        public TradecloudTerms Terms { get; set; }

        [JsonProperty("indicators")]
        public TradecloudIndicators Indicators { get; set; }

        [JsonProperty("properties")]
        public List<Property> Properties { get; set; }

        [JsonProperty("notes")]
        public List<string> Notes { get; set; }

        [JsonProperty("labels")]
        public List<string> Labels { get; set; }

        [JsonProperty("contact")]
        public Contact Contact { get; set; }

        [JsonProperty("supplierContact")]
        public Contact SupplierContact { get; set; }

        [JsonProperty("orderType")]
        public string OrderType { get; set; } = "Purchase";
    }

    public class TradecloudDestination
    {
        [Required]
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("names")]
        public List<string> Names { get; set; }

        [JsonProperty("addressLines")]
        public List<string> AddressLines { get; set; }

        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("countryCodeIso2")]
        public string CountryCodeIso2 { get; set; }

        [JsonProperty("locationType")]
        public string LocationType { get; set; }

        [JsonProperty("countryName")]
        public string CountryName { get; set; }
    }

    public class TradecloudIndicators
    {
        [JsonProperty("confirmed")]
        public bool Confirmed { get; set; }

        [JsonProperty("shipped")]
        public bool Shipped { get; set; }

        [JsonProperty("delivered")]
        public bool Delivered { get; set; }

        [JsonProperty("completed")]
        public bool Completed { get; set; }

        [JsonProperty("cancelled")]
        public bool Cancelled { get; set; }

        [JsonProperty("cancelLineWhenMissing")]
        public bool CancelLineWhenMissing { get; set; }

        [JsonProperty("autoConfirm")]
        public bool AutoConfirm { get; set; }
    }

    public class Property
    {
        [Required]
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class Contact
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }

    public class TradecloudTerms
    {
        [JsonProperty("incoterms")]
        public string Incoterms { get; set; }

        [JsonProperty("incotermsCode")]
        public string IncotermsCode { get; set; }

        [JsonProperty("paymentTerms")]
        public string PaymentTerms { get; set; }

        [JsonProperty("paymentTermsCode")]
        public string PaymentTermsCode { get; set; }
    }

    public class TradecloudDelivery
    {
        [JsonProperty("deliveryAddress")]
        public TradecloudAddress DeliveryAddress { get; set; }
    }

    public class TradecloudLocation
    {
        [JsonProperty("locationType")]
        public string LocationType { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("address")]
        public TradecloudAddress Address { get; set; }
    }

    public class TradecloudAddress
    {
        [JsonProperty("addressLine")]
        public string AddressLine { get; set; }

        [JsonProperty("houseNumber")]
        public string HouseNumber { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }

        [JsonProperty("countryCodeIso2")]
        public string CountryCodeIso2 { get; set; }
    }

    public class TradecloudOrderLine
    {
        [Required]
        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("row")]
        public string Row { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [Required]
        [JsonProperty("item")]
        public TradecloudItem Item { get; set; }

        [JsonProperty("prices")]
        public TradecloudPrices Prices { get; set; }

        [JsonProperty("terms")]
        public TradecloudLineTerms Terms { get; set; }

        [Required]
        [JsonProperty("deliverySchedule")]
        public List<TradecloudDeliverySchedule> DeliverySchedule { get; set; }

        [JsonProperty("indicators")]
        public TradecloudOrderIndicators Indicators { get; set; }
    }

    public class TradecloudLineTerms
    {
        [JsonProperty("contractNumber")]
        public string ContractNumber { get; set; }

        [JsonProperty("contractPosition")]
        public string ContractPosition { get; set; }
    }

    public class TradecloudItem
    {
        [Required]
        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("purchaseUnitOfMeasureIso")]
        public string PurchaseUnitOfMeasureIso { get; set; }

        [JsonProperty("supplierItemNumber")]
        public string SupplierItemNumber { get; set; }

        [JsonProperty("revision")]
        public string Revision { get; set; }
    }

    public class TradecloudPrices
    {
        [JsonProperty("grossPrice")]
        public TradecloudPrice GrossPrice { get; set; }

        [JsonProperty("priceUnitOfMeasureIso")]
        public string PriceUnitOfMeasureIso { get; set; }

        [JsonProperty("priceUnitQuantity")]
        public decimal PriceUnitQuantity { get; set; }
    }

    public class TradecloudPrice
    {
        [JsonProperty("priceInTransactionCurrency")]
        public TradecloudPriceCurrency PriceInTransactionCurrency { get; set; }
    }

    public class TradecloudPriceCurrency
    {
        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("currencyIso")]
        public string CurrencyIso { get; set; }
    }

    public class TradecloudStatus
    {
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    public class TradecloudOrderIndicators
    {
        [JsonProperty("cancelled")]
        public bool Cancelled { get; set; }

        [JsonProperty("confirmed")]
        public bool Confirmed { get; set; }

        [JsonProperty("shipped")]
        public bool Shipped { get; set; }

        [JsonProperty("delivered")]
        public bool Delivered { get; set; }

        [JsonProperty("completed")]
        public bool Completed { get; set; }

        [JsonProperty("cancelLineWhenMissing")]
        public bool CancelLineWhenMissing { get; set; }

        [JsonProperty("autoConfirm")]
        public bool AutoConfirm { get; set; }
    }

    public class TradecloudDeliverySchedule
    {
        [Required]
        [JsonProperty("position")]
        public string Position { get; set; }

        [Required]
        [JsonProperty("date")]
        public string Date { get; set; }

        [Required]
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("transportMode")]
        public string TransportMode { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}