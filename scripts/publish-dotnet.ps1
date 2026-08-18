# Framework-dependent net9.0 — sunucuda ASP.NET Core 9 Hosting Bundle gerekir
param(
    [string]$OutDir = "C:\wwwroot\shopifyApp_UpCard\publish"
)

$ErrorActionPreference = "Stop"
$proj = Join-Path $PSScriptRoot "..\src\UpCard.Web\UpCard.Web.csproj"
$webConfigSrc = Join-Path $PSScriptRoot "..\src\UpCard.Web\web.config"

if (Test-Path $OutDir) {
    Remove-Item -Recurse -Force $OutDir
}

Write-Host "Publishing framework-dependent net9.0 to $OutDir ..."
dotnet publish $proj -c Release --self-contained false -o $OutDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Copy-Item $webConfigSrc (Join-Path $OutDir "web.config") -Force

$logs = Join-Path $OutDir "logs"
$appData = Join-Path $OutDir "App_Data"
New-Item -ItemType Directory -Force -Path $logs | Out-Null
New-Item -ItemType Directory -Force -Path $appData | Out-Null
Set-Content (Join-Path $logs ".keep") ""
Set-Content (Join-Path $appData ".keep") ""

Write-Host "Done: $OutDir"
Write-Host "web.config -> dotnet .\UpCard.Web.dll (shared .NET 9)"
Get-Content (Join-Path $OutDir "UpCard.Web.runtimeconfig.json")
