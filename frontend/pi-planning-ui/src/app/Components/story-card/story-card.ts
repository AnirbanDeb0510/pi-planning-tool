import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Story } from '../../Models/story.model';
import { Board } from '../board/board';

@Component({
  selector: 'app-story-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './story-card.html',
  styleUrl: './story-card.css'
})
export class StoryCard {
  @Input() story!: Story;
  @Input() parent!: Board;
}
