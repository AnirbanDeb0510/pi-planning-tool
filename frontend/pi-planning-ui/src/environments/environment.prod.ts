import { RuntimeConfig } from '../app/core/config/runtime-config';

/**
 * Production environment configuration
 */
export const environment = {
  production: true,
  get apiBaseUrl() {
    return RuntimeConfig.apiBaseUrl;
  },
  apiVersion: 'v1',
  logLevel: 'error',
};
