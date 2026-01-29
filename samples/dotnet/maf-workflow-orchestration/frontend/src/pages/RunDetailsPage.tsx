import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Container,
  Title,
  Text,
  Paper,
  Stack,
  Group,
  Badge,
  Button,
  Alert,
  Loader,
  Code,
  Divider,
  Progress,
  Box,
  ThemeIcon,
  Grid,
  Tabs,
  ScrollArea,
  rem,
  CopyButton,
  ActionIcon,
  Tooltip
} from '@mantine/core';
import {
  IconArrowLeft,
  IconCheck,
  IconX,
  IconLoader2,
  IconClock,
  IconAlertCircle,
  IconSparkles,
  IconRobot,
  IconFileCheck,
  IconShieldCheck,
  IconPencil,
  IconPackage,
  IconRefresh,
  IconCopy,
  IconPlayerStop,
  IconDownload
} from '@tabler/icons-react';
import { api } from '../api/client';
import { runsHub } from '../api/signalr';
import type { StepRun } from '../types';

interface StepStatusInfo {
  message: string;
  progress: number;
  timestamp: Date;
}

interface LlmStreamData {
  stepName: string;
  chunks: string[];
}

const STEP_CONFIG: Record<string, { icon: any; gradient: string; description: string }> = {
  IntakeNormalize: {
    icon: IconFileCheck,
    gradient: 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
    description: 'Cleaning and standardizing input format'
  },
  ExtractFacts: {
    icon: IconSparkles,
    gradient: 'linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%)',
    description: 'Extracting key entities and data points'
  },
  DraftSummary: {
    icon: IconPencil,
    gradient: 'linear-gradient(135deg, #06b6d4 0%, #0891b2 100%)',
    description: 'Generating structured summary'
  },
  ComplianceCheck: {
    icon: IconShieldCheck,
    gradient: 'linear-gradient(135deg, #14b8a6 0%, #0d9488 100%)',
    description: 'Validating regulatory compliance'
  },
  BrandToneRewrite: {
    icon: IconRobot,
    gradient: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
    description: 'Adjusting voice for audience'
  },
  FinalPackage: {
    icon: IconPackage,
    gradient: 'linear-gradient(135deg, #10b981 0%, #059669 100%)',
    description: 'Assembling final deliverable'
  }
};

export function RunDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [activeStep, setActiveStep] = useState(0);
  const [stepStatuses, setStepStatuses] = useState<Record<string, StepStatusInfo>>({});
  const [llmStreams, setLlmStreams] = useState<Record<string, LlmStreamData>>({});

  const { data: run, isLoading, error } = useQuery({
    queryKey: ['run', id],
    queryFn: () => api.getRun(id!),
    enabled: !!id,
    refetchInterval: (query) => {
      const run = query.state.data;
      return run?.status === 'Running' || run?.status === 'Pending' ? 2000 : false;
    },
  });

  const cancelMutation = useMutation({
    mutationFn: () => api.cancelRun(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['run', id] });
    },
  });

  const rerunMutation = useMutation({
    mutationFn: (fromStep: string) => api.rerunFromStep(id!, { fromStep }),
    onSuccess: (data) => {
      navigate(`/runs/${data.newRunId}`);
    },
  });

  useEffect(() => {
    if (!id) return;

    runsHub.start().then(() => {
      runsHub.joinRun(id);
    });

    const handleStepStarted = () => {
      queryClient.invalidateQueries({ queryKey: ['run', id] });
    };

    const handleStepCompleted = () => {
      queryClient.invalidateQueries({ queryKey: ['run', id] });
    };

    const handleStepFailed = () => {
      queryClient.invalidateQueries({ queryKey: ['run', id] });
    };

    const handleRunCompleted = () => {
      queryClient.invalidateQueries({ queryKey: ['run', id] });
    };

    const handleStepStatus = (data: { stepName: string; statusMessage: string; progressPercent: number }) => {
      setStepStatuses(prev => ({
        ...prev,
        [data.stepName]: {
          message: data.statusMessage,
          progress: data.progressPercent ?? 0,
          timestamp: new Date()
        }
      }));
    };

    const handleLlmStream = (data: { stepName: string; partialText: string }) => {
      setLlmStreams(prev => {
        const existing = prev[data.stepName] || { stepName: data.stepName, chunks: [] };
        return {
          ...prev,
          [data.stepName]: {
            ...existing,
            chunks: [...existing.chunks, data.partialText]
          }
        };
      });
    };

    runsHub.on('StepStarted', handleStepStarted);
    runsHub.on('StepCompleted', handleStepCompleted);
    runsHub.on('StepFailed', handleStepFailed);
    runsHub.on('RunCompleted', handleRunCompleted);
    runsHub.on('StepStatus', handleStepStatus);
    runsHub.on('LlmStream', handleLlmStream);

    return () => {
      runsHub.off('StepStarted', handleStepStarted);
      runsHub.off('StepCompleted', handleStepCompleted);
      runsHub.off('StepFailed', handleStepFailed);
      runsHub.off('RunCompleted', handleRunCompleted);
      runsHub.off('StepStatus', handleStepStatus);
      runsHub.off('LlmStream', handleLlmStream);
      runsHub.leaveRun(id);
    };
  }, [id, queryClient]);

  useEffect(() => {
    if (run?.steps) {
      // Find last completed index
      let lastCompletedIndex = -1;
      for (let i = run.steps.length - 1; i >= 0; i--) {
        if (run.steps[i].status === 'Completed') {
          lastCompletedIndex = i;
          break;
        }
      }
      const runningIndex = run.steps.findIndex((s: StepRun) => s.status === 'Running');
      setActiveStep(runningIndex >= 0 ? runningIndex : lastCompletedIndex + 1);
    }
  }, [run?.steps]);

  if (isLoading) {
    return (
      <Container size="xl">
        <Box
          style={{
            minHeight: '60vh',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
        >
          <Stack align="center" gap="lg">
            <Box
              style={{
                width: 64,
                height: 64,
                borderRadius: 16,
                background: 'var(--gradient-primary)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                animation: 'pulse 2s ease-in-out infinite',
              }}
            >
              <IconSparkles size={32} color="white" />
            </Box>
            <Stack align="center" gap="xs">
              <Text size="lg" fw={600} style={{ color: 'white' }}>Initializing Workflow</Text>
              <Text size="sm" style={{ color: 'var(--color-text-muted)' }}>Loading execution details...</Text>
            </Stack>
          </Stack>
        </Box>
      </Container>
    );
  }

  if (error || !run) {
    return (
      <Container size="lg" py="xl">
        <Alert 
          icon={<IconAlertCircle size={20} />} 
          color="red" 
          title="Failed to Load Run"
          radius="lg"
          styles={{
            root: { background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.3)' },
            title: { color: '#ef4444' },
            message: { color: 'var(--color-text-secondary)' }
          }}
        >
          {error instanceof Error ? error.message : 'Unknown error occurred'}
        </Alert>
      </Container>
    );
  }

  const completedSteps = run.steps.filter(s => s.status === 'Completed').length;
  const progressPercent = (completedSteps / run.steps.length) * 100;

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Pending: 'gray',
      Running: 'blue',
      Completed: 'teal',
      Failed: 'red',
      Canceled: 'orange'
    };
    return colors[status] || 'gray';
  };

  return (
    <Container size="xl">
      <Stack gap="xl" className="animate-fadeIn">
        {/* Header */}
        <Group justify="space-between" align="flex-start">
          <Group gap="md">
            <Button
              leftSection={<IconArrowLeft size={16} />}
              variant="subtle"
              color="gray"
              onClick={() => navigate('/')}
              styles={{
                root: { color: 'var(--color-text-secondary)' }
              }}
            >
              Back to Home
            </Button>
          </Group>
          
          <Group gap="sm">
            {run.status === 'Running' && (
              <Button
                leftSection={<IconPlayerStop size={16} />}
                variant="light"
                color="red"
                onClick={() => cancelMutation.mutate()}
                loading={cancelMutation.isPending}
              >
                Cancel
              </Button>
            )}
            <CopyButton value={run.id}>
              {({ copied, copy }) => (
                <Tooltip label={copied ? 'Copied!' : 'Copy Run ID'}>
                  <Button
                    variant="subtle"
                    color="gray"
                    size="sm"
                    onClick={copy}
                    rightSection={<IconCopy size={14} />}
                    styles={{
                      root: { color: 'var(--color-text-muted)', fontFamily: 'monospace', fontSize: rem(12) }
                    }}
                  >
                    {run.id.substring(0, 8)}...
                  </Button>
                </Tooltip>
              )}
            </CopyButton>
          </Group>
        </Group>

        {/* Status Header Card */}
        <Paper className="glass-card" p="xl" radius="lg">
          <Grid gutter="xl" align="center">
            <Grid.Col span={{ base: 12, md: 8 }}>
              <Group gap="lg" align="flex-start">
                <Box
                  style={{
                    width: 56,
                    height: 56,
                    borderRadius: 14,
                    background: run.status === 'Completed' 
                      ? 'var(--gradient-success)' 
                      : run.status === 'Running' 
                        ? 'var(--gradient-primary)'
                        : run.status === 'Failed'
                          ? 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)'
                          : 'linear-gradient(135deg, #64748b 0%, #475569 100%)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    boxShadow: run.status === 'Running' ? 'var(--shadow-glow-blue)' : undefined,
                  }}
                  className={run.status === 'Running' ? 'animate-pulse' : ''}
                >
                  {run.status === 'Completed' ? (
                    <IconCheck size={28} color="white" />
                  ) : run.status === 'Running' ? (
                    <IconLoader2 size={28} color="white" className="animate-spin" />
                  ) : run.status === 'Failed' ? (
                    <IconX size={28} color="white" />
                  ) : (
                    <IconClock size={28} color="white" />
                  )}
                </Box>
                
                <Box style={{ flex: 1 }}>
                  <Group gap="md" mb="xs">
                    <Title order={2} style={{ color: 'white', letterSpacing: '-0.5px' }}>
                      Workflow {run.status}
                    </Title>
                    <Badge 
                      size="lg" 
                      color={getStatusColor(run.status)} 
                      variant="light"
                      radius="md"
                    >
                      {completedSteps}/{run.steps.length} Steps
                    </Badge>
                  </Group>
                  
                  <Group gap="lg">
                    {run.options && (
                      <>
                        <Group gap={4}>
                          <Text size="sm" style={{ color: 'var(--color-text-muted)' }}>Audience:</Text>
                          <Text size="sm" fw={500} style={{ color: 'var(--color-text-secondary)' }}>{run.options.audience}</Text>
                        </Group>
                        <Group gap={4}>
                          <Text size="sm" style={{ color: 'var(--color-text-muted)' }}>Tone:</Text>
                          <Text size="sm" fw={500} style={{ color: 'var(--color-text-secondary)' }}>{run.options.tone}</Text>
                        </Group>
                        {run.options.strictCompliance && (
                          <Badge size="sm" color="orange" variant="light">Strict Compliance</Badge>
                        )}
                      </>
                    )}
                    {run.totalDurationMs && (
                      <Group gap={4}>
                        <Text size="sm" style={{ color: 'var(--color-text-muted)' }}>Duration:</Text>
                        <Text size="sm" fw={500} style={{ color: 'var(--color-accent-teal)' }}>
                          {(run.totalDurationMs / 1000).toFixed(2)}s
                        </Text>
                      </Group>
                    )}
                  </Group>
                </Box>
              </Group>
            </Grid.Col>
            
            <Grid.Col span={{ base: 12, md: 4 }}>
              <Box>
                <Group justify="space-between" mb="xs">
                  <Text size="sm" style={{ color: 'var(--color-text-muted)' }}>Progress</Text>
                  <Text size="sm" fw={600} style={{ color: 'white' }}>{Math.round(progressPercent)}%</Text>
                </Group>
                <Progress 
                  value={progressPercent} 
                  size="lg" 
                  radius="xl"
                  animated={run.status === 'Running'}
                />
              </Box>
            </Grid.Col>
          </Grid>
          
          {run.error && (
            <Alert 
              icon={<IconAlertCircle size={18} />} 
              color="red" 
              mt="lg"
              radius="md"
              styles={{
                root: { background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.2)' },
                message: { color: 'var(--color-text-secondary)' }
              }}
            >
              {run.error}
            </Alert>
          )}
        </Paper>

        {/* Main Content Grid */}
        <Grid gutter="xl">
          {/* Steps Panel */}
          <Grid.Col span={{ base: 12, lg: 5 }}>
            <Paper className="glass-card" p="lg" radius="lg">
              <Text 
                size="sm" 
                fw={600} 
                mb="lg"
                style={{ color: 'var(--color-text-muted)', textTransform: 'uppercase', letterSpacing: 1 }}
              >
                Execution Pipeline
              </Text>
              
              <Stack gap="sm">
                {run.steps.map((step, index) => {
                  const config = STEP_CONFIG[step.stepName] || { 
                    icon: IconClock, 
                    gradient: 'linear-gradient(135deg, #64748b 0%, #475569 100%)',
                    description: ''
                  };
                  const StepIcon = config.icon;
                  const isActive = step.status === 'Running';
                  const isCompleted = step.status === 'Completed';
                  const isFailed = step.status === 'Failed';
                  
                  return (
                    <Box
                      key={step.id}
                      className="glass-card"
                      p="md"
                      style={{
                        cursor: isCompleted ? 'pointer' : 'default',
                        borderColor: isActive ? 'var(--color-accent-blue)' : undefined,
                        boxShadow: isActive ? 'var(--shadow-glow-blue)' : undefined,
                      }}
                      onClick={() => isCompleted && setActiveStep(index)}
                    >
                      <Group gap="md" wrap="nowrap">
                        <Box
                          style={{
                            width: 44,
                            height: 44,
                            borderRadius: 12,
                            background: isCompleted 
                              ? config.gradient 
                              : isFailed 
                                ? 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)'
                                : isActive
                                  ? config.gradient
                                  : 'rgba(255, 255, 255, 0.05)',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            flexShrink: 0,
                            opacity: !isCompleted && !isActive && !isFailed ? 0.5 : 1,
                          }}
                          className={isActive ? 'animate-pulse' : ''}
                        >
                          {isCompleted ? (
                            <IconCheck size={22} color="white" />
                          ) : isFailed ? (
                            <IconX size={22} color="white" />
                          ) : isActive ? (
                            <IconLoader2 size={22} color="white" className="animate-spin" />
                          ) : (
                            <StepIcon size={22} color="white" />
                          )}
                        </Box>
                        
                        <Box style={{ flex: 1, minWidth: 0 }}>
                          <Group justify="space-between" mb={2}>
                            <Text fw={600} size="sm" style={{ color: 'white' }}>
                              {step.stepName.replace(/([A-Z])/g, ' $1').trim()}
                            </Text>
                            <Text 
                              size="xs"
                              fw={500}
                              style={{ 
                                color: isCompleted 
                                  ? 'var(--color-accent-teal)' 
                                  : isFailed 
                                    ? 'var(--color-accent-red)'
                                    : isActive
                                      ? 'var(--color-accent-blue)'
                                      : 'var(--color-text-muted)'
                              }}
                            >
                              {step.durationMs ? `${(step.durationMs / 1000).toFixed(1)}s` : step.status}
                            </Text>
                          </Group>
                          <Text size="xs" style={{ color: 'var(--color-text-muted)' }}>
                            {config.description}
                          </Text>
                          
                          {/* Running step progress */}
                          {isActive && stepStatuses[step.stepName] && (
                            <Box mt="sm">
                              <Group justify="space-between" mb={4}>
                                <Text size="xs" style={{ color: 'var(--color-text-secondary)' }}>
                                  {stepStatuses[step.stepName].message}
                                </Text>
                                <Text size="xs" fw={500} style={{ color: 'var(--color-accent-blue)' }}>
                                  {stepStatuses[step.stepName].progress}%
                                </Text>
                              </Group>
                              <Progress 
                                value={stepStatuses[step.stepName].progress} 
                                size="xs" 
                                radius="xl"
                                animated 
                              />
                            </Box>
                          )}
                        </Box>
                      </Group>
                    </Box>
                  );
                })}
              </Stack>
            </Paper>
          </Grid.Col>

          {/* Output Panel */}
          <Grid.Col span={{ base: 12, lg: 7 }}>
            <Stack gap="lg">
              {/* Selected Step Detail or Live Stream */}
              {run.steps[activeStep] && (run.steps[activeStep].status === 'Completed' || run.steps[activeStep].status === 'Running') && (
                <Paper className="glass-card" p="lg" radius="lg">
                  <Group justify="space-between" mb="md">
                    <Group gap="sm">
                      <Text size="sm" fw={600} style={{ color: 'white' }}>
                        {run.steps[activeStep].stepName.replace(/([A-Z])/g, ' $1').trim()}
                      </Text>
                      <Badge size="sm" color={getStatusColor(run.steps[activeStep].status)} variant="light">
                        {run.steps[activeStep].status}
                      </Badge>
                    </Group>
                    
                    {run.steps[activeStep].status === 'Completed' && (
                      <Button
                        leftSection={<IconRefresh size={14} />}
                        variant="subtle"
                        size="xs"
                        color="blue"
                        onClick={() => rerunMutation.mutate(run.steps[activeStep].stepName)}
                        disabled={run.status === 'Running'}
                        loading={rerunMutation.isPending}
                      >
                        Re-run from here
                      </Button>
                    )}
                  </Group>
                  
                  {/* Live LLM Stream for running step */}
                  {run.steps[activeStep].status === 'Running' && llmStreams[run.steps[activeStep].stepName] && (
                    <Box>
                      <Text size="xs" fw={600} mb="xs" style={{ color: 'var(--color-accent-blue)' }}>
                        ● LIVE OUTPUT
                      </Text>
                      <ScrollArea h={300} type="auto">
                        <Code 
                          block 
                          style={{ 
                            fontSize: 12, 
                            background: 'rgba(15, 23, 42, 0.8)',
                            border: '1px solid var(--color-card-border)',
                            whiteSpace: 'pre-wrap',
                            wordBreak: 'break-word'
                          }}
                        >
                          {llmStreams[run.steps[activeStep].stepName].chunks.join('')}
                        </Code>
                      </ScrollArea>
                    </Box>
                  )}
                  
                  {/* Completed step output tabs */}
                  {run.steps[activeStep].status === 'Completed' && (run.steps[activeStep].outputSnapshot || run.steps[activeStep].inputSnapshot) && (
                    <Tabs defaultValue="output" variant="pills" radius="md">
                      <Tabs.List mb="md">
                        {run.steps[activeStep].outputSnapshot && (
                          <Tabs.Tab value="output" leftSection={<IconPackage size={14} />}>
                            Output
                          </Tabs.Tab>
                        )}
                        {run.steps[activeStep].inputSnapshot && (
                          <Tabs.Tab value="input" leftSection={<IconFileCheck size={14} />}>
                            Input
                          </Tabs.Tab>
                        )}
                      </Tabs.List>

                      {run.steps[activeStep].outputSnapshot && (
                        <Tabs.Panel value="output">
                          <ScrollArea h={350} type="auto">
                            <Code 
                              block 
                              style={{ 
                                fontSize: 12, 
                                background: 'rgba(15, 23, 42, 0.8)',
                                border: '1px solid var(--color-card-border)',
                                whiteSpace: 'pre-wrap',
                                wordBreak: 'break-word'
                              }}
                            >
                              {run.steps[activeStep].outputSnapshot}
                            </Code>
                          </ScrollArea>
                          {run.steps[activeStep].outputIsTruncated && (
                            <Text size="xs" mt="xs" style={{ color: 'var(--color-text-muted)' }}>
                              ⚠️ Truncated (full: {run.steps[activeStep].outputFullLength?.toLocaleString()} chars)
                            </Text>
                          )}
                        </Tabs.Panel>
                      )}

                      {run.steps[activeStep].inputSnapshot && (
                        <Tabs.Panel value="input">
                          <ScrollArea h={350} type="auto">
                            <Code 
                              block 
                              style={{ 
                                fontSize: 12, 
                                background: 'rgba(15, 23, 42, 0.8)',
                                border: '1px solid var(--color-card-border)',
                                whiteSpace: 'pre-wrap',
                                wordBreak: 'break-word'
                              }}
                            >
                              {run.steps[activeStep].inputSnapshot}
                            </Code>
                          </ScrollArea>
                          {run.steps[activeStep].inputIsTruncated && (
                            <Text size="xs" mt="xs" style={{ color: 'var(--color-text-muted)' }}>
                              ⚠️ Truncated (full: {run.steps[activeStep].inputFullLength?.toLocaleString()} chars)
                            </Text>
                          )}
                        </Tabs.Panel>
                      )}
                    </Tabs>
                  )}
                </Paper>
              )}

              {/* Final Output Card */}
              {run.finalOutputHtml && (
                <Paper 
                  className="glass-card" 
                  p="lg" 
                  radius="lg"
                  style={{
                    borderColor: 'rgba(16, 185, 129, 0.3)',
                  }}
                >
                  <Group justify="space-between" mb="md">
                    <Group gap="sm">
                      <Box
                        style={{
                          width: 36,
                          height: 36,
                          borderRadius: 10,
                          background: 'var(--gradient-success)',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        <IconPackage size={20} color="white" />
                      </Box>
                      <Box>
                        <Text fw={600} style={{ color: 'white' }}>Final Deliverable</Text>
                        <Text size="xs" style={{ color: 'var(--color-text-muted)' }}>
                          Ready for export
                        </Text>
                      </Box>
                    </Group>
                    
                    <Button
                      leftSection={<IconDownload size={16} />}
                      variant="light"
                      color="teal"
                      size="sm"
                    >
                      Export HTML
                    </Button>
                  </Group>
                  
                  <Box 
                    p="lg" 
                    style={{ 
                      borderRadius: 12, 
                      background: 'rgba(255, 255, 255, 0.95)',
                      color: '#1e293b'
                    }}
                    dangerouslySetInnerHTML={{ __html: run.finalOutputHtml }} 
                  />
                </Paper>
              )}

              {/* Waiting state */}
              {!run.finalOutputHtml && run.status !== 'Completed' && !run.steps[activeStep]?.outputSnapshot && (
                <Paper className="glass-card" p="xl" radius="lg">
                  <Stack align="center" py="xl">
                    <Loader type="dots" color="blue" size="lg" />
                    <Text size="sm" style={{ color: 'var(--color-text-muted)' }}>
                      Waiting for workflow to complete...
                    </Text>
                  </Stack>
                </Paper>
              )}
            </Stack>
          </Grid.Col>
        </Grid>
      </Stack>
    </Container>
  );
}
