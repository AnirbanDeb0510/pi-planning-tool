import { Injectable, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import {
  SignalrService,
  CapacityUpdatedEvent,
  TeamMemberAddedEvent,
  TeamMemberDeletedEvent,
  TeamMemberUpdatedEvent,
  StoryMovedEvent,
  UserPresenceEvent,
} from './signalr.service';
import { BoardService } from './board.service';
import { UserService } from '../../../core/services/user.service';
import { LABELS } from '../../../shared/constants';

/**
 * Service for managing SignalR realtime connections and event subscriptions for boards
 */
@Injectable({
  providedIn: 'root',
})
export class BoardRealtimeService {
  private signalrService = inject(SignalrService);
  private boardService = inject(BoardService);
  private userService = inject(UserService);

  private connectedBoardId: number | null = null;
  private realtimeSubscriptions: Subscription[] = [];
  private boardReloadTimeoutId: number | null = null;

  // Presence state
  public presenceUsers = signal<UserPresenceEvent[]>([]);

  /**
   * Connect to a board's realtime hub
   */
  async connect(boardId: number): Promise<void> {
    if (this.connectedBoardId === boardId) {
      return; // Already connected to this board
    }

    const rawUserName = this.userService.getName().trim();
    const userName = rawUserName || LABELS.APP.GUEST;
    const userId = this.userService.getOrCreateUserId();

    await this.signalrService.connect(boardId, userId, userName);
    this.connectedBoardId = boardId;

    this.subscribeRealtimeStreams();
  }

  /**
   * Disconnect and cleanup all realtime state
   */
  async disconnect(): Promise<void> {
    await this.signalrService.disconnect();
    this.teardownRealtimeState();
  }

  /**
   * Disconnect if board has changed
   */
  async ensureDisconnectedIfBoardChanged(nextBoardId: number): Promise<void> {
    if (this.connectedBoardId === null || this.connectedBoardId === nextBoardId) {
      return;
    }

    await this.disconnect();
  }

  /**
   * Get the currently connected board ID
   */
  getConnectedBoardId(): number | null {
    return this.connectedBoardId;
  }

  /**
   * Subscribe to all SignalR event streams
   */
  private subscribeRealtimeStreams(): void {
    this.clearRealtimeSubscriptions();

    const presenceSub = this.signalrService.presence$.subscribe((users) => {
      this.presenceUsers.set(users);
    });

    const storyMovedSub = this.signalrService.storyMoved$.subscribe((event) => {
      try {
        this.applyStoryMovedEvent(event);
      } catch (error) {
        console.error('Failed to apply story moved event:', error);
        if (this.connectedBoardId != null) {
          this.scheduleBoardReload(this.connectedBoardId);
        }
      }
    });

    const teamMemberAddedSub = this.signalrService.teamMemberAdded$.subscribe((event) => {
      try {
        this.applyTeamMemberAddedEvent(event);
      } catch (error) {
        console.error('Failed to apply team member added event:', error);
        if (this.connectedBoardId != null) {
          this.scheduleBoardReload(this.connectedBoardId);
        }
      }
    });

    const teamMemberUpdatedSub = this.signalrService.teamMemberUpdated$.subscribe((event) => {
      try {
        this.applyTeamMemberUpdatedEvent(event);
      } catch (error) {
        console.error('Failed to apply team member updated event:', error);
        if (this.connectedBoardId != null) {
          this.scheduleBoardReload(this.connectedBoardId);
        }
      }
    });

    const teamMemberDeletedSub = this.signalrService.teamMemberDeleted$.subscribe((event) => {
      try {
        this.applyTeamMemberDeletedEvent(event);
      } catch (error) {
        console.error('Failed to apply team member deleted event:', error);
        if (this.connectedBoardId != null) {
          this.scheduleBoardReload(this.connectedBoardId);
        }
      }
    });

    const capacityUpdatedSub = this.signalrService.capacityUpdated$.subscribe((event) => {
      try {
        this.applyCapacityUpdatedEvent(event);
      } catch (error) {
        console.error('Failed to apply capacity updated event:', error);
        if (this.connectedBoardId != null) {
          this.scheduleBoardReload(this.connectedBoardId);
        }
      }
    });

    const boardFinalizedSub = this.signalrService.boardFinalized$.subscribe((event) => {
      this.scheduleBoardReload(event.boardId);
    });

    const boardRestoredSub = this.signalrService.boardRestored$.subscribe((event) => {
      this.scheduleBoardReload(event.boardId);
    });

    const boardLockStateChangedSub = this.signalrService.boardLockStateChanged$.subscribe(
      (event) => {
        if (event.boardId === this.connectedBoardId) {
          const currentBoard = this.boardService.board();
          if (currentBoard) {
            // Directly update lock state without reloading entire board
            this.boardService.updateBoardState({ ...currentBoard, isLocked: event.isLocked });
          }
        }
      },
    );

    const featureImportedSub = this.signalrService.featureImported$.subscribe((event) => {
      this.scheduleBoardReload(event.boardId);
    });

    const featureRefreshedSub = this.signalrService.featureRefreshed$.subscribe((event) => {
      this.scheduleBoardReload(event.boardId);
    });

    const featuresReorderedSub = this.signalrService.featuresReordered$.subscribe((event) => {
      this.scheduleBoardReload(event.boardId);
    });

    const featureDeletedSub = this.signalrService.featureDeleted$.subscribe((event) => {
      this.scheduleBoardReload(event.boardId);
    });

    const storyRefreshedSub = this.signalrService.storyRefreshed$.subscribe(() => {
      if (this.connectedBoardId != null) {
        this.scheduleBoardReload(this.connectedBoardId);
      }
    });

    this.realtimeSubscriptions.push(
      presenceSub,
      storyMovedSub,
      teamMemberAddedSub,
      teamMemberUpdatedSub,
      teamMemberDeletedSub,
      capacityUpdatedSub,
      boardFinalizedSub,
      boardRestoredSub,
      boardLockStateChangedSub,
      featureImportedSub,
      featureRefreshedSub,
      featuresReorderedSub,
      featureDeletedSub,
      storyRefreshedSub,
    );
  }

  /**
   * Apply story moved event to board state
   */
  private applyStoryMovedEvent(event: StoryMovedEvent): void {
    if (this.connectedBoardId !== event.boardId) {
      return;
    }

    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) {
      return;
    }

    let hasChanges = false;
    const updatedFeatures = currentBoard.features.map((feature) => {
      const updatedStories = feature.userStories.map((story) => {
        if (story.id !== event.storyId) {
          return story;
        }

        hasChanges = hasChanges || story.sprintId !== event.targetSprintId;
        return {
          ...story,
          sprintId: event.targetSprintId,
          isMoved: story.originalSprintId !== event.targetSprintId,
        };
      });

      return {
        ...feature,
        userStories: updatedStories,
      };
    });

    if (!hasChanges) {
      return;
    }

    this.boardService.updateBoardState({
      ...currentBoard,
      features: updatedFeatures,
    });
  }

  /**
   * Apply team member added event to board state
   */
  private applyTeamMemberAddedEvent(event: TeamMemberAddedEvent): void {
    if (this.connectedBoardId !== event.boardId) {
      return;
    }

    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) {
      return;
    }

    const existingMemberIndex = currentBoard.teamMembers.findIndex(
      (member) => member.id === event.teamMember.id,
    );

    const updatedTeamMembers = [...currentBoard.teamMembers];
    if (existingMemberIndex === -1) {
      updatedTeamMembers.push(event.teamMember);
    } else {
      updatedTeamMembers[existingMemberIndex] = event.teamMember;
    }

    this.boardService.updateBoardState({
      ...currentBoard,
      teamMembers: updatedTeamMembers,
    });
  }

  /**
   * Apply team member updated event to board state
   */
  private applyTeamMemberUpdatedEvent(event: TeamMemberUpdatedEvent): void {
    if (this.connectedBoardId !== event.boardId) {
      return;
    }

    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) {
      return;
    }

    let hasChanges = false;
    const updatedTeamMembers = currentBoard.teamMembers.map((member) => {
      if (member.id !== event.teamMember.id) {
        return member;
      }

      hasChanges = true;
      return event.teamMember;
    });

    if (!hasChanges) {
      return;
    }

    this.boardService.updateBoardState({
      ...currentBoard,
      teamMembers: updatedTeamMembers,
    });
  }

  /**
   * Apply team member deleted event to board state
   */
  private applyTeamMemberDeletedEvent(event: TeamMemberDeletedEvent): void {
    if (this.connectedBoardId !== event.boardId) {
      return;
    }

    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) {
      return;
    }

    const updatedTeamMembers = currentBoard.teamMembers.filter(
      (member) => member.id !== event.teamMemberId,
    );

    if (updatedTeamMembers.length === currentBoard.teamMembers.length) {
      return;
    }

    this.boardService.updateBoardState({
      ...currentBoard,
      teamMembers: updatedTeamMembers,
    });
  }

  /**
   * Apply capacity updated event to board state
   */
  private applyCapacityUpdatedEvent(event: CapacityUpdatedEvent): void {
    if (this.connectedBoardId !== event.boardId) {
      return;
    }

    const currentBoard = this.boardService.getBoard();
    if (!currentBoard) {
      return;
    }

    let hasChanges = false;
    const updatedTeamMembers = currentBoard.teamMembers.map((member) => {
      if (member.id !== event.teamMemberId) {
        return member;
      }

      const capacityIndex = member.sprintCapacities.findIndex(
        (capacity) => capacity.sprintId === event.sprintId,
      );

      const updatedSprintCapacities = [...member.sprintCapacities];
      if (capacityIndex === -1) {
        updatedSprintCapacities.push({
          sprintId: event.sprintId,
          capacityDev: event.capacityDev,
          capacityTest: event.capacityTest,
        });
      } else {
        const currentCapacity = updatedSprintCapacities[capacityIndex];
        if (
          currentCapacity.capacityDev === event.capacityDev &&
          currentCapacity.capacityTest === event.capacityTest
        ) {
          return member;
        }

        updatedSprintCapacities[capacityIndex] = {
          ...currentCapacity,
          capacityDev: event.capacityDev,
          capacityTest: event.capacityTest,
        };
      }

      hasChanges = true;
      return {
        ...member,
        sprintCapacities: updatedSprintCapacities,
      };
    });

    if (!hasChanges) {
      return;
    }

    this.boardService.updateBoardState({
      ...currentBoard,
      teamMembers: updatedTeamMembers,
    });
  }

  /**
   * Schedule a board reload after a short delay (debounced)
   */
  private scheduleBoardReload(boardId: number): void {
    if (this.connectedBoardId !== boardId) {
      return;
    }

    if (this.boardReloadTimeoutId !== null) {
      return; // Already scheduled
    }

    this.boardReloadTimeoutId = window.setTimeout(() => {
      this.boardReloadTimeoutId = null;
      this.boardService.loadBoard(boardId);
    }, 1000);
  }

  /**
   * Teardown all realtime state
   */
  private teardownRealtimeState(): void {
    this.connectedBoardId = null;
    this.clearRealtimeSubscriptions();
    this.presenceUsers.set([]);

    if (this.boardReloadTimeoutId !== null) {
      window.clearTimeout(this.boardReloadTimeoutId);
      this.boardReloadTimeoutId = null;
    }
  }

  /**
   * Clear all SignalR subscriptions
   */
  private clearRealtimeSubscriptions(): void {
    this.realtimeSubscriptions.forEach((subscription) => subscription.unsubscribe());
    this.realtimeSubscriptions = [];
  }
}
