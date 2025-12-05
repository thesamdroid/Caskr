# Test runner image for server and client suites
# Includes the .NET SDK, Node.js, and Playwright browsers.
FROM mcr.microsoft.com/playwright/dotnet:v1.47.0-jammy

# Install Node.js (required for client tests)
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs

WORKDIR /workspace

# Restore server dependencies (restore specific projects, not solution, to avoid client project dependency)
COPY Caskr.Server/*.csproj ./Caskr.Server/
COPY Caskr.Server.Tests/*.csproj ./Caskr.Server.Tests/
RUN dotnet restore Caskr.Server.Tests/Caskr.Server.Tests.csproj

# Restore client dependencies
COPY package.json package-lock.json ./
COPY caskr.client/package.json caskr.client/package-lock.json caskr.client/
RUN npm --prefix caskr.client ci

# Copy the full repository for builds and tests
COPY . .

CMD ["bash"]
