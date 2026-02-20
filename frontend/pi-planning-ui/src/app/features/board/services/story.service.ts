import { Injectable, inject } from '@angular/core';
import { StoryApiService } from './board-api.service';
import { BoardService } from './board.service';
import { BoardResponseDto, FeatureResponseDto, UserStoryDto } from '../../../shared/models/board.dto';

/**
 * Story Service
 * Manages user story operations: move between sprints
 */
@Injectable({ providedIn: 'root' })
export class StoryService {
  private storyApi = inject(StoryApiService);
  private boardService = inject(BoardService);

  /**
   * Move a story from one sprint to another
   * Updates local state optimistically, then syncs with backend
   */
  public moveStory(storyId: number, fromSprintId: number, toSprintId: number): void {
    const currentBoard = this.boardService.getBoard();
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
      this.boardService.updateBoardState(updatedBoard);

      // Sync with backend (if using real API)
      this.storyApi.moveStory(currentBoard.id, storyId, toSprintId).subscribe({
        next: () => {
          console.log(`Story ${storyId} moved from Sprint ${fromSprintId} to Sprint ${toSprintId}`);
        },
        error: (error) => {
          console.error('Error moving story:', error);
          // Rollback on error
          this.boardService.updateBoardState(currentBoard);
          this.boardService.setError('Failed to move story. Please try again.');
        },
      });
    }
  }
}
