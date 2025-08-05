import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Separator } from '@/components/ui/separator'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
import { User, CreditCard, Wrench, ArrowsClockwise, PaperPlaneTilt, Clock, CheckCircle, ArrowRight, CaretDown, CaretUp } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { ImplementationToggle } from '@/components/ImplementationToggle'
import { BackendIntegration } from '@/components/BackendIntegration'
import { 
  customerServiceAPI, 
  mapTicketStatus, 
  mapAgentStatus,
  type CustomerTicket as BackendTicket,
  type AgentInfo as BackendAgent,
  type SubmitTicketRequest
} from '@/services/api'

interface Ticket {
  id: string
  customerName: string
  email: string
  subject: string
  description: string
  timestamp: string
  status: 'new' | 'routing' | 'processing' | 'completed'
  assignedAgents: string[]
  responses: AgentResponse[]
  finalResponse?: string
}

interface AgentResponse {
  agentId: string
  response: string
  timestamp: string
  status: 'pending' | 'completed'
}

interface Agent {
  id: string
  name: string
  type: 'front-desk' | 'billing' | 'technical' | 'orchestrator'
  status: 'idle' | 'processing' | 'completed'
  currentTicket?: string
  icon: any
  color: string
  description: string
}

// Legacy agents definition for fallback
const LEGACY_AGENTS: Agent[] = [
  {
    id: 'front-desk',
    name: 'Front Desk Agent',
    type: 'front-desk',
    status: 'idle',
    icon: User,
    color: 'agent-front-desk',
    description: 'Routes tickets and coordinates responses'
  },
  {
    id: 'billing',
    name: 'Billing Agent',
    type: 'billing',
    status: 'idle',
    icon: CreditCard,
    color: 'agent-billing',
    description: 'Handles billing and account issues'
  },
  {
    id: 'technical',
    name: 'Tech Support Agent',
    type: 'technical',
    status: 'idle',
    icon: Wrench,
    color: 'agent-tech',
    description: 'Resolves technical problems'
  },
  {
    id: 'orchestrator',
    name: 'Response Orchestrator Agent',
    type: 'orchestrator',
    status: 'idle',
    icon: ArrowsClockwise,
    color: 'agent-orchestrator',
    description: 'Synthesizes and coordinates multi-agent responses'
  }
]

// Icon mapping for backend agent data
const getIconByName = (iconName: string) => {
  switch (iconName) {
    case 'User': return User
    case 'CreditCard': return CreditCard
    case 'Wrench': return Wrench
    case 'ArrowsClockwise': return ArrowsClockwise
    default: return User
  }
}

// Convert backend agent info to frontend agent format
const convertBackendAgent = (backendAgent: any): Agent => ({
  id: backendAgent.id,
  name: backendAgent.name,
  type: backendAgent.type,
  status: mapAgentStatus(backendAgent.status),
  currentTicket: backendAgent.currentTicket,
  icon: getIconByName(backendAgent.iconName),
  color: getColorMapping(backendAgent.id), // Use ID-based color mapping instead of backend color
  description: backendAgent.description
})

// Map agent IDs to consistent color classes
const getColorMapping = (agentId: string): string => {
  switch (agentId) {
    case 'front-desk': return 'agent-front-desk'
    case 'billing': return 'agent-billing'
    case 'technical': return 'agent-tech'
    case 'orchestrator': return 'agent-orchestrator'
    default: return 'agent-front-desk'
  }
}

function App() {
  const [tickets, setTickets] = useState<Ticket[]>([])
  const [agents, setAgents] = useState<Agent[]>(LEGACY_AGENTS)
  const [currentTicket, setCurrentTicket] = useState<Ticket | null>(null)
  
  // Implementation toggle state
  const [useBackend, setUseBackend] = useState(false)
  
  // Collapsible panel states
  const [implementationPanelOpen, setImplementationPanelOpen] = useState(false)
  const [backendPanelOpen, setBackendPanelOpen] = useState(false)
  
  // Form state
  const [customerName, setCustomerName] = useState('')
  const [email, setEmail] = useState('')
  const [subject, setSubject] = useState('')
  const [description, setDescription] = useState('')

  // Load agents when implementation changes
  useEffect(() => {
    // Always fetch agents from backend (both mock and real implementations support 4 agents)
    loadAgentsFromBackend()
  }, [useBackend])

  // Load agents initially when component mounts
  useEffect(() => {
    loadAgentsFromBackend()
  }, [])

  const loadAgentsFromBackend = async () => {
    try {
      console.log('Loading agents from backend...')
      const backendAgents = await customerServiceAPI.getAgents()
      console.log('Backend agents received:', backendAgents)
      const frontendAgents = backendAgents.map(convertBackendAgent)
      console.log('Frontend agents mapped:', frontendAgents)
      setAgents(frontendAgents)
    } catch (error) {
      console.error('Failed to load agents from backend:', error)
      console.log('Falling back to LEGACY_AGENTS:', LEGACY_AGENTS)
      // Fallback to legacy agents (now includes orchestrator)
      setAgents(LEGACY_AGENTS)
    }
  }

  // Mock implementation functions (without GitHub Spark)
  const analyzeTicketMock = async (ticket: Ticket): Promise<string[]> => {
    // Simple keyword-based routing logic
    const text = (ticket.subject + ' ' + ticket.description).toLowerCase()
    const agents: string[] = []
    
    // Check for billing-related keywords
    if (text.includes('bill') || text.includes('charge') || text.includes('payment') || 
        text.includes('refund') || text.includes('invoice') || text.includes('cost')) {
      agents.push('billing')
    }
    
    // Check for technical keywords
    if (text.includes('error') || text.includes('bug') || text.includes('crash') || 
        text.includes('not working') || text.includes('broken') || text.includes('issue') || 
        text.includes('problem') || text.includes('login')) {
      agents.push('technical')
    }
    
    // Default to billing if no specific match
    if (agents.length === 0) {
      agents.push('billing')
    }
    
    return agents
  }

  const generateAgentResponseMock = async (ticket: Ticket, agentType: string): Promise<string> => {
    // Simulate processing delay
    await new Promise(resolve => setTimeout(resolve, 1500))
    
    const responses = {
      billing: [
        `Hello ${ticket.customerName}, I've reviewed your billing inquiry. Let me help you resolve this issue with your account.`,
        `Hi ${ticket.customerName}, I can see the billing concern you've raised. I'll look into your account details and provide a solution.`,
        `Dear ${ticket.customerName}, thank you for contacting us about your billing question. I'm here to assist you with this matter.`
      ],
      technical: [
        `Hi ${ticket.customerName}, I understand you're experiencing technical difficulties. Let me troubleshoot this issue for you.`,
        `Hello ${ticket.customerName}, I've received your technical support request. I'll help you resolve this problem step by step.`,
        `Dear ${ticket.customerName}, I can see the technical issue you're facing. Let me provide you with a solution.`
      ]
    }
    
    const agentResponses = responses[agentType as keyof typeof responses] || responses.billing
    return agentResponses[Math.floor(Math.random() * agentResponses.length)]
  }

  const aggregateResponsesMock = async (ticket: Ticket): Promise<string> => {
    // Simulate aggregation delay
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    const responses = ticket.responses.map(r => r.response).join(' ')
    
    return `Dear ${ticket.customerName},

Thank you for contacting our customer service team. We have reviewed your request regarding "${ticket.subject}" and our specialists have worked together to provide you with a comprehensive solution.

${responses}

If you need any further assistance, please don't hesitate to reach out to us. We're here to help!

Best regards,
Customer Service Team`
  }

  const generateOrchestratorResponseMock = async (ticket: Ticket, specialistResponses: AgentResponse[]): Promise<AgentResponse> => {
    // Simulate orchestrator processing delay
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    const combinedScenarios = [
      `After coordinating with our billing and technical specialists, I've identified that ${ticket.customerName} was unable to process their request due to interconnected issues. Our technical team has resolved the system problems, and our billing team has addressed the account concerns. This comprehensive solution ensures both technical functionality and billing accuracy are restored.`,
      
      `Our team coordination reveals that ${ticket.customerName}'s difficulties involved both technical and billing aspects. The specialists have worked together to provide an integrated solution that addresses all underlying causes. This coordinated approach ensures seamless service restoration.`,
      
      `Through A2A coordination, we've determined that ${ticket.customerName}'s issues required expertise from multiple departments. Our technical and billing teams have collaborated to implement a unified solution that resolves all aspects of your inquiry.`
    ]

    return {
      agentId: 'orchestrator',
      response: combinedScenarios[Math.floor(Math.random() * combinedScenarios.length)],
      timestamp: new Date().toISOString(),
      status: 'completed'
    }
  }

  const generateFinalResponseWithOrchestrationMock = async (ticket: Ticket, orchestratorResponse: AgentResponse): Promise<string> => {
    // Simulate final response generation
    await new Promise(resolve => setTimeout(resolve, 500))
    
    return `Dear ${ticket.customerName},

Thank you for contacting our customer service team regarding "${ticket.subject}".

${orchestratorResponse.response}

Our coordinated multi-agent approach ensures that all aspects of your inquiry have been thoroughly addressed by our specialized team members working together. If you need any further assistance or have additional questions, please don't hesitate to contact us.

Best regards,
Customer Service Team`
  }

  const processTicketMock = async (ticket: Ticket) => {
    // Update agents and ticket status
    setAgents(current => 
      current.map(agent => 
        agent.id === 'front-desk' 
          ? { ...agent, status: 'processing', currentTicket: ticket.id }
          : agent
      )
    )
    
    setTickets(current => 
      current.map(t => 
        t.id === ticket.id 
          ? { ...t, status: 'routing' }
          : t
      )
    )

    // Simulate front desk analysis time
    await new Promise(resolve => setTimeout(resolve, 2000))

    try {
      // Analyze ticket and determine routing
      const assignedAgentIds = await analyzeTicketMock(ticket)
      
      setTickets(current => 
        current.map(t => 
          t.id === ticket.id 
            ? { ...t, status: 'processing', assignedAgents: assignedAgentIds }
            : t
        )
      )

      // Update agent statuses
      setAgents(current => 
        current.map(agent => ({
          ...agent,
          status: assignedAgentIds.includes(agent.id) ? 'processing' : 
                  agent.id === 'front-desk' ? 'processing' : 'idle',
          currentTicket: assignedAgentIds.includes(agent.id) || agent.id === 'front-desk' ? ticket.id : undefined
        }))
      )

      // Generate responses from assigned agents
      const responses: AgentResponse[] = []
      
      for (const agentId of assignedAgentIds) {
        const response = await generateAgentResponseMock(ticket, agentId)
        responses.push({
          agentId,
          response,
          timestamp: new Date().toISOString(),
          status: 'completed'
        })
        
        // Update agent status
        setAgents(current => 
          current.map(agent => 
            agent.id === agentId 
              ? { ...agent, status: 'completed' }
              : agent
          )
        )
      }

      // Update ticket with responses
      setTickets(current => 
        current.map(t => 
          t.id === ticket.id 
            ? { ...t, responses }
            : t
        )
      )

      // LAYER 3: Orchestrator Agent - Response Synthesis & Coordination (only if multiple agents)
      let finalResponse: string
      
      if (assignedAgentIds.length > 1) {
        // Multiple agents - activate orchestrator
        setAgents(current => 
          current.map(agent => 
            agent.id === 'orchestrator'
              ? { ...agent, status: 'processing', currentTicket: ticket.id }
              : agent
          )
        )

        // Simulate orchestrator processing time
        await new Promise(resolve => setTimeout(resolve, 1500))

        // Generate orchestrator response
        const orchestratorResponse = await generateOrchestratorResponseMock(ticket, responses)
        
        // Add orchestrator response to the list
        const allResponses = [...responses, orchestratorResponse]
        
        setTickets(current => 
          current.map(t => 
            t.id === ticket.id 
              ? { ...t, responses: allResponses }
              : t
          )
        )

        // Mark orchestrator as completed
        setAgents(current => 
          current.map(agent => 
            agent.id === 'orchestrator'
              ? { ...agent, status: 'completed' }
              : agent
          )
        )

        // Generate final customer response with orchestrator synthesis
        finalResponse = await generateFinalResponseWithOrchestrationMock(ticket, orchestratorResponse)
      } else {
        // Single agent - no orchestration needed
        finalResponse = await aggregateResponsesMock({
          ...ticket,
          responses
        })
      }

      // Complete the ticket
      setTickets(current => 
        current.map(t => 
          t.id === ticket.id 
            ? { ...t, status: 'completed', finalResponse }
            : t
        )
      )

      // Update current ticket if it's the active one
      setCurrentTicket(current => 
        current?.id === ticket.id 
          ? { ...current, status: 'completed', responses, finalResponse }
          : current
      )

      // Reset all agents
      setAgents(current => 
        current.map(agent => ({
          ...agent,
          status: 'idle',
          currentTicket: undefined
        }))
      )

      toast.success('Ticket resolved successfully!')

    } catch (error) {
      toast.error('Error processing ticket')
      console.error(error)
    }
  }
  

  const submitTicket = async () => {
    if (!customerName || !email || !subject || !description) {
      toast.error('Please fill in all fields')
      return
    }

    try {
      if (useBackend) {
        // Backend implementation
        const categoryKeywords = {
          billing: ['bill', 'charge', 'payment', 'refund', 'invoice', 'cost'],
          technical: ['error', 'bug', 'crash', 'not working', 'broken', 'issue', 'problem'],
          general: ['question', 'help', 'support', 'information', 'inquiry']
        }
        
        const text = (subject + ' ' + description).toLowerCase()
        let category = 'general'
        
        if (categoryKeywords.billing.some(keyword => text.includes(keyword))) {
          category = 'billing'
        } else if (categoryKeywords.technical.some(keyword => text.includes(keyword))) {
          category = 'technical'
        }
        
        const backendTicket: SubmitTicketRequest = {
          customerName,
          email,
          subject,
          description,
          category
        }
        
        const response = await customerServiceAPI.submitTicket(backendTicket)
        
        // Convert backend response to local format
        const newTicket: Ticket = {
          id: response.id,
          customerName: response.customerName,
          email: response.email,
          subject: response.subject,
          description: response.description,
          timestamp: response.timestamp,
          status: mapTicketStatus(response.status),
          assignedAgents: response.assignedAgents || [],
          responses: []
        }

        setTickets(currentTickets => [...currentTickets, newTicket])
        setCurrentTicket(newTicket)
        
        toast.success('Support ticket submitted successfully!')
        
        // Start polling for backend updates
        pollBackendTicketUpdates(response.id)
        
      } else {
        // Mock implementation
        const newTicket: Ticket = {
          id: Date.now().toString(),
          customerName,
          email,
          subject,
          description,
          timestamp: new Date().toISOString(),
          status: 'new',
          assignedAgents: [],
          responses: []
        }

        setTickets(currentTickets => [...currentTickets, newTicket])
        setCurrentTicket(newTicket)
        
        toast.success('Support ticket submitted successfully!')
        
        // Start mock processing with real-time updates
        setTimeout(() => processTicketMock(newTicket), 1000)
      }
      
      // Clear form
      setCustomerName('')
      setEmail('')
      setSubject('')
      setDescription('')
      
    } catch (error) {
      console.error('Failed to submit ticket:', error)
      toast.error('Failed to submit ticket. Please try again.')
    }
  }



  const pollBackendTicketUpdates = async (ticketId: string) => {
    const maxAttempts = 10 // Poll for up to 30 seconds (3s intervals)
    let attempts = 0
    
    const pollTicket = async () => {
      try {
        // Fetch both ticket status AND agent statuses from backend
        const [ticketResponse, backendAgents] = await Promise.all([
          customerServiceAPI.getTicket(ticketId),
          customerServiceAPI.getAgents()
        ])
        
        // Update the ticket in our state
        setTickets(current => 
          current.map(t => 
            t.id === ticketId 
              ? {
                  ...t,
                  status: mapTicketStatus(ticketResponse.status),
                  assignedAgents: ticketResponse.assignedAgents || [],
                  finalResponse: ticketResponse.finalResponse,
                  // Map backend responses to frontend format
                  responses: ticketResponse.responses?.map(r => ({
                    agentId: r.agentId,
                    response: r.response,
                    timestamp: r.timestamp,
                    status: 'completed' as const
                  })) || []
                }
              : t
          )
        )
        
        // Update current ticket if it's the active one
        setCurrentTicket(current => 
          current?.id === ticketId 
            ? {
                ...current,
                status: mapTicketStatus(ticketResponse.status),
                assignedAgents: ticketResponse.assignedAgents || [],
                finalResponse: ticketResponse.finalResponse,
                // Map backend responses to frontend format
                responses: ticketResponse.responses?.map(r => ({
                  agentId: r.agentId,
                  response: r.response,
                  timestamp: r.timestamp,
                  status: 'completed' as const
                })) || []
              }
            : current
        )
        
        // Update agents directly from backend status - trust the backend completely
        const frontendAgents = backendAgents.map(convertBackendAgent)
        setAgents(frontendAgents)
        
        // If ticket is completed, stop polling and reset agents after a short delay
        if (ticketResponse.status === 3 || ticketResponse.status === 4) { // 3 = Completed, 4 = Failed
          setTimeout(() => {
            setAgents(current => 
              current.map(agent => ({
                ...agent,
                status: 'idle',
                currentTicket: undefined
              }))
            )
          }, 2000) // Show completion for 2 seconds before resetting
          
          toast.success('Ticket processing completed!')
          return
        }
        
        // Continue polling if not completed and we haven't reached max attempts
        attempts++
        if (attempts < maxAttempts) {
          setTimeout(pollTicket, 3000) // Poll every 3 seconds
        }
        
      } catch (error) {
        console.error('Error polling ticket updates:', error)
        attempts++
        if (attempts < maxAttempts) {
          setTimeout(pollTicket, 3000) // Retry on error
        }
      }
    }
    
    // Start polling after a short delay to allow backend processing to begin
    setTimeout(pollTicket, 1000)
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'idle':
        return <Badge variant="secondary">Idle</Badge>
      case 'processing':
        return <Badge className="status-processing">Processing</Badge>
      case 'completed':
        return <Badge className="status-completed">Completed</Badge>
      default:
        return <Badge variant="outline">{status}</Badge>
    }
  }

  const getTicketStatusBadge = (status: string) => {
    switch (status) {
      case 'new':
        return <Badge variant="outline">New</Badge>
      case 'routing':
        return <Badge className="status-processing">Routing</Badge>
      case 'processing':
        return <Badge className="status-waiting">Processing</Badge>
      case 'completed':
        return <Badge className="status-completed">Completed</Badge>
      default:
        return <Badge variant="outline">{status}</Badge>
    }
  }

  return (
    <div className="min-h-screen bg-background p-6">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header */}
        <div className="text-center space-y-2">
          <h1 className="text-4xl font-bold text-primary">Multi-Agent Customer Service</h1>
          <p className="text-lg text-muted-foreground">
            Intelligent agent-to-agent communication for seamless customer support
          </p>
        </div>

        {/* Implementation Mode Panel */}
        <Collapsible open={implementationPanelOpen} onOpenChange={setImplementationPanelOpen}>
          <Card className="border-blue-200 bg-blue-50/50">
            <CardHeader className="pb-3">
              <CollapsibleTrigger asChild>
                <Button variant="ghost" className="flex items-center justify-between w-full p-0 h-auto hover:bg-blue-100/80 transition-colors duration-200 rounded-lg">
                  <div className="flex items-center space-x-2">
                    <div className="text-blue-600">📋</div>
                    <CardTitle className="text-blue-800">Implementation Mode</CardTitle>
                  </div>
                  {implementationPanelOpen ? <CaretUp className="h-4 w-4" /> : <CaretDown className="h-4 w-4" />}
                </Button>
              </CollapsibleTrigger>
              <CardDescription className="text-blue-600 text-left">
                Toggle between mock and real A2A (Agent-to-Agent) implementations
              </CardDescription>
            </CardHeader>
            <CollapsibleContent>
              <CardContent className="pt-0">
                <ImplementationToggle onImplementationChange={setUseBackend} />
              </CardContent>
            </CollapsibleContent>
          </Card>
        </Collapsible>

        {/* Backend Integration Panel */}
        <Collapsible open={backendPanelOpen} onOpenChange={setBackendPanelOpen}>
          <Card className="border-green-200 bg-green-50/50">
            <CardHeader className="pb-3">
              <CollapsibleTrigger asChild>
                <Button variant="ghost" className="flex items-center justify-between w-full p-0 h-auto hover:bg-green-100/80 transition-colors duration-200 rounded-lg">
                  <div className="flex items-center space-x-2">
                    <div className="text-green-600">🔗</div>
                    <CardTitle className="text-green-800">Backend API Integration</CardTitle>
                  </div>
                  {backendPanelOpen ? <CaretUp className="h-4 w-4" /> : <CaretDown className="h-4 w-4" />}
                </Button>
              </CollapsibleTrigger>
              <CardDescription className="text-green-600 text-left">
                Real-time connection to .NET backend API
              </CardDescription>
            </CardHeader>
            <CollapsibleContent>
              <CardContent className="pt-0">
                <BackendIntegration />
              </CardContent>
            </CollapsibleContent>
          </Card>
        </Collapsible>

        {/* Agent Dashboard */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {agents.map((agent) => {
            const Icon = agent.icon
            return (
              <Card 
                key={agent.id} 
                className={`${agent.color} ${agent.status === 'processing' ? 'processing-pulse' : ''}`}
              >
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-background/50 rounded-lg">
                        <Icon size={24} style={{ color: 'var(--agent-color)' }} />
                      </div>
                      <div>
                        <CardTitle className="text-lg">{agent.name}</CardTitle>
                        <CardDescription className="text-sm">
                          {agent.description}
                        </CardDescription>
                      </div>
                    </div>
                    {getStatusBadge(agent.status)}
                  </div>
                </CardHeader>
                <CardContent>
                  {agent.currentTicket && (
                    <div className="space-y-2">
                      <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                        <Clock size={16} />
                        <span>Working on ticket #{agent.currentTicket}</span>
                      </div>
                      {agent.status === 'processing' && (
                        <Progress value={60} className="h-2" />
                      )}
                    </div>
                  )}
                </CardContent>
              </Card>
            )
          })}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {/* Ticket Submission Form */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <PaperPlaneTilt size={24} />
                <span>Submit Support Request</span>
              </CardTitle>
              <CardDescription>
                Describe your issue and our AI agents will route it to the right specialists
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium">Name</label>
                  <Input
                    value={customerName}
                    onChange={(e) => setCustomerName(e.target.value)}
                    placeholder="Your full name"
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Email</label>
                  <Input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="your.email@example.com"
                  />
                </div>
              </div>
              <div>
                <label className="text-sm font-medium">Subject</label>
                <Input
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                  placeholder="Brief description of your issue"
                />
              </div>
              <div>
                <label className="text-sm font-medium">Description</label>
                <Textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Please provide detailed information about your issue..."
                  rows={4}
                />
              </div>
              <Button 
                onClick={submitTicket} 
                className="w-full"
                disabled={agents.some(a => a.status === 'processing')}
              >
                Submit Support Request
              </Button>
            </CardContent>
          </Card>

          {/* Current Ticket Display */}
          {currentTicket && (
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Current Ticket #{currentTicket.id}</CardTitle>
                  {getTicketStatusBadge(currentTicket.status)}
                </div>
                <CardDescription>
                  Submitted at {new Date(currentTicket.timestamp).toLocaleString()}
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <h4 className="font-medium">{currentTicket.subject}</h4>
                  <p className="text-sm text-muted-foreground mt-1">
                    {currentTicket.description}
                  </p>
                </div>

                {currentTicket.assignedAgents.length > 0 && (
                  <div>
                    <h5 className="font-medium mb-2">Assigned Agents:</h5>
                    <div className="flex space-x-2">
                      {currentTicket.assignedAgents.map(agentId => (
                        <Badge key={agentId} variant="outline">
                          {agents.find(a => a.id === agentId)?.name}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}

                {currentTicket.responses.length > 0 && (
                  <div className="space-y-3">
                    <h5 className="font-medium">Agent Responses:</h5>
                    {currentTicket.responses.map((response, index) => (
                      <div key={index} className="bg-muted p-3 rounded-lg">
                        <div className="flex items-center space-x-2 mb-2">
                          <Badge variant="outline">
                            {agents.find(a => a.id === response.agentId)?.name}
                          </Badge>
                          <CheckCircle size={16} className="text-green-600" />
                        </div>
                        <p className="text-sm">{response.response}</p>
                      </div>
                    ))}
                  </div>
                )}

                {currentTicket.finalResponse && (
                  <div>
                    <h5 className="font-medium mb-2">Final Response:</h5>
                    <Alert>
                      <CheckCircle size={16} />
                      <AlertDescription className="whitespace-pre-line">
                        {currentTicket.finalResponse}
                      </AlertDescription>
                    </Alert>
                  </div>
                )}
              </CardContent>
            </Card>
          )}
        </div>

        {/* Recent Tickets */}
        {tickets.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Recent Tickets</CardTitle>
              <CardDescription>
                History of customer support requests and their resolution
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {tickets.slice(-5).reverse().map((ticket) => (
                  <div key={ticket.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex-1">
                      <div className="flex items-center space-x-3">
                        <span className="font-medium">#{ticket.id}</span>
                        <span>{ticket.subject}</span>
                        {getTicketStatusBadge(ticket.status)}
                      </div>
                      <p className="text-sm text-muted-foreground mt-1">
                        {ticket.customerName} • {new Date(ticket.timestamp).toLocaleString()}
                      </p>
                    </div>
                    <Button 
                      variant="outline" 
                      size="sm"
                      onClick={() => setCurrentTicket(ticket)}
                    >
                      View
                    </Button>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}

export default App