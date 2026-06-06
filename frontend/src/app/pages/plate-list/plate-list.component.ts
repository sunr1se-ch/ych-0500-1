import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { PlateSummary, CreatePlateRequest, CreateImpressionRequest } from '../../models/plate';

@Component({
  selector: 'app-plate-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './plate-list.component.html'
})
export class PlateListComponent implements OnInit {
  plates: PlateSummary[] = [];
  loading = true;
  showCreateModal = false;
  showImpressionModal = false;
  selectedPlateId: number | null = null;

  newPlate: CreatePlateRequest = {
    steelPlateNumber: '',
    designDepth: 0,
    lifeLimit: 10000
  };

  newImpression: CreateImpressionRequest = {
    plateId: 0,
    offsetX: 0,
    offsetY: 0,
    actualTemperature: 0
  };

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadPlates();
  }

  loadPlates(): void {
    this.loading = true;
    this.api.getPlates().subscribe({
      next: (data) => {
        this.plates = data;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  getProgressColor(percentage: number): string {
    if (percentage >= 100) return 'red';
    if (percentage >= 80) return 'orange';
    return 'green';
  }

  getStatusBadge(plate: PlateSummary): { class: string; text: string } {
    if (plate.isLocked) return { class: 'badge-danger', text: '已锁定' };
    if (plate.lifePercentage >= 80) return { class: 'badge-warning', text: '寿命预警' };
    return { class: 'badge-success', text: '正常' };
  }

  openCreateModal(): void {
    this.newPlate = { steelPlateNumber: '', designDepth: 0, lifeLimit: 10000 };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  createPlate(): void {
    if (!this.newPlate.steelPlateNumber || this.newPlate.designDepth <= 0) return;

    this.api.createPlate(this.newPlate).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadPlates();
      },
      error: (err) => alert('创建失败: ' + (err.error?.message || err.message))
    });
  }

  openImpressionModal(plateId: number): void {
    this.selectedPlateId = plateId;
    this.newImpression = {
      plateId,
      offsetX: 0,
      offsetY: 0,
      actualTemperature: 120
    };
    this.showImpressionModal = true;
  }

  closeImpressionModal(): void {
    this.showImpressionModal = false;
    this.selectedPlateId = null;
  }

  submitImpression(): void {
    if (this.newImpression.plateId <= 0) return;

    this.api.createImpression(this.newImpression).subscribe({
      next: (resp) => {
        this.closeImpressionModal();
        this.loadPlates();
        let message = '压印记录已创建';
        if (resp.plateLocked) message += '\n注意：版材已达到寿命极限，已自动锁定';
        if (resp.warnings.length > 0) message += `\n警告: ${resp.warnings[0].message}`;
        if (resp.incidents.length > 0)
          message += `\n套准异常: ${resp.incidents[0].axis}轴连续偏移超标`;
        alert(message);
      },
      error: (err) => alert('创建失败: ' + (err.error?.title || err.message))
    });
  }

  getStats(): { total: number; warning: number; locked: number } {
    return {
      total: this.plates.length,
      warning: this.plates.filter((p) => p.lifePercentage >= 80 && !p.isLocked).length,
      locked: this.plates.filter((p) => p.isLocked).length
    };
  }

  getTotalCount(): number {
    return this.plates.length;
  }

  getWarningCount(): number {
    return this.plates.filter((p) => p.lifePercentage >= 80 && !p.isLocked).length;
  }

  getLockedCount(): number {
    return this.plates.filter((p) => p.isLocked).length;
  }

  getLifePercentageWidth(percentage: number): string {
    return Math.min(percentage, 100) + '%';
  }
}
