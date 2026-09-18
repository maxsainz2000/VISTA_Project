import subprocess
import uvicorn
from fastapi import FastAPI, Request
from pydantic import BaseModel

app = FastAPI()

class WebhookPayload(BaseModel):
    pr_number: str
    pr_url: str

@app.post("/webhook")
async def handle_webhook(payload: WebhookPayload):
    print(f"Received webhook for PR #{payload.pr_number} - {payload.pr_url}")
    print("Spawning Antigravity Agent via agy CLI...")
    
    prompt = (
        f"Jules has opened PR #{payload.pr_number} ({payload.pr_url}). "
        "Please wake up, review the Jules output against the Specs/, and log any issues or "
        "deferred items in Deferred_Tasks.md before auto-launching the next task. "
        "Pause for my manual verification when done."
    )
    
    # Run the Antigravity CLI as a subprocess
    # Note: agy must be in the system PATH. We use --prompt-interactive (-i) so it
    # remains open and interactive for manual verification.
    try:
        subprocess.Popen(
            ["agy", "--cwd", "VISTA_Project", "-i", prompt],
            creationflags=subprocess.CREATE_NEW_CONSOLE  # Opens in a new window on Windows
        )
    except Exception as e:
        print(f"Error launching agy: {e}")
        return {"status": "error", "message": str(e)}

    return {"status": "success", "message": "Agent spawned"}

if __name__ == "__main__":
    print("Starting 24/7 Agent Webhook Listener on port 8080...")
    uvicorn.run(app, host="0.0.0.0", port=8080)
