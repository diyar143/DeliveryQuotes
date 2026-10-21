namespace DeliveryQuotes;

public static class DataLoader
{

    public static List<DeliveryRequestRaw> LoadFromCsv(string filePath)
    {
        var requests = new List<DeliveryRequestRaw>();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: file '{filePath}' not found.");
            return requests;
        }

        var lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var parts = line.Split(',');
            var raw = new DeliveryRequestRaw(
                parts.Length > 0 ? parts[0] : null,
                parts.Length > 1 ? parts[1] : null,
                parts.Length > 2 ? parts[2] : null,
                parts.Length > 3 ? parts[3] : null,
                parts.Length > 4 ? parts[4] : null
            );

            requests.Add(raw);
        }

        return requests;
    }
}
