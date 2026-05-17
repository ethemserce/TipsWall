import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useEffect, useState } from 'react';

import { env } from '@/src/lib/env';

export type LiveConnectionStatus = 'idle' | 'connecting' | 'connected' | 'disconnected';

let connection: HubConnection | null = null;
let starting: Promise<void> | null = null;
let status: LiveConnectionStatus = 'idle';
const statusListeners = new Set<(s: LiveConnectionStatus) => void>();

function setStatus(next: LiveConnectionStatus): void {
  if (status === next) return;
  status = next;
  for (const l of statusListeners) l(next);
}

function build(): HubConnection {
  const c = new HubConnectionBuilder()
    .withUrl(`${env.apiBaseUrl}/hubs/live`, {
      // Wildcard CORS (`Access-Control-Allow-Origin: *`) blocks credentialed
      // browser fetches. The hub is anonymous, so opt out so the web build
      // can negotiate.
      withCredentials: false,
      transport:
        HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    })
    // Unbounded retry policy. The default 6-step ramp (0,1,2,5,10,20s)
    // gives up after ~38 seconds — easy to exhaust if the user lets the
    // phone sleep for a few minutes or rides a flaky 4G link, and once
    // exhausted the connection sits at "disconnected" forever with no
    // way back without an app restart. We cap each attempt at 30s so
    // the loop stays cheap, and the AppState bridge in useAppFocusBridge
    // still calls ensureLiveConnected on foreground as a belt-and-braces
    // kicker.
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (ctx) => {
        const i = ctx.previousRetryCount;
        if (i === 0) return 0;
        if (i < 6) return Math.min(30_000, 1000 * 2 ** (i - 1));
        return 30_000;
      },
    })
    .configureLogging(LogLevel.Warning)
    .build();

  // Reflect SignalR's internal state machine into our public status enum so
  // the UI can render "bağlantı yok / yeniden bağlanılıyor" banners.
  c.onreconnecting(() => setStatus('connecting'));
  c.onreconnected(() => setStatus('connected'));
  c.onclose(() => setStatus('disconnected'));
  return c;
}

export function getLiveConnection(): HubConnection {
  if (!connection) connection = build();
  return connection;
}

export function getLiveStatus(): LiveConnectionStatus {
  return status;
}

export function subscribeLiveStatus(
  listener: (s: LiveConnectionStatus) => void,
): () => void {
  statusListeners.add(listener);
  return () => {
    statusListeners.delete(listener);
  };
}

export function useLiveStatus(): LiveConnectionStatus {
  const [snapshot, setSnapshot] = useState<LiveConnectionStatus>(status);
  useEffect(() => subscribeLiveStatus(setSnapshot), []);
  return snapshot;
}

export async function ensureLiveConnected(): Promise<HubConnection> {
  const c = getLiveConnection();
  if (c.state === HubConnectionState.Connected) {
    setStatus('connected');
    return c;
  }
  if (!starting) {
    setStatus('connecting');
    starting = c
      .start()
      .then(() => {
        setStatus('connected');
      })
      .catch((err) => {
        setStatus('disconnected');
        throw err;
      })
      .finally(() => {
        starting = null;
      });
  }
  await starting;
  return c;
}
