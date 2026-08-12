import type { ConfigResponse, CreateRunRequest, SampleScenario, TravelRun } from './types'

export const apiBase = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5166'

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, {
    headers: { 'Content-Type': 'application/json', ...(options?.headers ?? {}) },
    ...options,
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || `${response.status} ${response.statusText}`)
  }

  if (response.status === 202 || response.status === 204) {
    const text = await response.text()
    return (text ? JSON.parse(text) : undefined) as T
  }

  return (await response.json()) as T
}

export const api = {
  config: () => request<ConfigResponse>('/api/config'),
  samples: () => request<SampleScenario[]>('/api/samples'),
  runs: () => request<TravelRun[]>('/api/runs'),
  run: (runId: string) => request<TravelRun>(`/api/runs/${runId}`),
  createRun: (body: CreateRunRequest) =>
    request<{ runId: string }>('/api/runs', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  sendMessage: (runId: string, message: string) =>
    request<void>(`/api/runs/${runId}/messages`, {
      method: 'POST',
      body: JSON.stringify({ message }),
    }),
  cancel: (runId: string) =>
    request<void>(`/api/runs/${runId}/cancel`, {
      method: 'POST',
    }),
  operationsCases: () => request<TravelRun[]>('/api/operations/cases'),
  claimCase: (runId: string, operatorName: string) =>
    request<void>(`/api/operations/cases/${runId}/claim`, {
      method: 'POST',
      body: JSON.stringify({ operatorName }),
    }),
  resolveCase: (runId: string, operatorName: string, message: string, nextOwnerId: string) =>
    request<void>(`/api/operations/cases/${runId}/resolve`, {
      method: 'POST',
      body: JSON.stringify({ operatorName, message, nextOwnerId }),
    }),
}
