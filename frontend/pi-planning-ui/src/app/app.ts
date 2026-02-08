import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly title = signal('pi-planning-ui');
  private readonly themeService = inject(ThemeService);

  // Expose theme service to template
  protected readonly currentTheme = this.themeService.theme;

  protected toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
