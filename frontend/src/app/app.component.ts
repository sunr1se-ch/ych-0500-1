import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar">
      <div class="navbar-content">
        <div class="navbar-brand">烫金版材追溯工作台</div>
        <ul class="navbar-nav">
          <li><a routerLink="/plates" routerLinkActive="active">版材管理</a></li>
          <li><a routerLink="/incidents" routerLinkActive="active">套准异常</a></li>
        </ul>
      </div>
    </nav>
    <div class="container">
      <router-outlet></router-outlet>
    </div>
  `
})
export class AppComponent {}
