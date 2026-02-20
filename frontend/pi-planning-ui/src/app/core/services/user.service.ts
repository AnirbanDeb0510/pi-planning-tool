import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UserService {
  private _name: string = '';

  setName(name: string) {
    this._name = name;
  }

  getName(): string {
    return this._name;
  }
}
