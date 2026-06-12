import { Alert, Button, Loader, Progress, Select, Text, Textarea, Title } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  IconCalendar,
  IconCheck,
  IconChevronLeft,
  IconCopy,
  IconCurrencyDollar,
  IconDots,
  IconListDetails,
  IconMenu2,
  IconRefresh,
  IconRobot,
  IconSend,
  IconTool,
  IconUser,
  IconWorld,
  IconX,
} from '@tabler/icons-react'
import { useEffect, useMemo, useState } from 'react'
import { useRunStream } from './hooks/useRunStream'
import { api } from './lib/api'
import { formatDate, formatDuration, formatStepName, getStepRun, parseRecommendation, shortId } from './lib/presentation'
import type { RunDetail, RunSummary, SampleRequest, StepDefinition, WorkflowEvent } from './lib/types'

const navItems = [
  ['Boardroom', IconRobot],
  ['Intake', IconSend],
  ['Runs', IconListDetails],
  ['Agents', IconUser],
  ['Tools', IconTool],
] as const

export default function App() {
  const queryClient = useQueryClient()
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null)
  const [sampleIndex, setSampleIndex] = useState(0)
  const [inputText, setInputText] = useState<string | null>(null)
  const [region, setRegion] = useState('AU')
  const [urgency, setUrgency] = useState('High')
  const [maxRounds] = useState(6)
  const [tone] = useState('Executive')

  const samplesQuery = useQuery({ queryKey: ['samples'], queryFn: api.getSamples })
  const runsQuery = useQuery({ queryKey: ['runs'], queryFn: api.getRuns, refetchInterval: 1500 })
  const stepsQuery = useQuery({ queryKey: ['steps'], queryFn: api.getSteps })
  const activeRunId = selectedRunId ?? runsQuery.data?.[0]?.id ?? null
  const runQuery = useQuery({
    queryKey: ['run', activeRunId],
    queryFn: () => api.getRun(activeRunId!),
    enabled: Boolean(activeRunId),
    refetchInterval: activeRunId ? 1200 : false,
  })
  const liveEvents = useRunStream(activeRunId)

  const samples = samplesQuery.data ?? []
  const requestText = inputText ?? samples[sampleIndex]?.text ?? ''
  const currentRun = runQuery.data
  const runningCount = (runsQuery.data ?? []).filter((run) => run.status === 'Running').length
  const loading = samplesQuery.isLoading || runsQuery.isLoading || stepsQuery.isLoading
  const error = samplesQuery.error ?? runsQuery.error ?? stepsQuery.error

  useEffect(() => {
    if (!activeRunId || liveEvents.length === 0) return

    void queryClient.invalidateQueries({ queryKey: ['run', activeRunId] })
    void queryClient.invalidateQueries({ queryKey: ['runs'] })
  }, [activeRunId, liveEvents.length, queryClient])

  const createRun = useMutation({
    mutationFn: api.createRun,
    onSuccess: (response) => {
      setSelectedRunId(response.runId)
      void queryClient.invalidateQueries({ queryKey: ['runs'] })
      showNotification({ title: 'Run started', message: `Boardroom ${shortId(response.runId)} is live.`, color: 'teal' })
    },
    onError: (runError) => showNotification({ title: 'Run failed to start', message: runError instanceof Error ? runError.message : 'Unknown error', color: 'red' }),
  })

  const rerun = useMutation({
    mutationFn: (fromStep: string) => api.rerun(activeRunId!, fromStep),
    onSuccess: (response) => {
      setSelectedRunId(response.newRunId)
      void queryClient.invalidateQueries({ queryKey: ['runs'] })
    },
  })

  const cancel = useMutation({
    mutationFn: (id: string) => api.cancel(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['runs'] }),
  })

  return (
    <div className="product-shell">
      <NavRail />

      <div className="main-surface">
        <StatusBar run={currentRun} runningCount={runningCount} />

        {error ? (
          <Alert color="red" icon={<IconX size={18} />} className="top-alert">
            {error instanceof Error ? error.message : 'API unavailable'}
          </Alert>
        ) : null}

        {loading ? (
          <div className="loading">
            <Loader color="teal" />
            <Text>Loading OnboardRoom...</Text>
          </div>
        ) : (
          <>
            <main className="workspace-grid">
              <IntakeStudio
                samples={samples}
                sampleIndex={sampleIndex}
                requestText={requestText}
                region={region}
                urgency={urgency}
                maxRounds={maxRounds}
                tone={tone}
                isSubmitting={createRun.isPending}
                onSample={(index) => {
                  setSampleIndex(index)
                  setInputText(null)
                  setRegion(samples[index]?.region ?? 'AU')
                  setUrgency(samples[index]?.urgency ?? 'Normal')
                }}
                onText={setInputText}
                onUrgency={setUrgency}
                onStart={() => createRun.mutate({ inputText: requestText, region, urgency, maxRounds, tone })}
              />
              <Boardroom run={currentRun} events={liveEvents} />
              <ChairPanel run={currentRun} onCancel={() => activeRunId && cancel.mutate(activeRunId)} />
            </main>

            <section className="bottom-grid">
              <RunHistory runs={runsQuery.data ?? []} activeRunId={activeRunId} onSelect={setSelectedRunId} />
              <StepInspector run={currentRun} steps={stepsQuery.data ?? []} onRerun={(step) => rerun.mutate(step)} />
            </section>
          </>
        )}
      </div>
    </div>
  )
}

function NavRail() {
  return (
    <aside className="nav-rail">
      <div className="nav-top">
        <button className="nav-menu" aria-label="Menu"><IconMenu2 size={20} /></button>
        <div className="brand-mark" aria-hidden>
          <span />
          <span />
          <span />
          <span />
          <span />
        </div>
      </div>
      <nav className="nav-list">
        {navItems.map(([label, Icon]) => (
          <button key={label} className={label === 'Boardroom' ? 'nav-item active' : 'nav-item'}>
            <Icon size={18} />
            <span>{label}</span>
          </button>
        ))}
      </nav>
      <button className="collapse-button">
        <IconChevronLeft size={18} />
        <span>Collapse</span>
      </button>
    </aside>
  )
}

function StatusBar({ run, runningCount }: { run?: RunDetail; runningCount: number }) {
  const status = run?.status ?? (runningCount > 0 ? 'Running' : 'Ready')
  return (
    <header className="status-bar">
      <div className="status-title">
        <Title order={1}>OnboardRoom</Title>
        <Text>Microsoft Foundry Multi-Agent Orchestration</Text>
      </div>
      <StatusMetric label="Run Status" value={status.toUpperCase()} tone={status === 'Running' ? 'green' : 'teal'} />
      <StatusMetric label="Elapsed" value={formatDuration(run?.totalDurationMs ?? 0).replace('Queued', '00:00:00')} />
      <StatusMetric label="Deployment" value="Configured" />
      <div className="correlation">
        <span>Correlation ID</span>
        <strong>{run ? shortId(run.id) : 'No run'}</strong>
        <IconCopy size={16} />
      </div>
    </header>
  )
}

function StatusMetric({ label, value, detail, tone }: { label: string; value: string; detail?: string; tone?: 'green' | 'teal' }) {
  return (
    <div className={`status-metric ${tone ?? ''}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      {detail ? <small>{detail}</small> : null}
    </div>
  )
}

function IntakeStudio(props: {
  samples: SampleRequest[]
  sampleIndex: number
  requestText: string
  region: string
  urgency: string
  maxRounds: number
  tone: string
  isSubmitting: boolean
  onSample: (index: number) => void
  onText: (value: string) => void
  onUrgency: (value: string) => void
  onStart: () => void
}) {
  return (
    <section className="concept-panel intake-panel">
      <PanelHeader title="Intake Studio" />

      <label className="field-label">Onboarding Request <span>Required</span></label>
      <Textarea value={props.requestText} onChange={(event) => props.onText(event.currentTarget.value)} autosize minRows={8} maxRows={10} className="concept-textarea" />
      <div className="char-count">{props.requestText.length} / 3000</div>

      <Text className="section-label">Sample Requests</Text>
      <div className="concept-samples">
        {props.samples.map((sample, index) => (
          <button key={sample.title} className={index === props.sampleIndex ? 'concept-sample active' : 'concept-sample'} onClick={() => props.onSample(index)}>
            {index === 0 ? <IconUser size={18} /> : index === 1 ? <IconWorld size={18} /> : <IconCurrencyDollar size={18} />}
            <span>{sample.title}</span>
          </button>
        ))}
      </div>

      <div className="advanced-head">
        <Text className="section-label">Advanced Options</Text>
        <IconChevronLeft size={16} className="chevron-up" />
      </div>
      <div className="option-grid">
        <div>
          <label>Effective Date</label>
          <div className="date-input"><span>2026-06-12</span><IconCalendar size={16} /></div>
        </div>
        <div>
          <label>Urgency</label>
          <Select value={props.urgency} onChange={(value) => props.onUrgency(value ?? 'Normal')} data={['Normal', 'High', 'Critical']} />
        </div>
      </div>

      <div className="submit-row">
        <Button fullWidth size="md" color="teal" loading={props.isSubmitting} leftSection={<IconSend size={16} />} onClick={props.onStart}>
          Submit to Boardroom
        </Button>
      </div>
    </section>
  )
}

function PanelHeader({ title, action }: { title: string; action?: string }) {
  return (
    <div className="panel-header">
      <h2>{title}</h2>
      {action ? <button>{action}</button> : null}
    </div>
  )
}

function Boardroom({ run, events }: { run?: RunDetail; events: WorkflowEvent[] }) {
  const transcript = useMemo(() => run?.messages ?? [], [run?.messages])
  const timelineItems = events.length
    ? events.slice(0, 5).map((event) => ({
      key: `${event.kind}-${event.timestamp}`,
      time: formatEventTime(event.timestamp),
      label: event.speaker ? `${formatSpeaker(event.speaker)} spoke` : formatEventKind(event.kind),
    }))
    : transcript.slice(-5).reverse().map((message) => ({
      key: message.id,
      time: formatEventTime(message.createdAt),
      label: `${formatSpeaker(message.speaker)} spoke`,
    }))
  const getStep = (name: string) => run?.steps.find((step) => step.stepName === name)
  const stateFor = (name: string) => {
    if (!run) return 'READY'
    const step = getStep(name)
    if (run.status === 'Cancelled' && step?.status !== 'Completed') return 'CANCELLED'
    if (run.status === 'Failed' && step?.status !== 'Completed') return step ? 'FAILED' : 'STOPPED'
    if (step?.status) return step.status.toUpperCase()
    if (run.status === 'Running') return 'PENDING'
    return 'NOT STARTED'
  }
  const timeFor = (name: string) => {
    if (!run) return 'Not started'
    const step = getStep(name)
    if (!step) return run.status === 'Running' ? 'Queued' : 'Pending'
    if (run.status === 'Cancelled' && step.status !== 'Completed') return 'Cancelled'
    if (run.status === 'Failed' && step.status !== 'Completed') return 'Failed'
    return step.durationMs ? formatDuration(step.durationMs) : step.status
  }
  const boardroomStatus = run?.status ?? 'Ready'
  const phase = getBoardroomPhase(run)
  const agents = [
    { role: 'Intake Agent', area: 'Intake', step: 'IntakeNormalize', state: stateFor('IntakeNormalize'), tone: 'teal', text: run ? 'Normalizes the onboarding request for the boardroom.' : 'Waiting for a submitted onboarding request.', time: timeFor('IntakeNormalize') },
    { role: 'Benefits Agent', area: 'Benefits', step: 'BoardroomReview', state: stateFor('BoardroomReview'), tone: 'amber', text: 'Reviews benefits orientation and employee experience needs.', time: timeFor('BoardroomReview') },
    { role: 'Access Agent', area: 'Access', step: 'BoardroomReview', state: stateFor('BoardroomReview'), tone: 'green', text: 'Reviews device, identity, GitHub, and entitlement needs.', time: timeFor('BoardroomReview') },
    { role: 'Policy Agent', area: 'Policy', step: 'BoardroomReview', state: stateFor('BoardroomReview'), tone: 'orange', text: 'Reviews safety, compliance, and regional policy risks.', time: timeFor('BoardroomReview') },
    { role: 'Chair', area: 'Synthesis', step: 'ChairRecommendation', state: stateFor('ChairRecommendation'), tone: 'coral', text: 'Synthesizes the recommendation after the boardroom review.', time: timeFor('ChairRecommendation') },
  ]

  return (
    <section className="concept-panel boardroom-panel">
      <div className="boardroom-head">
        <div>
          <h2>Boardroom <span>({boardroomStatus})</span>{run?.status === 'Running' ? <b /> : null}</h2>
        </div>
        <div className="boardroom-tools">
          <span>{run ? `Run ${shortId(run.id)}` : 'No run selected'}</span>
        </div>
      </div>

      <div className="phase-strip">
        {phase.items.map((item) => (
          <div key={item.label} className={`phase-step ${item.state}`}>
            <span />
            <strong>{item.label}</strong>
          </div>
        ))}
      </div>

      <div className="topology">
        {agents.map((agent, index) => (
          <AgentCard key={agent.role} agent={agent} index={index} />
        ))}
        <div className="transcript-core">
          <div className="transcript-user" />
          <strong>{phase.current}</strong>
          <div className="phase-progress"><i style={{ width: `${phase.progress}%` }} /></div>
          {timelineItems.length ? (
            <ul>
              {timelineItems.map((item, index) => (
                <li key={item.key}>
                  <span className={`dot dot-${index}`} />
                  <em>{item.time}</em>
                  <p>{item.label}</p>
                </li>
              ))}
            </ul>
          ) : (
            <p className="empty-transcript">No live events yet.</p>
          )}
        </div>
      </div>

      <div className="transcript-drawer">
        {run?.error ? (
          <div className="run-error">
            <strong>Run failed</strong>
            <span>{run.error}</span>
          </div>
        ) : null}
        <BoardroomOutput run={run} transcript={transcript} />
      </div>
    </section>
  )
}

function BoardroomOutput({ run, transcript }: { run?: RunDetail; transcript: RunDetail['messages'] }) {
  const recommendation = parseRecommendation(run)
  const turns = transcript.slice(-5).reverse()

  if (!run) {
    return <Text c="dimmed">Submit a request to start the boardroom.</Text>
  }

  if (!recommendation && turns.length === 0) {
    return <Text c="dimmed">No boardroom output yet.</Text>
  }

  return (
    <div className="boardroom-output">
      <div className="output-summary-card">
        <div>
          <span>Boardroom Output</span>
          <strong>{recommendation?.decision ?? 'Awaiting chair recommendation'}</strong>
        </div>
        <div className="summary-metrics">
          <b>{recommendation?.riskLevel ?? 'Pending'} risk</b>
          <b>{recommendation?.confidence ?? 0}% confidence</b>
        </div>
      </div>

      {recommendation?.next48Hours?.length ? (
        <div className="next-actions">
          <span>Next 48 Hours</span>
          <ul>
            {recommendation.next48Hours.slice(0, 4).map((item) => <li key={item}>{item}</li>)}
          </ul>
        </div>
      ) : null}

      {turns.length ? (
        <div className="speaker-turns">
          <span>Speaker Notes</span>
          <div>
            {turns.map((message) => (
              <article key={message.id}>
                <strong>{formatSpeaker(message.speaker)}</strong>
                <em>{formatDate(message.createdAt)}</em>
                <p>{cleanTranscriptPreview(message.content)}</p>
              </article>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  )
}

function getBoardroomPhase(run?: RunDetail) {
  const intake = run?.steps.find((step) => step.stepName === 'IntakeNormalize')
  const review = run?.steps.find((step) => step.stepName === 'BoardroomReview')
  const chair = run?.steps.find((step) => step.stepName === 'ChairRecommendation')
  const exportStep = run?.steps.find((step) => step.stepName === 'FinalPackage')
  const isTerminal = run?.status === 'Failed' || run?.status === 'Cancelled'
  const phaseState = (step?: { status: string }, label?: string) => {
    if (!run) return 'pending'
    if (step?.status === 'Completed') return 'complete'
    if (isTerminal && step) return run.status.toLowerCase()
    if (isTerminal && label !== 'Intake') return run.status.toLowerCase()
    if (step?.status === 'Running') return 'active'
    return 'pending'
  }
  const items = [
    { label: 'Intake', state: phaseState(intake, 'Intake') },
    { label: 'Boardroom', state: phaseState(review, 'Boardroom') },
    { label: 'Chair', state: phaseState(chair, 'Chair') },
    { label: 'Export', state: phaseState(exportStep, 'Export') },
  ]
  const activeItem = items.find((item) => item.state === 'active')
  const terminalItem = items.find((item) => item.state === 'failed' || item.state === 'cancelled')
  const completed = items.filter((item) => item.state === 'complete').length
  const progress = run ? Math.min(100, Math.round((completed / items.length) * 100)) : 0
  return {
    current: activeItem ? `${activeItem.label} in progress` : terminalItem ? `${run?.status} at ${terminalItem.label}` : run?.status === 'Completed' ? 'Completed' : 'Waiting for run',
    items,
    progress: activeItem ? Math.max(progress, 35) : progress,
  }
}

function formatEventTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 'Pending'
  return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
}

function formatSpeaker(value: string) {
  const normalized = value.toLowerCase()
  if (normalized.includes('chair')) return 'Chair'
  if (normalized.includes('benefit')) return 'Benefits'
  if (normalized.includes('access')) return 'Access'
  if (normalized.includes('policy')) return 'Policy'
  if (normalized.includes('intake')) return 'Intake'
  return value
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (letter) => letter.toUpperCase())
}

function formatEventKind(value: string) {
  const text = value.replace(/([a-z])([A-Z])/g, '$1 $2')
  return text.charAt(0).toUpperCase() + text.slice(1)
}

function cleanTranscriptPreview(value: string) {
  return value
    .replace(/#{1,6}\s*/g, '')
    .replace(/\*\*/g, '')
    .replace(/\s*-\s*/g, ' - ')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 260)
}

function AgentCard({ agent, index }: { agent: { role: string; area: string; state: string; tone: string; text: string; time: string }; index: number }) {
  return (
    <article className={`agent-card agent-${index} ${agent.tone} ${agent.state.toLowerCase().replace(/\s+/g, '-')}`}>
      <div className="agent-card-head">
        <div className="agent-icon"><IconRobot size={18} /></div>
        <strong>{agent.role}</strong>
        <span>{agent.state}</span>
      </div>
      <div className="agent-subhead">
        <b>{agent.area}</b>
        <em>{agent.time}</em>
      </div>
      <p>{agent.text}</p>
    </article>
  )
}

function ChairPanel({ run, onCancel }: { run?: RunDetail; onCancel: () => void }) {
  const recommendation = parseRecommendation(run)
  const checklist = recommendation?.next48Hours ?? []
  const owners = recommendation?.owners ?? []
  return (
    <aside className="concept-panel chair-panel">
      <PanelHeader title="Chair Recommendation" />
      <div className="decision-row">
        <span>Decision</span>
        <strong className={!recommendation ? 'empty-decision' : ''}><IconCheck size={18} /> {recommendation?.decision ?? (run?.status === 'Failed' ? 'Run failed' : 'Awaiting recommendation')}</strong>
      </div>
      <div className="risk-row">
        <span>Risk Level</span>
        <strong>{recommendation?.riskLevel ?? 'Unknown'}</strong>
      </div>
      <div className="confidence-row">
        <span>Confidence</span>
        <b>{recommendation?.confidence ?? 0}%</b>
        <Progress value={recommendation?.confidence ?? 0} color="teal" />
      </div>
      <Text className="section-label">Owners</Text>
      {owners.length ? (
        <div className="owner-table">
          {owners.slice(0, 4).map((owner, index) => (
          <div key={owner}>
            <span>{['Process Owner', 'Access Owner', 'Benefits Owner', 'IT Owner'][index]}</span>
            <strong>{owner}</strong>
            <IconUser size={16} />
          </div>
          ))}
        </div>
      ) : <div className="empty-block">Owners appear after the chair recommendation is generated.</div>}
      <div className="checklist-head">
        <Text className="section-label">48-Hour Onboarding Checklist</Text>
        <strong>{checklist.length ? `${checklist.length} / ${checklist.length}` : '0 / 0'}</strong>
      </div>
      {checklist.length ? (
        <div className="checklist">
          {checklist.map((item, index) => (
          <div key={item}>
            <IconCheck size={16} />
            <span>{item}</span>
            <em>Due: {index < 2 ? `${(index + 1) * 2}h` : `${Math.min(48, index * 6)}h`}</em>
          </div>
          ))}
        </div>
      ) : <div className="empty-block">Checklist appears after a successful boardroom run.</div>}
      <div className="chair-actions">
        <Button variant="default" disabled={!run || run.status !== 'Running'} onClick={onCancel}>Cancel Run</Button>
        <Button color="teal" component="a" href={run?.finalOutputHtml ? api.exportUrl(run.id) : undefined} target="_blank" disabled={!run?.finalOutputHtml}>Export HTML</Button>
      </div>
    </aside>
  )
}

function RunHistory({ runs, activeRunId, onSelect }: { runs: RunSummary[]; activeRunId?: string | null; onSelect: (id: string) => void }) {
  return (
    <section className="concept-panel run-history-panel">
      <div className="table-head">
        <h2>Run History</h2>
      </div>
      <div className="history-table">
        <div className="history-header">
          <span>Run ID</span><span>Status</span><span>Summary</span><span>Started At</span><span>Duration</span><span>Risk</span><span>Decision</span><span>Actions</span>
        </div>
        {runs.map((run) => (
          <button key={run.id} className={run.id === activeRunId ? 'history-row active' : 'history-row'} onClick={() => onSelect(run.id)}>
            <span>{shortId(run.id)}</span>
            <StatusPill status={run.status} />
            <strong>{getRunSummaryLabel(run)}</strong>
            <span>{formatDate(run.createdAt)}</span>
            <span>{formatDuration(run.totalDurationMs)}</span>
            <RiskPill risk={run.recommendationRisk ?? 'Pending'} />
            <span>{run.recommendationDecision ?? 'Pending'}</span>
            <IconDots size={18} />
          </button>
        ))}
        {runs.length === 0 ? <div className="empty-table">No runs yet. Submit a request to populate history.</div> : null}
      </div>
      <div className="pagination"><span>Showing {runs.length ? `1 to ${runs.length} of ${runs.length}` : '0'} runs</span></div>
    </section>
  )
}

function StatusPill({ status }: { status: string }) {
  return <span className={`status-pill ${status.toLowerCase()}`}>{status}</span>
}

function RiskPill({ risk }: { risk: string }) {
  return <span className={`risk-pill ${risk.toLowerCase()}`}>{risk}</span>
}

function getRunSummaryLabel(run: RunSummary) {
  if (run.recommendationDecision) return run.recommendationDecision
  if (run.status === 'Cancelled') return 'Cancelled before recommendation'
  if (run.status === 'Failed') return 'Failed before recommendation'
  return 'Awaiting chair recommendation'
}

function StepInspector({ run, steps, onRerun }: { run?: RunDetail; steps: StepDefinition[]; onRerun: (step: string) => void }) {
  const [selectedStepName, setSelectedStepName] = useState<string | undefined>()
  const selectedStep = steps.find((step) => step.name === (selectedStepName ?? 'BoardroomReview')) ?? steps[0]
  const stepRun = selectedStep ? getStepRun(run, selectedStep) : undefined
  const displayStatus = getDisplayStepStatus(run, stepRun)
  const output = stepRun?.outputSnapshot ?? stepRun?.error ?? run?.error ?? getEmptyStepOutput(run, displayStatus)
  return (
    <section className="concept-panel inspector-panel">
      <div className="inspector-head">
        <h2>Step Inspector</h2>
        <label>Step</label>
        <select value={selectedStep?.name ?? ''} onChange={(event) => setSelectedStepName(event.currentTarget.value)}>
          {steps.map((step) => <option key={step.name} value={step.name}>{formatStepName(step.name)}</option>)}
        </select>
        <button disabled={!run || !selectedStep} onClick={() => selectedStep && onRerun(selectedStep.name)}><IconRefresh size={16} /> Rerun from step</button>
      </div>
      <div className="inspector-body">
        <div className="inspector-meta">
          <div><span>Status</span><StatusPill status={displayStatus} /></div>
          <div><span>Step</span><strong>{selectedStep ? formatStepName(selectedStep.name) : 'No step'}</strong></div>
          <div><span>Started</span><strong>{formatDate(stepRun?.startedAt)}</strong></div>
          <div><span>Completed</span><strong>{formatDate(stepRun?.completedAt)}</strong></div>
          <div><span>Duration</span><strong>{formatDuration(stepRun?.durationMs)}</strong></div>
          <div><span>Tool</span><strong>{stepRun ? 'Foundry agent/tool call' : 'Not started'}</strong></div>
          <div><span>Result</span><strong className={displayStatus === 'Completed' ? 'success-text' : ''}>{displayStatus}</strong></div>
        </div>
        <div className="output-pane">
          <div className="output-tabs"><button>Input</button><button className="active">Output</button><button>Events</button><button>Logs</button></div>
          <pre>{output}</pre>
        </div>
      </div>
    </section>
  )
}

function getDisplayStepStatus(run?: RunDetail, stepRun?: { status: string }) {
  if (!stepRun) return run?.status === 'Cancelled' || run?.status === 'Failed' ? run.status : 'Pending'
  if ((run?.status === 'Cancelled' || run?.status === 'Failed') && stepRun.status !== 'Completed') return run.status
  return stepRun.status
}

function getEmptyStepOutput(run: RunDetail | undefined, status: string) {
  if (status === 'Cancelled') return 'Run was cancelled before this step produced output.'
  if (status === 'Failed') return 'Run failed before this step produced output.'
  if (!run) return 'No run selected.'
  return 'No output yet.'
}
