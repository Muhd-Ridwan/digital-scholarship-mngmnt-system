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
    'University',
    'Course',
    'StudyLevel',
    'Category',
  ];
  protected readonly activeCategory = signal<ReferenceCategory>('University');

  private readonly items = signal<ReferenceDataItem[]>([]);

  constructor() {
    // MOCK read now; becomes a real HTTP GET once the backend endpoint is available.
    this.referenceApi.getAll().subscribe((list) => this.items.set(list));
  }

  protected readonly visibleItems = computed(() =>
    this.items().filter((item) => item.category === this.activeCategory()),
  );

  protected setCategory(category: ReferenceCategory): void {
    this.activeCategory.set(category);
  }

  protected toggleActive(item: ReferenceDataItem): void {
    this.items.update((list) =>
      list.map((i) => (i.id === item.id ? { ...i, isActive: !i.isActive } : i)),
    );
    this.toastService.success(
      item.isActive ? `Deactivated: ${item.label}` : `Activated: ${item.label}`,
    );
  }

  protected onAddSubmit(codeInput: HTMLInputElement, labelInput: HTMLInputElement): void {
    const code = codeInput.value.trim();
    const label = labelInput.value.trim();
    if (!code || !label) {
      this.toastService.error('Code and label are both required');
      return;
    }
    this.items.update((list) => [
      ...list,
      { id: `r-${Date.now()}`, category: this.activeCategory(), code, label, isActive: true },
    ]);
    this.toastService.success(`Added to ${this.categoryLabels[this.activeCategory()]}: ${label}`);
    codeInput.value = '';
    labelInput.value = '';
  }
}
