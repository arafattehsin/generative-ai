import { useState, useEffect, useCallback } from 'react'
import { customerServiceAPI, mapTicketStatus, mapAgentStatus, type CustomerTicket, type AgentInfo } from '@/services/api'

// Custom hook for managing backend API integration
export function useCustomerServiceAPI() {
  const [isConnected, setIsConnected] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Check connectivity
  const checkConnection = useCallback(async () => {
    try {
      await customerServiceAPI.healthCheck()
      setIsConnected(true)
      setError(null)
      return true
    } catch (err) {
      setIsConnected(false)
      setError(err instanceof Error ? err.message : 'Connection failed')
      return false
    }
  }, [])

  // Submit ticket with automatic fallback
  const submitTicket = useCallback(async (ticketData: {
    customerName: string
    email: string
    subject: string
    description: string
    category?: string
  }) => {
    try {
      setIsLoading(true)
      const backendTicket = await customerServiceAPI.submitTicket({
        ...ticketData,
        category: ticketData.category || 'general'
      })
      
      // Convert backend format to frontend format
      return {
        id: backendTicket.id,
        customerName: backendTicket.customerName,
        email: backendTicket.email,
        subject: backendTicket.subject,
        description: backendTicket.description,
        timestamp: backendTicket.timestamp,
        status: mapTicketStatus(backendTicket.status),
        assignedAgents: backendTicket.assignedAgents,
        responses: backendTicket.responses.map(r => ({
          agentId: r.agentId,
          response: r.response,
          timestamp: r.timestamp,
          status: r.status === 'Completed' ? 'completed' as const : 'pending' as const
        })),
        finalResponse: backendTicket.finalResponse
      }
    } catch (err) {
      console.error('Backend submission failed:', err)
      throw err
    } finally {
      setIsLoading(false)
    }
  }, [])

  // Get updated ticket
  const getTicket = useCallback(async (ticketId: string) => {
    try {
      const backendTicket = await customerServiceAPI.getTicket(ticketId)
      return {
        id: backendTicket.id,
        customerName: backendTicket.customerName,
        email: backendTicket.email,
        subject: backendTicket.subject,
        description: backendTicket.description,
        timestamp: backendTicket.timestamp,
        status: mapTicketStatus(backendTicket.status),
        assignedAgents: backendTicket.assignedAgents,
        responses: backendTicket.responses.map(r => ({
          agentId: r.agentId,
          response: r.response,
          timestamp: r.timestamp,
          status: r.status === 'Completed' ? 'completed' as const : 'pending' as const
        })),
        finalResponse: backendTicket.finalResponse
      }
    } catch (err) {
      console.error('Failed to get ticket:', err)
      throw err
    }
  }, [])

  // Get agents status
  const getAgents = useCallback(async () => {
    try {
      const backendAgents = await customerServiceAPI.getAgents()
      return backendAgents.map(agent => ({
        id: agent.id,
        name: agent.name,
        type: agent.type as 'front-desk' | 'billing' | 'technical',
        status: mapAgentStatus(agent.status),
        currentTicket: agent.currentTicket,
        description: agent.description
      }))
    } catch (err) {
      console.error('Failed to get agents:', err)
      throw err
    }
  }, [])

  // Initialize connection check
  useEffect(() => {
    const init = async () => {
      setIsLoading(true)
      await checkConnection()
      setIsLoading(false)
    }
    init()
  }, [checkConnection])

  return {
    isConnected,
    isLoading,
    error,
    checkConnection,
    submitTicket,
    getTicket,
    getAgents
  }
}
