namespace DeliveryQuotes;

public static class Pipeline
{

    public static List<DeliveryRequest> FilterValidRequests(List<DeliveryRequest> requests)
    {
        var result = new List<DeliveryRequest>();

        foreach (var req in requests)
        {
            if (req.DistanceKm > 0)
            {
                result.Add(req);
            }
        }

        return result;
    }
    public static List<DeliveryQuote> CalculateCosts(
        List<DeliveryRequest> requests,
        Func<DeliveryRequest, double> pricingPolicy,
        string policyName)
    {
        var quotes = new List<DeliveryQuote>();

        foreach (var req in requests)
        {
            double cost = pricingPolicy(req);

            var quote = new DeliveryQuote(
                req.SenderCity,
                req.ReceiverCity,
                req.DistanceKm,
                req.WeightKg,
                req.IsFragile,
                cost,
                policyName
            );

            quotes.Add(quote);
        }

        return quotes;
    }

    public static DeliveryReport BuildReport(List<DeliveryQuote> quotes)
    {
        var sorted = new List<DeliveryQuote>(quotes);
        sorted.Sort((a, b) => b.Cost.CompareTo(a.Cost));

        double totalCost = 0;
        foreach (var q in sorted)
        {
            totalCost += q.Cost;
        }

        return new DeliveryReport(sorted, sorted.Count, totalCost);
    }

    public static DeliveryReport RunPipeline(
        List<DeliveryRequest> validRequests,
        Func<DeliveryRequest, double> pricingPolicy,
        string policyName)
    {
        var filtered = FilterValidRequests(validRequests);

        var quotes = CalculateCosts(filtered, pricingPolicy, policyName);

        var report = BuildReport(quotes);

        return report;
    }
}
