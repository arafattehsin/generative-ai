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
  Timeline,
  ThemeIcon,
  Grid,
  Tabs,
  ScrollArea,
  rem
} from '@mantine/core';
import {
  IconArrowLeft,
  IconCheck,
  IconX,
  IconLoader,
  IconClock,
  IconAlertCircle,
  IconSparkles,
  IconRobot,
  IconFileCheck,
  IconShieldCheck,
  IconPencil,
  IconPackage,
  IconRefresh
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

// Define animations as CSS strings
const animationsCSS = `
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes shimmer {
  0% { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}
`;

// Inject animations into head
if (typeof document !== 'undefined' && !document.getElementById('workflow-animations')) {
  const style = document.createElement('style');
  style.id = 'workflow-animations';
  style.textContent = animationsCSS;
  document.head.appendChild(style);
}

const STEP_ICONS: Record<string, any> = {
  IntakeNormalize: IconFileCheck,
  ExtractFacts: IconSparkles,
  DraftSummary: IconPencil,
  ComplianceCheck: IconShieldCheck,
  BrandToneRewrite: IconRobot,
  FinalPackage: IconPackage,
};

const STEP_DESCRIPTIONS: Record<string, string> = {
  IntakeNormalize: "Cleaning and standardizing the input text, removing formatting issues and normalizing content structure",
  ExtractFacts: "Identifying and extracting key facts, dates, names, policies, and critical information from the document",
  DraftSummary: "Creating a structured summary of the content with organized sections and clear hierarchy",
  ComplianceCheck: "Analyzing the content for compliance issues, legal language gaps, and regulatory requirements",
  BrandToneRewrite: "Rewriting the content to match the target audience and desired professional tone",
  FinalPackage: "Assembling the final polished document with all refinements and formatting complete"
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
      <Box
        style={{
          minHeight: '100vh',
          background: '#E2E8F0',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center'
        }}
      >
        <Stack align="center" gap="md">
          <Loader size="xl" color="blue" type="bars" />
          <Text size="lg" c="dimmed" fw={500}>Initializing Workflow...</Text>
        </Stack>
      </Box>
    );
  }

  if (error || !run) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle size={16} />} color="red" title="System Error" variant="filled">
          Failed to load run details: {error instanceof Error ? error.message : 'Unknown error'}
        </Alert>
      </Container>
    );
  }

  const getStatusBadge = (status: string) => {
    const configs: Record<string, { color: string }> = {
      Pending: { color: 'gray' },
      Running: { color: 'blue' },
      Completed: { color: 'green' },
      Failed: { color: 'red' },
      Canceled: { color: 'orange' }
    };
    
    const config = configs[status] || { color: 'gray' };
    
    return (
      <Badge 
        size="lg" 
        color={config.color} 
        variant="light"
        radius="sm"
      >
        {status}
      </Badge>
    );
  };

  const getTimelineColor = (status: string) => {
    if (status === 'Completed') return 'teal';
    if (status === 'Failed') return 'red';
    if (status === 'Running') return 'blue';
    return 'gray';
  };

  const getTimelineIcon = (step: StepRun) => {
    const IconComponent = STEP_ICONS[step.stepName] || IconClock;
    
    if (step.status === 'Completed') return <IconCheck size={18} />;
    if (step.status === 'Failed') return <IconX size={18} />;
    if (step.status === 'Running')return <IconLoader size={18} className="spin" />;
    return <IconComponent size={18} />;
  };

  return (
    <Box
      style={{
        minHeight: '100vh',
        background: '#E2E8F0',
        paddingTop: rem(40),
        paddingBottom: rem(60)
      }}
    >
      <Container size="xl">
        <Stack gap="xl">
          {/* Header */}
          <Group justify="space-between">
            <Button
              leftSection={<IconArrowLeft size={16} />}
              variant="default"
              onClick={() => navigate('/')}
            >
              Back to Dashboard
            </Button>
            <Text size="sm" c="dimmed">
              Run ID: <Code>{run.id}</Code>
            </Text>
          </Group>

          {/* Main Card */}
          <Paper p="xl" className="modern-card">
            <Stack gap="lg">
              {/* Run Header */}
              <Group justify="space-between" wrap="nowrap" align="flex-start">
                <div>
                  <Group gap="md" mb="xs" align="center">
                    <Title order={2} c="dark.9" style={{ lineHeight: 1 }}>
                      Workflow Status
                    </Title>
                    {getStatusBadge(run.status)}
                  </Group>
                  <Text c="dimmed">
                    Started at {new Date().toLocaleTimeString()} • {run.steps.length} Steps configuration
                  </Text>
                </div>
                {run.status === 'Running' && (
                  <Button
                    color="red"
                    variant="subtle"
                    onClick={() => cancelMutation.mutate()}
                    loading={cancelMutation.isPending}
                  >
                    Cancel Execution
                  </Button>
                )}
              </Group>

              <Divider />

              {/* Metadata */}
              {run.options && (
                <Group gap="sm">
                  <Badge size="md" variant="outline" color="dark" radius="sm">
                    Target: {run.options.audience}
                  </Badge>
                  <Badge size="md" variant="outline" color="dark" radius="sm">
                    Tone: {run.options.tone}
                  </Badge>
                  {run.options.strictCompliance && (
                    <Badge size="md" variant="filled" color="orange" radius="sm">
                      Strict Compliance
                    </Badge>
                  )}
                </Group>
              )}

              {run.totalDurationMs && (
                <Alert color="teal" variant="light" title="Execution Complete">
                   Workflow finished successfully in {(run.totalDurationMs / 1000).toFixed(2)} seconds.
                </Alert>
              )}

              {run.error && (
                <Alert icon={<IconAlertCircle size={20} />} color="red" title="Run Failed">
                  {run.error}
                </Alert>
              )}
            </Stack>
          </Paper>

          <Grid gutter="xl">
            <Grid.Col span={{ base: 12, md: 7 }}>
              {/* Workflow Timeline */}
              <Paper p="xl" className="modern-card">
                <Title order={4} mb="xl" c="dark.9">Execution Log</Title>

                <Timeline active={activeStep} bulletSize={32} lineWidth={2}>
                  {run.steps.map((step) => (
                    <Timeline.Item
                      key={step.id}
                      bullet={
                        <ThemeIcon
                          size={32}
                          variant={step.status === 'Running' ? 'filled' : 'light'}
                          color={getTimelineColor(step.status)}
                          radius="xl"
                        >
                          {getTimelineIcon(step)}
                        </ThemeIcon>
                      }
                      title={
                        <Text fw={600} size="sm" c="dark.9">{step.stepName}</Text>
                      }
                    >
                      <Text size="xs" c="dimmed" mb="sm">
                        {STEP_DESCRIPTIONS[step.stepName]}
                      </Text>
                      
                      {step.status === 'Running' && stepStatuses[step.stepName] && (
                        <Paper p="sm" bg="gray.0" withBorder mb="sm">
                          <Group justify="space-between" mb="xs">
                            <Text size="xs" fw={500}>{stepStatuses[step.stepName].message}</Text>
                            <Text size="xs" c="dimmed">{stepStatuses[step.stepName].progress}%</Text>
                          </Group>
                          <Progress 
                            value={stepStatuses[step.stepName].progress} 
                            size="sm" 
                            animated 
                          />
                        </Paper>
                      )}

                      {/* LLM Streaming Output - Live View */}
                      {step.status === 'Running' && llmStreams[step.stepName] && (
                        <Box mt="sm">
                          <Text size="xs" fw={600} mb={4} c="blue">LIVE OUTPUT:</Text>
                          <Code block style={{ maxHeight: 150, overflowY: 'auto', fontSize: 11 }}>
                            {llmStreams[step.stepName].chunks.join('')}
                          </Code>
                        </Box>
                      )}
                      
                      {/* Input/Output Tabs for Completed Steps */}
                      {step.status === 'Completed' && (step.outputSnapshot || step.inputSnapshot) && (
                        <Paper p="sm" bg="gray.0" withBorder mt="sm">
                          <Tabs defaultValue="output" variant="pills" size="xs">
                            <Tabs.List mb="sm">
                              {step.outputSnapshot && (
                                <Tabs.Tab value="output" leftSection={<IconPackage size={12} />}>
                                  Output
                                </Tabs.Tab>
                              )}
                              {step.inputSnapshot && (
                                <Tabs.Tab value="input" leftSection={<IconFileCheck size={12} />}>
                                  Input
                                </Tabs.Tab>
                              )}
                            </Tabs.List>

                            {step.outputSnapshot && (
                              <Tabs.Panel value="output">
                                <ScrollArea h={200} type="auto">
                                  <Code block style={{ fontSize: 11, background: 'white' }}>
                                    {step.outputSnapshot}
                                  </Code>
                                </ScrollArea>
                                {step.outputIsTruncated && (
                                  <Text size="xs" c="dimmed" mt="xs">
                                    ⚠️ Truncated (full: {step.outputFullLength?.toLocaleString()} chars)
                                  </Text>
                                )}
                              </Tabs.Panel>
                            )}

                            {step.inputSnapshot && (
                              <Tabs.Panel value="input">
                                <ScrollArea h={200} type="auto">
                                  <Code block style={{ fontSize: 11, background: 'white' }}>
                                    {step.inputSnapshot}
                                  </Code>
                                </ScrollArea>
                                {step.inputIsTruncated && (
                                  <Text size="xs" c="dimmed" mt="xs">
                                    ⚠️ Truncated (full: {step.inputFullLength?.toLocaleString()} chars)
                                  </Text>
                                )}
                              </Tabs.Panel>
                            )}
                          </Tabs>
                          
                          <Group mt="sm" justify="flex-end">
                            <Button 
                              variant="subtle" 
                              size="xs"
                              leftSection={<IconRefresh size={12} />}
                              onClick={() => rerunMutation.mutate(step.stepName)}
                              disabled={run.status === 'Running'}
                            >
                              Re-run from here
                            </Button>
                          </Group>
                        </Paper>
                      )}

                      {/* Pending/Failed steps without data */}
                      {step.status !== 'Completed' && step.status !== 'Running' && (
                        <Text size="xs" c="dimmed" fs="italic">Waiting...</Text>
                      )}

                    </Timeline.Item>
                  ))}
                </Timeline>
              </Paper>
            </Grid.Col>

            <Grid.Col span={{ base: 12, md: 5 }}>
               {/* Final Output or Active Step Detail */}
               <Stack>
                 {run.finalOutputHtml ? (
                   <Paper p="xl" className="modern-card" style={{ borderColor: 'var(--color-brand-blue)' }}>
                      <Group mb="md">
                        <ThemeIcon size="lg" color="green" variant="light"><IconPackage size={20} /></ThemeIcon>
                        <Title order={4}>Final Deliverable</Title>
                      </Group>
                      <Box 
                        p="md" 
                        bg="gray.0" 
                        style={{ borderRadius: 8, border: '1px solid var(--color-slate-200)' }}
                        dangerouslySetInnerHTML={{ __html: run.finalOutputHtml }} 
                      />
                   </Paper>
                 ) : (
                   <Paper p="xl" className="modern-card" bg="gray.0">
                      <Stack align="center" py="xl" c="dimmed">
                        <Loader type="dots" color="gray" />
                        <Text size="sm">Waiting for workflow completion...</Text>
                      </Stack>
                   </Paper>
                 )}
               </Stack>
            </Grid.Col>
          </Grid>
        </Stack>
      </Container>
    </Box>
  );
}
