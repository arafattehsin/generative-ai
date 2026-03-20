import { Badge, Card, Group, Progress, ScrollArea, Stack, Text, TextInput, Title } from '@mantine/core'
import { IconSearch } from '@tabler/icons-react'
import type { RunSummary } from '../lib/types'
import {
  countRunsByStatus,
  formatDateTime,
  formatDuration,
  formatShortRunId,
  formatStepName,
  getRunProgressValue,
  statusColor,
  businessStatus,
} from '../lib/presentation'

interface RunHistoryProps {
  runs: RunSummary[]
  search: string
  onSearchChange: (value: string) => void
  selectedRunId: string | null
  onSelectRun: (runId: string) => void
}

export function RunHistory({ runs, search, onSearchChange, selectedRunId, onSelectRun }: RunHistoryProps) {
  const runningCount = countRunsByStatus(runs, 'Running')
  const completedCount = countRunsByStatus(runs, 'Completed')

  return (
    <Card className="surface-card history-card" padding="xl" radius="32px">
      <Stack gap="lg">
        <div>
          <Text fw={700} tt="uppercase" fz="xs" c="dimmed" mb={8}>
            Review history
          </Text>
          <Title order={4}>Past onboarding reviews</Title>
        </div>

        <div className="history-summary-grid">
          <div className="history-summary-card">
            <Text className="history-summary-value">{runs.length}</Text>
            <Text className="history-summary-label">Total reviews</Text>
          </div>
          <div className="history-summary-card">
            <Text className="history-summary-value">{runningCount}</Text>
            <Text className="history-summary-label">In progress</Text>
          </div>
          <div className="history-summary-card">
            <Text className="history-summary-value">{completedCount}</Text>
            <Text className="history-summary-label">Completed</Text>
          </div>
        </div>

        <TextInput
          radius="xl"
          placeholder="Search reviews..."
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(event) => onSearchChange(event.currentTarget.value)}
        />

        <ScrollArea h={760} offsetScrollbars>
          <Stack gap="sm">
            {runs.length === 0 ? (
              <Text c="dimmed">No reviews yet. Submit an onboarding request to get started.</Text>
            ) : (
              runs.map((run) => {
                const progressValue = getRunProgressValue(run.completedStepCount, run.stepCount)

                return (
                  <button
                    key={run.id}
                    type="button"
                    className={`history-row ${selectedRunId === run.id ? 'history-row-active' : ''}`}
                    onClick={() => onSelectRun(run.id)}
                  >
                    <Group justify="space-between" align="flex-start" wrap="nowrap">
                      <div>
                        <Text fw={700}>Review {formatShortRunId(run.id)}</Text>
                        <Text c="dimmed" fz="sm">
                          {formatDateTime(run.createdAt)}
                        </Text>
                      </div>
                      <Badge radius="xl" variant="light" color={statusColor[run.status]}>
                        {businessStatus[run.status] ?? run.status}
                      </Badge>
                    </Group>

                    <div>
                      <Group justify="space-between" align="center" mb={6}>
                        <Text fw={600} fz="sm">
                          {run.completedStepCount}/{run.stepCount} stages done
                        </Text>
                        <Text c="dimmed" fz="sm">
                          {progressValue}%
                        </Text>
                      </Group>
                      <Progress value={progressValue} size="xs" radius="xl" color={run.status === 'Completed' ? 'teal' : 'orange'} />
                    </div>

                    <div className="history-row-meta">
                      <span>{run.totalDurationMs ? formatDuration(run.totalDurationMs) : 'In progress'}</span>
                      <span>{run.rerunFromStep ? `Re-run from ${formatStepName(run.rerunFromStep)}` : 'Original review'}</span>
                      <span>{selectedRunId === run.id ? 'Viewing' : 'Open'}</span>
                    </div>
                  </button>
                )
              })
            )}
          </Stack>
        </ScrollArea>
      </Stack>
    </Card>
  )
}
