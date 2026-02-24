param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

Write-Host "Using dotnet: $(dotnet --version)"

Write-Host 'Restoring solution...'
dotnet restore "$PSScriptRoot/../../Ever.Client.sln"

Write-Host "Building solution in $Configuration mode..."
dotnet build "$PSScriptRoot/../../Ever.Client.sln" -c $Configuration --no-restore

Write-Host 'Build completed.'
