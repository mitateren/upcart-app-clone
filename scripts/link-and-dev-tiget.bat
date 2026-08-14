@echo off
cd /d C:\wwwroot\shopifyApp_UpCard
echo === UpCard: tiget-dev-store kurulumu ===
echo.
echo 1) Partner Dashboard'da "UpCard" adinda yeni app olustur (Create app)
echo    https://dev.shopify.com/dashboard/225511547/apps
echo 2) App Settings / Client credentials icinden Client ID'yi kopyala
echo.
set /p CLIENT_ID=Client ID yapistir ve Enter:
if "%CLIENT_ID%"=="" (
  echo Client ID gerekli.
  pause
  exit /b 1
)

call shopify app config link --client-id %CLIENT_ID% --force --file-name shopify.app.toml
if errorlevel 1 (
  echo config link basarisiz. Once: shopify auth login
  pause
  exit /b 1
)

echo.
echo Dev baslatiliyor: tiget-dev-store ...
call shopify app dev --store tiget-dev-store.myshopify.com
pause
