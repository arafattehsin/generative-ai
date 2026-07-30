import {
  AlertTriangle,
  ArrowLeft,
  ArrowRight,
  Check,
  ChevronRight,
  Clock3,
  FileText,
  Headphones,
  Plane,
  RefreshCw,
  Route,
  Send,
  ShieldCheck,
  UserRound,
} from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from './api'
import type { ConversationMessage, TravelRun } from './types'

const operatorName = 'Alex Morgan'

export default function OperationsApp() {
  const [cases, setCases] = useState<TravelRun[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [response, setResponse] = useState('')
  const [nextOwnerId, setNextOwnerId] = useState('insurance')
  const [isLoading, setIsLoading] = useState(true)
  const [isWorking, setIsWorking] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const selectedCase = useMemo(
    () => cases.find((item) => item.id === selectedId) ?? cases[0] ?? null,
    [cases, selectedId],
  )

  const loadCases = useCallback(async (quiet = false) => {
    if (!quiet) setIsLoading(true)
    try {
      const latest = await api.operationsCases()
      setCases(latest)
      setSelectedId((current) => latest.some((item) => item.id === current) ? current : latest[0]?.id ?? null)
      setError(null)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Unable to load the recovery queue.')
    } finally {
      if (!quiet) setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadCases()
    const timer = window.setInterval(() => void loadCases(true), 1600)
    return () => window.clearInterval(timer)
  }, [loadCases])

  useEffect(() => {
    if (!selectedCase) return
    setResponse('')
    setNextOwnerId('insurance')
  }, [selectedCase?.id])

  async function claimCase() {
    if (!selectedCase) return
    setIsWorking(true)
    setError(null)
    try {
      await api.claimCase(selectedCase.id, operatorName)
      await loadCases(true)
    } catch (claimError) {
      setError(claimError instanceof Error ? claimError.message : 'Unable to claim this case.')
    } finally {
      setIsWorking(false)
    }
  }

  async function resolveCase() {
    if (!selectedCase || !response.trim()) return
    setIsWorking(true)
    setError(null)
    try {
      await api.resolveCase(selectedCase.id, operatorName, response.trim(), nextOwnerId)
      await loadCases(true)
    } catch (resolveError) {
      setError(resolveError instanceof Error ? resolveError.message : 'Unable to send the recovery decision.')
    } finally {
      setIsWorking(false)
    }
  }

  return (
    <div className="ops-shell">
      <header className="ops-header">
        <div className="product-lockup">
          <span className="product-mark"><Headphones size={20} /></span>
          <div><h1>Recovery Operations</h1><p>Human decision workspace</p></div>
        </div>
        <div className="ops-header-actions">
          <div className="operator-presence"><span className="operator-avatar small">AM</span><span><strong>{operatorName}</strong><small>On duty</small></span></div>
          <a className="text-link" href="/" target="_blank" rel="noreferrer"><ArrowLeft size={16} /> Traveller view</a>
        </div>
      </header>

      <main className="ops-workspace">
        {error ? <div className="error-strip" role="alert"><AlertTriangle size={18} /><span>{error}</span></div> : null}

        <div className="ops-title-row">
          <div><span className="eyebrow">Live recovery desk</span><h2>Cases needing a person</h2><p>Time-critical decisions move here with the complete traveller context.</p></div>
          <button className="icon-button" onClick={() => void loadCases()} aria-label="Refresh queue" title="Refresh queue"><RefreshCw size={18} /></button>
        </div>

        <section className={`ops-layout ${!isLoading && cases.length === 0 ? 'empty' : ''}`}>
          <aside className="case-queue" aria-label="Human support queue">
            <div className="queue-heading"><span>Priority queue</span><strong>{cases.length}</strong></div>
            {isLoading ? <QueueSkeleton /> : cases.length ? cases.map((item) => (
              <QueueItem key={item.id} run={item} selected={item.id === selectedCase?.id} onSelect={() => setSelectedId(item.id)} />
            )) : <QueueEmpty />}
          </aside>

          <div className="ops-case-stage">
            {selectedCase ? (
              <CaseWorkspace
                run={selectedCase}
                response={response}
                nextOwnerId={nextOwnerId}
                isWorking={isWorking}
                onResponse={setResponse}
                onNextOwner={setNextOwnerId}
                onClaim={() => void claimCase()}
                onResolve={() => void resolveCase()}
              />
            ) : <NoSelectedCase />}
          </div>
        </section>
      </main>
    </div>
  )
}

function QueueItem({ run, selected, onSelect }: { run: TravelRun; selected: boolean; onSelect: () => void }) {
  const route = parseRoute(run.tripCode)
  return (
    <button className={`queue-item ${selected ? 'selected' : ''}`} onClick={onSelect}>
      <span className="queue-priority"><i />{run.urgency}</span>
      <strong>{route.from} <ArrowRight size={13} /> {route.to}</strong>
      <span>{run.travellerName}</span>
      <small><Clock3 size={13} /> Waiting {relativeTime(run.humanSupport?.requestedAt)}</small>
      <ChevronRight className="queue-chevron" size={17} />
    </button>
  )
}

function CaseWorkspace(props: {
  run: TravelRun
  response: string
  nextOwnerId: string
  isWorking: boolean
  onResponse: (value: string) => void
  onNextOwner: (value: string) => void
  onClaim: () => void
  onResolve: () => void
}) {
  const route = parseRoute(props.run.tripCode)
  const support = props.run.humanSupport
  const isClaimed = support?.status === 'Claimed'

  return (
    <article className="ops-case">
      <header className="ops-case-header">
        <div className="ops-route"><strong>{route.from}</strong><span><Plane size={16} /><i /></span><strong>{route.to}</strong></div>
        <div className="ops-case-heading"><span className="critical-status"><AlertTriangle size={14} /> {props.run.urgency}</span><h2>{props.run.travellerName}</h2><p>{props.run.tripCode}</p></div>
        <div className="claim-state"><span>{isClaimed ? 'Owned by' : 'Awaiting owner'}</span><strong>{support?.assignedTo ?? 'Recovery Operations'}</strong></div>
      </header>

      <section className="decision-brief" aria-label="Escalation brief">
        <BriefItem label="Escalated by" value={support?.requestedByAgentName ?? 'Flight Recovery'} />
        <BriefItem label="Why a person is needed" value={support?.reason ?? 'Manual recovery decision required.'} wide />
        <BriefItem label="Decision" value={support?.decisionNeeded ?? 'Confirm the safest recovery path.'} wide />
      </section>

      <div className="ops-case-body">
        <section className="ops-context">
          <div className="panel-heading"><div><span className="eyebrow">Carried context</span><h3>Conversation</h3></div><span>{props.run.messages.length} messages</span></div>
          <div className="ops-transcript">
            {props.run.messages.slice(-6).map((message) => <OpsMessage key={message.id} message={message} travellerName={props.run.travellerName} />)}
          </div>
          <div className="recommendation-strip"><ShieldCheck size={18} /><span><small>Agent recommendation</small><strong>{support?.recommendation}</strong></span></div>
        </section>

        <section className="human-decision">
          <div className="panel-heading"><div><span className="eyebrow">Human response</span><h3>Decision for Maya</h3></div><span className="operator-avatar">AM</span></div>

          {!isClaimed ? (
            <div className="claim-prompt">
              <span className="operator-avatar large">AM</span>
              <h4>Take ownership of this decision</h4>
              <p>Maya will see that a named recovery specialist is reviewing her case.</p>
              <button className="primary-action" disabled={props.isWorking} onClick={props.onClaim}><UserRound size={17} /> {props.isWorking ? 'Taking ownership...' : 'Take ownership'}</button>
            </div>
          ) : (
            <div className="decision-form">
              <label htmlFor="operator-response"><span>Response to Maya</span><textarea id="operator-response" value={props.response} placeholder="Write your decision for Maya..." onChange={(event) => props.onResponse(event.currentTarget.value)} /></label>
              <fieldset>
                <legend>Return ownership to</legend>
                <div className="owner-options">
                  <OwnerOption id="insurance" label="Travel Cover" selected={props.nextOwnerId === 'insurance'} onSelect={props.onNextOwner} />
                  <OwnerOption id="flight" label="Flight Recovery" selected={props.nextOwnerId === 'flight'} onSelect={props.onNextOwner} />
                  <OwnerOption id="triage" label="Journey Triage" selected={props.nextOwnerId === 'triage'} onSelect={props.onNextOwner} />
                </div>
              </fieldset>
              <div className="decision-note"><Check size={15} /><span>The human response will appear in Maya's conversation before ownership returns to the selected AI specialist.</span></div>
              <button className="primary-action send-decision" disabled={props.isWorking || !props.response.trim()} onClick={props.onResolve}><Send size={17} /> {props.isWorking ? 'Sending decision...' : 'Send decision to Maya'}</button>
            </div>
          )}
        </section>
      </div>
    </article>
  )
}

function BriefItem({ label, value, wide }: { label: string; value: string; wide?: boolean }) {
  return <div className={wide ? 'wide' : ''}><span>{label}</span><strong>{value}</strong></div>
}

function OpsMessage({ message, travellerName }: { message: ConversationMessage; travellerName: string }) {
  const isTraveller = message.authorType === 'User'
  const isHuman = message.authorType === 'Human'
  return (
    <div className={`ops-message ${isTraveller ? 'traveller' : ''} ${isHuman ? 'human' : ''}`}>
      <span className="ops-message-avatar">{initials(message.authorName)}</span>
      <div><div><strong>{message.authorName}</strong><small>{isTraveller ? 'Traveller' : isHuman ? 'Recovery specialist' : 'AI specialist'} · {formatTime(message.createdAt)}</small></div><p>{message.text}</p></div>
      <span className="sr-only">Message regarding {travellerName}</span>
    </div>
  )
}

function OwnerOption({ id, label, selected, onSelect }: { id: string; label: string; selected: boolean; onSelect: (id: string) => void }) {
  return <button type="button" className={selected ? 'selected' : ''} onClick={() => onSelect(id)}><span>{label}</span>{selected ? <Check size={15} /> : null}</button>
}

function QueueSkeleton() {
  return <div className="queue-skeleton" aria-label="Loading queue"><i /><i /><i /></div>
}

function QueueEmpty() {
  return <div className="queue-empty"><span><Check size={20} /></span><strong>No cases waiting</strong><p>New human escalations will appear here with their conversation context.</p></div>
}

function NoSelectedCase() {
  return <div className="no-selected-case"><span><FileText size={24} /></span><h2>The recovery desk is clear</h2><p>Alex is ready when an AI specialist requests a human decision.</p><a className="primary-action" href="/"><Route size={17} /> Open traveller experience</a></div>
}

function parseRoute(tripCode: string) {
  const routeCodes = tripCode.split('-').filter((part) => /^[A-Za-z]{3}$/.test(part))
  return { from: routeCodes[0] || 'SYD', to: routeCodes.at(-1) || 'LAX' }
}

function initials(name: string) {
  return name.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}

function relativeTime(value?: string) {
  if (!value) return 'just now'
  const minutes = Math.max(0, Math.floor((Date.now() - new Date(value).getTime()) / 60000))
  if (minutes < 1) return 'less than a minute'
  return `${minutes} min`
}
