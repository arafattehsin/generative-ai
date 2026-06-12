import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import type { WorkflowEvent } from '../lib/types'

const EVENT_NAMES = ['stepStarted', 'stepCompleted', 'stepFailed', 'groupChatMessageReceived', 'groupChatCompleted']

export function useRunStream(runId?: string | null) {
  const [events, setEvents] = useState<WorkflowEvent[]>([])

  useEffect(() => {
    if (!runId) {
      setEvents([])
      return
    }

    setEvents([])
    let disposed = false
    let joined = false
    const connection = new HubConnectionBuilder()
      .withUrl(`${api.baseUrl}/hubs/runs`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    EVENT_NAMES.forEach((eventName) => {
      connection.on(eventName, (event: WorkflowEvent) => {
        if (!disposed && event.runId === runId) {
          setEvents((current) => [event, ...current].slice(0, 60))
        }
      })
    })

    connection
      .start()
      .then(async () => {
        if (disposed) {
          await connection.stop()
          return
        }

        await connection.invoke('JoinRun', runId)
        joined = true
      })
      .catch(() => undefined)

    return () => {
      disposed = true
      void (async () => {
        try {
          if (joined && connection.state === HubConnectionState.Connected) {
            await connection.invoke('LeaveRun', runId)
          }
        } catch {
          // Best-effort disconnect during page reloads and run switches.
        }

        if (connection.state !== HubConnectionState.Disconnected) {
          await connection.stop().catch(() => undefined)
        }
      })()
    }
  }, [runId])

  return events
}
