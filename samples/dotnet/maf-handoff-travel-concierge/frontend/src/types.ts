export type RunStatus = 'Queued' | 'Running' | 'WaitingForUser' | 'WaitingForHuman' | 'HumanResponding' | 'Failed' | 'Cancelled'
export type ConversationAuthor = 'User' | 'Agent' | 'Human' | 'System'
export type HumanSupportStatus = 'Requested' | 'Claimed' | 'Resolved'

export type AgentDefinition = {
  id: string
  name: string
  role: string
  accent: string
  icon: string
  summary: string
}

export type ConfigResponse = {
  hasProjectEndpoint: boolean
  projectEndpoint: string
  deploymentName: string
  agents: AgentDefinition[]
}

export type SampleScenario = {
  id: string
  title: string
  travellerName: string
  tripCode: string
  urgency: string
  message: string
  tags: string[]
}

export type CreateRunRequest = {
  message: string
  travellerName: string
  tripCode: string
  urgency: string
}

export type TravelCaseSnapshot = {
  route: string
  disruption: string
  constraint: string
  recoveryPlan: string
  evidenceNeeded: string
  risk: string
}

export type ConversationMessage = {
  id: string
  authorType: ConversationAuthor
  agentId?: string
  authorName: string
  text: string
  createdAt: string
}

export type HandoffTimelineItem = {
  id: string
  fromAgentId: string
  fromAgentName: string
  toAgentId: string
  toAgentName: string
  reason: string
  createdAt: string
}

export type HumanSupportSnapshot = {
  requestId: string
  status: HumanSupportStatus
  requestedByAgentId: string
  requestedByAgentName: string
  reason: string
  decisionNeeded: string
  recommendation: string
  conversationSummary: string
  assignedTo?: string
  requestedAt: string
  claimedAt?: string
  resolvedAt?: string
}

export type TravelRun = {
  id: string
  createdAt: string
  updatedAt: string
  status: RunStatus
  mode: string
  travellerName: string
  tripCode: string
  urgency: string
  initialRequest: string
  currentOwnerId: string
  currentOwnerName: string
  summary: string
  case: TravelCaseSnapshot
  messages: ConversationMessage[]
  timeline: HandoffTimelineItem[]
  agents: AgentDefinition[]
  humanSupport?: HumanSupportSnapshot
}
