import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect } from 'react'
import { apiBase } from './api'
import type { TravelRun } from './types'

export function useRunStream(runId: string | null, onRun: (run: TravelRun) => void) {
  useEffect(() => {
    if (!runId) return

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBase}/hubs/runs`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    let isDisposed = false

    connection.on('runUpdated', (run: TravelRun) => {
      if (!isDisposed) onRun(run)
    })

    async function connect() {
      await connection.start()
      await connection.invoke('JoinRun', runId)
    }

    void connect().catch(console.error)

    return () => {
      isDisposed = true
      void connection.invoke('LeaveRun', runId).catch(() => undefined)
      void connection.stop().catch(() => undefined)
    }
  }, [onRun, runId])
}
