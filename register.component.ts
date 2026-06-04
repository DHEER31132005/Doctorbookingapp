import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-patient-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CommonModule],
  template: `
    <div class="container flex justify-center items-center" style="min-height: 80vh; padding: 2rem 0;">
      <div class="glass-panel animate-fade-in" style="padding: 3rem; width: 100%; max-width: 500px;">
        <div class="text-center mb-8">
          <span class="material-icons gradient-text" style="font-size: 3rem;">person_add</span>
          <h2 class="mt-4">Patient Registration</h2>
          <p class="text-muted">Create your CareConnect account</p>
        </div>

        <div *ngIf="errorMsg" class="form-group" style="color: var(--error); text-align: center;">
          {{ errorMsg }}
        </div>

        <form [formGroup]="regForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label class="form-label">Full Name</label>
            <input type="text" formControlName="fullName" class="form-control" placeholder="John Doe">
          </div>
          
          <div class="form-group">
            <label class="form-label">Email</label>
            <input type="email" formControlName="email" class="form-control" placeholder="john@example.com">
          </div>

          <div class="form-group">
            <label class="form-label">Phone Number</label>
            <input type="text" formControlName="phoneNumber" class="form-control" placeholder="9876543210">
          </div>
          
          <div class="form-group">
            <label class="form-label">Password</label>
            <input type="password" formControlName="password" class="form-control" placeholder="Strong password">
          </div>

          <div class="form-group">
            <label class="form-label">Confirm Password</label>
            <input type="password" formControlName="confirmPassword" class="form-control" placeholder="Confirm password">
          </div>

          <button type="submit" class="btn-primary w-full">
            {{ loading ? 'Registering...' : 'Register' }}
            {{ loading ? 'Registering...' : 'Register' }}
          </button>
        </form>

        <div class="text-center mt-4 text-muted" style="font-size: 0.875rem;">
          Already have an account? <a routerLink="/patient/login">Login here</a>
        </div>
      </div>
    </div>
  `
})
export class PatientRegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  regForm = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.required],
    password: ['', Validators.required],
    confirmPassword: ['', Validators.required]
  });

  loading = false;
  errorMsg = '';

  onSubmit() {
    if (this.regForm.invalid) return;
    if (this.regForm.value.password !== this.regForm.value.confirmPassword) {
      this.errorMsg = 'Passwords do not match';
      return;
    }
    
    this.loading = true;
    this.errorMsg = '';
    
    this.auth.patientRegister(this.regForm.value).subscribe({
      next: () => this.router.navigate(['/patient/login']),
      error: (err) => {
        this.errorMsg = err.error?.message || 'Registration failed';
        this.loading = false;
      }
    });
  }
}
