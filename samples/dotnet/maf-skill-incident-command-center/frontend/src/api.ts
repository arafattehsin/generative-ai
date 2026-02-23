import type {
  CommunicationDraftRequest,
  CommunicationDraftResponse,
  Incident,
  SessionResponse,
  SkillEvent,
  SkillSummary,
  TriageRequest,
  TriageResponse,
} from "./types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

async function callApi<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    ...init,
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    try {
      const body = await response.json();
      if (body?.detail) {
        message = body.detail;
      } else if (body?.error) {
        message = body.error;
      }
    } catch {
      // Ignore parse failure and keep status text.
    }

    throw new Error(message);
  }

  return response.json() as Promise<T>;
}

export function createSession(): Promise<SessionResponse> {
  return callApi<SessionResponse>("/api/sessions", { method: "POST" });
}

export function getIncidents(): Promise<Incident[]> {
  return callApi<Incident[]>("/api/incidents");
}

export function getSkills(): Promise<SkillSummary[]> {
  return callApi<SkillSummary[]>("/api/skills");
}

export function runTriage(payload: TriageRequest): Promise<TriageResponse> {
  return callApi<TriageResponse>("/api/triage", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function draftCommunication(payload: CommunicationDraftRequest): Promise<CommunicationDraftResponse> {
  return callApi<CommunicationDraftResponse>("/api/communications/draft", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function getRunEvents(runId: string): Promise<SkillEvent[]> {
  return callApi<SkillEvent[]>(`/api/runs/${runId}/events`);
}
