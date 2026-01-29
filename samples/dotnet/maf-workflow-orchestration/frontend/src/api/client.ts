import type {
  WorkflowRun,
  RunSummary,
  CreateRunRequest,
  CreateRunResponse,
  RerunRequest,
  RerunResponse,
  Sample,
  StepDefinition,
  PaginatedResponse
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5266/api';

async function fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || `HTTP ${response.status}: ${response.statusText}`);
  }

  return response.json();
}

export const api = {
  // Runs
  async getRuns(skip = 0, take = 20): Promise<PaginatedResponse<RunSummary>> {
    return fetchApi(`/runs?skip=${skip}&take=${take}`);
  },

  async getRun(id: string): Promise<WorkflowRun> {
    return fetchApi(`/runs/${id}`);
  },

  async createRun(request: CreateRunRequest): Promise<CreateRunResponse> {
    // Backend expects PascalCase properties
    const backendRequest = {
      InputText: request.inputText,
      Options: request.options ? {
        Audience: request.options.audience,
        Tone: request.options.tone,
        StrictCompliance: request.options.strictCompliance
      } : undefined
    };
    
    return fetchApi('/runs', {
      method: 'POST',
      body: JSON.stringify(backendRequest),
    });
  },

  async rerunFromStep(runId: string, request: RerunRequest): Promise<RerunResponse> {
    return fetchApi(`/runs/${runId}/rerun`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  async cancelRun(runId: string): Promise<void> {
    await fetchApi(`/runs/${runId}/cancel`, {
      method: 'POST',
    });
  },

  async getLineage(runId: string): Promise<RunSummary[]> {
    return fetchApi(`/runs/${runId}/lineage`);
  },

  async getStepDefinitions(): Promise<StepDefinition[]> {
    return fetchApi('/runs/steps');
  },

  // Samples
  async getSamples(): Promise<Sample[]> {
    return fetchApi('/samples');
  },

  async getSample(id: string): Promise<Sample> {
    return fetchApi(`/samples/${id}`);
  },
};
