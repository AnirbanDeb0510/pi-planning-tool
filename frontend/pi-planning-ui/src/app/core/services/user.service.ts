import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly STORAGE_KEY = 'pi-planning-user-name';
  private readonly USER_ID_STORAGE_KEY = 'pi-planning-user-id';
  private readonly MIN_NAME_LENGTH = 2;
  private _name: string = '';
  private _userId: string = '';

  constructor() {
    // Load name from sessionStorage on service initialization
    this._name = (sessionStorage.getItem(this.STORAGE_KEY) || '').trim();
    this._userId = (sessionStorage.getItem(this.USER_ID_STORAGE_KEY) || '').trim();
  }

  setName(name: string): void {
    const normalizedName = name.trim();
    this._name = normalizedName;
    sessionStorage.setItem(this.STORAGE_KEY, normalizedName);
  }

  getName(): string {
    // Lazy load from sessionStorage if not in memory
    if (!this._name) {
      this._name = (sessionStorage.getItem(this.STORAGE_KEY) || '').trim();
    }
    return this._name;
  }

  hasName(): boolean {
    return this.getName().length >= this.MIN_NAME_LENGTH;
  }

  getOrCreateUserId(): string {
    if (this._userId) {
      return this._userId;
    }

    const storedUserId = (sessionStorage.getItem(this.USER_ID_STORAGE_KEY) || '').trim();
    if (storedUserId) {
      this._userId = storedUserId;
      return this._userId;
    }

    this._userId = this.generateUserId();
    sessionStorage.setItem(this.USER_ID_STORAGE_KEY, this._userId);
    return this._userId;
  }

  clearName(): void {
    this._name = '';
    sessionStorage.removeItem(this.STORAGE_KEY);
  }

  private generateUserId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }

    return `uid-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
  }
}
