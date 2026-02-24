# Setup Guide

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/products/docker-desktop)
- Python 3 (only needed for `export_data.py`)
- `master.mdb` game data file placed in the project root

---

## 1. Database Setup

The API requires a MariaDB instance populated with data from `master.mdb`. (placed in the root directory or same location as `docker-compose.yml`)

### Start the database

```bash
docker compose up -d
```

This will:
1. Start a MariaDB container on port `3306`
2. Run the SQL scripts to create all tables, views, functions, and stored procedures
3. Import all data from `master.mdb` into the database via the loader service

The loader service exits automatically when the import is complete. Monitor its progress with:

```bash
docker logs -f umamusume-db-loader
```

> **Note:** The init scripts and data import only run on first startup. To reset the database with a new `master.mdb`, run:
> ```bash
> docker compose down -v
> docker compose up -d
> ```

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

With the API running, or automatically via the script:

```bash
python export_data.py
```

This starts the API, fetches all Terumi endpoints, and saves the results as JSON files to the `latest-data/` folder. The API is shut down automatically when the export is complete.

Exported files:
- `latest-data/TerumiFactorData.json`
- `latest-data/TerumiSimpleSkillData.json`
- `latest-data/TerumiCharacterData.json`
- `latest-data/TerumiSupportCardData.json`
- `latest-data/TerumiRaceData.json`
- `latest-data/SuccessionRelationMember.json`
- `latest-data/SuccessionRelation.json`

---
