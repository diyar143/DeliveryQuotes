namespace DeliveryQuotes;

public static class DataLoader
{
    public static Result<List<DeliveryRequestRaw>> LoadFromCsvSafe(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Result<List<DeliveryRequestRaw>>.Fail($"File not found: '{filePath}'");
            }

            var lines = File.ReadAllLines(filePath);
            var requests = new List<DeliveryRequestRaw>();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length != 5)
                {
                    return Result<List<DeliveryRequestRaw>>.Fail(
                        $"Malformed CSV content in file '{filePath}' at line {i + 1}: expected 5 columns, got {parts.Length}.");
                }

                var raw = new DeliveryRequestRaw(
                    parts[0],
                    parts[1],
                    parts[2],
                    parts[3],
                    parts[4]
                );

                requests.Add(raw);
            }

            return Result<List<DeliveryRequestRaw>>.Ok(requests);
        }
        catch (IOException ex)
        {
            return Result<List<DeliveryRequestRaw>>.Fail($"I/O error reading file '{filePath}': {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<List<DeliveryRequestRaw>>.Fail($"Unexpected error reading file '{filePath}': {ex.Message}");
        }
    }

    public static List<DeliveryRequestRaw> LoadFromCsv(string filePath)
    {
        var result = LoadFromCsvSafe(filePath);
        return result switch
        {
            Result<List<DeliveryRequestRaw>>.Success s => s.Value,
            _ => new List<DeliveryRequestRaw>()
        };
    }
}
