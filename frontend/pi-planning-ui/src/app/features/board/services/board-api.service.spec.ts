import { TestBed } from '@angular/core/testing';
import { HttpParams } from '@angular/common/http';
import { of } from 'rxjs';
import {
  AzureApiService,
  BoardApiService,
  FeatureApiService,
  StoryApiService,
  TeamApiService,
} from './board-api.service';
import { HttpClientService } from '../../../core/services/http-client.service';
import {
  BOARD_API,
  FEATURE_API,
  STORY_API,
  TEAM_API,
  AZURE_API,
} from '../../../core/constants/api-endpoints.constants';

describe('BoardApiService family', () => {
  let httpClientSpy: jasmine.SpyObj<HttpClientService>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj<HttpClientService>('HttpClientService', [
      'get',
      'post',
      'patch',
      'put',
      'delete',
    ]);

    httpClientSpy.get.and.returnValue(of([]));
    httpClientSpy.post.and.returnValue(of({}));
    httpClientSpy.patch.and.returnValue(of({}));
    httpClientSpy.put.and.returnValue(of({}));
    httpClientSpy.delete.and.returnValue(of({}));

    TestBed.configureTestingModule({
      providers: [{ provide: HttpClientService, useValue: httpClientSpy }],
    });
  });

  it('BoardApiService.getBoard calls the expected endpoint', () => {
    const service = TestBed.inject(BoardApiService);

    service.getBoard(7).subscribe();

    expect(httpClientSpy.get).toHaveBeenCalledWith(BOARD_API.GET_BOARD(7));
  });

  it('BoardApiService.searchBoards includes only provided filters as query params', () => {
    const service = TestBed.inject(BoardApiService);

    service
      .searchBoards({
        organization: 'Org',
        project: 'Project',
        isLocked: true,
      })
      .subscribe();

    const [, options] = httpClientSpy.get.calls.mostRecent().args;
    const params = options?.params as HttpParams;

    expect(httpClientSpy.get.calls.mostRecent().args[0]).toBe(BOARD_API.SEARCH_BOARDS);
    expect(params.get('organization')).toBe('Org');
    expect(params.get('project')).toBe('Project');
    expect(params.get('isLocked')).toBe('true');
    expect(params.has('search')).toBeFalse();
    expect(params.has('isFinalized')).toBeFalse();
  });

  it('BoardApiService.getBoardList converts boolean filters to strings', () => {
    const service = TestBed.inject(BoardApiService);

    service
      .getBoardList({
        search: 'PI',
        organization: 'Org',
        project: 'Proj',
        isLocked: false,
        isFinalized: true,
      })
      .subscribe();

    const [, options] = httpClientSpy.get.calls.mostRecent().args;
    const params = options?.params as HttpParams;

    expect(httpClientSpy.get.calls.mostRecent().args[0]).toBe(BOARD_API.GET_BOARD_LIST);
    expect(params.get('search')).toBe('PI');
    expect(params.get('organization')).toBe('Org');
    expect(params.get('project')).toBe('Proj');
    expect(params.get('isLocked')).toBe('false');
    expect(params.get('isFinalized')).toBe('true');
  });

  it('FeatureApiService.refreshFeature sends query params and empty patch body', () => {
    const service = TestBed.inject(FeatureApiService);

    service.refreshFeature(3, 10, 'Org', 'Proj', 'secret').subscribe();

    expect(httpClientSpy.patch).toHaveBeenCalledWith(
      FEATURE_API.REFRESH(3, 10),
      {},
      { params: { organization: 'Org', project: 'Proj', pat: 'secret' } },
    );
  });

  it('StoryApiService.moveStory sends target sprint in patch body', () => {
    const service = TestBed.inject(StoryApiService);

    service.moveStory(2, 9, 5).subscribe();

    expect(httpClientSpy.patch).toHaveBeenCalledWith(STORY_API.MOVE(2, 9), {
      targetSprintId: 5,
    });
  });

  it('TeamApiService.updateCapacity sends dev and test capacity payload', () => {
    const service = TestBed.inject(TeamApiService);

    service.updateCapacity(4, 6, 8, 11, 7).subscribe();

    expect(httpClientSpy.patch).toHaveBeenCalledWith(TEAM_API.UPDATE_CAPACITY(4, 6, 8), {
      capacityDev: 11,
      capacityTest: 7,
    });
  });

  it('AzureApiService.getFeatureWithChildren sends pat as a query param', () => {
    const service = TestBed.inject(AzureApiService);

    service.getFeatureWithChildren('Org', 'Proj', '123', 'token').subscribe();

    expect(httpClientSpy.get).toHaveBeenCalledWith(AZURE_API.GET_FEATURE('Org', 'Proj', '123'), {
      params: { pat: 'token' },
    });
  });

  it('AzureApiService.getFeatureWithChildrenForBoard uses board-scoped endpoint', () => {
    const service = TestBed.inject(AzureApiService);

    service.getFeatureWithChildrenForBoard(22, '987', 'token').subscribe();

    expect(httpClientSpy.get).toHaveBeenCalledWith(AZURE_API.GET_FEATURE_FOR_BOARD(22, '987'), {
      params: { pat: 'token' },
    });
  });
});
