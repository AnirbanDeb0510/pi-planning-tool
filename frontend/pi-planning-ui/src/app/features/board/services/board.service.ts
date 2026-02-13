import { Injectable, signal, inject } from '@angular/core';
import {
  BoardResponseDto,
  FeatureResponseDto,
  UserStoryDto,
  TeamMemberResponseDto,
} from '../../../shared/models/board.dto';
import { BoardApiService, StoryApiService, TeamApiService } from './board-api.service';

/**
 * Board Service - State Management Layer
 * Manages board state using signals and coordinates with API services
 * No longer contains mock data - uses injected API services instead
 */
@Injectable({ providedIn: 'root' })
export class BoardService {
  private boardApi = inject(BoardApiService);
  private storyApi = inject(StoryApiService);
  private teamApi = inject(TeamApiService);

  // State signals
  private boardSignal = signal<BoardResponseDto | null>(null);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

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
    const getWorkingDays = (startDate: Date, endDate: Date): number => {
      const msPerDay = 24 * 60 * 60 * 1000;
      const totalDays = Math.round((endDate.getTime() - startDate.getTime()) / msPerDay) + 1;
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
