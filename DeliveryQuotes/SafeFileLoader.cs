using System.Globalization;

namespace DeliveryQuotes;

public static class SafeFileLoader
{
    public static Result<List<DeliveryRequestRaw>> LoadWithStream(string filePath)
    {
        if (!File.Exists(filePath))
            return Result<List<DeliveryRequestRaw>>.Fail($"File not found: '{filePath}'");

        var requests = new List<DeliveryRequestRaw>();

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);

            string? header = reader.ReadLine();
            if (header == null)
                return Result<List<DeliveryRequestRaw>>.Ok(requests);

            int lineNum = 1;
            while (reader.ReadLine() is { } line)
            {
                lineNum++;
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var parts = trimmed.Split(',');
                if (parts.Length != 5)
                {
                    return Result<List<DeliveryRequestRaw>>.Fail(
                        $"Malformed CSV at line {lineNum}: expected 5 columns, got {parts.Length}.");
                }

                requests.Add(new DeliveryRequestRaw(parts[0], parts[1], parts[2], parts[3], parts[4]));
            }

            return Result<List<DeliveryRequestRaw>>.Ok(requests);
        }
        catch (IOException ex)
        {
            return Result<List<DeliveryRequestRaw>>.Fail($"I/O error: {ex.Message}");
        }
    }

    public static void ExportReport(string filePath, List<DeliveryQuote> quotes)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream);

        writer.WriteLine("SenderCity,ReceiverCity,DistanceKm,WeightKg,IsFragile,Cost,PolicyName");
        foreach (var q in quotes)
        {
            writer.WriteLine(string.Join(",",
                q.SenderCity,
                q.ReceiverCity,
                q.DistanceKm.ToString(CultureInfo.InvariantCulture),
                q.WeightKg.ToString(CultureInfo.InvariantCulture),
                q.IsFragile,
                q.Cost.ToString(CultureInfo.InvariantCulture),
                q.PolicyName));
        }
    }
}
