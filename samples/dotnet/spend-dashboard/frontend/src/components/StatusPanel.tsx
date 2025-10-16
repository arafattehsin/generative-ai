import { useEffect, useRef } from 'react';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { StatusBadge } from './StatusBadge';
import { InvoiceStatusResult } from '@/lib/types';
import { formatCurrency, formatDateTime } from '@/lib/utils';
import { Clock } from '@phosphor-icons/react';

interface StatusPanelProps {
  isOpen: boolean;
  onClose: () => void;
  statusResult: InvoiceStatusResult | null;
  lastUpdated: Date | null;
}

export function StatusPanel({
  isOpen,
  onClose,
  statusResult,
  lastUpdated,
}: StatusPanelProps) {
  const contentRef = useRef<HTMLDivElement>(null);

  // Focus management for accessibility
  useEffect(() => {
    if (isOpen && contentRef.current) {
      const focusableElement = contentRef.current.querySelector('button, [tabindex]:not([tabindex="-1"])') as HTMLElement;
      if (focusableElement) {
        focusableElement.focus();
      }
    }
  }, [isOpen]);

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Escape') {
      onClose();
    }
  };

  return (
    <Sheet open={isOpen} onOpenChange={onClose}>
      <SheetContent 
        className="w-full sm:max-w-md bg-gradient-to-br from-white to-slate-50 dark:from-slate-900 dark:to-slate-950 border-l border-slate-200 dark:border-slate-800"
        ref={contentRef}
        onKeyDown={handleKeyDown}
        aria-describedby={statusResult ? "status-description" : undefined}
      >
        <SheetHeader className="space-y-3 pb-6 border-b border-slate-200 dark:border-slate-800">
          <SheetTitle className="text-2xl font-bold bg-gradient-to-r from-slate-900 to-slate-700 dark:from-white dark:to-slate-300 bg-clip-text text-transparent">
            Invoice Status
          </SheetTitle>
          <p className="text-sm text-slate-600 dark:text-slate-400">
            Detailed invoice information and status
          </p>
        </SheetHeader>
        
        {statusResult ? (
          <div id="status-description" className="mt-8 space-y-6" role="region" aria-live="polite">
            <div className="p-5 rounded-2xl bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950/20 dark:to-indigo-950/20 border border-blue-200/50 dark:border-blue-800/30 shadow-lg">
              <h3 className="text-xs font-semibold text-blue-700 dark:text-blue-400 uppercase tracking-wider mb-2">Invoice ID</h3>
              <p className="font-mono text-lg font-bold text-slate-900 dark:text-white">{statusResult.invoiceId}</p>
            </div>
            
            <div className="p-5 rounded-2xl bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-950/20 dark:to-pink-950/20 border border-purple-200/50 dark:border-purple-800/30 shadow-lg">
              <h3 className="text-xs font-semibold text-purple-700 dark:text-purple-400 uppercase tracking-wider mb-2">Vendor</h3>
              <p className="text-lg font-bold text-slate-900 dark:text-white">{statusResult.vendorName}</p>
            </div>
            
            <div className="p-5 rounded-2xl bg-gradient-to-br from-emerald-50 to-green-50 dark:from-emerald-950/20 dark:to-green-950/20 border border-emerald-200/50 dark:border-emerald-800/30 shadow-lg">
              <h3 className="text-xs font-semibold text-emerald-700 dark:text-emerald-400 uppercase tracking-wider mb-2">Amount</h3>
              <p className="text-2xl font-bold bg-gradient-to-r from-emerald-700 to-green-700 dark:from-emerald-400 dark:to-green-400 bg-clip-text text-transparent">
                {formatCurrency(statusResult.amount, 'USD')}
              </p>
            </div>
            
            <div className="p-5 rounded-2xl bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-800 dark:to-slate-900 border border-slate-200 dark:border-slate-700 shadow-lg">
              <h3 className="text-xs font-semibold text-slate-700 dark:text-slate-400 uppercase tracking-wider mb-3">Current Status</h3>
              <StatusBadge status={statusResult.status} />
            </div>
            
            {lastUpdated && (
              <div className="p-5 rounded-2xl bg-gradient-to-br from-amber-50 to-orange-50 dark:from-amber-950/20 dark:to-orange-950/20 border border-amber-200/50 dark:border-amber-800/30 shadow-lg">
                <h3 className="text-xs font-semibold text-amber-700 dark:text-amber-400 uppercase tracking-wider mb-2">Last Updated</h3>
                <p className="text-sm font-medium text-slate-700 dark:text-slate-300">{formatDateTime(lastUpdated)}</p>
              </div>
            )}
          </div>
        ) : (
          <div className="mt-12 text-center">
            <div className="w-20 h-20 mx-auto rounded-full bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-800 dark:to-slate-700 flex items-center justify-center mb-4">
              <Clock size={40} className="text-slate-400 dark:text-slate-500" weight="duotone" />
            </div>
            <p className="text-slate-600 dark:text-slate-400 font-medium">No status information available</p>
            <p className="text-sm text-slate-500 dark:text-slate-500 mt-2">Check back later for updates</p>
          </div>
        )}
      </SheetContent>
    </Sheet>
  );
}