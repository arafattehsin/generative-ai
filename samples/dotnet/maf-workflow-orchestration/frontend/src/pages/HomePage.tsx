import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Container,
  Title,
  Text,
  Paper,
  Stack,
  Textarea,
  Button,
  Group,
  Select,
  Switch,
  Card,
  Badge,
  Grid,
  Box,
  rem,
  ThemeIcon,
  Tooltip,
  SimpleGrid
} from '@mantine/core';
import { 
  IconPlayerPlay, 
  IconFileText, 
  IconSparkles,
  IconFileCheck,
  IconPencil,
  IconShieldCheck,
  IconRobot,
  IconPackage,
  IconArrowRight,
  IconBolt
} from '@tabler/icons-react';
import { api } from '../api/client';
import { AudienceType, ToneType, type Sample } from '../types';

const WORKFLOW_STEPS = [
  { 
    title: 'Intake & Normalize', 
    desc: 'Clean and standardize input format', 
    icon: IconFileCheck,
    gradient: 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)' 
  },
  { 
    title: 'Extract Facts', 
    desc: 'Identify key entities and data points', 
    icon: IconSparkles,
    gradient: 'linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%)' 
  },
  { 
    title: 'Draft Summary', 
    desc: 'Generate structured executive brief', 
    icon: IconPencil,
    gradient: 'linear-gradient(135deg, #06b6d4 0%, #0891b2 100%)' 
  },
  { 
    title: 'Compliance Check', 
    desc: 'Validate against regulatory rules', 
    icon: IconShieldCheck,
    gradient: 'linear-gradient(135deg, #14b8a6 0%, #0d9488 100%)' 
  },
  { 
    title: 'Tone Rewrite', 
    desc: 'Adjust voice for target audience', 
    icon: IconRobot,
    gradient: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)' 
  },
  { 
    title: 'Final Package', 
    desc: 'Assemble polished deliverables', 
    icon: IconPackage,
    gradient: 'linear-gradient(135deg, #10b981 0%, #059669 100%)' 
  }
];

export function HomePage() {
  const navigate = useNavigate();
  const [inputText, setInputText] = useState('');
  const [audience, setAudience] = useState<AudienceType>(AudienceType.Customer);
  const [tone, setTone] = useState<ToneType>(ToneType.Professional);
  const [strictCompliance, setStrictCompliance] = useState(false);

  const { data: samples } = useQuery({
    queryKey: ['samples'],
    queryFn: () => api.getSamples(),
  });

  const createRunMutation = useMutation({
    mutationFn: (text: string) => api.createRun({
      inputText: text,
      options: { audience, tone, strictCompliance }
    }),
    onSuccess: (data) => {
      console.log('Run created successfully:', data);
      navigate(`/runs/${data.runId}`);
    },
    onError: (error) => {
      console.error('Failed to create run:', error);
      alert(`Failed to start workflow: ${error.message}`);
    },
  });

  const handleLoadSample = (sample: Sample) => {
    setInputText(sample.inputText);
  };

  const handleSubmit = () => {
    console.log('Submit clicked, inputText length:', inputText.length);
    console.log('Options:', { audience, tone, strictCompliance });
    if (inputText.trim()) {
      console.log('Triggering mutation...');
      createRunMutation.mutate(inputText);
    } else {
      console.warn('Input text is empty');
      alert('Please enter some text first');
    }
  };

  return (
    <Container size="xl">
      <Grid gutter={40}>
        {/* Left Column - Hero & Workflow Steps */}
        <Grid.Col span={{ base: 12, lg: 5 }}>
          <Stack gap="xl" style={{ position: 'sticky', top: 100 }}>
            {/* Hero Section */}
            <Box className="animate-fadeIn">
              <Badge 
                size="lg" 
                radius="xl" 
                variant="light"
                color="blue"
                mb="lg"
                leftSection={<IconBolt size={14} />}
                style={{
                  textTransform: 'none',
                  fontWeight: 500,
                }}
              >
                Microsoft Agent Framework
              </Badge>
              
              <Title
                order={1}
                size={rem(48)}
                fw={800}
                style={{
                  color: 'white',
                  letterSpacing: '-2px',
                  lineHeight: 1.1,
                }}
              >
                Transform Draft Documents into{' '}
                <Text
                  component="span"
                  inherit
                  className="gradient-text"
                >
                  Polished Packages
                </Text>
              </Title>
              
              <Text
                size="lg"
                mt="xl"
                style={{ 
                  color: 'var(--color-text-secondary)',
                  lineHeight: 1.7,
                  maxWidth: 420
                }}
              >
                Enterprise-grade document processing powered by AI Sequential Workflows. 
                Six intelligent steps transform your content into compliant, professional deliverables.
              </Text>
            </Box>

            {/* Workflow Pipeline Visualization */}
            <Box mt="xl" className="animate-fadeIn stagger-2">
              <Text 
                size="sm" 
                fw={600} 
                mb="md" 
                style={{ 
                  color: 'var(--color-text-muted)',
                  textTransform: 'uppercase',
                  letterSpacing: 1
                }}
              >
                <Group gap="xs">
                  <IconSparkles size={16} />
                  Processing Pipeline
                </Group>
              </Text>
              
              <Stack gap="xs">
                {WORKFLOW_STEPS.map((step, index) => (
                  <Box
                    key={index}
                    className="glass-card"
                    p="sm"
                    style={{
                      opacity: 0,
                      animation: `fadeIn 0.4s ease forwards`,
                      animationDelay: `${index * 0.1}s`,
                    }}
                  >
                    <Group gap="md" wrap="nowrap">
                      <Box
                        style={{
                          width: 40,
                          height: 40,
                          borderRadius: 10,
                          background: step.gradient,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          flexShrink: 0,
                        }}
                      >
                        <step.icon size={20} color="white" stroke={1.5} />
                      </Box>
                      <Box style={{ flex: 1, minWidth: 0 }}>
                        <Text fw={600} size="sm" style={{ color: 'white' }}>
                          {step.title}
                        </Text>
                        <Text size="xs" style={{ color: 'var(--color-text-muted)' }}>
                          {step.desc}
                        </Text>
                      </Box>
                      <Text 
                        size="xs" 
                        fw={600}
                        style={{ 
                          color: 'var(--color-text-muted)',
                          fontVariantNumeric: 'tabular-nums'
                        }}
                      >
                        {String(index + 1).padStart(2, '0')}
                      </Text>
                    </Group>
                  </Box>
                ))}
              </Stack>
            </Box>
          </Stack>
        </Grid.Col>

        {/* Right Column - Input Form */}
        <Grid.Col span={{ base: 12, lg: 7 }}>
          <Stack gap="xl" className="animate-fadeIn stagger-1">
            {/* Sample Templates */}
            {samples && samples.length > 0 && (
              <Box>
                <Group gap="sm" mb="md">
                  <ThemeIcon 
                    size="md" 
                    radius="md" 
                    variant="light" 
                    color="cyan"
                  >
                    <IconFileText size={16} />
                  </ThemeIcon>
                  <Text size="sm" fw={600} style={{ color: 'var(--color-text-secondary)' }}>
                    Quick Start Templates
                  </Text>
                </Group>
                <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
                  {samples.map((sample) => (
                    <Card
                      key={sample.id}
                      className="glass-card"
                      padding="lg"
                      style={{ cursor: 'pointer' }}
                      onClick={() => handleLoadSample(sample)}
                    >
                      <Group justify="space-between" mb="xs">
                        <Text fw={600} size="sm" style={{ color: 'white' }}>
                          {sample.name}
                        </Text>
                        <IconArrowRight 
                          size={16} 
                          style={{ color: 'var(--color-accent-blue)' }} 
                        />
                      </Group>
                      <Text 
                        size="xs" 
                        style={{ 
                          color: 'var(--color-text-muted)',
                          lineHeight: 1.5 
                        }}
                      >
                        {sample.description}
                      </Text>
                    </Card>
                  ))}
                </SimpleGrid>
              </Box>
            )}

            {/* Main Input Card */}
            <Paper className="glass-card" p="xl" radius="lg">
              <Stack gap="lg">
                <Textarea
                  label={
                    <Text size="sm" fw={600} mb={4} style={{ color: 'var(--color-text-secondary)' }}>
                      Policy Draft Input
                    </Text>
                  }
                  description="Paste your draft policy, warranty, or communication text. AI will process it through the workflow pipeline."
                  placeholder="Enter your draft policy text here...

Example: On December 15th, 2023, patient John Smith visited the emergency room for evaluation of chest pain. The attending physician, Dr. Sarah Johnson, ordered an electrocardiogram (ECG) and blood tests..."
                  autosize
                  minRows={12}
                  maxRows={20}
                  value={inputText}
                  onChange={(e) => setInputText(e.target.value)}
                  required
                  size="md"
                  styles={{
                    input: {
                      fontSize: rem(14),
                      lineHeight: 1.6,
                    }
                  }}
                />

                {/* Options Row */}
                <Grid gutter="md">
                  <Grid.Col span={{ base: 12, sm: 6 }}>
                    <Select
                      label="Target Audience"
                      data={[
                        { value: AudienceType.Customer, label: '👥 Customers / Patients' },
                        { value: AudienceType.Legal, label: '⚖️ Legal / Compliance' },
                        { value: AudienceType.Internal, label: '🏢 Internal Staff' },
                      ]}
                      value={audience}
                      onChange={(val) => setAudience(val as AudienceType)}
                      size="md"
                    />
                  </Grid.Col>
                  <Grid.Col span={{ base: 12, sm: 6 }}>
                    <Select
                      label="Tone"
                      data={[
                        { value: ToneType.Professional, label: '💼 Professional' },
                        { value: ToneType.Friendly, label: '😊 Friendly' },
                        { value: ToneType.Formal, label: '🎩 Formal' },
                      ]}
                      value={tone}
                      onChange={(val) => setTone(val as ToneType)}
                      size="md"
                    />
                  </Grid.Col>
                </Grid>

                <Group justify="space-between" align="center">
                  <Switch
                    label="Strict Compliance Mode"
                    description="Enable for regulatory-critical documents"
                    checked={strictCompliance}
                    onChange={(e) => setStrictCompliance(e.currentTarget.checked)}
                    size="md"
                    styles={{
                      label: { color: 'var(--color-text-secondary)' },
                      description: { color: 'var(--color-text-muted)' }
                    }}
                  />
                </Group>

                <Button
                  leftSection={<IconPlayerPlay size={20} />}
                  rightSection={<IconArrowRight size={18} />}
                  size="lg"
                  radius="md"
                  onClick={handleSubmit}
                  loading={createRunMutation.isPending}
                  disabled={!inputText || inputText.trim().length === 0}
                  className="gradient-button"
                  fullWidth
                  h={56}
                  styles={{
                    root: {
                      fontSize: rem(16),
                      fontWeight: 600,
                    }
                  }}
                >
                  Start Workflow Processing
                </Button>

                <Text 
                  size="xs" 
                  ta="center" 
                  style={{ color: 'var(--color-text-muted)' }}
                >
                  Your document will be processed through 6 AI-powered steps
                </Text>
              </Stack>
            </Paper>
          </Stack>
        </Grid.Col>
      </Grid>
    </Container>
  );
}
