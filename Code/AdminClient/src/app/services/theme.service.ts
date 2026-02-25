import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private darkMode = new BehaviorSubject<boolean>(false);
  darkMode$ = this.darkMode.asObservable();

  constructor() {
    // Check if user has a preference stored
    const savedTheme = localStorage.getItem('theme');
    
    if (savedTheme) {
      this.setDarkMode(savedTheme === 'dark');
    } else {
      // Check if user prefers dark mode based on system preference
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.setDarkMode(prefersDark);
    }

    // Listen for system theme changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
      if (localStorage.getItem('theme') === null) {
        this.setDarkMode(e.matches);
      }
    });
  }

  toggleDarkMode(): void {
    this.setDarkMode(!this.darkMode.value);
  }

  setDarkMode(isDark: boolean): void {
    this.darkMode.next(isDark);
    
    if (isDark) {
      document.querySelector('html').classList.add('dark-mode');
      // Force PrimeNG to update its theme
      document.documentElement.setAttribute('data-theme', 'dark');
      document.body.setAttribute('data-bs-theme', 'dark');  
    } else {
      document.querySelector('html').classList.remove('dark-mode');
      // Force PrimeNG to update its theme
      document.documentElement.setAttribute('data-theme', 'light');
      document.body.setAttribute('data-bs-theme', 'light');
    }
    
    // Save preference to localStorage
    localStorage.setItem('theme', isDark ? 'dark' : 'light');
  }
} 