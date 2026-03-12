# Setup Guide

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/products/docker-desktop)
- Python 3 (only needed for `export_data.py`)
- `master.mdb` game data files:
  - Global version: place in `Global/master.mdb`
  - JP version: place in `JP/master.mdb`

---

## 1. Database Setup

The API requires a MariaDB instance populated with data from `master.mdb`.

### Global Database (port 3306)

Place the Global `master.mdb` in the `Global/` folder, then:

```bash
docker compose up -d
```

Monitor import progress:
```bash
docker logs -f umamusume-db-loader
```

### JP Database (port 3308)

Place the JP `master.mdb` in the `JP/` folder, then:

```bash
docker compose -f docker-compose-jp.yml up -d
```

Monitor import progress:
```bash
docker logs -f umamusume-db-loader-jp
```

> **Note:** Both databases can run simultaneously on different ports.

### Reset a Database

To reset with a new `master.mdb`:

```bash
# Global
docker compose down -v
docker compose up -d

# JP
docker compose -f docker-compose-jp.yml down -v
docker compose -f docker-compose-jp.yml up -d
```

> If you see `Host '172.x.x.x' is not allowed to connect to this MariaDB server`, your DB volume was initialized with old grants. Run the reset commands above so MariaDB recreates users/database from compose env vars.

---

## 2. Configure the Connection String

Open `UmaMusumeAPI/Properties/launchSettings.json` and update the `MARIA_CONNECTION_STRING` for the profile you want to use:

```json
"MARIA_CONNECTION_STRING": "user id=root;password=yourpassword;host=localhost;database=umamusume;character set=utf8mb4"
```

| Field      | Value                                    |
|------------|------------------------------------------|
| `user id`  | MariaDB username (`root` by default)     |
| `password` | Password set in `docker-compose.yml`     |
| `host`     | `localhost` or the IP of the DB machine  |
| `database` | `umamusume`                              |

If connecting to a remote database, replace `host=localhost` with the remote IP (e.g. `host=192.168.1.101`).

---

## 3. Run the API (Optional/Verification)

### EN database (default)

```bash
dotnet run --project UmaMusumeAPI --launch-profile "UmaMusumeAPI"
```

### JP database

```bash
dotnet run --project UmaMusumeAPI --launch-profile "UmaMusumeAPI-JP"
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: https://localhost:5001/index.html

---

## 4. Export Data to JSON

### Global Data

```bash
python export_data.py
```

Exports to `latest-data/` folder.

### JP Data

```bash
python export_data.py --jp
```

Exports to `latest-data-jp/` folder.

The script starts the API, fetches all Terumi endpoints, saves the results as JSON files, and shuts down automatically.

Exported files:
- `TerumiFactorData.json`
- `TerumiSimpleSkillData.json`
- `TerumiCharacterData.json`
- `TerumiSupportCardData.json`
- `TerumiRaceData.json`
- `SuccessionRelationMember.json`
- `SuccessionRelation.json`

---
