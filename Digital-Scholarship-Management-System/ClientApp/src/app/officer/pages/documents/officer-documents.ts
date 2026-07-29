import { Component } from '@angular/core';
import { OnInit } from '@angular/core';
import { OfficerDocumentService } from '../../services/officer-document.service';
import { ToastService } from '../../../shared/services/toast.service';


@Component({
  selector: 'app-officer-documents',
  templateUrl: './officer-documents.html'
})
export class OfficerDocumentsComponent implements OnInit {
  documents: any[] = [];

  constructor(private documentService: OfficerDocumentService, private toast: ToastService) {}

  ngOnInit() {
    this.loadDocuments();
  }

  async loadDocuments() {
    try {
      this.documents = await this.documentService.getAllDocuments();
    } catch {
      this.toast.error('Could not load documents.');
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
