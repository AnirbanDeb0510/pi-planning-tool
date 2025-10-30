import { Component } from '@angular/core';
import { CdkDragDrop, transferArrayItem } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { StoryCard } from '../story-card/story-card';
import { Story } from '../../Models/story.model';
import { Sprint } from '../../Models/sprint.model';
import { Feature } from '../../Models/feature.model';

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, DragDropModule, StoryCard],
  templateUrl: './board.html',
  styleUrls: ['./board.css']
})
export class Board {
  endResult: Feature[] = [];
  cursorName = 'Arnab';
  cursorX = 0;
  cursorY = 0;

  sprints: Sprint[] = [
    { id: 'S1', name: 'Sprint 1' },
    { id: 'S2', name: 'Sprint 2' },
    { id: 'S3', name: 'Sprint 3' },
    { id: 'S4', name: 'Sprint 4' },
    { id: 'S5', name: 'Sprint 5' },
    { id: 'S6', name: 'Sprint 6' }
  ];

  features: Feature[] = [
    {
      name: 'Auth',
      parkingLot: [
        { id: 'A1', title: 'Login Page', devPoints: 3, testPoints: 2, feature: 'Auth' },
        { id: 'A2', title: 'Password Reset', points: 3, feature: 'Auth' }
      ],
      stories: { S1: [], S2: [], S3: [], S4: [], S5: [], S6: [] }
    },
    {
      name: 'UI',
      parkingLot: [
        { id: 'U1', title: 'User Dashboard', devPoints: 5, testPoints: 3, feature: 'UI' },
        { id: 'U2', title: 'Settings Panel', points: 5, feature: 'UI' }
      ],
      stories: { S1: [], S2: [], S3: [], S4: [], S5: [], S6: [] }
    },
    {
      name: 'Integration',
      parkingLot: [
        { id: 'I1', title: 'Email Notifications', devPoints: 2, testPoints: 1, feature: 'Integration' },
        { id: 'I2', title: 'API Sync Job', points: 8, feature: 'Integration' }
      ],
      stories: { S1: [], S2: [], S3: [], S4: [], S5: [], S6: [] }
    }
  ];


  drop(event: CdkDragDrop<Story[]>) {
    if (event.previousContainer === event.container) return;

    const [featureSrc, containerSrc] = event.previousContainer.id.split('_');
    const [featureTgt, containerTgt] = event.container.id.split('_');

    // Allow drop only within same feature
    if (featureSrc === featureTgt) {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
      this.endResult = this.features;
    }
  }

  getConnectedLists(feature: Feature): string[] {
    return [
      `${feature.name}_parkingLot`,
      ...this.sprints.map(s => `${feature.name}_${s.id}`)
    ];
  }

  getFeatureTotal(feature: Feature): number {
    const parkingLotTotal = feature.parkingLot
      .reduce((sum, s) => sum + this.getStoryTotalPoints(s), 0);

    const sprintTotals = Object.values(feature.stories)
      .map(stories => stories.reduce((sum, s) => sum + this.getStoryTotalPoints(s), 0))
      .reduce((a, b) => a + b, 0);

    return parkingLotTotal + sprintTotals;
  }

  getSprintTotal(sprintId: string): number {
    return this.features
      .map(f =>
        f.stories[sprintId]
          .reduce((sum, s) => sum + this.getStoryTotalPoints(s), 0)
      )
      .reduce((a, b) => a + b, 0);
  }

  private getStoryTotalPoints(story: Story): number {
    const dev = story.devPoints ?? 0;
    const test = story.testPoints ?? 0;
    const base = story.points ?? 0;
    return dev + test + base;
  }

  getSprintTotals(sprintId: string): { dev: number; test: number; total: number } {
    let dev = 0, test = 0, total = 0;

    this.features.forEach(feature => {
      feature.stories[sprintId].forEach(story => {
        dev += story.devPoints ?? 0;
        test += story.testPoints ?? 0;
        total += (story.devPoints ?? 0) + (story.testPoints ?? 0) + (story.points ?? 0);
      });
    });

    return { dev, test, total };
  }

  sendBoardResult(): void {
    console.log('Final Board State:', this.endResult);
  }

  onMouseMove(event: MouseEvent) {
    this.cursorX = event.pageX + 25; // small offset so it doesn’t sit exactly under the cursor
    this.cursorY = event.pageY + 25;
  }
}
