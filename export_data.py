import json
import os
import ssl
import subprocess
import sys
import time
import urllib.request

PROFILE = "UmaMusumeAPI"
BASE_URL = "https://localhost:5001/api"
OUTPUT_DIR = "latest-data"
STARTUP_TIMEOUT = 120  # seconds to wait for API to start

ENDPOINTS = [
    "TerumiFactorData",
    "TerumiSimpleSkillData",
    "TerumiCharacterData",
    "TerumiSupportCardData",
    "TerumiRaceData",
    "SuccessionRelationMember",
    "SuccessionRelation",
]

# bypass SSL verification for localhost dev certificate
ssl_ctx = ssl.create_default_context()
ssl_ctx.check_hostname = False
ssl_ctx.verify_mode = ssl.CERT_NONE


def wait_for_api(timeout):
    url = f"{BASE_URL}/TerumiCharacterData"
    print(f"Waiting for API to be ready at {BASE_URL}...")
    deadline = time.time() + timeout
    while time.time() < deadline:
        try:
            urllib.request.urlopen(url, context=ssl_ctx, timeout=3)
            print("API is ready.")
            return True
        except Exception:
            time.sleep(2)
    return False


script_dir = os.path.dirname(os.path.abspath(__file__))
print(f"Starting: dotnet run --project UmaMusumeAPI --launch-profile \"{PROFILE}\"")
proc = subprocess.Popen(
    ["dotnet", "run", "--project", "UmaMusumeAPI", "--launch-profile", PROFILE],
    cwd=script_dir,
)

try:
    if not wait_for_api(STARTUP_TIMEOUT):
        print("ERROR: API did not start in time.")
        proc.terminate()
        sys.exit(1)

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    for endpoint in ENDPOINTS:
        url = f"{BASE_URL}/{endpoint}"
        print(f"Fetching {url}...")
        try:
            with urllib.request.urlopen(url, context=ssl_ctx) as response:
                data = json.loads(response.read().decode("utf-8"))
            out_path = os.path.join(OUTPUT_DIR, f"{endpoint}.json")
            with open(out_path, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            print(f"  -> {out_path} ({len(data)} records)")
        except Exception as e:
            print(f"  -> ERROR: {e}")

    print("\nDone.")

finally:
    print("Stopping API...")
    proc.terminate()
    proc.wait()
