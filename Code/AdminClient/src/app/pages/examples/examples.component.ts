import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
    selector: 'app-examples',
    templateUrl: './examples.component.html',
    standalone: false
})
export class ExamplesComponent implements OnInit {
  
  constructor(private router: Router) {}
  
  ngOnInit() {
  }
  
  navigateTo(route: string) {
    this.router.navigate([route]);
  }
}
