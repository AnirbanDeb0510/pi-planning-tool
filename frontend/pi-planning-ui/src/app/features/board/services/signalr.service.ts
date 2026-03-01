import { Injectable, NgZone, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { RuntimeConfig } from '../../../core/config/runtime-config';
import { TeamMemberResponseDto, UserStoryDto } from '../../../shared/models/board.dto';

export interface CursorPresenceEvent {
  boardId: number;
  userId: string;
  displayName: string;
  cursor: {
    x: number;
    y: number;
  };
  coordinateSpace: 'board';
  color: string;
  avatar: string;
  activity: string;
  sequence: number;
  timestampUtc: string;
}

export interface UserPresenceEvent {
  boardId: number;
  userId: string;
  displayName: string;
  color: string;
  avatar: string;
  timestampUtc: string;
  reason: 'joined' | 'leave' | 'disconnect' | 'timeout';
}

export interface StoryMovedEvent {
  boardId: number;
  storyId: number;
  targetSprintId: number;
  timestampUtc: string;
}

export interface TeamMemberAddedEvent {
  boardId: number;
  teamMember: TeamMemberResponseDto;
  timestampUtc: string;
}

export interface TeamMemberUpdatedEvent {
  boardId: number;
  teamMember: TeamMemberResponseDto;
  timestampUtc: string;
}

export interface TeamMemberDeletedEvent {
  boardId: number;
  teamMemberId: number;
  timestampUtc: string;
}

export interface CapacityUpdatedEvent {
  boardId: number;
  teamMemberId: number;
  sprintId: number;
  capacityDev: number;
  capacityTest: number;
  timestampUtc: string;
}

export interface BoardFinalizedEvent {
  boardId: number;
  isFinalized: boolean;
  timestampUtc: string;
}

export interface BoardRestoredEvent {
  boardId: number;
  isFinalized: boolean;
  timestampUtc: string;
}

export interface FeatureImportedEvent {
  boardId: number;
  feature: FeatureSignalREvent;
  timestampUtc: string;
}

export interface FeatureRefreshedEvent {
  boardId: number;
  feature: FeatureSignalREvent;
  timestampUtc: string;
}

export interface FeaturesReorderedEvent {
  boardId: number;
  features: Array<{ featureId: number; newPriority: number }>;
  timestampUtc: string;
}

export interface FeatureDeletedEvent {
  boardId: number;
  featureId: number;
  timestampUtc: string;
}

export interface StoryRefreshedEvent extends UserStoryDto {
  boardId?: number;
}

export interface FeatureSignalREvent {
  id?: number;
  title: string;
  azureId?: string;
  priority?: number;
  valueArea?: string;
  children?: UserStoryDto[];
}

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private readonly ngZone = inject(NgZone);
  private connection: signalR.HubConnection | null = null;
  private connectedBoardId: number | null = null;
  private currentUserId = '';
  private currentUserName = '';

  private sequence = 0;
  private lastSentAt = 0;
  private lastSentX = Number.NaN;
  private lastSentY = Number.NaN;

  private readonly throttleMs = 66; // ~15 Hz
  private readonly minDelta = 3;

  private readonly cursorSubject = new Subject<CursorPresenceEvent>();
  private readonly presenceSubject = new BehaviorSubject<UserPresenceEvent[]>([]);
  private readonly storyMovedSubject = new Subject<StoryMovedEvent>();
  private readonly teamMemberAddedSubject = new Subject<TeamMemberAddedEvent>();
  private readonly teamMemberUpdatedSubject = new Subject<TeamMemberUpdatedEvent>();
  private readonly teamMemberDeletedSubject = new Subject<TeamMemberDeletedEvent>();
  private readonly capacityUpdatedSubject = new Subject<CapacityUpdatedEvent>();
  private readonly boardFinalizedSubject = new Subject<BoardFinalizedEvent>();
  private readonly boardRestoredSubject = new Subject<BoardRestoredEvent>();
  private readonly featureImportedSubject = new Subject<FeatureImportedEvent>();
  private readonly featureRefreshedSubject = new Subject<FeatureRefreshedEvent>();
  private readonly featuresReorderedSubject = new Subject<FeaturesReorderedEvent>();
  private readonly featureDeletedSubject = new Subject<FeatureDeletedEvent>();
  private readonly storyRefreshedSubject = new Subject<StoryRefreshedEvent>();

  readonly cursor$ = this.cursorSubject.asObservable();
  readonly presence$ = this.presenceSubject.asObservable();
  readonly storyMoved$ = this.storyMovedSubject.asObservable();
  readonly teamMemberAdded$ = this.teamMemberAddedSubject.asObservable();
  readonly teamMemberUpdated$ = this.teamMemberUpdatedSubject.asObservable();
  readonly teamMemberDeleted$ = this.teamMemberDeletedSubject.asObservable();
  readonly capacityUpdated$ = this.capacityUpdatedSubject.asObservable();
  readonly boardFinalized$ = this.boardFinalizedSubject.asObservable();
  readonly boardRestored$ = this.boardRestoredSubject.asObservable();
  readonly featureImported$ = this.featureImportedSubject.asObservable();
  readonly featureRefreshed$ = this.featureRefreshedSubject.asObservable();
  readonly featuresReordered$ = this.featuresReorderedSubject.asObservable();
  readonly featureDeleted$ = this.featureDeletedSubject.asObservable();
  readonly storyRefreshed$ = this.storyRefreshedSubject.asObservable();

  async connect(boardId: number, userId: string, userName: string): Promise<void> {
    if (
      this.connection?.state === signalR.HubConnectionState.Connected &&
      this.connectedBoardId === boardId &&
      this.currentUserId === userId
    ) {
      return;
    }

    await this.disconnect();

    this.currentUserId = userId;
    this.currentUserName = userName;
    this.connectedBoardId = boardId;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${RuntimeConfig.apiBaseUrl}/hub/planning`)
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
      .build();

    this.registerHandlers(this.connection);

    this.connection.onreconnected(async () => {
      if (!this.connection || this.connectedBoardId == null) {
        return;
      }

      await this.connection.invoke(
        'JoinBoard',
        this.connectedBoardId,
        this.currentUserId,
        this.currentUserName,
      );
    });

    await this.connection.start();
    await this.connection.invoke('JoinBoard', boardId, userId, userName);
  }

  async disconnect(): Promise<void> {
    if (!this.connection) {
      this.presenceSubject.next([]);
      return;
    }

    try {
      if (
        this.connection.state === signalR.HubConnectionState.Connected &&
        this.connectedBoardId != null
      ) {
        await this.connection.invoke('LeaveBoard', this.connectedBoardId, this.currentUserId);
      }
    } catch (error) {
      console.warn('Failed to leave board group gracefully:', error);
    }

    try {
      await this.connection.stop();
    } catch (error) {
      console.warn('Failed to stop SignalR connection:', error);
    }

    this.connection = null;
    this.connectedBoardId = null;
    this.sequence = 0;
    this.lastSentAt = 0;
    this.lastSentX = Number.NaN;
    this.lastSentY = Number.NaN;
    this.presenceSubject.next([]);
  }

  getConnectionId(): string | null {
    return this.connection?.connectionId ?? null;
  }

  async sendCursorUpdate(x: number, y: number): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    if (this.connectedBoardId == null) {
      return;
    }

    const now = Date.now();
    const deltaX = Number.isNaN(this.lastSentX)
      ? Number.POSITIVE_INFINITY
      : Math.abs(x - this.lastSentX);
    const deltaY = Number.isNaN(this.lastSentY)
      ? Number.POSITIVE_INFINITY
      : Math.abs(y - this.lastSentY);

    if (now - this.lastSentAt < this.throttleMs) {
      return;
    }

    if (deltaX < this.minDelta && deltaY < this.minDelta) {
      return;
    }

    this.lastSentAt = now;
    this.lastSentX = x;
    this.lastSentY = y;
    this.sequence += 1;

    await this.connection.invoke(
      'UpdateCursorPosition',
      this.connectedBoardId,
      this.currentUserId,
      Math.round(x),
      Math.round(y),
      this.sequence,
    );
  }

  private registerHandlers(connection: signalR.HubConnection): void {
    // SignalR callbacks can execute outside Angular; run inside NgZone to guarantee UI change detection.
    connection.on('PresenceSnapshot', (users: UserPresenceEvent[]) => {
      this.ngZone.run(() => {
        const dedupedUsers = this.buildPresenceMap(users);
        this.presenceSubject.next(dedupedUsers);
      });
    });

    connection.on('UserJoinedBoard', (user: UserPresenceEvent) => {
      this.ngZone.run(() => {
        const current = this.presenceSubject.getValue();
        this.presenceSubject.next(this.buildPresenceMap([...current, user]));
      });
    });

    connection.on('UserLeftBoard', (user: UserPresenceEvent) => {
      this.ngZone.run(() => {
        const current = this.presenceSubject.getValue();
        this.presenceSubject.next(current.filter((entry) => entry.userId !== user.userId));
      });
    });

    connection.on('CursorPresenceUpdated', (payload: CursorPresenceEvent) => {
      this.ngZone.run(() => {
        this.cursorSubject.next(payload);
      });
    });

    connection.on('StoryMoved', (payload: StoryMovedEvent) => {
      this.ngZone.run(() => {
        this.storyMovedSubject.next(payload);
      });
    });

    connection.on('TeamMemberAdded', (payload: TeamMemberAddedEvent) => {
      this.ngZone.run(() => {
        this.teamMemberAddedSubject.next(payload);
      });
    });

    connection.on('TeamMemberUpdated', (payload: TeamMemberUpdatedEvent) => {
      this.ngZone.run(() => {
        this.teamMemberUpdatedSubject.next(payload);
      });
    });

    connection.on('TeamMemberDeleted', (payload: TeamMemberDeletedEvent) => {
      this.ngZone.run(() => {
        this.teamMemberDeletedSubject.next(payload);
      });
    });

    connection.on('CapacityUpdated', (payload: CapacityUpdatedEvent) => {
      this.ngZone.run(() => {
        this.capacityUpdatedSubject.next(payload);
      });
    });

    connection.on('BoardFinalized', (payload: BoardFinalizedEvent) => {
      this.ngZone.run(() => {
        this.boardFinalizedSubject.next(payload);
      });
    });

    connection.on('BoardRestored', (payload: BoardRestoredEvent) => {
      this.ngZone.run(() => {
        this.boardRestoredSubject.next(payload);
      });
    });

    connection.on('FeatureImported', (payload: FeatureImportedEvent) => {
      this.ngZone.run(() => {
        this.featureImportedSubject.next(payload);
      });
    });

    connection.on('FeatureRefreshed', (payload: FeatureRefreshedEvent) => {
      this.ngZone.run(() => {
        this.featureRefreshedSubject.next(payload);
      });
    });

    connection.on('FeaturesReordered', (payload: FeaturesReorderedEvent) => {
      this.ngZone.run(() => {
        this.featuresReorderedSubject.next(payload);
      });
    });

    connection.on('FeatureDeleted', (payload: FeatureDeletedEvent) => {
      this.ngZone.run(() => {
        this.featureDeletedSubject.next(payload);
      });
    });

    connection.on('StoryRefreshed', (payload: StoryRefreshedEvent) => {
      this.ngZone.run(() => {
        this.storyRefreshedSubject.next(payload);
      });
    });
  }

  private buildPresenceMap(users: UserPresenceEvent[]): UserPresenceEvent[] {
    const map = new Map<string, UserPresenceEvent>();
    for (const user of users) {
      map.set(user.userId, user);
    }
    return [...map.values()];
  }
}
