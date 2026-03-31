import { useDeferredValue, useMemo, useState, startTransition } from 'react'
import { Alert, Container, Grid, Loader, Stack, Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { IconAlertCircle } from '@tabler/icons-react'
import { BackgroundStage } from './components/BackgroundStage'
import { HeaderBar } from './components/HeaderBar'
import { IntakeStudio } from './components/IntakeStudio'
import { OutputDeck } from './components/OutputDeck'
import { RunBoard } from './components/RunBoard'
import { RunHistory } from './components/RunHistory'
import { useRunStream } from './hooks/useRunStream'
import { api } from './lib/api'
import { countRunsByStatus } from './lib/presentation'

export default function App() {
  const queryClient = useQueryClient()
  const [manualSelectedRunId, setManualSelectedRunId] = useState<string | null>(null)
  const [activeSampleIndex, setActiveSampleIndex] = useState(0)
  const [editedInputText, setEditedInputText] = useState<string | null>(null)
  const [historySearch, setHistorySearch] = useState('')
  const deferredSearch = useDeferredValue(historySearch)

  const samplesQuery = useQuery({ queryKey: ['samples'], queryFn: api.getSamples })
  const runsQuery = useQuery({ queryKey: ['runs'], queryFn: api.getRuns, refetchInterval: 7000 })
  const stepsQuery = useQuery({ queryKey: ['steps'], queryFn: api.getSteps })
  const selectedRunId = manualSelectedRunId ?? runsQuery.data?.[0]?.id ?? null
  const inputText = editedInputText ?? samplesQuery.data?.[activeSampleIndex]?.text ?? ''
  const runDetailQuery = useQuery({
    queryKey: ['run', selectedRunId],
    queryFn: () => api.getRun(selectedRunId!),
    enabled: Boolean(selectedRunId),
    refetchInterval: selectedRunId ? 4000 : false,
  })

  const timeline = useRunStream(selectedRunId)

  const createRun = useMutation({
    mutationFn: api.createRun,
    onSuccess: (result) => {
      setManualSelectedRunId(result.runId)
      void queryClient.invalidateQueries({ queryKey: ['runs'] })
      showNotification({
        title: 'Run started',
        message: `OnboardFlow run ${result.runId.slice(0, 8)} is now live.`,
        color: 'teal',
      })
    },
    onError: (error) => {
      showNotification({
        title: 'Could not start run',
        message: error instanceof Error ? error.message : 'Unknown error',
        color: 'red',
      })
    },
  })

  const rerun = useMutation({
    mutationFn: ({ runId, fromStep }: { runId: string; fromStep: string }) => api.rerun(runId, { fromStep }),
    onSuccess: (result) => {
      setManualSelectedRunId(result.newRunId)
      void queryClient.invalidateQueries({ queryKey: ['runs'] })
      showNotification({
        title: 'Rerun launched',
        message: `Replaying from the selected checkpoint. New run ${result.newRunId.slice(0, 8)}.`,
        color: 'orange',
      })
    },
  })

  const cancelRun = useMutation({
    mutationFn: api.cancelRun,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['runs'] })
      if (selectedRunId) {
        void queryClient.invalidateQueries({ queryKey: ['run', selectedRunId] })
      }
      showNotification({
        title: 'Run cancelled',
        message: 'The orchestration was asked to halt after the current superstep.',
        color: 'gray',
      })
    },
  })

  const filteredRuns = useMemo(() => {
    const runs = runsQuery.data ?? []
    const needle = deferredSearch.trim().toLowerCase()
    if (!needle) {
      return runs
    }

    return runs.filter((run) => {
      const haystack = `${run.id} ${run.status} ${run.rerunFromStep ?? ''}`.toLowerCase()
      return haystack.includes(needle)
    })
  }, [deferredSearch, runsQuery.data])

  const currentStatus = runDetailQuery.data?.status
  const runs = runsQuery.data ?? []
  const runningRunCount = countRunsByStatus(runs, 'Running')

  const isLoadingShell = samplesQuery.isLoading || runsQuery.isLoading || stepsQuery.isLoading
  const shellError = samplesQuery.error ?? runsQuery.error ?? stepsQuery.error

  return (
    <div className="app-shell">
      <BackgroundStage />
      <Container size="xl" className="app-container">
        <Stack gap="xl">
          <HeaderBar
            selectedRunId={selectedRunId}
            currentStatus={currentStatus}
            totalRuns={runs.length}
            runningRunCount={runningRunCount}
            concurrentLaneCount={(stepsQuery.data ?? []).filter((step) => step.isConcurrent).length}
          />

          {shellError ? (
            <Alert icon={<IconAlertCircle size={18} />} color="red" radius="xl" className="shell-alert">
              {shellError instanceof Error ? shellError.message : 'The frontend could not reach the backend API.'}
            </Alert>
          ) : null}

          {isLoadingShell ? (
            <div className="loading-shell">
              <Loader color="orange" />
              <Text c="dimmed">Loading OnboardFlow...</Text>
            </div>
          ) : (
            <>
              <Grid gutter="xl" align="start">
                <Grid.Col span={{ base: 12, xl: 8 }}>
                  <IntakeStudio
                    samples={samplesQuery.data ?? []}
                    activeSampleIndex={activeSampleIndex}
                    inputText={inputText}
                    isSubmitting={createRun.isPending}
                    currentRunStatus={currentStatus}
                    onPickSample={(index) => {
                      startTransition(() => {
                        setActiveSampleIndex(index)
                        setEditedInputText(null)
                      })
                    }}
                    onInputTextChange={setEditedInputText}
                    onStartRun={() => createRun.mutate({ inputText })}
                    onCancelRun={() => selectedRunId && cancelRun.mutate(selectedRunId)}
                  />
                </Grid.Col>

                <Grid.Col span={{ base: 12, xl: 4 }}>
                  <RunHistory
                    runs={filteredRuns}
                    search={historySearch}
                    onSearchChange={setHistorySearch}
                    selectedRunId={selectedRunId}
                    onSelectRun={setManualSelectedRunId}
                  />
                </Grid.Col>
              </Grid>

              <RunBoard runDetail={runDetailQuery.data} steps={stepsQuery.data ?? []} timeline={timeline} />

              <OutputDeck
                runDetail={runDetailQuery.data}
                steps={stepsQuery.data ?? []}
                onRerun={(fromStep) => {
                  if (!selectedRunId) {
                    return
                  }
                  rerun.mutate({ runId: selectedRunId, fromStep })
                }}
              />
            </>
          )}
        </Stack>
      </Container>
    </div>
  )
}
