import { InjectionToken } from '@angular/core';

/** Runtime configuration loaded from /assets/config.json before the app starts. */
export interface AppRuntimeConfig {
  gatewayUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppRuntimeConfig>('APP_CONFIG');

/** Loaded once at bootstrap; services read from here. */
export const runtimeConfig: AppRuntimeConfig = {
  gatewayUrl: 'http://localhost:8000',
};

/**
 * Fetches /assets/config.json at startup so the API URL can be changed on the
 * server (or in Docker) without rebuilding the Angular bundle. Falls back to
 * the default if the file is missing.
 */
export async function loadRuntimeConfig(): Promise<void> {
  try {
    const res = await fetch('/assets/config.json', { cache: 'no-store' });
    if (res.ok) {
      const cfg = (await res.json()) as Partial<AppRuntimeConfig>;
      if (cfg.gatewayUrl) runtimeConfig.gatewayUrl = cfg.gatewayUrl;
    }
  } catch {
    /* keep default */
  }
}
