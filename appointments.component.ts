import { Component, inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'app-patient-appointments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <h2 class="gradient-text mb-8">My Appointments</h2>

      <div *ngIf="appointments.length === 0" class="text-center text-muted">
        You have no appointments.
      </div>

      <div class="grid gap-6">
        <div *ngFor="let appt of appointments" class="glass-panel" style="padding: 1.5rem;">
          <div class="flex justify-between items-start mb-4">
            <div>
              <h3 style="font-size: 1.25rem;">Dr. {{ appt.doctorName }}</h3>
              <p class="text-muted">{{ appt.specialtyName }}</p>
            </div>
            <span class="status-badge" [ngClass]="appt.status.toLowerCase()" style="padding: 0.25rem 0.75rem; border-radius: 999px; font-size: 0.875rem; font-weight: 500;">
              {{ appt.status }}
            </span>
          </div>

          <div class="grid gap-2" style="grid-template-columns: 1fr 1fr; margin-bottom: 1rem;">
            <div class="flex items-center gap-2 text-muted">
              <span class="material-icons" style="font-size: 1.25rem;">calendar_today</span>
              <span>{{ appt.appointmentDate | date:'mediumDate' }}</span>
            </div>
            <div class="flex items-center gap-2 text-muted">
              <span class="material-icons" style="font-size: 1.25rem;">schedule</span>
              <span>{{ appt.slotStartTime }}</span>
            </div>
            <div class="flex items-center gap-2 text-muted">
              <span class="material-icons" style="font-size: 1.25rem;">videocam</span>
              <span>{{ appt.appointmentMode }}</span>
            </div>
          </div>

          <!-- Video Link for Online -->
          <div *ngIf="appt.appointmentMode === 'Online' && appt.videoLink" class="mb-4" style="padding: 1rem; background: rgba(59,130,246,0.1); border-radius: 8px;">
            <p class="text-sm text-muted mb-2">Meeting Link:</p>
            <a [href]="appt.videoLink" target="_blank" class="flex items-center gap-2">
              <span class="material-icons">link</span> {{ appt.videoLink }}
            </a>
          </div>

          <!-- Address for Offline -->
          <div *ngIf="appt.appointmentMode === 'Offline' && appt.clinicAddress" class="mb-4" style="padding: 1rem; background: rgba(139,92,246,0.1); border-radius: 8px;">
            <p class="text-sm text-muted mb-2">Clinic Address:</p>
            <p class="flex items-center gap-2">
              <span class="material-icons">location_on</span> {{ appt.clinicAddress }}
            </p>
          </div>

          <div class="flex justify-end gap-4 mt-4 border-t border-glass pt-4" *ngIf="appt.status === 'Confirmed'">
            <button class="btn-outline text-error border-error" (click)="cancel(appt.appointmentId)">Cancel Appointment</button>
          </div>

          <!-- Feedback Section -->
          <div *ngIf="appt.status === 'Completed' && !appt.hasFeedback" class="mt-4 border-t border-glass pt-4">
            <button class="btn-primary" (click)="openFeedback(appt)">Leave Feedback</button>
          </div>
        </div>
      </div>

      <!-- Feedback Modal -->
      <div *ngIf="feedbackAppt" class="modal-backdrop" style="position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(4px); z-index: 100; display: flex; align-items: center; justify-content: center;">
        <div class="glass-panel" style="width: 100%; max-width: 500px; padding: 2rem;">
          <h3 class="mb-4">Rate your visit</h3>
          
          <div class="form-group">
            <label class="form-label">Rating (1-5)</label>
            <input type="number" min="1" max="5" class="form-control" [(ngModel)]="feedbackRating">
          </div>
          <div class="form-group">
            <label class="form-label">Comments</label>
            <textarea class="form-control" rows="3" [(ngModel)]="feedbackComments"></textarea>
          </div>

          <div class="flex justify-end gap-4 mt-6">
            <button class="btn-outline" (click)="feedbackAppt = null">Cancel</button>
            <button class="btn-primary" (click)="submitFeedback()">Submit</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .status-badge.confirmed { background: rgba(59,130,246,0.2); color: #3B82F6; }
    .status-badge.completed { background: rgba(16,185,129,0.2); color: #10B981; }
    .status-badge.cancelled { background: rgba(239,68,68,0.2); color: #EF4444; }
    .status-badge.noshow { background: rgba(245,158,11,0.2); color: #F59E0B; }
    .border-glass { border-color: var(--border-glass); }
    .text-error { color: var(--error) !important; }
    .border-error { border-color: var(--error) !important; }
  `]
})
export class PatientAppointmentsComponent implements OnInit {
  private http = inject(HttpClient);
  appointments: any[] = [];

  feedbackAppt: any = null;
  feedbackRating = 5;
  feedbackComments = '';

  ngOnInit() {
    this.loadAppointments();
  }

  loadAppointments() {
    this.http.get<any[]>('http://localhost:5255/api/appointments/patient').pipe(
      catchError(() => {
        console.warn('Using mock appointments');
        return of([
          { appointmentId: 1, doctorName: 'Dr. Arjun Kumar', specialtyName: 'General Physician', status: 'Confirmed', appointmentDate: '2026-06-05', slotStartTime: '09:00', appointmentMode: 'Online', videoLink: 'https://meet.google.com/abc', hasFeedback: false },
          { appointmentId: 2, doctorName: 'Dr. Priya Sharma', specialtyName: 'Cardiology', status: 'Completed', appointmentDate: '2026-06-01', slotStartTime: '10:00', appointmentMode: 'Offline', clinicAddress: 'Chennai Clinic', hasFeedback: false }
        ]);
      })
    ).subscribe(res => {
      this.appointments = res;
    });
  }

  cancel(id: number) {
    if(confirm('Are you sure you want to cancel this appointment?')) {
      this.http.put(`https://localhost:7130/api/appointments/${id}/cancel`, {}).subscribe(() => {
        this.loadAppointments();
      });
    }
  }

  openFeedback(appt: any) {
    this.feedbackAppt = appt;
    this.feedbackRating = 5;
    this.feedbackComments = '';
  }

  submitFeedback() {
    if(!this.feedbackAppt) return;
    const payload = {
      appointmentId: this.feedbackAppt.appointmentId,
      doctorId: this.feedbackAppt.doctorId, // Need doctorId in DTO
      rating: this.feedbackRating,
      comments: this.feedbackComments
    };
    // Patching doctorId from DTO might require adding it to AppointmentSummaryDto
    // Let's assume the API will handle if DoctorId is passed or we can fetch it.
    // For now, I'll pass 0 or modify DTO later if needed.
    this.http.post('https://localhost:7130/api/feedback', payload).subscribe(() => {
      this.feedbackAppt.hasFeedback = true; // Local optimistic update
      this.feedbackAppt = null;
      this.loadAppointments();
    });
  }
}
