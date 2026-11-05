using System.Globalization;

namespace DeliveryQuotes;

public static class Validator
{
    public static Result<DeliveryRequest> ValidateRaw(DeliveryRequestRaw raw)
    {
        if (string.IsNullOrWhiteSpace(raw.SenderCity))
            return Result<DeliveryRequest>.Fail("Sender city is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.ReceiverCity))
            return Result<DeliveryRequest>.Fail("Receiver city is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.DistanceKm))
            return Result<DeliveryRequest>.Fail("Distance is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.WeightKg))
            return Result<DeliveryRequest>.Fail("Weight is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.IsFragile))
            return Result<DeliveryRequest>.Fail("Fragile flag is missing or empty.");

        if (!double.TryParse(raw.DistanceKm, NumberStyles.Any, CultureInfo.InvariantCulture, out double distance))
            return Result<DeliveryRequest>.Fail($"Distance '{raw.DistanceKm}' is not a valid number.");

        if (distance < 0)
            return Result<DeliveryRequest>.Fail($"Distance cannot be negative ({distance} km).");

        if (!double.TryParse(raw.WeightKg, NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
            return Result<DeliveryRequest>.Fail($"Weight '{raw.WeightKg}' is not a valid number.");

        if (weight <= 0)
            return Result<DeliveryRequest>.Fail($"Weight must be positive ({weight} kg).");

        if (!bool.TryParse(raw.IsFragile, out bool isFragile))
            return Result<DeliveryRequest>.Fail($"Fragile '{raw.IsFragile}' is not a valid boolean (use true/false).");

        return Result<DeliveryRequest>.Ok(new DeliveryRequest(
            raw.SenderCity.Trim(),
            raw.ReceiverCity.Trim(),
            distance,
            weight,
            isFragile
        ));
    }

    public static (DeliveryRequest? Request, string? Error) Validate(DeliveryRequestRaw raw)
    {
        var result = ValidateRaw(raw);
        return result switch
        {
            Result<DeliveryRequest>.Success s => (s.Value, null),
            Result<DeliveryRequest>.Failure f => (null, f.Error),
            _ => throw new InvalidOperationException()
        };
    }

    public static (List<DeliveryRequest> Valid, List<string> Errors) ValidateAll(
        IReadOnlyList<DeliveryRequestRaw> rawRequests)
    {
        var valid = new List<DeliveryRequest>();
        var errors = new List<string>();

        for (int i = 0; i < rawRequests.Count; i++)
        {
            var result = ValidateRaw(rawRequests[i]);
            if (result is Result<DeliveryRequest>.Success s)
            {
                valid.Add(s.Value);
            }
            else if (result is Result<DeliveryRequest>.Failure f)
            {
                errors.Add($"Record {i + 1}: {f.Error}");
            }
        }

        return (valid, errors);
    }
}
