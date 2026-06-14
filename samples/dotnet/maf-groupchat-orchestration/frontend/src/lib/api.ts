import type { AppConfig, RerunResponse, RunDetail, RunSummary, SampleRequest, StartRunRequest, StartRunResponse, StepDefinition } from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5088'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
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
  getConfig: () => request<AppConfig>('/api/config'),
  getSamples: () => request<SampleRequest[]>('/api/samples'),
  getRuns: () => request<RunSummary[]>('/api/runs'),
  getRun: (id: string) => request<RunDetail>(`/api/runs/${id}`),
  getSteps: () => request<StepDefinition[]>('/api/runs/steps'),
  createRun: (body: StartRunRequest) => request<StartRunResponse>('/api/runs', { method: 'POST', body: JSON.stringify(body) }),
  rerun: (id: string, fromStep: string) =>
    request<RerunResponse>(`/api/runs/${id}/rerun`, { method: 'POST', body: JSON.stringify({ fromStep }) }),
  cancel: (id: string) => request<void>(`/api/runs/${id}/cancel`, { method: 'POST' }),
  exportUrl: (id: string) => `${API_BASE_URL}/api/runs/${id}/export`,
}
