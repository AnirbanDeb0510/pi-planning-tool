import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { Board } from './app/Components/board/board';

bootstrapApplication(Board, appConfig)
  .catch((err) => console.error(err));
