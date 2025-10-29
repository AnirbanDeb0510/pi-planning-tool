import { Component } from '@angular/core';
import { CdkDragDrop, transferArrayItem } from '@angular/cdk/drag-drop';
import { Sprint } from '../../Models/sprint.model';
import { Story } from '../../Models/story.model';
import { CommonModule } from '@angular/common';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { StoryCard } from '../story-card/story-card';

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, DragDropModule, StoryCard],
  templateUrl: './board.html',
  styleUrls: ['./board.css']
})
export class Board {

  parkingLot: Story[] = [
    { id: '1', title: 'Login Page', points: 5, feature: 'Auth' },
    { id: '2', title: 'User Dashboard', points: 8, feature: 'UI' },
    { id: '3', title: 'Email Notifications', points: 3, feature: 'Integration' },
  ];

  sprints: Sprint[] = [
    { id: 'S1', name: 'Sprint 1', stories: [] },
    { id: 'S2', name: 'Sprint 2', stories: [] },
    { id: 'S3', name: 'Sprint 3', stories: [] },
    { id: 'S4', name: 'Sprint 4', stories: [] },
    { id: 'S5', name: 'Sprint 5', stories: [] },
    { id: 'S6', name: 'Sprint 6', stories: [] },
  ];

  get connectedDropLists(): string[] {
    return ['parkingLot', ...this.sprints.map(s => s.id)];
  }

  drop(event: CdkDragDrop<Story[]>) {
    if (event.previousContainer === event.container) return;

    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );
  }

  getSprintTotal(sprint: Sprint): number {
    return sprint.stories.reduce((sum, s) => sum + s.points, 0);
  }
}
