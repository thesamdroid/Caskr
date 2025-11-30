# Test Runner Image

This repository includes a `build/test-runner/Dockerfile` that packages the full test toolchain (.NET SDK, Node.js, and Playwright Chromium) so server and client suites can run without additional host installs.

## Building the image

From the repository root:

```bash
docker build -t caskr-test-runner -f build/test-runner/Dockerfile .
```

If your network routes through an HTTP(S) proxy, pass the proxy settings as build arguments so the base image and dependency restores can complete:

```bash
docker build -t caskr-test-runner \
  --build-arg HTTP_PROXY=http://<proxy>:<port> \
  --build-arg HTTPS_PROXY=http://<proxy>:<port> \
  --build-arg NO_PROXY=localhost,127.0.0.1 \
  -f build/test-runner/Dockerfile .
```

## Running tests inside the container

Start an interactive shell that mounts your working copy:

```bash
docker run --rm -it -v "$PWD:/workspace" caskr-test-runner
```

Inside the container, run the full suite:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
npm --prefix caskr.client test
```

## Troubleshooting

* If dependency restores fail due to restricted feeds, point the container at internal NuGet/npm mirrors or place cached packages in mounted volume paths (`~/.nuget/packages` and `caskr.client/node_modules`).
* The Playwright base image ships with Chromium; if browser launches fail, set `PLAYWRIGHT_BROWSERS_PATH=/ms-playwright` to ensure the preinstalled binaries are discovered.
* When running on hosts that disallow Docker pulls, build the image in an allowed environment and push it to an internal registry that the proxy permits.
