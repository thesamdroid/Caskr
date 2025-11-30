# Test runner image for server and client suites
# Includes the .NET SDK, Node.js, and Playwright browsers.
FROM mcr.microsoft.com/playwright/dotnet:v1.47.0-jammy

WORKDIR /workspace

# Restore server dependencies
COPY Caskr.Server/*.csproj ./Caskr.Server/
COPY Caskr.Server.Tests/*.csproj ./Caskr.Server.Tests/
COPY Caskr.sln ./
RUN dotnet restore

# Restore client dependencies
COPY package.json package-lock.json ./
COPY caskr.client/package.json caskr.client/package-lock.json caskr.client/
RUN npm --prefix caskr.client ci

# Copy the full repository for builds and tests
COPY . .

CMD ["bash"]
