import { useCallback, useEffect, useRef, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import {
  connectToLiveRun, getFoundryConfiguration, getLiveRun, reviewLivePlan, startLiveRun,
  type CivicWorksRun, type FoundryConfiguration, type PlanReviewCommand,
} from './api'

const runKey = 'civicworks:active-run:v1'
function rememberRun(id: string) {
  try { sessionStorage.setItem(runKey, id) } catch { /* Storage is optional. */ }
}
function savedRun() {
  try { return sessionStorage.getItem(runKey) } catch { return null }
}

export function useLiveCivicWorks() {
  const [configuration, setConfiguration] = useState<FoundryConfiguration | null>(null)
  const [run, setRun] = useState<CivicWorksRun | null>(null)
  const [busy, setBusy] = useState(false)
  const [restoring, setRestoring] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [connectionState, setConnectionState] = useState<'connected' | 'reconnecting' | 'disconnected'>('disconnected')
  const connectionRef = useRef<HubConnection | null>(null)
  const generation = useRef(0)

  const receive = useCallback((next: CivicWorksRun) => {
    setRun(current => current?.id === next.id && current.updatedAt > next.updatedAt ? current : next)
  }, [])

  const connect = useCallback(async (runId: string) => {
    const currentGeneration = ++generation.current
    await connectionRef.current?.stop()
    const connection = await connectToLiveRun(runId,
      next => { if (generation.current === currentGeneration) receive(next) },
      reason => { if (generation.current === currentGeneration) setError(reason.message) },
      state => {
        if (generation.current === currentGeneration) {
          setConnectionState(state)
          if (state === 'connected') setError(null)
        }
      })
    if (generation.current !== currentGeneration) await connection.stop()
    else connectionRef.current = connection
  }, [receive])

  useEffect(() => {
    let cancelled = false
    const connectionGeneration = generation
    getFoundryConfiguration().then(status => {
      if (!cancelled) setConfiguration(status)
    }).catch((reason: unknown) => {
      if (!cancelled) setError(reason instanceof Error ? reason.message : 'Could not reach the CivicWorks API.')
    })
    const runId = savedRun()
    const restore = async () => {
      try {
        if (runId) {
          const previous = await getLiveRun(runId)
          if (!cancelled) {
            receive(previous)
            await connect(runId)
          }
        }
      } catch {
        if (!cancelled) {
          try { sessionStorage.removeItem(runKey) } catch { /* Storage is optional. */ }
        }
      } finally {
        if (!cancelled) setRestoring(false)
      }
    }
    void restore()
    return () => {
      cancelled = true
      connectionGeneration.current++
      void connectionRef.current?.stop()
    }
  }, [connect, receive])

  const start = async () => {
    setBusy(true)
    setError(null)
    try {
      const next = await startLiveRun()
      rememberRun(next.id)
      receive(next)
      await connect(next.id)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'The live investigation could not start.')
    } finally { setBusy(false) }
  }

  const review = async (command: PlanReviewCommand) => {
    if (!run) return
    setBusy(true)
    setError(null)
    try { receive(await reviewLivePlan(run.id, command)) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'The plan could not be approved.') }
    finally { setBusy(false) }
  }

  const reconnect = async () => {
    if (!run) return
    setBusy(true)
    try { await connect(run.id) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Could not reconnect.') }
    finally { setBusy(false) }
  }

  return { configuration, run, busy, restoring, error, start, review, connectionState, reconnect }
}
