import { Accordion, Badge, Button, Card, Code, Group, ScrollArea, Stack, Tabs, Text, Title } from '@mantine/core'
import { IconArrowBackUp, IconBinaryTree2, IconFileText, IconListDetails } from '@tabler/icons-react'
import type { RunDetail, StepDefinition } from '../lib/types'
import {
  formatShortRunId,
  formatStepName,
  getRunProgressValue,
  statusColor,
  businessStatus,
} from '../lib/presentation'

interface OutputDeckProps {
  runDetail?: RunDetail
  steps: StepDefinition[]
  onRerun: (fromStep: string) => void
}

function prettyJson(value: string | null | undefined) {
  if (!value) {
    return '// no output yet'
  }

  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

export function OutputDeck({ runDetail, steps, onRerun }: OutputDeckProps) {
  const finalStep = runDetail?.steps.find((step) => step.stepName === 'AggregateFindings')
  const customerNextSteps = runDetail?.steps.find((step) => step.stepName === 'CustomerNextSteps')
  const visibleSteps = runDetail?.steps ?? []
  const completedCount = visibleSteps.filter((step) => step.status === 'Completed').length
  const progressValue = getRunProgressValue(completedCount, visibleSteps.length)
  const latestCompletedStep = [...visibleSteps].reverse().find((step) => step.status === 'Completed')

  return (
    <Card className="surface-card output-deck-card" padding="xl" radius="32px">
      <Stack gap="lg">
        <Group justify="space-between" align="flex-start" gap="md">
          <div>
            <Text fw={700} tt="uppercase" fz="xs" c="dimmed" mb={8}>
              Decision summary
            </Text>
            <Title order={3} className="section-title">
              Onboarding report, reviewer findings, and next steps
            </Title>
          </div>

          <div className="output-summary-chip">
            <Text fw={700}>Review {formatShortRunId(runDetail?.id)}</Text>
            <Badge radius="xl" variant="light" color={statusColor[runDetail?.status ?? 'Pending']}>
              {businessStatus[runDetail?.status ?? 'Pending'] ?? runDetail?.status ?? 'Idle'}
            </Badge>
            <Text c="dimmed" fz="sm">
              {progressValue}% complete
            </Text>
          </div>
        </Group>

        <Tabs defaultValue="report" color="orange" radius="xl">
          <Tabs.List className="deck-tabs-list">
            <Tabs.Tab value="report" leftSection={<IconFileText size={16} />}>
              Onboarding report
            </Tabs.Tab>
            <Tabs.Tab value="decision" leftSection={<IconBinaryTree2 size={16} />}>
              Reviewer findings
            </Tabs.Tab>
            <Tabs.Tab value="steps" leftSection={<IconListDetails size={16} />}>
              Detailed review data
            </Tabs.Tab>
          </Tabs.List>

          <Tabs.Panel value="report" pt="lg">
            <div className="report-panel">
              <div className="report-preview-panel">
                <div className="report-preview-topline">
                  <Text fw={700}>Rendered HTML package</Text>
                  <Text c="dimmed" fz="sm">
                    Customer-facing onboarding document
                  </Text>
                </div>

                {runDetail?.finalOutputHtml ? (
                  <div className="report-frame-wrap">
                    <iframe title="Final report" srcDoc={runDetail.finalOutputHtml} className="report-frame" />
                  </div>
                ) : (
                  <div className="report-empty-state">
                    <Text fw={700}>No report yet</Text>
                    <Text c="dimmed" fz="sm">
                      The onboarding report will appear once all reviews are complete.
                    </Text>
                  </div>
                )}
              </div>

              <div className="report-insights-panel">
                <Text fw={700}>Report status</Text>
                <div className="report-insight-card">
                  <Text className="report-insight-label">Decision source</Text>
                  <Text fw={700}>{finalStep ? 'Combined expert findings' : 'Customer next steps'}</Text>
                </div>
                <div className="report-insight-card">
                  <Text className="report-insight-label">Stages completed</Text>
                  <Text fw={700}>
                    {completedCount}/{visibleSteps.length || steps.length}
                  </Text>
                </div>
                <div className="report-insight-card">
                  <Text className="report-insight-label">Latest stage</Text>
                  <Text fw={700}>
                    {latestCompletedStep ? formatStepName(latestCompletedStep.stepName) : 'Waiting for first review'}
                  </Text>
                </div>
              </div>
            </div>
          </Tabs.Panel>

          <Tabs.Panel value="decision" pt="lg">
            <div className="decision-panel">
              <div className="decision-panel-copy">
                <Text fw={700}>Consolidated reviewer findings</Text>
                <Text c="dimmed" fz="sm">
                  This shows the combined output from all expert reviewers. If reviews are still in progress,
                  partial findings are shown.
                </Text>
              </div>
              <ScrollArea h={420} offsetScrollbars>
                <Code block className="json-block">
                  {prettyJson(finalStep?.outputSnapshot ?? customerNextSteps?.outputSnapshot)}
                </Code>
              </ScrollArea>
            </div>
          </Tabs.Panel>

          <Tabs.Panel value="steps" pt="lg">
            <div className="steps-panel">
              <div className="rerun-panel">
                <Text fw={700} mb="xs">
                  Re-run from a specific stage
                </Text>
                <Text c="dimmed" mb="md">
                  Choose the stage you want to re-run from. A new review will be created without
                  affecting the current one.
                </Text>
                <Stack gap="sm">
                  {steps.map((step) => (
                    <Button
                      key={step.name}
                      justify="space-between"
                      radius="xl"
                      variant="default"
                      rightSection={<IconArrowBackUp size={16} />}
                      onClick={() => onRerun(step.name)}
                      disabled={!runDetail}
                    >
                      {formatStepName(step.name)}
                    </Button>
                  ))}
                </Stack>
              </div>

              <div className="step-accordion-panel">
                {visibleSteps.length === 0 ? (
                  <Text c="dimmed">Review stage details appear here once a review is selected.</Text>
                ) : (
                  <Accordion variant="separated" radius="xl" classNames={{ item: 'step-accordion-item' }}>
                    {visibleSteps.map((step) => (
                      <Accordion.Item key={step.id} value={step.id}>
                        <Accordion.Control>
                          <Group justify="space-between" wrap="nowrap" gap="sm">
                            <div>
                              <Text fw={700}>{formatStepName(step.stepName)}</Text>
                              <Text c="dimmed" fz="sm">
                                Review output details
                              </Text>
                            </div>
                            <Badge radius="xl" variant="light" color={statusColor[step.status]}>
                              {step.status}
                            </Badge>
                          </Group>
                        </Accordion.Control>
                        <Accordion.Panel>
                          <Code block className="json-block compact">
                            {prettyJson(step.outputSnapshot)}
                          </Code>
                        </Accordion.Panel>
                      </Accordion.Item>
                    ))}
                  </Accordion>
                )}
              </div>
            </div>
          </Tabs.Panel>
        </Tabs>
      </Stack>
    </Card>
  )
}
