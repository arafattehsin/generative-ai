// API service for communicating with the A2A Customer Service backend
const API_BASE_URL = 'http://localhost:5000/api/customerservice';

export interface SubmitTicketRequest {
  customerName: string;
  email: string;
  subject: string;
  description: string;
  category: string;
}

export interface CustomerTicket {
  id: string;
  customerName: string;
  email: string;
  subject: string;
  description: string;
  category: string;
  status: number; // Backend returns enum as number: 0=New, 1=Routing, 2=Processing, 3=Completed, 4=Failed
  priority: number; // Backend returns enum as number: 0=Low, 1=Medium, 2=High, 3=Urgent
  timestamp: string;
  assignedAgents: string[];
  responses: AgentResponse[];
  finalResponse?: string;
}

export interface AgentResponse {
  agentId: string;
  agentType: string;
  response: string;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed';
  timestamp: string;
}

export interface AgentInfo {
  id: string;
  name: string;
  type: string;
  status: 'Idle' | 'Processing' | 'Completed';
  currentTicket?: string;
  description: string;
  capabilities: string[];
  iconName: string;
  color: string;
}

export interface ImplementationStatus {
  implementation: 'mock' | 'real';
  timestamp: string;
}

class A2ACustomerServiceAPI {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  // Submit a new ticket
  async submitTicket(request: SubmitTicketRequest): Promise<CustomerTicket> {
    const response = await fetch(`${this.baseUrl}/tickets`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to submit ticket: ${response.statusText}`);
    }

    return await response.json();
  }

  // Get a specific ticket by ID
  async getTicket(ticketId: string): Promise<CustomerTicket> {
    const response = await fetch(`${this.baseUrl}/tickets/${ticketId}`);
    
    if (!response.ok) {
      throw new Error(`Failed to get ticket: ${response.statusText}`);
    }

    return await response.json();
  }

  // Get all tickets
  async getAllTickets(): Promise<CustomerTicket[]> {
    const response = await fetch(`${this.baseUrl}/tickets`);
    
    if (!response.ok) {
      throw new Error(`Failed to get tickets: ${response.statusText}`);
    }

    return await response.json();
  }

  // Get all agents and their current status
  async getAgents(): Promise<AgentInfo[]> {
    const response = await fetch(`${this.baseUrl}/agents`);
    
    if (!response.ok) {
      throw new Error(`Failed to get agents: ${response.statusText}`);
    }

    return await response.json();
  }

  // Get current implementation status (mock vs real)
  async getStatus(): Promise<ImplementationStatus> {
    const response = await fetch(`${this.baseUrl}/status`);
    
    if (!response.ok) {
      throw new Error(`Failed to get status: ${response.statusText}`);
    }

    return await response.json();
  }

  // Health check
  async healthCheck(): Promise<{ status: string; timestamp: string }> {
    const response = await fetch('http://localhost:5000/health');
    
    if (!response.ok) {
      throw new Error(`Health check failed: ${response.statusText}`);
    }

    return await response.json();
  }

  // Toggle between mock and real implementations
  async toggleImplementation(useReal: boolean): Promise<ImplementationStatus> {
    const response = await fetch(`${this.baseUrl}/toggle-implementation`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ useReal }),
    });

    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.error || `Failed to toggle implementation: ${response.statusText}`);
    }

    return await response.json();
  }
}

// Export a singleton instance
export const customerServiceAPI = new A2ACustomerServiceAPI();

// Helper function to map backend status to frontend status
export function mapTicketStatus(backendStatus: string | number): 'new' | 'routing' | 'processing' | 'completed' {
  // Handle both enum numbers and string values
  if (typeof backendStatus === 'number') {
    const numericStatusMap: { [key: number]: 'new' | 'routing' | 'processing' | 'completed' } = {
      0: 'new',        // New
      1: 'routing',    // Routing
      2: 'processing', // Processing
      3: 'completed',  // Completed
      4: 'completed'   // Failed (treat as completed)
    };
    return numericStatusMap[backendStatus] || 'new';
  }
  
  // Handle string values (fallback)
  const statusMap: { [key: string]: 'new' | 'routing' | 'processing' | 'completed' } = {
    'New': 'new',
    'Routing': 'routing',
    'Processing': 'processing',
    'Completed': 'completed',
    'Failed': 'completed' // Treat failed as completed for UI purposes
  };
  
  return statusMap[backendStatus] || 'new';
}

// Helper function to map agent status
export function mapAgentStatus(backendStatus: string): 'idle' | 'processing' | 'completed' {
  const statusMap: { [key: string]: 'idle' | 'processing' | 'completed' } = {
    'Idle': 'idle',
    'Processing': 'processing',
    'Completed': 'completed'
  };
  
  return statusMap[backendStatus] || 'idle';
}
