import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly STORAGE_KEY = 'pi-planning-user-name';
  private readonly MIN_NAME_LENGTH = 2;
  private _name: string = '';

  constructor() {
    // Load name from sessionStorage on service initialization
    this._name = (sessionStorage.getItem(this.STORAGE_KEY) || '').trim();
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

  clearName(): void {
    this._name = '';
    sessionStorage.removeItem(this.STORAGE_KEY);
  }
}
