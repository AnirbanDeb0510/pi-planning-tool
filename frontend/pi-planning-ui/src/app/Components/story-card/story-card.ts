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
}
