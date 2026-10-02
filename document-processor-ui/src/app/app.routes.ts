import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'invoices', pathMatch: 'full' },
  { path: 'upload', loadComponent: () => import('./upload/upload').then(m => m.UploadComponent) },
  { path: 'invoices', loadComponent: () => import('./invoices/invoices').then(m => m.InvoicesComponent) },
  { path: 'invoices/:id', loadComponent: () => import('./invoice-detail/invoice-detail').then(m => m.InvoiceDetailComponent) }
];