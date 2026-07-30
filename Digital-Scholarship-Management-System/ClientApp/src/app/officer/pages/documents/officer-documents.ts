import { Component, inject, signal } from '@angular/core';
import { OnInit } from '@angular/core';
import { OfficerDocumentService } from '../../services/officer-document.service';
import { ToastService } from '../../../shared/services/toast.service';
import { LucideAngularModule } from 'lucide-angular';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-officer-documents',
  standalone: true,
  imports: [LucideAngularModule, DatePipe, RouterLink, CommonModule],
  templateUrl: './officer-documents.html'
})

export class OfficerDocumentsComponent{
  private readonly documentService = inject(OfficerDocumentService);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(true);
  protected readonly documents = signal<any[]>([]);

  constructor() {
    this.loadDocuments();
  }

  async loadDocuments() {
    try {
      this.documents.set(await this.documentService.getAllDocuments());
    } catch {
      this.toast.error('Could not load documents.');
    }finally{
      this.loading.set(false);
    }
  }

  async reviewDocument(id: number, status: 'Approved' | 'Rejected') {
    try {
      await this.documentService.reviewDocument(id, status);
      this.toast.success(`Document ${status}.`);
      this.loadDocuments();
    } catch {
      this.toast.error('Could not update status.');
    }
  }

  async downloadDocument(id: number) {
    try {
      const url = await this.documentService.getDownloadUrlOfficer(id);
      window.open(url, '_blank');
    } catch {
      this.toast.error('Could not download document.');
    }
  }
}
