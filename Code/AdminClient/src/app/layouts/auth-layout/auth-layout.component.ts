import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-auth-layout',
  templateUrl: './auth-layout.component.html',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet
  ]
})
export class AuthLayoutComponent {
  // Current year for the footer copyright
  currentYear = new Date().getFullYear();
} 