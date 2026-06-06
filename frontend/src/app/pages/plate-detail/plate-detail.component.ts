import { Component, OnInit, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { PlateDetail, Impression, Warning, Incident, CreateImpressionRequest } from '../../models/plate';

@Component({
  selector: 'app-plate-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './plate-detail.component.html'
})
export class PlateDetailComponent implements OnInit, AfterViewChecked {
  plateId!: number;
  plate?: PlateDetail;
  loading = true;
  activeTab = 'timeline';
  showImpressionModal = false;
  needRedrawChart = false;

  newImpression: CreateImpressionRequest = {
    plateId: 0,
    offsetX: 0,
    offsetY: 0,
    actualTemperature: 0
  };

  @ViewChild('scatterCanvas') scatterCanvas?: ElementRef<HTMLCanvasElement>;

  constructor(private route: ActivatedRoute, private api: ApiService) {}

  ngOnInit(): void {
    this.plateId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadPlate();
  }

  ngAfterViewChecked(): void {
    if (this.needRedrawChart && this.activeTab === 'timeline') {
      this.drawScatterChart();
      this.needRedrawChart = false;
    }
  }

  loadPlate(): void {
    this.loading = true;
    this.api.getPlate(this.plateId).subscribe({
      next: (data) => {
        this.plate = data;
        this.loading = false;
        this.needRedrawChart = true;
      },
      error: () => (this.loading = false)
    });
  }

  getProgressColor(percentage: number): string {
    if (percentage >= 100) return 'red';
    if (percentage >= 80) return 'orange';
    return 'green';
  }

  setTab(tab: string): void {
    this.activeTab = tab;
    if (tab === 'timeline') {
      this.needRedrawChart = true;
    }
  }

  private drawScatterChart(): void {
    if (!this.scatterCanvas || !this.plate) return;

    const canvas = this.scatterCanvas.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const container = canvas.parentElement;
    if (!container) return;

    const rect = container.getBoundingClientRect();
    canvas.width = rect.width * window.devicePixelRatio;
    canvas.height = rect.height * window.devicePixelRatio;
    canvas.style.width = rect.width + 'px';
    canvas.style.height = rect.height + 'px';
    ctx.scale(window.devicePixelRatio, window.devicePixelRatio);

    const width = rect.width;
    const height = rect.height;
    const padding = { top: 40, right: 40, bottom: 50, left: 60 };
    const chartWidth = width - padding.left - padding.right;
    const chartHeight = height - padding.top - padding.bottom;

    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, width, height);

    const impressions = [...this.plate.impressions].reverse();
    if (impressions.length === 0) {
      ctx.fillStyle = '#999';
      ctx.font = '14px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText('暂无压印数据', width / 2, height / 2);
      return;
    }

    const maxOffset = 0.12;

    ctx.strokeStyle = '#f0f0f0';
    ctx.lineWidth = 1;

    for (let i = 0; i <= 4; i++) {
      const y = padding.top + (chartHeight / 4) * i;
      ctx.beginPath();
      ctx.moveTo(padding.left, y);
      ctx.lineTo(width - padding.right, y);
      ctx.stroke();

      const x = padding.left + (chartWidth / 4) * i;
      ctx.beginPath();
      ctx.moveTo(x, padding.top);
      ctx.lineTo(x, height - padding.bottom);
      ctx.stroke();
    }

    ctx.strokeStyle = '#ff4d4f';
    ctx.lineWidth = 2;
    ctx.setLineDash([5, 5]);

    const thresholdX = 0.08;
    const xPosRight = padding.left + chartWidth * ((thresholdX + maxOffset) / (maxOffset * 2));
    const xPosLeft = padding.left + chartWidth * ((-thresholdX + maxOffset) / (maxOffset * 2));
    const yPosTop = padding.top + chartHeight * ((maxOffset - thresholdX) / (maxOffset * 2));
    const yPosBottom = padding.top + chartHeight * ((maxOffset + thresholdX) / (maxOffset * 2));

    ctx.beginPath();
    ctx.moveTo(xPosRight, padding.top);
    ctx.lineTo(xPosRight, height - padding.bottom);
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(xPosLeft, padding.top);
    ctx.lineTo(xPosLeft, height - padding.bottom);
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(padding.left, yPosTop);
    ctx.lineTo(width - padding.right, yPosTop);
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(padding.left, yPosBottom);
    ctx.lineTo(width - padding.right, yPosBottom);
    ctx.stroke();

    ctx.setLineDash([]);

    const getColor = (x: number, y: number) => {
      const dist = Math.sqrt(x * x + y * y);
      if (dist > 0.1) return '#d4380d';
      if (dist > 0.08) return '#fa8c16';
      if (dist > 0.05) return '#fadb14';
      if (dist > 0.03) return '#73d13d';
      return '#52c41a';
    };

    impressions.forEach((imp, idx) => {
      const x = padding.left + chartWidth * ((imp.offsetX + maxOffset) / (maxOffset * 2));
      const y = padding.top + chartHeight * ((maxOffset - imp.offsetY) / (maxOffset * 2));

      ctx.beginPath();
      ctx.arc(x, y, 6, 0, Math.PI * 2);
      ctx.fillStyle = getColor(imp.offsetX, imp.offsetY) + 'cc';
      ctx.fill();
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 1;
      ctx.stroke();
    });

    ctx.fillStyle = '#333';
    ctx.font = '12px sans-serif';
    ctx.textAlign = 'right';
    for (let i = 0; i <= 4; i++) {
      const val = maxOffset - (maxOffset * 2 / 4) * i;
      const y = padding.top + (chartHeight / 4) * i;
      ctx.fillText(val.toFixed(2) + ' μm', padding.left - 10, y + 4);
    }

    ctx.textAlign = 'center';
    for (let i = 0; i <= 4; i++) {
      const val = -maxOffset + (maxOffset * 2 / 4) * i;
      const x = padding.left + (chartWidth / 4) * i;
      ctx.fillText(val.toFixed(2) + ' μm', x, height - padding.bottom + 20);
    }

    ctx.font = 'bold 14px sans-serif';
    ctx.fillText('X轴偏移 (μm)', width / 2, height - 15);

    ctx.save();
    ctx.translate(15, height / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.fillText('Y轴偏移 (μm)', 0, 0);
    ctx.restore();

    ctx.font = 'bold 14px sans-serif';
    ctx.fillStyle = '#1a1a1a';
    ctx.textAlign = 'left';
    ctx.fillText('偏移热力散点图 (按时间顺序)', padding.left, 20);

    const legend = [
      { color: '#52c41a', label: '< 0.03' },
      { color: '#73d13d', label: '0.03-0.05' },
      { color: '#fadb14', label: '0.05-0.08' },
      { color: '#fa8c16', label: '0.08-0.10' },
      { color: '#d4380d', label: '> 0.10' }
    ];

    let legendX = width - padding.right - 180;
    let legendY = 15;
    ctx.font = '11px sans-serif';

    legend.forEach((item) => {
      ctx.fillStyle = item.color;
      ctx.fillRect(legendX, legendY, 12, 12);
      ctx.fillStyle = '#666';
      ctx.fillText(item.label + ' μm', legendX + 18, legendY + 10);
      legendY += 18;
    });
  }

  openImpressionModal(): void {
    if (!this.plate) return;
    this.newImpression = {
      plateId: this.plate.id,
      offsetX: 0,
      offsetY: 0,
      actualTemperature: 120
    };
    this.showImpressionModal = true;
  }

  closeImpressionModal(): void {
    this.showImpressionModal = false;
  }

  submitImpression(): void {
    if (this.newImpression.plateId <= 0) return;

    this.api.createImpression(this.newImpression).subscribe({
      next: (resp) => {
        this.closeImpressionModal();
        this.loadPlate();
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

  async downloadReport(): Promise<void> {
    try {
      const blob = await firstValueFrom(this.api.downloadReport(this.plateId));
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `plate_${this.plateId}_report.pdf`;
      a.click();
      window.URL.revokeObjectURL(url);
    } catch (err: any) {
      let errorMessage = '下载失败';
      if (err.error instanceof Blob) {
        try {
          const errorText = await err.error.text();
          const errorJson = JSON.parse(errorText);
          errorMessage = errorJson.title || errorJson.message || errorMessage;
        } catch {
          errorMessage = '下载失败，请稍后重试';
        }
      } else if (err.error?.title) {
        errorMessage = err.error.title;
      } else if (err.message) {
        errorMessage = err.message;
      }
      alert(errorMessage);
    }
  }

  acknowledgeWarning(id: number): void {
    this.api.acknowledgeWarning(id).subscribe({
      next: () => this.loadPlate(),
      error: (err) => alert('操作失败: ' + (err.error?.title || err.message))
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString('zh-CN');
  }

  getOffsetStyle(offset: number): { [key: string]: string } {
    const abs = Math.abs(offset);
    if (abs > 0.1) return { color: '#d4380d', 'font-weight': 'bold' };
    if (abs > 0.08) return { color: '#fa8c16', 'font-weight': 'bold' };
    return {};
  }

  clampPercentage(value: number): number {
    return Math.min(value, 100);
  }

  getStatusBadgeClass(): string {
    if (!this.plate) return 'badge-success';
    if (this.plate.isLocked) return 'badge-danger';
    if (this.plate.lifePercentage >= 80) return 'badge-warning';
    return 'badge-success';
  }

  getStatusText(): string {
    if (!this.plate) return '正常';
    if (this.plate.isLocked) return '已锁定';
    if (this.plate.lifePercentage >= 80) return '寿命预警';
    return '正常';
  }

  getLifePercentageColor(): string {
    if (!this.plate) return '#52c41a';
    if (this.plate.lifePercentage >= 100) return '#ff4d4f';
    if (this.plate.lifePercentage >= 80) return '#faad14';
    return '#52c41a';
  }

  hasImpressions(): boolean {
    return !!this.plate?.impressions && this.plate.impressions.length > 0;
  }

  hasWarnings(): boolean {
    return !!this.plate?.warnings && this.plate.warnings.length > 0;
  }

  hasIncidents(): boolean {
    return !!this.plate?.incidents && this.plate.incidents.length > 0;
  }

  hasUnacknowledgedWarnings(): boolean {
    return !!this.plate?.warnings && this.plate.warnings.filter(w => !w.isAcknowledged).length > 0;
  }

  getUnacknowledgedWarningCount(): number {
    return this.plate?.warnings?.filter(w => !w.isAcknowledged).length || 0;
  }

  getWarningBadgeClass(type: string): string {
    return type === 'LifeExceeded' ? 'badge-danger' : 'badge-warning';
  }

  getWarningTypeText(type: string): string {
    return type === 'LifeExceeded' ? '寿命超限' : '寿命预警';
  }

  getWarningStatusBadgeClass(acknowledged: boolean): string {
    return acknowledged ? 'badge-success' : 'badge-warning';
  }

  getWarningStatusText(acknowledged: boolean): string {
    return acknowledged ? '已确认' : '待确认';
  }

  getIncidentStatusBadgeClass(resolved: boolean): string {
    return resolved ? 'badge-success' : 'badge-danger';
  }

  getIncidentStatusText(resolved: boolean): string {
    return resolved ? '已解决' : '待处理';
  }

  getTabActiveClass(tab: string): string {
    return this.activeTab === tab ? 'active' : '';
  }
}
