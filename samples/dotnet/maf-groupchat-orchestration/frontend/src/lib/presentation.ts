import type { RunDetail, RunSummary, StepDefinition, StepRun } from './types'

export const statusColor: Record<string, string> = {
  Pending: 'gray',
  Running: 'teal',
  Completed: 'green',
  Failed: 'red',
  Cancelled: 'dark',
}

export function formatStepName(name: string) {
  return name.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export function formatDuration(ms?: number) {
  if (!ms) return 'Queued'
  if (ms < 1000) return `${ms} ms`
  return `${(ms / 1000).toFixed(1)} s`
}

export function shortId(id?: string) {
  return id ? id.slice(0, 8) : 'none'
}

export function parseApiDate(value?: string) {
  if (!value) return undefined

  const hasTimeZone = /(?:z|[+-]\d{2}:?\d{2})$/i.test(value)
  const date = new Date(hasTimeZone ? value : `${value}Z`)
  return Number.isNaN(date.getTime()) ? undefined : date
}

export function formatDate(value?: string) {
  const date = parseApiDate(value)
  if (!date) return 'Pending'
  return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }).format(date)
}

export function progress(run?: RunDetail | RunSummary) {
  if (!run?.stepCount) return 0
  return Math.round((run.completedStepCount / run.stepCount) * 100)
}

export function getStepRun(run: RunDetail | undefined, step: StepDefinition): StepRun | undefined {
  return run?.steps.find((candidate) => candidate.stepName === step.name)
}

export function parseRecommendation(run?: RunDetail) {
  if (!run?.chairRecommendationJson) return undefined
  try {
    return JSON.parse(run.chairRecommendationJson) as {
      decision: string
      riskLevel: string
      confidence: number
      owners: string[]
      blockers: string[]
      next48Hours: string[]
    }
  } catch {
    return undefined
  }
}
