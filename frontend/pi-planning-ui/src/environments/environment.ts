import { RuntimeConfig } from '../app/core/config/runtime-config';

/**
 * Development environment configuration
 */
export const environment = {
  production: false,
  get apiBaseUrl() {
    return RuntimeConfig.apiBaseUrl;
  },
  apiVersion: 'v1',
  logLevel: 'debug',
};
