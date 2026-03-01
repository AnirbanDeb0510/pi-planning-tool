import { Component, Input, Signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Board } from '../board';
import { BoardResponseDto } from '../../../../shared/models/board.dto';
import { LABELS, TOOLTIPS } from '../../../../shared/constants';

@Component({
  selector: 'app-board-header',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './board-header.html',
  styleUrls: ['./board-header.css'],
})
export class BoardHeader {
  @Input() board!: Signal<BoardResponseDto | null>;
  @Input() parent!: Board;
  @Input() showDevTest!: Signal<boolean>;

  protected readonly LABELS = LABELS;
  protected readonly TOOLTIPS = TOOLTIPS;

  toggleDevTest(): void {
    this.parent.toggleDevTest();
  }

  refreshBoard(): void {
    const currentBoard = this.board();
    if (currentBoard) {
      this.parent.boardService.loadBoard(currentBoard.id);
    }
  }

  openLockModal(): void {
    this.parent.modals?.openLockBoardModal();
  }

  openUnlockModal(): void {
    this.parent.modals?.openUnlockBoardModal();
  }
}
