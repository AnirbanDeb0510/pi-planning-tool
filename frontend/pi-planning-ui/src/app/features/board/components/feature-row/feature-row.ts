import { Component, Input, Signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { StoryCard } from '../../../../shared/components/story-card/story-card';
import { Board } from '../board';
import { BoardResponseDto, FeatureResponseDto } from '../../../../shared/models/board.dto';
import { LABELS, TOOLTIPS } from '../../../../shared/constants';

@Component({
  selector: 'app-feature-row',
  standalone: true,
  imports: [CommonModule, DragDropModule, MatMenuModule, MatIconModule, MatButtonModule, StoryCard],
  templateUrl: './feature-row.html',
  styleUrls: ['./feature-row.css'],
})
export class FeatureRow {
  @Input() board!: Signal<BoardResponseDto | null>;
  @Input() parent!: Board;
  @Input() feature!: FeatureResponseDto;
  @Input() showDevTest!: Signal<boolean>;

  protected readonly LABELS = LABELS;
  protected readonly TOOLTIPS = TOOLTIPS;

  protected getDisplayedSprints() {
    return this.parent.getDisplayedSprints();
  }

  protected getGridTemplateColumns() {
    return this.parent.getGridTemplateColumns();
  }

  protected getConnectedLists(featureId: number): string[] {
    return this.parent.getConnectedLists(featureId);
  }

  protected getStoriesInSprint(feature: FeatureResponseDto, sprintId: number) {
    return this.parent.getStoriesInSprint(feature, sprintId);
  }

  protected getParkingLotStories(feature: FeatureResponseDto) {
    return this.parent.getParkingLotStories(feature);
  }

  protected getFeatureTotal(feature: FeatureResponseDto): number {
    return this.parent.getFeatureTotal(feature);
  }

  protected getFeatureSprintDevTestTotals(feature: FeatureResponseDto, sprintId: number) {
    return this.parent.getFeatureSprintDevTestTotals(feature, sprintId);
  }

  protected openRefreshFeatureModal(feature: FeatureResponseDto): void {
    this.parent.openRefreshFeatureModal(feature);
  }

  protected openDeleteFeatureModal(feature: FeatureResponseDto): void {
    this.parent.openDeleteFeatureModal(feature);
  }

  protected isOperationBlocked(): boolean {
    return this.parent.isOperationBlocked();
  }

  protected getOperationBlockedMessage(action: string): string {
    return this.parent.getOperationBlockedMessage(action);
  }
}
