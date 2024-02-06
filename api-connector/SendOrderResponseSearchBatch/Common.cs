namespace Com.Tradecloud1.SDK.SendOrderResponseSearchBatch;

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
