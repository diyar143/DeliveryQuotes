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

    [Fact]
    public void StandardPolicy_CalculatesCorrectly()
    {
        var request = MakeRequest("A", "B", 10.0, 2.0, true);

        double cost = PricingPolicies.StandardPolicy(request);

        Assert.Equal(1800, cost);
    }
    [Fact]
    public void Validation_AcceptsValidData()
    {
        var raw = new DeliveryRequestRaw("Almaty", "Astana", "5.5", "2.0", "false");

        var (request, error) = Validator.Validate(raw);

        Assert.NotNull(request);
        Assert.Null(error);
        Assert.Equal("Almaty", request.SenderCity);
        Assert.Equal(5.5, request.DistanceKm);
        Assert.Equal(2.0, request.WeightKg);
        Assert.False(request.IsFragile);
    }
}
