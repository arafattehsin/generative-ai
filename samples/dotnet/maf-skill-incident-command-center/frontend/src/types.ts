export type Incident = {
  id: string;
  title: string;
  region: string;
  vendor: string;
  status: string;
  openOrders: number;
  atRiskOrders: number;
  slaDeadlineUtc: string;
  summary: string;
  signals: string[];
  constraints: string[];
  lastUpdatedUtc: string;
};

export type SessionResponse = {
  sessionId: string;
};

export type TriageRequest = {
  sessionId: string;
  incidentId: string;
  userPrompt?: string;
};

export type TriageResponse = {
  runId: string;
  summary: string;
  severity: "low" | "medium" | "high" | "critical";
  probableCauses: string[];
  recommendedActions: string[];
  atRiskOrders: string[];
};

export type CommunicationDraftRequest = {
  sessionId: string;
  incidentId: string;
  audience: string;
  triageSummary?: string;
};

export type CommunicationDraftResponse = {
  runId: string;
  audience: string;
  draft: string;
};

export type SkillSummary = {
  name: string;
  description: string;
  resources: string[];
};

export type SkillEvent = {
  runId: string;
  timestamp: string;
  stage: "advertised" | "loaded" | "resource_read" | "completed";
  skillName: string;
  resource?: string;
  note?: string;
};
