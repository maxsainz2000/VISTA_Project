@echo off
cd /d "C:\Users\maxsa\Documents\24_7_Agent"

:: Set this to the same secret you use in GitHub!
set WEBHOOK_SECRET=my-super-secret-123

echo Starting Jules Webhook Listener...
start "Jules Webhook Listener" cmd /k "python jules_webhook_listener.py"

echo Starting Secure Tunnel...
start "Jules Tunnel" powershell.exe -ExecutionPolicy Bypass -File start_tunnel.ps1
