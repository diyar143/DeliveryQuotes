namespace DeliveryQuotes;

public static class LazyPipeline
{
    public static int ProcessedCount;

    public static IEnumerable<DeliveryQuote> GenerateQuotes(
        IReadOnlyList<DeliveryRequest> requests,
        Func<DeliveryRequest, double> pricingPolicy,
        string policyName)
    {
        foreach (var req in requests)
        {
            if (req.DistanceKm <= 0)
                continue;

            ProcessedCount++;
            yield return Pipeline.CreateQuote(req, pricingPolicy, policyName);
        }
    }
}
