# BeepDM CLI Build and Test Script
# This script builds, packages, and optionally installs the BeepDM CLI tool

param(
    [Parameter(Mandatory=$false)]
    [switch]$Install = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Test = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║      BeepDM CLI - Build & Install Script      ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    
    if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
    if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
    if (Test-Path "nupkg") { Remove-Item -Recurse -Force "nupkg" }
    
    Write-Host "✓ Clean complete" -ForegroundColor Green
    Write-Host ""
}

# Restore dependencies
Write-Host "📦 Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Restore complete" -ForegroundColor Green
Write-Host ""

# Build project
Write-Host "🔨 Building CLI (Configuration: $Configuration)..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build complete" -ForegroundColor Green
Write-Host ""

# Run tests if requested
if ($Test) {
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    # Add test commands here when tests are available
    Write-Host "ℹ No tests configured yet" -ForegroundColor Cyan
    Write-Host ""
}

# Pack the tool
Write-Host "📦 Creating NuGet package..." -ForegroundColor Yellow
dotnet pack --configuration $Configuration --no-build --output ./nupkg
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Pack failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Package created" -ForegroundColor Green
Write-Host ""

# Install if requested
if ($Install) {
    Write-Host "🚀 Installing CLI tool..." -ForegroundColor Yellow
    
    # Check if tool is already installed
    $toolList = dotnet tool list --global | Select-String "thetechidea.beep.cli"
    
    if ($toolList) {
        Write-Host "  Uninstalling existing version..." -ForegroundColor Cyan
        dotnet tool uninstall --global TheTechIdea.Beep.CLI
    }
    
    Write-Host "  Installing new version..." -ForegroundColor Cyan
    dotnet tool install --global --add-source ./nupkg TheTechIdea.Beep.CLI
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Installation failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ Installation complete" -ForegroundColor Green
    Write-Host ""
    
    # Verify installation
    Write-Host "🔍 Verifying installation..." -ForegroundColor Yellow
    $version = beep --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ CLI is installed and working!" -ForegroundColor Green
        Write-Host "  Version: $version" -ForegroundColor Cyan
    } else {
        Write-Host "⚠ Installation succeeded but verification failed" -ForegroundColor Yellow
        Write-Host "  You may need to restart your terminal" -ForegroundColor Cyan
    }
    Write-Host ""
}

# Summary
Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              Build Summary                     ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration:  $Configuration" -ForegroundColor Cyan
Write-Host "Package:        $(Get-ChildItem -Path ./nupkg -Filter *.nupkg | Select-Object -First 1 -ExpandProperty Name)" -ForegroundColor Cyan
Write-Host "Installed:      $(if ($Install) { 'Yes ✓' } else { 'No (use -Install flag)' })" -ForegroundColor Cyan
Write-Host ""

if (-not $Install) {
    Write-Host "💡 To install the CLI globally, run:" -ForegroundColor Yellow
    Write-Host "   .\build-and-test.ps1 -Install" -ForegroundColor White
    Write-Host ""
}

Write-Host "📚 Next steps:" -ForegroundColor Yellow
Write-Host "   • Run 'beep --help' to see available commands" -ForegroundColor White
Write-Host "   • Read QUICKSTART.md for getting started" -ForegroundColor White
Write-Host "   • Read FEATURES.md for complete feature list" -ForegroundColor White
Write-Host ""

Write-Host "✅ All done! Happy coding! 🚀" -ForegroundColor Green

