import { Routes } from '@angular/router';
import { Board } from './features/board/components/board';
import { HomeComponent } from './features/home/home/home.component';
import { CreateBoardComponent } from './features/board/components/create-board/create-board.component';
import { BoardListComponent } from './features/board/components/board-list/board-list.component';
import { EnterYourName } from './shared/components/enter-your-name/enter-your-name';
import { userNameGuard } from './core/guards/user-name.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'boards', component: BoardListComponent },
  { path: 'boards/new', component: CreateBoardComponent },
  { path: 'boards/:id', component: Board, canActivate: [userNameGuard] },
  { path: 'name', component: EnterYourName },
  { path: 'board', redirectTo: 'boards/1', pathMatch: 'full' }, // Redirect old route
  { path: '**', redirectTo: '' }
];
