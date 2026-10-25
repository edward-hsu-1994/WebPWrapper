# WebP CLI download security

`WebPExecuteDownloader.DownloadAsync` downloads the fixed libwebp 1.6.0 archive for the current supported platform, verifies its SHA-256 digest, and only then extracts and installs it under the current user's local application data directory at `WebPWrapper/libwebp/1.6.0`.

## Integrity pins

The four digests below are pinned in `WebPExecuteDownloader`. They were calculated from the corresponding libwebp 1.6.0 archives fetched over HTTPS from the official `downloads.webmproject.org` storage endpoint. The official release index does not publish SHA-256 values for these four archives; these pins therefore trust the official HTTPS source used when the pins were recorded. They are not independent publisher-signature verification. A later upstream change to any archive will fail closed until its replacement digest is independently reviewed and updated with the URL.

| Platform | Archive | SHA-256 |
| --- | --- | --- |
| Windows x64 | `libwebp-1.6.0-windows-x64.zip` | `48886f506b21f62e4661f0f4cbfca19800897c385128e8902542d29a950c93f1` |
| Linux x64 | `libwebp-1.6.0-linux-x86-64.tar.gz` | `1c5ffab71efecefa0e3c23516c3a3a1dccb45cc310ae1095c6f14ae268e38067` |
| macOS x64 | `libwebp-1.6.0-mac-x86-64.tar.gz` | `f112dd83b420ab2a4b27d46610d9827ddf4200216023281de378647ecca31c2a` |
| macOS ARM64 | `libwebp-1.6.0-mac-arm64.tar.gz` | `bc6bf84cc70f3f8574fba797d1e4a7dea4feebe9fa4be919f202413ea2b3b8f2` |

Official release index: <https://storage.googleapis.com/downloads.webmproject.org/releases/webp/index.html>.

## Limits and extraction rules

- Compressed download: at most 64 MiB.
- Decompressed TAR stream: at most 320 MiB.
- Extracted content: at most 256 MiB total, 128 MiB per entry, and 4,096 entries.
- TAR parsing uses SharpZipLib; ZIP extraction uses `ZipArchive`.
- Absolute paths, drive-qualified names, `..` path segments, symbolic and hard links, ZIP reparse-point entries, and unsupported TAR entry types are rejected.
- Entry data is streamed to disk; declared sizes are checked before copying. Executable bits from the verified TAR archive are applied with the platform `chmod` API, without invoking a shell.

## Installation and failure behavior

Downloads are serialized across processes with an exclusive lock at `.webp.install.lock` beside the versioned installation directory. Recovery and replacement also run under this lock. An existing directory is treated as current only when both selected-version executables (`cwebp` and `dwebp`) are present; incomplete installations are replaced. Each libwebp release installs into a separate version directory, so later upgrades do not delete older usable versions. The archive and extracted files are staged beside the versioned installation directory so directory moves stay on the same filesystem. The current installation is moved to a backup only after the new archive passes integrity and extraction checks. A transaction state file with a unique installer-generated identifier records the target, whether an old installation existed, and transaction order. On the next invocation, recovery restores the newest identified backup if promotion had not completed. If the target and backup both exist, recovery leaves both and their state untouched because it cannot distinguish a completed replacement from a preexisting path collision. A successful install removes its backup and state file. Backups without a matching valid transaction state are never touched. This cross-platform two-move replacement is rollback-protected, but readers can observe a brief interval between the directory moves; it is not a single atomic exchange operation.

`DownloadAsync` and `Download` accept cancellation-token overloads. Cancellation is propagated to the HTTP request, response-body reads, and installation-lock wait; it is checked after archive extraction and before promotion. Synchronous ZIP/TAR extraction cannot be interrupted mid-operation, so cancellation during extraction is observed when extraction finishes. Canceled operations throw `OperationCanceledException`. Lock acquisition retries every 100 ms for at most 30 seconds, then fails with `TimeoutException` if the exclusive lock remains unavailable. Existing overloads without a token remain available and use `CancellationToken.None`.
Existing installations under the previous `./webp` layout are not moved or deleted. Default builders continue to use a matching legacy executable when the versioned cache does not contain it; after `Download()` installs the versioned copy, builders use that copy. Applications that supply an explicit executable path continue to use it unchanged.

Temporary archives, decompressed TAR files, and incomplete staging directories are cleaned after failure where filesystem permissions permit. A failed digest, invalid archive, or extraction limit leaves the prior installation untouched. If the process stops after moving the old installation aside, the next locked invocation restores it before downloading, without requiring network access.

## Verification

The `WebPWrapper.Test` suite includes deterministic tests for TAR path traversal, entry limits, link rejection, ZIP path traversal, digest mismatch, successful TAR installation and TAR temporary-file cleanup, response-body cancellation, installation-lock cancellation and timeout, interrupted-install recovery, newest-transaction selection, preservation after failed promotion, and leaving unidentified or ambiguous backups untouched. Existing encoder/decoder tests also exercise the public downloader against the official platform archive and therefore require network access.
