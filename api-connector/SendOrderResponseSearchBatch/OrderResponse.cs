namespace Com.Tradecloud1.SDK.SendOrderResponseSearchBatch;

using System.Collections.Generic;

public class OrderResponse
{
    public OrderResponseOrder Order { get; set; }
    public List<OrderResponseLine> Lines { get; set; }
}

public class OrderResponseOrder
{
    public string CompanyId { get; set; }
    public string BuyerAccountNumber { get; set; }
    public string PurchaseOrderNumber { get; set; }
}

public class OrderResponseLine
{
    public string PurchaseOrderLinePosition { get; set; }
    public string SalesOrderNumber { get; set; }
    public string SalesOrderLinePosition { get; set; }
    public List<DeliveryLine> DeliverySchedule { get; set; }
    public Prices Prices { get; set; }
    public string Reason { get; set; }
}
