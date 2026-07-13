# robust-db: EF Core Database Migration Workflow

Use this command when adding a new database-backed entity to RobustToolbox — new table, new `DbSet<T>`, new `IServerDbManager` methods.

## Prerequisites

- `dotnet-ef` installed globally: `dotnet tool install --global dotnet-ef`
- Build succeeds from `Content.Server.Database`
- Both SQLite and PostgreSQL migrations are ALWAYS required — they use different type systems (TEXT vs native uuid, etc.)

## Step 1 — Edit the Model

In `Content.Server.Database/Model.cs`:
- Add `DbSet<YourEntity> YourEntities { get; set; } = null!;`
- Define the entity class with `[Table("your_table")]` and properties
- Add EF Fluent API config in `OnModelCreating`: indexes, FK constraints, unique constraints

## Step 2 — Generate Migrations (BOTH contexts)

Run from the `Content.Server.Database` directory — NOT from repo root:

```powershell
cd Content.Server.Database
.\add-migration.ps1 YourMigrationName
```

This script runs `dotnet ef migrations add` for BOTH contexts automatically:
- `SqliteServerDbContext` → `Migrations/Sqlite/`
- `PostgresServerDbContext` → `Migrations/Postgres/`

**Never generate migrations manually for individual contexts** unless the script failed for one of them, in which case:
```powershell
dotnet ef migrations add --context PostgresServerDbContext -o Migrations/Postgres YourMigrationName
```

## Step 3 — Verify Both Migrations

Check that both folders have new migration files with matching logical operations (same table/indexes, different type syntax).

## Step 4 — Update Tools Constants

Search for `LATEST_DB_MIGRATION` in `Tools/dump_user_data.py` and `Tools/erase_user_data.py` and update to the new migration ID (the string inside `[Migration("...")]`).

## Step 5 — Implement DB API

- Add interface methods to `IServerDbManager` (add/get/remove/list)
- Implement async EF logic in `ServerDbBase` using `DbContext`
- Add wrappers in `ServerDbManager` forwarding through `RunDbCommand` and updating metrics counters

## Verification Checklist

- [ ] `Model.cs` updated with `DbSet<T>` and `OnModelCreating` config
- [ ] Ran `add-migration.ps1 <Name>` from `Content.Server.Database/`
- [ ] Both `Migrations/Sqlite/` and `Migrations/Postgres/` have new files
- [ ] `LATEST_DB_MIGRATION` updated in `Tools/` scripts
- [ ] `IServerDbManager` interface methods added
- [ ] `ServerDbBase` and `ServerDbManager` implementations added
- [ ] `dotnet build` passes with no pending model changes

## Troubleshooting

| Error | Fix |
|-------|-----|
| Missing PostgreSQL migration | Run: `dotnet ef migrations add --context PostgresServerDbContext -o Migrations/Postgres <Name>` from `Content.Server.Database/` |
| `PendingModelChangesWarning` | Re-run migration scripts from `Content.Server.Database/` |
| `dotnet ef` not found | `dotnet tool install --global dotnet-ef` |
| Script didn't generate both | Ensure you ran from `Content.Server.Database/`, not repo root |

## Key File Paths

- `Content.Server.Database/Model.cs` — entities, DbSet<>, OnModelCreating
- `Content.Server.Database/add-migration.ps1` — Windows migration script
- `Content.Server.Database/add-migration.sh` — Linux/macOS migration script
- `Content.Server.Database/DesignTimeContextFactories.cs` — EF tooling factories
- `Content.Server.Database/Migrations/Sqlite/` — SQLite migrations
- `Content.Server.Database/Migrations/Postgres/` — PostgreSQL migrations
- `Tools/dump_user_data.py` — update `LATEST_DB_MIGRATION`
- `Tools/erase_user_data.py` — update `LATEST_DB_MIGRATION`

## Best Practices

- Keep `Up`/`Down` migration methods symmetric
- Store `ProtoId<T>` as strings in DB — avoids cross-layer serialization
- Add unique composite indexes for `(player_user_id, proto_id)` to prevent duplicates
- Prefer async everywhere; use `GetAwaiter().GetResult()` only when environment forces sync
- SQLite uses TEXT for UUIDs, PostgreSQL uses native `uuid` — this is normal and expected
