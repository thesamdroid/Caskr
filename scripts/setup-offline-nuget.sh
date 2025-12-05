#!/bin/bash
set -e

# Setup offline NuGet packages for environments without direct NuGet access
# This script downloads packages via curl and configures a local NuGet source

PACKAGES_DIR="${HOME}/.nuget-offline"
mkdir -p "$PACKAGES_DIR"

echo "=== Setting up offline NuGet packages ==="

# Function to download a package
download_package() {
    local package_id="$1"
    local version="$2"
    local package_lower=$(echo "$package_id" | tr '[:upper:]' '[:lower:]')
    local filename="${package_lower}.${version}.nupkg"
    local url="https://api.nuget.org/v3-flatcontainer/${package_lower}/${version}/${filename}"

    if [ -f "$PACKAGES_DIR/$filename" ]; then
        echo "  [SKIP] $package_id $version (already exists)"
        return 0
    fi

    echo "  [DOWNLOAD] $package_id $version"
    if curl -sL "$url" -o "$PACKAGES_DIR/$filename"; then
        return 0
    else
        echo "  [FAILED] $package_id $version"
        return 1
    fi
}

# Core packages from Caskr.Server.csproj
echo ""
echo "Downloading core packages..."
download_package "Microsoft.AspNetCore.SpaProxy" "8.0.0"
download_package "Microsoft.EntityFrameworkCore.Design" "9.0.1"
download_package "Microsoft.EntityFrameworkCore.Tools" "9.0.1"
download_package "Microsoft.VisualStudio.Web.CodeGeneration.Design" "8.0.7"
download_package "Npgsql.EntityFrameworkCore.PostgreSQL" "9.0.3"
download_package "SendGrid" "9.29.3"
download_package "Swashbuckle.AspNetCore" "7.2.0"
download_package "Microsoft.AspNetCore.Authentication.JwtBearer" "8.0.0"
download_package "itext7" "8.0.5"
download_package "itext7.bouncy-castle-adapter" "8.0.5"
download_package "System.Text.Json" "9.0.1"
download_package "IppDotNetSdkForQuickBooksApiV3" "14.7.0.3"
download_package "MediatR" "11.1.0"
download_package "MediatR.Extensions.Microsoft.DependencyInjection" "11.1.0"
download_package "BCrypt.Net-Next" "4.0.3"

# Serilog packages
echo ""
echo "Downloading Serilog packages..."
download_package "Serilog.AspNetCore" "8.0.3"
download_package "Serilog.Enrichers.Environment" "3.0.1"
download_package "Serilog.Enrichers.Thread" "4.0.0"
download_package "Serilog.Enrichers.Process" "3.0.0"
download_package "Serilog.Enrichers.CorrelationId" "3.0.1"
download_package "Serilog.Exceptions" "8.4.0"
download_package "Serilog.Sinks.Seq" "8.0.0"
download_package "Serilog.Sinks.Console" "6.0.0"
download_package "Serilog.Sinks.File" "6.0.0"
download_package "Serilog.Formatting.Compact" "3.0.0"

# Test packages from Caskr.Server.Tests.csproj
echo ""
echo "Downloading test packages..."
download_package "Microsoft.NET.Test.Sdk" "17.6.0"
download_package "Microsoft.Extensions.DependencyInjection" "9.0.1"
download_package "xunit" "2.4.2"
download_package "Moq" "4.20.72"
download_package "Microsoft.EntityFrameworkCore.InMemory" "9.0.1"
download_package "xunit.runner.visualstudio" "2.4.5"
download_package "coverlet.collector" "6.0.0"

# Configure NuGet to use the local source
echo ""
echo "Configuring local NuGet source..."

mkdir -p ~/.nuget/NuGet
cat > ~/.nuget/NuGet/NuGet.Config << EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-offline" value="$PACKAGES_DIR" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

echo ""
echo "=== Setup complete ==="
echo "Local packages: $PACKAGES_DIR"
echo "NuGet config: ~/.nuget/NuGet/NuGet.Config"
echo ""
echo "Run 'dotnet restore' to use the local packages."
