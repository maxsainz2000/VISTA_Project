import subprocess
import uvicorn
import hmac
import hashlib
import os
from fastapi import FastAPI, Request, HTTPException
from pydantic import BaseModel

app = FastAPI()

class WebhookPayload(BaseModel):
    pr_number: str
    pr_url: str

@app.post("/webhook")
async def handle_webhook(request: Request, payload: WebhookPayload):
    # Verify HMAC signature
    signature = request.headers.get("X-Hub-Signature-256")
    if not signature:
        raise HTTPException(status_code=400, detail="Missing signature")
    
    secret = os.getenv("WEBHOOK_SECRET", "").encode("utf-8")
    body = await request.body()
    expected_mac = hmac.new(secret, body, hashlib.sha256).hexdigest()
    expected_signature = f"sha256={expected_mac}"
    
    if not hmac.compare_digest(expected_signature, signature):
        raise HTTPException(status_code=403, detail="Invalid signature")

    print(f"Received webhook for PR #{payload.pr_number} - {payload.pr_url}")
    print("Spawning Antigravity Agent via agy CLI...")
    
    # Use static prompt and pass data via safe arguments/env variables to prevent injection
    prompt = (
        "Jules has opened a new PR. "
        "The PR number is available in the environment variable $JULES_PR_NUMBER and the URL in $JULES_PR_URL. "
        "Please wake up, checkout the PR, review the Jules output against the Specs/, and log any issues or "
        "deferred items in Deferred_Tasks.md before auto-launching the next task. "
        "Pause for my manual verification when done."
    )
    
    # Run the Antigravity CLI as a subprocess safely
    try:
        env = os.environ.copy()
        env["JULES_PR_NUMBER"] = str(payload.pr_number)
        env["JULES_PR_URL"] = str(payload.pr_url)
        subprocess.Popen(
            ["agy", "-i", prompt],
            env=env,
            cwd="VISTA_Project",
            creationflags=subprocess.CREATE_NEW_CONSOLE
        )
    except Exception as e:
        print(f"Error launching agy: {e}")
        return {"status": "error", "message": str(e)}

    return {"status": "success", "message": "Agent spawned"}

if __name__ == "__main__":
    print("Starting 24/7 Agent Webhook Listener on port 8080...")
    uvicorn.run(app, host="0.0.0.0", port=8080)
