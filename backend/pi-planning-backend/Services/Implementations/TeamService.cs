using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    public class TeamService(ITeamRepository teamRepository, IBoardRepository boardRepository, IValidationService validationService, ILogger<TeamService> logger, ICorrelationIdProvider correlationIdProvider, ITransactionService transactionService) : ITeamService
    {
        private readonly ITeamRepository _teamRepository = teamRepository;
        private readonly IBoardRepository _boardRepository = boardRepository;
        private readonly IValidationService _validationService = validationService;
        private readonly ILogger<TeamService> _logger = logger;
        private readonly ICorrelationIdProvider _correlationIdProvider = correlationIdProvider;
        private readonly ITransactionService _transactionService = transactionService;

        public async Task<List<TeamMemberDto>> GetTeamAsync(int boardId)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Get team members started | CorrelationId: {CorrelationId} | BoardId: {BoardId}",
                correlationId, boardId);

            await _validationService.ValidateBoardExists(boardId);
            List<TeamMember> members = await _teamRepository.GetTeamAsync(boardId);

            _logger.LogInformation(
                "Team members retrieved | CorrelationId: {CorrelationId} | BoardId: {BoardId} | MemberCount: {MemberCount}",
                correlationId, boardId, members.Count);

            return [.. members.Select(m => new TeamMemberDto
            {
                Id = m.Id,
                Name = m.Name,
                IsDev = m.IsDev,
                IsTest = m.IsTest
            })];
        }

        public async Task<TeamMemberResponseDto> AddTeamMemberAsync(int boardId, TeamMemberDto memberDto)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Team member addition started | CorrelationId: {CorrelationId} | BoardId: {BoardId} | MemberName: {MemberName} | IsDev: {IsDev} | IsTest: {IsTest}",
                correlationId, boardId, memberDto.Name, memberDto.IsDev, memberDto.IsTest);

            return await _transactionService.ExecuteInTransactionAsync(async () =>
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(memberDto.Name))
                    throw new ArgumentException("Team member name cannot be empty");

                if (!memberDto.IsDev && !memberDto.IsTest)
                    throw new ArgumentException("Team member must have at least one role (Dev or Test)");

                await _validationService.ValidateBoardExists(boardId);
                Board board = await _boardRepository.GetBoardWithSprintsAsync(boardId)
                    ?? throw new KeyNotFoundException("Board not found");

                // Guard: Prevent adding team members if board is finalized
                _validationService.ValidateBoardNotFinalized(board, "add team members");

                var member = new TeamMember
                {
                    BoardId = boardId,
                    Name = memberDto.Name,
                    IsDev = memberDto.IsDev,
                    IsTest = memberDto.IsTest
                };

                await _teamRepository.AddTeamMemberAsync(member);

                foreach (Sprint sprint in board.Sprints)
                {
                    var (capacityDev, capacityTest) = GetDefaultCapacities(board, sprint, member);

                    TeamMemberSprint tms = new()
                    {
                        SprintId = sprint.Id,
                        TeamMember = member,
                        CapacityDev = capacityDev,
                        CapacityTest = capacityTest
                    };

                    member.TeamMemberSprints.Add(tms);
                    await _teamRepository.AddTeamMemberSprintAsync(tms);
                }

                await _teamRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "Team member added successfully | CorrelationId: {CorrelationId} | MemberId: {MemberId} | Name: {Name} | SprintCount: {SprintCount}",
                    correlationId, member.Id, member.Name, member.TeamMemberSprints.Count);

                return MapTeamMemberResponse(member);
            });
        }

        public async Task<TeamMemberResponseDto?> UpdateTeamMemberAsync(int boardId, int memberId, TeamMemberDto memberDto)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Team member update started | CorrelationId: {CorrelationId} | BoardId: {BoardId} | MemberId: {MemberId} | NewName: {NewName}",
                correlationId, boardId, memberId, memberDto.Name);

            // Validate input
            if (string.IsNullOrWhiteSpace(memberDto.Name))
                throw new ArgumentException("Team member name cannot be empty");

            if (!memberDto.IsDev && !memberDto.IsTest)
                throw new ArgumentException("Team member must have at least one role (Dev or Test)");

            await _validationService.ValidateTeamMemberBelongsToBoard(memberId, boardId);
            var member = await _teamRepository.GetTeamMemberAsync(memberId)
                ?? throw new KeyNotFoundException($"Team member with ID {memberId} not found.");

            // Guard: Prevent updating team members if board is finalized
            await _validationService.ValidateBoardExists(boardId);
            Board board = await _boardRepository.GetBoardWithSprintsAsync(boardId)
                ?? throw new KeyNotFoundException("Board not found");
            _validationService.ValidateBoardNotFinalized(board, "update team members");

            // Check if role has changed
            bool roleChanged = member.IsDev != memberDto.IsDev || member.IsTest != memberDto.IsTest;

            member.Name = memberDto.Name;
            member.IsDev = memberDto.IsDev;
            member.IsTest = memberDto.IsTest;

            // If role changed, recalculate capacities for all sprints
            if (roleChanged)
            {
                foreach (var tms in member.TeamMemberSprints)
                {
                    var sprint = board.Sprints.FirstOrDefault(s => s.Id == tms.SprintId);
                    if (sprint != null)
                    {
                        var (capacityDev, capacityTest) = GetDefaultCapacities(board, sprint, member);
                        tms.CapacityDev = capacityDev;
                        tms.CapacityTest = capacityTest;
                    }
                }
            }

            await _teamRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Team member updated successfully | CorrelationId: {CorrelationId} | MemberId: {MemberId} | Name: {Name} | IsDev: {IsDev} | IsTest: {IsTest} | RoleChanged: {RoleChanged}",
                correlationId, member.Id, member.Name, member.IsDev, member.IsTest, roleChanged);

            return MapTeamMemberResponse(member);
        }

        public async Task<bool> DeleteTeamMemberAsync(int boardId, int memberId)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Team member deletion started | CorrelationId: {CorrelationId} | BoardId: {BoardId} | MemberId: {MemberId}",
                correlationId, boardId, memberId);

            await _validationService.ValidateTeamMemberBelongsToBoard(memberId, boardId);
            var member = await _teamRepository.GetTeamMemberAsync(memberId)
                ?? throw new KeyNotFoundException($"Team member with ID {memberId} not found.");

            // Guard: Prevent deleting team members if board is finalized
            await _validationService.ValidateBoardExists(boardId);
            Board board = await _boardRepository.GetBoardWithSprintsAsync(boardId)
                ?? throw new KeyNotFoundException("Board not found");
            _validationService.ValidateBoardNotFinalized(board, "delete team members");

            await _teamRepository.DeleteTeamMemberAsync(member);
            await _teamRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Team member deleted successfully | CorrelationId: {CorrelationId} | MemberId: {MemberId} | Name: {Name}",
                correlationId, memberId, member.Name);

            return true;
        }

        public async Task<TeamMemberSprint?> UpdateCapacityAsync(int boardId, int sprintId, int teamMemberId, UpdateTeamMemberCapacityDto dto)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Team member capacity update started | CorrelationId: {CorrelationId} | MemberId: {MemberId} | SprintId: {SprintId} | CapacityDev: {CapacityDev} | CapacityTest: {CapacityTest}",
                correlationId, teamMemberId, sprintId, dto.CapacityDev, dto.CapacityTest);

            await _validationService.ValidateTeamMemberBelongsToBoard(teamMemberId, boardId);
            await _validationService.ValidateSprintBelongsToBoard(sprintId, boardId);
            var tms = await _teamRepository.GetTeamMemberSprintAsync(sprintId, teamMemberId)
                ?? throw new KeyNotFoundException("Team member sprint mapping not found.");

            // Calculate max allowed capacity (working days in sprint)
            int maxCapacity = 0;
            if (tms.Sprint!.StartDate.HasValue && tms.Sprint.EndDate.HasValue)
            {
                var totalDays = (tms.Sprint.EndDate.Value - tms.Sprint.StartDate.Value).Days + 1;
                maxCapacity = (int)Math.Floor((totalDays / 7.0) * 5);
            }

            // Validate capacity doesn't exceed sprint duration
            _validationService.ValidateTeamMemberCapacity(dto.CapacityDev, maxCapacity);
            _validationService.ValidateTeamMemberCapacity(dto.CapacityTest, maxCapacity);

            if (tms.Sprint!.Board!.DevTestToggle)
            {
                tms.CapacityDev = tms.TeamMember.IsDev ? dto.CapacityDev : 0;
                tms.CapacityTest = tms.TeamMember.IsTest ? dto.CapacityTest : 0;
            }
            else
            {
                tms.CapacityDev = dto.CapacityDev;
                tms.CapacityTest = 0;
            }

            await _teamRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Team member capacity updated | CorrelationId: {CorrelationId} | MemberId: {MemberId} | SprintId: {SprintId} | FinalCapacityDev: {FinalCapacityDev} | FinalCapacityTest: {FinalCapacityTest}",
                correlationId, teamMemberId, sprintId, tms.CapacityDev, tms.CapacityTest);

            return tms;
        }

        private static (int capacityDev, int capacityTest) GetDefaultCapacities(Board board, Sprint sprint, TeamMember member)
        {
            int workingDays = 0;

            // Calculate working days from sprint dates (5 working days per 7 calendar days)
            if (sprint.StartDate.HasValue && sprint.EndDate.HasValue)
            {
                var totalDays = (sprint.EndDate.Value - sprint.StartDate.Value).Days + 1;
                workingDays = (int)Math.Floor((totalDays / 7.0) * 5);
            }

            if (board.DevTestToggle)
            {
                var dev = member.IsDev ? workingDays : 0;
                var test = member.IsTest ? workingDays : 0;
                return (dev, test);
            }

            return (workingDays, 0);
        }

        private static TeamMemberResponseDto MapTeamMemberResponse(TeamMember member)
        {
            return new TeamMemberResponseDto
            {
                Id = member.Id,
                Name = member.Name,
                IsDev = member.IsDev,
                IsTest = member.IsTest,
                SprintCapacities = [.. member.TeamMemberSprints.Select(tms => new TeamMemberSprintDto
                {
                    SprintId = tms.SprintId,
                    CapacityDev = tms.CapacityDev,
                    CapacityTest = tms.CapacityTest
                })]
            };
        }
    }
}
