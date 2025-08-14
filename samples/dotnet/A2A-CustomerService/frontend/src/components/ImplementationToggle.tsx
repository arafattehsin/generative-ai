import { useState, useEffect } from 'react';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { customerServiceAPI, ImplementationStatus } from '@/services/api';
import { ArrowClockwise, Robot, Users } from '@phosphor-icons/react';

interface ImplementationToggleProps {
  onImplementationChange?: (isReal: boolean) => void;
}

export function ImplementationToggle({ onImplementationChange }: ImplementationToggleProps) {
  const [status, setStatus] = useState<ImplementationStatus | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isToggling, setIsToggling] = useState(false);
  const [isHealthy, setIsHealthy] = useState(false);

  const checkStatus = async () => {
    try {
      setIsLoading(true);
      
      // Check health first
      const healthResponse = await customerServiceAPI.healthCheck();
      setIsHealthy(healthResponse.status === 'healthy');
      
      // Get implementation status
      const statusResponse = await customerServiceAPI.getStatus();
      setStatus(statusResponse);
      
      // Notify parent component
      onImplementationChange?.(statusResponse.implementation === 'real');
    } catch (error) {
      console.error('Failed to check status:', error);
      setIsHealthy(false);
      setStatus(null);
    } finally {
      setIsLoading(false);
    }
  };

  const handleToggle = async (useReal: boolean) => {
    try {
      setIsToggling(true);
      
      const newStatus = await customerServiceAPI.toggleImplementation(useReal);
      setStatus(newStatus);
      
      // Notify parent component
      onImplementationChange?.(newStatus.implementation === 'real');
      
      // Success feedback
      const mode = newStatus.implementation === 'real' ? 'Real A2A' : 'Mock';
      // You can add a toast notification here if you have a toast system
      console.log(`Successfully switched to ${mode} mode`);
      
    } catch (error) {
      console.error('Failed to toggle implementation:', error);
      
      // Show error to user (you can replace with proper error handling)
      alert(`Failed to toggle implementation: ${error}`);
      
      // Refresh status to ensure UI is in sync
      await checkStatus();
    } finally {
      setIsToggling(false);
    }
  };

  useEffect(() => {
    checkStatus();
    
    // Check status every 30 seconds
    const interval = setInterval(checkStatus, 30000);
    return () => clearInterval(interval);
  }, []);

  const isRealImplementation = status?.implementation === 'real';

  return (
    <Card className="mb-6">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Robot className="h-5 w-5" />
            <CardTitle className="text-lg">Implementation Mode</CardTitle>
          </div>
          <button
            onClick={checkStatus}
            disabled={isLoading}
            className="p-1 hover:bg-gray-100 rounded-md transition-colors"
            title="Refresh status"
            aria-label="Refresh implementation status"
          >
            <ArrowClockwise className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
          </button>
        </div>
        <CardDescription>
          Toggle between mock and real A2A (Agent-to-Agent) implementations
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Backend Health Status */}
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">Backend Status:</span>
            <Badge variant={isHealthy ? 'default' : 'destructive'}>
              {isHealthy ? 'Healthy' : 'Offline'}
            </Badge>
          </div>

          {/* Implementation Status */}
          {status && (
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium">Current Implementation:</span>
              <div className="flex items-center gap-2">
                <Badge variant={isRealImplementation ? 'default' : 'secondary'}>
                  {isRealImplementation ? (
                    <div className="flex items-center gap-1">
                      <Users className="h-3 w-3" />
                      Real A2A
                    </div>
                  ) : (
                    <div className="flex items-center gap-1">
                      <Robot className="h-3 w-3" />
                      Mock
                    </div>
                  )}
                </Badge>
              </div>
            </div>
          )}

          {/* Toggle Switch (Note: Currently read-only as backend controls this) */}
          <div className="pt-2 border-t">
            <div className="flex items-center justify-between">
              <div className="space-y-1">
                <div className="text-sm font-medium">
                  {isRealImplementation ? 'Real A2A Mode' : 'Mock Mode'}
                </div>
                <div className="text-xs text-muted-foreground">
                  {isRealImplementation 
                    ? 'Using Azure OpenAI and real Agent-to-Agent communication'
                    : 'Using simulated responses with local mock implementation'
                  }
                </div>
              </div>
              <Switch
                checked={isRealImplementation}
                disabled={isToggling || !isHealthy}
                onCheckedChange={handleToggle}
                aria-label="Toggle implementation mode"
              />
            </div>
            <div className="text-xs text-muted-foreground mt-2">
              {isToggling 
                ? '🔄 Switching implementation...' 
                : !isHealthy 
                  ? '⚠️ Backend offline - toggle disabled'
                  : '💡 Click to switch between mock and real A2A implementations'
              }
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
