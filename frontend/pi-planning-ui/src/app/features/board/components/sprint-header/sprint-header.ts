import { Component, Input, Signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Board } from '../board';
import { BoardResponseDto } from '../../../../shared/models/board.dto';

@Component({
  selector: 'app-sprint-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sprint-header.html',
  styleUrls: ['./sprint-header.css'],
})
export class SprintHeader {
  @Input() board!: Signal<BoardResponseDto | null>;
  @Input() parent!: Board;
  @Input() showDevTest!: Signal<boolean>;

  protected getDisplayedSprints() {
    return this.parent.getDisplayedSprints();
  }

  protected getGridTemplateColumns() {
    return this.parent.getGridTemplateColumns();
  }

  protected getSprintTotals(sprintId: number) {
    return this.parent.getSprintTotals(sprintId);
  }

  protected getSprintCapacityTotals(sprintId: number) {
    return this.parent.getSprintCapacityTotals(sprintId);
  }

  protected isSprintOverCapacity(sprintId: number, type: 'dev' | 'test' | 'total'): boolean {
    return this.parent.isSprintOverCapacity(sprintId, type);
  }
}
