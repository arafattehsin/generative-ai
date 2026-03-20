import { Badge, Button, Card, Group, Stack, TagsInput, Text, Textarea, ThemeIcon, Title } from '@mantine/core'
import { IconArrowRight, IconPlayerPlay, IconUserCheck, IconSquareRoundedX } from '@tabler/icons-react'
import type { SampleRequest, WorkflowStatus } from '../lib/types'

interface IntakeStudioProps {
  samples: SampleRequest[]
  activeSampleIndex: number
  inputText: string
  isSubmitting: boolean
  currentRunStatus?: WorkflowStatus | string
  onPickSample: (index: number) => void
  onInputTextChange: (value: string) => void
  onStartRun: () => void
  onCancelRun: () => void
}

function getPreview(text: string) {
  return text.split('\n').filter(Boolean).slice(0, 2).join(' ')
}

export function IntakeStudio({
  samples,
  activeSampleIndex,
  inputText,
  isSubmitting,
  currentRunStatus,
  onPickSample,
  onInputTextChange,
  onStartRun,
  onCancelRun,
}: IntakeStudioProps) {
  const selectedSample = samples[activeSampleIndex]

  return (
    <Card className="surface-card intake-card" padding="xl" radius="32px">
      <Stack gap="lg">
        <Group justify="space-between" align="flex-start" gap="md">
          <div>
            <Group gap="sm" mb={10}>
              <ThemeIcon size={38} radius="xl" variant="light" color="orange">
                <IconUserCheck size={20} />
              </ThemeIcon>
              <div>
                <Text fw={700} tt="uppercase" fz="xs" c="dimmed">
                  New onboarding request
                </Text>
                <Title order={3} className="section-title">
                  Submit an applicant for review
                </Title>
              </div>
            </Group>
            <Text c="dimmed" maw={720}>
              Choose a sample applicant profile or paste a new onboarding request. Our AI reviewers will
              assess security, compliance, and finance risks and deliver a consolidated decision.
            </Text>
          </div>

          <Badge radius="xl" size="lg" variant="light" color={currentRunStatus === 'Running' ? 'orange' : 'gray'}>
            {currentRunStatus === 'Running' ? 'Under review' : currentRunStatus ?? 'Ready'}
          </Badge>
        </Group>

        <div className="studio-grid">
          <div className="studio-samples">
            <div className="studio-panel-header">
              <Text fw={700}>Example applicants</Text>
              <Text c="dimmed" fz="sm">
                Select one of these sample profiles to see how the review process works.
              </Text>
            </div>

            <div className="sample-list">
              {samples.map((sample, index) => (
                <button
                  key={sample.title}
                  type="button"
                  className={`sample-card ${index === activeSampleIndex ? 'sample-card-active' : ''}`}
                  onClick={() => onPickSample(index)}
                >
                  <span className="sample-card-kicker">Applicant 0{index + 1}</span>
                  <strong>{sample.title}</strong>
                  <span>{getPreview(sample.text)}</span>
                </button>
              ))}
            </div>
          </div>

          <div className="studio-editor">
            <div className="studio-editor-topline">
              <div>
                <Text fw={700}>Applicant details</Text>
                <Text c="dimmed" fz="sm">
                  {selectedSample?.title ?? 'Custom request'}
                </Text>
              </div>
              <Group gap="xs">
                <Badge radius="xl" variant="light" color="teal">
                  {inputText.trim().length} chars
                </Badge>
                <Badge radius="xl" variant="light" color="amber">
                  Ready for review
                </Badge>
              </Group>
            </div>

            <Textarea
              autosize
              minRows={14}
              maxRows={22}
              value={inputText}
              onChange={(event) => onInputTextChange(event.currentTarget.value)}
              placeholder="Paste the applicant onboarding details here..."
              classNames={{ input: 'editor-textarea' }}
            />

            <div className="studio-chip-row">
              <TagsInput
                readOnly
                value={[
                  'PII redaction',
                  'profile extraction',
                  'security review',
                  'compliance review',
                  'finance review',
                ]}
                classNames={{ input: 'tags-input-chip' }}
              />
            </div>

            <div className="studio-footer">
              <div className="studio-footnote">
                <Text fw={600}>How it works</Text>
                <Text c="dimmed" fz="sm">
                  Your request is reviewed by three AI experts simultaneously. You can re-run from any stage if needed.
                </Text>
              </div>

              <Group>
                <Button
                  variant="default"
                  radius="xl"
                  size="md"
                  leftSection={<IconSquareRoundedX size={18} />}
                  onClick={onCancelRun}
                  disabled={currentRunStatus !== 'Running'}
                >
                  Cancel review
                </Button>
                <Button
                  radius="xl"
                  size="md"
                  className="accent-button"
                  rightSection={isSubmitting ? <IconPlayerPlay size={18} /> : <IconArrowRight size={18} />}
                  loading={isSubmitting}
                  onClick={onStartRun}
                >
                  Submit for review
                </Button>
              </Group>
            </div>
          </div>
        </div>
      </Stack>
    </Card>
  )
}
