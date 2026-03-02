import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register-page.component.html',
  styleUrl: './register-page.component.scss'
})
export class RegisterPageComponent {
  name = '';
  password = '';
  isManager = false;
  profilePicture: File | null = null;
  errorMessage = '';
  isSubmitting = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.profilePicture = input.files[0];
    } else {
      this.profilePicture = null;
    }
  }

  submit(): void {
    this.errorMessage = '';
    this.isSubmitting = true;
    this.authService
      .register(this.name.trim(), this.password, this.isManager, this.profilePicture)
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.router.navigate(['/forums']);
        },
        error: (error: unknown) => {
          this.isSubmitting = false;
          this.errorMessage = error instanceof Error ? error.message : 'Failed to register.';
        }
      });
  }
}
