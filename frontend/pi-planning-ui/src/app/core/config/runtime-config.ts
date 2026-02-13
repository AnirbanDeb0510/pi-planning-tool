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
    this.config = {
      apiBaseUrl: windowEnv?.['apiBaseUrl'] || 'http://localhost:5000',
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
}
