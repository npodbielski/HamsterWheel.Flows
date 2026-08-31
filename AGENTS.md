# Agent instructions

Standard rules for AI coding agents (and humans) working in this repository.

## Git workflow

- **Never merge a merge request without explicit approval** from the maintainer.
- **Never push directly to `master`.** All changes go through merge requests.
- **Merge requests target `alpha` first**, not `master`. Alpha builds and publishes
  prerelease NuGet packages (`package-alpha` job) so the change can be validated
  by prerelease consumers before it reaches production.
- **Merges to `master` always come from `alpha`** — an `alpha → master` MR after the
  prerelease has been validated. `master` publishes the production package to
  NuGet.org (`package` job).

Flow: `feature branch` → MR → `alpha` (prerelease validated) → MR → `master` (release).

## Pipeline

- `build` — builds `HamsterWheel.Flows.slnx`
- `style-check` — `dotnet format HamsterWheel.Flows.slnx style --verify-no-changes --severity error`
- `tests` — unit tests with coverlet + coverage gate (80% line-rate), then integration tests
- `package-alpha` — on `alpha` only: packs `-alpha-<job_id>` prerelease into the project registry
- `package` — on `master` only: packs and pushes to the project registry and NuGet.org

## Definition of done (before pushing)

From the repository root:

1. `dotnet test --no-build` — all tests green (whole solution, no coverage).
2. `rm -rf test-results && dotnet test --no-build test/HamsterWheel.Flows.Tests/HamsterWheel.Flows.Tests.csproj --coverlet --coverlet-output-format cobertura --results-directory ./test-results`
   and `bash ci/coverage-gate.sh ./test-results` — coverage of the HamsterWheel.Flows
   package (Cronos/Humanizer are excluded via `testconfig.json`) not below the gate.
3. `dotnet format HamsterWheel.Flows.slnx style --verify-no-changes --severity error`
4. Push the branch and monitor the pipeline via the GitLab API until it is green.
   Do not merge — wait for approval.

## Test conventions

- Microsoft.Testing.Platform + xUnit v3 + FluentAssertions; tests use arrange/act/assert comments.
- Integration tests live in `test/HamsterWheel.Flows.IntegrationTests` (part of the solution)
  and treat the library as a black box: public API only, no internals.

## Known quirks

- MTP: extra `dotnet test` CLI args are forwarded to the test host — never pass restore
  options (e.g. `--packages`) to it; restore separately.
- Cronos 0.11: the default `CronExpression.Parse` overload is `CronFormat.Standard` (5 fields);
  seconds require `CronFormat.IncludeSeconds`. `FlowScheduler` accepts both formats.
