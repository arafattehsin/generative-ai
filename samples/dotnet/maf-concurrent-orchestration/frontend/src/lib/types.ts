export type WorkflowStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Canceled'
export type StepStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Skipped'

export interface SampleRequest {
  title: string
  text: string
}

export interface CreateRunRequest {
  inputText: string
}

export interface CreateRunResponse {
  runId: string
  status: string
  createdAt: string
}

export interface RerunResponse {
  newRunId: string
  parentRunId: string
  fromStep: string
  status: string
  createdAt: string
}

export interface RerunRequest {
  fromStep: string
}

export interface RunSummary {
  id: string
  status: WorkflowStatus | string
  createdAt: string
  completedAt?: string | null
  totalDurationMs?: number | null
  parentRunId?: string | null
  rerunFromStep?: string | null
  stepCount: number
  completedStepCount: number
  error?: string | null
}

export interface RunStep {
  id: string
  stepName: string
  stepOrder: number
  status: StepStatus | string
  concurrencyGroup?: string | null
  startedAt?: string | null
  completedAt?: string | null
  durationMs?: number | null
  outputSnapshot?: string | null
  error?: string | null
}

export interface RunDetail {
  id: string
  status: WorkflowStatus | string
  createdAt: string
  startedAt?: string | null
  completedAt?: string | null
  totalDurationMs?: number | null
  parentRunId?: string | null
  rootRunId?: string | null
  rerunFromStep?: string | null
  inputTextRedacted?: string | null
  finalOutputHtml?: string | null
  error?: string | null
  steps: RunStep[]
}

export interface StepDefinition {
  name: string
  order: number
  description: string
  isConcurrent: boolean
  concurrencyGroup?: string | null
}

export interface WorkflowEventPayload {
  RunId: string
  StepName?: string
  StartedAt?: string
  CompletedAt?: string
  FailedAt?: string
  DurationMs?: number
  Error?: string
  StatusMessage?: string
  ProgressPercent?: number | null
  ReviewerNames?: string[]
  ReleasedAt?: string
  Success?: boolean
}

export interface TimelineEvent {
  kind:
    | 'StepStarted'
    | 'StepCompleted'
    | 'StepFailed'
    | 'StepStatus'
    | 'ConcurrentGroupStarted'
    | 'BarrierReleased'
    | 'RunCompleted'
  label: string
  timestamp: string
}
