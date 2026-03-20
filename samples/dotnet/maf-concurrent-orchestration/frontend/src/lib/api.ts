import type {
  CreateRunRequest,
  CreateRunResponse,
  RerunResponse,
  RerunRequest,
  RunDetail,
  RunSummary,
  SampleRequest,
  StepDefinition,
} from './types'

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5099'

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Request failed with ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const api = {
  baseUrl: API_BASE_URL,
  getSamples: () => requestJson<SampleRequest[]>('/api/samples'),
  getRuns: () => requestJson<RunSummary[]>('/api/runs'),
  getRun: (runId: string) => requestJson<RunDetail>(`/api/runs/${runId}`),
  getLineage: (runId: string) => requestJson<RunSummary[]>(`/api/runs/${runId}/lineage`),
  getSteps: () => requestJson<StepDefinition[]>('/api/runs/steps'),
  createRun: (payload: CreateRunRequest) =>
    requestJson<CreateRunResponse>('/api/runs', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  rerun: (runId: string, payload: RerunRequest) =>
    requestJson<RerunResponse>(`/api/runs/${runId}/rerun`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  cancelRun: (runId: string) =>
    requestJson<void>(`/api/runs/${runId}/cancel`, {
      method: 'POST',
    }),
}
