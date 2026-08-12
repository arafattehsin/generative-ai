import {
  Activity,
  AlertTriangle,
  ArrowRight,
  Bed,
  Check,
  CircleStop,
  CornerDownRight,
  FileText,
  Headphones,
  HeartPulse,
  ListChecks,
  Luggage,
  Plane,
  Play,
  Plus,
  ReceiptText,
  Route,
  Send,
  ShieldCheck,
  Star,
  UserRound,
  UsersRound,
} from 'lucide-react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { CSSProperties, KeyboardEvent } from 'react'
import { api } from './api'
import type { AgentDefinition, ConversationMessage, SampleScenario, TravelRun } from './types'
import { useRunStream } from './useRunStream'

const iconMap = {
  route: Route,
  plane: Plane,
  bed: Bed,
  shield: ShieldCheck,
  star: Star,
  user: UsersRound,
}

type RichContentBlock =
  | { kind: 'paragraph'; text: string }
  | { kind: 'list'; items: string[]; ordered: boolean; start: number }

type RichSection = {
  title?: string
  blocks: RichContentBlock[]
}

const activeRunStorageKey = 'travel-concierge-active-run'

export default function App() {
  const [samples, setSamples] = useState<SampleScenario[]>([])
  const [activeRun, setActiveRun] = useState<TravelRun | null>(null)
  const [selectedSampleId, setSelectedSampleId] = useState('')
  const [message, setMessage] = useState('')
  const [travellerName, setTravellerName] = useState('')
  const [tripCode, setTripCode] = useState('')
  const [urgency, setUrgency] = useState('High')
  const [followUp, setFollowUp] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isStarting, setIsStarting] = useState(false)
  const [isIntakeOpen, setIsIntakeOpen] = useState(false)

  const activeRunId = activeRun?.id ?? null
  const activeOwner = useMemo(
    () => activeRun?.agents.find((agent) => agent.id === activeRun.currentOwnerId),
    [activeRun],
  )
  const selectedRoute = useMemo(() => parseRoute(activeRun?.tripCode ?? tripCode), [activeRun, tripCode])
  const selectedSample = useMemo(
    () => samples.find((sample) => sample.id === selectedSampleId),
    [samples, selectedSampleId],
  )

  const applySample = useCallback((sample: SampleScenario) => {
    setSelectedSampleId(sample.id)
    setMessage(sample.message)
    setTravellerName(sample.travellerName)
    setTripCode(sample.tripCode)
    setUrgency(sample.urgency)
  }, [])

  const refreshRuns = useCallback(async () => {
    const latestRuns = await api.runs()
    setActiveRun((current) => {
      if (!current) return null
      return latestRuns.find((run) => run.id === current.id) ?? null
    })
  }, [])

  useEffect(() => {
    async function load() {
      try {
        const sampleResponse = await api.samples()
        setSamples(sampleResponse)
        if (sampleResponse[0]) applySample(sampleResponse[0])

        const storedRunId = window.sessionStorage.getItem(activeRunStorageKey)
        if (storedRunId) {
          try {
            const storedRun = await api.run(storedRunId)
            setActiveRun(storedRun)
            setIsIntakeOpen(false)
            return
          } catch {
            window.sessionStorage.removeItem(activeRunStorageKey)
          }
        }

        setActiveRun(null)
        setIsIntakeOpen(true)
      } catch (loadError) {
        setError(loadError instanceof Error ? loadError.message : 'Unable to load the sample API.')
      }
    }

    void load()
  }, [applySample])

  useEffect(() => {
    if (!activeRunId) return
    const timer = window.setInterval(() => void refreshRuns().catch(() => undefined), 1800)
    return () => window.clearInterval(timer)
  }, [activeRunId, refreshRuns])

  const handleRunStream = useCallback((run: TravelRun) => {
    setActiveRun(run)
  }, [])

  useRunStream(activeRunId, handleRunStream)

  async function startRun() {
    setError(null)
    setIsStarting(true)
    try {
      const response = await api.createRun({ message, travellerName, tripCode, urgency })
      const run = await api.run(response.runId)
      window.sessionStorage.setItem(activeRunStorageKey, run.id)
      setActiveRun(run)
      setIsIntakeOpen(false)
      await refreshRuns()
    } catch (startError) {
      setError(startError instanceof Error ? startError.message : 'Unable to start run.')
    } finally {
      setIsStarting(false)
    }
  }

  async function sendFollowUp() {
    if (!activeRun || !followUp.trim()) return
    const text = followUp.trim()
    setFollowUp('')
    setError(null)
    try {
      await api.sendMessage(activeRun.id, text)
      await refreshRuns()
    } catch (sendError) {
      setFollowUp(text)
      setError(sendError instanceof Error ? sendError.message : 'Unable to send the follow-up.')
    }
  }

  async function cancelRun() {
    if (!activeRun) return
    setError(null)
    try {
      await api.cancel(activeRun.id)
      await refreshRuns()
    } catch (cancelError) {
      setError(cancelError instanceof Error ? cancelError.message : 'Unable to stop the run.')
    }
  }

  async function startNewChat() {
    setError(null)
    try {
      const sampleResponse = await api.samples()
      setSamples(sampleResponse)
      const refreshedSample = sampleResponse.find((sample) => sample.id === selectedSampleId) ?? sampleResponse[0]
      if (refreshedSample) applySample(refreshedSample)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Unable to reload the sample API.')
    }

    setActiveRun(null)
    window.sessionStorage.removeItem(activeRunStorageKey)
    setFollowUp('')
    setIsIntakeOpen(true)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="product-lockup">
          <span className="product-mark"><Route size={20} /></span>
          <div>
            <h1>Live Recovery</h1>
            <p>Travel support when plans change</p>
          </div>
        </div>

        <div className="header-actions">
          <div className="support-status"><Headphones size={17} /><span>Travel support</span></div>
          {activeRun ? (
            <button className="new-case-button" onClick={startNewChat}>
              <Plus size={17} />
              New chat
            </button>
          ) : null}
        </div>
      </header>

      <main className="workspace">
        {error ? <div className="error-strip" role="alert"><AlertTriangle size={18} /><span>{error}</span></div> : null}

        <CaseBar
          route={selectedRoute}
          run={activeRun}
          travellerName={travellerName}
          tripCode={tripCode}
          activeOwner={activeOwner}
          scenarioTitle={selectedSample?.title}
        />

        <section className={`recovery-layout ${isIntakeOpen ? 'intake-open' : ''}`}>
          <ConversationPanel
            run={activeRun}
            activeOwner={activeOwner}
            followUp={followUp}
            onFollowUp={setFollowUp}
            onSend={() => void sendFollowUp()}
            onCancel={() => void cancelRun()}
          />

          <aside className="operations-rail">
            {isIntakeOpen ? (
              <IntakePanel
                samples={samples}
                selectedSampleId={selectedSampleId}
                message={message}
                isStarting={isStarting}
                onSample={(id) => {
                  const sample = samples.find((item) => item.id === id)
                  if (sample) applySample(sample)
                }}
                onMessage={setMessage}
                onStart={() => void startRun()}
              />
            ) : (
              <>
                <OwnerPanel run={activeRun} activeOwner={activeOwner} />
                <HandoffHistory run={activeRun} />
                <CaseBrief run={activeRun} />
              </>
            )}
          </aside>
        </section>

        <footer className="system-footer">
          <span><ShieldCheck size={15} /> Secure support chat</span>
          <span>Your conversation follows you when a specialist takes over.</span>
          <span>No booking is made without your confirmation.</span>
        </footer>
      </main>
    </div>
  )
}

function CaseBar(props: {
  route: ReturnType<typeof parseRoute>
  run: TravelRun | null
  travellerName: string
  tripCode: string
  activeOwner?: AgentDefinition
  scenarioTitle?: string
}) {
  const OwnerIcon = getAgentIcon(props.activeOwner)
  const disruption = customerDisruptionLabel(props.scenarioTitle)

  return (
    <section className="case-bar" aria-label="Active travel case">
      <div className="route-summary">
        <div className="airport-code"><strong>{props.route.from}</strong><span>{props.route.fromName}</span></div>
        <div className="route-line"><Plane size={17} /><i /></div>
        <div className="airport-code"><strong>{props.route.to}</strong><span>{props.route.toName}</span></div>
      </div>

      <div className="case-status">
        <span className="critical-status"><AlertTriangle size={15} /> {disruption}</span>
        <p>{customerRunSummary(props.run)}</p>
      </div>

      <div className={`case-identity ${props.run ? '' : 'before-chat'}`}>
        <div className="identity-item">
          <span className="identity-icon person"><UserRound size={18} /></span>
          <span><small>Traveller</small><strong>{props.run?.travellerName || props.travellerName || 'Not set'}</strong></span>
        </div>
        <div className="identity-item case-reference">
          <span className="identity-icon"><FileText size={18} /></span>
          <span><small>Case reference</small><strong>{props.run?.tripCode || props.tripCode || 'Not set'}</strong></span>
        </div>
        {props.run ? (
          <>
            <div className="identity-item">
              <span className="identity-icon"><OwnerIcon size={18} /></span>
              <span><small>Helping now</small><strong>{props.run.currentOwnerName || props.activeOwner?.name || 'Journey Triage'}</strong></span>
            </div>
            <span className={`run-status ${runStatusClass(props.run.status)}`}>{customerRunStatus(props.run.status)}</span>
          </>
        ) : null}
      </div>
    </section>
  )
}

function IntakePanel(props: {
  samples: SampleScenario[]
  selectedSampleId: string
  message: string
  isStarting: boolean
  onSample: (id: string) => void
  onMessage: (value: string) => void
  onStart: () => void
}) {
  const sample = props.samples.find((item) => item.id === props.selectedSampleId)
  const route = parseRoute(sample?.tripCode ?? '')
  const priorities = sample ? customerPriorityTags(sample) : []

  return (
    <section className="intake-panel">
      <SectionHeading icon={<Headphones size={18} />} title={`Hi ${firstName(sample?.travellerName)}, how can we help?`} />
      <p className="section-intro">We have your disrupted journey. Review what matters and send your opening message.</p>

      <label className="field-group" htmlFor="sample">
        <span>Affected journey</span>
        <select id="sample" value={props.selectedSampleId} onChange={(event) => props.onSample(event.currentTarget.value)}>
          {props.samples.map((item) => {
            const itemRoute = parseRoute(item.tripCode)
            return <option key={item.id} value={item.id}>{itemRoute.from} to {itemRoute.to} - {item.title}</option>
          })}
        </select>
      </label>

      <div className="customer-trip-summary">
        <div><UserRound size={17} /><span><small>Traveller</small><strong>{sample?.travellerName ?? 'Traveller'}</strong></span></div>
        <div><Route size={17} /><span><small>Journey</small><strong>{route.from} to {route.to}</strong></span></div>
        <div><FileText size={17} /><span><small>Reference</small><strong>{sample?.tripCode ?? 'Not set'}</strong></span></div>
      </div>

      <div className="priority-section">
        <span className="field-caption">What matters most</span>
        <div className="priority-list">
          {priorities.map((tag) => <CustomerPriority key={tag} tag={tag} />)}
        </div>
      </div>

      <label className="field-group" htmlFor="request">
        <span>Tell us what you need</span>
        <textarea id="request" value={props.message} onChange={(event) => props.onMessage(event.currentTarget.value)} />
        <small className="field-helper">This will be the first message in your support chat.</small>
      </label>

      <div className="support-note"><ShieldCheck size={17} /><span>Your trip details stay with the support team throughout this chat.</span></div>
      <button className="primary-action" disabled={props.isStarting || !props.message.trim()} onClick={props.onStart}>
        <Play size={17} /> {props.isStarting ? 'Connecting you...' : 'Start chat'}
      </button>
    </section>
  )
}

function CustomerPriority({ tag }: { tag: string }) {
  const { label, Icon } = customerPriority(tag)
  return <div className="priority-item"><Icon size={16} /><span>{label}</span></div>
}

function ConversationPanel(props: {
  run: TravelRun | null
  activeOwner?: AgentDefinition
  followUp: string
  onFollowUp: (value: string) => void
  onSend: () => void
  onCancel: () => void
}) {
  const messageListRef = useRef<HTMLDivElement>(null)
  const normalizedStatus = normalizeRunStatus(props.run?.status)
  const isRunning = normalizedStatus === 'Running' || normalizedStatus === 'Queued'
  const isWaitingForHuman = normalizedStatus === 'WaitingForHuman' || normalizedStatus === 'HumanResponding'
  const isReplyLocked = isRunning || isWaitingForHuman
  const OwnerIcon = getAgentIcon(props.activeOwner)
  const messages = props.run?.messages ?? []
  const lastTravellerMessageIndex = messages.map((item) => item.authorType === 'User').lastIndexOf(true)
  const visibleMessages = isRunning
    ? messages.filter((_, index) => index <= lastTravellerMessageIndex)
    : messages
  const lastTravellerMessageId = messages[lastTravellerMessageIndex]?.id

  useEffect(() => {
    const messageList = messageListRef.current
    if (!messageList || !lastTravellerMessageId) return

    const message = messageList.querySelector<HTMLElement>(`[data-message-id="${lastTravellerMessageId}"]`)
    if (message) {
      const messageTop = message.getBoundingClientRect().top - messageList.getBoundingClientRect().top + messageList.scrollTop
      messageList.scrollTo({ top: Math.max(0, messageTop - 18) })
    }
  }, [lastTravellerMessageId])

  function handleReplyKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing && props.followUp.trim()) {
      event.preventDefault()
      props.onSend()
    }
  }

  return (
    <section className={`conversation-panel ${props.run ? '' : 'before-chat'}`}>
      <div className="conversation-header">
        <div>
          <span className="conversation-kicker">Recovery conversation</span>
          <h2>{props.run ? `${props.run.travellerName}'s journey` : 'Traveller chat'}</h2>
        </div>
        {props.run ? (
          <div className="owner-chip">
            <span><OwnerIcon size={17} /></span>
            <div><small>Helping now</small><strong>{props.run.currentOwnerName || props.activeOwner?.name || 'Journey Triage'}</strong></div>
          </div>
        ) : null}
      </div>

      <div className="message-list" ref={messageListRef}>
        {visibleMessages.map((conversationMessage) => (
          <MessageBubble
            key={conversationMessage.id}
            message={conversationMessage}
            agents={props.run?.agents ?? []}
            travellerName={props.run?.travellerName ?? 'Traveller'}
          />
        ))}
        {isRunning ? <ThinkingMessage agent={props.activeOwner} travellerName={props.run?.travellerName ?? 'Traveller'} /> : null}
        {isWaitingForHuman && props.run ? <HumanReviewStatus run={props.run} /> : null}
      </div>

      {props.run ? (
        <div className="reply-dock">
          <div className="reply-recipient"><CornerDownRight size={15} /> Reply to <strong>{props.run.currentOwnerName || props.activeOwner?.name || 'current owner'}</strong></div>
          <textarea
            value={props.followUp}
            disabled={isReplyLocked}
            placeholder={isWaitingForHuman ? 'Your recovery specialist is reviewing the case...' : isRunning ? `${props.activeOwner?.name ?? 'The current owner'} is preparing a response...` : 'Reply or provide the missing detail...'}
            onChange={(event) => props.onFollowUp(event.currentTarget.value)}
            onKeyDown={handleReplyKeyDown}
          />
          <div className="reply-actions">
            <button className="stop-button" disabled={normalizedStatus === 'Cancelled'} onClick={props.onCancel} aria-label="End chat" title="End chat">
              <CircleStop size={18} />
            </button>
            <button className="send-button" disabled={isReplyLocked || !props.followUp.trim()} onClick={props.onSend} aria-label="Send reply" title="Send reply">
              <Send size={18} />
            </button>
          </div>
        </div>
      ) : null}
    </section>
  )
}

function MessageBubble(props: {
  message: ConversationMessage
  agents: AgentDefinition[]
  travellerName: string
}) {
  const agent = props.agents.find((item) => item.id === props.message.agentId)
  const isUser = props.message.authorType === 'User'
  const isSystem = props.message.authorType === 'System'
  const isHuman = props.message.authorType === 'Human'
  const Icon = isUser ? UserRound : isSystem ? Activity : getAgentIcon(agent)
  const displayName = isSystem ? 'Live Recovery' : props.message.authorName
  const role = isUser ? 'You' : isSystem ? 'Support update' : isHuman ? 'Recovery specialist' : customerAgentRole(agent)
  const recipient = isUser ? 'Support team' : props.travellerName
  const messageText = isSystem ? friendlySystemMessage(props.message.text) : props.message.text

  return (
    <article data-message-id={props.message.id} className={`message-row ${isUser ? 'traveller' : ''} ${isSystem ? 'system' : isHuman ? 'human' : 'agent'}`}>
      <div className="message-avatar" style={{ '--agent-color': agent?.accent } as CSSProperties}>
        {isUser || isHuman ? <strong>{initials(props.message.authorName)}</strong> : <Icon size={19} />}
      </div>
      <div className="message-shell">
        <div className="message-meta">
          <div><strong>{displayName}</strong><span>{role}</span></div>
          <div><CornerDownRight size={13} /><span>To {recipient}</span><time>{formatTime(props.message.createdAt)}</time></div>
        </div>
        <div className="message-content">
          {isSystem || isUser ? <PlainMessage text={messageText} /> : <RichAgentMessage text={messageText} />}
        </div>
      </div>
    </article>
  )
}

function HumanReviewStatus({ run }: { run: TravelRun }) {
  const assignedTo = run.humanSupport?.assignedTo
  return (
    <div className="human-review-status" role="status">
      <span className="human-review-avatar">{assignedTo ? initials(assignedTo) : <Headphones size={20} />}</span>
      <div>
        <small>{assignedTo ? 'Recovery specialist joined' : 'Connecting you to a person'}</small>
        <strong>{assignedTo ?? 'Recovery Operations'}</strong>
        <p>{assignedTo ? 'Reviewing the time-critical recovery decision with your full journey context.' : 'Your conversation is paused while the recovery desk accepts the case.'}</p>
      </div>
      <span className="review-live"><i /> Live review</span>
    </div>
  )
}

function PlainMessage({ text }: { text: string }) {
  return <p className="plain-message">{text || '...'}</p>
}

function RichAgentMessage({ text }: { text: string }) {
  if (!text.trim()) return <div className="streaming-lines"><i /><i /><i /></div>
  const sections = parseRichMessage(text)

  return (
    <div className="rich-response">
      {sections.map((section, sectionIndex) => {
        const hasList = section.blocks.some((block) => block.kind === 'list')
        const isEvidenceCard = Boolean(section.title && /evidence|claim/i.test(section.title))
        const CardIcon = isEvidenceCard ? ReceiptText : ListChecks
        const ItemIcon = isEvidenceCard ? FileText : ArrowRight
        if (!section.title && !hasList) {
          return section.blocks.map((block, blockIndex) => block.kind === 'paragraph'
            ? <p key={`paragraph-${sectionIndex}-${blockIndex}`}>{block.text}</p>
            : null)
        }

        return (
          <section className={`response-card ${section.title ? '' : 'response-card-untitled'}`} key={`section-${sectionIndex}`}>
            {section.title ? <div className="response-card-title"><CardIcon size={17} /><strong>{section.title}</strong></div> : null}
            <div className="response-card-body">
              {section.blocks.map((block, blockIndex) => block.kind === 'paragraph' ? (
                <p key={`paragraph-${blockIndex}`}>{block.text}</p>
              ) : block.ordered ? (
                <ol key={`list-${blockIndex}`} start={block.start}>
                  {block.items.map((item, itemIndex) => <li key={`${item}-${itemIndex}`}><span>{block.start + itemIndex}</span><p>{item}</p></li>)}
                </ol>
              ) : (
                <ul key={`list-${blockIndex}`}>{block.items.map((item, itemIndex) => <li key={`${item}-${itemIndex}`}><ItemIcon size={15} /><p>{item}</p></li>)}</ul>
              ))}
            </div>
          </section>
        )
      })}
    </div>
  )
}

function ThinkingMessage({ agent, travellerName }: { agent?: AgentDefinition; travellerName: string }) {
  const Icon = getAgentIcon(agent)
  return (
    <article className="message-row agent thinking-message">
      <div className="message-avatar"><Icon size={19} /></div>
      <div className="message-shell">
        <div className="message-meta">
          <div><strong>{agent?.name ?? 'Journey Triage'}</strong><span>{customerAgentRole(agent)}</span></div>
          <div><CornerDownRight size={13} /><span>To {travellerName}</span></div>
        </div>
        <div className="message-content"><div className="streaming-lines"><i /><i /><i /></div></div>
      </div>
    </article>
  )
}

function OwnerPanel({ run, activeOwner }: { run: TravelRun | null; activeOwner?: AgentDefinition }) {
  const Icon = getAgentIcon(activeOwner)
  const isHuman = activeOwner?.id === 'human'
  const ownerName = run?.currentOwnerName || activeOwner?.name || 'Journey Triage'
  return (
    <section className="rail-section owner-section">
      <SectionHeading icon={<Headphones size={18} />} title="Helping you now" />
      <div className="owner-profile">
        <span className={`owner-avatar ${isHuman ? 'human' : ''}`}>{isHuman ? initials(ownerName) : <Icon size={22} />}</span>
        <div><small>{run ? customerRunStatus(run.status) : 'Ready'}</small><strong>{ownerName}</strong></div>
      </div>
      <p>{customerAgentDescription(activeOwner)}</p>
      <div className="ownership-rule"><ArrowRight size={15} /><span>Your conversation and trip details move with you when support changes.</span></div>
    </section>
  )
}

function HandoffHistory({ run }: { run: TravelRun | null }) {
  const agents = run?.agents ?? []
  const firstAgent = agents.find((agent) => agent.id === (run?.timeline[0]?.fromAgentId ?? 'triage'))
  const events = run?.timeline ?? []
  const specialistCount = events.length + 1

  return (
    <section className="rail-section handoff-section">
      <SectionHeading icon={<UsersRound size={18} />} title="Who has helped" action={`${specialistCount} specialist${specialistCount === 1 ? '' : 's'}`} />
      <div className="handoff-timeline">
        <TimelineNode agent={firstAgent} name={firstAgent?.name ?? 'Journey Triage'} detail="Received your request" active={events.length === 0} />
        {events.map((event, index) => {
          const agent = agents.find((item) => item.id === event.toAgentId)
          return (
            <TimelineNode
              key={event.id}
              agent={agent}
              name={event.toAgentId === 'human' && run?.humanSupport?.assignedTo ? run.humanSupport.assignedTo : event.toAgentName}
              detail="Took over this part of your journey"
              time={formatTime(event.createdAt)}
              active={index === events.length - 1}
            />
          )
        })}
      </div>
      {!run ? <p className="rail-empty">Start a chat to connect with the right travel specialist.</p> : null}
    </section>
  )
}

function TimelineNode(props: {
  agent?: AgentDefinition
  name: string
  detail: string
  time?: string
  active?: boolean
}) {
  const Icon = getAgentIcon(props.agent)
  return (
    <div className={`timeline-node ${props.active ? 'active' : ''}`}>
      <span><Icon size={16} /></span>
      <div><strong>{props.name}</strong><p>{props.detail}</p></div>
      {props.time ? <time>{props.time}</time> : null}
    </div>
  )
}

function CaseBrief({ run }: { run: TravelRun | null }) {
  return (
    <section className="rail-section case-brief">
      <SectionHeading icon={<FileText size={18} />} title="Your request" />
      {run ? (
        <>
          <blockquote>{run.initialRequest}</blockquote>
          <dl>
            <div><dt>Traveller</dt><dd>{run.travellerName}</dd></div>
            <div><dt>Trip</dt><dd>{run.tripCode}</dd></div>
            <div><dt>Support level</dt><dd>{run.urgency === 'Critical' ? 'Priority' : 'Standard'}</dd></div>
          </dl>
        </>
      ) : <p className="rail-empty">The original request stays visible here while specialists take ownership.</p>}
    </section>
  )
}

function SectionHeading({ icon, title, action }: { icon: React.ReactNode; title: string; action?: string }) {
  return (
    <div className="section-heading">
      <div>{icon}<h2>{title}</h2></div>
      {action ? <span>{action}</span> : null}
    </div>
  )
}

function parseRichMessage(text: string): RichSection[] {
  const normalized = text
    .replace(/\r/g, '')
    .replace(/^\s*\*\*([^*\n]+)\*\*\s*(?=\S)/gm, '**$1**\n')
    .replace(/([^\n])\s+(\d{1,2}[.)])\s+/g, '$1\n$2 ')
  const lines = normalized.split('\n').map((line) => line.trim())
  const sections: RichSection[] = []
  let section: RichSection = { blocks: [] }
  let paragraph: string[] = []
  let listItems: string[] = []
  let listOrdered = false
  let listStart = 1

  const flushParagraph = () => {
    const value = cleanRichText(paragraph.join(' '))
    if (value) section.blocks.push({ kind: 'paragraph', text: value })
    paragraph = []
  }

  const flushList = () => {
    if (!listItems.length) return
    section.blocks.push({ kind: 'list', items: listItems, ordered: listOrdered, start: listStart })
    listItems = []
  }

  const flushSection = () => {
    flushParagraph()
    flushList()
    if (section.title || section.blocks.length) sections.push(section)
    section = { blocks: [] }
  }

  for (const line of lines) {
    if (!line) {
      flushParagraph()
      flushList()
      continue
    }

    const boldHeadingMatch = line.match(/^\*\*([^*]+)\*\*$/)
    const markdownHeadingMatch = line.match(/^#{1,4}\s+(.+)$/)
    const headingMatch = markdownHeadingMatch ?? (
      boldHeadingMatch && !boldHeadingMatch[1].includes(' - ') ? boldHeadingMatch : null
    )
    if (headingMatch) {
      flushSection()
      section.title = cleanRichText(headingMatch[1])
      continue
    }

    const orderedMatch = line.match(/^(\d{1,2})[.)]\s+(.+)/)
    const bulletMatch = line.match(/^[-*]\s+(.+)/)
    const listMatch = orderedMatch ?? bulletMatch
    if (listMatch) {
      flushParagraph()
      const ordered = Boolean(orderedMatch)
      if (listItems.length && ordered !== listOrdered) flushList()
      if (orderedMatch && listItems.length) {
        const marker = Number(orderedMatch[1])
        const expectedMarker = listStart + listItems.length
        if (marker !== expectedMarker) flushList()
      }
      if (!listItems.length) listStart = orderedMatch ? Number(orderedMatch[1]) : 1
      listOrdered = ordered
      listItems.push(cleanRichText(orderedMatch ? orderedMatch[2] : listMatch[1]))
      continue
    }

    flushList()
    paragraph.push(line)
  }

  flushSection()
  return sections.length ? sections : [{ blocks: [{ kind: 'paragraph', text: cleanRichText(text) }] }]
}

function cleanRichText(value: string) {
  return value
    .replace(/^#{1,4}\s*/, '')
    .replace(/\*\*([^*]+)\*\*/g, '$1')
    .replace(/__([^_]+)__/g, '$1')
    .replace(/`([^`]+)`/g, '$1')
    .trim()
}

function getAgentIcon(agent?: AgentDefinition) {
  return iconMap[agent?.icon as keyof typeof iconMap] ?? Route
}

function firstName(name?: string) {
  return name?.split(/\s+/)[0] || 'there'
}

function initials(name: string) {
  return name.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()
}

function customerDisruptionLabel(title?: string) {
  const value = title?.toLowerCase() ?? ''
  if (value.includes('missed connection')) return 'Connection missed'
  if (value.includes('diversion')) return 'Flight diverted'
  return 'Flight cancelled'
}

function customerRunSummary(run: TravelRun | null) {
  if (!run) return 'We are ready to help with your journey.'
  switch (normalizeRunStatus(run.status)) {
    case 'Queued':
    case 'Running': return 'We are finding the right specialist for your journey.'
    case 'WaitingForUser': return 'Your specialist is ready for your reply.'
    case 'WaitingForHuman': return 'A recovery specialist has been requested.'
    case 'HumanResponding': return `${run.humanSupport?.assignedTo ?? 'A recovery specialist'} is reviewing your case.`
    case 'Failed': return 'We could not connect you to support. Please try again.'
    case 'Cancelled': return 'This support chat has been closed.'
  }
}

function customerRunStatus(status: unknown) {
  switch (normalizeRunStatus(status)) {
    case 'Queued': return 'Connecting'
    case 'Running': return 'Reviewing your request'
    case 'WaitingForUser': return 'Waiting for your reply'
    case 'WaitingForHuman': return 'Waiting for a person'
    case 'HumanResponding': return 'Human review in progress'
    case 'Failed': return 'Needs attention'
    case 'Cancelled': return 'Chat closed'
  }
}

function normalizeRunStatus(status: unknown): TravelRun['status'] | undefined {
  if (typeof status === 'string') return status as TravelRun['status']
  if (typeof status === 'number') return ['Queued', 'Running', 'WaitingForUser', 'WaitingForHuman', 'HumanResponding', 'Failed', 'Cancelled'][status] as TravelRun['status'] | undefined
  return undefined
}

function runStatusClass(status: unknown) {
  return (normalizeRunStatus(status) ?? 'Ready').toLowerCase()
}

function customerAgentRole(agent?: AgentDefinition) {
  switch (agent?.id) {
    case 'flight': return 'Flight specialist'
    case 'stay': return 'Hotel and transport specialist'
    case 'insurance': return 'Travel cover specialist'
    case 'loyalty': return 'Priority support specialist'
    case 'human': return 'Customer care specialist'
    default: return 'Journey specialist'
  }
}

function customerAgentDescription(agent?: AgentDefinition) {
  switch (agent?.id) {
    case 'flight': return 'Helping with replacement flights, connections, and baggage routing.'
    case 'stay': return 'Helping with accommodation, meals, and airport transport.'
    case 'insurance': return 'Helping you keep the right evidence and prepare for a claim.'
    case 'loyalty': return 'Using your fare and membership details to shape the support path.'
    case 'human': return 'Reviewing your case when personal assistance is needed.'
    default: return 'Reviewing your situation and connecting you with the right travel specialist.'
  }
}

function friendlySystemMessage(text: string) {
  const value = text.toLowerCase()
  if (value.includes('azure cli') || value.includes('workflow') || value.includes('executor')) {
    return "We couldn't connect you to a specialist right now. Please try again shortly."
  }
  return text
}

function customerPriorityTags(sample: SampleScenario) {
  const tags = [...sample.tags]
  if (sample.message.toLowerCase().includes('medical appointment')) tags.unshift('medical appointment')
  return tags
}

function customerPriority(tag: string) {
  switch (tag.toLowerCase()) {
    case 'medical appointment': return { label: 'Time-sensitive appointment', Icon: HeartPulse }
    case 'cancelled flight': return { label: 'Earliest flight home', Icon: Plane }
    case 'hotel': return { label: 'Hotel if stranded', Icon: Bed }
    case 'insurance': return { label: 'Receipts for insurance', Icon: ReceiptText }
    case 'missed connection': return { label: 'Next available connection', Icon: Route }
    case 'family': return { label: 'Travelling with children', Icon: UsersRound }
    case 'baggage': return { label: 'Checked baggage', Icon: Luggage }
    case 'diversion': return { label: 'Diversion recovery', Icon: Route }
    case 'loyalty': return { label: 'Membership status', Icon: Star }
    case 'reroute': return { label: 'Fastest practical reroute', Icon: Plane }
    default: return { label: tag, Icon: Check }
  }
}

function parseRoute(tripCode: string) {
  const routeCodes = tripCode.split('-').filter((part) => /^[A-Za-z]{3}$/.test(part))
  const from = routeCodes[0] || 'SYD'
  const to = routeCodes.at(-1) || 'LAX'
  return { from, to, fromName: cityName(from), toName: cityName(to) }
}

function cityName(code: string) {
  const map: Record<string, string> = {
    AKL: 'Auckland', BNE: 'Brisbane', DXB: 'Dubai', LAX: 'Los Angeles', LHR: 'London',
    MEL: 'Melbourne', PER: 'Perth', SIN: 'Singapore', SYD: 'Sydney',
  }
  return map[code.toUpperCase()] ?? code.toUpperCase()
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}
