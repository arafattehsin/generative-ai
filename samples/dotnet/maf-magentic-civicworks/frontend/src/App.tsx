import { useState } from 'react'
import {
  IconArrowRight, IconArrowUpRight, IconBuildingCommunity, IconCheck, IconChevronRight,
  IconCircleCheck, IconFileDescription, IconLayoutDashboard, IconMapPin,
  IconPlayerPlay, IconRoute, IconShieldCheck, IconStack2, IconWifiOff,
} from '@tabler/icons-react'
import { useLiveCivicWorks } from './useLiveCivicWorks'
import {
  ActivityPanel, Budget, EvidencePanel, OptionsPanel, PlanPanel,
  SpecialistTeam, StageTrack,
} from './Workspace'
import { timeOfDay, describeRun } from './display'

type View = 'investigation' | 'evidence' | 'brief'

export default function App() {
  const live = useLiveCivicWorks()
  const { run, configuration, busy, error, restoring, connectionState } = live
  const [view, setView] = useState<View>('investigation')
  const [evidenceId, setEvidenceId] = useState<string | null>(null)
  const info = describeRun(run)
  const isReview = run?.phase === 'InitialReview' || run?.phase === 'RevisedReview'
  const terminal = !!run && ['Completed', 'Failed', 'BoundedIncomplete'].includes(run.phase)
  const canStart = !busy && !restoring && configuration?.isConfigured === true && (!run || terminal)
  const evidence = run?.evidence ?? []
  const openEvidence = (id: string) => { setEvidenceId(id); setView('evidence') }
  const start = async () => {
    setView('investigation')
    setEvidenceId(null)
    await live.start()
  }
  const nav = [
    { id: 'investigation' as const, label: 'Investigation', icon: IconLayoutDashboard, count: null },
    { id: 'evidence' as const, label: 'Evidence register', icon: IconStack2, count: evidence.length },
    { id: 'brief' as const, label: 'Options brief', icon: IconFileDescription, count: run?.brief ? 'Ready' : null },
  ]
  return (
    <div className="app-shell">
      <a className="skip-link" href="#workspace">Skip to investigation</a>
      <aside className="navigation" aria-label="Case navigation">
        <a className="brand" href="/" aria-label="CivicWorks home">
          <span className="brand-symbol"><IconBuildingCommunity size={25} stroke={1.6} /></span>
          <span>CivicWorks<small>INVESTIGATION WORKSPACE</small></span>
        </a>
        <div className="nav-label">WORKSPACE</div>
        <nav>{nav.map(item => <button key={item.id} onClick={() => setView(item.id)} aria-label={item.label}
          className={view === item.id ? 'nav-item selected' : 'nav-item'}
          aria-current={view === item.id ? 'page' : undefined}>
          <item.icon size={19} stroke={1.65} /><span>{item.label}</span>
          {item.count === 'Ready' ? <i className="nav-ready" title="Brief ready" /> : item.count !== null ? <small>{item.count}</small> : null}
        </button>)}</nav>
        <div className="case-context">
          <div className="nav-label">CURRENT CASE</div>
          <div className="context-name"><span className="case-dot" /><strong>Marrin Precinct</strong></div>
          <p>CW-2047 · Public realm</p>
          <PrecinctMap />
          <div className="context-facts">
            <span><IconMapPin size={16} /> Library · Centre · Bus stop 214</span>
            <span><IconShieldCheck size={16} /> Read-only investigation</span>
          </div>
          <div className="context-note">Fictional NSW council<br />Synthetic evidence, live agents.</div>
        </div>
        <div className="nav-bottom">
          <div className="officer-avatar">SN</div>
          <div><strong>Sarah Nguyen</strong><span>Place Projects Officer</span></div>
        </div>
      </aside>

      <div className="app-main">
        <header className="topbar">
          <div className="breadcrumbs"><span>Cases</span><IconChevronRight size={14} /><strong>CW-2047</strong></div>
          <div className="topbar-right">
            <span className={'connection ' + (configuration?.isConfigured ? 'online' : '')}>
              <i />{configuration?.isConfigured ? 'Foundry configured' : 'Checking configuration'}
            </span>
            <span className="environment">LIVE DEMO</span>
          </div>
        </header>

        <main id="workspace" className="workspace">
          <div className="page-heading">
            <div>
              <div className="eyebrow">PRECINCT INVESTIGATION <span>/</span> CW-2047</div>
              <h1>Marrin Precinct</h1>
              <p>A safer, more accessible route through the neighbourhood.</p>
            </div>
            <button className={'button primary start-button ' + (run && !terminal ? 'is-active' : '')} onClick={start} disabled={!canStart}>
              {run && !terminal ? <span className="live-dot" /> : <IconPlayerPlay size={17} />}
              {restoring ? 'Restoring investigation…' : !run ? 'Start investigation' : terminal ? 'New investigation' : 'Investigation active'}
              {canStart ? <IconArrowUpRight size={17} /> : null}
            </button>
          </div>

          {error || run?.error ? <div className="error-notice" role="alert">
            <IconWifiOff size={20} /><div><strong>{run?.error ? 'Investigation stopped' : 'Connection needs attention'}</strong><p>{run?.error ?? error}</p></div>
            {run && !terminal ? <button className="button subtle" onClick={live.reconnect} disabled={busy}>Reconnect</button> : null}
          </div> : null}

          <StageTrack run={run} />
          <div className="content-layout">
            <div className="primary-column">
              <section className={'run-status tone-' + info.tone} aria-label="Current investigation status">
                <div className="status-topline">
                  <span className="eyebrow">{info.label}</span>
                  <span className="status-chip"><i className={info.active ? 'live-dot' : ''} />{info.state}</span>
                </div>
                <h2>{info.title}</h2>
                <p>{info.description}</p>
                {isReview ? <button className="text-button review-shortcut" onClick={() => {
                  setView('investigation')
                  requestAnimationFrame(() => document.getElementById('plan-review')?.scrollIntoView({ behavior: 'instant', block: 'start' }))
                }}>Go to the plan review <IconArrowRight size={16} /></button> : null}
                <div className="status-footer">
                  <span><IconShieldCheck size={16} /> Human approval at every plan change</span>
                  {run ? <span>Plan {String(Math.max(1, run.planVersion)).padStart(2, '0')}</span> : <span>5 specialists + 1 manager</span>}
                </div>
              </section>

              <div className="view-tabs" aria-label="Investigation views">
                {nav.map(item => <button key={item.id} aria-current={view === item.id ? 'page' : undefined}
                  className={view === item.id ? 'active' : ''} onClick={() => setView(item.id)}>
                  <item.icon size={17} />{item.label}
                  {item.id === 'evidence' ? <span>{evidence.length}</span> : null}
                  {item.id === 'brief' && run?.brief ? <i className="ready-dot" /> : null}
                </button>)}
              </div>

              {view === 'investigation' ? <div className="view-content">
                {!run ? <CaseBrief /> : null}
                {run?.revisionReason || evidence.some(item => item.id === 'sandstone') ? <div className="revision-explainer">
                  <div className="revision-icon"><IconRoute size={23} /></div>
                  <div><div className="eyebrow">{run?.revisionReason ? 'WHY THE PLAN CHANGED' : 'AN EVIDENCE CONFLICT HAS SURFACED'}</div><h3>The records and the site disagree.</h3>
                    <p>{run?.revisionReason ? run.revisionReason + ' The manager added verification before progressing the works options.' : 'The site observation describes a sandstone feature that conflicts with the drainage register. The manager must assess this finding before proceeding.'}</p>
                    <button className="text-button" onClick={() => openEvidence('sandstone')}>Inspect the conflicting evidence <IconArrowRight size={16} /></button>
                  </div>
                </div> : null}
                {run && !isReview && !terminal ? <section className="assignment panel">
                  <div className="section-title"><div><div className="eyebrow">CURRENT ASSIGNMENT</div>
                    <h3>{run.activeSpecialist || 'Magentic Manager'}</h3></div><span className="working-indicator"><i /> Working</span></div>
                  <p className="assignment-instruction">{run.activeActivity || run.statusMessage}</p>
                  <div className="quiet-note">The manager selects the next specialist after evaluating progress. Evidence appears below as tools return.</div>
                </section> : null}
                <PlanPanel key={run?.id ?? 'empty'} run={run} busy={busy}
                  onApprove={constraints => live.review({ action: 'approve', constraints })} />
                {run?.brief ? <section className="completion-callout">
                  <IconCircleCheck size={28} /><div><h3>The evidence challenge is complete.</h3>
                    <p>{run.brief.claims.length} cited claims · {run.brief.openMatters.length} open matters · Option {run.brief.recommendation} recommended</p></div>
                  <button className="button primary" onClick={() => setView('brief')}>Read the brief <IconArrowRight size={17} /></button>
                </section> : null}
                <SpecialistTeam run={run} />
                {evidence.length ? <section className="recent-evidence panel">
                  <div className="section-title"><div><div className="eyebrow">FROM THE INVESTIGATION</div><h3>Latest evidence</h3></div>
                    <button className="text-button" onClick={() => setView('evidence')}>View all {evidence.length} <IconArrowRight size={15} /></button></div>
                  {evidence.slice(-3).reverse().map(item => <button className="evidence-preview" key={item.id} onClick={() => openEvidence(item.id)}>
                    <span className={'evidence-symbol ' + item.status.toLowerCase()}>{item.status === 'Verified' ? <IconCheck size={15} /> : item.status === 'Conflict' ? '!' : '…'}</span>
                    <span><strong>{item.title}</strong><small>{item.reference} · {item.specialist}</small></span><IconArrowUpRight size={17} />
                  </button>)}
                </section> : null}
              </div> : null}
              {view === 'evidence' ? <EvidencePanel evidence={evidence} selectedId={evidenceId} onSelect={setEvidenceId} /> : null}
              {view === 'brief' ? <OptionsPanel brief={run?.brief ?? null} evidence={evidence} onEvidence={openEvidence} /> : null}
            </div>

            <aside className="activity-column" aria-label="Live investigation activity">
              <ActivityPanel run={run} connectionState={connectionState} onEvidence={openEvidence} />
              <Budget run={run} />
              <div className="authority-note"><IconShieldCheck size={18} /><p>Decision support only. Engineering, heritage, expenditure and works approval stay with the responsible people.</p></div>
            </aside>
          </div>

          <footer className="workspace-footer"><span>CivicWorks <b>·</b> Microsoft Agent Framework</span>
            <span>{run ? 'Started ' + timeOfDay(run.startedAt) : 'Evidence-led investigation'} <b>·</b> {configuration?.modelDeployment || 'Microsoft Foundry'}</span>
          </footer>
        </main>
      </div>
    </div>
  )
}

function CaseBrief() {
  return <section className="case-brief panel">
    <div className="section-title"><div><div className="eyebrow">THE QUESTION</div><h3>What should the council do next?</h3></div><span className="document-label">CASE BRIEF / 01</span></div>
    <p className="brief-question">Water keeps pooling near the library crossing. The accessible path to the community centre and bus stop is incomplete. Before recommending works, we need to understand the assets, the constraints and the evidence.</p>
    <div className="case-brief-grid">
      <div><span>INVESTIGATE</span><p>Service requests, access impacts, drainage records and current site observations.</p></div>
      <div><span>DELIVER</span><p>Three works options, a supported recommendation and the questions still unresolved.</p></div>
    </div>
    <div className="quiet-note"><IconShieldCheck size={16} /> Protect street trees, business access and a continuous accessible path.</div>
  </section>
}

function PrecinctMap() {
  return <figure className="precinct-map">
    <svg viewBox="0 0 200 146" role="img" aria-label="Illustrative precinct layout showing the library, community centre and bus stop">
      <path d="M-10 100L210 60M75 -10L107 155" stroke="#273e3e" strokeWidth="19" fill="none" />
      <path d="M-10 100L210 60M75 -10L107 155" stroke="#42605b" strokeWidth="1" strokeDasharray="3 5" fill="none" />
      <rect x="15" y="24" width="45" height="37" rx="4" fill="#3b5751" /><rect x="115" y="94" width="58" height="33" rx="4" fill="#3b5751" />
      <path d="M38 68L75 76L96 98L132 83L174 74" stroke="#acd1ad" strokeWidth="2" strokeDasharray="4 4" fill="none" />
      <circle cx="38" cy="68" r="4" fill="#d8ecb1" /><circle cx="132" cy="83" r="4" fill="#d8ecb1" /><circle cx="174" cy="74" r="4" fill="#d8ecb1" />
      <g fill="#d5dfd6" fontSize="9" fontFamily="Arial,sans-serif"><text x="17" y="17">Library</text><text x="118" y="140">Community centre</text><text x="130" y="57">Bus stop 214</text></g>
      <g fill="#5b7a5b"><circle cx="23" cy="122" r="8" /><circle cx="49" cy="118" r="6" /><circle cx="162" cy="24" r="9" /><circle cx="183" cy="33" r="6" /></g>
    </svg><figcaption>PRECINCT CONTEXT · ILLUSTRATIVE</figcaption>
  </figure>
}
