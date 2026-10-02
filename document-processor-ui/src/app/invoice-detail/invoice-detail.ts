import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentService, InvoiceDetail } from '../document.service';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoice-detail.html',
  styleUrls: ['./invoice-detail.css']
})
export class InvoiceDetailComponent implements OnInit {
  private readonly api = inject(DocumentService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly invoice = signal<InvoiceDetail | null>(null);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly showRawText = signal(false);

  form = {
    vendorName: '',
    invoiceNumber: '',
    invoiceDate: '',
    dueDate: '',
    subtotal: null as number | null,
    taxAmount: null as number | null,
    totalAmount: null as number | null,
    currency: '',
    notes: ''
  };

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.get(id).subscribe({
      next: (inv) => {
        this.invoice.set(inv);
        this.form = {
          vendorName: inv.vendorName ?? '',
          invoiceNumber: inv.invoiceNumber ?? '',
          invoiceDate: inv.invoiceDate?.slice(0, 10) ?? '',
          dueDate: inv.dueDate?.slice(0, 10) ?? '',
          subtotal: inv.subtotal,
          taxAmount: inv.taxAmount,
          totalAmount: inv.totalAmount,
          currency: inv.currency ?? 'INR',
          notes: ''
        };
      }
    });
  }

  save(): void {
    const inv = this.invoice();
    if (!inv) return;

    this.saving.set(true);
    this.saved.set(false);

    const payload = {
      vendorName: this.form.vendorName || null,
      invoiceNumber: this.form.invoiceNumber || null,
      invoiceDate: this.form.invoiceDate || null,
      dueDate: this.form.dueDate || null,
      subtotal: this.form.subtotal,
      taxAmount: this.form.taxAmount,
      totalAmount: this.form.totalAmount,
      currency: this.form.currency || null,
      notes: this.form.notes || null
    };

    this.api.update(inv.invoiceId, payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.router.navigate(['/invoices']), 800);
      },
      error: () => this.saving.set(false)
    });
  }

  toggleRawText(): void {
    this.showRawText.update(v => !v);
  }

  formatCurrency(v: number | null, currency = 'INR'): string {
    if (v === null) return '—';
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency, maximumFractionDigits: 2 }).format(v);
  }
}