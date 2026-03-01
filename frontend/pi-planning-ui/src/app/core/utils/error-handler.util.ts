import { ApiError } from '../services/http-client.service';
import { MESSAGES } from '../../shared/constants';

/**
 * Type guard to check if error is an ApiError
 */
export function isApiError(error: unknown): error is ApiError {
  return (
    typeof error === 'object' &&
    error !== null &&
    'message' in error &&
    'statusCode' in error &&
    typeof (error as ApiError).message === 'string' &&
    typeof (error as ApiError).statusCode === 'number'
  );
}

/**
 * Extract error message from various error types
 * Returns a user-friendly error message with fallback
 */
export function getErrorMessage(error: unknown, fallbackMessage: string): string {
  if (isApiError(error)) {
    // Check for specific status codes
    if (error.statusCode === 403) {
      return error.message || MESSAGES.BOARD.OPERATION_BLOCKED_LOCKED;
    }
    return error.message || fallbackMessage;
  }

  if (error instanceof Error) {
    return error.message || fallbackMessage;
  }

  if (typeof error === 'string') {
    return error || fallbackMessage;
  }

  return fallbackMessage;
}

/**
 * Check if error is a 403 Forbidden (locked board) error
 */
export function isLockedBoardError(error: unknown): boolean {
  return isApiError(error) && error.statusCode === 403;
}
