/**
 * Runtime Configuration Service
 * Reads configuration from window object (injected by Docker at runtime)
 */
export class RuntimeConfig {
  private static config: any = null;

  /**
   * Load configuration from window object
   */
  static load(): void {
    const windowEnv = (window as any)['__env'];
    const rawPatTtlMinutes = windowEnv?.['patTtlMinutes'];
    const parsedPatTtlMinutes = Number.parseInt(rawPatTtlMinutes, 10);
    this.config = {
      apiBaseUrl: windowEnv?.['apiBaseUrl'] || 'http://localhost:5000',
      patTtlMinutes: Number.isFinite(parsedPatTtlMinutes) && parsedPatTtlMinutes > 0
        ? parsedPatTtlMinutes
        : 10,
    };
  }

  /**
   * Get API base URL
   */
  static get apiBaseUrl(): string {
    if (!this.config) {
      this.load();
    }
    return this.config.apiBaseUrl;
  }

  /**
   * PAT time-to-live in minutes (defaults to 10 if not configured)
   */
  static get patTtlMinutes(): number {
    if (!this.config) {
      this.load();
    }
    return this.config.patTtlMinutes;
  }
}
