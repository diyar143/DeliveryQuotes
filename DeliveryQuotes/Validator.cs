using System.Globalization;

namespace DeliveryQuotes;

public static class Validator
{
    public static (DeliveryRequest? Request, string? Error) Validate(DeliveryRequestRaw raw)
    {
        if (string.IsNullOrWhiteSpace(raw.SenderCity))
            return (null, "Sender city is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.ReceiverCity))
            return (null, "Receiver city is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.DistanceKm))
            return (null, "Distance is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.WeightKg))
            return (null, "Weight is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.IsFragile))
            return (null, "Fragile flag is missing or empty.");

        if (!double.TryParse(raw.DistanceKm, NumberStyles.Any, CultureInfo.InvariantCulture, out double distance))
            return (null, $"Distance '{raw.DistanceKm}' is not a valid number.");

        if (distance < 0)
            return (null, $"Distance cannot be negative ({distance} km).");

        if (!double.TryParse(raw.WeightKg, NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
            return (null, $"Weight '{raw.WeightKg}' is not a valid number.");

        if (weight <= 0)
            return (null, $"Weight must be positive ({weight} kg).");

        if (!bool.TryParse(raw.IsFragile, out bool isFragile))
            return (null, $"Fragile '{raw.IsFragile}' is not a valid boolean (use true/false).");

        var request = new DeliveryRequest(
            raw.SenderCity.Trim(),
            raw.ReceiverCity.Trim(),
            distance,
            weight,
            isFragile
        );

        return (request, null);
    }

    public static (List<DeliveryRequest> Valid, List<string> Errors) ValidateAll(
        IReadOnlyList<DeliveryRequestRaw> rawRequests)
    {
        var valid = new List<DeliveryRequest>();
        var errors = new List<string>();

        for (int i = 0; i < rawRequests.Count; i++)
        {
            var (request, error) = Validate(rawRequests[i]);
            if (request != null)
            {
                valid.Add(request);
            }
            else
            {
                errors.Add($"Record {i + 1}: {error}");
            }
        }

        return (valid, errors);
    }
}
