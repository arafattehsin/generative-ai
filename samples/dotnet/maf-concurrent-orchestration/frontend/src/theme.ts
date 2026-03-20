import { createTheme } from '@mantine/core'

export const theme = createTheme({
  primaryColor: 'amber',
  defaultRadius: 'xl',
  fontFamily: 'Manrope Variable, ui-sans-serif, sans-serif',
  headings: {
    fontFamily: 'Fraunces Variable, Georgia, serif',
    fontWeight: '700',
  },
  colors: {
    amber: [
      '#fff3dc',
      '#fce3b4',
      '#f8d188',
      '#f3bc5a',
      '#efaa37',
      '#e79620',
      '#cc7915',
      '#a55b14',
      '#854916',
      '#6d3c15',
    ],
    ink: [
      '#f8f4ec',
      '#ece4d7',
      '#d7c8ae',
      '#c1aa80',
      '#ab8c52',
      '#8a6f39',
      '#6a542a',
      '#4a3a1c',
      '#2f2513',
      '#171108',
    ],
  },
  primaryShade: 5,
})
