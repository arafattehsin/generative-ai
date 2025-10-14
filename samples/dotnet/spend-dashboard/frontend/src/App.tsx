import { useEffect, useState } from 'react';
import { QueryClient, QueryClientProvider, useQuery, useQueryClient } from '@tanstack/react-query';
import { Toaster, toast } from 'sonner';
import { AppShell } from '@/components/AppShell';
import { InvoicesTable } from '@/components/InvoicesTable';
import { ApprovalModal } from '@/components/ApprovalModal';
import { StatusPanel } from '@/components/StatusPanel';
import { Button } from '@/components/ui/button';
import { apiService } from '@/lib/api';
import { ApprovalRequest, InvoiceStatusResult, ApprovalDecision } from '@/lib/types';
import { ArrowClockwise } from '@phosphor-icons/react';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function SpendControlApp() {
  const [loadingActions, setLoadingActions] = useState<Record<string, boolean>>({});
  const [errorActions, setErrorActions] = useState<Record<string, string>>({});
  const [approvalRequests, setApprovalRequests] = useState<ApprovalRequest[]>([]);
  const [isApprovalModalOpen, setIsApprovalModalOpen] = useState(false);
  const [isStatusPanelOpen, setIsStatusPanelOpen] = useState(false);
  const [selectedInvoiceStatus, setSelectedInvoiceStatus] = useState<InvoiceStatusResult | null>(null);
  const [lastStatusUpdate, setLastStatusUpdate] = useState<Date | null>(null);
  const [isSubmittingApproval, setIsSubmittingApproval] = useState(false);
  

  const queryClient = useQueryClient();

  // Initialize session on app startup
  useEffect(() => {
    const initSession = async () => {
      try {
        await apiService.createSession();
      } catch (error) {
        toast.error('Failed to initialize session');
        console.error('Session initialization error:', error);
      }
    };

    initSession();
  }, []);

  // Fetch invoices
  const {
    data: invoices = [],
    isLoading: isLoadingInvoices,
    error: invoicesError,
    refetch: refetchInvoices,
  } = useQuery({
    queryKey: ['invoices'],
    queryFn: apiService.getInvoices,
    enabled: !!localStorage.getItem('spendctl.sessionId'),
  });

  // Handle invoice status checking
  const handleGetStatus = async (invoiceId: string) => {
    const actionKey = `status-${invoiceId}`;
    setLoadingActions(prev => ({ ...prev, [actionKey]: true }));
    setErrorActions(prev => ({ ...prev, [actionKey]: '' }));

    try {
      const response = await apiService.getStatus(invoiceId);
      
      if (response.result) {
        setSelectedInvoiceStatus(response.result);
        setLastStatusUpdate(new Date());
        setIsStatusPanelOpen(true);
        toast.success(`Status retrieved for ${invoiceId}`);
      }
    } catch (error) {
      const errorMessage = 'Failed to get status';
      setErrorActions(prev => ({ ...prev, [actionKey]: errorMessage }));
      toast.error(errorMessage);
      console.error('Get status error:', error);
      
      // Keep drawer open on failure if it was previously open
      if (selectedInvoiceStatus?.invoiceId === invoiceId) {
        setIsStatusPanelOpen(true);
      }
    } finally {
      setLoadingActions(prev => ({ ...prev, [actionKey]: false }));
    }
  };

  // Handle payment release
  const handleReleasePayment = async (invoiceId: string) => {
    const actionKey = `payment-${invoiceId}`;
    setLoadingActions(prev => ({ ...prev, [actionKey]: true }));
    setErrorActions(prev => ({ ...prev, [actionKey]: '' }));

    try {
      const response = await apiService.releasePayment(invoiceId);
      
      if (response.result) {
  // Auto-approved payment
        toast.success(`Payment released for ${invoiceId}`);
        queryClient.invalidateQueries({ queryKey: ['invoices'] });
      } else if (response.userInputRequests.length > 0) {
        // Approval required
        setApprovalRequests(response.userInputRequests);
        setIsApprovalModalOpen(true);
      }
    } catch (error) {
      const errorMessage = 'Failed to release payment';
      setErrorActions(prev => ({ ...prev, [actionKey]: errorMessage }));
      toast.error(errorMessage);
      console.error('Release payment error:', error);
    } finally {
      setLoadingActions(prev => ({ ...prev, [actionKey]: false }));
    }
  };

  // Handle approval submissions
  const handleApprovalSubmit = async (decisions: ApprovalDecision[]) => {
    setIsSubmittingApproval(true);

    try {
      const pendingIds = approvalRequests
        .map(r => (r.functionCall?.arguments?.invoiceId as string) || '')
        .filter(Boolean);
      const response = await apiService.submitApprovals(decisions);
      
      const approved = decisions.some(d => d.approved);
      if (approved && response.result) {
        toast.success(`Payment approved and released`);
      } else {
        toast.success('Approval denied; invoice marked Rejected');
      }
      
      setIsApprovalModalOpen(false);
      setApprovalRequests([]);
      // Refresh invoices list to reflect any change
      await queryClient.invalidateQueries({ queryKey: ['invoices'] });

      // If the status panel is open for this invoice, refresh it too
      if (selectedInvoiceStatus && pendingIds.includes(selectedInvoiceStatus.invoiceId)) {
        try {
          const statusResp = await apiService.getStatus(selectedInvoiceStatus.invoiceId);
          if (statusResp.result) {
            setSelectedInvoiceStatus(statusResp.result);
            setLastStatusUpdate(new Date());
            setIsStatusPanelOpen(true);
          }
        } catch (e) {
          // Non-fatal: keep current panel values if refresh fails
          console.warn('Failed to refresh status panel after approval decision:', e);
        }
      }
    } catch (error) {
      toast.error('Failed to submit approval decision');
      console.error('Approval submission error:', error);
    } finally {
      setIsSubmittingApproval(false);
    }
  };

  if (invoicesError) {
    return (
      <AppShell>
        <div className="text-center py-8">
          <p className="text-red-600 mb-4">Failed to load invoices</p>
          <Button onClick={() => refetchInvoices()}>Retry</Button>
        </div>
      </AppShell>
    );
  }

  return (
    <AppShell>
      <div className="space-y-8">
        {/* Modern header section with gradient card */}
        <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-white to-slate-50 dark:from-slate-800 dark:to-slate-900 border border-slate-200/60 dark:border-slate-700/60 shadow-xl shadow-slate-200/50 dark:shadow-none p-8">
          {/* Decorative background elements */}
          <div className="absolute top-0 right-0 w-64 h-64 bg-gradient-to-br from-blue-500/10 to-indigo-500/10 rounded-full blur-3xl"></div>
          <div className="absolute bottom-0 left-0 w-48 h-48 bg-gradient-to-tr from-purple-500/10 to-pink-500/10 rounded-full blur-3xl"></div>
          
          <div className="relative flex items-center justify-between">
            <div className="space-y-2">
              <h2 className="text-3xl font-bold bg-gradient-to-r from-slate-900 via-slate-800 to-slate-700 dark:from-white dark:via-slate-200 dark:to-slate-300 bg-clip-text text-transparent">
                Invoice Management
              </h2>
              <p className="text-slate-600 dark:text-slate-400 text-base">
                Manage invoice payments and approvals with ease
              </p>
              {invoices.length > 0 && (
                <div className="flex items-center gap-4 mt-4">
                  <div className="flex items-center gap-2 px-4 py-2 rounded-xl bg-gradient-to-r from-blue-500/10 to-indigo-500/10 border border-blue-500/20">
                    <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Total Invoices</span>
                    <span className="text-lg font-bold text-blue-600 dark:text-blue-400">{invoices.length}</span>
                  </div>
                  <div className="flex items-center gap-2 px-4 py-2 rounded-xl bg-gradient-to-r from-emerald-500/10 to-green-500/10 border border-emerald-500/20">
                    <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Paid</span>
                    <span className="text-lg font-bold text-emerald-600 dark:text-emerald-400">
                      {invoices.filter(inv => inv.status === 'Paid').length}
                    </span>
                  </div>
                </div>
              )}
            </div>
            <Button
              variant="outline"
              size="lg"
              onClick={() => refetchInvoices()}
              disabled={isLoadingInvoices}
              className="relative overflow-hidden backdrop-blur-sm bg-white/80 dark:bg-slate-800/80 border-slate-300 dark:border-slate-600 hover:bg-white dark:hover:bg-slate-700 hover:shadow-lg hover:scale-105 transition-all duration-300"
            >
              <ArrowClockwise 
                size={18} 
                className={`mr-2 ${isLoadingInvoices ? 'animate-spin' : ''}`} 
              />
              <span className="font-medium">Refresh</span>
            </Button>
          </div>
        </div>

        {/* Table with modern card styling */}
        <div className="rounded-2xl bg-white/80 dark:bg-slate-800/80 backdrop-blur-xl border border-slate-200/60 dark:border-slate-700/60 shadow-xl shadow-slate-200/50 dark:shadow-none overflow-hidden">
          <InvoicesTable
            invoices={invoices}
            onGetStatus={handleGetStatus}
            onReleasePayment={handleReleasePayment}
            loadingActions={loadingActions}
            errorActions={errorActions}
          />
        </div>
      </div>

      <ApprovalModal
        isOpen={isApprovalModalOpen}
        onClose={() => setIsApprovalModalOpen(false)}
        approvalRequests={approvalRequests}
        onSubmit={handleApprovalSubmit}
        isSubmitting={isSubmittingApproval}
      />

      <StatusPanel
        isOpen={isStatusPanelOpen}
        onClose={() => setIsStatusPanelOpen(false)}
        statusResult={selectedInvoiceStatus}
        lastUpdated={lastStatusUpdate}
      />
    </AppShell>
  );
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <SpendControlApp />
      <Toaster position="top-right" />
    </QueryClientProvider>
  );
}

export default App