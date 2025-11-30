# Network Proxy Limitations and Workarounds

## Why installations are blocked
The execution environment forces outbound HTTP(S) traffic through a restrictive proxy that returns `403 Forbidden` for most external package feeds (for example `archive.ubuntu.com`, `security.ubuntu.com`, npm registries, and Playwright browser downloads). Because apt, dotnet-install scripts, and Playwright fetches rely on these endpoints, installation attempts fail before any packages are downloaded.

## How to work around the restriction
- **Use a pre-baked build image** that already contains the .NET SDK and Chromium/Playwright browsers. Running tests inside that image avoids the need for outbound downloads.
- **Mirror dependencies inside the network** by hosting an internal apt feed, NuGet source, and npm/Playwright mirror that the proxy allows. Update `sources.list`, `NuGet.config`, and `.npmrc` to point at the mirror.
- **Vendor binaries** by checking the required SDK and browser archives into an internal artifact store (or as Git LFS assets) and configuring installation scripts to consume the local paths instead of reaching the public internet.
- **Leverage offline caches** by persisting `~/.nuget/packages`, `~/.cache/ms-playwright`, and npm caches between runs so restore/test steps reuse already-downloaded artifacts without contacting blocked hosts.
- **Request proxy exemptions** for the minimal set of endpoints (e.g., `dot.net`, `playwright.azureedge.net`, `registry.npmjs.org`, `archive.ubuntu.com`) if policy allows.

## Recommended workflow for this repository
1. Build inside a container image that includes the .NET SDK and Chrome/Chromium so `dotnet restore`, `dotnet test`, and `npm --prefix caskr.client test` can run without downloads.
2. If running locally behind the same proxy, configure `NUGET_PACKAGES` to point at a pre-populated package cache and set `PLAYWRIGHT_BROWSERS_PATH=0` after unpacking the Playwright browser archive into `node_modules/playwright/.local-browsers`.
3. When adding new dependencies, publish them to the internal mirrors first; then update configuration files to use those mirrors to keep CI reproducible behind the proxy.
