import { Component, OnInit } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { AdminBreadcrumbService } from '../../services/breadcrumb.service';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'admin-breadcrumb',
  standalone: true,
  imports: [BreadcrumbModule, CommonModule],
  template: `
    <p-breadcrumb 
      class="max-w-full" 
      [model]="items" 
      [home]="home"
      styleClass="border-0 p-0 bg-transparent mb-3">
    </p-breadcrumb>
  `
})
export class AdminBreadcrumbComponent implements OnInit {
  items: MenuItem[] = [];
  home: MenuItem = { icon: 'pi pi-home', routerLink: '/' };

  constructor(private breadcrumbService: AdminBreadcrumbService) {}

  ngOnInit() {
    this.breadcrumbService.breadcrumbs$.subscribe(breadcrumbs => {
      this.items = breadcrumbs;
    });
  }
} 