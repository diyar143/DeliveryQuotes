namespace DeliveryQuotes;

public static class Pipeline
{
    public static Result<DeliveryRequest> CheckBusinessRules(DeliveryRequest req)
    {
        if (req.DistanceKm <= 0)
        {
            return Result<DeliveryRequest>.Fail("Distance must be greater than zero.");
        }

        if (req.WeightKg > 50.0)
        {
            return Result<DeliveryRequest>.Fail($"Weight {req.WeightKg} kg exceeds the maximum allowable package limit of 50 kg.");
        }

        if (req.IsFragile && req.DistanceKm > 300.0)
        {
            return Result<DeliveryRequest>.Fail($"Fragile items cannot be transported on routes exceeding 300 km (requested: {req.DistanceKm} km).");
        }

        return Result<DeliveryRequest>.Ok(req);
    }

    public static DeliveryQuote CreateQuote(
        DeliveryRequest req,
        Func<DeliveryRequest, double> pricingPolicy,
        string policyName)
    {
        double cost = pricingPolicy(req);

        return new DeliveryQuote(
            req.SenderCity,
            req.ReceiverCity,
            req.DistanceKm,
            req.WeightKg,
            req.IsFragile,
            cost,
            policyName
        );
    }

    public static DeliveryQuote ApplyOptionalDiscount(DeliveryQuote quote, Maybe<double> discountMaybe)
    {
        double discountRate = discountMaybe.GetOrElse(0.0);
        double discountedCost = quote.Cost - (quote.Cost * discountRate);

        return quote with { Cost = discountedCost };
    }

    public static Result<DeliveryQuote> ProcessRequest(
        DeliveryRequestRaw raw,
        Func<DeliveryRequest, double> pricingPolicy,
        string policyName)
    {
        return Validator.ValidateRaw(raw)
            .Bind(CityCatalog.VerifyServicedRoute)
            .Bind(CheckBusinessRules)
            .Map(req => CreateQuote(req, pricingPolicy, policyName));
    }

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
            quotes.Add(CreateQuote(req, pricingPolicy, policyName));
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
