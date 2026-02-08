import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly THEME_KEY = 'theme-preference';
  private readonly themeSignal = signal<Theme>(this.getInitialTheme());

  public theme = this.themeSignal.asReadonly();

  constructor() {
    // Apply theme to document on init
    this.applyTheme(this.themeSignal());
  }

  /**
   * Get initial theme: check localStorage, then system preference, default to light
   */
  private getInitialTheme(): Theme {
    // 1. Check localStorage
    const stored = localStorage.getItem(this.THEME_KEY) as Theme | null;
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }

    // 2. Check system preference
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }

    // 3. Default to light
    return 'light';
  }

  /**
   * Toggle theme between light and dark
   */
  public toggleTheme(): void {
    const newTheme = this.themeSignal() === 'light' ? 'dark' : 'light';
    this.themeSignal.set(newTheme);
    this.applyTheme(newTheme);
    localStorage.setItem(this.THEME_KEY, newTheme);
  }

  /**
   * Set theme explicitly
   */
  public setTheme(theme: Theme): void {
    this.themeSignal.set(theme);
    this.applyTheme(theme);
    localStorage.setItem(this.THEME_KEY, theme);
  }

  /**
   * Apply theme class to document
   */
  private applyTheme(theme: Theme): void {
    const html = document.documentElement;
    if (theme === 'dark') {
      html.classList.add('dark-theme');
    } else {
      html.classList.remove('dark-theme');
    }
  }
}
