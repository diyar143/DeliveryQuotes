namespace DeliveryQuotes;
public static class PricingPolicies
{
    public static double StandardPolicy(DeliveryRequest request)
    {
        double cost = 500 + (100 * request.DistanceKm) + (50 * request.WeightKg);

        if (request.IsFragile)
        {
            cost += 200;
        }

        return cost;
    }
    public static double ExpressPolicy(DeliveryRequest request)
    {
        double cost = 800 + (150 * request.DistanceKm) + (75 * request.WeightKg);

        if (request.IsFragile)
        {
            cost += 400;
        }

        return cost;
    }
}
