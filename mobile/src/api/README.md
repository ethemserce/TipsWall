# Mobile API client

Two halves live here:

1. **Hand-written wrappers** (`signals.ts`, `fixtures.ts`, etc.) — the
   thin functions screens import. They typed against:
2. **Generated types** (`types.gen.ts`) — built from the backend's
   Swagger document so request / response shapes can never drift.

## Why codegen

Before this turn, `src/types/*.ts` was a manual mirror of the backend
DTOs. Every C# property change had to be remembered on both sides; we
had real bugs from drift (an outcome label rename that didn't make it
across, a nullable that was actually optional in C# but required in
TS, etc.). The generated types eliminate that whole class of bug.

Hand-written wrappers stay because:
- The backend wraps responses in `ApiResponse<T>` envelopes, and we want
  the wrappers to do the unwrap once.
- Some endpoints take camelCase params we map to snake_case query
  strings; codegen would surface the wire shape directly.

## Regenerating

```bash
# 1. Make sure the backend is running with Swagger enabled (Development).
#    By default that's http://localhost:28333/swagger/v3/swagger.json
curl http://localhost:28333/swagger/v3/swagger.json > src/api/openapi.json

# 2. Regenerate the TypeScript types.
npm run api:types
```

The generated file is committed so a fresh `npm install` doesn't need
the API to be running. Re-run after every backend DTO change and
include the regenerated `types.gen.ts` in the same PR as the C#
change.

## Style guard

`types.gen.ts` is auto-generated — do not edit it. If a wrapper needs
a slightly different shape (e.g. flattening a paginated envelope),
build that in the wrapper file and keep `types.gen.ts` raw.
