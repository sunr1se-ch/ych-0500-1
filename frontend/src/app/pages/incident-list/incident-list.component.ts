import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Incident } from '../../models/plate';

@Component({
  selector: 'app-incident-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './incident-list.component.html'
})
export class IncidentListComponent implements OnInit {
  incidents: Incident[] = [];
  loading = true;
  filter = 'all';
  showResolveModal = false;
  selectedIncident: Incident | null = null;
  resolveNotes = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadIncidents();
  }

  loadIncidents(): void {
    this.loading = true;
    this.api.getIncidents().subscribe({
      next: (data) => {
        this.incidents = data;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  get filteredIncidents(): Incident[] {
    if (this.filter === 'pending') return this.incidents.filter((i) => !i.isResolved);
    if (this.filter === 'resolved') return this.incidents.filter((i) => i.isResolved);
    return this.incidents;
  }

  getStats(): { total: number; pending: number; resolved: number } {
    return {
      total: this.incidents.length,
      pending: this.incidents.filter((i) => !i.isResolved).length,
      resolved: this.incidents.filter((i) => i.isResolved).length
    };
  }

  getTotalCount(): number {
    return this.incidents.length;
  }

  getPendingCount(): number {
    return this.incidents.filter((i) => !i.isResolved).length;
  }

  getResolvedCount(): number {
    return this.incidents.filter((i) => i.isResolved).length;
  }

  getFilterActiveClass(filterValue: string): string {
    return this.filter === filterValue ? 'active' : '';
  }

  setFilter(filterValue: string): void {
    this.filter = filterValue;
  }

  getIncidentStatusBadgeClass(resolved: boolean): string {
    return resolved ? 'badge-success' : 'badge-danger';
  }

  getIncidentStatusText(resolved: boolean): string {
    return resolved ? '已解决' : '待处理';
  }

  getEmptyMessage(): string {
    if (this.loading) return '';
    if (this.filter === 'all') return '暂无套准异常记录';
    if (this.filter === 'pending') return '暂无待处理的异常';
    return '暂无已解决的异常';
  }

  openResolveModal(incident: Incident): void {
    this.selectedIncident = incident;
    this.resolveNotes = '';
    this.showResolveModal = true;
  }

  closeResolveModal(): void {
    this.showResolveModal = false;
    this.selectedIncident = null;
    this.resolveNotes = '';
  }

  resolveIncident(): void {
    if (!this.selectedIncident) return;

    this.api.resolveIncident(this.selectedIncident.id, this.resolveNotes || undefined).subscribe({
      next: () => {
        this.closeResolveModal();
        this.loadIncidents();
        alert('异常已标记为已解决');
      },
      error: (err) => alert('操作失败: ' + (err.error?.title || err.message))
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString('zh-CN');
  }
}
