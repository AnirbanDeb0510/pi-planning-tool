import { Injectable, signal, inject } from '@angular/core';
import {
  BoardResponseDto,
  FeatureResponseDto,
  UserStoryDto,
  TeamMemberResponseDto,
} from '../../../shared/models/board.dto';
import { BoardSummaryDto } from '../../../shared/models/board-api.dto';
import { BoardApiService, StoryApiService, TeamApiService, FeatureApiService, AzureApiService } from './board-api.service';
import { firstValueFrom } from 'rxjs';

/**
 * Board Service - State Management Layer
 * Manages board state using signals and coordinates with API services
 * No longer contains mock data - uses injected API services instead
 */
@Injectable({ providedIn: 'root' })
export class BoardService {
  private boardApi = inject(BoardApiService);
  private featureApi = inject(FeatureApiService);
  private storyApi = inject(StoryApiService);
  private teamApi = inject(TeamApiService);
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
   * Import feature from Azure DevOps
   * First fetches the feature from Azure, then imports it to the board
   */
  public async importFeature(
    boardId: number,
    organization: string,
    project: string,
    featureId: string,
    pat: string
  ): Promise<void> {
    try {
      // Step 1: Fetch feature from Azure DevOps
      console.log('Fetching feature from Azure:', { organization, project, featureId });
      const featureDto = await firstValueFrom(
        this.azureApi.getFeatureWithChildren(organization, project, featureId, pat)
      );

      // Step 2: Import the feature to the board
      console.log('Importing feature to board:', featureDto);
      const importedFeature = await firstValueFrom(
        this.featureApi.importFeature(boardId, featureDto)
      );

      // Step 3: Reload the board to ensure UI matches backend state
      console.log('Feature imported successfully, reloading board...');
      this.loadBoard(boardId);
    } catch (error: any) {
      console.error('Error importing feature:', error);
      throw new Error(error.message || 'Failed to import feature');
    }
  }

  /**
   * Move a story from one sprint to another
   * Updates local state optimistically, then syncs with backend
   */
  public moveStory(storyId: number, fromSprintId: number, toSprintId: number): void {
    const currentBoard = this.boardSignal();
    if (!currentBoard) return;

    // Optimistic update
    let found = false;
    const updatedFeatures = currentBoard.features.map((feature: FeatureResponseDto) => {
      const updatedStories = feature.userStories.map((s: UserStoryDto) => {
        if (s.id === storyId) {
          found = true;
          return {
            ...s,
            sprintId: toSprintId,
            isMoved: s.originalSprintId !== toSprintId,
          };
        }
        return s;
      });
      return {
        ...feature,
        userStories: updatedStories,
      };
    });

    if (found) {
      // Update local state immediately
      const updatedBoard = {
        ...currentBoard,
        features: updatedFeatures,
      };
      this.boardSignal.set(updatedBoard);

      // Sync with backend (if using real API)
      this.storyApi.moveStory(currentBoard.id, storyId, toSprintId).subscribe({
        next: () => {
          console.log(`Story ${storyId} moved from Sprint ${fromSprintId} to Sprint ${toSprintId}`);
        },
        error: (error) => {
          console.error('Error moving story:', error);
          // Rollback on error
          this.boardSignal.set(currentBoard);
          this.errorSignal.set('Failed to move story. Please try again.');
        },
      });
    }
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
   * Add a new team member with default capacities per sprint
   */
  public addTeamMember(name: string, role: 'dev' | 'test', devTestEnabled: boolean): void {
    const currentBoard = this.boardSignal();
    if (!currentBoard) return;

    const isDev = devTestEnabled ? role === 'dev' : true;
    const isTest = devTestEnabled ? role === 'test' : false;

    // Calculate working days for default capacity
    const getWorkingDays = (startDate: Date | string, endDate: Date | string): number => {
      const start = startDate instanceof Date ? startDate : new Date(startDate);
      const end = endDate instanceof Date ? endDate : new Date(endDate);
      const msPerDay = 24 * 60 * 60 * 1000;
      const totalDays = Math.round((end.getTime() - start.getTime()) / msPerDay) + 1;
      return Math.floor((totalDays / 7) * 5);
    };

    // Create temporary member for optimistic update
    const nextId = Math.max(0, ...currentBoard.teamMembers.map((m) => m.id)) + 1;
    const tempMember: TeamMemberResponseDto = {
      id: nextId,
      name,
      isDev,
      isTest,
      sprintCapacities: currentBoard.sprints
        .filter((s) => s.id > 0)
        .map((sprint) => {
          const workingDays = getWorkingDays(sprint.startDate, sprint.endDate);
          return {
            sprintId: sprint.id,
            capacityDev: isDev ? workingDays : 0,
            capacityTest: isTest ? workingDays : 0,
          };
        }),
    };

    // Optimistic update
    const updatedBoard = {
      ...currentBoard,
      teamMembers: [...currentBoard.teamMembers, tempMember],
    };
    this.boardSignal.set(updatedBoard);

    // Sync with backend
    this.teamApi.addTeamMember(currentBoard.id, name, isDev, isTest).subscribe({
      next: (member) => {
        // Replace temp member with real member from backend
        const finalBoard = {
          ...updatedBoard,
          teamMembers: updatedBoard.teamMembers.map((m) =>
            m.id === nextId ? member : m
          ),
        };
        this.boardSignal.set(finalBoard);
        console.log('Team member added:', member);
      },
      error: (error) => {
        console.error('Error adding team member:', error);
        // Rollback on error
        this.boardSignal.set(currentBoard);
        this.errorSignal.set('Failed to add team member. Please try again.');
      },
    });
  }

  /**
   * Update team member details (name/role)
   */
  public updateTeamMember(
    memberId: number,
    name: string,
    role: 'dev' | 'test',
    devTestEnabled: boolean
  ): void {
    const currentBoard = this.boardSignal();
    if (!currentBoard) return;

    const isDev = devTestEnabled ? role === 'dev' : true;
    const isTest = devTestEnabled ? role === 'test' : false;

    const updatedMembers = currentBoard.teamMembers.map((member) =>
      member.id === memberId
        ? { ...member, name, isDev, isTest }
        : member
    );

    const updatedBoard = { ...currentBoard, teamMembers: updatedMembers };
    this.boardSignal.set(updatedBoard);

    this.teamApi.updateTeamMember(currentBoard.id, memberId, name, isDev, isTest).subscribe({
      next: (member) => {
        const finalBoard = {
          ...updatedBoard,
          teamMembers: updatedBoard.teamMembers.map((m) =>
            m.id === memberId ? member : m
          ),
        };
        this.boardSignal.set(finalBoard);
      },
      error: (error) => {
        console.error('Error updating team member:', error);
        this.boardSignal.set(currentBoard);
        this.errorSignal.set('Failed to update team member. Please try again.');
      },
    });
  }

  /**
   * Remove a team member
   */
  public removeTeamMember(memberId: number): void {
    const currentBoard = this.boardSignal();
    if (!currentBoard) return;

    const updatedBoard = {
      ...currentBoard,
      teamMembers: currentBoard.teamMembers.filter((m) => m.id !== memberId),
    };
    this.boardSignal.set(updatedBoard);

    this.teamApi.removeTeamMember(currentBoard.id, memberId).subscribe({
      next: () => {
        console.log(`Team member ${memberId} removed`);
      },
      error: (error) => {
        console.error('Error removing team member:', error);
        this.boardSignal.set(currentBoard);
        this.errorSignal.set('Failed to remove team member. Please try again.');
      },
    });
  }

  /**
   * Update capacities for a team member in a specific sprint
   */
  public updateTeamMemberCapacity(
    memberId: number,
    sprintId: number,
    capacityDev: number,
    capacityTest: number
  ): void {
    const currentBoard = this.boardSignal();
    if (!currentBoard) return;

    // Optimistic update
    const updatedMembers = currentBoard.teamMembers.map((member) => {
      if (member.id !== memberId) return member;

      const updatedCapacities = member.sprintCapacities.map((cap) => {
        if (cap.sprintId !== sprintId) return cap;
        return { ...cap, capacityDev, capacityTest };
      });

      return { ...member, sprintCapacities: updatedCapacities };
    });

    const updatedBoard = { ...currentBoard, teamMembers: updatedMembers };
    this.boardSignal.set(updatedBoard);

    // Sync with backend
    this.teamApi
      .updateCapacity(currentBoard.id, memberId, sprintId, capacityDev, capacityTest)
      .subscribe({
        next: () => {
          console.log(`Capacity updated for member ${memberId} in sprint ${sprintId}`);
        },
        error: (error) => {
          console.error('Error updating capacity:', error);
          // Rollback on error
          this.boardSignal.set(currentBoard);
          this.errorSignal.set('Failed to update capacity. Please try again.');
        },
      });
  }

  /**
   * Refresh feature from Azure DevOps
   */
  public async refreshFeature(
    boardId: number,
    featureId: number,
    organization: string,
    project: string,
    pat: string
  ): Promise<void> {
    try {
      console.log('Refreshing feature from Azure:', { boardId, featureId });
      await firstValueFrom(
        this.featureApi.refreshFeature(boardId, featureId, organization, project, pat)
      );

      // Reload board to show updated data
      console.log('Feature refreshed successfully, reloading board...');
      this.loadBoard(boardId);
    } catch (error: any) {
      console.error('Error refreshing feature:', error);
      throw new Error(error.message || 'Failed to refresh feature');
    }
  }

  /**
   * Reorder features by priority
   */
  public async reorderFeatures(
    boardId: number,
    features: Array<{ featureId: number; newPriority: number }>
  ): Promise<void> {
    try {
      await firstValueFrom(this.featureApi.reorderFeatures(boardId, features));
      this.loadBoard(boardId);
    } catch (error: any) {
      console.error('Error reordering features:', error);
      throw new Error(error.message || 'Failed to reorder features');
    }
  }

  /**
   * Delete feature and its user stories
   */
  public async deleteFeature(boardId: number, featureId: number): Promise<void> {
    try {
      console.log('Deleting feature:', { boardId, featureId });
      await firstValueFrom(this.featureApi.deleteFeature(boardId, featureId));

      // Reload board to show updated data
      console.log('Feature deleted successfully, reloading board...');
      this.loadBoard(boardId);
    } catch (error: any) {
      console.error('Error deleting feature:', error);
      throw new Error(error.message || 'Failed to delete feature');
    }
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

    const tenMinutes = 10 * 60 * 1000;
    if (Date.now() - stored.timestamp > tenMinutes) {
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
   * Submit/finalize board
   */
  public submitBoard(): void {
    const currentBoard = this.boardSignal();
    if (!currentBoard) return;

    console.log('Board submitted:', currentBoard);
    // Implementation for board finalization would go here
  }
}
