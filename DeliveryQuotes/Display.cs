namespace DeliveryQuotes;

public static class Display
{
    public static void PrintReport(DeliveryReport report, string title)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine($"  {title}");
        Console.WriteLine("========================================");

        if (report.TotalOrders == 0)
        {
            Console.WriteLine("  No delivery quotes to display.");
            Console.WriteLine($"  Total orders: {report.TotalOrders}");
            Console.WriteLine($"  Total cost: {report.TotalCost}");
            Console.WriteLine("========================================");
            return;
        }

        foreach (var quote in report.Quotes)
        {
            Console.WriteLine($"  {quote.SenderCity} -> {quote.ReceiverCity}");
            Console.WriteLine($"    Distance: {quote.DistanceKm} km, Weight: {quote.WeightKg} kg, Fragile: {quote.IsFragile}");
            Console.WriteLine($"    Cost: {quote.Cost} ({quote.PolicyName})");
            Console.WriteLine();
        }

        Console.WriteLine($"  Total orders: {report.TotalOrders}");
        Console.WriteLine($"  Total cost: {report.TotalCost}");
        Console.WriteLine("========================================");
    }

    public static void PrintErrors(List<string> errors)
    {
        if (errors.Count == 0)
            return;

        Console.WriteLine();
        Console.WriteLine("--- Validation Errors ---");
        foreach (var error in errors)
        {
            Console.WriteLine($"  [!] {error}");
        }
    }
}
