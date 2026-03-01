import { Injectable, signal, ElementRef } from '@angular/core';
import { CursorPresenceEvent } from './signalr.service';

export interface RemoteCursorViewModel {
  userId: string;
  displayName: string;
  color: string;
  x: number;
  y: number;
  lastSeenAt: number;
  sequence: number;
}

/**
 * Service for tracking user cursors (local and remote) on the board
 */
@Injectable({
  providedIn: 'root',
})
export class CursorTrackingService {
  private remoteCursorMap = new Map<string, RemoteCursorViewModel>();
  private remoteCursorCleanupIntervalId: number | null = null;
  private readonly cursorLabelOffset = 20;
  private readonly remoteCursorIdleMs = 3000;

  // Signals for cursor state
  public cursorX = signal(0);
  public cursorY = signal(0);
  public remoteCursors = signal<RemoteCursorViewModel[]>([]);

  /**
   * Update local cursor position based on mouse move event
   * Returns normalized position for SignalR broadcast
   */
  updateLocalCursor(
    event: MouseEvent,
    boardContainerRef?: ElementRef<HTMLDivElement>,
  ): { x: number; y: number } {
    const position = this.getBoardRelativePosition(event, boardContainerRef);

    this.cursorX.set(position.x + this.cursorLabelOffset);
    this.cursorY.set(position.y + this.cursorLabelOffset);

    return position;
  }

  /**
   * Apply remote cursor event from SignalR
   */
  applyRemoteCursorEvent(event: CursorPresenceEvent): void {
    const existing = this.remoteCursorMap.get(event.userId);
    if (existing && event.sequence <= existing.sequence) {
      return; // Ignore out-of-order events
    }

    this.remoteCursorMap.set(event.userId, {
      userId: event.userId,
      displayName: event.displayName,
      color: event.color,
      x: event.cursor.x + this.cursorLabelOffset,
      y: event.cursor.y + this.cursorLabelOffset,
      lastSeenAt: Date.now(),
      sequence: event.sequence,
    });

    this.remoteCursors.set([...this.remoteCursorMap.values()]);
  }

  /**
   * Start periodic cleanup of idle remote cursors
   */
  startRemoteCursorCleanup(): void {
    if (this.remoteCursorCleanupIntervalId !== null) {
      window.clearInterval(this.remoteCursorCleanupIntervalId);
    }

    this.remoteCursorCleanupIntervalId = window.setInterval(() => {
      const now = Date.now();
      let hasChanges = false;

      for (const [userId, cursor] of this.remoteCursorMap.entries()) {
        if (now - cursor.lastSeenAt > this.remoteCursorIdleMs) {
          this.remoteCursorMap.delete(userId);
          hasChanges = true;
        }
      }

      if (hasChanges) {
        this.remoteCursors.set([...this.remoteCursorMap.values()]);
      }
    }, 500);
  }

  /**
   * Stop cursor cleanup and clear all remote cursors
   */
  stopRemoteCursorCleanup(): void {
    if (this.remoteCursorCleanupIntervalId !== null) {
      window.clearInterval(this.remoteCursorCleanupIntervalId);
      this.remoteCursorCleanupIntervalId = null;
    }

    this.remoteCursorMap.clear();
    this.remoteCursors.set([]);
  }

  /**
   * Reset local cursor position
   */
  resetLocalCursor(): void {
    this.cursorX.set(0);
    this.cursorY.set(0);
  }

  /**
   * Get board-relative cursor position accounting for scroll
   */
  private getBoardRelativePosition(
    event: MouseEvent,
    boardContainerRef?: ElementRef<HTMLDivElement>,
  ): { x: number; y: number } {
    if (!boardContainerRef?.nativeElement) {
      return {
        x: event.clientX,
        y: event.clientY,
      };
    }

    const boardContainer = boardContainerRef.nativeElement;
    const rect = boardContainer.getBoundingClientRect();

    return {
      x: Math.round(event.clientX - rect.left + boardContainer.scrollLeft),
      y: Math.round(event.clientY - rect.top + boardContainer.scrollTop),
    };
  }
}
