// API Types

export const WorkflowStatus = {
  Pending: 'Pending',
  Running: 'Running',
  Completed: 'Completed',
  Failed: 'Failed',
  Canceled: 'Canceled'
} as const;
export type WorkflowStatus = typeof WorkflowStatus[keyof typeof WorkflowStatus];

export const StepStatus = {
  Pending: 'Pending',
  Running: 'Running',
  Completed: 'Completed',
  Failed: 'Failed',
  Skipped: 'Skipped'
} as const;
export type StepStatus = typeof StepStatus[keyof typeof StepStatus];

export const AudienceType = {
  Customer: 'Customer',
  Internal: 'Internal',
  Legal: 'Legal'
} as const;
export type AudienceType = typeof AudienceType[keyof typeof AudienceType];

export const ToneType = {
  Professional: 'Professional',
  Friendly: 'Friendly',
  Formal: 'Formal'
} as const;
export type ToneType = typeof ToneType[keyof typeof ToneType];

export interface RunOptions {
  audience: AudienceType;
  tone: ToneType;
  strictCompliance: boolean;
}

export interface StepDefinition {
  name: string;
  order: number;
  description: string;
  usesLlm: boolean;
}

export interface StepRun {
  id: string;
  stepName: string;
  stepOrder: number;
  status: string;
  startedAt?: string;
  completedAt?: string;
  durationMs?: number;
  inputSnapshot?: string;
  inputIsTruncated: boolean;
  inputFullLength?: number;
  outputSnapshot?: string;
  outputIsTruncated: boolean;
  outputFullLength?: number;
  warningsJson?: string;
  error?: string;
}

export interface WorkflowRun {
  id: string;
  parentRunId?: string;
  rootRunId?: string;
  status: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  totalDurationMs?: number;
  inputTextRedacted?: string;
  finalOutputHtml?: string;
  error?: string;
  rerunFromStep?: string;
  options?: RunOptions;
  steps: StepRun[];
}

export interface RunSummary {
  id: string;
  parentRunId?: string;
  status: string;
  createdAt: string;
  completedAt?: string;
  totalDurationMs?: number;
  rerunFromStep?: string;
  completedSteps: number;
  totalSteps: number;
  options?: RunOptions;
}

export interface CreateRunRequest {
  inputText: string;
  options?: RunOptions;
}

export interface CreateRunResponse {
  runId: string;
  status: string;
  createdAt: string;
}

export interface RerunRequest {
  fromStep: string;
}

export interface RerunResponse {
  newRunId: string;
  parentRunId: string;
  fromStep: string;
  status: string;
  createdAt: string;
}

export interface Sample {
  id: string;
  name: string;
  description: string;
  inputText: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  skip: number;
  take: number;
}

// SignalR Event Types
export interface StepStartedEvent {
  runId: string;
  stepName: string;
  startedAt: string;
}

export interface StepCompletedEvent {
  runId: string;
  stepName: string;
  durationMs: number;
  completedAt: string;
}

export interface StepFailedEvent {
  runId: string;
  stepName: string;
  error: string;
  failedAt: string;
}

export interface RunCompletedEvent {
  runId: string;
  success: boolean;
  error?: string;
  completedAt: string;
}
