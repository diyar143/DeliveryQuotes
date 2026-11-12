using System.Runtime.InteropServices;

namespace DeliveryQuotes;

public static class NativeFee
{
    [DllImport("delivery_fee", CallingConvention = CallingConvention.Cdecl)]
    private static extern int calculate_delivery_fee(int distance_km, int weight_kg, int is_fragile);

    public static Result<int> Calculate(int distanceKm, int weightKg, bool isFragile)
    {
        if (distanceKm <= 0)
            return Result<int>.Fail("Distance must be positive for native fee.");
        if (weightKg <= 0)
            return Result<int>.Fail("Weight must be positive for native fee.");

        try
        {
            int fragileFlag = isFragile ? 1 : 0;
            int fee = calculate_delivery_fee(distanceKm, weightKg, fragileFlag);

            if (fee < 0)
                return Result<int>.Fail("Native library returned an error code.");

            return Result<int>.Ok(fee);
        }
        catch (DllNotFoundException)
        {
            return Result<int>.Fail("Native library 'delivery_fee' not found. Build it first with native/build.bat.");
        }
        catch (EntryPointNotFoundException)
        {
            return Result<int>.Fail("Function 'calculate_delivery_fee' not found in native library.");
        }
    }
}
