# Publishing to NuGet.org

The [`publish.yml`](../.github/workflows/publish.yml) workflow builds and tests the repository, packs `WebPWrapper`, and publishes the package when a version tag matching `vMAJOR.MINOR.PATCH` is pushed (for example, `v1.2.3`). Publishing uses NuGet.org Trusted Publishing through GitHub Actions OIDC; it does not require a long-lived NuGet API key.

## One-time NuGet.org setup

1. Sign in to the NuGet.org account or organization that owns the `WebPWrapper` package and open **Trusted Publishing**.
2. Add a trusted publishing policy for this GitHub workflow. Use these repository values:

   | Policy field | Value |
   | --- | --- |
   | Repository owner | `edward-hsu-1994` |
   | Repository | `WebPWrapper` |
   | Workflow file | `publish.yml` |
   | Environment | Leave empty; the workflow does not declare a GitHub Actions environment. |

   Enter only the workflow filename, not `.github/workflows/publish.yml`. Select the NuGet account or organization that owns the package as the policy owner, and limit the policy scope to the package and publishing actions required for this release workflow.

3. In the GitHub repository, add the Actions repository secret `NUGET_USER` with the NuGet.org account's **profile username** (not its email address): **Settings → Secrets and variables → Actions → New repository secret**.

The workflow already requests `id-token: write` for its publishing job and passes `NUGET_USER` to `NuGet/login@v1`. That action exchanges the GitHub OIDC identity for a short-lived NuGet API key. The workflow supplies the generated key to `dotnet nuget push`; do not add a persistent `NUGET_API_KEY` secret.

## Publishing a release

After the trusted publisher policy and `NUGET_USER` secret are configured, push a version tag from the commit intended for release:

```shell
git tag v1.2.3
git push origin v1.2.3
```

The workflow runs its test job before packaging and publishing. A tag push is a real NuGet.org publication; confirm the package version and release commit before pushing the tag.

## References

- [NuGet.org Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [GitHub Actions OIDC security hardening](https://docs.github.com/en/actions/security-for-github-actions/security-hardening-your-deployments/configuring-openid-connect-in-cloud-providers)
