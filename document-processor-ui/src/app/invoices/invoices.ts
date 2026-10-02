import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { DocumentService, InvoiceSummary, DashboardStats } from '../document.service';

@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoices.html',
  styleUrls: ['./invoices.css']
})
export class InvoicesComponent implements OnInit {
  private readonly api = inject(DocumentService);

  readonly invoices = signal<InvoiceSummary[]>([]);
  readonly stats = signal<DashboardStats | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.list().subscribe({
      next: (data) => { this.invoices.set(data); this.loading.set(false); }
    });
    this.api.stats().subscribe({
      next: (s) => this.stats.set(s)
    });
  }

  confidenceClass(score: number | null): string {
    if (score === null) return '';
    if (score >= 0.9) return 'high';
    if (score >= 0.75) return 'medium';
    return 'low';
  }

  statusClass(status: string): string {
    return status.toLowerCase().replace('_', '-');
  }

  formatCurrency(v: number | null): string {
    if (v === null) return '—';
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(v);
  }
}