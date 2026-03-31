import type { RunDetail, RunSummary, StepStatus, TimelineEvent } from './types'

export const statusColor: Record<string, string> = {
  Pending: 'gray',
  Running: 'orange',
  Completed: 'teal',
  Failed: 'red',
  Skipped: 'dark',
  Canceled: 'gray',
}

export const statusTone: Record<string, string> = {
  Pending: 'Queued',
  Running: 'In progress',
  Completed: 'Done',
  Failed: 'Needs attention',
  Skipped: 'Skipped',
  Canceled: 'Stopped',
}

export const businessStatus: Record<string, string> = {
  Pending: 'Pending',
  Running: 'Under review',
  Completed: 'Approved',
  Failed: 'Flagged',
  Skipped: 'Skipped',
  Canceled: 'Cancelled',
}

const businessStepName: Record<string, string> = {
  IntakeNormalize: 'Validate Application',
  ExtractProfile: 'Extract Profile',
  SecurityReview: 'Security Review',
  ComplianceReview: 'Compliance Review',
  FinanceReview: 'Finance Review',
  AggregateFindings: 'Aggregate Findings',
  CustomerNextSteps: 'Customer Next Steps',
  FinalPackage: 'Final Package',
}

export function formatStepName(stepName: string) {
  return businessStepName[stepName] ?? stepName.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/\bSso\b/g, 'SSO').replace(/\bScim\b/g, 'SCIM')
}

export function formatShortRunId(runId: string | null | undefined) {
  if (!runId) {
    return 'No run selected'
  }

  return runId.slice(0, 8)
}

export function formatDateTime(value: string | null | undefined) {
  if (!value) {
    return 'Waiting'
  }

  return new Date(value).toLocaleString([], {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

export function formatTime(value: string | null | undefined) {
  if (!value) {
    return 'Just now'
  }

  return new Date(value).toLocaleTimeString([], {
    hour: 'numeric',
    minute: '2-digit',
    second: '2-digit',
  })
}

export function formatDuration(durationMs: number | null | undefined, fallback = 'In progress') {
  if (durationMs == null) {
    return fallback
  }

  if (durationMs < 1000) {
    return `${durationMs}ms`
  }

  if (durationMs < 60_000) {
    return `${Math.round(durationMs / 100) / 10}s`
  }

  const minutes = Math.floor(durationMs / 60_000)
  const seconds = Math.round((durationMs % 60_000) / 1000)
  return `${minutes}m ${seconds}s`
}

export function getRunProgressValue(completedStepCount: number, stepCount: number) {
  if (!stepCount) {
    return 0
  }

  return Math.round((completedStepCount / stepCount) * 100)
}

export function getStepStatus(runDetail: RunDetail | undefined, stepName: string): StepStatus | 'Pending' {
  return (runDetail?.steps.find((step) => step.stepName === stepName)?.status as StepStatus | undefined) ?? 'Pending'
}

export function buildTimelineFromRun(runDetail: RunDetail | undefined): TimelineEvent[] {
  if (!runDetail) {
    return []
  }

  const events: TimelineEvent[] = []

  for (const step of runDetail.steps) {
    const label = formatStepName(step.stepName)

    if (step.startedAt) {
      events.push({
        kind: 'StepStarted',
        label: `${label} started`,
        timestamp: step.startedAt,
      })
    }

    if (step.status === 'Completed' && step.completedAt) {
      events.push({
        kind: 'StepCompleted',
        label: `${label} completed`,
        timestamp: step.completedAt,
      })
    }

    if (step.status === 'Failed') {
      events.push({
        kind: 'StepFailed',
        label: step.error ? `${label} failed: ${step.error}` : `${label} failed`,
        timestamp: step.completedAt ?? step.startedAt ?? runDetail.createdAt,
      })
    }
  }

  if (runDetail.completedAt) {
    events.push({
      kind: 'RunCompleted',
      label: runDetail.status === 'Completed' ? 'Run completed' : `Run ${runDetail.status.toLowerCase()}`,
      timestamp: runDetail.completedAt,
    })
  }

  return events.sort((left, right) => new Date(right.timestamp).getTime() - new Date(left.timestamp).getTime()).slice(0, 12)
}

export function countRunsByStatus(runs: RunSummary[], status: string) {
  return runs.filter((run) => run.status === status).length
}

export const eventKindColor: Record<string, string> = {
  StepStarted: 'blue',
  StepCompleted: 'teal',
  StepFailed: 'red',
  RunCompleted: 'green',
  RunFailed: 'red',
  RunCanceled: 'gray',
  BarrierReleased: 'orange',
}

export const eventKindLabel: Record<string, string> = {
  StepStarted: 'Step started',
  StepCompleted: 'Step completed',
  StepFailed: 'Step failed',
  RunCompleted: 'Run completed',
  RunFailed: 'Run failed',
  RunCanceled: 'Run cancelled',
  BarrierReleased: 'Gate released',
}
