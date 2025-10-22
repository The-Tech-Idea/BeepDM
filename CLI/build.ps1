# Build and test BeepDM CLI

Write-Host "Building BeepDM CLI..." -ForegroundColor Cyan

# Build the CLI project
dotnet build BeepCLI.csproj -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    
    # Test basic commands
    Write-Host "`nTesting CLI commands..." -ForegroundColor Cyan
    
    dotnet run -- --version
    dotnet run -- --help
    dotnet run -- profile list
    dotnet run -- config show
    
    Write-Host "`nTo install as a global tool:" -ForegroundColor Yellow
    Write-Host "  dotnet pack" -ForegroundColor White
    Write-Host "  dotnet tool install --global --add-source ./nupkg TheTechIdea.Beep.CLI" -ForegroundColor White
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
