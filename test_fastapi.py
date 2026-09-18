import asyncio
from fastapi import FastAPI, Request
from pydantic import BaseModel
from fastapi.testclient import TestClient

app = FastAPI()
class Payload(BaseModel):
    x: str

@app.post("/")
async def test_endpoint(request: Request, payload: Payload):
    body = await request.body()
    return {"body_len": len(body), "payload": payload.x}

client = TestClient(app)
response = client.post("/", json={"x": "hello"})
print(response.json())
