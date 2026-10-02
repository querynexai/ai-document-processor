import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface InvoiceSummary {
  invoiceId: number;
  filename: string;
  documentType: string | null;
  vendorName: string | null;
  invoiceNumber: string | null;
  invoiceDate: string | null;
  totalAmount: number | null;
  status: string;
  confidenceScore: number | null;
  uploadedAt: string;
}

export interface LineItem {
  lineItemId: number;
  lineNumber: number;
  description: string | null;
  quantity: number | null;
  unitPrice: number | null;
  totalPrice: number | null;
  confidenceScore: number | null;
}

export interface InvoiceDetail extends InvoiceSummary {
  dueDate: string | null;
  subtotal: number | null;
  taxAmount: number | null;
  currency: string | null;
  rawText: string | null;
  lineItems: LineItem[];
  processedAt: string | null;
}

export interface DashboardStats {
  totalInvoices: number;
  pendingReview: number;
  approved: number;
  averageConfidence: number;
  totalValue: number;
}

export interface ProcessingResult {
  invoiceId: number;
  success: boolean;
  overallConfidence: number | null;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = `${environment.apiBase}/documents`;

  list(status?: string): Observable<InvoiceSummary[]> {
    const url = status ? `${this.apiBase}?status=${status}` : this.apiBase;
    return this.http.get<InvoiceSummary[]>(url);
  }

  get(id: number): Observable<InvoiceDetail> {
    return this.http.get<InvoiceDetail>(`${this.apiBase}/${id}`);
  }

  stats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.apiBase}/stats`);
  }

  update(id: number, data: any): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/${id}`, data);
  }

  upload(file: File): Observable<ProcessingResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ProcessingResult>(`${this.apiBase}/upload`, formData);
  }
}