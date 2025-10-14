import { useState } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { Eye, CreditCard } from '@phosphor-icons/react';
import { StatusBadge } from './StatusBadge';
import { Invoice } from '@/lib/types';
import { formatCurrency } from '@/lib/utils';
import { Loader2 } from 'lucide-react';

interface InvoicesTableProps {
  invoices: Invoice[];
  onGetStatus: (invoiceId: string) => void;
  onReleasePayment: (invoiceId: string) => void;
  loadingActions: Record<string, boolean>;
  errorActions?: Record<string, string>;
}

export function InvoicesTable({
  invoices,
  onGetStatus,
  onReleasePayment,
  loadingActions,
  errorActions = {},
}: InvoicesTableProps) {
  return (
    <TooltipProvider>
      <div className="overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="border-b border-slate-200 dark:border-slate-700 bg-gradient-to-r from-slate-50 to-slate-100/50 dark:from-slate-800 dark:to-slate-800/50 hover:bg-slate-50 dark:hover:bg-slate-800">
              <TableHead className="font-semibold text-slate-700 dark:text-slate-300">Invoice ID</TableHead>
              <TableHead className="font-semibold text-slate-700 dark:text-slate-300">Vendor</TableHead>
              <TableHead className="text-right font-semibold text-slate-700 dark:text-slate-300">Amount</TableHead>
              <TableHead className="font-semibold text-slate-700 dark:text-slate-300">Status</TableHead>
              <TableHead className="text-right font-semibold text-slate-700 dark:text-slate-300">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {invoices.length === 0 ? (
              <TableRow className="hover:bg-transparent">
                <TableCell colSpan={5} className="text-center py-16">
                  <div className="flex flex-col items-center gap-3">
                    <div className="w-16 h-16 rounded-full bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-800 dark:to-slate-700 flex items-center justify-center">
                      <Eye size={32} className="text-slate-400 dark:text-slate-500" />
                    </div>
                    <p className="text-slate-500 dark:text-slate-400 font-medium">No invoices found</p>
                    <p className="text-sm text-slate-400 dark:text-slate-500">Start by adding some invoices to manage</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              invoices.map((invoice, index) => {
                const isLoadingStatus = loadingActions[`status-${invoice.invoiceId}`];
                const isLoadingPayment = loadingActions[`payment-${invoice.invoiceId}`];
                const statusError = errorActions[`status-${invoice.invoiceId}`];
                const paymentError = errorActions[`payment-${invoice.invoiceId}`];
                const isPaid = invoice.status === 'Paid';

                return (
                  <TableRow 
                    key={invoice.invoiceId} 
                    className="border-b border-slate-100 dark:border-slate-800 hover:bg-gradient-to-r hover:from-blue-50/50 hover:to-indigo-50/50 dark:hover:from-blue-950/20 dark:hover:to-indigo-950/20 transition-all duration-300 group animate-fade-in-up"
                    style={{
                      animationDelay: `${index * 50}ms`
                    }}
                  >
                    <TableCell className="font-mono text-sm">
                      <div className="flex items-center gap-2">
                        <div className="w-1.5 h-8 rounded-full bg-gradient-to-b from-blue-500 to-indigo-500 opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
                        <span className="font-medium text-slate-700 dark:text-slate-300">{invoice.invoiceId}</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className="font-medium text-slate-900 dark:text-slate-100">{invoice.vendorName}</span>
                    </TableCell>
                    <TableCell className="text-right">
                      <span className="font-bold text-lg bg-gradient-to-r from-slate-900 to-slate-700 dark:from-white dark:to-slate-300 bg-clip-text text-transparent">
                        {formatCurrency(invoice.amount, invoice.currency)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <div className="space-y-1">
                        <StatusBadge status={invoice.status} />
                        {(statusError || paymentError) && (
                          <p className="text-xs text-red-600 dark:text-red-400 font-medium">
                            {statusError || paymentError}
                          </p>
                        )}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex gap-2 justify-end">
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => onGetStatus(invoice.invoiceId)}
                              disabled={isLoadingStatus}
                              aria-label={`View status for invoice ${invoice.invoiceId}`}
                              className="hover:bg-blue-100 dark:hover:bg-blue-900/30 hover:text-blue-700 dark:hover:text-blue-300 transition-all duration-300 hover:scale-110"
                            >
                              {isLoadingStatus ? (
                                <Loader2 size={18} className="animate-spin" />
                              ) : (
                                <Eye size={18} weight="duotone" />
                              )}
                            </Button>
                          </TooltipTrigger>
                          <TooltipContent className="bg-slate-900 dark:bg-slate-100 text-white dark:text-slate-900 border-slate-700 dark:border-slate-300">
                            <p className="font-medium">View status</p>
                          </TooltipContent>
                        </Tooltip>
                        
                        {isPaid ? (
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                variant="ghost"
                                size="sm"
                                disabled
                                aria-label="Already paid"
                                className="opacity-40"
                              >
                                <CreditCard size={18} weight="duotone" />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent className="bg-slate-900 dark:bg-slate-100 text-white dark:text-slate-900 border-slate-700 dark:border-slate-300">
                              <p className="font-medium">Already paid</p>
                            </TooltipContent>
                          </Tooltip>
                        ) : (
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => onReleasePayment(invoice.invoiceId)}
                                disabled={isLoadingPayment}
                                aria-label={`Release payment for invoice ${invoice.invoiceId}`}
                                className="hover:bg-emerald-100 dark:hover:bg-emerald-900/30 hover:text-emerald-700 dark:hover:text-emerald-300 transition-all duration-300 hover:scale-110"
                              >
                                {isLoadingPayment ? (
                                  <Loader2 size={18} className="animate-spin" />
                                ) : (
                                  <CreditCard size={18} weight="duotone" />
                                )}
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent className="bg-slate-900 dark:bg-slate-100 text-white dark:text-slate-900 border-slate-700 dark:border-slate-300">
                              <p className="font-medium">Release payment</p>
                            </TooltipContent>
                          </Tooltip>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>
    </TooltipProvider>
  );
}