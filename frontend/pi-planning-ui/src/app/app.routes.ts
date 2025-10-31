import { Routes } from '@angular/router';
import { Board } from './Components/board/board';
import { EnterYourName } from './Components/enter-your-name/enter-your-name';

export const routes: Routes = [
  { path: '', component: EnterYourName },
  { path: 'board', component: Board },
  { path: '**', redirectTo: '' }
];