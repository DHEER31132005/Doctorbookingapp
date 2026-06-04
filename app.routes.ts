import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home.component';
import { PatientLoginComponent } from './pages/patient/login.component';
import { PatientRegisterComponent } from './pages/patient/register.component';
import { DoctorLoginComponent } from './pages/doctor/login.component';
import { DoctorRegisterComponent } from './pages/doctor/register.component';

import { SpecialtiesComponent } from './pages/patient/specialties.component';
import { PatientAppointmentsComponent } from './pages/patient/appointments.component';
import { DoctorAppointmentsComponent } from './pages/doctor/appointments.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'patient/login', component: PatientLoginComponent },
  { path: 'patient/register', component: PatientRegisterComponent },
  { path: 'doctor/login', component: DoctorLoginComponent },
  { path: 'doctor/register', component: DoctorRegisterComponent },
  
  { path: 'patient/dashboard/specialties', component: SpecialtiesComponent },
  { path: 'patient/dashboard/appointments', component: PatientAppointmentsComponent },
  { path: 'doctor/dashboard/appointments', component: DoctorAppointmentsComponent },
  
  { path: '**', redirectTo: '' }
];
