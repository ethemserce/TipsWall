# Git Branch Workflow

This project is tracked with a task-based branch workflow.

## Branches

- `main`: stable baseline only.
- `task/<number>-<short-name>`: one branch per accepted task.

## Task Flow

1. Create a branch from `main`.
2. Implement only the scope of that task.
3. Run the task's acceptance checks.
4. Share the result for acceptance.
5. Merge only after acceptance.

## Current Task Branches

- `task/001-postgresql-schema-plan`: Task 1, PostgreSQL target schema and migration strategy.

## Sensitive Files

Real configuration files are intentionally ignored:

- `appsettings.json`
- `appsettings.*.json`
- `.env`
- `secrets.json`

Use example configuration files for tracked placeholders.
