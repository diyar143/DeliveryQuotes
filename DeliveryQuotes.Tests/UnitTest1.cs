using DeliveryQuotes;

namespace DeliveryQuotes.Tests;

public class PipelineTests
{
    private static DeliveryRequest MakeRequest(
        string from, string to, double dist, double weight, bool fragile)
    {
        return new DeliveryRequest(from, to, dist, weight, fragile);
    }

    [Fact]
    public void CompleteSuccess_IntegratedResultChain_ProducesCorrectQuote()
    {
        var raw = new DeliveryRequestRaw("Almaty", "Astana", "12.5", "3.0", "false");

        var result = Pipeline.ProcessRequest(raw, PricingPolicies.StandardPolicy, "Standard");

        Assert.True(result.IsSuccess);
        var success = Assert.IsType<Result<DeliveryQuote>.Success>(result);
        Assert.Equal("Almaty", success.Value.SenderCity);
        Assert.Equal("Astana", success.Value.ReceiverCity);
        Assert.Equal(1900, success.Value.Cost);
    }

    [Fact]
    public void DomainFailure_IdenticalCities_FailsWithUsefulError()
    {
        var raw = new DeliveryRequestRaw("Almaty", "Almaty", "15.0", "5.0", "false");
        var result = Pipeline.ProcessRequest(raw, PricingPolicies.StandardPolicy, "Standard");
        Assert.True(result.IsFailure);
        var failure = Assert.IsType<Result<DeliveryQuote>.Failure>(result);
        Assert.Contains("identical", failure.Error);
    }

    [Fact]
    public void DomainFailure_PackageOverweight_FailsWithUsefulError()
    {
        var raw = new DeliveryRequestRaw("Almaty", "Astana", "100.0", "65.0", "false");
        var result = Pipeline.ProcessRequest(raw, PricingPolicies.StandardPolicy, "Standard");
        Assert.True(result.IsFailure);
        var failure = Assert.IsType<Result<DeliveryQuote>.Failure>(result);
        Assert.Contains("exceeds the maximum allowable", failure.Error);
    }

    [Fact]
    public void DomainFailure_FragileLongDistance_FailsWithUsefulError()
    {
        var raw = new DeliveryRequestRaw("Almaty", "Aktau", "450.0", "5.0", "true");
        var result = Pipeline.ProcessRequest(raw, PricingPolicies.StandardPolicy, "Standard");
        Assert.True(result.IsFailure);
        var failure = Assert.IsType<Result<DeliveryQuote>.Failure>(result);
        Assert.Contains("Fragile items cannot be transported", failure.Error);
    }

    [Fact]
    public void Map_Success_TransformsValue()
    {
        var initial = Result<int>.Ok(10);
        var mapped = initial.Map(x => x * 2);
        Assert.True(mapped.IsSuccess);
        var success = Assert.IsType<Result<int>.Success>(mapped);
        Assert.Equal(20, success.Value);
    }

    [Fact]
    public void FailurePropagation_MapAndBind_DoNotRun()
    {
        int counter = 0;
        var failure = Result<int>.Fail("error");
        var afterMap = failure.Map(x => { counter++; return x * 2; });
        var afterBind = failure.Bind(x => { counter++; return Result<int>.Ok(x); });
        Assert.True(afterMap.IsFailure);
        Assert.True(afterBind.IsFailure);
        Assert.Equal(0, counter);
    }

    [Fact]
    public void MissingFile_ReturnsSafeFailure()
    {
        var result = SafeFileLoader.LoadWithStream("data/nonexistent_file.csv");
        Assert.True(result.IsFailure);
        var failure = Assert.IsType<Result<List<DeliveryRequestRaw>>.Failure>(result);
        Assert.Contains("File not found", failure.Error);
    }

    [Fact]
    public void MalformedFile_ReturnsSafeFailure()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "H1,H2,H3,H4,H5\nBrokenRow\n");
            var result = SafeFileLoader.LoadWithStream(tempFile);
            Assert.True(result.IsFailure);
            var failure = Assert.IsType<Result<List<DeliveryRequestRaw>>.Failure>(result);
            Assert.Contains("Malformed CSV", failure.Error);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Report_CostByRoute_SortedDescendingWithTieBreaker()
    {
        var quotes = new List<DeliveryQuote>
        {
            new("Almaty", "Astana", 10, 5, false, 1500, "Standard"),
            new("Astana", "Almaty", 10, 5, false, 1500, "Standard"),
            new("Almaty", "Shymkent", 5, 2, false, 800, "Standard")
        };

        var sorted = Reports.CostByRoute(quotes);

        Assert.Equal(3, sorted.Count);
        Assert.Equal(1500, sorted[0].Cost);
        Assert.Equal("Almaty", sorted[0].SenderCity);
        Assert.Equal(1500, sorted[1].Cost);
        Assert.Equal("Astana", sorted[1].SenderCity);
        Assert.Equal(800, sorted[2].Cost);
    }

    [Fact]
    public void Report_EmptyInput_ReturnsEmptyList()
    {
        var empty = new List<DeliveryQuote>();
        Assert.Empty(Reports.CostByRoute(empty));
        Assert.Empty(Reports.TotalsBySenderCity(empty));
    }

    [Fact]
    public void Report_TotalsBySenderCity_GroupsCorrectly()
    {
        var quotes = new List<DeliveryQuote>
        {
            new("Almaty", "Astana", 10, 5, false, 1000, "Standard"),
            new("Almaty", "Shymkent", 5, 2, false, 500, "Standard"),
            new("Astana", "Almaty", 10, 5, false, 1200, "Standard")
        };

        var totals = Reports.TotalsBySenderCity(quotes);

        Assert.Equal(2, totals.Count);
        Assert.Equal("Almaty", totals[0].City);
        Assert.Equal(2, totals[0].OrderCount);
        Assert.Equal(1500, totals[0].TotalCost);
        Assert.Equal("Astana", totals[1].City);
        Assert.Equal(1, totals[1].OrderCount);
        Assert.Equal(1200, totals[1].TotalCost);
    }

    [Fact]
    public void Extension_ToSummary_FormatsCorrectly()
    {
        var quote = new DeliveryQuote("Almaty", "Astana", 10, 5, false, 1500, "Standard");
        string summary = quote.ToSummary();
        Assert.Contains("Almaty -> Astana", summary);
        Assert.Contains("1500", summary);
        Assert.Contains("Standard", summary);
        Assert.DoesNotContain("FRAGILE", summary);

        var fragile = new DeliveryQuote("Almaty", "Astana", 10, 5, true, 1700, "Standard");
        Assert.Contains("[FRAGILE]", fragile.ToSummary());
    }

    [Fact]
    public void Extension_ThenValidate_ChainsValidation()
    {
        var raw = new DeliveryRequestRaw("Almaty", "Astana", "12.5", "3.0", "false");
        var result = Validator.ValidateRaw(raw)
            .ThenValidate(CityCatalog.VerifyServicedRoute)
            .ThenValidate(Pipeline.CheckBusinessRules);
        Assert.True(result.IsSuccess);

        var badRaw = new DeliveryRequestRaw("Almaty", "Almaty", "12.5", "3.0", "false");
        var badResult = Validator.ValidateRaw(badRaw)
            .ThenValidate(CityCatalog.VerifyServicedRoute);
        Assert.True(badResult.IsFailure);
    }

    [Fact]
    public void Currying_TwoScorers_DifferentResults()
    {
        var req = MakeRequest("Almaty", "Astana", 10, 5, false);
        double economy = Currying.EconomyScorer(req);
        double premium = Currying.PremiumScorer(req);
        Assert.Equal(1300, economy);
        Assert.Equal(3500, premium);
        Assert.NotEqual(economy, premium);
    }

    [Fact]
    public void DeferredExecution_DoesNotRunUntilIterated()
    {
        LazyPipeline.ProcessedCount = 0;
        var requests = new List<DeliveryRequest>
        {
            MakeRequest("Almaty", "Astana", 10, 5, false),
            MakeRequest("Astana", "Karaganda", 5, 3, false)
        };

        var lazy = LazyPipeline.GenerateQuotes(requests, PricingPolicies.StandardPolicy, "Standard");
        Assert.Equal(0, LazyPipeline.ProcessedCount);

        var list = lazy.ToList();
        Assert.Equal(2, LazyPipeline.ProcessedCount);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void NativeFee_ValidInput_CalculatesCorrectly()
    {
        var result = NativeFee.Calculate(10, 5, false);
        Assert.True(result.IsSuccess);
        var s = Assert.IsType<Result<int>.Success>(result);
        Assert.Equal(700, s.Value);

        var fragileResult = NativeFee.Calculate(10, 5, true);
        Assert.True(fragileResult.IsSuccess);
        var fs = Assert.IsType<Result<int>.Success>(fragileResult);
        Assert.Equal(1000, fs.Value);
    }

    [Fact]
    public void NativeFee_InvalidInput_ReturnsFailure()
    {
        var result = NativeFee.Calculate(-5, 3, false);
        Assert.True(result.IsFailure);
        var failure = Assert.IsType<Result<int>.Failure>(result);
        Assert.Contains("positive", failure.Error);
    }

    [Fact]
    public void NormalWorkflow_FullPipeline_ProducesCorrectResult()
    {
        var requests = new List<DeliveryRequest>
        {
            MakeRequest("Almaty", "Astana", 12.5, 3.0, false),
            MakeRequest("Almaty", "Shymkent", 7.0, 10.0, true)
        };
        var report = Pipeline.RunPipeline(requests, PricingPolicies.StandardPolicy, "Standard");
        Assert.Equal(2, report.TotalOrders);
        Assert.Equal(3800, report.TotalCost);

        var sortedByCity = Reports.TotalsBySenderCity(report.Quotes.ToList());
        Assert.Single(sortedByCity);
        Assert.Equal("Almaty", sortedByCity[0].City);
    }

    [Fact]
    public void FileExport_WritesAndReadsBack()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            var quotes = new List<DeliveryQuote>
            {
                new("Almaty", "Astana", 10, 5, false, 1500, "Standard")
            };
            SafeFileLoader.ExportReport(tempFile, quotes);
            Assert.True(File.Exists(tempFile));
            var lines = File.ReadAllLines(tempFile);
            Assert.Equal(2, lines.Length);
            Assert.Contains("Almaty", lines[1]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Report_NoMatches_ReturnsEmpty()
    {
        var quotes = new List<DeliveryQuote>
        {
            new("A", "B", 10, 5, false, 0, "Standard")
        };
        var filtered = Reports.CostByRoute(quotes);
        Assert.Empty(filtered);
    }
}
