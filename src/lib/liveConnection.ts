import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

import { env } from '@/src/lib/env';

let connection: HubConnection | null = null;
let starting: Promise<void> | null = null;

function build(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${env.apiBaseUrl}/hubs/live`, {
      // Wildcard CORS (`Access-Control-Allow-Origin: *`) blocks credentialed
      // browser fetches. The hub is anonymous, so opt out so the web build
      // can negotiate.
      withCredentials: false,
      transport:
        HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 20000])
    .configureLogging(LogLevel.Warning)
    .build();
}

export function getLiveConnection(): HubConnection {
  if (!connection) connection = build();
  return connection;
}

export async function ensureLiveConnected(): Promise<HubConnection> {
  const c = getLiveConnection();
  if (c.state === HubConnectionState.Connected) return c;
  if (!starting) {
    starting = c.start().finally(() => {
      starting = null;
    });
  }
  await starting;
  return c;
}
