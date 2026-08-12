import { Component, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideAngularModule } from 'lucide-angular';
import { ScholarshipService } from '../../../scholarships/services/scholarship.service';
import { ScholarshipDetail } from '../../../scholarships/models/scholarship.model';
import { DocumentService } from '../../services/document.service';
import {
  DocumentType,
  StudentDocument,
  DOCUMENT_TYPE_LABELS,
  DOCUMENT_TYPE_CLASSES,
} from '../../models/document.model';
import { ApplicationService } from '../../services/application.service';
import { ToastService } from '../../../shared/services/toast.service';

const ALLOWED_EXTENSIONS = ['.pdf', '.doc', '.docx', '.png', '.jpg', '.jpeg'];

@Component({
  selector: 'app-scholarship-apply',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, RouterLink, LucideAngularModule],
  templateUrl: './scholarship-apply.html',
})
export class ScholarshipApply {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly scholarshipService = inject(ScholarshipService);
  private readonly documentService = inject(DocumentService);
  private readonly applicationService = inject(ApplicationService);
  private readonly toast = inject(ToastService);

  protected readonly scholarshipId = Number(this.route.snapshot.paramMap.get('scholarshipId'));

  protected readonly scholarship = signal<ScholarshipDetail | null>(null);
  protected readonly documents = signal<StudentDocument[]>([]);
  protected readonly selectedIds = signal<Set<number>>(new Set());
  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly submitting = signal(false);
  protected readonly selectedType = signal<DocumentType | ''>('');
  private selectedFile: File | null = null;

  constructor() {
    this.loadData();
  }

  private async loadData() {
    try {
      const [scholarship, documents] = await Promise.all([
        this.scholarshipService.getById(this.scholarshipId),
        this.documentService.getDocuments(),
      ]);
      this.scholarship.set(scholarship);
      this.documents.set(documents);
    } catch {
      this.toast.error('Could not load this scholarship. Please try again later.');
    } finally {
      this.loading.set(false);
    }
  }

  toggleDocument(id: number) {
    this.selectedIds.update((ids) => {
      const next = new Set(ids);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  protected documentTypeLabel(type: DocumentType): string {
    return DOCUMENT_TYPE_LABELS[type];
  }

  protected documentTypeClasses(type: DocumentType): string {
    return DOCUMENT_TYPE_CLASSES[type];
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

  async onUploadNew() {
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
      this.toggleDocument(uploaded.id);
      this.toast.success('Document uploaded and selected.');
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

  async onSubmit() {
    this.submitting.set(true);

    try {
      await this.applicationService.apply({
        scholarshipId: this.scholarshipId,
        existingDocumentIds: [...this.selectedIds()],
      });
      this.toast.success('Application submitted successfully.');
      await this.router.navigateByUrl('/student/applications');
    } catch (err) {
      if (err instanceof HttpErrorResponse && typeof err.error === 'string') {
        this.toast.error(err.error);
      } else {
        this.toast.error('Could not submit your application.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
