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
  ThemeIcon
} from '@mantine/core';
import { 
  IconPlayerPlay, 
  IconFileText, 
  IconSparkles,
  IconFileCheck,
  IconPencil,
  IconShieldCheck,
  IconRobot,
  IconPackage
} from '@tabler/icons-react';
import { api } from '../api/client';
import { AudienceType, ToneType, type Sample } from '../types';

// Define animations as CSS strings
const fadeInAnimation = '@keyframes fadeIn { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }';
const floatAnimation = '@keyframes float { 0%, 100% { transform: translateY(0px); } 50% { transform: translateY(-10px); } }';

// Inject animations into head
if (typeof document !== 'undefined') {
  const style = document.createElement('style');
  style.textContent = fadeInAnimation + floatAnimation;
  document.head.appendChild(style);
}

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
    <Box
      style={{
        minHeight: '100vh',
        background: 'var(--color-bg)',
        paddingTop: rem(60),
        paddingBottom: rem(60)
      }}
    >
      <Container size="xl">
        <Stack gap="xl">
          {/* Hero Section */}
          <Box style={{ textAlign: 'center', paddingBottom: rem(20) }}>
            <ThemeIcon 
              size={80} 
              radius="md" 
              variant="light" 
              color="blue" 
              mb="lg"
            >
              <IconSparkles size={40} />
            </ThemeIcon>
            <Title
              order={1}
              size={rem(42)}
              style={{
                background: 'linear-gradient(135deg, #1e293b 0%, #334155 100%)',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                letterSpacing: '-1.5px',
                lineHeight: 1.2
              }}
            >
              PolicyPack Builder
            </Title>
            <Text
              size="xl"
              mt="md"
              c="dimmed"
              style={{ maxWidth: rem(600), marginInline: 'auto' }}
            >
              Enterprise-grade document processing powered by AI Sequential Workflows
            </Text>
            <Group justify="center" gap="xs" mt="md">
              <Badge variant="dot" size="lg" color="green">System Online</Badge>
              <Badge variant="outline" size="lg" color="gray">v2.0.0</Badge>
            </Group>
          </Box>

          {/* Sample Cards */}
          {samples && samples.length > 0 && (
            <Paper p="xl" className="modern-card">
              <Group gap="sm" mb="md">
                <ThemeIcon variant="light" size="lg" color="blue">
                  <IconFileText size={20} />
                </ThemeIcon>
                <Text size="lg" fw={600} className="gradient-text">Try a Sample</Text>
              </Group>
              <Grid>
                {samples.map((sample, idx) => (
                  <Grid.Col key={sample.id} span={{ base: 12, sm: 6 }}>
                    <Card
                      withBorder
                      padding="lg"
                      radius="md"
                      style={{ cursor: 'pointer', height: '100%' }}
                      className="modern-card"
                      onClick={() => handleLoadSample(sample)}
                    >
                      <Group justify="space-between" mb="xs">
                        <Text fw={600} size="md" c="dark.7">{sample.name}</Text>
                        <Badge size="sm" variant="light" color="blue">Sample {idx + 1}</Badge>
                      </Group>
                      <Text size="sm" c="dimmed" style={{ lineHeight: 1.6 }}>
                        {sample.description}
                      </Text>
                    </Card>
                  </Grid.Col>
                ))}
              </Grid>
            </Paper>
          )}

          {/* Main Input Form */}
          <Paper p="xl" className="modern-card">
            <Stack gap="xl">
              <Textarea
                label={<Text size="lg" fw={600} mb="xs" c="dark.9">Policy Draft Input</Text>}
                description="Paste your draft policy, warranty, or communication text here. Our AI will process it through 6 workflow steps."
                placeholder="Enter your draft policy text here...&#10;&#10;Example: On December 15th, 2023, patient John Smith visited the emergency room..."
                autosize
                minRows={15}
                maxRows={25}
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                required
                size="md"
              />

              <Grid>
                <Grid.Col span={{ base: 12, sm: 4 }}>
                  <Select
                    label="Target Audience"
                    data={[
                      { value: AudienceType.Customer, label: '👥 Customers/Patients' },
                      { value: AudienceType.Legal, label: '⚖️ Legal/Compliance' },
                      { value: AudienceType.Internal, label: '🏢 Internal Staff' },
                    ]}
                    value={audience}
                    onChange={(val) => setAudience(val as AudienceType)}
                    size="md"
                  />
                </Grid.Col>
                <Grid.Col span={{ base: 12, sm: 4 }}>
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
                <Grid.Col span={{ base: 12, sm: 4 }}>
                  <Box mt="lg">
                    <Switch
                      label="Strict Compliance Mode"
                      description="Enable for regulatory-critical documents"
                      checked={strictCompliance}
                      onChange={(e) => setStrictCompliance(e.currentTarget.checked)}
                      size="md"
                    />
                  </Box>
                </Grid.Col>
              </Grid>

              <Button
                leftSection={<IconPlayerPlay size={20} />}
                size="xl"
                radius="md"
                onClick={handleSubmit}
                loading={createRunMutation.isPending}
                disabled={!inputText || inputText.trim().length === 0}
                className="gradient-bg"
                fullWidth
              >
                Start Workflow Processing
              </Button>

              {/* Enhanced Workflow Steps Preview */}
              <Box mt="xl">
                <Text fw={700} size="lg" mb="lg" c="dimmed" tt="uppercase" style={{ letterSpacing: 1 }}>
                  <Group gap="xs">
                    <IconSparkles size={20} />
                    Processing Pipeline
                  </Group>
                </Text>
                
                <Grid>
                  {[
                    { 
                      title: 'Intake & Normalize', 
                      desc: 'Standardizes format and removes noise', 
                      icon: IconFileCheck,
                      color: 'blue' 
                    },
                    { 
                      title: 'Extract Facts', 
                      desc: 'Identifies key entities and requirements', 
                      icon: IconSparkles,
                      color: 'indigo' 
                    },
                    { 
                      title: 'Draft Summary', 
                      desc: 'Creates structured executive briefing', 
                      icon: IconPencil,
                      color: 'cyan' 
                    },
                    { 
                      title: 'Compliance Check', 
                      desc: 'Validates against regulatory rules', 
                      icon: IconShieldCheck,
                      color: 'teal' 
                    },
                    { 
                      title: 'Tone Rewrite', 
                      desc: 'Adjusts voice for target audience', 
                      icon: IconRobot,
                      color: 'grape' 
                    },
                    { 
                      title: 'Final Package', 
                      desc: 'Assembles polished deliverables', 
                      icon: IconPackage,
                      color: 'green' 
                    }
                  ].map((step, index) => (
                    <Grid.Col span={{ base: 12, sm: 6, md: 4 }} key={index}>
                      <Paper p="md" className="modern-card" bg="gray.0">
                        <Group align="flex-start" wrap="nowrap">
                          <ThemeIcon size={40} radius="md" variant="light" color={step.color}>
                            <step.icon size={20} />
                          </ThemeIcon>
                          <Box>
                            <Text fw={600} size="sm" c="dark.8">{step.title}</Text>
                            <Text size="xs" c="dimmed" mt={2} style={{ lineHeight: 1.4 }}>
                              {step.desc}
                            </Text>
                          </Box>
                        </Group>
                      </Paper>
                    </Grid.Col>
                  ))}
                </Grid>
              </Box>
            </Stack>
          </Paper>
        </Stack>
      </Container>
    </Box>
  );
}
