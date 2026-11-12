#include <stdint.h>

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT
#endif

EXPORT int32_t calculate_delivery_fee(int32_t distance_km, int32_t weight_kg, int32_t is_fragile)
{
    if (distance_km <= 0 || weight_kg <= 0)
        return -1;

    int32_t fee = 500 + (distance_km * 10) + (weight_kg * 20);

    if (is_fragile != 0)
        fee += 300;

    return fee;
}
