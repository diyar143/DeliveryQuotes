namespace DeliveryQuotes;

public static class Reports
{
    public static List<DeliveryQuote> CostByRoute(List<DeliveryQuote> quotes)
    {
        return quotes
            .Where(q => q.Cost > 0)
            .OrderByDescending(q => q.Cost)
            .ThenBy(q => q.SenderCity)
            .ThenBy(q => q.ReceiverCity)
            .ToList();
    }

    public record CitySummary(string City, int OrderCount, double TotalCost);

    public static List<CitySummary> TotalsBySenderCity(List<DeliveryQuote> quotes)
    {
        return quotes
            .GroupBy(q => q.SenderCity)
            .Select(g => new CitySummary(
                g.Key,
                g.Count(),
                g.Sum(q => q.Cost)
            ))
            .OrderByDescending(s => s.TotalCost)
            .ThenBy(s => s.City)
            .ToList();
    }
}
