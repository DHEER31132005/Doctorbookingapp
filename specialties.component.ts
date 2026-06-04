import { Component, inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'app-specialties',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <div class="text-center mb-8 animate-fade-in">
        <h2 class="gradient-text">Find a Doctor</h2>
        <p class="text-muted">Select a specialty and consultation mode</p>
      </div>

      <!-- Filter Section -->
      <div class="glass-panel animate-fade-in flex gap-4 items-center justify-center" style="padding: 1.5rem; margin-bottom: 2rem; flex-wrap: wrap;">
        <div class="form-group" style="margin: 0; min-width: 200px;">
          <select class="form-control" [(ngModel)]="selectedSpecialty" (change)="fetchDoctors()">
            <option value="">All Specialties</option>
            <option *ngFor="let s of specialties" [value]="s.specialtyId">{{ s.specialtyName }}</option>
          </select>
        </div>

        <div class="form-group" style="margin: 0; min-width: 200px;">
          <select class="form-control" [(ngModel)]="selectedMode" (change)="fetchDoctors()">
            <option value="">Any Mode</option>
            <option value="Online">Online</option>
            <option value="Offline">Offline</option>
          </select>
        </div>
      </div>

      <!-- Doctors List -->
      <div class="flex flex-col gap-6">
        <div *ngIf="doctors.length === 0" class="text-center text-muted py-8">
          No doctors found for the selected filters.
        </div>

        <div *ngFor="let doc of doctors; let i = index" class="glass-panel animate-fade-in flex gap-6" [style.animation-delay]="(i * 0.1) + 's'" style="padding: 1.5rem;">
          
          <div class="doc-photo" style="width: 100px; height: 100px; border-radius: 50%; background: var(--bg-surface); display: flex; align-items: center; justify-content: center; font-size: 3rem; color: var(--accent-primary);">
            <span class="material-icons">person</span>
          </div>

          <div class="doc-info flex-1">
            <h3 style="margin-bottom: 0.5rem; font-size: 1.25rem;">{{ doc.doctorName }}</h3>
            <p class="text-muted" style="font-size: 0.875rem; margin-bottom: 1rem;">
              {{ doc.specialtyName }} • {{ doc.experienceYears }} Yrs Exp • 
              <span [class.text-success]="doc.consultationMode === 'Online'" [class.text-warning]="doc.consultationMode === 'Offline'">
                {{ doc.consultationMode }}
              </span>
            </p>
            <div class="flex gap-4">
              <span class="flex items-center gap-2 text-muted" style="font-size: 0.875rem;">
                <span class="material-icons" style="font-size: 1rem;">payments</span> ₹{{ doc.consultationFee }}
              </span>
              <span *ngIf="doc.clinicAddress" class="flex items-center gap-2 text-muted" style="font-size: 0.875rem;">
                <span class="material-icons" style="font-size: 1rem;">location_on</span> {{ doc.clinicAddress }}
              </span>
            </div>
          </div>

          <div class="flex items-center justify-center">
            <button class="btn-primary" (click)="viewSlots(doc)">Book Appointment</button>
          </div>
        </div>
      </div>

      <!-- Booking Modal -->
      <div *ngIf="selectedDoctor" class="modal-backdrop" style="position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(4px); z-index: 100; display: flex; align-items: center; justify-content: center;">
        <div class="glass-panel animate-fade-in" style="width: 100%; max-width: 500px; padding: 2rem;">
          <div class="flex justify-between items-center mb-6">
            <h3>Book with {{ selectedDoctor.doctorName }}</h3>
            <button class="btn-outline btn-sm" (click)="selectedDoctor = null; slots = []">Close</button>
          </div>

          <div class="form-group">
            <label class="form-label">Select Date</label>
            <input type="date" class="form-control" [(ngModel)]="bookingDate" (change)="fetchSlots()">
          </div>

          <div class="slots-grid grid gap-4 mt-4" style="display: grid; grid-template-columns: repeat(3, 1fr);">
            <div *ngFor="let slot of slots" 
                 class="slot-btn text-center" 
                 [class.selected]="selectedSlot === slot"
                 (click)="selectedSlot = slot"
                 style="padding: 0.5rem; border: 1px solid var(--border-glass); border-radius: 8px; cursor: pointer;">
              {{ slot.startTime }}
            </div>
          </div>
          
          <div *ngIf="slots.length === 0 && bookingDate" class="text-center text-muted mt-4">
            No slots available on this date.
          </div>

          <button *ngIf="selectedSlot" class="btn-primary w-full mt-6" (click)="confirmBooking()" [disabled]="bookingLoading">
            {{ bookingLoading ? 'Booking...' : 'Confirm Booking' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .slot-btn:hover { background: rgba(255,255,255,0.05); }
    .slot-btn.selected { background: var(--accent-primary); color: white; border-color: var(--accent-primary); }
    .text-success { color: var(--success); }
    .text-warning { color: var(--warning); }
  `]
})
export class SpecialtiesComponent implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);

  specialties: any[] = [];
  doctors: any[] = [];
  
  selectedSpecialty = '';
  selectedMode = '';

  selectedDoctor: any = null;
  bookingDate: string = new Date(new Date().getTime() + 86400000).toISOString().split('T')[0]; // tomorrow
  slots: any[] = [];
  selectedSlot: any = null;
  bookingLoading = false;

  ngOnInit() {
        this.http.get<any[]>('http://localhost:5255/api/specialties').pipe(
          catchError(() => {
            console.warn('Using mock specialties');
            const specialties = Array.from({ length: 50 }, (_, i) => ({
              specialtyId: i + 1,
              specialtyName: `Specialty ${i + 1}`
            }));
            return of(specialties);
          })
        ).subscribe(res => this.specialties = res);
        this.fetchDoctors();
  }

  fetchDoctors() {
    let url = 'http://localhost:5255/api/doctors?';
    if (this.selectedSpecialty) url += `specialtyId=${this.selectedSpecialty}&`;
    // Generate mock doctors for each specialty (10 each, mixed online/offline)
    this.doctors = [];
    this.specialties.forEach(s => {
        for (let d = 1; d <= 10; d++) {
            const mode = d % 2 === 0 ? 'Offline' : 'Online';
            this.doctors.push({
                doctorId: s.specialtyId * 100 + d,
                doctorName: `Dr. ${s.specialtyName} ${d}`,
                specialtyId: s.specialtyId,
                specialtyName: s.specialtyName,
                consultationMode: mode,
                consultationFee: 500 + (d * 20),
                experienceYears: 5 + d,
                clinicAddress: mode === 'Offline' ? `${s.specialtyName} Clinic ${d}` : undefined
            });
        }
    });
    // Apply current filters
    this.applyFilters();
  }

  viewSlots(doc: any) {
    this.selectedDoctor = doc;
    this.selectedSlot = null;
    this.fetchSlots();
  }

  fetchSlots() {
    if (!this.bookingDate || !this.selectedDoctor) return;
    this.http.get<any[]>(`http://localhost:5255/api/doctors/${this.selectedDoctor.doctorId}/slots?date=${this.bookingDate}`).pipe(
      catchError(() => {
        return of([
          { slotId: 1, startTime: '09:00', endTime: '09:30', isBooked: false },
          { slotId: 2, startTime: '10:00', endTime: '10:30', isBooked: false }
        ]);
      })
    ).subscribe(res => this.slots = res);
  }

  confirmBooking() {
    if (!this.selectedSlot || !this.selectedDoctor) return;
    this.bookingLoading = true;

    const payload = {
      doctorId: this.selectedDoctor.doctorId,
      slotId: this.selectedSlot.slotId,
      notes: ''
    };

    this.http.post('http://localhost:5255/api/appointments', payload).pipe(
      catchError(() => {
        console.warn('Mock booking success');
        return of({ message: 'Success' });
      })
    ).subscribe({
      next: () => {
        this.bookingLoading = false;
        this.selectedDoctor = null;
        this.router.navigate(['/patient/dashboard/appointments']);
      },
      error: () => {
        alert('Failed to book appointment');
        this.bookingLoading = false;
      }
    });
  }
}
