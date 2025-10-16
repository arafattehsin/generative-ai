export type SessionResponse = {
  sessionId: string;
};

export type Invoice = {
  invoiceId: string;
  vendorName: string;
  amount: number;
  currency: string;
  status: "Pending" | "Paid" | "Rejected";
};

export type FunctionCall = {
  name: string;
  arguments: Record<string, unknown>;
};

export type ApprovalRequest = {
  id: string;
  type: "function-approval-request";
  functionCall: FunctionCall;
};

export type ApprovalDecision = {
  requestId: string;
  approved: boolean;
};

export type InvoiceStatusResult = {
  invoiceId: string;
  status: "Pending" | "Paid" | "Rejected";
  amount: number;
  vendorName: string;
};

export type PaymentResult = {
  invoiceId: string;
  status: "Pending" | "Paid" | "Rejected";
};

export type ActionResponse<T> = {
  result: T | null;
  userInputRequests: ApprovalRequest[];
};