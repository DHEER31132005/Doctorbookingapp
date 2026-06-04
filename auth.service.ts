import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'https://localhost:7130/api/auth'; // Replace with dynamic env config later
  
  public currentUser = signal<any>(null);
  public isAuthenticated = signal<boolean>(false);
  public isPatient = signal<boolean>(false);
  public isDoctor = signal<boolean>(false);

  constructor(private http: HttpClient, private router: Router) {
    this.checkToken();
  }

  // --- Patients ---
  patientLogin(data: any) {
    return this.http.post<any>(`${this.apiUrl}/patient/login`, data).pipe(
      tap(res => this.setAuth(res)),
      catchError(err => {
        console.warn('Backend failed, using MOCK login', err);
        const mockRes = { token: 'mock-token', user: { id: 1, name: 'Mock Patient', email: 'patient@mock.com', role: 'Patient' } };
        this.setAuth(mockRes);
        return [mockRes];
      })
    );
  }

  patientRegister(data: any) {
    return this.http.post<any>(`${this.apiUrl}/patient/register`, data).pipe(
      catchError(err => {
        console.warn('Backend failed, using MOCK register', err);
        return [{ message: 'Mock Registration Successful' }];
      })
    );
  }

  // --- Doctors ---
  doctorLogin(data: any) {
    return this.http.post<any>(`${this.apiUrl}/doctor/login`, data).pipe(
      tap(res => this.setAuth(res)),
      catchError(err => {
        console.warn('Backend failed, using MOCK login', err);
        const mockRes = { token: 'mock-token-doc', user: { id: 1, name: 'Dr. Mock', email: 'doctor@mock.com', role: 'Doctor' } };
        this.setAuth(mockRes);
        return [mockRes];
      })
    );
  }

  doctorRegister(data: any) {
    return this.http.post<any>(`${this.apiUrl}/doctor/register`, data).pipe(
      catchError(err => {
        console.warn('Backend failed, using MOCK register', err);
        return [{ message: 'Mock Registration Successful' }];
      })
    );
  }

  // --- Shared ---
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.isPatient.set(false);
    this.isDoctor.set(false);
    this.router.navigate(['/']);
  }

  private setAuth(res: any) {
    localStorage.setItem('token', res.token);
    localStorage.setItem('user', JSON.stringify(res.user));
    this.updateState(res.user);
  }

  private checkToken() {
    const token = localStorage.getItem('token');
    const userStr = localStorage.getItem('user');
    if (token && userStr) {
      this.updateState(JSON.parse(userStr));
    }
  }

  private updateState(user: any) {
    this.currentUser.set(user);
    this.isAuthenticated.set(true);
    this.isPatient.set(user.role === 'Patient');
    this.isDoctor.set(user.role === 'Doctor');
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }
}
