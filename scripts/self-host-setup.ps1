# UpCard self-host setup (Windows)
# Usage: .\scripts\self-host-setup.ps1

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

Write-Host "==> npm ci" -ForegroundColor Cyan
npm ci

Write-Host "==> prisma generate + migrate" -ForegroundColor Cyan
if (-not $env:DATABASE_URL) {
  $env:DATABASE_URL = "file:./prisma/prod.sqlite"
}
npx prisma generate
npx prisma migrate deploy

Write-Host "==> build" -ForegroundColor Cyan
npm run build

New-Item -ItemType Directory -Force -Path ".\logs" | Out-Null

Write-Host ""
Write-Host "Build tamam. Sonraki adimlar:" -ForegroundColor Green
Write-Host "1) .env dosyasini doldur (SHOPIFY_API_SECRET, SHOPIFY_APP_URL)"
Write-Host "2) IIS siteyi bu klasore bagla (web.config) VEYA: npm run start"
Write-Host "3) shopify.app.toml URL'lerini domainine cevir -> shopify app deploy"
Write-Host "Detay: DEPLOY.md"
