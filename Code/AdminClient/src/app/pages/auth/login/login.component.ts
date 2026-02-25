import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { MessageModule } from 'primeng/message';
import { FloatLabelModule } from 'primeng/floatlabel';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { LoginService } from '../../../services/login.service';
import { DialogModule } from 'primeng/dialog';
import { finalize } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    CheckboxModule,
    ToastModule,
    RouterModule,
    MessageModule,
    FloatLabelModule,
    IconFieldModule,
    InputIconModule,
    DialogModule,
    TranslateModule
  ],
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  forgotPasswordForm: FormGroup;
  otpVerificationForm: FormGroup;
  resetPasswordForm: FormGroup;
  loginOtpForm: FormGroup;

  submitted: boolean = false;
  loading: boolean = false;

  showForgotPasswordDialog: boolean = false;
  showOtpVerificationDialog: boolean = false;
  showResetPasswordDialog: boolean = false;
  showLoginOtpDialog: boolean = false;
  otp: string = '';

  currentEmail: string = '';

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private messageService: MessageService,
    private loginService: LoginService,
    private translateService: TranslateService
  ) {}

  ngOnInit(): void {
    // Initialize forms
    this.initForms();

    // Check if user is already authenticated
    this.loginService.checkAuthStatus().subscribe({
      next: (isAuthenticated) => {
        if (isAuthenticated) {
          this.router.navigate(['/home']);
        }
      },
    });
  }

  private initForms(): void {
    this.loginForm = this.formBuilder.group({
      username: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false],
    });

    this.forgotPasswordForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
    });

    this.otpVerificationForm = this.formBuilder.group({
      otp: ['', [Validators.required, Validators.pattern('^[0-9]{6}$')]],
    });

    this.resetPasswordForm = this.formBuilder.group(
      {
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
      },
      {
        validators: this.passwordMatchValidator,
      }
    );
    
    this.loginOtpForm = this.formBuilder.group({
      otp: ['', [Validators.required, Validators.pattern('^[0-9]{6}$')]],
    });
  }

  get f() {
    return this.loginForm.controls;
  }
  get fpf() {
    return this.forgotPasswordForm.controls;
  }
  get ovf() {
    return this.otpVerificationForm.controls;
  }
  get rpf() {
    return this.resetPasswordForm.controls;
  }
  get lof() {
    return this.loginOtpForm.controls;
  }

  onSubmit(): void {
    this.submitted = true;

    // Stop here if form is invalid
    if (this.loginForm.invalid) {
      return;
    }

    this.loading = true;

    const email = this.f['username'].value;
    const password = this.f['password'].value;

    this.loginService
      .login(email, password)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            // Success, navigate to dashboard
            this.router.navigate(['/home']);
          } else if (response.requireOtp) {
            // OTP required for login
            this.showLoginOtpDialog = true;
            this.loginOtpForm.reset();
            this.messageService.add({
              severity: 'info',
              summary: 'Verification Required',
              detail: 'A verification code has been sent to your email.',
            });
          } else if (response.error) {
            // Show error message
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: response.error,
            });
          }
        },
      });
  }

  submitLoginOtpForm(): void {
    if (this.loginOtpForm.invalid) {
      return;
    }

    this.loading = true;
    const otp = this.lof['otp'].value;

    this.loginService
      .verifyLoginOtp(otp)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            // Success, navigate to dashboard
            this.showLoginOtpDialog = false;
            this.router.navigate(['/home']);
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Login successful',
            });
          } else if (response.error) {
            // Show error message
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: response.error,
            });
          }
        },
      });
  }

  forgotPassword(): void {
    this.submitted = false;
    this.forgotPasswordForm.reset();
    this.showForgotPasswordDialog = true;
  }

  submitForgotPasswordForm(): void {
    if (this.forgotPasswordForm.invalid) {
      return;
    }

    this.loading = true;
    const email = this.fpf['email'].value;
    this.currentEmail = email;

    this.loginService
      .forgotPassword(email)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (errorMessage) => {
          if (errorMessage === null) {
            this.showForgotPasswordDialog = false;
            this.showOtpVerificationDialog = true;
            this.messageService.add({
              severity: 'info',
              summary: 'OTP Sent',
              detail:
                'A one-time password has been sent to your email address.',
            });
            // Reset the OTP form
            this.otpVerificationForm.reset();
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: errorMessage,
            });
          }
        },
      });
  }

  submitOtpVerificationForm(): void {
    if (this.otpVerificationForm.invalid) {
      return;
    }

    this.otp = this.ovf['otp'].value;

    this.showOtpVerificationDialog = false;
    this.showResetPasswordDialog = true;
    // Reset the password form
    this.resetPasswordForm.reset();
  }

  submitResetPasswordForm(): void {
    if (this.resetPasswordForm.invalid) {
      return;
    }

    this.loading = true;
    const newPassword = this.rpf['newPassword'].value;
    const otp = this.otp;

    this.loginService
      .resetPassword(this.currentEmail, otp, newPassword)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (errorMessage) => {
          if (errorMessage === null) {
            this.showResetPasswordDialog = false;
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail:
                'Your password has been reset successfully. You can now log in with your new password.',
            });

            // Reset forms
            this.resetPasswordForm.reset();
            this.otpVerificationForm.reset();
            this.forgotPasswordForm.reset();
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: errorMessage,
            });
          }
        },
      });
  }

  passwordMatchValidator(
    formGroup: FormGroup
  ): { [key: string]: boolean } | null {
    const password = formGroup.get('newPassword')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;

    if (password !== confirmPassword) {
      formGroup.get('confirmPassword')?.setErrors({ passwordMismatch: true });
    } else {
      // Only clear the passwordMismatch error
      const confirmErrors = formGroup.get('confirmPassword')?.errors;
      if (
        confirmErrors &&
        Object.keys(confirmErrors).length === 1 &&
        confirmErrors['passwordMismatch']
      ) {
        formGroup.get('confirmPassword')?.setErrors(null);
      }
    }

    return null;
  }

  // Helper methods for dialogs
  closeForgotPasswordDialog(): void {
    this.showForgotPasswordDialog = false;
    this.forgotPasswordForm.reset();
  }

  closeOtpVerificationDialog(): void {
    this.showOtpVerificationDialog = false;
    this.otpVerificationForm.reset();
  }

  closeResetPasswordDialog(): void {
    this.showResetPasswordDialog = false;
    this.resetPasswordForm.reset();
  }
  
  closeLoginOtpDialog(): void {
    this.showLoginOtpDialog = false;
    this.loginOtpForm.reset();
  }
}
