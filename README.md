# UiPath FX Expense Converter

Converts expense-claim lines to EUR (or another currency) using ECB reference rates from the public [Frankfurter API](https://frankfurter.dev).

Built for **Exercise 4: REST API Integration with Coding Agents**. The workflows, helper classes and tests were generated with **UiPath Autopilot** (coding agent) in Studio Desktop, then reviewed and tested by hand.

## How it works

```text
expenses.csv → Main.xaml → ConvertExpenseClaim.cs → GetExchangeRate (FX.Rates library) → Frankfurter API
                                    ↓
                       expenses_converted.csv
```

| Component | Type | What it does |
| --- | --- | --- |
| `Main.xaml` | Low-code workflow (entry point) | Holds the run settings and invokes the coded workflow |
| `ConvertExpenseClaim.cs` | C# coded workflow | Reads the CSV, validates each line, gets rates (cached per currency and date), converts, rounds, writes the result with a total |
| `ExpenseLine.cs`, `ExpenseCalculator.cs`, `ExpenseCsv.cs` | Code source files | Data model, validation, calculation and CSV handling |
| `ConvertExpenseClaimTests.cs` | Coded test case | Tests rounding, validation and totals |
| `WorkflowCoverageTests.xaml` | Test case | Covers `Main.xaml` and `GetExchangeRate` without calling the API |
| `GetExchangeRate` | Activity from the **FX.Rates** library | Calls the Frankfurter API with validation, retries, error handling and logging |

## Requirements

- UiPath Studio 26.10 or later (Windows project, C# expressions)
- The **FX.Rates** library package installed (see `FX.Rates/README.md`)
- Internet access to `api.frankfurter.dev`. No API key is needed.

## How to run

1. Clone the repo and open `project.json` in UiPath Studio.
2. Check that FX.Rates is installed: **Manage Packages** → your Orchestrator tenant feed.
3. Run `Main.xaml`. Default arguments:

| Argument | Default |
| --- | --- |
| `in_InputPath` | `Data/Input/expenses.csv` |
| `in_OutputPath` | `Data/Output/expenses_converted.csv` |
| `in_TargetCurrency` | `EUR` |

4. Open `Data/Output/expenses_converted.csv`.

## Input and output

**Input** (`Data/Input/expenses.csv`): `LineId, ExpenseDate (yyyy-MM-dd), Description, Amount, Currency`

**Output:** the input columns plus `Rate, RateDate, ConvertedAmount, Status, Message`, and a final total row.

- `Status` is `Converted` or `Failed`. A failed line does not stop the run.
- `RateDate` can differ from `ExpenseDate` (for example a weekend uses the previous business day's rate).
- Amounts use `decimal` and are rounded to 2 places (`MidpointRounding.AwayFromZero`); the total sums the rounded converted lines.

The sample input has 8 made-up lines: 5 convert, 3 fail on purpose (unsupported currency, negative amount, future date).

## Error handling and logging

- Input validation before any API call (3-letter currency codes, valid dates, positive amounts)
- Try Catch around the HTTP call; Retry Scope (`NumberOfRetries = 3`, 2-second interval, 10-second timeout) for timeouts and HTTP 5xx only
- Clear messages: "Invalid input", "Currency or date not supported", "Rate service unavailable, try again later"
- Log Message at Info, Warning and Error levels; full URLs, responses and data rows are never logged

## Repository contents

| Path | Contents |
| --- | --- |
| `Data/Input/` | Sample test data (made up) |
| `test-data/` | Sample Frankfurter API response |
| `docs/prompts.md` | Every Autopilot prompt used |
| `docs/findings.md` | Exercise 4 evaluation: intervention log and answers to the key questions |
| `docs/screenshots/` | Test runs and logs |

## Notes

- **Authentication:** not applicable. Frankfurter is a public, keyless API, and this repo contains no secrets.
- Rates are ECB reference rates, not bank or card rates.
- This is a proof of concept. Autopilot's own review rated it "Needs Improvement" (no Critical findings); see `docs/findings.md`. Review it before using it against live systems.

## Author

Viktoriia Bazylevska
