import * as signalR from '@microsoft/signalr';
import type {
  StepStartedEvent,
  StepCompletedEvent,
  StepFailedEvent,
  RunCompletedEvent
} from '../types';

const HUB_URL = import.meta.env.VITE_HUB_URL || 'http://localhost:5266/hubs/runs';

export class RunsHubClient {
  private connection: signalR.HubConnection;
  private handlers = new Map<string, Set<(...args: any[]) => void>>();

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    this.connection.on('StepStarted', (event: StepStartedEvent) => {
      this.emit('StepStarted', event);
    });

    this.connection.on('StepCompleted', (event: StepCompletedEvent) => {
      this.emit('StepCompleted', event);
    });

    this.connection.on('StepFailed', (event: StepFailedEvent) => {
      this.emit('StepFailed', event);
    });

    this.connection.on('RunCompleted', (event: RunCompletedEvent) => {
      this.emit('RunCompleted', event);
    });
  }

  async start() {
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  async stop() {
    if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
      await this.connection.stop();
    }
  }

  async joinRun(runId: string) {
    await this.connection.invoke('JoinRun', runId);
  }

  async leaveRun(runId: string) {
    await this.connection.invoke('LeaveRun', runId);
  }

  on<T = any>(event: string, handler: (data: T) => void) {
    if (!this.handlers.has(event)) {
      this.handlers.set(event, new Set());
    }
    this.handlers.get(event)!.add(handler);
  }

  off<T = any>(event: string, handler: (data: T) => void) {
    const handlers = this.handlers.get(event);
    if (handlers) {
      handlers.delete(handler);
    }
  }

  private emit(event: string, data: any) {
    const handlers = this.handlers.get(event);
    if (handlers) {
      handlers.forEach(handler => handler(data));
    }
  }

  get state() {
    return this.connection.state;
  }
}

export const runsHub = new RunsHubClient();
