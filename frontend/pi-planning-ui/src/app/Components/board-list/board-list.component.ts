import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BoardApiService } from '../../features/board/services/board-api.service';
import { BoardSummaryDto, BoardFilters } from '../../shared/models/board-api.dto';

/**
 * Board List Component
 * Browse and search existing boards
 */
@Component({
  selector: 'app-board-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './board-list.component.html',
  styleUrls: ['./board-list.component.css']
})
export class BoardListComponent implements OnInit {
  private boardApi = inject(BoardApiService);
  private router = inject(Router);

  protected boards = signal<BoardSummaryDto[]>([]);
  protected loading = signal(false);
  protected error = signal<string | null>(null);

  protected searchTerm = '';
  protected showLockedOnly = false;
  protected showFinalizedOnly = false;

  ngOnInit(): void {
    this.loadBoards();
  }

  loadBoards(): void {
    this.loading.set(true);
    this.error.set(null);

    const filters: BoardFilters = {
      search: this.searchTerm || undefined,
      isLocked: this.showLockedOnly ? true : undefined,
      isFinalized: this.showFinalizedOnly ? true : undefined,
    };

    this.boardApi.getBoardList(filters).subscribe({
      next: (boards: BoardSummaryDto[]) => {
        this.boards.set(boards);
        this.loading.set(false);
      },
      error: (error: any) => {
        this.error.set(error.message || 'Failed to load boards');
        this.loading.set(false);
        console.error('Error loading boards:', error);
      },
    });
  }

  onSearchChange(): void {
    this.loadBoards();
  }

  onFilterChange(): void {
    this.loadBoards();
  }

  openBoard(id: number): void {
    this.router.navigate(['/boards', id]);
  }

  navigateToCreate(): void {
    this.router.navigate(['/boards/new']);
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}
