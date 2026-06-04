import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-patient-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CommonModule],
  template: `
    <div class="container flex justify-center items-center" style="min-height: 80vh;">
      <div class="glass-panel animate-fade-in" style="padding: 3rem; width: 100%; max-width: 450px;">
        <div class="text-center mb-8">
          <span class="material-icons gradient-text" style="font-size: 3rem;">personal_injury</span>
          <h2 class="mt-4">Patient Login</h2>
          <p class="text-muted">Welcome back to CareConnect</p>
        </div>

        <div *ngIf="errorMsg" class="form-group" style="color: var(--error); text-align: center;">
          {{ errorMsg }}
        </div>

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label class="form-label">Email</label>
            <input type="email" formControlName="email" class="form-control" placeholder="Enter your email">
          </div>
          <div class="form-group">
            <label class="form-label">Phone Number</label>
            <input type="text" formControlName="phoneNumber" class="form-control" placeholder="Enter your phone number">
          </div>
          <div class="form-group">
            <label class="form-label">Password</label>
            <input type="password" formControlName="password" class="form-control" placeholder="Enter your password">
          </div>

          <button type="submit" class="btn-primary w-full">
            {{ loading ? 'Logging in...' : 'Login' }}
            {{ loading ? 'Logging in...' : 'Login' }}
          </button>
        </form>

        <div class="text-center mt-4">
          <a routerLink="/patient/forgot-password" style="font-size: 0.875rem;">Forgot Password?</a>
        </div>
        <div class="text-center mt-4 text-muted" style="font-size: 0.875rem;">
          Don't have an account? <a routerLink="/patient/register">Register here</a>
        </div>

        <div *ngIf="showOtpModal" class="modal-overlay" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); display: flex; justify-content: center; align-items: center;">
          <div class="glass-panel" style="padding: 2rem; background: white; border-radius: 8px;">
            <h3>Enter OTP</h3>
            <input type="text" [(ngModel)]="enteredOtp" class="form-control" placeholder="Enter 6-digit OTP">
            <button (click)="verifyOtp()" class="btn-primary" style="margin-top: 1rem;">Verify</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class PatientLoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
    password: ['', Validators.required]
  });

  loading = false;
  errorMsg = '';
  private generatedOtp: string = '';
  showOtpModal: boolean = false;
  enteredOtp: string = '';

  onSubmit() {
    if (this.loginForm.invalid) return;
    this.loading = true;
    this.errorMsg = '';
    
    this.generatedOtp = Math.floor(100000 + Math.random() * 900000).toString();
    console.log('Mock OTP sent to email:', this.generatedOtp);
    this.showOtpModal = true;
    this.loading = false;
  }

  verifyOtp() {
    if (this.enteredOtp === this.generatedOtp) {
      this.auth.patientLogin(this.loginForm.value).subscribe({
        next: () => this.router.navigate(['/patient/dashboard/specialties']),
        error: (err) => {
          this.errorMsg = err.error?.message || 'Login failed';
        }
      });
      this.showOtpModal = false;
    } else {
      this.errorMsg = 'Invalid OTP';
    }
  }
}
