import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserStoryDto } from '../../models/board.dto';
import { Board } from '../../../features/board/components/board';
import { LABELS, MESSAGES } from '../../constants';

@Component({
  selector: 'app-story-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './story-card.html',
  styleUrl: './story-card.css',
})
export class StoryCard {
  @Input() story!: UserStoryDto;
  @Input() parent!: Board;

  protected readonly LABELS = LABELS;
  protected readonly MESSAGES = MESSAGES;

  /**
   * Get sprint name by ID from parent board
   */
  getSprintName(sprintId: number | undefined): string {
    if (this.parent?.getSprintNameById) {
      return this.parent.getSprintNameById(sprintId);
    }
    const sprintLabel = sprintId ? `${LABELS.FIELDS.SPRINT} ${sprintId}` : LABELS.FIELDS.PARKING_LOT;
    return sprintLabel;
  }

  /**
   * Get story status indicator based on movement tracking
   */
  getStoryIndicator(): { type: 'normal' | 'moved' | 'new'; label: string; icon: string } {
    const originalSprintId = this.story.originalSprintId;
    const currentSprintId = this.story.sprintId;

    // Get sprint names for comparison
    const originalSprintName = this.getSprintName(originalSprintId);
    const currentSprintName = this.getSprintName(currentSprintId);

    // New story: OriginalSprint is "Sprint 0" (parking lot)
    if (originalSprintName.toLowerCase().includes('sprint 0')) {
      return { type: 'new', label: MESSAGES.STORY.ADDED_POST_PLAN, icon: '🆕' };
    }

    // Moved story: OriginalSprint name != CurrentSprint name
    if (originalSprintName !== currentSprintName) {
      return { 
        type: 'moved', 
        label: MESSAGES.STORY.MOVED_FROM(originalSprintName), 
        icon: '📍' 
      };
    }

    // Normal: no change
    return { type: 'normal', label: '', icon: '' };
  }

  /**
   * Check if story has been moved
   */
  hasMoved(): boolean {
    return this.getStoryIndicator().type !== 'normal';
  }
}
