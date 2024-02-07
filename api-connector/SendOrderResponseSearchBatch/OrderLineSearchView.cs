namespace Com.Tradecloud1.SDK.SendOrderResponseSearchBatch;

using System.Collections.Generic;

public class OrderLineSearchView
{
    public List<OrderLineView> Data { get; set; }
    public int Total { get; set; }
}

public class OrderLineView
{
    public string Id { get; set; }
    public string OrderId { get; set; }
    public BuyerOrder BuyerOrder { get; set; }
    public SupplierOrder SupplierOrder { get; set; }
    public BuyerLine BuyerLine { get; set; }
    public SupplierLine SupplierLine { get; set; }
    public List<DeliveryLine> DeliverySchedule { get; set; }
    public Prices Prices { get; set; }
    public LineStatus Status { get; set; }
}


public class BuyerOrder
{
    public string CompanyId { get; set; }
    public string purchaseOrderNumber { get; set; }
}

public class SupplierOrder
{
    public string CompanyId { get; set; }
}

public class BuyerLine
{
    public string Position { get; set; }
}

public class SupplierLine
{
    public string SalesOrderNumber { get; set; }
    public string Position { get; set; }
}

public class LineStatus
{
    public string ProcessStatus { get; set; }
    public string LogisticsStatus { get; set; }
}