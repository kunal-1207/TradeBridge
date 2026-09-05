namespace TradeBridge.Contracts.Risk;

public class RiskCheckResponse
{
    public bool Approved { get; set; }
    public string? Reason { get; set; }
}
