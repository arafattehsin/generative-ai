import {
  SessionResponse,
  Invoice,
  ActionResponse,
  InvoiceStatusResult,
  PaymentResult,
  ApprovalDecision,
} from './types';

const API_BASE = (import.meta.env.VITE_API_BASE_URL as string) || 'http://localhost:5071';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, init);
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `HTTP ${res.status}`);
  }
  return res.json() as Promise<T>;
}

class ApiService {
  private get sessionId(): string | null {
    return localStorage.getItem('spendctl.sessionId');
  }

  private set sessionId(value: string | null) {
    if (value) localStorage.setItem('spendctl.sessionId', value);
  }

  async createSession(): Promise<SessionResponse> {
    const res = await request<SessionResponse>('/api/session', {
      method: 'POST',
    });
    this.sessionId = res.sessionId;
    return res;
  }

  async getInvoices(): Promise<Invoice[]> {
    return request<Invoice[]>('/api/invoices');
  }

  async getStatus(invoiceId: string): Promise<ActionResponse<InvoiceStatusResult>> {
    let sid = this.sessionId;
    if (!sid) {
      await this.createSession();
      sid = this.sessionId;
    }
    try {
      return await request<ActionResponse<InvoiceStatusResult>>('/api/actions/get-status', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sessionId: sid, invoiceId }),
      });
    } catch (err: any) {
      const msg = String(err?.message || err || '');
      if (msg.includes('Invalid session')) {
        await this.createSession();
        sid = this.sessionId;
        return request<ActionResponse<InvoiceStatusResult>>('/api/actions/get-status', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ sessionId: sid, invoiceId }),
        });
      }
      throw err;
    }
  }

  async releasePayment(invoiceId: string): Promise<ActionResponse<PaymentResult>> {
    let sid = this.sessionId;
    if (!sid) {
      await this.createSession();
      sid = this.sessionId;
    }
    try {
      return await request<ActionResponse<PaymentResult>>('/api/actions/release-payment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sessionId: sid, invoiceId }),
      });
    } catch (err: any) {
      const msg = String(err?.message || err || '');
      if (msg.includes('Invalid session')) {
        await this.createSession();
        sid = this.sessionId;
        return request<ActionResponse<PaymentResult>>('/api/actions/release-payment', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ sessionId: sid, invoiceId }),
        });
      }
      throw err;
    }
  }

  async submitApprovals(approvals: ApprovalDecision[]): Promise<ActionResponse<PaymentResult>> {
    let sid = this.sessionId;
    if (!sid) {
      await this.createSession();
      sid = this.sessionId;
    }
    try {
      return await request<ActionResponse<PaymentResult>>('/api/approvals', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sessionId: sid, approvals }),
      });
    } catch (err: any) {
      const msg = String(err?.message || err || '');
      if (msg.includes('Invalid session')) {
        await this.createSession();
        sid = this.sessionId;
        return request<ActionResponse<PaymentResult>>('/api/approvals', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ sessionId: sid, approvals }),
        });
      }
      throw err;
    }
  }
}

export const apiService = new ApiService();