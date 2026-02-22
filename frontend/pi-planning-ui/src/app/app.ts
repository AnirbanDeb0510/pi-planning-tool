import { Component, signal, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { filter } from 'rxjs/operators';
import { ThemeService } from './core/services/theme.service';
import { BoardService } from './features/board/services/board.service';
import { LABELS, TOOLTIPS } from './shared/constants';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly LABELS = LABELS;
  protected readonly TOOLTIPS = TOOLTIPS;
  protected readonly title = signal(LABELS.APP.TITLE);
  private readonly themeService = inject(ThemeService);
  private readonly boardService = inject(BoardService);
  private readonly router = inject(Router);
  private readonly titleService = inject(Title);

  // Expose theme service to template
  protected readonly currentTheme = this.themeService.theme;

  constructor() {
    // Update title when board changes
    effect(() => {
      const board = this.boardService.board();
      if (board) {
        this.title.set(board.name);
        this.titleService.setTitle(`${board.name} - ${LABELS.APP.TITLE}`);
      } else {
        this.title.set(LABELS.APP.TITLE);
        this.titleService.setTitle(LABELS.APP.TITLE);
      }
    });

    // Listen to route changes to reset title when leaving board
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        if (!event.url.includes('/board/')) {
          this.title.set(LABELS.APP.TITLE);
          this.titleService.setTitle(LABELS.APP.TITLE);
        }
      });
  }

  protected toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  protected navigateHome(): void {
    this.router.navigate(['/']);
  }
}
