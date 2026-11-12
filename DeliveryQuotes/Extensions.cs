namespace DeliveryQuotes;

public static class Extensions
{
    public static Result<U> ThenValidate<U>(this Result<U> result, Func<U, Result<U>> rule)
    {
        return result.Bind(rule);
    }

    public static string ToSummary(this DeliveryQuote quote)
    {
        string fragileTag = quote.IsFragile ? " [FRAGILE]" : "";
        return $"{quote.SenderCity} -> {quote.ReceiverCity}: {quote.Cost:F0} tg ({quote.PolicyName}){fragileTag}";
    }
}
