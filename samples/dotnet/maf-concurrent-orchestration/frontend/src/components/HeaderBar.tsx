import { Badge, Card, Group, Stack, Text, ThemeIcon, Title } from '@mantine/core'
import {
  IconActivityHeartbeat,
  IconBinaryTree2,
  IconClipboardCheck,
  IconShieldCheck,
} from '@tabler/icons-react'
import { formatShortRunId } from '../lib/presentation'

interface HeaderBarProps {
  selectedRunId: string | null
  currentStatus?: string
  totalRuns: number
  runningRunCount: number
  concurrentLaneCount: number
}

const reviewStages = ['Data Intake', 'Expert Reviews', 'Decision Gate', 'Onboarding Package']

export function HeaderBar({
  selectedRunId,
  currentStatus,
  totalRuns,
  runningRunCount,
  concurrentLaneCount,
}: HeaderBarProps) {
  return (
    <Card className="surface-card hero-card" padding="xl" radius="36px">
      <div className="hero-card-inner">
        <div className="hero-copy">
          <Group gap="sm" mb={16}>
            <Badge radius="xl" size="lg" variant="light" color="amber">
              OnboardFlow
            </Badge>
            <Badge radius="xl" size="lg" variant="outline" color="dark">
              Customer Onboarding
            </Badge>
          </Group>

          <Title order={1} className="hero-title">
            Intelligent customer onboarding, from intake to decision.
          </Title>
          <Text mt="md" className="hero-description">
            Submit an onboarding request and watch AI-powered reviewers evaluate security, compliance, and
            finance risks in parallel — then receive a consolidated decision package with clear next steps
            for your customer.
          </Text>

          <div className="hero-journey">
            {reviewStages.map((stage, index) => (
              <div key={stage} className="hero-journey-pill">
                <span className="hero-journey-index">0{index + 1}</span>
                <span>{stage}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="hero-sidecar">
          <div className="hero-metric-grid">
            <div className="hero-metric-card">
              <ThemeIcon size={42} radius="xl" variant="light" color="orange">
                <IconActivityHeartbeat size={20} />
              </ThemeIcon>
              <div>
                <Text className="hero-metric-value">{runningRunCount}</Text>
                <Text className="hero-metric-label">Reviews in progress</Text>
              </div>
            </div>

            <div className="hero-metric-card">
              <ThemeIcon size={42} radius="xl" variant="light" color="teal">
                <IconShieldCheck size={20} />
              </ThemeIcon>
              <div>
                <Text className="hero-metric-value">{concurrentLaneCount}</Text>
                <Text className="hero-metric-label">Expert reviewers</Text>
              </div>
            </div>

            <div className="hero-metric-card">
              <ThemeIcon size={42} radius="xl" variant="light" color="amber">
                <IconClipboardCheck size={20} />
              </ThemeIcon>
              <div>
                <Text className="hero-metric-value">{totalRuns}</Text>
                <Text className="hero-metric-label">Total reviews</Text>
              </div>
            </div>

            <div className="hero-metric-card">
              <ThemeIcon size={42} radius="xl" variant="light" color="dark">
                <IconBinaryTree2 size={20} />
              </ThemeIcon>
              <div>
                <Text className="hero-metric-value">{currentStatus ?? 'Idle'}</Text>
                <Text className="hero-metric-label">Current status</Text>
              </div>
            </div>
          </div>

          <Stack gap="sm" className="hero-spotlight">
            <div className="hero-spotlight-topline">
              <span className="hero-live-dot" />
              <span>Active review</span>
            </div>
            <Text fw={700} fz="lg">
              Review {formatShortRunId(selectedRunId)}
            </Text>
            <Text c="dimmed" fz="sm">
              Track the progress of each reviewer in real time. Once all expert reviews are complete, a
              consolidated decision package is generated automatically.
            </Text>
          </Stack>
        </div>
      </div>
    </Card>
  )
}
