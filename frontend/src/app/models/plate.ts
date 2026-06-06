export interface PlateSummary {
  id: number;
  steelPlateNumber: string;
  designDepth: number;
  lifeLimit: number;
  impressionCount: number;
  isLocked: boolean;
  lifePercentage: number;
  createdAt: string;
}

export interface PlateDetail extends PlateSummary {
  impressions: Impression[];
  warnings: Warning[];
  incidents: Incident[];
}

export interface Impression {
  id: number;
  plateId: number;
  offsetX: number;
  offsetY: number;
  actualTemperature: number;
  createdAt: string;
}

export interface Warning {
  id: number;
  plateId: number;
  warningType: string;
  message: string;
  isAcknowledged: boolean;
  createdAt: string;
}

export interface Incident {
  id: number;
  plateId: number;
  steelPlateNumber: string;
  startImpressionId: number;
  endImpressionId: number;
  axis: string;
  notes?: string;
  isResolved: boolean;
  createdAt: string;
}

export interface CreatePlateRequest {
  steelPlateNumber: string;
  designDepth: number;
  lifeLimit: number;
}

export interface CreateImpressionRequest {
  plateId: number;
  offsetX: number;
  offsetY: number;
  actualTemperature: number;
}

export interface CreateImpressionResponse {
  impression: Impression;
  warnings: Warning[];
  incidents: Incident[];
  plateLocked: boolean;
}
