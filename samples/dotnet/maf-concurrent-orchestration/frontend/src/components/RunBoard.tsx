import { Badge, Card, Group, Progress, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { IconBarrierBlock, IconBolt, IconChecks, IconClockHour4, IconGitMerge, IconClipboardList } from '@tabler/icons-react'
import type { RunDetail, StepDefinition, TimelineEvent } from '../lib/types'
import {
  buildTimelineFromRun,
  businessStatus,
  eventKindColor,
  eventKindLabel,
  formatDateTime,
  formatDuration,
  formatStepName,
  formatTime,
  getRunProgressValue,
  getStepStatus,
  statusColor,
  statusTone,
} from '../lib/presentation'

interface RunBoardProps {
  runDetail?: RunDetail
  steps: StepDefinition[]
  timeline: TimelineEvent[]
}

function formatEventKind(kind: TimelineEvent['kind']) {
  return kind.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export function RunBoard({ runDetail, steps, timeline }: RunBoardProps) {
  const concurrentSteps = steps.filter((step) => step.isConcurrent)
  const nonConcurrentSteps = steps.filter((step) => !step.isConcurrent)
  const firstConcurrentOrder = concurrentSteps[0]?.order ?? Number.POSITIVE_INFINITY
  const lastConcurrentOrder = concurrentSteps.length ? concurrentSteps[concurrentSteps.length - 1].order : Number.NEGATIVE_INFINITY
  const preConcurrentSteps = nonConcurrentSteps.filter((step) => step.order < firstConcurrentOrder)
  const postConcurrentSteps = nonConcurrentSteps.filter((step) => step.order > lastConcurrentOrder)
  const completedCount = runDetail?.steps.filter((step) => step.status === 'Completed').length ?? 0
  const progressValue = getRunProgressValue(completedCount, runDetail?.steps.length ?? 0)
  const concurrentCompletedCount = concurrentSteps.filter((step) => getStepStatus(runDetail, step.name) === 'Completed').length
  const barrierStatus = concurrentCompletedCount === concurrentSteps.length ? 'All cleared' : 'Awaiting reviews'
  const displayTimeline = timeline.length > 0 ? timeline : buildTimelineFromRun(runDetail)
  const currentRunStartedAt = runDetail?.startedAt ?? runDetail?.createdAt

  return (
    <Stack gap="lg">
      <Card className="surface-card live-board-card" padding="xl" radius="32px">
        <Stack gap="lg">
          <Group justify="space-between" align="flex-start" gap="md">
            <div>
              <Text fw={700} tt="uppercase" fz="xs" c="dimmed" mb={8}>
                Review progress
              </Text>
              <Title order={3} className="section-title">
                Applicant review stages and expert findings
              </Title>
            </div>
            <Badge radius="xl" size="lg" color={statusColor[runDetail?.status ?? 'Pending']} variant="light">
              {businessStatus[runDetail?.status ?? 'Pending'] ?? runDetail?.status ?? 'No review selected'}
            </Badge>
          </Group>

          <div className="board-summary-grid">
            <div className="board-summary-card">
              <Text className="board-summary-label">Review progress</Text>
              <Text className="board-summary-value">{progressValue}%</Text>
              <Progress value={progressValue} size="sm" radius="xl" color="orange" />
            </div>

            <div className="board-summary-card">
              <Text className="board-summary-label">Expert reviews</Text>
              <Text className="board-summary-value">
                {concurrentCompletedCount}/{concurrentSteps.length}
              </Text>
              <Text c="dimmed" fz="sm">
                reviews completed
              </Text>
            </div>

            <div className="board-summary-card">
              <Text className="board-summary-label">Decision gate</Text>
              <Text className="board-summary-value">{barrierStatus}</Text>
              <Text c="dimmed" fz="sm">
                final approval
              </Text>
            </div>

            <div className="board-summary-card">
              <Text className="board-summary-label">Submitted</Text>
              <Text className="board-summary-value board-summary-value-small">{formatDateTime(currentRunStartedAt)}</Text>
              <Text c="dimmed" fz="sm">
                {formatDuration(runDetail?.totalDurationMs)}
              </Text>
            </div>
          </div>

          <div className="orchestration-map">
            <div className="process-column">
              <div className="process-column-header">
                <IconClipboardList size={18} />
                <Text fw={700}>Data preparation</Text>
              </div>

              <div className="process-list">
                {preConcurrentSteps.map((step) => {
                  const stepRun = runDetail?.steps.find((candidate) => candidate.stepName === step.name)
                  const status = getStepStatus(runDetail, step.name)
                  return (
                    <div key={step.name} className={`process-card status-${status.toLowerCase()}`}>
                      <div className="process-card-index">0{step.order}</div>
                      <div className="process-card-copy">
                        <Group justify="space-between" align="flex-start" wrap="nowrap">
                          <div>
                            <Text fw={700}>{formatStepName(step.name)}</Text>
                            <Text c="dimmed" fz="sm">
                              {step.description}
                            </Text>
                          </div>
                          <Badge radius="xl" color={statusColor[status]} variant="light" style={{ flexShrink: 0 }}>
                            {statusTone[status] ?? status}
                          </Badge>
                        </Group>
                        <Text c="dimmed" fz="sm">
                          {formatDuration(stepRun?.durationMs, 'Queued')}
                        </Text>
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>

            <div className="concurrency-stage">
              <div className="concurrency-stage-header">
                <Group gap="sm">
                  <div className="process-card-index">0{preConcurrentSteps.length + 1}</div>
                  <IconBolt size={18} />
                  <Text fw={700}>Expert reviews</Text>
                </Group>
                <Badge radius="xl" color="orange" variant="light">
                  Parallel
                </Badge>
              </div>

              <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md">
                {concurrentSteps.map((step) => {
                  const stepRun = runDetail?.steps.find((candidate) => candidate.stepName === step.name)
                  const status = getStepStatus(runDetail, step.name)

                  return (
                    <div key={step.name} className={`review-lane status-${status.toLowerCase()}`}>
                      <div className="review-lane-topline">
                        <Text fw={700}>{formatStepName(step.name)}</Text>
                        <Badge radius="xl" color={statusColor[status]} variant="light" style={{ flexShrink: 0 }}>
                          {businessStatus[status] ?? status}
                        </Badge>
                      </div>
                      <Text c="dimmed" fz="sm">
                        {step.description}
                      </Text>
                      <div className="review-lane-footer">
                        <span>{formatDuration(stepRun?.durationMs, 'Queued')}</span>
                        <span>{stepRun?.startedAt ? `Started ${formatTime(stepRun.startedAt)}` : 'Awaiting start'}</span>
                      </div>
                    </div>
                  )
                })}
              </SimpleGrid>

              <div className="barrier-node">
                <Group gap="sm">
                  <IconBarrierBlock size={18} />
                  <Text fw={700}>Decision gate</Text>
                </Group>
                <Text c="dimmed" fz="sm">
                  The decision package is generated only after all expert reviews are complete.
                </Text>
              </div>
            </div>

            <div className="process-column">
              <div className="process-column-header">
                <IconGitMerge size={18} />
                <Text fw={700}>Final decision</Text>
              </div>

              <div className="process-list">
                {postConcurrentSteps.map((step, index) => {
                  const stepRun = runDetail?.steps.find((candidate) => candidate.stepName === step.name)
                  const status = getStepStatus(runDetail, step.name)
                  const isAggregate = step.name === 'AggregateFindings'
                  const displayOrder = preConcurrentSteps.length + 1 + index + 1

                  return (
                    <div
                      key={step.name}
                      className={`${isAggregate ? 'aggregate-node process-card' : 'process-card'} status-${status.toLowerCase()}`}
                    >
                      <div className="process-card-index">{String(displayOrder).padStart(2, '0')}</div>
                      <div className="process-card-copy">
                        <Group justify="space-between" align="flex-start" wrap="nowrap">
                          <div>
                            <Text fw={700}>{formatStepName(step.name)}</Text>
                            <Text c="dimmed" fz="sm">
                              {step.description}
                            </Text>
                          </div>
                          <Badge radius="xl" color={statusColor[status]} variant="light" style={{ flexShrink: 0 }}>
                            {statusTone[status] ?? status}
                          </Badge>
                        </Group>
                        <Text c="dimmed" fz="sm">
                          {formatDuration(stepRun?.durationMs, 'Queued')}
                        </Text>
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          </div>
        </Stack>
      </Card>

      <Card className="surface-card telemetry-card" padding="xl" radius="32px">
        <Group justify="space-between" align="center" mb="lg">
          <div>
            <Text fw={700} tt="uppercase" fz="xs" c="dimmed" mb={6}>
              Activity log
            </Text>
            <Title order={4}>Recent updates</Title>
          </div>
          <Group gap="sm" c="dimmed">
            <IconChecks size={16} />
            <IconClockHour4 size={16} />
          </Group>
        </Group>

        <div className="timeline-feed">
          {displayTimeline.length === 0 ? (
            <Text c="dimmed">Activity updates will appear here as each review stage progresses.</Text>
          ) : (
            displayTimeline.map((event) => (
              <div key={`${event.timestamp}-${event.label}`} className="timeline-event">
                <span className="timeline-pulse" />
                <div className="timeline-event-copy">
                  <Group justify="space-between" align="flex-start" gap="sm">
                    <div>
                      <Text fw={600}>{event.label}</Text>
                      <Text c="dimmed" fz="sm">
                        {formatTime(event.timestamp)}
                      </Text>
                    </div>
                    <Badge radius="xl" variant="light" color={eventKindColor[event.kind] ?? 'dark'}>
                      {eventKindLabel[event.kind] ?? formatEventKind(event.kind)}
                    </Badge>
                  </Group>
                </div>
              </div>
            ))
          )}
        </div>
      </Card>
    </Stack>
  )
}
