export interface StartRunRequest {
  inputText: string
  region: string
  urgency: string
  maxRounds: number
  tone: string
}

export interface StartRunResponse {
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

export interface RunSummary {
  id: string
  status: string
  region: string
  urgency: string
  tone: string
  createdAt: string
  completedAt?: string
  totalDurationMs?: number
  parentRunId?: string
  rootRunId?: string
  rerunFromStep?: string
  stepCount: number
  completedStepCount: number
  recommendationDecision?: string
  recommendationRisk?: string
  error?: string
}

export interface RunDetail extends RunSummary {
  maxRounds: number
  startedAt?: string
  inputTextRedacted: string
  profileJson?: string
  chairRecommendationJson?: string
  finalOutputHtml?: string
  steps: StepRun[]
  messages: GroupChatMessage[]
}

export interface StepRun {
  id: string
  stepName: string
  stepOrder: number
  status: string
  startedAt?: string
  completedAt?: string
  durationMs?: number
  outputSnapshot?: string
  error?: string
}

export interface GroupChatMessage {
  id: string
  speaker: string
  role: string
  content: string
  sequence: number
  createdAt: string
}

export interface StepDefinition {
  name: string
  order: number
  description: string
  isGroupChatStep: boolean
}

export interface SampleRequest {
  title: string
  region: string
  urgency: string
  text: string
}

export interface WorkflowEvent {
  runId: string
  kind: string
  stepName?: string
  speaker?: string
  content?: string
  timestamp: string
}
