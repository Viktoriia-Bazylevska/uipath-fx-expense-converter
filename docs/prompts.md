# Autopilot prompts

All prompts sent to UiPath Autopilot (Studio Desktop, model GPT-5.6 Luna) for the ExpenseClaimConverter project, in order. Exported from the Autopilot session history; personal details removed.

Total: 46 prompts.

## 1. 16 Nov 2026, 16:48

```text
Set up the project structure. Create these folders and files in the project folder:
- Data/Input/expenses.csv with this content: <paste expenses.csv>
- Data/Output/ (empty)
- test-data/sample-response.json with this content: <paste sample-response.json>
- docs/prompts.md with the heading "# Autopilot prompts"
- docs/findings.md with the heading "# Exercise 4 findings"
- docs/screenshots/ (empty)
Also make sure .gitignore contains: .local/ .objects/ .screenshots/ *.nupkg Data/Output/
Do not change anything else.
```

## 2. 16 Nov 2026, 16:55

```text
Set up the project structure. Create these folders and files in the project if not created folder :
- Data/Input/expenses.csv with this content: <paste expenses.csv>
- Data/Output/ (empty)
- test-data/sample-response.json with this content: <paste sample-response.json>
- docs/prompts.md with the heading "# Autopilot prompts"
- docs/findings.md with the heading "# Exercise 4 findings"
- docs/screenshots/ (empty)
Also make sure .gitignore contains: .local/ .objects/ .screenshots/ *.nupkg Data/Output/
Do not change anything else.
```

## 3. 16 Nov 2026, 16:58

```text
Project conventions for this session:
- This is a cross-platform C# process named ExpenseClaimConverter.
- Prefer native UiPath activities and APIs. Do not use Invoke Code. Ask before adding any NuGet package and pin versions in project.json.
- Every external call sits inside Try Catch; errors are logged and returned, never swallowed.
- Use Log Message (Info/Warning/Error) at key points; never log full URLs, whole responses or full data rows.
- Clear names: in_/out_ prefixes for arguments, descriptive activity display names.
- Run Analyze Project and fix all errors and warnings before saying a task is done.
- Ask me before publishing anything to Orchestrator.
Remember these conventions for this project.
```

## 4. 16 Nov 2026, 19:24

```text
Plan a new low-code workflow GetExchangeRate.xaml. Do not create anything until I approve.

Goal: return the exchange rate between two currencies on a given date from the public Frankfurter API (no authentication).

Arguments:
- in_BaseCurrency (String), in_QuoteCurrency (String), in_RateDate (DateTime)
- out_Success (Boolean), out_Rate (Decimal), out_RateDate (DateTime), out_ErrorMessage (String)

Behaviour:
1. If in_BaseCurrency equals in_QuoteCurrency, set out_Success = True, out_Rate = 1, out_RateDate = in_RateDate, and skip the API call.
2. Otherwise send GET https://api.frankfurter.dev/v2/rates?date={yyyy-MM-dd}&base={base}&quotes={quote} with the HTTP Request activity.
3. Parse the JSON response and set out_Rate and out_RateDate from it (use the date the API returns, not the requested date).

Here is a real sample response: <paste test-data/sample-response.json>

List the activities in order with their display names, the packages needed, and how the response is parsed.
```

## 5. 16 Nov 2026, 19:28

```text
The plan is approved. Build GetExchangeRate.xaml. Then run it with in_BaseCurrency = USD, in_QuoteCurrency = EUR, in_RateDate = 2026-11-03, and show me the output arguments and the log.
```

## 6. 16 Nov 2026, 19:38

```text
Add input validation at the start of GetExchangeRate.xaml, before the HTTP call:
- in_BaseCurrency and in_QuoteCurrency must be exactly 3 letters; trim and convert to upper case.
- in_RateDate must not be in the future.
If validation fails, set out_Success = False and out_ErrorMessage = "Invalid input: <field and reason>", log a Warning and do not call the API.
```

## 7. 16 Nov 2026, 19:41

```text
analyze project - 1 error - fix it
```

## 8. 16 Nov 2026, 19:48

```text
Wrap the HTTP Request and the JSON parsing in a Try Catch.
- Check the HTTP status code: 2xx continues; 404 or 422 -> out_ErrorMessage = "Currency or date not supported"; 5xx -> "Rate service unavailable, try again later".
- If the response has no rate for the quote currency -> "No rate returned for <quote>".
- Catch timeouts and other exceptions -> "Rate service unavailable, try again later".
- Add a Retry Scope around the HTTP call for 5xx and timeouts only: 3 attempts, 2 seconds apart. Never retry 4xx.
- Every path must end with out_Success set; never leave the Catch block empty.
```

## 9. 16 Nov 2026, 19:53

```text
Add Log Message activities:
- Info at start: "GetExchangeRate started" with base, quote and date
- Info on success: "Rate received" with rate and rate date
- Warning when the returned rate date differs from the requested date (weekend or holiday)
- Warning when validation fails
- Error in the Catch block with the status code and a short message
Do not log the full URL or the full response body.
```

## 10. 16 Nov 2026, 19:58

```text
Run Analyze Project and fix all errors and warnings if they are. Then review GetExchangeRate.xaml for missing error handling, silent failures, unclear names and activity choice. Give findings with severity (high/medium/low). Do not change anything during the review.
```

## 11. 16 Nov 2026, 20:07

```text
Run GetExchangeRate.xaml once for each of these 7 cases: <project folder>\test-data\test-cases.xlsx
Show a pass/fail table with the actual output arguments for each case and the log lines. If a case fails, explain why before changing anything.
```

## 12. 16 Nov 2026, 20:14

```text
Plan a C# coded workflow ConvertExpenseClaim.cs and make it the entry point of the project. Do not create anything until I approve.

Arguments: in_InputPath (String, default "Data/Input/expenses.csv"), in_OutputPath (String, default "Data/Output/expenses_converted.csv"), in_TargetCurrency (String, default "EUR").

Behaviour:
1. Read the CSV (columns LineId, ExpenseDate, Description, Amount, Currency).
2. For each line, validate: Amount is a number > 0, Currency is 3 letters, ExpenseDate is a valid date not in the future. If invalid, set Status = "Failed" and Message = the reason, log a Warning, and continue with the next line.
3. If Currency equals in_TargetCurrency, use rate 1 without a lookup.
4. Otherwise get the rate by invoking GetExchangeRate.xaml (arguments as defined in that workflow). Cache results in a dictionary keyed by currency + date, so each pair is looked up once.
5. If GetExchangeRate returns out_Success = False, mark the line Failed with its out_ErrorMessage.
6. ConvertedAmount = Math.Round(Amount * Rate, 2, MidpointRounding.AwayFromZero), using decimal, never double.
7. Write the output CSV: the input columns + Rate, RateDate, ConvertedAmount, Status (Converted/Failed), Message, then a TOTAL row with the sum of converted lines.
8. Log Info at start and end (lines read, converted, failed, total in target currency), Warning per failed line, Error for unexpected exceptions; wrap the whole run in try/catch.

Structure: put the line model in ExpenseLine.cs and the validation and calculation in a static class (e.g. ExpenseCalculator.cs) as code source files, so they can be unit-tested. Keep Execute() short.

Show me the files, classes, methods and the order of work.
```

## 13. 16 Nov 2026, 20:25

```text
Run Analyze Project and fix all errors and warnings, and show me the results.
```

## 14. 16 Nov 2026, 20:28

```text
Explain ConvertExpenseClaim.cs section by section: how one line flows through validation, the cache, GetExchangeRate, rounding and output. List every assumption you made.
```

## 15. 16 Nov 2026, 20:39

```text
Change date parsing in ConvertExpenseClaim to accept only yyyy-MM-dd using DateTime.TryParseExact with InvariantCulture. Any other format fails the line with "Invalid date format, expected yyyy-MM-dd". Then rerun Analyze Project and the sample CSV.
```

## 16. 16 Nov 2026, 20:49

```text
Create a coded test case ConvertExpenseClaimTests.cs that tests ExpenseCalculator without calling the API:
- rounding: 10.005 -> 10.01, 2.344 -> 2.34
- same currency gives rate 1
- negative amount, invalid currency code and future date are rejected with a clear message
- the TOTAL sums only converted lines
Use UiPath testing verifications. Run the test case and show the results.
```

## 17. 16 Nov 2026, 20:55

```text
Review the whole project for security issues, incomplete error handling, activity and API choices, and anything that could fail silently. Give findings with severity. Do not change anything.
```

## 18. 16 Nov 2026, 21:00

```text
go through 16 validation messages, fix them
```

## 19. 16 Nov 2026, 21:19

```text
[Pasted list of Workflow Analyzer validation messages]
```

## 20. 16 Nov 2026, 21:21

```text
Run and check if the project works well
```

## 21. 17 Nov 2026, 10:32

```text
Can you please fix this error:
[{"RuleName":"Untested Workflows","Code":"TA-DBP-002","Description":"Workflow GetExchangeRate.xaml does not have any assigned Test Cases.","DisplayNameFile":null,"FullFilePath":null,"Severity":3}]
```

## 22. 17 Nov 2026, 10:44

```text
Run GetExchangeRateWorkflowTests.xaml locally and show the verification results
```

## 23. 17 Nov 2026, 10:45

```text
Run the test again without skipping the build and compare the runner output
```

## 24. 17 Nov 2026, 10:53

```text
Publish ExpenseClaimConverter to my Orchestrator folder <Personal Workspace> with version notes "Exercise 4 PoC". Ask me before publishing, and show me the result.
```

## 25. 17 Nov 2026, 10:57

```text
Show me the published package version and Orchestrator result
```

## 26. 17 Nov 2026, 11:01

```text
Show me the accessible Orchestrator folders and their keys
```

## 27. 17 Nov 2026, 11:02

```text
<personal workspace> - use this folder
```

## 28. 17 Nov 2026, 11:09

```text
Show me the publish result when available
```

## 29. 17 Nov 2026, 11:17

```text
stop publishing
```

## 30. 17 Nov 2026, 11:27

```text
Remove the default values from in_InputPath and in_OutputPath. Read them from Orchestrator assets named FX_InputPath and FX_OutputPath when the arguments are empty. Keep in_TargetCurrency defaulting to EUR.
```

## 31. 17 Nov 2026, 11:33

```text
GetExchangeRate.xaml has errors, find and fix them, they are in Workflow Analizer also
```

## 32. 17 Nov 2026, 11:39

```text
Run WorkflowCoverageTests.xaml locally and show the verification results
```

## 33. 17 Nov 2026, 11:48

```text
[Pasted Studio error output: job failed in ConvertExpenseClaim.cs]
```

## 34. 17 Nov 2026, 12:07

```text
Check which file is the main entry point in project.json. Make ConvertExpenseClaim.cs the main entry point, then delete the empty Main.xaml. Run Analyze Project and run the project once to confirm it still works.
```

## 35. 17 Nov 2026, 12:12

```text
Show me git status. Then stage all changes, commit with the message "Main was deleted", and push to origin main. Show me the result.
```

## 36. 17 Nov 2026, 12:13

```text
Show me git status. Then stage all changes, commit with the message "Main was deleted", and push to main. Show me the result.
```

## 37. 17 Nov 2026, 12:28

```text
Add a clear workflow annotation and a description for every argument (shown as tooltips), and make sure it has no dependency on project-specific paths. Do not change its behaviour. Run Analyze Project.
```

## 38. 17 Nov 2026, 12:44

```text
Add a clear GetExchangeRate workflow annotation and a description for every argument (shown as tooltips), and make sure it has no dependency on project-specific paths. Do not change its behaviour. Run Analyze Project.
```

## 39. 17 Nov 2026, 12:53

```text
In GetExchangeRate.xaml, edit the XAML file directly: add an sap2010:Annotation.AnnotationText attribute to the root Sequence with the text "Returns the ECB reference exchange rate between two currencies on a date (Frankfurter API).", and add an annotation to each x:Property argument describing it. Do not change any activity or logic. Then reopen the file and confirm the annotations show in the designer.
```

## 40. 17 Nov 2026, 12:58

```text
List every activity in GetExchangeRate.xaml in order, with its display name, type and all its property values (for Assign: To and Value; for Log Message: Level and Message). Do not change anything.
```

## 41. 17 Nov 2026, 13:44

```text
FX.Rates is installed as a dependency. Update ConvertExpenseClaim.cs to use GetExchangeRate from FX.Rates instead of the local file, then delete the local GetExchangeRate.xaml. Run Analyze Project and the sample CSV.
```

## 42. 17 Nov 2026, 13:51

```text
15 validation warnings in workflow analizer, check and fix
```

## 43. 17 Nov 2026, 14:27

```text
Create Main.xaml as the project's main entry point:
- In arguments with default values: in_InputPath = "Data/Input/expenses.csv", in_OutputPath = "Data/Output/expenses_converted.csv", in_TargetCurrency = "EUR".
- Log Info "ExpenseClaimConverter started", then invoke ConvertExpenseClaim.cs passing the three arguments, then log Info "ExpenseClaimConverter finished".
- Set Main.xaml as the main entry point in project.json; ConvertExpenseClaim.cs is no longer the entry point.
- Keep ConvertExpenseClaim's own validation, but make empty arguments fall back to the same defaults as a safety net.
Run Analyze Project, then run the project with no arguments and show me the result.
```

## 44. 17 Nov 2026, 18:08

```text
Coded test case: give me number of test cases
```

## 45. 17 Nov 2026, 18:11

```text
confirm GetExchangeRate actually has the Retry Scope (3 attempts, 5xx and timeouts only), does not change anything
```

## 46. 17 Nov 2026, 18:16

```text
Date parsing: did you switch to TryParseExact with yyyy-MM-dd? Tell me yes or no and I’ll update the wording.
```

