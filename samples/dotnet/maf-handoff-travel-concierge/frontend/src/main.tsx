import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import OperationsApp from './OperationsApp'
import './index.css'

const isOperationsWorkspace = window.location.pathname.startsWith('/operations')

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {isOperationsWorkspace ? <OperationsApp /> : <App />}
  </StrictMode>,
)
