using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Services.Pricing;

public interface ITradePricing
{
    TradeType TradeType { get; }
    decimal UnitPrice(NodePricing pricing);
}

public class ExportPricing : ITradePricing
{
    // Prosumer exports (sells) to station, so the station's buy price applies
    public TradeType TradeType => TradeType.Export;
    public decimal UnitPrice(NodePricing pricing) => pricing.BuyPricePerKwh;
}

public class ImportPricing : ITradePricing
{
    // Prosumer imports (buys) from station, so the station's sell price applies
    public TradeType TradeType => TradeType.Import;
    public decimal UnitPrice(NodePricing pricing) => pricing.SellPricePerKwh;
}
