import { UserService } from './user.service';

describe('UserService', () => {
  let service: UserService;

  beforeEach(() => {
    sessionStorage.clear();
    service = new UserService();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('setName stores trimmed name and getName returns it', () => {
    service.setName('  Alice  ');

    expect(service.getName()).toBe('Alice');
    expect(sessionStorage.getItem('pi-planning-user-name')).toBe('Alice');
  });

  it('hasName returns false when name is shorter than minimum length', () => {
    service.setName('A');

    expect(service.hasName()).toBeFalse();
  });

  it('hasName returns true when name has minimum length', () => {
    service.setName('Al');

    expect(service.hasName()).toBeTrue();
  });

  it('getName lazy-loads from sessionStorage when in-memory value is empty', () => {
    // Create service first (empty storage → _name = '')
    const freshService = new UserService();
    // Then populate storage to simulate a value written externally after construction
    sessionStorage.setItem('pi-planning-user-name', '  Bob  ');

    expect(freshService.getName()).toBe('Bob');
  });

  it('getOrCreateUserId returns existing in-memory id', () => {
    const first = service.getOrCreateUserId();
    const second = service.getOrCreateUserId();

    expect(second).toBe(first);
  });

  it('getOrCreateUserId uses stored id from sessionStorage when available', () => {
    sessionStorage.setItem('pi-planning-user-id', 'stored-user-id');
    const freshService = new UserService();

    expect(freshService.getOrCreateUserId()).toBe('stored-user-id');
  });

  it('getOrCreateUserId generates and stores new id when missing', () => {
    const id = service.getOrCreateUserId();

    expect(id).toBeTruthy();
    expect(sessionStorage.getItem('pi-planning-user-id')).toBe(id);
  });

  it('clearName removes name from sessionStorage and memory', () => {
    service.setName('Charlie');

    service.clearName();

    expect(service.getName()).toBe('');
    expect(sessionStorage.getItem('pi-planning-user-name')).toBeNull();
  });
});
