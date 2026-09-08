import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { loadRuntimeConfig } from './app/app-config';

// Load runtime config (gateway URL) before bootstrapping so services use the
// correct API address in every environment (local, Docker, production).
loadRuntimeConfig().then(() =>
  bootstrapApplication(App, appConfig).catch((err) => console.error(err)),
);
