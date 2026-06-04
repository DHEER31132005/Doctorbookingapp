import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="container mt-4">
      <div class="text-center mb-8 animate-fade-in">
        <h1 class="gradient-text" style="font-size: 3rem; margin-bottom: 1rem;">Welcome to CareConnect</h1>
        <p class="text-muted" style="font-size: 1.25rem;">Seamless doctor appointments, built for modern healthcare.</p>
      </div>

      <div class="flex gap-6 justify-center animate-fade-in" style="animation-delay: 0.2s; flex-wrap: wrap;">
        <!-- Patient Card -->
        <div class="glass-panel text-center" style="padding: 3rem 2rem; flex: 1; min-width: 300px; max-width: 400px;">
          <span class="material-icons gradient-text" style="font-size: 4rem; margin-bottom: 1rem;">personal_injury</span>
          <h2>I am a Patient</h2>
          <p class="text-muted mt-4 mb-8">Book appointments with top specialists, track your health history, and consult online or offline.</p>
          <a routerLink="/patient/login" class="btn-primary w-full" style="display: block;">Patient Portal</a>
        </div>

        <!-- Doctor Card -->
        <div class="glass-panel text-center" style="padding: 3rem 2rem; flex: 1; min-width: 300px; max-width: 400px;">
          <span class="material-icons gradient-text" style="font-size: 4rem; margin-bottom: 1rem;">medication</span>
          <h2>I am a Doctor</h2>
          <p class="text-muted mt-4 mb-8">Manage your schedule, conduct online consultations, and grow your practice seamlessly.</p>
          <a routerLink="/doctor/login" class="btn-outline w-full" style="display: block;">Doctor Portal</a>
        </div>
      </div>
    </div>
  `
})
export class HomeComponent {}
