import sqlite3
import pymysql
import os
import sys
import time


def sqlite_type_to_mysql(sqlite_type: str) -> str:
    t = sqlite_type.upper().strip()
    if "INT" in t:
        return "BIGINT"
    elif any(x in t for x in ("CHAR", "TEXT", "CLOB", "VARCHAR")):
        return "LONGTEXT"
    elif any(x in t for x in ("REAL", "FLOA", "DOUB")):
        return "DOUBLE"
    elif "BLOB" in t or t == "":
        return "LONGBLOB"
    elif any(x in t for x in ("NUMER", "DECI")):
        return "DECIMAL(20,10)"
    else:
        return "LONGTEXT"


db_host = os.environ.get("DB_HOST", "localhost")
db_user = os.environ.get("DB_USER", "root")
db_password = os.environ.get("DB_PASSWORD", "")
db_name = os.environ.get("DB_NAME", "umamusume")
mdb_path = os.environ.get("MDB_PATH", "/data/master.mdb")

print(f"Connecting to MariaDB at {db_host}...")
mysql_conn = None
for i in range(30):
    try:
        mysql_conn = pymysql.connect(
            host=db_host,
            user=db_user,
            password=db_password,
            database=db_name,
            charset="utf8mb4",
        )
        print("Connected.")
        break
    except Exception as e:
        print(f"  Waiting... ({i + 1}/30): {e}")
        time.sleep(3)

if mysql_conn is None:
    print("ERROR: Could not connect to MariaDB after 30 attempts.")
    sys.exit(1)

print(f"Opening {mdb_path}...")
sqlite_conn = sqlite3.connect(mdb_path)
sqlite_cursor = sqlite_conn.cursor()
mysql_cursor = mysql_conn.cursor()

mysql_cursor.execute("SET FOREIGN_KEY_CHECKS = 0")

sqlite_cursor.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
tables = [row[0] for row in sqlite_cursor.fetchall()]
print(f"Found {len(tables)} tables in master.mdb\n")

total_rows = 0
for table in tables:
    sqlite_cursor.execute(f"PRAGMA table_info(`{table}`)")
    columns = sqlite_cursor.fetchall()
    # columns: (cid, name, type, notnull, dflt_value, pk)

    col_defs = []
    pk_cols = [col[1] for col in columns if col[5]]
    for col in columns:
        name = col[1]
        mysql_type = sqlite_type_to_mysql(col[2])
        col_defs.append(f"  `{name}` {mysql_type}")
    if pk_cols:
        pk_list = ", ".join([f"`{c}`" for c in pk_cols])
        col_defs.append(f"  PRIMARY KEY ({pk_list})")

    create_sql = (
        f"CREATE TABLE IF NOT EXISTS `{table}` (\n"
        + ",\n".join(col_defs)
        + "\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci"
    )

    try:
        mysql_cursor.execute(create_sql)
        mysql_conn.commit()
    except Exception as e:
        print(f"  ERR creating {table}: {e}")
        continue

    col_names = [col[1] for col in columns]
    col_list = ", ".join([f"`{c}`" for c in col_names])
    placeholders = ", ".join(["%s"] * len(col_names))

    sqlite_cursor.execute(f"SELECT {col_list} FROM `{table}`")
    rows = sqlite_cursor.fetchall()

    if not rows:
        continue

    try:
        batch_size = 1000
        inserted = 0
        for i in range(0, len(rows), batch_size):
            batch = rows[i : i + batch_size]
            mysql_cursor.executemany(
                f"INSERT IGNORE INTO `{table}` ({col_list}) VALUES ({placeholders})",
                batch,
            )
            mysql_conn.commit()
            inserted += len(batch)
        print(f"  OK  {table}: {inserted} rows")
        total_rows += inserted
    except Exception as e:
        mysql_conn.rollback()
        print(f"  ERR {table}: {e}")

mysql_cursor.execute("SET FOREIGN_KEY_CHECKS = 1")
mysql_conn.commit()

sqlite_conn.close()
mysql_conn.close()
print(f"\nDone. {total_rows} total rows imported.")
