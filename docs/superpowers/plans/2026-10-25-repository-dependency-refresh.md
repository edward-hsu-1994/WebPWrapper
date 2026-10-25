# Repository Dependency Refresh Implementation Plan

> **For agentic workers:** Execute the single task below in the existing isolated worktree. Preserve the approved spec as the source of truth. Do not publish a package or push a release tag.

**Goal:** Upgrade every direct NuGet dependency and repository GitHub Action to the approved stable versions, migrate test projects to xUnit v3, and eliminate the Newtonsoft.Json 9.0.1 NU1903 warning.

**Architecture:** Keep the library on `netstandard2.1` and test projects on `net10.0`. Apply package updates to the existing project files and action updates to the existing workflows; make only the test-project changes required by xUnit v3. No library API changes.

**Tech Stack:** .NET 10, NuGet, xUnit v3, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-10-25-repository-dependency-refresh-design.md`

**Status:** Completed through PR #24 (merge commit `b76752c`).

## Global Constraints

- Keep the library target `netstandard2.1` and test targets `net10.0`.
- Use `Microsoft.NET.Test.Sdk` 18.11.0, `xunit.v3` 4.0.2, and `xunit.runner.visualstudio` 4.0.1 in both test projects.
- Keep `SharpZipLib` at 1.4.2; NuGet currently lists no newer stable release.
- Update actions to checkout v7, setup-dotnet v6, CodeQL v4, and NuGet/login v1.2.0.
- Preserve the existing semantic assertions and test coverage; do not skip, weaken, or delete tests to resolve migration failures.
- Do not change the library's public API, publish a package, create a release tag, or alter the NuGet OIDC configuration.
- The resolved graph must not contain Newtonsoft.Json 9.0.1 and restore must not report NU1903 / GHSA-5crp-9r3c-p9vr.

---

### Task 1: Upgrade NuGet packages, xUnit test projects, and workflow actions

**Files:**
- Modify: `src/WebPWrapper.Test/WebPWrapper.Test.csproj`
- Modify: `src/WebPWrapper.IntegrationTest/WebPWrapper.IntegrationTest.csproj`
- Modify only if required by xUnit v3 compiler/runtime diagnostics: C# test files under `src/WebPWrapper.Test/`
- Modify: `.github/workflows/unit-test.yml`
- Modify: `.github/workflows/publish.yml`
- Modify: `.github/workflows/codeql.yml`
- Do not modify: `src/WebPWrapper/WebPWrapper.csproj`; SharpZipLib is already at its newest listed stable version.

- [x] **Step 1: Upgrade both test project manifests**

  In both `.csproj` files, remove the `xunit` v2 reference and use `xunit.v3` 4.0.2. Set `Microsoft.NET.Test.Sdk` to 18.11.0 and `xunit.runner.visualstudio` to 4.0.1. Set `<OutputType>Exe</OutputType>` and `<IsTestProject>true</IsTestProject>` in each test project's `PropertyGroup`; retain `TargetFramework` `net10.0` and all existing project references, compile includes/removes, and sample content.

- [x] **Step 2: Restore and inspect the resolved graph**

  Run:

  ```sh
  dotnet restore src/WebPWrapper.Test/WebPWrapper.Test.csproj
  dotnet restore src/WebPWrapper.IntegrationTest/WebPWrapper.IntegrationTest.csproj
  dotnet list src/WebPWrapper.Test/WebPWrapper.Test.csproj package --include-transitive
  dotnet list src/WebPWrapper.IntegrationTest/WebPWrapper.IntegrationTest.csproj package --include-transitive
  ```

  Confirm neither resolved graph contains `Newtonsoft.Json 9.0.1` and neither restore reports NU1903 for GHSA-5crp-9r3c-p9vr. Resolve any restore/compatibility errors rather than suppressing NuGet audit warnings.

- [x] **Step 3: Build both test projects and adapt only real xUnit v3 incompatibilities**

  Run:

  ```sh
  dotnet build src/WebPWrapper.Test/WebPWrapper.Test.csproj --configuration Release --no-restore
  dotnet build src/WebPWrapper.IntegrationTest/WebPWrapper.IntegrationTest.csproj --configuration Release --no-restore
  ```

  If compilation exposes xUnit v3 incompatibilities, consult the official migration guide at `https://xunit.net/docs/getting-started/v3/migration` and make the smallest source changes that preserve the original test behavior. Keep the existing linked integration-test files and collection fixture semantics intact.

- [x] **Step 4: Upgrade workflow action references**

  Change every existing occurrence in the three workflow files to `actions/checkout@v7`, `actions/setup-dotnet@v6`, `github/codeql-action/init@v4`, `github/codeql-action/analyze@v4`, and `NuGet/login@v1.2.0`. Preserve permissions, triggers, .NET SDK `10.0.x`, matrix operating systems, and publish ordering/OIDC inputs.

- [x] **Step 5: Run project behavior and package verification**

  Run:

  ```sh
  dotnet build src/WebPWrapper/WebPWrapper.csproj --configuration Release
  dotnet test src/WebPWrapper.Test/WebPWrapper.Test.csproj --configuration Release
  dotnet test src/WebPWrapper.IntegrationTest/WebPWrapper.IntegrationTest.csproj --configuration Release
  dotnet pack src/WebPWrapper/WebPWrapper.csproj --configuration Release --output /tmp/webpwrapper-dependency-refresh-package
  git diff --check
  ```

  Confirm the library remains buildable for `netstandard2.1`, all unit and integration tests execute and pass, and the packed library nuspec lists only the library's intended runtime dependency (`SharpZipLib` 1.4.2), not test-only packages. Remove any temporary verification output from the worktree.

- [x] **Step 6: Verify CI and report**

  Open the dependency-refresh PR only after local restore/build/test/pack checks pass. Confirm the PR's Ubuntu, Windows, and macOS unit/integration matrix and CodeQL checks pass. Do not create a version tag: the publish workflow is tag-triggered and would publish to NuGet.org. Report that the OIDC publishing action was updated but the tag-triggered publishing path was not executed.

**Acceptance:** Both test projects use the approved packages and xUnit v3 model; NU1903 for Newtonsoft.Json 9.0.1 is absent from restore output and resolved graphs; library and test builds and tests pass; package metadata does not include test-only dependencies; updated PR CI succeeds across the three OSes and CodeQL; no package is published.
