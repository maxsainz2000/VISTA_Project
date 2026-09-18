# Starts ngrok to expose local port 8080
# Make sure ngrok is installed and authenticated

Write-Host "Starting secure tunnel on port 8080..." -ForegroundColor Cyan
ngrok http --domain=keg-photo-eternal.ngrok-free.dev 8080
