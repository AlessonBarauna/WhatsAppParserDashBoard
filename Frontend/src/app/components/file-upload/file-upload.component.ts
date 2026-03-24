import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, UploadResult } from '../../services/api.service';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './file-upload.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileUploadComponent {
  private readonly apiService = inject(ApiService);

  protected readonly selectedFile = signal<File | null>(null);
  protected readonly isUploading = signal(false);
  protected readonly result = signal<UploadResult | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly isDragOver = signal(false);

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.setFile(input.files[0]);
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  protected onDragLeave(): void {
    this.isDragOver.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) this.setFile(file);
  }

  protected upload(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.isUploading.set(true);
    this.result.set(null);
    this.error.set(null);

    this.apiService.uploadFile(file).subscribe({
      next: (res) => {
        this.result.set(res);
        this.isUploading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error ?? 'Upload failed. Please try again.');
        this.isUploading.set(false);
      },
    });
  }

  protected reset(): void {
    this.selectedFile.set(null);
    this.result.set(null);
    this.error.set(null);
  }

  private setFile(file: File): void {
    this.result.set(null);
    this.error.set(null);
    this.selectedFile.set(file);
  }
}
