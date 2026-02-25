import { Injectable } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { BehaviorSubject, Observable } from 'rxjs';
import { Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { filter } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AdminBreadcrumbService {
  private breadcrumbsSubject = new BehaviorSubject<MenuItem[]>([]);
  breadcrumbs$: Observable<MenuItem[]> = this.breadcrumbsSubject.asObservable();

  constructor(private router: Router, private activatedRoute: ActivatedRoute) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      const breadcrumbs = this.createBreadcrumbs();
      this.breadcrumbsSubject.next(breadcrumbs);
    });
  }

  private createBreadcrumbs(): MenuItem[] {
    const breadcrumbs: MenuItem[] = [];
    let url = '';
    
    // Handle examples routes
    if (this.router.url.includes('/examples')) {
      breadcrumbs.push({
        label: 'Examples',
        routerLink: '/examples'
      });
      
      // Add specific example page
      if (this.router.url !== '/examples') {
        const path = this.router.url.split('/').pop();
        if (path) {
          const label = this.formatLabel(path);
          breadcrumbs.push({
            label,
            routerLink: this.router.url
          });
        }
      }
    } else {
      // Handle other routes
      const pathSegments = this.router.url.split('/').filter(segment => segment);
      
      pathSegments.forEach((segment, index) => {
        url += `/${segment}`;
        const label = this.formatLabel(segment);
        
        breadcrumbs.push({
          label,
          routerLink: url
        });
      });
    }
    
    return breadcrumbs;
  }
  
  private formatLabel(path: string): string {
    // Convert kebab-case to Title Case
    return path
      .split('-')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }
  
  // Method to manually set breadcrumbs if needed
  setBreadcrumbs(breadcrumbs: MenuItem[]): void {
    this.breadcrumbsSubject.next(breadcrumbs);
  }
} 