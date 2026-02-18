import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserStoryDto } from '../../shared/models/board.dto';
import { Board } from '../board/board';

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

  /**
   * Get sprint name by ID from parent board
   */
  getSprintName(sprintId: number | undefined): string {
    return this.parent?.getSprintNameById?.(sprintId) || `Sprint ${sprintId || 'Parking Lot'}`;
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
      return { type: 'new', label: 'Added post-plan', icon: '🆕' };
    }

    // Moved story: OriginalSprint name != CurrentSprint name
    if (originalSprintName !== currentSprintName) {
      return { 
        type: 'moved', 
        label: `Moved from ${originalSprintName}`, 
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
