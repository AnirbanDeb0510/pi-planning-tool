import { Injectable, inject } from '@angular/core';
import { BoardService } from './board.service';

/**
 * Sprint Service
 * Manages sprint-related operations
 * Currently a placeholder for future sprint management features
 */
@Injectable({ providedIn: 'root' })
export class SprintService {
  private boardService = inject(BoardService);

  /**
   * Get current board sprints
   */
  public getSprints() {
    const board = this.boardService.getBoard();
    return board?.sprints || [];
  }

  /**
   * Get sprint by ID
   */
  public getSprintById(sprintId: number) {
    return this.getSprints().find((s) => s.id === sprintId);
  }

  /**
   * Check if a sprint is archived (id <= 0)
   */
  public isSprintArchived(sprintId: number): boolean {
    return sprintId <= 0;
  }
}
