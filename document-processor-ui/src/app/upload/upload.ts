import { Component, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { DocumentService } from '../document.service';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './upload.html',
  styleUrls: ['./upload.css']
})
export class UploadComponent {
  private readonly api = inject(DocumentService);
  private readonly router = inject(Router);

  readonly uploading = signal(false);
  readonly dragOver = signal(false);
  readonly message = signal('');
  readonly isError = signal(false);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.processFile(input.files[0]);
    }
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    if (event.dataTransfer?.files.length) {
      this.processFile(event.dataTransfer.files[0]);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(true);
  }

  onDragLeave(): void {
    this.dragOver.set(false);
  }

  private processFile(file: File): void {
    this.uploading.set(true);
    this.message.set('');
    this.isError.set(false);

    this.api.upload(file).subscribe({
      next: (res) => {
        this.uploading.set(false);
        if (res.success) {
          this.message.set(`✅ Processed successfully. Confidence: ${((res.overallConfidence ?? 0) * 100).toFixed(0)}%`);
          setTimeout(() => this.router.navigate(['/invoices', res.invoiceId]), 1200);
        } else {
          this.isError.set(true);
          this.message.set(`❌ ${res.error}`);
        }
      },
      error: (err) => {
        this.uploading.set(false);
        this.isError.set(true);
        this.message.set(err.error?.error ?? 'Upload failed.');
      }
    });
  }
}