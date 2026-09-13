import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr'

export type ApiRunPhase =
  | 'Starting'
  | 'Planning'
  | 'InitialReview'
  | 'Running'
  | 'RevisedReview'
  | 'Completed'
  | 'BoundedIncomplete'
  | 'Failed'

export type ApiEvidenceStatus =
  | 'Queued'
  | 'Checking'
  | 'Verified'
  | 'Conflict'

export type EvidenceItem = {
  id: string
  sequence: number
  reference: string
  title: string
  source: string
  detail: string
  status: ApiEvidenceStatus
  updated: string
  specialist: string
  activity: string
}

export type ExecutionBudget = {
  roundsUsed: number
  maxRounds: number
  toolCallsUsed: number
  maxToolCalls: number
  stallsObserved: number
  maxStallsBeforeReplan: number
  resetsUsed: number
  maxResets: number
  elapsedSeconds: number
  maxElapsedSeconds: number
}

export type WorksOption = {
  code: string
  title: string
  assessment: string
  supportStatus: string
}

export type PreliminaryWorksOptionsBrief = {
  recommendation: string
  summary: string
  options: WorksOption[]
  claims: { claim: string; evidenceReferences: string[] }[]
  openMatters: string[]
}

export type CivicWorksRun = {
  id: string
  caseId: string
  caseName: string
  phase: ApiRunPhase
  statusMessage: string
  planVersion: number
  planText: string | null
  revisionReason: string | null
  activeSpecialist: string | null
  activeActivity: string | null
  focusEvidenceId: string | null
  evidence: EvidenceItem[]
  budget: ExecutionBudget
  brief: PreliminaryWorksOptionsBrief | null
  startedAt: string
  updatedAt: string
  error: string | null
  activity: RunActivity[]
  plans: InvestigationPlan[]
}

export type RunActivity = {
  sequence: number
  at: string
  kind: string
  actor: string
  message: string
  detail: string | null
  evidenceId: string | null
  planVersion: number
}

export type InvestigationPlan = {
  version: number
  text: string
  createdAt: string
  approvedAt: string | null
  constraints: string[]
}

export type FoundryConfiguration = {
  isConfigured: boolean
  simulationFallbackEnabled: boolean
  authentication: string
  modelDeployment: string | null
  missingSettings: string[]
}

export type PlanReviewCommand = {
  action: 'approve' | 'revise'
  feedback?: string
  constraints?: string[]
}

const apiBaseUrl = (
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5188'
).replace(/\/$/, '')

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  })

  if (!response.ok) {
    let detail = `${response.status} ${response.statusText}`
    try {
      const problem = (await response.json()) as { detail?: string; title?: string }
      detail = problem.detail ?? problem.title ?? detail
    } catch {
      // The HTTP status remains the authoritative failure when no problem JSON exists.
    }
    throw new Error(detail)
  }

  return (await response.json()) as T
}

export function getFoundryConfiguration(): Promise<FoundryConfiguration> {
  return request<FoundryConfiguration>('/api/config')
}

export function startLiveRun(): Promise<CivicWorksRun> {
  return request<CivicWorksRun>('/api/runs', { method: 'POST' })
}

export function getLiveRun(runId: string): Promise<CivicWorksRun> {
  return request<CivicWorksRun>(`/api/runs/${runId}`)
}

export function reviewLivePlan(
  runId: string,
  command: PlanReviewCommand,
): Promise<CivicWorksRun> {
  return request<CivicWorksRun>(`/api/runs/${runId}/plan-review`, {
    method: 'POST',
    body: JSON.stringify(command),
  })
}

export async function connectToLiveRun(
  runId: string,
  onUpdate: (run: CivicWorksRun) => void,
  onConnectionError: (error: Error) => void,
  onConnectionState: (state: 'connected' | 'reconnecting' | 'disconnected') => void,
): Promise<HubConnection> {
  const connection = new HubConnectionBuilder()
    .withUrl(`${apiBaseUrl}/hubs/civicworks`)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  connection.on('runUpdated', onUpdate)
  connection.onreconnecting((error) => {
    onConnectionState('reconnecting')
    if (error) onConnectionError(error)
  })
  connection.onclose((error) => {
    onConnectionState('disconnected')
    if (error) onConnectionError(error)
  })

  connection.onreconnected(async () => {
    try {
      await connection.invoke('JoinRun', runId)
      onUpdate(await getLiveRun(runId))
      onConnectionState('connected')
    } catch (error) {
      onConnectionState('disconnected')
      onConnectionError(error instanceof Error ? error : new Error('Could not rejoin the investigation.'))
    }
  })
  try {
    await connection.start()
    await connection.invoke('JoinRun', runId)
    onUpdate(await getLiveRun(runId))
    onConnectionState('connected')
    return connection
  } catch (error) {
    await connection.stop()
    throw error
  }
}
