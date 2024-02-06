namespace Com.Tradecloud1.SDK.SendOrderResponseSearchBatch;

public class DeliveryLine
{
    public string Position { get; set; }
    public string Date { get; set; }
    public decimal Quantity { get; set; }
}

public class Prices
{
    public Price GrossPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public Price NetPrice { get; set; }
    public string PriceUnitOfMeasureIso { get; set; }
    public decimal PriceUnitQuantity { get; set; }
}

public class Price
{
    public Money PriceInTransactionCurrency { get; set; }
    public Money PriceInBaseCurrency { get; set; }
}

public class Money
{
    public decimal Value { get; set; }
    public string CurrencyIso { get; set; }
}
