# Delivery Quotes Calculator

## Topic

A console application that calculates delivery cost quotes for shipping orders between cities. The system takes delivery requests (sender city, receiver city, distance, weight, fragile flag) and computes the cost using configurable pricing policies.

## Intended Users

Logistics companies or delivery services that need to quickly estimate shipping costs for multiple orders.

## Team

Diyar Kabyken IT-2503 (individual work)

## Input Fields

| Field        | Type    | Description                          |
|-------------|---------|--------------------------------------|
| SenderCity  | string  | City where the package is sent from  |
| ReceiverCity| string  | Destination city                     |
| DistanceKm  | double  | Distance in kilometres               |
| WeightKg    | double  | Package weight in kilograms          |
| IsFragile   | bool    | Whether the package is fragile       |

Input is loaded from a CSV file (`data/requests.csv`).

## Business Rules

1. **Validation**: All fields are required. Distance must be >= 0, weight must be > 0. Invalid records are rejected with a clear message.
2. **Filtering**: Records with distance = 0 are filtered out (no delivery needed).
3. **Standard Policy**: Base cost 500 + 100 per km + 50 per kg. Fragile surcharge: +200.
4. **Express Policy**: Base cost 800 + 150 per km + 75 per kg. Fragile surcharge: +400.
5. **Report**: Results sorted by cost (descending), with total count and total cost.

## Expected Results for Sample Data

### Valid records (Standard Policy):

| Route                    | Distance | Weight | Fragile | Cost  |
|--------------------------|----------|--------|---------|-------|
| Karaganda → Almaty       | 25.0 km  | 8.0 kg | Yes     | 3600  |
| Almaty → Astana          | 12.5 km  | 3.0 kg | No      | 1900  |
| Almaty → Shymkent        | 7.0 km   | 10.0 kg| Yes     | 1900  |
| Astana → Karaganda       | 2.0 km   | 5.0 kg | No      | 950   |

### Rejected records:

| Record | Reason                        |
|--------|-------------------------------|
| 4      | Distance = 0 (filtered)       |
| 5      | Distance "abc" not a number   |
| 6      | Sender city is missing        |
| 7      | Weight is negative            |

### Policy comparison (Astana → Karaganda, 2 km, 5 kg, not fragile):

- Standard: 500 + 100×2 + 50×5 = **950**
- Express: 800 + 150×2 + 75×5 = **1475**

## How to Build and Run

**Prerequisites:** .NET SDK 8.0

```bash
# Build
dotnet build

# Run the application
dotnet run --project DeliveryQuotes

# Run tests
dotnet test
```

**Working directory:** the solution root folder (`DeliveryQuotes/`).

## Failure Demonstration

- Invalid records are printed with error messages before the pipeline runs.
- Empty dataset produces a report with 0 orders and 0 cost without crashing.

## Requirement Mapping

| Requirement               | File(s)                          | Method(s)                                   |
|--------------------------|----------------------------------|---------------------------------------------|
| Task 1: Workflow          | `Program.cs`, `README.md`        | Main program flow                           |
| Task 2: Validation        | `Validator.cs`, `Models.cs`      | `Validate()`, `ValidateAll()`               |
| Task 3: Pipeline stages   | `Pipeline.cs`                    | `FilterValidRequests()`, `CalculateCosts()`, `BuildReport()` |
| Task 4: Configurable rule | `Pipeline.cs`, `PricingPolicies.cs` | `CalculateCosts(Func<>)`, `StandardPolicy()`, `ExpressPolicy()` |
| Task 5: Purity            | `Pipeline.cs`                    | All stages are pure, I/O in `Display.cs`    |
| Tests                     | `DeliveryQuotes.Tests/UnitTest1.cs` | 8 test methods covering all required cases |

## Test Cases

| # | Test Name                              | Case Type              | Expected Result                        |
|---|----------------------------------------|------------------------|----------------------------------------|
| 1 | NormalWorkflow_ProducesCorrectResult   | Normal workflow        | 2 orders, total cost 3800              |
| 2 | EmptyDataset_ProducesEmptyReport       | Empty dataset          | 0 orders, 0 cost, no crash             |
| 3 | BoundaryValue_ZeroDistance_IsFilteredOut| Boundary value         | Distance=0 filtered, 1 order remains   |
| 4 | InvalidInput_IsRejectedWithMessage     | Invalid/missing input  | 3 errors, 0 valid records              |
| 5 | TwoPolicies_ProduceDifferentResults    | Two policies           | Standard=950, Express=1475             |
| 6 | PurityCheck_InputListIsNotModified     | Purity                 | Input list unchanged after pipeline    |
| 7 | StandardPolicy_CalculatesCorrectly     | Stage check            | Fragile 10km 2kg = 1800               |
| 8 | Validation_AcceptsValidData            | Validation             | Valid raw data parsed correctly         |

## Team Contributions

Diyar Kabyken Implemented all pipeline stages, validation logic, pricing policies, data loading, display output, all 8 tests, and this documentation.
