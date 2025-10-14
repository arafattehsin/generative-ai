import { Badge } from '@/components/ui/badge';
import { CheckCircle, Clock, XCircle } from '@phosphor-icons/react';

interface StatusBadgeProps {
  status: "Pending" | "Paid" | "Rejected";
}

export function StatusBadge({ status }: StatusBadgeProps) {
  if (status === "Paid") {
    return (
      <Badge 
        variant="secondary" 
        className="relative overflow-hidden bg-gradient-to-r from-emerald-500 to-green-600 text-white border-0 shadow-lg shadow-emerald-500/30 hover:shadow-emerald-500/50 transition-all duration-300 hover:scale-105 px-3 py-1"
      >
        <div className="absolute inset-0 bg-gradient-to-r from-white/0 via-white/20 to-white/0 animate-shimmer"></div>
        <CheckCircle size={14} weight="fill" className="mr-1.5 relative z-10" />
        <span className="relative z-10 font-semibold text-sm">Paid</span>
      </Badge>
    );
  }

  if (status === "Rejected") {
    return (
      <Badge 
        variant="secondary" 
        className="relative overflow-hidden bg-gradient-to-r from-red-500 to-rose-600 text-white border-0 shadow-lg shadow-red-500/30 hover:shadow-red-500/50 transition-all duration-300 hover:scale-105 px-3 py-1"
      >
        <XCircle size={14} weight="fill" className="mr-1.5" />
        <span className="font-semibold text-sm">Rejected</span>
      </Badge>
    );
  }

  return (
    <Badge 
      variant="outline" 
      className="relative overflow-hidden bg-gradient-to-r from-amber-50 to-orange-50 dark:from-amber-950/30 dark:to-orange-950/30 text-amber-700 dark:text-amber-400 border-amber-200 dark:border-amber-800 hover:scale-105 transition-all duration-300 px-3 py-1"
    >
      <Clock size={14} weight="fill" className="mr-1.5 animate-pulse" />
      <span className="font-semibold text-sm">Pending</span>
    </Badge>
  );
}