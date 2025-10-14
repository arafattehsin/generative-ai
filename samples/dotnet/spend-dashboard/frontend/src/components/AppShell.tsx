import { ReactNode } from 'react';
import { ArrowSquareOut, Wallet } from '@phosphor-icons/react';

interface AppShellProps {
  children: ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-50 dark:from-slate-950 dark:via-slate-900 dark:to-indigo-950 flex flex-col">
      {/* Header with glassmorphism */}
      <header className="sticky top-0 z-50 backdrop-blur-xl bg-white/70 dark:bg-slate-900/70 border-b border-white/20 dark:border-slate-700/50 shadow-lg shadow-black/5">
        <div className="container mx-auto px-6 h-20 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center shadow-lg shadow-blue-500/30">
              <Wallet size={24} weight="duotone" className="text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold bg-gradient-to-r from-slate-900 to-slate-700 dark:from-white dark:to-slate-300 bg-clip-text text-transparent">
                Spend Control
              </h1>
              <p className="text-xs text-slate-500 dark:text-slate-400">Financial Management Dashboard</p>
            </div>
          </div>
          
          <div className="flex items-center gap-2">
            <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 rounded-full bg-gradient-to-r from-emerald-500/10 to-green-500/10 border border-emerald-500/20">
              <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></div>
              <span className="text-xs font-medium text-emerald-700 dark:text-emerald-400">Live</span>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content with enhanced spacing */}
      <main className="flex-1 container mx-auto px-6 py-8 lg:py-12">
        {children}
      </main>

      {/* Footer with glassmorphism */}
      <footer className="backdrop-blur-xl bg-white/50 dark:bg-slate-900/50 border-t border-white/20 dark:border-slate-700/50 mt-auto">
        <div className="container mx-auto px-6 h-16 flex items-center justify-center">
          <a
            href="https://arafattehsin.com/"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400 hover:text-blue-600 dark:hover:text-blue-400 transition-all duration-300 hover:gap-3"
          >
            <span>Developed by Arafat Tehsin</span>
            <ArrowSquareOut size={16} className="opacity-60" />
          </a>
        </div>
      </footer>
    </div>
  );
}