using DeliveryQuotes;

Console.WriteLine("==========================================");
Console.WriteLine("=== Delivery Quotes - Assignment 8 ===");
Console.WriteLine("==========================================");
Console.WriteLine();

string dataFile = "data/requests.csv";
if (!File.Exists(dataFile))
{
    string baseDirFile = Path.Combine(AppContext.BaseDirectory, "data", "requests.csv");
    if (File.Exists(baseDirFile))
        dataFile = baseDirFile;
    else if (File.Exists("DeliveryQuotes/data/requests.csv"))
        dataFile = "DeliveryQuotes/data/requests.csv";
}
var loadResult = SafeFileLoader.LoadWithStream(dataFile);

if (loadResult is Result<List<DeliveryRequestRaw>>.Failure loadFail)
{
    Console.WriteLine($"Failed to load: {loadFail.Error}");
    return;
}

var rawRecords = ((Result<List<DeliveryRequestRaw>>.Success)loadResult).Value;
Console.WriteLine($"Loaded {rawRecords.Count} records from CSV (using StreamReader with using).");

var (validRequests, errors) = Validator.ValidateAll(rawRecords);
Console.WriteLine($"Valid: {validRequests.Count}, Invalid: {errors.Count}");
Display.PrintErrors(errors);

Console.WriteLine();
Console.WriteLine("=== Report 1: Quotes sorted by cost (descending) ===");
var allQuotes = Pipeline.CalculateCosts(
    Pipeline.FilterValidRequests(validRequests),
    PricingPolicies.StandardPolicy,
    "Standard");

var sortedReport = Reports.CostByRoute(allQuotes);
foreach (var q in sortedReport)
    Console.WriteLine("  " + q.ToSummary());

Console.WriteLine();
Console.WriteLine("=== Report 2: Totals by sender city ===");
var cityTotals = Reports.TotalsBySenderCity(allQuotes);
foreach (var ct in cityTotals)
    Console.WriteLine($"  {ct.City}: {ct.OrderCount} orders, total {ct.TotalCost:F0} tg");

Console.WriteLine();
Console.WriteLine("=== Extension methods demo ===");
var testRaw = new DeliveryRequestRaw("Almaty", "Astana", "12.5", "3.0", "false");
var chainResult = Validator.ValidateRaw(testRaw)
    .ThenValidate(CityCatalog.VerifyServicedRoute)
    .ThenValidate(Pipeline.CheckBusinessRules);

if (chainResult is Result<DeliveryRequest>.Success okReq)
{
    var quote = Pipeline.CreateQuote(okReq.Value, PricingPolicies.StandardPolicy, "Standard");
    Console.WriteLine("  ToSummary: " + quote.ToSummary());
}

Console.WriteLine();
Console.WriteLine("=== Curried scoring functions ===");
var sampleReq = new DeliveryRequest("Almaty", "Astana", 10, 5, false);
double economyCost = Currying.EconomyScorer(sampleReq);
double premiumCost = Currying.PremiumScorer(sampleReq);
Console.WriteLine($"  Same input (dist=10, weight=5, fragile=false):");
Console.WriteLine($"  Economy scorer: {economyCost}");
Console.WriteLine($"  Premium scorer: {premiumCost}");

Console.WriteLine();
Console.WriteLine("=== Deferred execution demo ===");
LazyPipeline.ProcessedCount = 0;
var lazyQuotes = LazyPipeline.GenerateQuotes(validRequests, PricingPolicies.StandardPolicy, "Standard");
Console.WriteLine($"  After creating IEnumerable: ProcessedCount = {LazyPipeline.ProcessedCount}");
var materialized = lazyQuotes.ToList();
Console.WriteLine($"  After ToList(): ProcessedCount = {LazyPipeline.ProcessedCount}");
Console.WriteLine($"  Materialized {materialized.Count} quotes (snapshot, safe to reuse).");

Console.WriteLine();
Console.WriteLine("=== File export (using StreamWriter with using) ===");
string exportPath = Path.Combine(AppContext.BaseDirectory, "data", "report_export.csv");
SafeFileLoader.ExportReport(exportPath, materialized);
Console.WriteLine($"  Exported {materialized.Count} quotes to {exportPath}");

Console.WriteLine();
Console.WriteLine("=== Native C library: delivery fee ===");
var nativeFee = NativeFee.Calculate(10, 5, false);
if (nativeFee is Result<int>.Success nativeOk)
    Console.WriteLine($"  Native fee (dist=10, weight=5, not fragile): {nativeOk.Value} tg");
else if (nativeFee is Result<int>.Failure nativeFail)
    Console.WriteLine($"  Native fee error: {nativeFail.Error}");

var nativeFragile = NativeFee.Calculate(10, 5, true);
if (nativeFragile is Result<int>.Success nfOk)
    Console.WriteLine($"  Native fee (dist=10, weight=5, fragile): {nfOk.Value} tg");

Console.WriteLine();
Console.WriteLine("=== Domain failures (from Assignment 7) ===");
var identicalRouteRaw = new DeliveryRequestRaw("Almaty", "Almaty", "15.0", "5.0", "false");
var res1 = Pipeline.ProcessRequest(identicalRouteRaw, PricingPolicies.StandardPolicy, "Standard");
Display.PrintResultOutcome(res1, "Identical Route");

var overweightRaw = new DeliveryRequestRaw("Almaty", "Astana", "100.0", "65.0", "false");
var res2 = Pipeline.ProcessRequest(overweightRaw, PricingPolicies.StandardPolicy, "Standard");
Display.PrintResultOutcome(res2, "Overweight Package");

Console.WriteLine();
Console.WriteLine("=== Missing file handling ===");
var missingFileResult = SafeFileLoader.LoadWithStream("data/does_not_exist.csv");
if (missingFileResult is Result<List<DeliveryRequestRaw>>.Failure mf)
    Console.WriteLine($"  Caught: {mf.Error}");

Console.WriteLine();
Console.WriteLine("=== Native fee adapter failure demo ===");
var badNative = NativeFee.Calculate(-5, 3, false);
if (badNative is Result<int>.Failure bf)
    Console.WriteLine($"  Caught: {bf.Error}");

Console.WriteLine();
Console.WriteLine("Done.");
