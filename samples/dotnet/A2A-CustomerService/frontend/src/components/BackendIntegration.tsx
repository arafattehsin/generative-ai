import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { ArrowClockwise, ArrowSquareOut, CheckCircle, Warning } from '@phosphor-icons/react'

// Simple backend integration component that can be added to existing App.tsx
export function BackendIntegration() {
  const [backendStatus, setBackendStatus] = useState<'checking' | 'connected' | 'disconnected'>('checking')
  const [implementationMode, setImplementationMode] = useState<'mock' | 'real' | null>(null)
  const [lastCheck, setLastCheck] = useState<string>('')

  const checkBackend = async () => {
    setBackendStatus('checking')
    try {
      // Try to fetch health status
      const healthResponse = await fetch('http://localhost:5000/health')
      if (healthResponse.ok) {
        // Try to get implementation status
        const statusResponse = await fetch('http://localhost:5000/api/customerservice/status')
        if (statusResponse.ok) {
          const statusData = await statusResponse.json()
          setImplementationMode(statusData.implementation)
          setBackendStatus('connected')
          setLastCheck(new Date().toLocaleTimeString())
        } else {
          setBackendStatus('disconnected')
        }
      } else {
        setBackendStatus('disconnected')
      }
    } catch (error) {
      setBackendStatus('disconnected')
      console.log('Backend not available:', error)
    }
  }

  const testBackendSubmission = async () => {
    try {
      const testTicket = {
        customerName: 'Test Customer',
        email: 'test@example.com',
        subject: 'Backend Integration Test',
        description: 'This is a test ticket to verify backend integration.',
        category: 'technical'
      }

      const response = await fetch('http://localhost:5000/api/customerservice/tickets', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(testTicket),
      })

      if (response.ok) {
        const ticket = await response.json()
        alert(`✅ Backend integration successful! Ticket ID: ${ticket.id}`)
      } else {
        alert('❌ Backend submission failed')
      }
    } catch (error) {
      alert('❌ Backend submission error: ' + error)
    }
  }

  useEffect(() => {
    checkBackend()
    // Check every 30 seconds
    const interval = setInterval(checkBackend, 30000)
    return () => clearInterval(interval)
  }, [])

  return (
    <Card className="border-l-4 border-l-blue-500">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="text-lg flex items-center gap-2">
              🔗 Backend API Integration
              {backendStatus === 'connected' && <CheckCircle className="h-5 w-5 text-green-500" />}
              {backendStatus === 'disconnected' && <Warning className="h-5 w-5 text-red-500" />}
            </CardTitle>
            <CardDescription>
              Real-time connection to .NET backend API
            </CardDescription>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={checkBackend}
            disabled={backendStatus === 'checking'}
          >
            <ArrowClockwise className={`h-4 w-4 mr-2 ${backendStatus === 'checking' ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <div className="text-sm font-medium">Connection Status</div>
            <Badge variant={backendStatus === 'connected' ? 'default' : 'destructive'}>
              {backendStatus === 'checking' ? 'Checking...' : 
               backendStatus === 'connected' ? 'Connected' : 'Disconnected'}
            </Badge>
          </div>
          
          {implementationMode && (
            <div>
              <div className="text-sm font-medium">Implementation</div>
              <Badge variant={implementationMode === 'real' ? 'default' : 'secondary'}>
                {implementationMode === 'real' ? '🤖 Real A2A' : '🎭 Mock Mode'}
              </Badge>
            </div>
          )}
        </div>

        {lastCheck && (
          <div className="text-xs text-muted-foreground">
            Last checked: {lastCheck}
          </div>
        )}

        <Separator />

        <div className="space-y-2">
          <div className="text-sm font-medium">Available Endpoints:</div>
          <div className="grid grid-cols-1 gap-1 text-xs">
            <div className="flex items-center gap-2">
              <Badge variant="outline">GET</Badge>
              <code>/health</code>
              <span className="text-muted-foreground">Health check</span>
            </div>
            <div className="flex items-center gap-2">
              <Badge variant="outline">POST</Badge>
              <code>/api/customerservice/tickets</code>
              <span className="text-muted-foreground">Submit ticket</span>
            </div>
            <div className="flex items-center gap-2">
              <Badge variant="outline">GET</Badge>
              <code>/api/customerservice/agents</code>
              <span className="text-muted-foreground">Get agents</span>
            </div>
          </div>
        </div>

        {backendStatus === 'connected' && (
          <div className="pt-2">
            <Button 
              onClick={testBackendSubmission}
              variant="outline" 
              size="sm"
              className="w-full"
            >
              <ArrowSquareOut className="h-4 w-4 mr-2" />
              Test Backend Submission
            </Button>
          </div>
        )}

        {backendStatus === 'disconnected' && (
          <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
            <div className="text-sm text-yellow-800">
              <strong>Backend Offline:</strong> Using mock implementation mode.
              <br />
              Start the backend server to enable real A2A processing.
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
