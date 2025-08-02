import { useState, useEffect } from 'react'
import { useKV } from '@/lib/spark-hooks'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Separator } from '@/components/ui/separator'
import { User, CreditCard, Wrench, PaperPlaneTilt, Clock, CheckCircle, ArrowRight } from '@phosphor-icons/react'
import { toast } from 'sonner'

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
  type: 'front-desk' | 'billing' | 'tech'
  status: 'idle' | 'processing' | 'completed'
  currentTicket?: string
  icon: any
  color: string
  description: string
}

const AGENTS: Agent[] = [
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
    id: 'tech',
    name: 'Tech Support Agent',
    type: 'tech',
    status: 'idle',
    icon: Wrench,
    color: 'agent-tech',
    description: 'Resolves technical problems'
  }
]

function App() {
  const [tickets, setTickets] = useKV<Ticket[]>('customer-tickets', [])
  const [agents, setAgents] = useState<Agent[]>(AGENTS)
  const [currentTicket, setCurrentTicket] = useState<Ticket | null>(null)
  
  // Form state
  const [customerName, setCustomerName] = useState('')
  const [email, setEmail] = useState('')
  const [subject, setSubject] = useState('')
  const [description, setDescription] = useState('')

  const analyzeTicket = async (ticket: Ticket): Promise<string[]> => {
    const prompt = spark.llmPrompt`
    Analyze this customer support ticket and determine which agents should handle it.
    
    Subject: ${ticket.subject}
    Description: ${ticket.description}
    
    Available agents:
    - billing: handles billing, payments, account issues, charges, refunds
    - tech: handles technical issues, login problems, software bugs, system errors
    
    Return only the agent IDs that should handle this ticket as a comma-separated list.
    If it involves multiple areas, include multiple agents.
    Examples: "billing", "tech", "billing,tech"
    `
    
    const result = await spark.llm(prompt, 'gpt-4o-mini')
    return result.split(',').map(s => s.trim()).filter(s => ['billing', 'tech'].includes(s))
  }

  const generateAgentResponse = async (ticket: Ticket, agentType: string): Promise<string> => {
    const agentContext = {
      billing: 'You are a billing specialist. Focus on payment issues, account charges, refunds, and billing inquiries.',
      tech: 'You are a technical support specialist. Focus on login issues, software problems, system errors, and technical troubleshooting.'
    }

    const prompt = spark.llmPrompt`
    ${agentContext[agentType as keyof typeof agentContext]}
    
    Customer Issue:
    Subject: ${ticket.subject}
    Description: ${ticket.description}
    
    Provide a helpful response addressing the ${agentType} aspects of this issue. Be specific and actionable.
    Keep your response concise (2-3 sentences) as it will be combined with other agent responses.
    `
    
    return await spark.llm(prompt, 'gpt-4o-mini')
  }

  const aggregateResponses = async (ticket: Ticket): Promise<string> => {
    const responses = ticket.responses.map(r => `${r.agentId.toUpperCase()}: ${r.response}`).join('\n\n')
    
    const prompt = spark.llmPrompt`
    Combine these specialist responses into a single, coherent customer service reply:
    
    ${responses}
    
    Create a unified response that:
    1. Addresses the customer by name (${ticket.customerName})
    2. Acknowledges their issue clearly
    3. Combines the specialist insights smoothly
    4. Provides clear next steps
    5. Maintains a professional, helpful tone
    
    Start with "Dear ${ticket.customerName}," and sign off with "Best regards, Customer Service Team"
    `
    
    return await spark.llm(prompt, 'gpt-4o-mini')
  }

  const submitTicket = async () => {
    if (!customerName || !email || !subject || !description) {
      toast.error('Please fill in all fields')
      return
    }

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
    
    // Clear form
    setCustomerName('')
    setEmail('')
    setSubject('')
    setDescription('')
    
    toast.success('Support ticket submitted successfully!')
    
    // Start processing
    processTicket(newTicket)
  }

  const processTicket = async (ticket: Ticket) => {
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
      const assignedAgentIds = await analyzeTicket(ticket)
      
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
        // Simulate processing time
        await new Promise(resolve => setTimeout(resolve, 1500))
        
        const response = await generateAgentResponse(ticket, agentId)
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

      // Simulate aggregation time
      await new Promise(resolve => setTimeout(resolve, 1000))

      // Generate final response
      const finalResponse = await aggregateResponses({
        ...ticket,
        responses
      })

      // Complete the ticket
      setTickets(current => 
        current.map(t => 
          t.id === ticket.id 
            ? { ...t, status: 'completed', finalResponse }
            : t
        )
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

        {/* Agent Dashboard */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
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