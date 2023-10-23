using Microsoft.AspNetCore.Mvc;

[Route("api")]
public class HelloWorldController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "Tradecloud Business Central Connector API";
    }
}