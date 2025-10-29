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
      name: 'Backend',
      parkingLot: [
        { id: 'A1', title: 'Springboot', points: 5, feature: 'Backend' },
        { id: 'A2', title: 'Dotnet', points: 3, feature: 'Backend' },
        { id: 'A3', title: 'Ruby', points: 8, feature: 'Backend' }
      ],
      stories: { S1: [], S2: [], S3: [], S4: [], S5: [], S6: [] }
    },
    {
      name: 'UI',
      parkingLot: [
        { id: 'U1', title: 'Angular', points: 8, feature: 'UI' },
        { id: 'U2', title: 'React', points: 5, feature: 'UI' }
      ],
      stories: { S1: [], S2: [], S3: [], S4: [], S5: [], S6: [] }
    },
    {
      name: 'Integration',
      parkingLot: [
        { id: 'I1', title: 'API 1', points: 3, feature: 'Integration' },
        { id: 'I2', title: 'API 2', points: 8, feature: 'Integration' }
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
    }
  }

  getConnectedLists(feature: Feature): string[] {
    return [
      `${feature.name}_parkingLot`,
      ...this.sprints.map(s => `${feature.name}_${s.id}`)
    ];
  }

  getSprintTotal(sprintId: string): number {
    return this.features
      .map(f => f.stories[sprintId].reduce((sum, s) => sum + s.points, 0))
      .reduce((a, b) => a + b, 0);
  }

  getFeatureTotal(feature: Feature): number {
    const parkingLotTotal = feature.parkingLot.reduce((sum, s) => sum + s.points, 0);

    const sprintTotals = Object.values(feature.stories)
      .map(stories => stories.reduce((sum, s) => sum + s.points, 0))
      .reduce((a, b) => a + b, 0);

    return parkingLotTotal + sprintTotals;
  }
}
