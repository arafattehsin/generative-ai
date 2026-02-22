import { useEffect, useMemo, useState } from "react";
import {
  createSession,
  draftCommunication,
  getIncidents,
  getRunEvents,
  getSkills,
  runTriage,
} from "./api";
import type { Incident, SkillEvent, SkillSummary, TriageResponse } from "./types";

const STAGE_LABELS: Record<string, string> = {
  advertised: "Advertised",
  loaded: "Loaded",
  resource_read: "Resource Read",
  completed: "Completed",
};

type BootstrapData = {
  sessionId: string;
  incidents: Incident[];
  skills: SkillSummary[];
};

let bootstrapPromise: Promise<BootstrapData> | null = null;

function getBootstrapData(): Promise<BootstrapData> {
  if (!bootstrapPromise) {
    bootstrapPromise = Promise.all([
      createSession(),
      getIncidents(),
      getSkills(),
    ]).then(([session, incidents, skills]) => ({
      sessionId: session.sessionId,
      incidents,
      skills,
    }));
  }

  return bootstrapPromise;
}

export default function App() {
  const [sessionId, setSessionId] = useState<string>("");
  const [incidents, setIncidents] = useState<Incident[]>([]);
  const [skills, setSkills] = useState<SkillSummary[]>([]);
  const [selectedIncidentId, setSelectedIncidentId] = useState<string>("");
  const [triagePrompt, setTriagePrompt] = useState<string>("Prioritize actions for the first two hours and explain escalation.");
  const [triage, setTriage] = useState<TriageResponse | null>(null);
  const [timelineEvents, setTimelineEvents] = useState<SkillEvent[]>([]);
  const [audience, setAudience] = useState<string>("customer");
  const [draft, setDraft] = useState<string>("");
  const [error, setError] = useState<string>("");
  const [isTriageLoading, setIsTriageLoading] = useState(false);
  const [isDraftLoading, setIsDraftLoading] = useState(false);

  useEffect(() => {
    let active = true;

    async function bootstrap() {
      try {
        const { sessionId: bootSessionId, incidents: incidentData, skills: skillData } = await getBootstrapData();

        if (!active) {
          return;
        }

        setSessionId(bootSessionId);
        setIncidents(incidentData);
        setSkills(skillData);

        if (incidentData.length > 0) {
          setSelectedIncidentId(incidentData[0].id);
        }
      } catch (bootstrapError) {
        if (active) {
          setError((bootstrapError as Error).message);
        }
      }
    }

    bootstrap();

    return () => {
      active = false;
    };
  }, []);

  const selectedIncident = useMemo(
    () => incidents.find((incident) => incident.id === selectedIncidentId) ?? null,
    [incidents, selectedIncidentId]
  );

  const metrics = useMemo(() => {
    const openIncidents = incidents.filter((incident) => incident.status !== "resolved").length;
    const highSeverityRisk = incidents.filter((incident) => incident.atRiskOrders >= 10).length;
    const totalAtRiskOrders = incidents.reduce((sum, incident) => sum + incident.atRiskOrders, 0);

    return {
      openIncidents,
      highSeverityRisk,
      totalAtRiskOrders,
    };
  }, [incidents]);

  const evidenceResources = useMemo(() => {
    return Array.from(new Set(timelineEvents.map((event) => event.resource).filter(Boolean) as string[]));
  }, [timelineEvents]);

  async function refreshEvents(runId: string) {
    const events = await getRunEvents(runId);
    setTimelineEvents(events);
  }

  async function handleRunTriage() {
    if (!sessionId || !selectedIncident) {
      return;
    }

    setError("");
    setDraft("");
    setIsTriageLoading(true);

    try {
      const response = await runTriage({
        sessionId,
        incidentId: selectedIncident.id,
        userPrompt: triagePrompt,
      });

      setTriage(response);
      await refreshEvents(response.runId);
    } catch (triageError) {
      setError((triageError as Error).message);
    } finally {
      setIsTriageLoading(false);
    }
  }

  async function handleDraftCommunication() {
    if (!sessionId || !selectedIncident || !triage) {
      return;
    }

    setError("");
    setIsDraftLoading(true);

    try {
      const response = await draftCommunication({
        sessionId,
        incidentId: selectedIncident.id,
        audience,
        triageSummary: triage.summary,
      });

      setDraft(response.draft);
      await refreshEvents(response.runId);
    } catch (draftError) {
      setError((draftError as Error).message);
    } finally {
      setIsDraftLoading(false);
    }
  }

  return (
    <div className="app-shell">
      <header className="topbar fade-in-up">
        <div>
          <p className="eyebrow">Microsoft Agent Framework + Skills</p>
          <h1>Supply Chain Incident Command Center</h1>
          <p className="subtitle">
            Progressive disclosure in action: skills advertise, load, and read evidence before producing triage and communication output.
          </p>
        </div>
        <div className="session-chip">Session: {sessionId || "starting..."}</div>
      </header>

      <section className="kpi-strip fade-in-up delay-1">
        <article className="kpi-card">
          <span>Open incidents</span>
          <strong>{metrics.openIncidents}</strong>
        </article>
        <article className="kpi-card">
          <span>High-risk incidents</span>
          <strong>{metrics.highSeverityRisk}</strong>
        </article>
        <article className="kpi-card">
          <span>At-risk orders</span>
          <strong>{metrics.totalAtRiskOrders}</strong>
        </article>
      </section>

      {error ? <div className="error-banner">{error}</div> : null}

      <main className="layout-grid">
        <aside className="panel queue-panel fade-in-up delay-2">
          <div className="panel-header">
            <h2>Incident Queue</h2>
          </div>
          <ul className="incident-list">
            {incidents.map((incident) => (
              <li key={incident.id}>
                <button
                  className={`incident-item ${incident.id === selectedIncidentId ? "active" : ""}`}
                  onClick={() => setSelectedIncidentId(incident.id)}
                  type="button"
                >
                  <div className="incident-top">
                    <span>{incident.id}</span>
                    <span className="status-pill">{incident.status}</span>
                  </div>
                  <h3>{incident.title}</h3>
                  <p>{incident.region} • {incident.vendor}</p>
                  <small>{incident.atRiskOrders} at-risk orders</small>
                </button>
              </li>
            ))}
          </ul>
        </aside>

        <section className="panel workspace-panel fade-in-up delay-3">
          <div className="panel-header">
            <h2>Triage Workspace</h2>
          </div>

          {selectedIncident ? (
            <>
              <article className="incident-context">
                <h3>{selectedIncident.title}</h3>
                <p>{selectedIncident.summary}</p>
                <div className="chips">
                  <span>{selectedIncident.region}</span>
                  <span>{selectedIncident.vendor}</span>
                  <span>SLA: {new Date(selectedIncident.slaDeadlineUtc).toLocaleString()}</span>
                </div>
              </article>

              <label className="field-label" htmlFor="triage-prompt">
                Operator directive
              </label>
              <textarea
                id="triage-prompt"
                value={triagePrompt}
                onChange={(event) => setTriagePrompt(event.target.value)}
                rows={4}
              />

              <div className="actions">
                <button onClick={handleRunTriage} disabled={isTriageLoading} type="button">
                  {isTriageLoading ? "Running triage..." : "Run Triage"}
                </button>
              </div>

              {triage ? (
                <article className="triage-card fade-in-up">
                  <div className="triage-head">
                    <h3>Triage Result</h3>
                    <span className={`severity ${triage.severity}`}>{triage.severity}</span>
                  </div>
                  <p>{triage.summary}</p>

                  <div className="triage-grid">
                    <div>
                      <h4>Probable causes</h4>
                      <ul>
                        {triage.probableCauses.map((cause) => (
                          <li key={cause}>{cause}</li>
                        ))}
                      </ul>
                    </div>
                    <div>
                      <h4>Recommended actions</h4>
                      <ul>
                        {triage.recommendedActions.map((action) => (
                          <li key={action}>{action}</li>
                        ))}
                      </ul>
                    </div>
                    <div>
                      <h4>At-risk order clusters</h4>
                      <ul>
                        {triage.atRiskOrders.map((order) => (
                          <li key={order}>{order}</li>
                        ))}
                      </ul>
                    </div>
                  </div>

                  <div className="draft-zone">
                    <h4>Phase 2: Stakeholder Draft</h4>
                    <div className="draft-controls">
                      <select value={audience} onChange={(event) => setAudience(event.target.value)}>
                        <option value="customer">Customer</option>
                        <option value="supplier">Supplier</option>
                        <option value="internal-leadership">Internal leadership</option>
                      </select>
                      <button onClick={handleDraftCommunication} disabled={isDraftLoading} type="button">
                        {isDraftLoading ? "Drafting..." : "Draft Stakeholder Update"}
                      </button>
                    </div>
                    {draft ? <pre className="draft-output">{draft}</pre> : null}
                  </div>
                </article>
              ) : null}
            </>
          ) : (
            <p className="empty-state">No incident selected.</p>
          )}
        </section>

        <aside className="panel timeline-panel fade-in-up delay-4">
          <div className="panel-header">
            <h2>Skill Timeline</h2>
          </div>

          <ul className="timeline-list">
            {timelineEvents.length === 0 ? (
              <li className="timeline-empty">Run triage to see progressive disclosure events.</li>
            ) : (
              timelineEvents.map((event, index) => (
                <li key={`${event.timestamp}-${index}`} className={`timeline-event stage-${event.stage}`}>
                  <span>{STAGE_LABELS[event.stage] ?? event.stage}</span>
                  <strong>{event.skillName}</strong>
                  {event.resource ? <code>{event.resource}</code> : null}
                  {event.note ? <p>{event.note}</p> : null}
                  <small>{new Date(event.timestamp).toLocaleTimeString()}</small>
                </li>
              ))
            )}
          </ul>

          <details className="evidence-drawer" open>
            <summary>Evidence Drawer ({evidenceResources.length})</summary>
            <ul>
              {evidenceResources.length === 0 ? (
                <li>No resources loaded yet.</li>
              ) : (
                evidenceResources.map((resource) => <li key={resource}>{resource}</li>)
              )}
            </ul>
          </details>

          <section className="skill-catalog">
            <h3>Advertised Skills</h3>
            {skills.map((skill) => (
              <article key={skill.name} className="skill-card">
                <h4>{skill.name}</h4>
                <p>{skill.description}</p>
              </article>
            ))}
          </section>
        </aside>
      </main>
    </div>
  );
}
