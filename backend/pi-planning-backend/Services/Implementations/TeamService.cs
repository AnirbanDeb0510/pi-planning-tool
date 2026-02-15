using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IBoardRepository _boardRepository;

        public TeamService(ITeamRepository teamRepository, IBoardRepository boardRepository)
        {
            _teamRepository = teamRepository;
            _boardRepository = boardRepository;
        }

        public async Task<List<TeamMemberDto>> GetTeamAsync(int boardId)
        {
            List<TeamMember> members = await _teamRepository.GetTeamAsync(boardId);
            return members.Select(m => new TeamMemberDto
            {
                Id = m.Id,
                Name = m.Name,
                IsDev = m.IsDev,
                IsTest = m.IsTest
            }).ToList();
        }

        public async Task<TeamMemberResponseDto> AddTeamMemberAsync(int boardId, TeamMemberDto memberDto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(memberDto.Name))
                throw new ArgumentException("Team member name cannot be empty");

            if (!memberDto.IsDev && !memberDto.IsTest)
                throw new ArgumentException("Team member must have at least one role (Dev or Test)");

            Board board = await _boardRepository.GetBoardWithSprintsAsync(boardId)
                ?? throw new Exception("Board not found");

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

            return MapTeamMemberResponse(member);
        }

        public async Task<TeamMemberResponseDto?> UpdateTeamMemberAsync(int boardId, int memberId, TeamMemberDto memberDto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(memberDto.Name))
                throw new ArgumentException("Team member name cannot be empty");

            if (!memberDto.IsDev && !memberDto.IsTest)
                throw new ArgumentException("Team member must have at least one role (Dev or Test)");

            var member = await _teamRepository.GetTeamMemberAsync(memberId);
            if (member == null || member.BoardId != boardId) return null;

            // Check if role has changed
            bool roleChanged = member.IsDev != memberDto.IsDev || member.IsTest != memberDto.IsTest;

            member.Name = memberDto.Name;
            member.IsDev = memberDto.IsDev;
            member.IsTest = memberDto.IsTest;

            // If role changed, recalculate capacities for all sprints
            if (roleChanged)
            {
                Board board = await _boardRepository.GetBoardWithSprintsAsync(boardId)
                    ?? throw new Exception("Board not found");

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

            return MapTeamMemberResponse(member);
        }

        public async Task<bool> DeleteTeamMemberAsync(int boardId, int memberId)
        {
            var member = await _teamRepository.GetTeamMemberAsync(memberId);
            if (member == null || member.BoardId != boardId) return false;

            await _teamRepository.DeleteTeamMemberAsync(member);
            await _teamRepository.SaveChangesAsync();
            return true;
        }

        public async Task<TeamMemberSprint?> UpdateCapacityAsync(int boardId, int sprintId, int teamMemberId, UpdateTeamMemberCapacityDto dto)
        {
            var tms = await _teamRepository.GetTeamMemberSprintAsync(sprintId, teamMemberId);
            if (tms == null) return null;
            if (tms.Sprint?.BoardId != boardId) return null;

            // Calculate max allowed capacity (working days in sprint)
            double maxCapacity = 0;
            if (tms.Sprint!.StartDate.HasValue && tms.Sprint.EndDate.HasValue)
            {
                var totalDays = (tms.Sprint.EndDate.Value - tms.Sprint.StartDate.Value).Days + 1;
                maxCapacity = Math.Floor((totalDays / 7.0) * 5);
            }

            // Validate capacity doesn't exceed sprint duration
            if (dto.CapacityDev > maxCapacity || dto.CapacityTest > maxCapacity)
                throw new ArgumentException($"Capacity cannot exceed sprint duration ({maxCapacity} working days)");

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
                SprintCapacities = member.TeamMemberSprints.Select(tms => new TeamMemberSprintDto
                {
                    SprintId = tms.SprintId,
                    CapacityDev = tms.CapacityDev,
                    CapacityTest = tms.CapacityTest
                }).ToList()
            };
        }
    }
}
