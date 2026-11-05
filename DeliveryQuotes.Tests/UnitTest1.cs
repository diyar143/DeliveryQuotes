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
        // Cost: 500 + 100*12.5 + 50*3.0 = 500 + 1250 + 150 = 1900
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
    public void Lookup_PresentAndAbsentCases_WorkCorrectly()
    {
        var found = CityCatalog.FindCity("Astana");
        Assert.True(found.HasValue);
        var some = Assert.IsType<Maybe<CityInfo>.Some>(found);
        Assert.Equal("North", some.Value.Region);

        var missing = CityCatalog.FindCity("Paris");
        Assert.False(missing.HasValue);
        Assert.IsType<Maybe<CityInfo>.None>(missing);
    }

    [Fact]
    public void Lookup_LegitimateZeroValue_NotMistakenForAbsence()
    {
        var discountPresentZero = CityCatalog.GetCityDiscountPercent("Almaty");
        Assert.True(discountPresentZero.HasValue);
        var some = Assert.IsType<Maybe<double>.Some>(discountPresentZero);
        Assert.Equal(0.0, some.Value);

        var discountAbsent = CityCatalog.GetCityDiscountPercent("Aktau");
        Assert.False(discountAbsent.HasValue);
        Assert.Equal(0.0, discountAbsent.GetOrElse(0.0));
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
    public void Bind_Success_ChainsOperation()
    {
        var initial = Result<int>.Ok(10);

        var bound = initial.Bind(x => Result<string>.Ok($"Number: {x}"));

        Assert.True(bound.IsSuccess);
        var success = Assert.IsType<Result<string>.Success>(bound);
        Assert.Equal("Number: 10", success.Value);
    }

    [Fact]
    public void FailurePropagation_MapAndBind_DoNotInvokeSuppliedFunctions()
    {
        int mapCounter = 0;
        int bindCounter = 0;

        var failure = Result<int>.Fail("Original domain error");

        var afterMap = failure.Map(x =>
        {
            mapCounter++;
            return x * 2;
        });

        var afterBind = failure.Bind(x =>
        {
            bindCounter++;
            return Result<int>.Ok(x + 5);
        });

        Assert.True(afterMap.IsFailure);
        Assert.True(afterBind.IsFailure);
        Assert.Equal(0, mapCounter);
        Assert.Equal(0, bindCounter);
        Assert.Equal("Original domain error", ((Result<int>.Failure)afterMap).Error);
        Assert.Equal("Original domain error", ((Result<int>.Failure)afterBind).Error);
    }

    [Fact]
    public void MissingFile_BoundaryReportsProblemWithoutCrashing()
    {
        var result = DataLoader.LoadFromCsvSafe("data/definitely_non_existent_file.csv");

        Assert.True(result.IsFailure);
        var failure = Assert.IsType<Result<List<DeliveryRequestRaw>>.Failure>(result);
        Assert.Contains("File not found", failure.Error);
    }

    [Fact]
    public void MalformedFile_BoundaryReportsProblemWithoutPartialUpdate()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "SenderCity,ReceiverCity,DistanceKm,WeightKg,IsFragile\nAlmaty,Astana,10.0,2.0,false\nCorruptedRowWithoutCommas\n");

            var result = DataLoader.LoadFromCsvSafe(tempFile);

            Assert.True(result.IsFailure);
            var failure = Assert.IsType<Result<List<DeliveryRequestRaw>>.Failure>(result);
            Assert.Contains("Malformed CSV content", failure.Error);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void NormalWorkflow_ProducesCorrectResult()
    {
        var requests = new List<DeliveryRequest>
        {
            MakeRequest("Almaty", "Astana", 12.5, 3.0, false),
            MakeRequest("Almaty", "Shymkent", 7.0, 10.0, true)
        };

        var report = Pipeline.RunPipeline(requests, PricingPolicies.StandardPolicy, "Standard");

        Assert.Equal(2, report.TotalOrders);
        Assert.Equal(3800, report.TotalCost);
    }

    [Fact]
    public void EmptyDataset_ProducesEmptyReport()
    {
        var requests = new List<DeliveryRequest>();

        var report = Pipeline.RunPipeline(requests, PricingPolicies.StandardPolicy, "Standard");

        Assert.Equal(0, report.TotalOrders);
        Assert.Equal(0, report.TotalCost);
        Assert.Empty(report.Quotes);
    }

    [Fact]
    public void BoundaryValue_ZeroDistance_IsFilteredOut()
    {
        var requests = new List<DeliveryRequest>
        {
            MakeRequest("Shymkent", "Aktau", 0, 2.0, false),
            MakeRequest("Astana", "Karaganda", 2.0, 5.0, false)
        };

        var report = Pipeline.RunPipeline(requests, PricingPolicies.StandardPolicy, "Standard");

        Assert.Equal(1, report.TotalOrders);
        Assert.Equal("Astana", report.Quotes[0].SenderCity);
        Assert.Equal(950, report.Quotes[0].Cost);
    }

    [Fact]
    public void InvalidInput_IsRejectedWithMessage()
    {
        var rawRequests = new List<DeliveryRequestRaw>
        {
            new DeliveryRequestRaw("Almaty", "Atyrau", "abc", "4.0", "true"),
            new DeliveryRequestRaw(null, "Astana", "5.0", "1.5", "false"),
            new DeliveryRequestRaw("Astana", "Almaty", "1.0", "-3.0", "true")
        };

        var (valid, errors) = Validator.ValidateAll(rawRequests);

        Assert.Empty(valid);
        Assert.Equal(3, errors.Count);
        Assert.Contains("not a valid number", errors[0]);
        Assert.Contains("missing", errors[1]);
        Assert.Contains("positive", errors[2]);
    }

    [Fact]
    public void TwoPolicies_ProduceDifferentResults()
    {
        var requests = new List<DeliveryRequest>
        {
            MakeRequest("Astana", "Karaganda", 2.0, 5.0, false)
        };

        var standardReport = Pipeline.RunPipeline(
            requests, PricingPolicies.StandardPolicy, "Standard");
        var expressReport = Pipeline.RunPipeline(
            requests, PricingPolicies.ExpressPolicy, "Express");

        Assert.Equal(950, standardReport.Quotes[0].Cost);
        Assert.Equal(1475, expressReport.Quotes[0].Cost);
        Assert.NotEqual(standardReport.Quotes[0].Cost, expressReport.Quotes[0].Cost);
    }

    [Fact]
    public void PurityCheck_InputListIsNotModified()
    {
        var requests = new List<DeliveryRequest>
        {
            MakeRequest("Almaty", "Astana", 12.5, 3.0, false),
            MakeRequest("Shymkent", "Aktau", 0, 2.0, false),
            MakeRequest("Astana", "Karaganda", 2.0, 5.0, false)
        };

        int originalCount = requests.Count;
        var firstItem = requests[0];
        var lastItem = requests[2];

        var filtered = Pipeline.FilterValidRequests(requests);
        var quotes = Pipeline.CalculateCosts(filtered, PricingPolicies.StandardPolicy, "Standard");
        var report = Pipeline.BuildReport(quotes);

        Assert.Equal(originalCount, requests.Count);
        Assert.Equal(firstItem, requests[0]);
        Assert.Equal(lastItem, requests[2]);
        Assert.NotSame(requests, filtered);
    }
}
