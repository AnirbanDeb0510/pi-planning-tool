import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BoardApiService } from '../../services/board-api.service';
import { BoardSummaryDto, BoardFilters } from '../../../../shared/models/board-api.dto';
import { LABELS, MESSAGES, PLACEHOLDERS } from '../../../../shared/constants';

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

  // Mandatory filters (text inputs - user must know the values)
  protected organizationInput = signal<string>('');
  protected projectInput = signal<string>('');

  // Optional filters
  protected searchTerm = '';
  protected showLockedOnly = false;
  protected showFinalizedOnly = false;

  protected readonly LABELS = LABELS;
  protected readonly MESSAGES = MESSAGES;
  protected readonly PLACEHOLDERS = PLACEHOLDERS;

  ngOnInit(): void {
    // No need to load anything on init - user must provide org/project
  }

  /**
   * Handle organization input change
   */
  onOrganizationChange(): void {
    this.loadBoards();
  }

  /**
   * Handle project input change
   */
  onProjectChange(): void {
    this.loadBoards();
  }

  /**
   * Load boards based on provided org/project and optional filters
   * Only calls API if both org and project are provided
   */
  loadBoards(): void {
    const org = this.organizationInput().trim();
    const project = this.projectInput().trim();

    // Org and project must be provided
    if (!org || !project) {
      this.boards.set([]);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const filters: BoardFilters = {
      organization: org,
      project: project,
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
        this.error.set(error.message || MESSAGES.BOARD_LIST.ERROR);
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
