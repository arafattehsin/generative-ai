import { useState, useEffect, useRef } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { ApprovalRequest, ApprovalDecision } from '@/lib/types';
import { formatCurrency } from '@/lib/utils';

interface ApprovalModalProps {
  isOpen: boolean;
  onClose: () => void;
  approvalRequests: ApprovalRequest[];
  onSubmit: (decisions: ApprovalDecision[]) => void;
  isSubmitting?: boolean;
}

export function ApprovalModal({
  isOpen,
  onClose,
  approvalRequests,
  onSubmit,
  isSubmitting = false,
}: ApprovalModalProps) {
  const [decisions, setDecisions] = useState<Record<string, boolean>>({});
  const approveButtonRef = useRef<HTMLButtonElement>(null);

  // Focus management for accessibility
  useEffect(() => {
    if (isOpen && approveButtonRef.current) {
      // Small delay to ensure dialog is fully rendered
      const timer = setTimeout(() => {
        approveButtonRef.current?.focus();
      }, 100);
      return () => clearTimeout(timer);
    }
  }, [isOpen]);

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Escape' && !isSubmitting) {
      onClose();
    }
  };

  const handleApproveAll = () => {
    const allDecisions: ApprovalDecision[] = approvalRequests.map((request) => ({
      requestId: request.id,
      approved: true,
    }));
    onSubmit(allDecisions);
  };

  const handleRejectAll = () => {
    const allDecisions: ApprovalDecision[] = approvalRequests.map((request) => ({
      requestId: request.id,
      approved: false,
    }));
    onSubmit(allDecisions);
  };

  if (approvalRequests.length === 0) return null;

  const firstRequest = approvalRequests[0];
  const args = (firstRequest?.functionCall?.arguments ?? {}) as Record<string, unknown>;
  const rawName = firstRequest?.functionCall?.name ?? '';
  const fnLabel = /releasepayment/i.test(rawName) ? 'Release Payment' : (rawName || 'Function');
  const invoiceId = (args["invoiceId"] as string) ?? '';
  const amount = typeof args["amount"] === 'number' ? (args["amount"] as number) : Number(args["amount"] ?? NaN);
  const currency = (args["currency"] as string) ?? 'USD';
  const vendorName = (args["vendorName"] as string) ?? '';

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent 
        className="sm:max-w-xl bg-gradient-to-br from-white to-slate-50 dark:from-slate-900 dark:to-slate-950 border-slate-200 dark:border-slate-800 shadow-2xl" 
        aria-describedby="approval-description"
        onKeyDown={handleKeyDown}
      >
        <DialogHeader className="space-y-3 pb-6 border-b border-slate-200 dark:border-slate-800">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-amber-500 to-orange-600 flex items-center justify-center shadow-lg shadow-amber-500/30">
              <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
            <div>
              <DialogTitle className="text-2xl font-bold bg-gradient-to-r from-slate-900 to-slate-700 dark:from-white dark:to-slate-300 bg-clip-text text-transparent">
                Approval Required
              </DialogTitle>
              <p className="text-sm text-slate-600 dark:text-slate-400 mt-1">Review and approve this payment request</p>
            </div>
          </div>
        </DialogHeader>
        
        <div id="approval-description" className="space-y-5 py-6">
          <div className="p-6 rounded-2xl bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950/20 dark:to-indigo-950/20 border border-blue-200/50 dark:border-blue-800/30 shadow-lg">
            <div className="flex items-center justify-between mb-4">
              <p className="text-xs font-semibold text-blue-700 dark:text-blue-400 uppercase tracking-wider">Function Call</p>
              <span className="px-3 py-1 rounded-full bg-blue-600 text-white text-xs font-bold">{fnLabel}</span>
            </div>
            
            <div className="grid grid-cols-2 gap-4 mb-4">
              <div className="p-3 rounded-xl bg-white/60 dark:bg-slate-800/60 backdrop-blur-sm">
                <p className="text-xs text-slate-600 dark:text-slate-400 mb-1 font-medium">Invoice ID</p>
                <p className="font-mono text-sm font-bold text-slate-900 dark:text-white">{invoiceId}</p>
              </div>
              <div className="p-3 rounded-xl bg-white/60 dark:bg-slate-800/60 backdrop-blur-sm">
                <p className="text-xs text-slate-600 dark:text-slate-400 mb-1 font-medium">Amount</p>
                <p className="font-bold text-sm bg-gradient-to-r from-emerald-700 to-green-700 dark:from-emerald-400 dark:to-green-400 bg-clip-text text-transparent">
                  {formatCurrency(amount, currency)}
                </p>
              </div>
            </div>
            
            <div className="p-3 rounded-xl bg-white/60 dark:bg-slate-800/60 backdrop-blur-sm">
              <p className="text-xs text-slate-600 dark:text-slate-400 mb-1 font-medium">Vendor</p>
              <p className="text-sm font-bold text-slate-900 dark:text-white">{vendorName}</p>
            </div>
          </div>

          <Separator className="bg-gradient-to-r from-transparent via-slate-300 dark:via-slate-700 to-transparent" />

          <div className="p-5 rounded-2xl bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-950/20 dark:to-pink-950/20 border border-purple-200/50 dark:border-purple-800/30">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-lg bg-purple-600 dark:bg-purple-500 flex items-center justify-center flex-shrink-0 mt-0.5">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <div>
                <p className="text-xs font-semibold text-purple-700 dark:text-purple-400 uppercase tracking-wider mb-2">Impact</p>
                <p className="text-sm text-slate-700 dark:text-slate-300 leading-relaxed">
                  This will mark invoice <span className="font-mono font-bold text-blue-700 dark:text-blue-400">{invoiceId}</span> as <span className="font-bold text-emerald-700 dark:text-emerald-400">Paid</span> and release the payment of <span className="font-bold text-slate-900 dark:text-white">{formatCurrency(amount, currency)}</span> to <span className="font-bold text-slate-900 dark:text-white">{vendorName}</span>.
                </p>
              </div>
            </div>
          </div>

          <details className="group">
            <summary className="cursor-pointer p-4 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 transition-all duration-300">
              <span className="text-sm font-medium text-slate-700 dark:text-slate-300">View technical details</span>
            </summary>
            <div className="mt-3 p-4 rounded-xl bg-slate-900 dark:bg-slate-950 border border-slate-300 dark:border-slate-700 overflow-auto">
              <pre className="text-xs text-emerald-400 font-mono">{JSON.stringify(firstRequest.functionCall.arguments, null, 2)}</pre>
            </div>
          </details>

          {approvalRequests.length > 1 && (
            <div className="flex items-center gap-2 p-3 rounded-xl bg-amber-100 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800">
              <div className="w-6 h-6 rounded-full bg-amber-600 text-white flex items-center justify-center text-xs font-bold">
                {approvalRequests.length}
              </div>
              <p className="text-sm font-medium text-amber-900 dark:text-amber-200">
                Multiple approval requests pending
              </p>
            </div>
          )}
        </div>

        <DialogFooter className="gap-3 pt-6 border-t border-slate-200 dark:border-slate-800">
          <Button 
            variant="outline" 
            onClick={onClose} 
            disabled={isSubmitting}
            aria-label="Cancel approval request"
            className="hover:bg-slate-100 dark:hover:bg-slate-800 transition-all duration-300"
          >
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={handleRejectAll}
            disabled={isSubmitting}
            aria-label={`Reject payment for ${invoiceId}`}
            className="bg-gradient-to-r from-red-500 to-rose-600 hover:from-red-600 hover:to-rose-700 shadow-lg shadow-red-500/30 hover:shadow-red-500/50 transition-all duration-300 hover:scale-105"
          >
            {isSubmitting ? 'Processing...' : 'Reject Payment'}
          </Button>
          <Button
            ref={approveButtonRef}
            onClick={handleApproveAll}
            disabled={isSubmitting}
            aria-label={`Approve payment for ${invoiceId}`}
            className="bg-gradient-to-r from-emerald-500 to-green-600 hover:from-emerald-600 hover:to-green-700 shadow-lg shadow-emerald-500/30 hover:shadow-emerald-500/50 transition-all duration-300 hover:scale-105"
          >
            {isSubmitting ? 'Processing...' : 'Approve Payment'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}