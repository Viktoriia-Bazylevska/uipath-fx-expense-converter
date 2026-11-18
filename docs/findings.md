# Exercise 4 findings: REST API Integration with Coding Agents

**Author:** Viktoriia Bazylevska
**Date:** November 2026
**Repo:** https://github.com/Viktoriia-Bazylevska/uipath-fx-expense-converter
**Evidence:** Autopilot session history for ExpenseClaimConverter (16–17 Nov 2026), Studio Workflow Analyzer, test runs, Autopilot review reports

## 1. Objective

Investigate whether coding agents can accelerate the development of API integrations and reusable components.

## 2. What was built

An expense-claim currency converter that converts expense lines to EUR using ECB reference rates from the public Frankfurter API.

| Component | Type | Purpose |
| --- | --- | --- |
| `Main.xaml` | Low-code workflow (entry point) | Holds the run settings (input path, output path, target currency) with default values and invokes the coded workflow |
| `ConvertExpenseClaim.cs` | C# coded workflow | Reads the CSV, validates each line, gets rates (cached per currency and date), converts and rounds amounts, writes the output CSV with a total |
| `ExpenseLine.cs`, `ExpenseCalculator.cs`, `ExpenseCsv.cs` | Code source files | Data model, validation and calculation logic, CSV reading and writing |
| `GetExchangeRate` (FX.Rates library 1.0.2) | Low-code workflow, published as a library activity | Calls the Frankfurter API: input validation, HTTP Request, Try Catch, Retry Scope, Log Message |
| `ConvertExpenseClaimTests.cs` | Coded test case | 7 verifications: rounding, same currency, validation, totals |
| `WorkflowCoverageTests.xaml` | Low-code test case | Added by Autopilot to cover `Main.xaml` and `GetExchangeRate.xaml` (rule TA-DBP-002) |

**Flow:** `expenses.csv` → `Main.xaml` → `ConvertExpenseClaim.cs` → `GetExchangeRate` (FX.Rates) → Frankfurter API → `expenses_converted.csv`

**Tools:** UiPath Studio Desktop 26.10 with Autopilot (coding agent), model GPT-5.6 Luna, Community plan; Git and GitHub through Studio's Source Control panel. Project target: Windows, C# expressions.

## 3. Exercise activities: how each was covered

| Activity | How it was covered |
| --- | --- |
| Select a public REST API | Frankfurter (frankfurter.dev): free, open-source, central-bank reference rates |
| Use coding agents to generate the solution | Autopilot generated both workflows, helper classes and tests from prompts: 46 prompts in total (see `docs/prompts.md`) |
| API connectivity | HTTP Request activity in `GetExchangeRate` (UiPath.WebAPI.Activities 2.5.2, pinned) |
| Authentication | Not applicable: Frankfurter is a public, keyless API. No credentials are stored or logged |
| Error handling | Input validation; Try Catch; Retry Scope (`NumberOfRetries = 3`, 2-second interval, 10-second HTTP timeout) that retries timeouts and HTTP 5xx only; 4xx and other connection errors are handled without retry; clear error messages; failed lines do not stop the run |
| Logging | Log Message at Info, Warning and Error levels; no full URLs, responses or data rows logged |
| Test and validate | 7 low-code test cases, end-to-end run with 8 sample lines, coded test case (7 verifications), coverage test (4 verifications), two Autopilot review reports |
| Document manual intervention | Intervention log below |

## 4. Intervention log

Times are hands-on minutes I logged. Prompt counts come from the Autopilot session history.

| Step | What Autopilot did | Manual work or correction needed | Time (min) | Prompts |
| --- | --- | --- | --- | --- |
| Project set-up | Created the docs folders and updated `.gitignore`; found the CSV and JSON files already in place | Prompt sent twice; the prompt still contained `<paste …>` placeholders, so the test files were created by hand | 3 | 2 |
| Conventions | Accepted the conventions; pointed out the project is Windows, not cross-platform as the prompt said | None | — | 1 |
| GetExchangeRate.xaml | Planned (one plan revision), built, added and pinned the WebAPI package, ran it: rate 0.89367 for USD→EUR on 3 Nov | None | 3 | 2 |
| Validation, errors, logging | Added validation, Try Catch, Retry Scope and Log Message | One analyzer error (`CultureInfo` namespace, CS0103) fixed by a follow-up prompt | 3 | 5 |
| Low-code tests | Ran 7 cases, reported pass/fail with logs | 5 of 7 passed; 2 differed from my expectations (see section 8) | 3 | 1 |
| ConvertExpenseClaim.cs | Planned and built 4 files; explained the code and listed its assumptions | Changed date parsing to the fixed format `yyyy-MM-dd` (`TryParseExact`) by prompt | 3 | 4 |
| Coded test and end-to-end run | Created the coded test case (7 verifications, all passed); ran the sample CSV: 5 converted, 3 failed as expected, total 919.91 EUR | None | 5 | 2 |
| Reviews | Read-only review of GetExchangeRate and of the whole project | Findings recorded (section 6); not all fixed | — | 2 |
| Analyzer and validation messages | Fixed C# style diagnostics (IDE0056, IDE0063 and others); added coverage tests for rule TA-DBP-002 | Several rounds: 1 error, 16 messages, TA-DBP-002, 15 warnings | — | 6 |
| Orchestrator assets | Changed the paths to read from assets FX_InputPath and FX_OutputPath | The run then failed (assets did not exist, HTTP 404); Autopilot diagnosed it | — | 2 |
| Entry point | Deleted the empty `Main.xaml`; later created a new `Main.xaml` with default arguments | Needed because the path defaults had been removed (see "Annotations") and the asset fallback failed; run with no arguments then worked | 5 | 2 |
| Annotations | First prompt named no file, so Autopilot documented `ConvertExpenseClaim.cs` and also removed its path defaults; the XAML annotations were added on the 2nd and 3rd prompt | Checked and finished the XAML annotations and argument descriptions by hand | 5 | 4 |
| Library export | Cannot run Export As → Library | Exported FX.Rates in Studio; the export included `ConvertExpenseClaim`; removed it and republished as 1.0.2 | 2 | 0 |
| Use the library | Switched `ConvertExpenseClaim.cs` to `fXRates.GetExchangeRate(...)`, deleted the local copy, ran the sample CSV | 14 CS0436 type-conflict warnings appeared (the library still contains helper types with the same names); Autopilot suppressed them rather than removing the duplicates | — | 2 |
| Publish | Packed the project, but publishing failed (folder "Personal Workspace" not found); the retry hung | Stopped Autopilot and published by hand (about 2 min) | 5 | 6 |
| Git and GitHub | First attempt rejected at the confirmation step; second attempt committed locally but did not push (no `origin` remote) | Committed and pushed in Studio's Source Control panel (about 1 min per commit) | 2 | 2 |

## 5. Answers to the key questions

| Key question | Answer | Evidence |
| --- | --- | --- |
| Can coding agents accelerate API development? | **Yes** | About 4.5–5 hours in total (preparation including the step-by-step guide ~2 h, build with Autopilot ~1 h, human review 30–60 min, documentation ~1 h), compared with an estimated 11–18 hours for a junior developer by hand |
| What level of quality is produced? | **Good for a proof of concept, not production-ready without review** | Code built and validated cleanly (0 analyzer errors). 5 of 7 low-code cases passed first time, the coded test and the end-to-end run passed. Autopilot's own reviews found 3 High issues in GetExchangeRate and rated the whole project "Needs Improvement" (no Critical findings) |
| How much manual correction is required? | **Moderate: low in the code, high in platform steps** | 46 prompts in total, about 3 times the 14 planned. Extra prompts went on analyzer fixes, an unclear prompt that changed the wrong file, and platform steps: Autopilot's publish failed and its Git push did not happen, so both were done by hand |
| Can the generated solution be reused? | **Yes, with one cleanup** | `GetExchangeRate` was exported to the FX.Rates library (manual export) and is reused by `ConvertExpenseClaim.cs` through `fXRates.GetExchangeRate(...)`. The library still contains duplicate helper types, which cause CS0436 warnings in the consumer |

### Estimate: building it by hand (junior developer, no AI assistant)

| Part | Estimate |
| --- | --- |
| GetExchangeRate.xaml: HTTP Request, JSON parsing, validation, Try Catch, retries, logging | 3–5 h |
| ConvertExpenseClaim.cs: CSV reading and writing, validation, cache, rounding, totals in C# | 4–6 h |
| Coded test case | 1–2 h |
| Testing and debugging all cases | 2–3 h |
| Main.xaml, library export, documentation | 1–2 h |
| **Total** | **≈ 11–18 h (about 2 working days)** |

A mid-level developer would need roughly 6–8 h. These are estimates, not measurements.

**With Autopilot (first build):**

| Part | Time |
| --- | --- |
| Preparation: API choice, contracts, test data, step-by-step guide and prompts | ~2 h |
| Build with Autopilot, including manual steps | ~1 h |
| Human review: code, Autopilot's assumptions, results and logs | 30–60 min |
| Documentation: findings, READMEs, prompts list | ~1 h |
| **Total** | **≈ 4.5–5 h** |

Compared with 11–18 hours by hand, this is roughly 2.5–4 times faster. Not included: personal learning time for UiPath Autopilot and its documentation, because an experienced developer would not need it, and the presentation. A repeat build can reuse the guide and prompts, so preparation would be shorter.

The Autopilot sessions themselves ran on 16 Nov (16:48–21:21) and 17 Nov (10:32–14:27). That elapsed time includes Autopilot's processing, waiting for answers and breaks, so it is longer than the hands-on time.

## 6. Quality observations

**What Autopilot did well**
- Built low-code and C# workflows that followed the requested contracts and naming conventions, and pinned package versions.
- Validated and built after every change, and reported analyzer results honestly, including warnings it could not fix.
- Asked before running workflows and before publishing, as instructed.
- Made sensible decisions of its own: caching failed lookups, totalling the rounded amounts, stopping the run on a malformed CSV.
- Diagnosed failures well, e.g. the missing Orchestrator assets (HTTP 404) and the CS0436 type conflicts.

**What Autopilot's own reviews found**
- GetExchangeRate review: 3 High, 7 Medium, 2 Low findings. High: the workflow was not yet called from the entry point (later fixed); connection errors other than timeouts are not retried (**still open**, confirmed in FX.Rates 1.0.2); failed lookups can finish as a successful job because errors are returned, not thrown.
- Whole-project review: "Needs Improvement", no Critical findings, 11 warnings. Examples: output file can be overwritten without checks; relative paths depend on the robot's working directory; `requiresUserInteraction` is set to true for a headless process; tests do not cover CSV parsing, HTTP error branches or retries.

**What needed a person**
- An unclear prompt (no file named) made Autopilot change the wrong file and remove the path defaults, which later broke the run. Prompts must name the exact file.
- XAML annotations needed three prompts and a manual check.
- Some assumptions needed a decision: Autopilot used flexible date parsing (`DateTime.TryParse`); changed by prompt to one fixed format (`TryParseExact`, `yyyy-MM-dd`) so expense dates cannot be misread.
- Platform actions: package install for the library, library export, publishing and Git push were done by hand. Autopilot's publish failed and its push did not run.
- The Studio runner reported "Execution aborted" (ST-ANA-003, ST-ANA-009) on every run although the workflows completed and all checks passed. This had to be interpreted by a person.

## 7. Reuse

- `GetExchangeRate` is a self-contained component with typed in/out arguments and no project-specific paths.
- It was exported as the **FX.Rates** library and published to the Orchestrator tenant feed; the current version, 1.0.2, no longer contains `ConvertExpenseClaim`. The process has no local copy: `ConvertExpenseClaim.cs` calls the packaged activity with `fXRates.GetExchangeRate(...)`. Any developer can install the package and use **Get Exchange Rate** from the Activities panel, in low-code or coded workflows.
- Open cleanup: FX.Rates 1.0.2 still contains helper types (`ExpenseCalculator`, `ExpenseLine` and others) that clash with the consumer project (CS0436). Removing them in a 1.0.3 release is cleaner than suppressing the warnings.
- Possible uses: invoice processing, purchase orders, travel and expense reports, finance reconciliation, reporting in one currency.

## 8. Test results

| Test set | Cases | Passed first run | Notes |
| --- | --- | --- | --- |
| GetExchangeRate (low-code) | 7 | 5 of 7 | Case 2 (Saturday): the API returned a rate dated Saturday itself, so no previous-business-day fallback or Warning happened; my expectation was wrong, not the workflow. Case 6 (unsupported currency XYZ): correct result and message, but logged as Warning where I expected Error |
| End-to-end (sample CSV) | 8 lines | 8 of 8 as designed | 5 converted, 3 failed as expected (unsupported currency, negative amount, future date); total 919.91 EUR. The Saturday line used the API's Saturday rate |
| Coded test case `ConvertExpenseClaimTests.cs` | 1 test case, 7 verifications | 7 of 7 | Rounding, same currency, validation, totals |
| Coverage test `WorkflowCoverageTests.xaml` | 1 test case, 4 verifications | 4 of 4 | Same-currency path through `Main.xaml` and `GetExchangeRate.xaml`, no API call |

On every run the Studio runner showed "Execution aborted" with informational entries ST-ANA-003 and ST-ANA-009, although the workflows reached "Completed" and produced the expected output.

Screenshots: `docs/screenshots/`

## 9. Other notes

- **Autopilot credit usage:** not measured. The `/usage` command did not respond in Studio Desktop Autopilot.
- **Model used:** GPT-5.6 Luna
- **Remaining analyzer warning:** ST-USG-034 (Automation Hub URL required by organization policy), left empty on purpose.
- **Rates:** central-bank reference rates, not bank or card rates.

## 10. Conclusion

Coding agents clearly speed up building API integrations, and the generated code is good enough for a proof of concept. It is not production-ready without review: Autopilot's own reviews found open issues, and a vague prompt caused an unwanted change.

**Takeaway:** use coding agents to build and test; keep platform steps such as publishing, packages and Git manual. A coding agent does not check the compliance of the code it generates, so a person must review it before the automation runs against live systems.

**A coding agent should work for you, not think instead of you.**
