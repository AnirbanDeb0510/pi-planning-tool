import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpClientService, ApiError } from './http-client.service';
import { SignalrService } from '../../features/board/services/signalr.service';
import { environment } from '../../../environments/environment';

describe('HttpClientService', () => {
  let service: HttpClientService;
  let httpMock: HttpTestingController;
  let signalrMock: { getConnectionId: jasmine.Spy };

  beforeEach(() => {
    signalrMock = {
      getConnectionId: jasmine.createSpy().and.returnValue(null),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: SignalrService, useValue: signalrMock },
      ],
    });

    service = TestBed.inject(HttpClientService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('get builds expected URL', () => {
    service.get<{ ok: boolean }>('api/test').subscribe((res) => {
      expect(res.ok).toBeTrue();
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/test`);
    expect(req.request.method).toBe('GET');
    req.flush({ ok: true });
  });

  it('post adds X-SignalR-ConnectionId header when connection id exists', () => {
    signalrMock.getConnectionId.and.returnValue('conn-123');

    service.post('api/test', { a: 1 }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/test`);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('X-SignalR-ConnectionId')).toBe('conn-123');
    req.flush({});
  });

  it('patch preserves existing headers and adds connection id header', () => {
    signalrMock.getConnectionId.and.returnValue('conn-xyz');

    service.patch('api/test', {}, { headers: { 'X-Custom': 'yes' } }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/test`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.headers.get('X-Custom')).toBe('yes');
    expect(req.request.headers.get('X-SignalR-ConnectionId')).toBe('conn-xyz');
    req.flush({});
  });

  it('put works with HttpHeaders options', () => {
    signalrMock.getConnectionId.and.returnValue('conn-abc');

    service.put('api/test', {}, { headers: new HttpHeaders({ 'X-Test': '1' }) }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/test`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.headers.get('X-Test')).toBe('1');
    expect(req.request.headers.get('X-SignalR-ConnectionId')).toBe('conn-abc');
    req.flush({});
  });

  it('delete does not add connection header when connection id is absent', () => {
    signalrMock.getConnectionId.and.returnValue(null);

    service.delete('api/test').subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/test`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.headers.has('X-SignalR-ConnectionId')).toBeFalse();
    req.flush({});
  });

  it('maps backend error details into ApiError', () => {
    let capturedError: ApiError | undefined;
    const errorBody = { error: { details: 'Validation failed' }, errors: { name: ['Required'] } };
    const errorOpts = { status: 400, statusText: 'Bad Request' };

    service.get('api/test').subscribe({
      next: () => fail('Expected error'),
      error: (err: ApiError) => {
        capturedError = err;
      },
    });

    // GET has retry(1): flush both the first attempt and the retry with the same error
    httpMock.expectOne(`${environment.apiBaseUrl}/api/test`).flush(errorBody, errorOpts);
    httpMock.expectOne(`${environment.apiBaseUrl}/api/test`).flush(errorBody, errorOpts);

    expect(capturedError).toEqual(
      jasmine.objectContaining({
        message: 'Validation failed',
        statusCode: 400,
        errors: { name: ['Required'] },
      }),
    );
  });

  it('maps network errors into statusCode 0 ApiError', () => {
    let capturedError: ApiError | undefined;
    const networkEvent = new ErrorEvent('error', { message: 'connection refused' });

    service.get('api/test').subscribe({
      next: () => fail('Expected network error'),
      error: (err: ApiError) => {
        capturedError = err;
      },
    });

    // GET has retry(1): trigger network error on both the first attempt and the retry
    httpMock.expectOne(`${environment.apiBaseUrl}/api/test`).error(networkEvent);
    httpMock.expectOne(`${environment.apiBaseUrl}/api/test`).error(networkEvent);

    expect(capturedError).toEqual(
      jasmine.objectContaining({
        statusCode: 0,
      }),
    );
    expect(capturedError!.message).toContain('Network error');
  });

  it('retries get once and succeeds on second attempt', () => {
    let response: { ok: boolean } | undefined;

    service.get<{ ok: boolean }>('api/retry').subscribe((res) => {
      response = res;
    });

    const first = httpMock.expectOne(`${environment.apiBaseUrl}/api/retry`);
    first.flush({ message: 'temporary failure' }, { status: 500, statusText: 'Server Error' });

    const second = httpMock.expectOne(`${environment.apiBaseUrl}/api/retry`);
    second.flush({ ok: true });

    expect(response).toEqual({ ok: true });
  });
});
