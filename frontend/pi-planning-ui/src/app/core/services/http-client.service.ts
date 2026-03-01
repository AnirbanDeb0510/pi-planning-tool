import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { SignalrService } from '../../features/board/services/signalr.service';

/**
 * HTTP request options
 */
export interface HttpOptions {
  headers?: HttpHeaders | { [header: string]: string | string[] };
  params?: HttpParams | { [param: string]: string | string[] };
  observe?: 'body';
  responseType?: 'json';
  withCredentials?: boolean;
}

/**
 * API Error Response
 */
export interface ApiError {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
}

/**
 * Base HTTP Client Service
 * Provides centralized HTTP communication with error handling, retry logic, and interceptors
 */
@Injectable({ providedIn: 'root' })
export class HttpClientService {
  private http = inject(HttpClient);
  private signalrService = inject(SignalrService);
  private baseUrl = environment.apiBaseUrl;

  /**
   * Perform GET request
   */
  get<T>(endpoint: string, options?: HttpOptions): Observable<T> {
    const url = this.buildUrl(endpoint);
    return this.http.get<T>(url, options).pipe(
      retry(1), // Retry once on failure
      catchError(this.handleError),
    );
  }

  /**
   * Perform POST request
   */
  post<T>(endpoint: string, body: unknown, options?: HttpOptions): Observable<T> {
    const url = this.buildUrl(endpoint);
    const mergedOptions = this.addConnectionIdHeader(options);
    return this.http.post<T>(url, body, mergedOptions).pipe(catchError(this.handleError));
  }

  /**
   * Perform PUT request
   */
  put<T>(endpoint: string, body: unknown, options?: HttpOptions): Observable<T> {
    const url = this.buildUrl(endpoint);
    const mergedOptions = this.addConnectionIdHeader(options);
    return this.http.put<T>(url, body, mergedOptions).pipe(catchError(this.handleError));
  }

  /**
   * Perform PATCH request
   */
  patch<T>(endpoint: string, body: unknown, options?: HttpOptions): Observable<T> {
    const url = this.buildUrl(endpoint);
    const mergedOptions = this.addConnectionIdHeader(options);
    return this.http.patch<T>(url, body, mergedOptions).pipe(catchError(this.handleError));
  }

  /**
   * Perform DELETE request
   */
  delete<T>(endpoint: string, options?: HttpOptions): Observable<T> {
    const url = this.buildUrl(endpoint);
    const mergedOptions = this.addConnectionIdHeader(options);
    return this.http.delete<T>(url, mergedOptions).pipe(catchError(this.handleError));
  }

  /**
   * Build full URL from endpoint
   * Note: endpoint should include /api/ prefix (use constants from api-endpoints.constants.ts)
   */
  private buildUrl(endpoint: string): string {
    // Remove leading slash if present to avoid double slashes
    const cleanEndpoint = endpoint.startsWith('/') ? endpoint.slice(1) : endpoint;
    return `${this.baseUrl}/${cleanEndpoint}`;
  }

  /**
   * Add SignalR connectionId header to request options
   * This header is used by the backend to exclude the initiator from broadcast notifications
   */
  private addConnectionIdHeader(options?: HttpOptions): HttpOptions {
    const connectionId = this.signalrService.getConnectionId();
    if (!connectionId) {
      return options || {};
    }

    // Get existing headers or create new ones
    let headers: HttpHeaders;

    if (options?.headers instanceof HttpHeaders) {
      // Already an HttpHeaders object
      headers = options.headers.set('X-SignalR-ConnectionId', connectionId);
    } else if (options?.headers) {
      // It's a plain object with header values
      headers = new HttpHeaders(options.headers).set('X-SignalR-ConnectionId', connectionId);
    } else {
      // No headers provided
      headers = new HttpHeaders().set('X-SignalR-ConnectionId', connectionId);
    }

    return {
      ...options,
      headers,
    };
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage: ApiError;

    if (error.error instanceof ErrorEvent) {
      // Client-side or network error
      errorMessage = {
        message: `Network error: ${error.error.message}`,
        statusCode: 0,
      };
    } else {
      // Backend error
      // Prioritize error.details over error.message for more specific error information
      const apiError = error.error?.error; // Handle nested error object from middleware
      const detailedMessage =
        apiError?.details || apiError?.message || error.error?.message || error.message;

      errorMessage = {
        message: detailedMessage || 'An unexpected error occurred',
        statusCode: error.status,
        errors: error.error?.errors,
      };
    }

    // Log error in development
    if (!environment.production) {
      console.error('HTTP Error:', errorMessage);
    }

    return throwError(() => errorMessage);
  }
}
