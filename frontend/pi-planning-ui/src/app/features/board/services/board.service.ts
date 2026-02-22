import { Injectable, signal, inject } from '@angular/core';
import {
  BoardResponseDto,
} from '../../../shared/models/board.dto';
import { BoardSummaryDto } from '../../../shared/models/board-api.dto';
import { BoardApiService, AzureApiService } from './board-api.service';
import { firstValueFrom } from 'rxjs';
import { RuntimeConfig } from '../../../core/config/runtime-config';

/**
 * Board Service - State Management Layer
 * Manages board state using signals
 * Delegates domain operations to specialized services (FeatureService, TeamService, etc.)
 */
@Injectable({ providedIn: 'root' })
export class BoardService {
  private boardApi = inject(BoardApiService);
  private azureApi = inject(AzureApiService);

  // State signals
  private boardSignal = signal<BoardResponseDto | null>(null);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  // PAT storage with timestamp for expiry
  private patStorage = signal<{ pat: string; timestamp: number } | null>(null);

  // Public read-only signals
  public board = this.boardSignal.asReadonly();
  public loading = this.loadingSignal.asReadonly();
  public error = this.errorSignal.asReadonly();

  /**
   * Load board by ID from API
   */
  public loadBoard(id: number): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    this.boardApi.getBoard(id).subscribe({
      next: (board: BoardResponseDto) => {
        this.boardSignal.set(board);
        this.loadingSignal.set(false);
        console.log('Board loaded:', board);
      },
      error: (error) => {
        this.errorSignal.set(error.message || 'Failed to load board');
        this.loadingSignal.set(false);
        console.error('Error loading board:', error);
      },
    });
  }

  /**
   * Get the current board state (or null if not loaded)
   */
  public getBoard(): BoardResponseDto | null {
    return this.board();
  }

  /**
   * Update board state (called by delegated services)
   */
  public updateBoardState(board: BoardResponseDto): void {
    this.boardSignal.set(board);
  }

  /**
   * Set error message (called by delegated services)
   */
  public setError(message: string): void {
    this.errorSignal.set(message);
  }

  /**
   * Toggle dev/test display mode
   */
  public toggleDevTestToggle(): void {
    const currentBoard = this.boardSignal();
    if (!currentBoard) return;

    const updatedBoard = {
      ...currentBoard,
      devTestToggle: !currentBoard.devTestToggle,
    };
    this.boardSignal.set(updatedBoard);
  }

  /**
   * Store PAT with timestamp for 10-minute expiry
   */
  public storePat(pat: string): void {
    this.patStorage.set({ pat, timestamp: Date.now() });
  }

  /**
   * Get stored PAT if not expired (10 minutes)
   */
  public getStoredPat(): string | null {
    const stored = this.patStorage();
    if (!stored) return null;

    const ttlMinutes = RuntimeConfig.patTtlMinutes;
    const ttlMs = ttlMinutes * 60 * 1000;
    if (Date.now() - stored.timestamp > ttlMs) {
      this.patStorage.set(null); // Expired
      return null;
    }

    return stored.pat;
  }

  /**
   * Validate PAT by attempting to access a feature from Azure DevOps
   * Returns true if PAT is valid, false otherwise
   */
  public async validatePatForBoard(
    organization: string,
    project: string,
    featureAzureId: string,
    pat: string
  ): Promise<boolean> {
    try {
      // Make a test call to verify PAT access
      // This is a read-only operation (fetch feature details)
      const result = await firstValueFrom(
        this.azureApi.getFeatureWithChildren(
          organization,
          project,
          featureAzureId,
          pat
        )
      );

      if (result) {
        // Valid PAT - store it temporarily  
        this.patStorage.set({ pat, timestamp: Date.now() });
        return true;
      }

      return false;
    } catch (error) {
      console.error('PAT validation failed:', error);
      return false;
    }
  }

  /**
   * Check if board requires PAT validation (has features)
   */
  public boardRequiresPatValidation(): boolean {
    const board = this.boardSignal();
    return board != null && board.features.length > 0;
  }

  /**
   * Get board preview without loading full board data
   * Returns BoardSummaryDto with organization, project, and sample feature ID for PAT validation
   */
  public async getBoardPreview(boardId: number): Promise<BoardSummaryDto | null> {
    try {
      const preview = await firstValueFrom(
        this.boardApi.getBoardPreview(boardId)
      );
      return preview;
    } catch (error) {
      console.error('Error fetching board preview:', error);
      this.errorSignal.set('Failed to load board information');
      return null;
    }
  }

  /**
   * Clear stored PAT
   */
  public clearPat(): void {
    this.patStorage.set(null);
  }

  /**
   * Clear error message
   */
  public clearError(): void {
    this.errorSignal.set(null);
  }

  /**
   * Get finalization warnings before submitting
   */
  public async getFinalizationWarnings(boardId: number): Promise<string[]> {
    try {
      const warnings = await firstValueFrom(this.boardApi.validateBoardForFinalization(boardId));
      console.log('Finalization warnings:', warnings);
      return warnings;
    } catch (error: any) {
      console.error('Error fetching finalization warnings:', error);
      return [];
    }
  }

  /**
   * Submit/finalize board
   */
  public async finalizeBoard(boardId: number): Promise<BoardResponseDto | null> {
    try {
      this.loadingSignal.set(true);
      this.errorSignal.set(null);

      // Call finalize endpoint
      await firstValueFrom(this.boardApi.finalizeBoard(boardId));

      // Reload board to get fresh state with finalized flag and originalSprintId updated
      const updatedBoard = await firstValueFrom(this.boardApi.getBoard(boardId));
      this.boardSignal.set(updatedBoard);
      this.loadingSignal.set(false);

      console.log('Board finalized successfully');
      return updatedBoard;
    } catch (error: any) {
      const errorMsg = error.message || 'Failed to finalize board';
      this.errorSignal.set(errorMsg);
      this.loadingSignal.set(false);
      console.error('Error finalizing board:', error);
      throw new Error(errorMsg);
    }
  }

  /**
   * Restore board (allow further editing)
   */
  public async restoreBoard(boardId: number): Promise<BoardResponseDto | null> {
    try {
      this.loadingSignal.set(true);
      this.errorSignal.set(null);

      await firstValueFrom(this.boardApi.restoreBoard(boardId));

      // Reload board to get updated state
      const updatedBoard = await firstValueFrom(this.boardApi.getBoard(boardId));
      this.boardSignal.set(updatedBoard);
      this.loadingSignal.set(false);

      console.log('Board restored successfully');
      return updatedBoard;
    } catch (error: any) {
      const errorMsg = error.message || 'Failed to restore board';
      this.errorSignal.set(errorMsg);
      this.loadingSignal.set(false);
      console.error('Error restoring board:', error);
      throw new Error(errorMsg);
    }
  }
}
