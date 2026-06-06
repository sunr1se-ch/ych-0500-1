import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PlateSummary,
  PlateDetail,
  Impression,
  Warning,
  Incident,
  CreatePlateRequest,
  CreateImpressionRequest,
  CreateImpressionResponse
} from '../models/plate';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  getPlates(): Observable<PlateSummary[]> {
    return this.http.get<PlateSummary[]>(`${this.baseUrl}/plates`);
  }

  getPlate(id: number): Observable<PlateDetail> {
    return this.http.get<PlateDetail>(`${this.baseUrl}/plates/${id}`);
  }

  createPlate(data: CreatePlateRequest): Observable<PlateSummary> {
    return this.http.post<PlateSummary>(`${this.baseUrl}/plates`, data);
  }

  createImpression(data: CreateImpressionRequest): Observable<CreateImpressionResponse> {
    return this.http.post<CreateImpressionResponse>(`${this.baseUrl}/impressions`, data);
  }

  getIncidents(): Observable<Incident[]> {
    return this.http.get<Incident[]>(`${this.baseUrl}/incidents`);
  }

  resolveIncident(id: number, notes?: string): Observable<Incident> {
    return this.http.put<Incident>(`${this.baseUrl}/incidents/${id}/resolve`, { notes });
  }

  getWarnings(): Observable<Warning[]> {
    return this.http.get<Warning[]>(`${this.baseUrl}/warnings`);
  }

  acknowledgeWarning(id: number): Observable<Warning> {
    return this.http.put<Warning>(`${this.baseUrl}/warnings/${id}/acknowledge`, {});
  }

  downloadReport(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/plates/${id}/report`, {
      responseType: 'blob'
    });
  }
}
