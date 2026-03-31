import { useEffect, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { TimelineEvent, WorkflowEventPayload } from '../lib/types'
import { createRunsConnection } from '../lib/signalr'

const eventLabels: Record<string, (payload: WorkflowEventPayload) => string> = {
  StepStarted: (payload) => `${payload.StepName} started`,
  StepCompleted: (payload) => `${payload.StepName} completed`,
  StepFailed: (payload) => `${payload.StepName} failed`,
  StepStatus: (payload) => payload.StatusMessage ?? 'Status update',
  ConcurrentGroupStarted: (payload) => `Concurrent review started: ${(payload.ReviewerNames ?? []).join(', ')}`,
  BarrierReleased: () => 'All concurrent reviewers finished',
  RunCompleted: (payload) => (payload.Success ? 'Run completed' : `Run failed: ${payload.Error ?? 'Unknown error'}`),
}

export function useRunStream(selectedRunId: string | null) {
  const queryClient = useQueryClient()
  const [timelineByRunId, setTimelineByRunId] = useState<Record<string, TimelineEvent[]>>({})

  useEffect(() => {
    if (!selectedRunId) {
      return
    }

    const connection = createRunsConnection()
    let isDisposed = false
    const eventNames = Object.keys(eventLabels)

    for (const eventName of eventNames) {
      connection.on(eventName, (payload: WorkflowEventPayload) => {
        if (payload.RunId !== selectedRunId) {
          return
        }

        setTimelineByRunId((current) => ({
          ...current,
          [selectedRunId]: [
            {
              kind: eventName as TimelineEvent['kind'],
              label: eventLabels[eventName](payload),
              timestamp:
                payload.StartedAt ??
                payload.CompletedAt ??
                payload.FailedAt ??
                payload.ReleasedAt ??
                new Date().toISOString(),
            },
            ...(current[selectedRunId] ?? []),
          ].slice(0, 24),
        }))

        void queryClient.invalidateQueries({ queryKey: ['runs'] })
        void queryClient.invalidateQueries({ queryKey: ['run', selectedRunId] })
      })
    }

    async function connect() {
      try {
        await connection.start()
        await connection.invoke('JoinRun', selectedRunId)
      } catch {
        if (!isDisposed) {
          void connection.stop().catch(() => undefined)
        }
      }
    }

    void connect()

    return () => {
      isDisposed = true
      void connection.invoke('LeaveRun', selectedRunId).catch(() => undefined)
      void connection.stop()
    }
  }, [queryClient, selectedRunId])

  return selectedRunId ? timelineByRunId[selectedRunId] ?? [] : []
}
