# Delivery Quotes Calculator — Assignment: Extension Methods and LINQ

## Topic

A console application for calculating delivery quotes that integrates functional programming techniques: custom extension methods, LINQ query reports with grouping and tie-breaking, function currying and partial application, deferred execution with lazy enumeration, safe resource disposal (`using` streams), and a native C library integration via P/Invoke.

## Intended Users

Logistics operators and dispatchers who need deterministic delivery cost analysis, route rankings, and regional shipment summaries with strong safety and explicit error propagation.

## Team

- **Diyar Kabyken** — Group IT-2503 (Individual submission)

---

## Input Fields

| Field | Type | Description |
|---|---|---|
| SenderCity | string | Origin city of shipment |
| ReceiverCity | string | Destination city of shipment |
| DistanceKm | double | Travel distance in kilometres |
| WeightKg | double | Cargo weight in kilograms |
| IsFragile | bool | Flag indicating fragile package |

Input is read safely from CSV files (`data/requests.csv`) using stream-based I/O.

---

## Assignment Tasks & Implementation

### Task 1: Custom Extension Methods
Implemented in `DeliveryQuotes/Extensions.cs`:
1. `ThenValidate<U>(this Result<U> result, Func<U, Result<U>> rule) -> Result<U>`
   - **Receiver type:** `Result<U>`
   - **Purpose:** Chains domain validation rules in a fluent pipeline without repeatedly writing `.Bind(...)`.
   - **Why on this receiver:** It naturally extends the monadic `Result<U>` workflow for sequential rule checking.
2. `ToSummary(this DeliveryQuote quote) -> string`
   - **Receiver type:** `DeliveryQuote`
   - **Purpose:** Produces a concise, standardized single-line string representation (route, cost in tenge, policy, fragile tag).
   - **Why on this receiver:** It formats a single quote entity for logging, display, or UI summaries.

### Task 2: Two LINQ Reports
Implemented in `DeliveryQuotes/Reports.cs`:

1. **Report 1: Quotes Sorted by Cost (`CostByRoute`)**
   - **Intended user:** Pricing analyst reviewing high-value deliveries.
   - **Pipeline stages:** `Where(q => q.Cost > 0)` -> `OrderByDescending(q => q.Cost)` -> `ThenBy(q => q.SenderCity)` -> `ThenBy(q => q.ReceiverCity)` -> `ToList()`.
   - **Tie-breakers:** Primary order is `Cost` descending. If costs are identical, ties are resolved deterministically by `SenderCity` ascending, then `ReceiverCity` ascending.

2. **Report 2: Totals by Sender City (`TotalsBySenderCity`)**
   - **Intended user:** Regional hub manager monitoring departure volume and revenue.
   - **Pipeline stages:** `GroupBy(q => q.SenderCity)` -> `Select(g => new CitySummary(City, Count, Sum))` -> `OrderByDescending(s => s.TotalCost)` -> `ThenBy(s => s.City)` -> `ToList()`.
   - **Aggregates:** Uses `.Count()` for total orders and `.Sum(q => q.Cost)` for total revenue per city.

### Task 3: Function Currying & Partial Application
Implemented in `DeliveryQuotes/Currying.cs`:
- **Curried function:** `CreateScorer(baseFee, distanceRate, weightRate, fragileSurcharge) -> (DeliveryRequest -> double)`
- **Configured function 1 (Economy):** `EconomyScorer = CreateScorer(300, 80, 40, 100)`
- **Configured function 2 (Premium):** `PremiumScorer = CreateScorer(1000, 200, 100, 500)`
- **Different results on same input:**
  - Input: Distance = 10 km, Weight = 5 kg, Fragile = false.
  - Economy cost: `300 + (80 * 10) + (40 * 5) = 1300 tg`
  - Premium cost: `1000 + (200 * 10) + (100 * 5) = 3500 tg`

### Task 4 & 5: Deferred Execution & Safe Resource Disposal
Implemented in `DeliveryQuotes/LazyPipeline.cs` and `DeliveryQuotes/SafeFileLoader.cs`:
- **Deferred execution:** `LazyPipeline.GenerateQuotes` uses `yield return`. Creating the enumerable does not execute the generator body (`ProcessedCount == 0`). Processing occurs only upon enumeration (`ToList()`), advancing `ProcessedCount` to 4.
- **Safe resource disposal:**
  - `SafeFileLoader.LoadWithStream`: Uses `using var stream = new FileStream(...)` and `using var reader = new StreamReader(stream)`. Releases the file handle immediately, even on exception or early return.
  - `SafeFileLoader.ExportReport`: Uses `using var stream = new FileStream(...)` and `using var writer = new StreamWriter(stream)` to export processed quotes to `report_export.csv`.

### Task 6: Retention & Allocation Note
- **Materialisation point:** Datasets are materialized via `.ToList()` before report generation.
- **Trade-off:**
  - *Streaming (`IEnumerable` with `yield return`):* Minimum memory footprint, processes one element at a time, but repeats parsing or calculations if enumerated multiple times.
  - *Snapshot (`List<T>` / array):* Retains elements in memory, but provides stable, idempotent access for multiple LINQ queries, reports, and file exports without repeated I/O or state re-evaluation.
- **Decision:** We stream raw input into memory once, validate and process, then snapshot into a immutable/read-only list for pure reporting and export. No global caches or static event subscriptions are retained, preventing memory leaks.

### Tasks 7–9: Native C Library Integration
Implemented in `DeliveryQuotes/native/delivery_fee.c` and `DeliveryQuotes/NativeFee.cs`:
- **Function contract:**
  ```c
  int32_t calculate_delivery_fee(int32_t distance_km, int32_t weight_kg, int32_t is_fragile);
  ```
  - **Inputs:** `distance_km` (> 0), `weight_kg` (> 0), `is_fragile` (0 = false, 1 = true).
  - **Formula:** `500 + (distance_km * 10) + (weight_kg * 20) + (is_fragile ? 300 : 0)`.
  - **Invalid input:** Returns `-1`.
  - **Manual check:** For distance = 10, weight = 5, fragile = 0: `500 + 100 + 100 = 700 tg`. For fragile = 1: `700 + 300 = 1000 tg`.
- **C# P/Invoke binding & adapter:**
  - `[DllImport("delivery_fee", CallingConvention = CallingConvention.Cdecl)]`
  - The `NativeFee.Calculate` adapter validates inputs in managed code before invoking the native export and converts missing library or entry point errors into safe `Result<int>.Fail(...)` outcomes.
- **Build & packaging:**
  - Platform: Windows x64.
  - Compiler: GCC / MinGW (`gcc -shared -o delivery_fee.dll delivery_fee.c`) or reproducible PE builder (`python native/build_dll.py`).
  - Output: `delivery_fee.dll` is configured in `.csproj` to automatically copy to the output directory on build (`PreserveNewest`).
- **Integration difficulty and resolution:** Initial testing without DLL in the search path produced `DllNotFoundException`. Resolved by adding build target links in `.csproj` to automatically place `delivery_fee.dll` adjacent to the executable and test assemblies.

---

## How to Build, Test, and Run

**Environment:**
- OS: Windows 10/11 x64
- .NET SDK: 8.0 (.NET 8.0.201)

```powershell
# 1. Build the entire solution (copies native DLL and data files)
dotnet build

# 2. Run all automated tests (20 tests)
dotnet test

# 3. Run the console application
dotnet run --project DeliveryQuotes
```

**Working Directory:** Solution root (`DeliveryQuotes/`).

---

## Requirement Mapping

| Task | Requirement | Implementing File(s) | Method / Construct |
|---|---|---|---|
| **Task 1** | Two custom extensions | `Extensions.cs` | `ThenValidate()`, `ToSummary()` |
| **Task 2** | Two LINQ reports | `Reports.cs` | `CostByRoute()`, `TotalsBySenderCity()` |
| **Task 3** | Currying & partial application | `Currying.cs` | `CreateScorer()`, `EconomyScorer`, `PremiumScorer` |
| **Task 4** | Deferred execution | `LazyPipeline.cs` | `GenerateQuotes()` with `yield return`, `ToList()` |
| **Task 5** | Safe resource disposal | `SafeFileLoader.cs` | `LoadWithStream()`, `ExportReport()` with `using` |
| **Task 6** | Retention note | `README.md` | Task 6 section in README |
| **Task 7** | Native C function | `native/delivery_fee.c` | `calculate_delivery_fee()` |
| **Task 8** | P/Invoke binding & adapter | `NativeFee.cs` | `[DllImport]`, `NativeFee.Calculate()` |
| **Task 9** | Native build & tests | `native/build.bat`, `UnitTest1.cs` | Native build scripts and unit tests |

---

## Automated Test Cases (20 Tests)

| # | Test Name | Area | Expected Result | Status |
|---|---|---|---|---|
| 1 | `CompleteSuccess_IntegratedResultChain_ProducesCorrectQuote` | Core Pipeline | Result.Success, Cost = 1900 | Passed |
| 2 | `DomainFailure_IdenticalCities_FailsWithUsefulError` | Domain Rule | Result.Failure with identical cities error | Passed |
| 3 | `DomainFailure_PackageOverweight_FailsWithUsefulError` | Domain Rule | Result.Failure for weight > 50 kg | Passed |
| 4 | `DomainFailure_FragileLongDistance_FailsWithUsefulError` | Domain Rule | Result.Failure for fragile > 300 km | Passed |
| 5 | `Map_Success_TransformsValue` | Result Functor | Maps 10 -> 20 | Passed |
| 6 | `FailurePropagation_MapAndBind_DoNotRun` | Monadic Chain | Functions not executed on failure | Passed |
| 7 | `MissingFile_ReturnsSafeFailure` | File Boundary | Safe Result.Failure on missing file | Passed |
| 8 | `MalformedFile_ReturnsSafeFailure` | File Boundary | Safe Result.Failure on corrupt row | Passed |
| 9 | `Report_CostByRoute_SortedDescendingWithTieBreaker` | LINQ Report 1 | Primary order by cost desc, tied by sender city | Passed |
| 10 | `Report_EmptyInput_ReturnsEmptyList` | LINQ Edge Case | Empty list returned on empty input | Passed |
| 11 | `Report_NoMatches_ReturnsEmpty` | LINQ Edge Case | Empty list when filters match nothing | Passed |
| 12 | `Report_TotalsBySenderCity_GroupsCorrectly` | LINQ Report 2 | Grouped by sender, count and sum aggregated | Passed |
| 13 | `Extension_ToSummary_FormatsCorrectly` | Extension Method | String contains route, cost, policy, fragile tag | Passed |
| 14 | `Extension_ThenValidate_ChainsValidation` | Extension Method | Successfully chains Result validation | Passed |
| 15 | `Currying_TwoScorers_DifferentResults` | Currying | Economy = 1300, Premium = 3500 on same input | Passed |
| 16 | `DeferredExecution_DoesNotRunUntilIterated` | Deferred Execution | Counter 0 before iteration, 2 after ToList() | Passed |
| 17 | `NativeFee_ValidInput_CalculatesCorrectly` | Native P/Invoke | Valid call returns 700 (standard), 1000 (fragile) | Passed |
| 18 | `NativeFee_InvalidInput_ReturnsFailure` | Native Adapter | Invalid input returns Result.Failure | Passed |
| 19 | `NormalWorkflow_FullPipeline_ProducesCorrectResult` | End-to-End Workflow | Full pipeline with reports produces correct result | Passed |
| 20 | `FileExport_WritesAndReadsBack` | Stream I/O | Exported CSV file written and verified | Passed |

---

## Author & Contribution

- **Diyar Kabyken (Group IT-2503)**:
  - Designed and implemented custom extension methods (`ThenValidate`, `ToSummary`).
  - Implemented LINQ reports with multi-stage transformations, grouping, and deterministic tie-breaking.
  - Implemented curried scoring functions and partially applied configurations.
  - Implemented deferred quote generation using `yield return` and demonstrated timing with probes.
  - Implemented safe file loading and exporting using `using` stream patterns.
  - Implemented native C library (`calculate_delivery_fee`), C# P/Invoke binding, and domain adapter.
  - Authored comprehensive test suite (20 automated tests) and documentation.
