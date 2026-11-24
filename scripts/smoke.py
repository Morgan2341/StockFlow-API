"""End-to-end smoke test against a running local API."""
import json
import urllib.request
import urllib.error
import uuid

BASE = "http://localhost:5080"

def request(method, path, data=None, expected=200):
    req = urllib.request.Request(BASE + path, method=method,
        data=json.dumps(data).encode() if data is not None else None,
        headers={"Content-Type": "application/json"})
    try:
        response = urllib.request.urlopen(req, timeout=10)
    except urllib.error.HTTPError as exc:
        response = exc
    with response:
        assert response.status == expected, (path, response.status, response.read())
        body = response.read()
        return json.loads(body) if body else None

assert request("GET", "/health")["status"] == "ok"
p = request("POST", "/products", {"name": "Smoke test", "sku": uuid.uuid4().hex, "reorderLevel": 5}, 201)
path = f'/products/{p["id"]}'
assert request("POST", path + "/movements", {"delta": 12})["quantity"] == 12
assert request("POST", path + "/movements", {"delta": -9})["quantity"] == 3
request("POST", path + "/movements", {"delta": -4}, 409)
assert request("GET", path)["quantity"] == 3
assert len(request("GET", path + "/movements")) == 2
assert any(x["id"] == p["id"] for x in request("GET", "/products?lowStock=true"))
print("Inventory smoke test passed.")
