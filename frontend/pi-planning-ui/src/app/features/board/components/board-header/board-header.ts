import { Component, Input, Signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Board } from '../board';
import { BoardResponseDto } from '../../../../shared/models/board.dto';

@Component({
  selector: 'app-board-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './board-header.html',
  styleUrls: ['./board-header.css'],
})
export class BoardHeader {
  @Input() board!: Signal<BoardResponseDto | null>;
  @Input() parent!: Board;
  @Input() showDevTest!: Signal<boolean>;

  toggleDevTest(): void {
    this.parent.toggleDevTest();
  }
}
