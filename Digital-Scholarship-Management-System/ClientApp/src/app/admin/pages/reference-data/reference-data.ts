import { Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService } from '../../../shared/services/toast.service';
import {
  REFERENCE_CATEGORY_LABELS,
  ReferenceCategory,
  ReferenceDataItem,
} from '../../../shared/models/reference-data.model';
import { ReferenceDataService } from '../../../shared/services/api/reference-data.service';

@Component({
  selector: 'app-admin-reference-data',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './reference-data.html',
})
export class AdminReferenceData {
  private readonly toastService = inject(ToastService);
  private readonly referenceApi = inject(ReferenceDataService);

  protected readonly categoryLabels = REFERENCE_CATEGORY_LABELS;
  protected readonly categories: ReferenceCategory[] = [
    'FundType',
    'StudyLocation',
    'OrganisationType',
  ];
  protected readonly activeCategory = signal<ReferenceCategory>('FundType');
  protected readonly loading = signal(true);

  private readonly items = signal<ReferenceDataItem[]>([]);

  constructor() {
    this.loadItems();
  }

  private async loadItems(): Promise<void> {
    try {
      this.items.set(await this.referenceApi.getAll());
    } catch {
      this.toastService.error('Could not load reference data.');
    } finally {
      this.loading.set(false);
    }
  }

  protected readonly visibleItems = computed(() =>
    this.items().filter((item) => item.category === this.activeCategory()),
  );

  protected setCategory(category: ReferenceCategory): void {
    this.activeCategory.set(category);
  }
}
