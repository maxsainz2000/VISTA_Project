@echo off
:: Check for Admin privileges
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Administrator privileges confirmed.
) else (
    echo Requesting Administrator privileges...
    powershell -Command "Start-Process '%~dpnx0' -Verb RunAs"
    exit /b
)

cd /d "C:\Users\maxsa\Documents\24_7_Agent"

echo Installing NSSM (Non-Sucking Service Manager)...
winget install NSSM.NSSM --accept-package-agreements --accept-source-agreements --silent

echo.
echo Stopping old services if they exist...
nssm stop JulesWebhook >nul 2>&1
nssm stop JulesTunnel >nul 2>&1
nssm remove JulesWebhook confirm >nul 2>&1
nssm remove JulesTunnel confirm >nul 2>&1

echo.
echo Configuring Jules Webhook Service...
nssm install JulesWebhook "python" "C:\Users\maxsa\Documents\24_7_Agent\jules_webhook_listener.py"
nssm set JulesWebhook AppDirectory "C:\Users\maxsa\Documents\24_7_Agent"
nssm set JulesWebhook AppEnvironmentExtra "WEBHOOK_SECRET=my-super-secret-123"
nssm set JulesWebhook AppStdout "C:\Users\maxsa\Documents\24_7_Agent\webhook.log"
nssm set JulesWebhook AppStderr "C:\Users\maxsa\Documents\24_7_Agent\webhook.log"

echo.
echo Configuring Jules Tunnel Service...
nssm install JulesTunnel "ngrok" "http --config=C:\Users\maxsa\AppData\Local\ngrok\ngrok.yml --domain=keg-photo-eternal.ngrok-free.dev 8080"
nssm set JulesTunnel AppDirectory "C:\Users\maxsa\Documents\24_7_Agent"
nssm set JulesTunnel AppStdout "C:\Users\maxsa\Documents\24_7_Agent\tunnel.log"
nssm set JulesTunnel AppStderr "C:\Users\maxsa\Documents\24_7_Agent\tunnel.log"

echo.
echo Starting services...
nssm start JulesWebhook
nssm start JulesTunnel

echo.
echo ===================================================
echo Done! The Jules Agent is now running as a 24/7 background Windows Service.
echo It will automatically restart if it crashes and start on boot.
echo Logs are available in webhook.log and tunnel.log.
echo ===================================================
pause
