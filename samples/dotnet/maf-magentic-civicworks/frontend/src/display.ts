import type { CivicWorksRun } from './api'

export const timeOfDay = (value: string) => new Date(value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })
export const duration = (seconds: number) => Math.floor(seconds / 60).toString().padStart(2, '0') + ':' + (seconds % 60).toString().padStart(2, '0')
export const isTerminal = (run: CivicWorksRun | null) => !!run && ['Completed', 'Failed', 'BoundedIncomplete'].includes(run.phase)

export function describeRun(run: CivicWorksRun | null) {
  if (!run) return { label: 'READY TO INVESTIGATE', state: 'Not started', title: 'A clear question. An open investigation.', description: 'Start with the known facts. The manager will propose an investigation plan for you to review, then coordinate five specialists to gather and challenge the evidence.', tone: 'neutral', active: false }
  const version = String(Math.max(1, run.planVersion)).padStart(2, '0')
  switch (run.phase) {
    case 'Starting':
    case 'Planning':
      return { label: 'MANAGER AT WORK', state: 'Planning', title: run.planVersion > 1 ? 'New evidence. A revised plan.' : 'Building the investigation plan.', description: run.planVersion > 1 ? 'The manager is reconsidering the route using the evidence already collected. The revised plan will pause for your approval before specialists continue.' : 'The manager is defining what to check and who should check it. The proposed plan will appear below; evidence collection waits for your approval.', tone: 'live', active: true }
    case 'InitialReview':
      return { label: 'YOUR REVIEW IS NEEDED', state: 'Paused for approval', title: 'Review the first investigation plan.', description: 'Read the manager’s proposed checks below. Approving Plan ' + version + ' lets the specialists begin collecting read-only evidence within the case constraints.', tone: 'review', active: false }
    case 'RevisedReview':
      return { label: 'THE EVIDENCE CHANGED THE ROUTE', state: 'Paused for approval', title: 'The revised plan needs your review.', description: 'The site observation conflicts with the drainage register. Compare the plans below and approve the added verification steps before the investigation continues.', tone: 'review', active: false }
    case 'Completed':
      return { label: 'INVESTIGATION COMPLETE', state: 'Brief ready', title: 'A recommendation you can trace to evidence.', description: 'The manager has returned three options after an independent evidence challenge. Review the recommendation, its supporting claims and the matters that still need investigation.', tone: 'complete', active: false }
    case 'Failed':
    case 'BoundedIncomplete':
      return { label: 'INVESTIGATION STOPPED', state: 'Needs attention', title: run.phase === 'Failed' ? 'The live run could not finish.' : 'The investigation reached its execution limit.', description: 'The evidence collected so far remains available. Read the last activity and the error above before starting a new investigation.', tone: 'stopped', active: false }
    default:
      return { label: 'LIVE INVESTIGATION · PLAN ' + version, state: 'Specialists working', title: run.planVersion > 1 ? 'Testing the revised route.' : 'Following the evidence, one check at a time.', description: run.planVersion > 1 ? 'Specialists are checking the added constraints, comparing options and challenging the evidence. Follow their assignments and tool results in the live activity panel.' : 'The manager is assigning checks and evaluating each result. If a finding invalidates the current approach, it will rebuild the plan and ask for your approval.', tone: 'live', active: true }
  }
}
