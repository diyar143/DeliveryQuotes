namespace DeliveryQuotes;

public static class Currying
{
    public static Func<DeliveryRequest, double> CreateScorer(
        double baseFee, double distanceRate, double weightRate, double fragileSurcharge)
    {
        return (DeliveryRequest req) =>
        {
            double cost = baseFee + (distanceRate * req.DistanceKm) + (weightRate * req.WeightKg);
            if (req.IsFragile)
                cost += fragileSurcharge;
            return cost;
        };
    }

    public static readonly Func<DeliveryRequest, double> EconomyScorer =
        CreateScorer(300, 80, 40, 100);

    public static readonly Func<DeliveryRequest, double> PremiumScorer =
        CreateScorer(1000, 200, 100, 500);
}
