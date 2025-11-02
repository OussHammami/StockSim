namespace StockSim.Web.Demo
{
    public sealed class DemoSeedOptions
    {
        public bool Enabled { get; set; } = true;            // only used in Development
        public string DemoUserSubject { get; set; } = "demo@stocksim.local";
        public decimal InitialCash { get; set; } = 10_000m;
        public string[] Symbols { get; set; } = ["AAPL", "MSFT", "GOOG", "AMZN"];
        public decimal DefaultQty { get; set; } = 5m;
        public decimal DefaultLimit { get; set; } = 100m;
    }
}
