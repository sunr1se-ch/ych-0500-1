import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'plates', pathMatch: 'full' },
  {
    path: 'plates',
    loadComponent: () =>
      import('./pages/plate-list/plate-list.component').then((m) => m.PlateListComponent)
  },
  {
    path: 'plates/:id',
    loadComponent: () =>
      import('./pages/plate-detail/plate-detail.component').then((m) => m.PlateDetailComponent)
  },
  {
    path: 'incidents',
    loadComponent: () =>
      import('./pages/incident-list/incident-list.component').then((m) => m.IncidentListComponent)
  },
  { path: '**', redirectTo: 'plates' }
];
