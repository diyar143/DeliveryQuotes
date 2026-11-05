# Delivery Quotes Calculator

## Topic

A console application that calculates delivery cost quotes for shipping packages between cities. The application demonstrates explicit domain error handling, functional composition via `Result` (`Map`, `Bind`), missing value modeling with `Maybe`, and boundary exception handling for operational failures (missing or malformed files).

## Intended Users

Logistics companies, couriers, or dispatch operators who need reliable shipping cost calculations with compile-time state guarantees and explicit business failure reporting.

## Team

- Diyar Kabyken — Group IT-2503 (individual work)

## Input Fields

| Field        | Type    | Description                          |
|-------------|---------|--------------------------------------|
| SenderCity  | string  | Origin city of shipment              |
| ReceiverCity| string  | Destination city                     |
| DistanceKm  | double  | Distance in kilometres               |
| WeightKg    | double  | Package weight in kilograms          |
| IsFragile   | bool    | Flag indicating fragile contents     |

Input is loaded from CSV files (`data/requests.csv`).

---

## Business Rules & Explicit Domain Outcomes

### 1. Three Explicit Domain Failures (Task 1)
Instead of throwing exceptions during business calculations, domain operations return an explicit `Result<T>`:
- **Domain Failure 1 (Identical Route):** Sender and receiver cannot be the same city.  
  *Rule:* `SenderCity == ReceiverCity`  
  *Error:* `"Sender and destination cities cannot be identical: '{city}'."`
- **Domain Failure 2 (Package Overweight):** Single packages cannot exceed courier weight capacity (50 kg).  
  *Rule:* `WeightKg > 50.0`  
  *Error:* `"Weight {weight} kg exceeds the maximum allowable package limit of 50 kg."`
- **Domain Failure 3 (Fragile Long-Distance Restriction):** Fragile items cannot be transported over long-distance routes (> 300 km).  
  *Rule:* `IsFragile && DistanceKm > 300.0`  
  *Error:* `"Fragile items cannot be transported on routes exceeding 300 km (requested: {distance} km)."`

### 2. Missing Values & Real Lookups (Task 2)
The system uses `Maybe<T>` to represent present (`Some`) and absent (`None`) lookup results:
- **Required Lookup:** Destination city lookup in `CityCatalog.FindCity(cityName)`. If the city is missing (`None`), it is explicitly converted to a domain failure via `.ToResult("Destination city '{city}' is not in the serviced network.")`.
- **Optional Lookup with Primitive Value:** `CityCatalog.GetCityDiscountPercent(cityName)`.
  - For Astana, promotional discount is `0.10` (`Some(0.10)`).
  - For Almaty, the discount is `0.0` (`Some(0.0)`). Here, `0.0` is a **legitimate primitive value**, not absence.
  - For Aktau (unlisted promotion), it returns `None`, defaulting cleanly to `0.0` via `.GetOrElse(0.0)`.

### 3. Pipeline Composition via Map and Bind (Task 3)
Operations are composed into a pure processing chain:
```csharp
Validator.ValidateRaw(raw)
    .Bind(CityCatalog.VerifyServicedRoute)
    .Bind(CheckBusinessRules)
    .Map(req => CreateQuote(req, pricingPolicy, policyName));
```
- **Bind:** Chains operations that return `Result<T>`. If an operation fails, subsequent operations are skipped.
- **Map:** Transforms a successful `DeliveryRequest` into a `DeliveryQuote` using a pure projection.
- **Failure Propagation:** When any stage fails, the failure error is preserved and downstream functions are never executed.

### 4. Boundary Operational Exception Handling (Task 4)
File and parsing operations are isolated in `DataLoader.cs` at the boundary:
- **Missing File:** Caught at boundary and converted to `Result.Fail("File not found: '{filePath}'")`.
- **Malformed Content:** Corrupted CSV rows (e.g. wrong column count) produce `Result.Fail("Malformed CSV content in file ... at line X: ...")` without creating partial records.

### 5. Pricing Policies
- **Standard Policy:** Base 500 + 100/km + 50/kg (+ 200 surcharge for fragile).
- **Express Policy:** Base 800 + 150/km + 75/kg (+ 400 surcharge for fragile).

---

## Expected Results for Sample Data

### Valid Records (Standard Policy):

| Route                | Distance | Weight | Fragile | Cost (KZT) |
|----------------------|----------|--------|---------|------------|
| Karaganda → Almaty   | 25.0 km  | 8.0 kg | Yes     | 3600       |
| Almaty → Astana      | 12.5 km  | 3.0 kg | No      | 1900       |
| Almaty → Shymkent    | 7.0 km   | 10.0 kg| Yes     | 1900       |
| Astana → Karaganda   | 2.0 km   | 5.0 kg | No      | 950        |

### Policy Comparison (Astana → Karaganda, 2 km, 5 kg, non-fragile):
- **Standard:** 500 + (100 × 2) + (50 × 5) = **950**
- **Express:** 800 + (150 × 2) + (75 × 5) = **1475**

---

## How to Build and Run

**Prerequisites:** .NET SDK 8.0

```bash
# Build solution
dotnet build

# Run application
dotnet run --project DeliveryQuotes

# Run automated tests
dotnet test
```

**Working Directory:** Solution root (`DeliveryQuotes/`).

---

## Requirement Mapping

| Assignment Requirement | Implementing File(s) | Method / Type |
|---|---|---|
| **Task 1: Result Type & 3 Domain Failures** | `Result.cs`, `Pipeline.cs`, `CityCatalog.cs` | `Result<T>`, `CheckBusinessRules()`, `VerifyServicedRoute()` |
| **Task 2: Missing Values (Maybe)** | `Maybe.cs`, `CityCatalog.cs` | `Maybe<T>`, `FindCity()`, `GetCityDiscountPercent()` |
| **Task 3: Map and Bind Composition** | `Result.cs`, `Pipeline.cs` | `Result.Map()`, `Result.Bind()`, `ProcessRequest()` |
| **Task 4: Boundary Exception Handling** | `DataLoader.cs` | `LoadFromCsvSafe()` |
| **Task 5: Complete Integration** | `Program.cs`, `Display.cs` | Main flow covering normal, 3 failures, lookups, 2 file failures |
| **Automated Tests** | `DeliveryQuotes.Tests/UnitTest1.cs` | 17 cumulative unit tests covering all required cases |

---

## Test Cases Summary

| # | Test Name | Case Type | Expected Outcome | Latest Status |
|---|---|---|---|---|
| 1 | `CompleteSuccess_IntegratedResultChain_ProducesCorrectQuote` | Normal Chain | `Result.Success` with Cost = 1900 | Passed |
| 2 | `DomainFailure_IdenticalCities_FailsWithUsefulError` | Domain Failure 1 | Failure: identical cities error | Passed |
| 3 | `DomainFailure_PackageOverweight_FailsWithUsefulError` | Domain Failure 2 | Failure: weight exceeds 50 kg | Passed |
| 4 | `DomainFailure_FragileLongDistance_FailsWithUsefulError` | Domain Failure 3 | Failure: fragile route > 300 km | Passed |
| 5 | `Lookup_PresentAndAbsentCases_WorkCorrectly` | Lookup (Required) | Astana -> Some("North"), Paris -> None | Passed |
| 6 | `Lookup_LegitimateZeroValue_NotMistakenForAbsence` | Lookup (Primitive) | Almaty -> Some(0.0), Aktau -> None (default 0.0) | Passed |
| 7 | `Map_Success_TransformsValue` | Result Transformation | Maps 10 -> 20 | Passed |
| 8 | `Bind_Success_ChainsOperation` | Result Continuation | Chains 10 -> "Number: 10" | Passed |
| 9 | `FailurePropagation_MapAndBind_DoNotInvokeSuppliedFunctions` | Failure Propagation | Counters remain 0; functions not called | Passed |
| 10 | `MissingFile_BoundaryReportsProblemWithoutCrashing` | File Boundary | Failure: "File not found" | Passed |
| 11 | `MalformedFile_BoundaryReportsProblemWithoutPartialUpdate` | File Boundary | Failure: "Malformed CSV content", no partial state | Passed |
| 12 | `NormalWorkflow_ProducesCorrectResult` | Core Pipeline | 2 orders, TotalCost = 3800 | Passed |
| 13 | `EmptyDataset_ProducesEmptyReport` | Edge Case | 0 orders, TotalCost = 0 | Passed |
| 14 | `BoundaryValue_ZeroDistance_IsFilteredOut` | Boundary Value | Distance = 0 removed, 1 order remains | Passed |
| 15 | `InvalidInput_IsRejectedWithMessage` | Input Validation | 3 invalid raw inputs rejected | Passed |
| 16 | `TwoPolicies_ProduceDifferentResults` | Configurable Rule | Standard = 950, Express = 1475 | Passed |
| 17 | `PurityCheck_InputListIsNotModified` | Pure Functions | Caller input list remains unchanged | Passed |

---

## Team Contributions

- **Diyar Kabyken (Group IT-2503)**: Implemented functional `Result<T>` and `Maybe<T>` types with `Map` and `Bind`, added domain rules and route verification, implemented boundary exception handling for missing and malformed CSV files, created 17 automated tests covering all required cases, and updated the documentation.
