import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClientService } from '../../../core/services/http-client.service';
import { BOARD_API, FEATURE_API, STORY_API, TEAM_API, AZURE_API } from '../../../core/constants/api-endpoints.constants';
import {
  BoardResponseDto,
  FeatureResponseDto,
  UserStoryDto,
  TeamMemberResponseDto,
} from '../../../shared/models/board.dto';
import {
  BoardCreateDto,
  BoardCreatedDto,
  BoardSummaryDto,
  BoardFilters,
} from '../../../shared/models/board-api.dto';
import { IBoardApiService, IFeatureApiService, IStoryApiService, ITeamApiService } from './board-api.interface';

/**
 * Board API Service Implementation
 * Handles HTTP calls to backend board endpoints
 */
@Injectable({ providedIn: 'root' })
export class BoardApiService implements IBoardApiService {
  private http = inject(HttpClientService);

  getBoard(id: number): Observable<BoardResponseDto> {
    return this.http.get<BoardResponseDto>(BOARD_API.GET_BOARD(id));
  }

  createBoard(dto: BoardCreateDto): Observable<BoardCreatedDto> {
    return this.http.post<BoardCreatedDto>(BOARD_API.CREATE_BOARD, dto);
  }

  getBoardList(filters?: BoardFilters): Observable<BoardSummaryDto[]> {
    const params: any = {};
    if (filters) {
      if (filters.search) params.search = filters.search;
      if (filters.organization) params.organization = filters.organization;
      if (filters.project) params.project = filters.project;
      if (filters.isLocked !== undefined) params.isLocked = filters.isLocked.toString();
      if (filters.isFinalized !== undefined) params.isFinalized = filters.isFinalized.toString();
    }
    return this.http.get<BoardSummaryDto[]>(BOARD_API.GET_BOARD_LIST, { params });
  }

  lockBoard(id: number): Observable<void> {
    return this.http.patch<void>(BOARD_API.LOCK_BOARD(id), {});
  }

  unlockBoard(id: number): Observable<void> {
    return this.http.patch<void>(BOARD_API.UNLOCK_BOARD(id), {});
  }

  finalizeBoard(id: number): Observable<void> {
    return this.http.patch<void>(BOARD_API.FINALIZE_BOARD(id), {});
  }

  deleteBoard(id: number): Observable<void> {
    return this.http.delete<void>(BOARD_API.DELETE_BOARD(id));
  }
}

/**
 * Feature API Service Implementation
 */
@Injectable({ providedIn: 'root' })
export class FeatureApiService implements IFeatureApiService {
  private http = inject(HttpClientService);

  importFeature(boardId: number, featureDto: any): Observable<FeatureResponseDto> {
    return this.http.post<FeatureResponseDto>(FEATURE_API.IMPORT(boardId), featureDto);
  }

  reorderFeatures(
    boardId: number,
    features: Array<{ featureId: number; newPriority: number }>
  ): Observable<void> {
    return this.http.patch<void>(FEATURE_API.REORDER(boardId), {
      features,
    });
  }

  refreshFeature(
    boardId: number,
    featureId: number,
    organization: string,
    project: string,
    pat: string
  ): Observable<FeatureResponseDto> {
    return this.http.patch<FeatureResponseDto>(
      FEATURE_API.REFRESH(boardId, featureId),
      {},
      { params: { organization, project, pat } }
    );
  }

  deleteFeature(boardId: number, featureId: number): Observable<void> {
    return this.http.delete<void>(FEATURE_API.DELETE(boardId, featureId));
  }
}

/**
 * Story API Service Implementation
 */
@Injectable({ providedIn: 'root' })
export class StoryApiService implements IStoryApiService {
  private http = inject(HttpClientService);

  moveStory(boardId: number, storyId: number, targetSprintId: number): Observable<void> {
    return this.http.patch<void>(STORY_API.MOVE(boardId, storyId), {
      targetSprintId,
    });
  }

  refreshStory(boardId: number, storyId: number): Observable<UserStoryDto> {
    return this.http.patch<UserStoryDto>(STORY_API.REFRESH(boardId, storyId), {});
  }
}

/**
 * Team API Service Implementation
 */
@Injectable({ providedIn: 'root' })
export class TeamApiService implements ITeamApiService {
  private http = inject(HttpClientService);

  getTeamMembers(boardId: number): Observable<TeamMemberResponseDto[]> {
    return this.http.get<TeamMemberResponseDto[]>(TEAM_API.GET_MEMBERS(boardId));
  }

  addTeamMember(
    boardId: number,
    name: string,
    isDev: boolean,
    isTest: boolean
  ): Observable<TeamMemberResponseDto> {
    return this.http.post<TeamMemberResponseDto>(TEAM_API.ADD_MEMBER(boardId), {
      name,
      isDev,
      isTest,
    });
  }

  updateCapacity(
    boardId: number,
    memberId: number,
    sprintId: number,
    capacityDev: number,
    capacityTest: number
  ): Observable<void> {
    return this.http.patch<void>(TEAM_API.UPDATE_CAPACITY(boardId), {
      memberId,
      sprintId,
      capacityDev,
      capacityTest,
    });
  }

  removeTeamMember(boardId: number, memberId: number): Observable<void> {
    return this.http.delete<void>(TEAM_API.REMOVE_MEMBER(boardId, memberId));
  }
}

/**
 * Azure DevOps API Service Implementation
 */
@Injectable({ providedIn: 'root' })
export class AzureApiService {
  private http = inject(HttpClientService);

  /**
   * Fetch feature with children from Azure DevOps
   */
  getFeatureWithChildren(
    organization: string,
    project: string,
    featureId: string,
    pat: string
  ): Observable<FeatureResponseDto> {
    return this.http.get<FeatureResponseDto>(
      AZURE_API.GET_FEATURE(organization, project, featureId),
      { params: { pat } }
    );
  }
}
