import json
import os
import ssl
import subprocess
import sys
import time
import urllib.request

# Check for --jp flag
IS_JP = "--jp" in sys.argv

PROFILE = "UmaMusumeAPI-JP" if IS_JP else "UmaMusumeAPI"
BASE_URL = "https://localhost:5001/api"
OUTPUT_DIR = "latest-data-jp" if IS_JP else "latest-data"
STARTUP_TIMEOUT = 120  # seconds to wait for API to start
PLAYABLE_CHARACTER_CARD_ID_LIMIT = 9000000
CHARACTER_REQUIRED_FIELDS = (
    "baseSpeed",
    "baseStamina",
    "basePower",
    "baseGuts",
    "baseWisdom",
    "aptitudeTurf",
    "aptitudeDirt",
    "aptitudeShort",
    "aptitudeMile",
    "aptitudeMiddle",
    "aptitudeLong",
    "aptitudeRunner",
    "aptitudeLeader",
    "aptitudeBetweener",
    "aptitudeChaser",
)

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


def is_playable_character_row(row):
    if not isinstance(row, dict):
        return False

    card_id = row.get("cardId")
    return (
        isinstance(card_id, int)
        and card_id < PLAYABLE_CHARACTER_CARD_ID_LIMIT
        and all(row.get(field) is not None for field in CHARACTER_REQUIRED_FIELDS)
    )


def filter_endpoint_data(endpoint, data):
    if endpoint != "TerumiCharacterData" or not isinstance(data, list):
        return data

    return [row for row in data if is_playable_character_row(row)]


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
            before_count = len(data) if isinstance(data, list) else None
            data = filter_endpoint_data(endpoint, data)
            out_path = os.path.join(OUTPUT_DIR, f"{endpoint}.json")
            with open(out_path, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            print(f"  -> {out_path} ({len(data)} records)")
            if before_count is not None and before_count != len(data):
                print(f"     filtered {before_count - len(data)} non-playable records")
        except Exception as e:
            print(f"  -> ERROR: {e}")

    print("\nDone.")

finally:
    print("Stopping API...")
    proc.terminate()
    proc.wait()
