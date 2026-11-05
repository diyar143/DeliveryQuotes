using DeliveryQuotes;

Console.WriteLine("========================================");
Console.WriteLine("=== Delivery Quotes - Assignment 7 ===");
Console.WriteLine("========================================");
Console.WriteLine();

// 1. Normal Path (File loading & pipeline)
string dataFile = "data/requests.csv";
Console.WriteLine($"[1] Loading data from: {dataFile}");

var loadResult = DataLoader.LoadFromCsvSafe(dataFile);
loadResult.Match(
    rawRecords =>
    {
        Console.WriteLine($"Successfully loaded {rawRecords.Count} raw records.");

        var (validRequests, errors) = Validator.ValidateAll(rawRecords);
        Console.WriteLine($"Valid: {validRequests.Count}, Invalid: {errors.Count}");
        Display.PrintErrors(errors);

        var standardReport = Pipeline.RunPipeline(
            validRequests,
            PricingPolicies.StandardPolicy,
            "Standard"
        );
        Display.PrintReport(standardReport, "Standard Policy Results");

        var expressReport = Pipeline.RunPipeline(
            validRequests,
            PricingPolicies.ExpressPolicy,
            "Express"
        );
        Display.PrintReport(expressReport, "Express Policy Results");

        return true;
    },
    error =>
    {
        Console.WriteLine($"Failed to load file: {error}");
        return false;
    }
);

// 2. Three Domain Failure Paths (Demonstrated via ProcessRequest chain)
Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("=== Domain Failure Demonstrations ===");
Console.WriteLine("========================================");

var identicalRouteRaw = new DeliveryRequestRaw("Almaty", "Almaty", "15.0", "5.0", "false");
var res1 = Pipeline.ProcessRequest(identicalRouteRaw, PricingPolicies.StandardPolicy, "Standard");
Display.PrintResultOutcome(res1, "Domain Failure 1: Identical Route");

var overweightRaw = new DeliveryRequestRaw("Almaty", "Astana", "100.0", "65.0", "false");
var res2 = Pipeline.ProcessRequest(overweightRaw, PricingPolicies.StandardPolicy, "Standard");
Display.PrintResultOutcome(res2, "Domain Failure 2: Overweight Package (> 50 kg)");

var fragileLongRouteRaw = new DeliveryRequestRaw("Almaty", "Aktau", "450.0", "5.0", "true");
var res3 = Pipeline.ProcessRequest(fragileLongRouteRaw, PricingPolicies.StandardPolicy, "Standard");
Display.PrintResultOutcome(res3, "Domain Failure 3: Fragile Cargo on Route > 300 km");

// 3. Lookup Paths (Present, Absent, and Legitimate Zero)
Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("=== Lookup Paths Demonstrations ===");
Console.WriteLine("========================================");

var foundCity = CityCatalog.FindCity("Astana");
Console.WriteLine(foundCity.Match(
    info => $"City lookup 'Astana': Found in region '{info.Region}', Customs: {info.RequiresCustoms}",
    () => "City lookup 'Astana': Not found"
));

var missingCity = CityCatalog.FindCity("Paris");
Console.WriteLine(missingCity.Match(
    info => $"City lookup 'Paris': Found in region '{info.Region}'",
    () => "City lookup 'Paris': Not found (converted to None)"
));

var almatyDiscount = CityCatalog.GetCityDiscountPercent("Almaty");
Console.WriteLine(almatyDiscount.Match(
    rate => $"Discount lookup 'Almaty': Present with rate {rate * 100}% (legitimate zero value)",
    () => "Discount lookup 'Almaty': No discount record found"
));

// 4. Boundary Operational File Failures
Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("=== Boundary File Exception Handling ===");
Console.WriteLine("========================================");

var missingFileResult = DataLoader.LoadFromCsvSafe("data/non_existent.csv");
missingFileResult.Match(
    _ => "Unexpected success",
    err =>
    {
        Console.WriteLine($"[Missing File Caught]: {err}");
        return err;
    }
);

var malformedFileResult = DataLoader.LoadFromCsvSafe("data/malformed.csv");
malformedFileResult.Match(
    _ => "Unexpected success",
    err =>
    {
        Console.WriteLine($"[Malformed File Caught]: {err}");
        return err;
    }
);

// 5. Empty Dataset Test
Console.WriteLine();
Console.WriteLine("--- Testing with empty dataset ---");
var emptyReport = Pipeline.RunPipeline(
    new List<DeliveryRequest>(),
    PricingPolicies.StandardPolicy,
    "Standard"
);
Display.PrintReport(emptyReport, "Empty Dataset Results");
