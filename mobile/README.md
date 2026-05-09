# PreOddsMobile

React Native (Expo) mobile client for the PreOddsApi V3 backend.

The first milestone is a Sofascore-style "today's matches" screen wired to
`/api/v3/fixtures`. The plan is to grow this app into the public face of the
backend — fixtures, odds, leagues, analytics — one screen at a time.

## Stack

- Expo SDK 54 + React Native 0.81 + TypeScript
- expo-router (file-based routing, bottom tabs)
- @tanstack/react-query for data fetching/caching
- axios for HTTP
- date-fns for date math/formatting

## Getting started

```bash
npm install
cp .env.example .env       # then edit it
npm run start              # opens Metro; press a/i/w for Android/iOS/web
```

### API base URL

The app reads `EXPO_PUBLIC_API_BASE_URL` from `.env`. Set it to wherever the
.NET backend (`PreOddsApi.WebApi`) is reachable from the device running the
app:

| Where the app runs        | Value                          |
| ------------------------- | ------------------------------ |
| Android emulator          | `http://10.0.2.2:28333`        |
| iOS simulator             | `http://localhost:28333`       |
| Physical device on Wi-Fi  | `http://<your-LAN-IP>:28333`   |

The path `/api/v3` is appended automatically by the API client. After changing
`.env` you need to restart the Metro bundler.

## Project layout

```
app/                 expo-router routes (tab screens, modals)
src/
  api/               axios client + per-resource fetchers
  components/        reusable UI (FixtureCard, DateBar, …)
  hooks/             TanStack Query hooks
  lib/               env, queryClient
  types/             V3 DTO mirrors (FixtureSummary, ApiResponse, …)
```

## Backend

The companion repo lives at `D:\Projects\PreOddsApi`. Run the API with:

```bash
dotnet run --project D:\Projects\PreOddsApi\PreOddsApi.WebApi
```

It listens on `http://localhost:28333`.
