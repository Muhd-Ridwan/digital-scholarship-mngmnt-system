import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideAngularModule } from 'lucide-angular';
import { DocumentService } from '../../services/document.service';
import { ToastService } from '../../../shared/services/toast.service';
import { DocumentType, StudentDocument } from '../../models/document.model';
import { RouterLink } from '@angular/router';

const ALLOWED_EXTENSIONS = ['.pdf', '.doc', '.docx', '.png', '.jpg', '.jpeg'];

@Component({
  selector: 'app-student-documents',
  standalone: true,
  imports: [LucideAngularModule, DatePipe, RouterLink],
  templateUrl: './student-documents.html',
})
export class StudentDocuments {
  private readonly documentService = inject(DocumentService);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly documents = signal<StudentDocument[]>([]);
  protected readonly selectedType = signal<DocumentType | ''>('');
  private selectedFile: File | null = null;

  constructor() {
    this.loadDocuments();
  }

  private async loadDocuments() {
    try {
      this.documents.set(await this.documentService.getDocuments());
    } catch {
      this.toast.error('Could not load your documents.');
    } finally {
      this.loading.set(false);
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (file) {
      const dotIndex = file.name.lastIndexOf('.');
      const extension = dotIndex === -1 ? '' : file.name.slice(dotIndex).toLowerCase();
      if (!ALLOWED_EXTENSIONS.includes(extension)) {
        this.toast.error(
          'Unsupported file type. Allowed types: .pdf, .doc, .docx, .png, .jpg, .jpeg',
        );
        input.value = '';
        this.selectedFile = null;
        return;
      }
    }
    this.selectedFile = file;
  }

  onTypeChange(event: Event) {
    const select = event.target as HTMLSelectElement;
    this.selectedType.set(select.value as DocumentType);
  }

  async onUpload() {
    if (!this.selectedFile) {
      this.toast.error('Please choose a file first.');
      return;
    }
    if (!this.selectedType()) {
      this.toast.error('Please select a document type.');
      return;
    }

    this.uploading.set(true);

    try {
      const uploaded = await this.documentService.upload(
        this.selectedFile,
        this.selectedType() as DocumentType,
      );
      this.documents.update((docs) => [uploaded, ...docs]);
      this.toast.success('Document Uploaded.');
      this.selectedFile = null;
    } catch (err) {
      if (err instanceof HttpErrorResponse && typeof err.error === 'string') {
        this.toast.error(err.error);
      } else {
        this.toast.error('Could not upload the document.');
      }
    } finally {
      this.uploading.set(false);
    }
  }

  async onDownload(id: number) {
    try {
      const url = await this.documentService.getDownloadUrl(id);
      window.open(url, '_blank');
    } catch {
      this.toast.error('Could not get the download link.');
    }
  }

  async onDelete(id: number) {
    try {
      await this.documentService.deleteDocument(id);
      this.documents.update((docs) => docs.filter((d) => d.id !== id));
      this.toast.success('Document Deleted.');
    } catch {
      this.toast.error('Could not delete the document.');
    }
  }

  protected documentTypeLabel(type: DocumentType): string {
    switch (type) {
      case 'bank_statement':
        return 'Bank Statement';
      case 'academic_result':
        return 'Academic Result';
      case 'certificate':
        return 'Certificate';
      default:
        return 'Other';
    }
  }
}
