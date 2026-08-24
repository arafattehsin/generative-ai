import { useEffect, useRef, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import {
  connectToLiveRun,
  getFoundryConfiguration,
  reviewLivePlan,
  startLiveRun,
  type CivicWorksRun,
  type FoundryConfiguration,
  type PlanReviewCommand,
} from './api'

export function useLiveCivicWorks() {
  const [configuration, setConfiguration] =
    useState<FoundryConfiguration | null>(null)
  const [run, setRun] = useState<CivicWorksRun | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    let cancelled = false
    getFoundryConfiguration()
      .then((status) => {
        if (!cancelled) setConfiguration(status)
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setError(
            reason instanceof Error
              ? reason.message
              : 'Could not reach the CivicWorks backend.',
          )
        }
      })

    return () => {
      cancelled = true
      void connectionRef.current?.stop()
    }
  }, [])

  const connect = async (runId: string) => {
    await connectionRef.current?.stop()
    connectionRef.current = await connectToLiveRun(
      runId,
      (nextRun) => {
        setRun(nextRun)
        if (nextRun.phase === 'Failed') setError(nextRun.error)
      },
      (connectionError) => setError(connectionError.message),
    )
  }

  const start = async () => {
    setBusy(true)
    setError(null)
    try {
      const nextRun = await startLiveRun()
      setRun(nextRun)
      await connect(nextRun.id)
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : 'The live Microsoft Foundry run could not start.',
      )
    } finally {
      setBusy(false)
    }
  }

  const review = async (command: PlanReviewCommand) => {
    if (!run) return
    setBusy(true)
    setError(null)
    try {
      setRun(await reviewLivePlan(run.id, command))
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : 'The live plan-review response could not be sent.',
      )
    } finally {
      setBusy(false)
    }
  }

  return { configuration, run, busy, error, start, review }
}
