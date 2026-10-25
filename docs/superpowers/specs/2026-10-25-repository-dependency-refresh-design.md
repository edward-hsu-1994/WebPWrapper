# Repository Dependency Refresh — Design

**Status:** Approved by the user; implemented and merged in PR #24 (merge commit `b76752c`).

## Goal

Upgrade the repository's direct NuGet dependencies and GitHub Actions to current stable versions, migrate the test projects from xUnit v2 to the latest stable xUnit major, and remove the `Newtonsoft.Json 9.0.1` NU1903 security warning without changing the library's public API or target framework.

## Scope

The refresh covers package references in the library and both test projects, plus all GitHub Actions referenced by the repository workflows. The implementation stays on the existing library target `netstandard2.1` and test target `net10.0`.

| Dependency | Current | Planned target |
|---|---:|---:|
| `Microsoft.NET.Test.Sdk` | 16.0.1 | 18.11.0 |
| `xunit` | 2.4.0 | replace with `xunit.v3` 4.0.2 |
| `xunit.runner.visualstudio` | 2.4.0 | 4.0.1 |
| `SharpZipLib` | 1.4.2 | remain 1.4.2; this is the newest stable version currently listed on NuGet |
| `actions/checkout` | v4 | v7 |
| `actions/setup-dotnet` | v4 | v6 |
| `github/codeql-action` | v3 | v4 |
| `NuGet/login` | v1 | v1.2.0 |

NuGet package version evidence: [Microsoft.NET.Test.Sdk](https://api.nuget.org/v3-flatcontainer/microsoft.net.test.sdk/index.json), [xunit.v3](https://api.nuget.org/v3-flatcontainer/xunit.v3/index.json), [xunit.runner.visualstudio](https://api.nuget.org/v3-flatcontainer/xunit.runner.visualstudio/index.json), and [SharpZipLib](https://api.nuget.org/v3-flatcontainer/sharpziplib/index.json). Action target releases: [checkout v7.0.1](https://github.com/actions/checkout/releases/tag/v7.0.1), [setup-dotnet v6.0.0](https://github.com/actions/setup-dotnet/releases/tag/v6.0.0), [CodeQL Action v4.39.1](https://github.com/github/codeql-action/releases/tag/v4.39.1), and [NuGet/login v1.2.0](https://github.com/NuGet/login/releases/tag/v1.2.0). Workflow references will use the corresponding major/minor tags already established by each action, not full commit-SHA pins.

## Design and compatibility

- Apply the same test SDK and xUnit package versions to `src/WebPWrapper.Test/WebPWrapper.Test.csproj` and `src/WebPWrapper.IntegrationTest/WebPWrapper.IntegrationTest.csproj`.
- Replace the xUnit v2 package with `xunit.v3` and update test-project settings required by the v3 execution model. Keep both test projects on `net10.0`; xUnit v3's documented minimum is .NET 8.
- Preserve existing test intent. Make only API/project-file changes required for compilation and execution under xUnit v3; do not rewrite assertions or weaken coverage to accommodate the migration.
- Keep `SharpZipLib` at 1.4.2 because the NuGet version index currently lists no newer stable release.
- Update action references consistently in `.github/workflows/unit-test.yml`, `.github/workflows/publish.yml`, and `.github/workflows/codeql.yml`.
- Do not publish a package or create a release tag during this work. The NuGet publish workflow is tag-triggered and uses OIDC; its external account configuration remains unchanged.

## Verification gates

1. Restore all projects and inspect the resolved dependency graph; `Newtonsoft.Json 9.0.1` and NU1903 / GHSA-5crp-9r3c-p9vr must be absent.
2. Build the `netstandard2.1` library and both `net10.0` test projects in Release configuration.
3. Run unit and integration tests and retain meaningful existing assertions.
4. Run PR CI across Ubuntu, Windows, and macOS; verify CodeQL workflow checks pass.
5. Pack the library locally and verify test-only dependencies did not leak into the NuGet package's dependency metadata.
6. Check the workflow YAML and dependency tags. The NuGet OIDC publish step cannot be executed safely on a pull request because it is tag-triggered and would publish a package; do not trigger it as a smoke test.

## Primary risks

- xUnit v3 changes test-project execution and includes API/lifecycle changes beyond a version bump. Existing tests may need mechanical compatibility updates; CI across all supported operating systems is the acceptance gate.
- Major GitHub Action updates may have new runner/runtime requirements. The repository uses GitHub-hosted `*-latest` runners; PR CI must confirm compatibility.
- An upgraded test SDK may resolve additional transitive packages. The lock-in criterion is a clean restore without the reported vulnerable Newtonsoft version, not merely a changed direct version.
