import { Injectable, inject, signal } from '@angular/core';
import { HttpClientService } from './http-client.service';
import { HEALTH_API } from '../constants/api-endpoints.constants';

export type ServerStatus = 'connecting' | 'ready' | 'error';

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClientService);

  readonly status = signal<ServerStatus>('connecting');

  /** Fire-and-forget wake-up ping. Retries up to maxRetries times with delay. */
  ping(maxRetries = 5, delayMs = 4000): void {
    this.status.set('connecting');
    this.attempt(0, maxRetries, delayMs);
  }

  private attempt(attempt: number, maxRetries: number, delayMs: number): void {
    // Strip leading slash — HttpClientService.buildUrl prepends apiBaseUrl with its own separator
    const endpoint = HEALTH_API.PING.replace(/^\//, '');
    this.http.get<unknown>(endpoint).subscribe({
      next: () => this.status.set('ready'),
      error: () => {
        if (attempt < maxRetries - 1) {
          setTimeout(() => this.attempt(attempt + 1, maxRetries, delayMs), delayMs);
        } else {
          this.status.set('error');
        }
      },
    });
  }
}
