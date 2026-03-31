import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5099'
const SIGNALR_URL = (import.meta.env.VITE_SIGNALR_URL as string | undefined)?.replace(/\/$/, '') ?? `${API_BASE_URL}/hubs/runs`

export function createRunsConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(SIGNALR_URL)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}
