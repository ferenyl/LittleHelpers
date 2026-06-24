import { Injectable, inject } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

export interface ChildRealtimeUpdate {
  childId: number;
  changeType: string;
  changedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private auth = inject(AuthService);
  private connection: HubConnection | null = null;
  private desiredChildIds = new Set<number>();
  private joinedChildIds = new Set<number>();
  private syncChain = Promise.resolve();
  private readonly updatesSubject = new Subject<ChildRealtimeUpdate>();

  readonly childUpdates$ = this.updatesSubject.asObservable();

  setTrackedChildren(childIds: number[]) {
    this.desiredChildIds = new Set(childIds);
    this.queueGroupSync();
  }

  trackChild(childId: number) {
    this.desiredChildIds.add(childId);
    this.queueGroupSync();
  }

  untrackChild(childId: number) {
    this.desiredChildIds.delete(childId);
    this.queueGroupSync();
  }

  private queueGroupSync() {
    this.syncChain = this.syncChain
      .then(() => this.syncGroups())
      .catch(error => {
        console.error('SignalR group sync failed.', error);
      });
  }

  private async syncGroups() {
    const connection = await this.ensureConnection();
    if (!connection) {
      return;
    }

    for (const childId of [...this.joinedChildIds]) {
      if (this.desiredChildIds.has(childId)) {
        continue;
      }

      await connection.invoke('LeaveChildGroup', childId);
      this.joinedChildIds.delete(childId);
    }

    for (const childId of this.desiredChildIds) {
      if (this.joinedChildIds.has(childId)) {
        continue;
      }

      await connection.invoke('JoinChildGroup', childId);
      this.joinedChildIds.add(childId);
    }
  }

  private async ensureConnection() {
    if (!this.auth.token()) {
      return null;
    }

    if (!this.connection) {
      this.connection = new HubConnectionBuilder()
        .withUrl(`${environment.apiUrl}/realtime/updates`, {
          accessTokenFactory: () => this.auth.token() ?? '',
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

      this.connection.on('childUpdated', payload => {
        this.updatesSubject.next(payload as ChildRealtimeUpdate);
      });

      this.connection.onreconnected(() => this.queueGroupSync());
      this.connection.onclose(error => {
        this.joinedChildIds.clear();
        if (error) {
          console.error('SignalR connection closed.', error);
        }
      });
    }

    if (this.connection.state === HubConnectionState.Disconnected) {
      await this.connection.start();
    }

    return this.connection;
  }
}
