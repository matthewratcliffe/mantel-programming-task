### 🧾 Mantel Coding Test — Log Analysis Console App

This repository contains a .NET 9 console application that parses a web access log and answers common queries:

- Count of unique IP addresses
- Top 3 most visited URLs
- Top 3 most active IP addresses

The app presents an interactive menu in the terminal. Navigation uses the Up/Down arrow keys and Enter to select.

---

### 📚 Table of Contents

- [🧰 Tech stack](#-tech-stack)
- [🏗️ This is wayyyyyyyyy over‑engineered by design, but there is a SimpleSolution.ps1](#️-this-is-wayyyyyyyyy-overengineered-by-design-but-there-is-a-simplesolutionps1)
- [🤖 AI usage in this project](#-ai-usage-in-this-project)
- [⚠️ Possible Issues Approaching This Problem](#️-possible-issues-approaching-this-problem)
- [🚀 Getting started](#-getting-started)
  - [📦 Prerequisites](#-prerequisites)
  - [📥 Clone the repository](#-clone-the-repository)
  - [🛠️ Build](#️-build)
  - [▶️ Run the console app](#️-run-the-console-app)
- [📄 Log file input](#-log-file-input)
- [✨ Features in detail](#-features-in-detail)
- [🗂️ Project structure](#️-project-structure)
- [⚙️ Configuration (optional: VirusTotal)](#-configuration-optional-virustotal)
- [🚨 Warning about VirusTotal API key](#-warning-about-virustotal-api-key)
- [🧪 Testing](#-testing)
- [📊 Code Coverage](#-code-coverage)
- [🔍 Code Quality & Static Analysis (SonarQube, Qodana)](#-code-quality--static-analysis-sonarqube-qodana)
- [📝 Development notes](#-development-notes)
- [🧠 Learnings & decisions](#-learnings--decisions)
  - [🔎 Research insights](#-research-insights)
  - [⚖️ Design choices and trade‑offs](#-design-choices-and-tradeoffs)
  - [❌ What didn’t work (and why)](#-what-didnt-work-and-why)
  - [🧪 Testing and operational notes](#-testing-and-operational-notes)
  - [🧭 Possible next steps](#-possible-next-steps)
- [🧮 Working Out](#-working-out)
  - [🧮 Unique IP Addresses](#-unique-ip-addresses)
  - [📈 Top X Most Visited URLs](#-top-x-most-visited-urls)
  - [📊 Top X Most Active IP Addresses](#-top-x-most-active-ip-addresses)
- [🆘 Troubleshooting](#-troubleshooting)
- [📜 License](#-license)

### 🧰 Tech stack

- .NET 9 (net9.0)
- MediatR for request/response application flow
- Microsoft.Extensions.DependencyInjection for DI
- Regex-based log parsing (Infrastructure layer)
- Optional VirusTotal client (Infrastructure) for file scanning utilities

---

### 🏗️ This is wayyyyyyyyy over‑engineered by design, but there is a SimpleSolution(.ps1)

This implementation is intentionally over‑engineered for the size of the problem. In a real‑world, one‑off scenario, a short PowerShell script would likely be sufficient. As a demonstration, a minimal script generated with ChatGPT is included at `SimpleSolution.ps1` in the repository root.

The additional architecture here is to showcase areas of consideration when approaching problems in a product/codebase context, including:

- Separation of concerns with clear layer boundaries (Application/Domain/Infrastructure/ConsoleApp)
- Dependency Injection and factories (e.g., `IVirusScanServiceFactory`) to enable configuration and swapping implementations
- Request/response use cases with MediatR for explicit, testable flows
- Interfaces/abstractions for IO and environment (`IFileReader`, `IConsole`, `IApplicationLifetime`, `ILogParser`)
- Concrete parsing behind an abstraction (Regex parser as the `ILogParser` implementation)
- Unit testing and integration testing projects and conventions
- Error handling and graceful shutdown through the application lifetime abstraction
- Async programming patterns and resilient external calls (e.g., retry/backoff in `VirusTotalClient`)
- Configuration via environment variables (e.g., `VIRUSTOTAL_API_KEY`)
- Console UI decoupled behind interfaces (`IMenu`, `IMenuItems`) and testability hints (`InternalsVisibleTo` for console tests)
- CI‑friendly test project naming to allow filtering by test type

If you just want the fastest path to answers and have PowerShell available, you can run the simple script from the repository root (may require PowerShell 7+ on macOS/Linux):

```
pwsh ./SimpleSolution.ps1
```

---

### 🤖 AI usage in this project

AI tools were intentionally used to accelerate parts of the workflow and validate assumptions:

- ChatGPT
  - Used for reasoning about and validating the nature of the sample log/input files (e.g., confirming Apache access log characteristics).
  - Assisted with quick ideation and lightweight verification of parsing approaches.
  - Used in whole to generate the `SimpleSolution.ps1` script.
- Claude
  - Used as a coding co‑pilot for small, well‑bounded development tasks.
  - Helped construct and refine unit tests.
- Junie
  - Used to scaffold and iteratively refine this `README.md` and related documentation sections.

All AI‑assisted outputs were reviewed and adapted to fit the project’s architecture, coding standards, and constraints.

## ⚠️ Possible Issues Approaching This Problem

**- Treating the log as a rigid format; it has anomalies compared to standard Apache combined logs.**

Parser is a compiled regex (RegexParserService, src/Infrastructure/Services/LogParser/RegexParser.cs) anchored only at
start (^) and not at end. It captures up through the closing user-agent quote and tolerates trailing junk (e.g., the
sample line with junk extra). Invalid lines are tagged as raw rather than failing. Provides resilience here, with tests
confirming behavior (e.g., Parse_MixedValidAndInvalidLines_HandlesCorrectly).

**- Request line can contain absolute URLs (with scheme/host) or relative paths; must normalize consistently.**

The GetTopVisitedUrlsQueryHandler groups by the literal path field emitted by the regex. Absolute
request targets (e.g., http://example.net/faq/) will be counted as distinct from /faq/. There’s no normalization to
extract the path from absolute URLs. Risk: over-splitting counts for same resource.

**- Deciding what “URL” means for counting: path-only vs full URL; handling of trailing slashes and case.**

Current behavior is literal: whatever appears in the captured path is used as the key. No trailing-slash normalization
and no case normalization are applied. If variants exist, they’ll be treated as different URLs. This should be
documented or adjusted depending on desired semantics.

**- Assuming the authenticated user field is always -; some lines include a username (e.g., admin).**

Regex captures authuser as \S+, so entries like admin are parsed correctly. Handlers don’t rely on this field, so no
fragility here. Covered by tests for general field extraction.

** - Naively splitting by spaces instead of parsing bracketed/quoted sections, causing breakage with spaces inside.**

Avoid naive Split(' ') and use a robust regex that respects [] and "" boundaries.

**- Extra trailing tokens after user-agent (e.g., junk extra, or additional numbers) can break strict parsers.**

Because the regex doesn’t anchor at end, extra tokens after the user-agent are ignored, allowing lines like the
provided junk extra to parse successfully.

**- Leading zero octets in IPs (50.112.00.28) may affect IP validation/normalization and unique counts.**

Unique IPs and “most active IPs” use literal strings from the ip group (GetUniqueIpAddressesQueryHandler,
GetTopMostActiveIpAddressesQueryHandler). IPs like 50.112.00.28 won’t be normalized; they’ll be treated as distinct
if a normalized equivalent also appears. May need to consider normalization.

**- Not specifying tie-breakers for “top 3” when counts are equal (deterministic ordering).**

Handlers use deterministic ordering: they OrderByDescending(count) then ThenBy(item) (lexicographic) and then group by
equal counts into ranks. This provides stable tie-breaking and a clean “place-based” result.

**- Assuming only GET requests; method could vary (robustness concern).**

The regex captures method generically (\S+), and the counting logic ignores method entirely (counts any parsed request).

**- Accidentally filtering by status code; the task implies counting all requests (200/301/307/404/500, etc.).**

No filtering by status: both handlers count all parsed entries (ip or path) regardless of status. This matches the task
unless you explicitly want to exclude redirects/errors. This would be clarified in requirements workshopping ideally.

**- Mishandling timestamp with timezone offset due to naive regex.**

Regex captures the entire timestamp field as a single string; it is not parsed into a DateTime, so no timezone pitfalls.
If date based analysis is required, consider using a more robust parser in future.

**- Not handling malformed lines gracefully (skip/log vs crash or miscount).**

Malformed lines are added as { Key = "raw", Value = line } with correct LineNumber.
They do not impact counts, since query handlers filter on specific keys (ip, path). Tests cover this path.

**- Performance: loading whole file vs streaming for large inputs.**

RegexParserService.Parse converts all bytes to a string and splits by \n into memory.
This is fine for the exercise-sized file, but will not scale to huge logs.
If needed, switch to a streaming line-by-line reader.

**- Missing or ambiguous test coverage for edge cases and normalization choices.**

Tests cover: empty input, invalid lines, multiple lines/line numbers, special chars in path, different statuses,
empty referrer/user-agent, large user-agent, mixed valid/invalid. However, there isn’t an explicit test for
absolute URL vs relative path normalization or for lines with trailing tokens like the sample’s junk extra.

Also not tested: leading-zero IP handling or deterministic tie-breaking in the presence of equal counts for URLs
containing absolute vs relative forms.

---

### 🚀 Getting started

#### 📦 Prerequisites

- .NET SDK 9.x installed

Verify your version:

```
dotnet --version
```

#### 📥 Clone the repository

```
git clone <repo-url>
cd MantelCodingTest
```

#### 🛠️ Build

```
dotnet build MantelCodingTest.sln
```

#### ▶️ Run the console app

```
cd src/ConsoleApp
dotnet run
```

You will see the menu:

```
Log Analysis Menu
-----------------

→ Unique IP Addresses
  Top 3 Visited Urls
  Top 3 Most Active IPs
  Exit

Use arrow keys to navigate and Enter to select
```

Press Enter on a menu item to execute the query. After results, press any key to return to the menu.

---

### 📄 Log file input

- By default, the app reads `programming-task-example-data.log` from the current working directory at runtime.
- The `ConsoleApp` project is configured to copy this file to its output on build, so running from `src/ConsoleApp` will just work.

Implementation detail:

```
// Application/LogParse/Base/LogParseBase.cs
// Default file name
fileName = "programming-task-example-data.log";
```

If you want to analyze a different file, you can extend the handlers in `Application/LogParse/Queries` to accept a custom file name, or replace the file in the output directory before running.

---

### ✨ Features in detail

- Unique IP Addresses (`GetUniqueIpAddressesQuery`)
  - Parses the log and returns the count of distinct client IPs.

- Top 3 Visited URLs (`GetTopVisitedUrlsQuery`)
  - Groups by the `path` field and returns the top N (default 3) with visit counts.

- Top 3 Most Active IPs (`GetTopMostActiveIpAddressesQuery`)
  - Groups by `ip` and returns the top N (default 3) with request counts.

These queries are wired through MediatR and triggered from the menu in `ConsoleApp`.

---

### 🗂️ Project structure

```
MantelCodingTest.sln
src/
  Application/                 # Use cases and MediatR queries
    Interfaces/                # Abstractions for parsing, IO, app lifetime
    LogParse/
      Base/                    # Shared log parsing helpers (e.g., file resolution)
      Queries/                 # Query handlers (unique IPs, top URLs, top IPs)
  ConsoleApp/                  # Terminal app (menu) and DI composition root
    Interfaces/                # Console-specific abstractions and implementations
  Domain/                      # Domain models and value objects
  Infrastructure/              # Implementations (Regex parser, file IO, VirusTotal)
    Services/
      FileReader/
      LogParser/
      VirusScan/
tests/
  Test.UnitTests.*             # Unit tests for layers and features
  Test.UnitTests.Integration   # Integration tests
```

---

### ⚙️ Configuration (optional: VirusTotal)

The Infrastructure layer includes a `VirusTotalClient` wrapper. If you plan to use or extend virus scanning features, set the API key via environment variable before running or testing:

```
export VIRUSTOTAL_API_KEY=your_key_here   # macOS/Linux
setx VIRUSTOTAL_API_KEY your_key_here     # Windows (new shells)
```

The console menu provided in this kata does not directly expose a virus-scan option, but the client is available via DI: `IVirusScanServiceFactory` reads `VIRUSTOTAL_API_KEY` during service registration.

### 🚨 Warning about VirusTotal API key

If a `VIRUSTOTAL_API_KEY` is not provided, the application will fall back to the dummy virus scanner. The dummy scanner simulates detections with a 20% positive rate, which is intended for demonstration/testing only and does not represent real malware detection. For real scanning results, supply a valid VirusTotal API key.

---

### 📊 Code Coverage

![Line coverage](coverage/badge_linecoverage.svg)
![Branch coverage](coverage/badge_branchcoverage.svg)

Overall coverage (generated locally):

- Line coverage: 92.4% (365/395)
- Branch coverage: 90.4% (76/84)

Per assembly line coverage:

- Application: 97.8%
- ConsoleApp: 80.5%
- Domain: 100%
- Infrastructure: 100%

Full summary (details per class and file): `coverage/Summary.md`

How to regenerate this report locally:

```
# 1) Run tests with coverage (writes Cobertura XML under each test project's TestResults folder)
dotnet test MantelCodingTest.sln --collect:"XPlat Code Coverage"

# 2) Generate Markdown summary + badges into ./coverage
dotnet tool restore
dotnet tool run reportgenerator \
  -reports:"tests/**/TestResults/**/coverage.cobertura.xml" \
  -targetdir:"coverage" \
  -reporttypes:"MarkdownSummary;Badges" \
  -assemblyfilters:"+Application;+ConsoleApp;+Domain;+Infrastructure"
```

---

### 🔍 Code Quality & Static Analysis (SonarQube, Qodana)

- SonarQube: This solution was checked locally using SonarQube Community Edition with the default C# quality profile. The scan covered code smells, bugs, and security hotspots; no blocking issues were identified for the scope of this kata.
  - How to reproduce (example):
    1. Install `dotnet-sonarscanner` and run a local SonarQube server.
    2. From the repo root, run:
       ```
       dotnet sonarscanner begin /k:"MantelCodingTest" /d:sonar.host.url="http://localhost:9000" /d:sonar.login="<token>"
       dotnet build MantelCodingTest.sln
       dotnet sonarscanner end /d:sonar.login="<token>"
       ```

- Qodana: Also validated with JetBrains Qodana locally.
  - Settings summary (see `qodana.yaml`):
    - `version: 1.0`, `ide: QDNET` (JetBrains Rider/IDE-based inspections for .NET)
    - Profile: `qodana.starter`
    - Includes notable inspections such as: `ConvertIfStatementToSwitchStatement`, `ConvertToPrimaryConstructor`, `FieldCanBeMadeReadOnly.Local`, `MemberCanBeMadeStatic`, `UseCollectionExpression`, `UnusedMember.Global`, NUnit rules (`NUnit2010`, `NUnit2014`, `NUnit2045`, `NUnit2046`), and CA analyzers (`CA1822`, `CA1825`, `CA1861`), among others.
    - Quality gate examples are documented but commented out (severity thresholds and code coverage), meaning the default thresholds apply unless enabled in CI.
  - How to run (one option):
    - Via IDE (Qodana plugin) or with Docker/CLI following https://www.jetbrains.com/help/qodana/ . This repository already contains `qodana.yaml` for consistent local/CI runs.

Run all tests from the solution root:

```
dotnet test MantelCodingTest.sln
```

You can also execute test projects individually from their directories.

---

### 📝 Development notes

- Target framework: `net9.0`
- DI is composed in `src/ConsoleApp/Program.cs` using `ServiceCollectionExtensions.AddInfrastructureServices()` and `AddConsoleAppServices()`.
- The menu system is implemented in `src/ConsoleApp/Interfaces/Concrete/Menu.cs` and `MenuItems.cs`.
- Log parsing and file access details live under `src/Infrastructure/Services/LogParser` and `FileReader` with interfaces in `src/Application/Interfaces`.

---

### 🧠 Learnings & decisions

#### 🔎 Research insights

- Log type identification: Determined the sample is an Apache access log (not Nginx) based on dash patterns for missing fields, timestamp format, and the `GET /... HTTP/1.1` segment. Initial confirmation was cross‑checked with ChatGPT suggestions and pattern matching.
- Parsing options explored:
  - Considered `ApacheLogParser` (Apache‑specific).
  - Looked for a generic “detect log type then parse” library; `LogParserLib` was investigated but not available on NuGet at the time.
  - Full observability stacks (Splunk/ELK/Loki/Logstash/Grafana) would work but are overkill for this task.
- Virus scanning approach: For demo purposes used a `VirusTotal` client; in an enterprise setup you would typically use Microsoft Defender (e.g., via a storage account integration).
- Guiding principle: The problem is common and largely solved; compose proven components and add tests to keep the code simple and maintainable.
- Future exploration: Consider whether AI/NLP could assist with log insights beyond the required queries.

#### ⚖️ Design choices and trade‑offs

- Language/runtime: C# on .NET 9.
- Architecture: Followed a light Clean Architecture separation (Application/Domain/Infrastructure/ConsoleApp). Not strictly necessary, but keeps concerns clear and testing straightforward.
- MediatR v12: Selected due to licensing considerations and to keep request/response use cases explicit and testable.
- Parsing strategy: Chose a Regex‑based parser in Infrastructure to avoid heavy dependencies and ensure cross‑platform behavior.

#### ❌ What didn’t work (and why)

- `LogParserLib`: Couldn’t locate a usable NuGet package.
- Grok patterns:
  - `Grok.Net` was considered.
  - The grok parser approach had cross‑platform issues in practice for this scenario, so it was set aside in favor of a simpler Regex implementation.

#### 🧪 Testing and operational notes

- Solution layout: `src` and `tests` folders keep boundaries clear.
- Test project naming: Used `Test.UnitTests.*.csproj` conventions so CI/CD pipelines can filter by test type (e.g., future separation of unit vs integration).
- SDK client wrapper: Some test arguments are passed via the SDK client wrapper to validate expected SDK behavior; acceptable for this scope.

#### 🧭 Possible next steps

- Add a mode to select alternate log files at runtime (currently default file is copied to output).
- Introduce pluggable parsers (Apache, Nginx) with auto‑detection heuristics.
- Optional: Extend the console with a virus‑scan command using the existing `VirusTotal` wrapper when an API key is provided.
- Consider basic NLP summaries (e.g., “top anomalies” explanation) as an experimental feature.

### 🧮 Working Out

The below tables list the validation performed in a spreadsheet.

### 🧮 Unique IP Addresses

| IP            |
|---------------|
| 168.41.191.34 |
| 168.41.191.40 |
| 168.41.191.41 |
| 168.41.191.43 |
| 168.41.191.9  |
| 177.71.128.21 |
| 50.112.00.11  |
| 50.112.00.28  |
| 72.44.32.10   |
| 72.44.32.11   |
| 79.125.00.21  |

### 📈 Top X Most Visited URLs

| Page                                             | Page (Count All) |
|--------------------------------------------------|-------------------|
| /docs/manage-websites/                           | 2                 |
| /                                                | 1                 |
| /asset.css                                       | 1                 |
| /asset.js                                        | 1                 |
| /blog/2018/08/survey-your-opinion-matters/       | 1                 |
| /blog/category/community/                        | 1                 |
| /docs/                                           | 1                 |
| /docs/manage-users/                              | 1                 |
| /download/counter/                               | 1                 |
| /faq/                                            | 1                 |
| /faq/how-to-install/                             | 1                 |
| /faq/how-to/                                     | 1                 |
| /hosting/                                        | 1                 |
| /intranet-analytics/                             | 1                 |
| /moved-permanently                               | 1                 |
| /newsletter/                                     | 1                 |
| /temp-redirect                                   | 1                 |
| /this/page/does/not/exist/                       | 1                 |
| /to-an-error                                     | 1                 |
| /translations/                                   | 1                 |
| http://example.net/blog/category/meta/           | 1                 |
| http://example.net/faq/                          | 1                 |

### 📊 Top X Most Active IP Addresses

| IP            | Page (Count) |
|---------------|--------------|
| 168.41.191.40 | 4            |
| 177.71.128.21 | 3            |
| 50.112.00.11  | 3            |
| 72.44.32.10   | 3            |
| 168.41.191.34 | 2            |
| 168.41.191.43 | 2            |
| 168.41.191.9  | 2            |
| 168.41.191.41 | 1            |
| 50.112.00.28  | 1            |
| 72.44.32.11   | 1            |
| 79.125.00.21  | 1            |

---

### 🆘 Troubleshooting

- If the app immediately exits or reports it cannot read the log file, ensure `programming-task-example-data.log` exists in the working directory. Building `ConsoleApp` should copy it automatically to `bin/Debug/net9.0`.
- For tests involving VirusTotal, ensure the `VIRUSTOTAL_API_KEY` is set, or use unit tests that mock the client.

---

### 📜 License

This project is provided as part of a coding exercise. No explicit license is included.
