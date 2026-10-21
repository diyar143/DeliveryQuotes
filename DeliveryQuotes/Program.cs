using DeliveryQuotes;

Console.WriteLine("=== Delivery Quotes Calculator ===");
Console.WriteLine();

string dataFile = "data/requests.csv";
Console.WriteLine($"Loading data from: {dataFile}");

var rawRequests = DataLoader.LoadFromCsv(dataFile);
Console.WriteLine($"Loaded {rawRequests.Count} raw records.");

var (validRequests, errors) = Validator.ValidateAll(rawRequests);
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

Console.WriteLine();
Console.WriteLine("--- Testing with empty dataset ---");
var emptyReport = Pipeline.RunPipeline(
    new List<DeliveryRequest>(),
    PricingPolicies.StandardPolicy,
    "Standard"
);

Display.PrintReport(emptyReport, "Empty Dataset Results");
