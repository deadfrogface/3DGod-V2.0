# Stage 5 — Download infrastructure

Decision: **CUSTOM_BUILD** thin `IComponentDownloadService` over `HttpClient`.

Evaluated: [bezzad/Downloader](https://github.com/bezzad/Downloader).

Why not WRAP Downloader:
- Extra dependency surface for SHA pinning / license gates we already own
- Resume via HTTP Range is enough for pinned component/uv packages
- Keeps Downloader types out of the app

Capabilities: async, progress, cancellation, SHA-256 verify, partial resume, honest HttpFailure/HashMismatch/InvalidUrl errors.
