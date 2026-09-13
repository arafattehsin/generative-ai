import { useEffect, useState } from 'react'
import {
  IconActivity, IconArrowRight, IconCheck, IconChevronDown, IconClock,
  IconFileDescription, IconLock, IconPlayerPause, IconSearch, IconShieldCheck,
  IconStack2,
} from '@tabler/icons-react'
import type { CivicWorksRun, EvidenceItem, PreliminaryWorksOptionsBrief } from './api'

import { timeOfDay, duration, isTerminal } from './display'

export function StageTrack({ run }: { run: CivicWorksRun | null }) {
  const stage = !run || run.phase === 'InitialReview' || (run.phase === 'Planning' && run.planVersion < 2) || run.phase === 'Starting' ? 0
    : run.phase === 'Completed' ? 4 : run.phase === 'RevisedReview' || (run.phase === 'Planning' && run.planVersion > 1) ? 2
      : run.planVersion > 1 ? 3 : 1
  const stages = [['Plan', 'Review the approach'], ['Investigate', 'Collect the evidence'], ['Reconsider', 'Adapt when needed'], ['Verify', 'Challenge the options'], ['Brief', 'Review the outcome']]
  return <ol className="stage-track" aria-label="Investigation progress">
    {stages.map(([title, subtitle], i) => <li key={title} className={(i < stage ? 'done ' : '') + (i === stage ? 'current' : '')} aria-current={i === stage ? 'step' : undefined}>
      <span className="stage-number">{i < stage || run?.phase === 'Completed' ? <IconCheck size={15} /> : String(i + 1).padStart(2, '0')}</span>
      <span><strong>{title}</strong><small>{subtitle}</small></span>
      {i < stages.length - 1 ? <IconArrowRight className="stage-arrow" size={16} /> : null}
    </li>)}
  </ol>
}

const constraints = ['Keep all evidence read-only', 'Do not assume heritage significance', 'Preserve business and accessible access']

export function PlanPanel({ run, busy, onApprove }: {
  run: CivicWorksRun | null; busy: boolean; onApprove: (constraints: string[]) => Promise<void>
}) {
  const [selectedVersion, setSelectedVersion] = useState<number | null>(null)
  if (!run?.planText) return null
  const plans = run.plans?.length ? run.plans : [{ version: run.planVersion, text: run.planText, createdAt: run.updatedAt, approvedAt: null, constraints: [] }]
  const selected = plans.find(plan => plan.version === selectedVersion) ?? plans[plans.length - 1]
  const current = selected.version === run.planVersion
  const awaiting = current && (run.phase === 'InitialReview' || run.phase === 'RevisedReview')
  return <section className="plan-panel panel" id="plan-review" aria-label="Manager plan">
    <div className="section-title"><div><div className="eyebrow">PROPOSED BY THE MAGENTIC MANAGER</div><h3>The investigation plan</h3></div>
      <span className={'plan-state ' + (awaiting ? 'awaiting' : '')}>{awaiting ? 'Review needed' : selected.approvedAt ? 'Approved' : 'Proposed'}</span>
    </div>
    <div className="plan-versions" aria-label="Plan versions">{plans.map(plan => <button key={plan.version}
      aria-pressed={plan.version === selected.version} className={plan.version === selected.version ? 'selected' : ''}
      onClick={() => setSelectedVersion(plan.version)}>
      Plan {String(plan.version).padStart(2, '0')} <span>{plan.version < run.planVersion ? 'Previous' : 'Current'}</span>
    </button>)}</div>
    <div className="plan-document">
      <div className="document-meta"><IconFileDescription size={16} /><span>Live manager output</span><time dateTime={selected.createdAt}>{timeOfDay(selected.createdAt)}</time></div>
      <div className="plan-prose" tabIndex={0} aria-label={'Full text of Plan ' + selected.version}><PlanText text={selected.text} /></div>
    </div>
    {selected.approvedAt ? <div className="approval-receipt"><IconShieldCheck size={18} /><div><strong>Approved by the officer at {timeOfDay(selected.approvedAt)}</strong>
      <p>{selected.constraints.join(' · ')}</p></div></div> : null}
    {awaiting ? <PlanApproval key={selected.version} busy={busy} onApprove={onApprove} version={selected.version} /> : null}
  </section>
}

function PlanApproval({ busy, onApprove, version }: { busy: boolean; onApprove: (constraints: string[]) => Promise<void>; version: number }) {
  const [reviewed, setReviewed] = useState(false)
  return <div className="plan-approval">
    <div className="eyebrow">OFFICER CHECKPOINT</div>
    <p>The workflow is paused. Approval allows the investigation to proceed with these constraints:</p>
    <ul>{constraints.map(item => <li key={item}><IconLock size={14} />{item}</li>)}</ul>
    <label className="review-confirm"><input type="checkbox" checked={reviewed} onChange={e => setReviewed(e.target.checked)} />I have reviewed this plan and its constraints.</label>
    <button className="button primary" disabled={!reviewed || busy} onClick={() => onApprove(constraints)}>
      <IconCheck size={18} />{busy ? 'Sending approval…' : 'Approve Plan ' + String(version).padStart(2, '0')}<IconArrowRight size={17} />
    </button>
    <span className="approval-hint"><IconPlayerPause size={14} /> Continue reading to keep the workflow paused.</span>
  </div>
}

function PlanText({ text }: { text: string }) {
  let structured: Record<string, unknown> | null = null
  try {
    const parsed: unknown = JSON.parse(text.replace(/^\s*```(?:json)?\s*|\s*```\s*$/g, ''))
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) structured = parsed as Record<string, unknown>
  } catch { /* The manager can also return a prose plan. */ }
  if (structured && Array.isArray(structured.plan) && structured.plan.every(step => typeof step === 'string')) {
    return <>
      <ol className="plan-steps">{structured.plan.map((step: string, i: number) =>
        <li key={i}><span>{String(i + 1).padStart(2, '0')}</span><p>{step.replace(/([a-z])([A-Z])/g, '$1 $2')}</p></li>)}</ol>
      {typeof structured.instruction === 'string' ? <details className="manager-output">
        <summary>Manager's next assignment{typeof structured.nextSpecialist === 'string' ? ' · ' + structured.nextSpecialist.replace(/([a-z])([A-Z])/g, '$1 $2') : ''}</summary>
        <div><PlanText text={structured.instruction} /></div>
      </details> : null}
      <details className="manager-output"><summary>Full manager output</summary><pre>{JSON.stringify(structured, null, 2)}</pre></details>
    </>
  }
  if (structured) return <>
    <StructuredPlan value={structured} />
    <details className="manager-output"><summary>Full manager output</summary><pre>{JSON.stringify(structured, null, 2)}</pre></details>
  </>
  const inline = (line: string) => line.split(/(\*\*[^*]+\*\*)/g).map((part, i) =>
    part.startsWith('**') ? <strong key={i}>{part.slice(2, -2)}</strong> : part)
  return <>{text.split('\n').map((line, i) => {
    if (!line.trim()) return null
    if (/^#{1,6}\s/.test(line)) return <h4 key={i}>{inline(line.replace(/^#{1,6}\s+/, ''))}</h4>
    if (/^\s*[-*]\s/.test(line)) return <p className="plan-bullet" key={i}>{inline(line.replace(/^\s*[-*]\s+/, ''))}</p>
    return <p key={i}>{inline(line)}</p>
  })}</>
}

function StructuredPlan({ value, depth = 0 }: { value: unknown; depth?: number }) {
  if (depth > 8) return <pre>{JSON.stringify(value, null, 2)}</pre>
  if (value === null || value === '') return null
  if (typeof value === 'string') return <p>{value.replace(/([a-z])([A-Z])/g, '$1 $2')}</p>
  if (typeof value === 'boolean' || typeof value === 'number') return <p>{String(value)}</p>
  if (Array.isArray(value)) return <div className="structured-list">{value.map((item, i) =>
    <div key={i}><span className="structured-index">{String(i + 1).padStart(2, '0')}</span><div><StructuredPlan value={item} depth={depth + 1} /></div></div>)}</div>
  if (typeof value === 'object') return <div className="structured-fields">{Object.entries(value).filter(([, item]) => item !== '' && item !== null).map(([key, item]) =>
    <section key={key}><h4>{key.replace(/([a-z])([A-Z])/g, '$1 $2').replaceAll('_', ' ')}</h4><StructuredPlan value={item} depth={depth + 1} /></section>)}</div>
  return null
}

const specialists = [
  { short: 'CA', name: 'Community & Access Analyst', label: 'Community & access', role: 'Service requests and accessible-route impacts', ids: ['complaints', 'access'] },
  { short: 'CE', name: 'Civil Assets Analyst', label: 'Civil assets', role: 'Drainage records and alignment uncertainty', ids: ['asset'] },
  { short: 'PC', name: 'Place & Constraints Advisor', label: 'Place & constraints', role: 'Site observations, heritage and survey checks', ids: ['sandstone', 'constraints', 'heritage', 'survey'] },
  { short: 'CD', name: 'Cost & Delivery Analyst', label: 'Cost & delivery', role: 'Option costs, disruption and delivery', ids: ['cost'] },
  { short: 'EV', name: 'Evidence Verifier', label: 'Evidence verification', role: 'Independent challenge of the cited claims', ids: ['verification'] },
]
export function SpecialistTeam({ run }: { run: CivicWorksRun | null }) {
  return <section className="team-panel panel">
    <div className="section-title"><div><div className="eyebrow">THE INVESTIGATION TEAM</div><h3>Five specialists. One shared objective.</h3></div></div>
    <div className="team-list">{specialists.map(agent => {
      const active = run?.phase === 'Running' && run.activeSpecialist === agent.name
      const count = run?.evidence.filter(item => agent.ids.includes(item.id)).length ?? 0
      return <div className={'team-member ' + (active ? 'active' : '')} key={agent.short}>
        <span className="agent-avatar">{agent.short}</span><div><strong>{agent.label}</strong><p>{agent.role}</p></div>
        <span className={'team-state ' + (active ? 'working' : '')}>{active ? <><i className="live-dot" />Working</> : count ? count + (count === 1 ? ' record' : ' records') : 'On standby'}</span>
      </div>
    })}</div>
  </section>
}

export function ActivityPanel({ run, connectionState, onEvidence }: { run: CivicWorksRun | null; connectionState: string; onEvidence: (id: string) => void }) {
  const [filter, setFilter] = useState('all')
  const [expanded, setExpanded] = useState(false)
  const events = (run?.activity ?? []).filter(event => filter === 'all' || event.evidenceId).slice().reverse()
  const visible = expanded ? events : events.slice(0, 16)
  const review = run?.phase === 'InitialReview' || run?.phase === 'RevisedReview'
  return <section className="activity-panel">
    <div className="activity-heading"><div><IconActivity size={19} /><h2>Live activity</h2></div><span className={'stream-state ' + (connectionState === 'connected' ? 'connected' : '')}><i />{!run ? 'Ready' : connectionState === 'connected' ? 'Connected' : connectionState === 'reconnecting' ? 'Reconnecting' : 'Disconnected'}</span></div>
    <div className="activity-now" role="status" aria-live="polite" aria-atomic="true">
      <span className="eyebrow">{!run ? 'WHEN YOU START' : review ? 'WAITING FOR YOU' : isTerminal(run) ? 'RUN FINISHED' : 'HAPPENING NOW'}</span>
      <strong>{!run ? 'The manager takes the first step.' : review ? 'Officer plan review' : isTerminal(run) ? (run.phase === 'Completed' ? 'Options brief prepared' : 'Investigation stopped') : run.activeSpecialist || 'Magentic Manager'}</strong>
      <p>{!run ? 'Watch the plan form, see specialists gather evidence, and follow the decisions that change the route.' : review ? 'Review the plan and confirm its constraints to resume. No specialist work proceeds while this checkpoint is open.' : run.statusMessage}</p>
    </div>
    <div className="activity-toolbar"><span>{run?.activity?.length ?? 0} recorded events</span><select aria-label="Filter activity" value={filter} onChange={e => setFilter(e.target.value)}><option value="all">All activity</option><option value="evidence">Evidence only</option></select></div>
    {!visible.length ? <div className="activity-empty"><span className="empty-timeline"><i /><i /><i /></span><strong>{run ? 'Waiting for events' : 'Every step will appear here'}</strong><p>Specialist assignments, tool results, plan changes and your approvals, timestamped as they happen.</p></div> :
      <ol className="event-list">{visible.map(event => <li key={event.sequence} className={'event event-' + event.kind}>
        <span className="event-dot" /><div className="event-meta"><strong>{event.actor}</strong><time dateTime={event.at}>{timeOfDay(event.at)}</time></div>
        <p>{event.message}</p>
        {event.evidenceId ? <button className="event-link" onClick={() => onEvidence(event.evidenceId!)}>Open evidence <IconArrowRight size={13} /></button> : null}
        {event.detail && event.detail !== event.message ? <details><summary>{event.kind === 'delegation' ? 'Read the assignment' : 'More detail'}<IconChevronDown size={13} /></summary><p>{event.detail}</p></details> : null}
      </li>)}</ol>}
    {events.length > 16 ? <button className="activity-more" onClick={() => setExpanded(!expanded)}>{expanded ? 'Show recent events' : 'Show all ' + events.length + ' events'}<IconChevronDown size={14} /></button> : null}
  </section>
}

export function Budget({ run }: { run: CivicWorksRun | null }) {
  const [now, setNow] = useState(() => Date.now())
  const active = !!run && !isTerminal(run)
  useEffect(() => {
    if (!active) return
    const id = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(id)
  }, [active])
  const budget = run?.budget
  const elapsed = !run ? 0 : active ? Math.max(0, Math.min(600, Math.floor((now - new Date(run.startedAt).getTime()) / 1000))) : budget?.elapsedSeconds ?? 0
  const lines = [
    { label: 'Manager rounds', used: budget?.roundsUsed ?? 0, max: budget?.maxRounds ?? 12 },
    { label: 'Read-only tool calls', used: budget?.toolCallsUsed ?? 0, max: budget?.maxToolCalls ?? 24 },
    { label: 'Plan resets', used: budget?.resetsUsed ?? 0, max: budget?.maxResets ?? 1 },
  ]
  return <section className="budget-panel" aria-label="Execution limits">
    <div className="budget-title"><h3>Execution limits</h3><IconShieldCheck size={17} /></div>
    <div className="elapsed"><IconClock size={17} /><strong>{duration(elapsed)}</strong><span>/ 10:00 elapsed</span></div>
    {lines.map(line => <div className="budget-row" key={line.label}><div><span>{line.label}</span><strong>{line.used}<em> / {line.max}</em></strong></div><progress aria-label={line.label} value={line.used} max={line.max} /></div>)}
    <p>The run stops at its limits. The elapsed-time limit includes officer review.</p>
  </section>
}

export function EvidencePanel({ evidence, selectedId, onSelect }: { evidence: EvidenceItem[]; selectedId: string | null; onSelect: (id: string) => void }) {
  const [query, setQuery] = useState('')
  const filtered = evidence.filter(item => (item.title + ' ' + item.reference + ' ' + item.specialist).toLowerCase().includes(query.toLowerCase()))
  const selected = filtered.find(item => item.id === selectedId) ?? filtered[0]
  return <section className="evidence-panel panel view-content">
    <div className="section-title"><div><div className="eyebrow">SOURCE MATERIAL</div><h3>Evidence register</h3></div><span className="document-label">{evidence.length} RECORDS</span></div>
    <p className="section-description">Read the original tool findings, where they came from and how they affect the investigation. All case records are synthetic.</p>
    <label className="evidence-search"><IconSearch size={17} /><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Search evidence, reference or specialist" aria-label="Search evidence" /></label>
    {!evidence.length ? <EmptyState icon="evidence" title="Evidence will arrive as specialists work." description="After you approve the first plan, each returned tool record will appear here with its source, reference and finding." /> : !filtered.length ? <EmptyState icon="evidence" title="No matching records." description="Try an evidence reference, title or specialist name." /> : <>
      <div className="evidence-selector">{filtered.map(item => <button key={item.id} className={selected?.id === item.id ? 'selected' : ''} onClick={() => onSelect(item.id)}>
        <span className={'evidence-symbol ' + item.status.toLowerCase()}>{item.status === 'Verified' ? <IconCheck size={13} /> : item.status === 'Conflict' ? '!' : '…'}</span>
        <span><strong>{item.title}</strong><small>{item.reference}</small></span><IconArrowRight size={15} />
      </button>)}</div>
      {selected ? <article className="evidence-inspector" key={selected.id}>
        <div className="eyebrow">SELECTED RECORD <span className={'record-status ' + selected.status.toLowerCase()}>{selected.status}</span></div>
        <h3>{selected.title}</h3><p className="evidence-finding">{selected.detail}</p>
        <dl><div><dt>REFERENCE</dt><dd>{selected.reference}</dd></div><div><dt>SOURCE</dt><dd>{selected.source}</dd></div>
          <div><dt>RETRIEVED BY</dt><dd>{selected.specialist}</dd></div><div><dt>INVESTIGATION IMPACT</dt><dd>{selected.activity}</dd></div></dl>
      </article> : null}
    </>}
  </section>
}

export function OptionsPanel({ brief, evidence, onEvidence }: { brief: PreliminaryWorksOptionsBrief | null; evidence: EvidenceItem[]; onEvidence: (id: string) => void }) {
  if (!brief) return <section className="panel view-content"><EmptyState icon="brief" title="The brief follows the evidence." description="The manager will compare options A, B and C after the specialist checks and the independent evidence challenge. Its recommendation and supporting claims will appear here." /></section>
  return <section className="options-panel view-content">
    <div className="recommendation"><div className="eyebrow"><IconShieldCheck size={17} /> PRELIMINARY WORKS OPTIONS BRIEF</div>
      <span className="recommendation-letter">{brief.recommendation}</span><h2>Option {brief.recommendation} is the supported next step.</h2><p>{brief.summary}</p></div>
    <section className="options-comparison panel"><div className="section-title"><h3>The three options</h3><span className="document-label">EVIDENCE-LED COMPARISON</span></div>
      {brief.options.map(option => <article key={option.code} className={'option-row ' + (option.code === brief.recommendation ? 'recommended' : '')}>
        <span className="option-code">{option.code}</span><div><div className="option-title"><h3>{option.title}</h3>{option.code === brief.recommendation ? <span>Recommended</span> : null}</div>
          <p>{option.assessment}</p><small>{option.supportStatus}</small></div>
      </article>)}
    </section>
    <section className="open-matters panel"><div className="eyebrow">WHAT REMAINS UNRESOLVED</div><h3>Questions for the next investigation</h3>
      <ol>{brief.openMatters.map(matter => <li key={matter}>{matter}</li>)}</ol>
      <p className="quiet-note">These are open matters, even with a supported recommendation. This brief does not approve works.</p>
    </section>
    <section className="claims-panel panel"><div className="section-title"><div><div className="eyebrow">TRACE THE RECOMMENDATION</div><h3>{brief.claims.length} claims with evidence</h3></div></div>
      {brief.claims.map((claim, index) => <div className="claim" key={index}><span>{String(index + 1).padStart(2, '0')}</span><div><p>{claim.claim}</p><div className="claim-references">{claim.evidenceReferences.map(reference => {
        const record = evidence.find(item => item.reference.split(/\s*·\s*|,\s*/).includes(reference))
        return record ? <button key={reference} onClick={() => onEvidence(record.id)}>{reference}<IconArrowRight size={12} /></button> : <span key={reference}>{reference}</span>
      })}</div></div></div>)}
    </section>
  </section>
}

function EmptyState({ icon, title, description }: { icon: string; title: string; description: string }) {
  return <div className="empty-state">{icon === 'brief' ? <IconFileDescription size={32} stroke={1.3} /> : <IconStack2 size={32} stroke={1.3} />}<h3>{title}</h3><p>{description}</p></div>
}
