namespace DeliveryQuotes;

public static class CityCatalog
{
    private static readonly Dictionary<string, CityInfo> ServicedCities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Almaty"] = new CityInfo("Almaty", "South", false),
        ["Astana"] = new CityInfo("Astana", "North", false),
        ["Shymkent"] = new CityInfo("Shymkent", "South", false),
        ["Karaganda"] = new CityInfo("Karaganda", "Central", false),
        ["Aktau"] = new CityInfo("Aktau", "West", false)
    };

    private static readonly Dictionary<string, double> CityDiscounts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Astana"] = 0.10,
        ["Almaty"] = 0.0
    };

    public static Maybe<CityInfo> FindCity(string cityName)
    {
        if (!string.IsNullOrWhiteSpace(cityName) && ServicedCities.TryGetValue(cityName.Trim(), out var info))
        {
            return Maybe<CityInfo>.Just(info);
        }

        return Maybe<CityInfo>.Empty();
    }

    public static Maybe<double> GetCityDiscountPercent(string cityName)
    {
        if (!string.IsNullOrWhiteSpace(cityName) && CityDiscounts.TryGetValue(cityName.Trim(), out var discount))
        {
            return Maybe<double>.Just(discount);
        }

        return Maybe<double>.Empty();
    }

    public static Result<DeliveryRequest> VerifyServicedRoute(DeliveryRequest request)
    {
        if (string.Equals(request.SenderCity, request.ReceiverCity, StringComparison.OrdinalIgnoreCase))
        {
            return Result<DeliveryRequest>.Fail($"Sender and destination cities cannot be identical: '{request.SenderCity}'.");
        }

        var senderResult = FindCity(request.SenderCity)
            .ToResult($"Sender city '{request.SenderCity}' is not in the serviced network.");
        if (senderResult is Result<CityInfo>.Failure senderFail)
        {
            return Result<DeliveryRequest>.Fail(senderFail.Error);
        }

        var receiverResult = FindCity(request.ReceiverCity)
            .ToResult($"Destination city '{request.ReceiverCity}' is not in the serviced network.");
        if (receiverResult is Result<CityInfo>.Failure receiverFail)
        {
            return Result<DeliveryRequest>.Fail(receiverFail.Error);
        }

        return Result<DeliveryRequest>.Ok(request);
    }
}
