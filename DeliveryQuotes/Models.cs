namespace DeliveryQuotes;

public record DeliveryRequestRaw(
    string? SenderCity,
    string? ReceiverCity,
    string? DistanceKm,
    string? WeightKg,
    string? IsFragile
);

public record DeliveryRequest(
    string SenderCity,
    string ReceiverCity,
    double DistanceKm,
    double WeightKg,
    bool IsFragile
);

public record DeliveryQuote(
    string SenderCity,
    string ReceiverCity,
    double DistanceKm,
    double WeightKg,
    bool IsFragile,
    double Cost,
    string PolicyName
);

public record DeliveryReport(
    IReadOnlyList<DeliveryQuote> Quotes,
    int TotalOrders,
    double TotalCost
);
