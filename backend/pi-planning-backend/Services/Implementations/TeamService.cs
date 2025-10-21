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

        public async Task AddOrUpdateTeamAsync(int boardId, List<TeamMemberDto> membersDto)
        {
            Board board = await _boardRepository.GetBoardWithSprintsAsync(boardId) ?? throw new Exception("Board not found");
            List<TeamMember> existingMembers = await _teamRepository.GetTeamAsync(boardId);

            foreach (TeamMemberDto dto in membersDto)
            {
                TeamMember member;

                if (dto.Id > 0)
                {
                    // update existing member
                    member = existingMembers.FirstOrDefault(m => m.Id == dto.Id)
                             ?? throw new Exception($"Team member with Id {dto.Id} not found");
                    member.Name = dto.Name;
                    member.IsDev = dto.IsDev;
                    member.IsTest = dto.IsTest;
                }
                else
                {
                    // add new member
                    member = new TeamMember
                    {
                        Name = dto.Name,
                        IsDev = dto.IsDev,
                        IsTest = dto.IsTest
                    };
                    await _teamRepository.AddAsync(boardId, member);
                }

                // assign capacities for each sprint
                foreach (Sprint sprint in board.Sprints)
                {
                    TeamMemberSprint? existingTms = member.TeamMemberSprints
                        .FirstOrDefault(tms => tms.SprintId == sprint.Id);

                    if (existingTms == null)
                    {
                        double capacityDev = 0;
                        double capacityTest = 0;

                        // Determine capacity based on board.DevTestToggle and member role
                        if (board.DevTestToggle)
                        {
                            if (member.IsDev) capacityDev = board.SprintDuration; // or working days
                            if (member.IsTest) capacityTest = board.SprintDuration;
                        }
                        else
                        {
                            // Use DevCapacity for all if toggle is false
                            capacityDev = board.SprintDuration;
                        }

                        TeamMemberSprint tms = new()
                        {
                            SprintId = sprint.Id,
                            TeamMemberId = member.Id,
                            CapacityDev = capacityDev,
                            CapacityTest = capacityTest
                        };

                        member.TeamMemberSprints.Add(tms);
                    }
                    else
                    {
                        // Optionally, update existing capacities if needed
                        if (board.DevTestToggle)
                        {
                            existingTms.CapacityDev = member.IsDev ? board.SprintDuration : 0;
                            existingTms.CapacityTest = member.IsTest ? board.SprintDuration : 0;
                        }
                        else
                        {
                            existingTms.CapacityDev = board.SprintDuration;
                            existingTms.CapacityTest = 0;
                        }
                    }
                }
            }

            await _teamRepository.SaveChangesAsync();
        }

        public async Task<TeamMemberSprint?> UpdateCapacityAsync(int sprintId, int teamMemberId, double capacity)
        {
            var tms = await _teamRepository.GetTeamMemberSprintAsync(sprintId, teamMemberId);
            if (tms == null) return null;

            if (tms.Sprint!.Board!.DevTestToggle)
            {
                // Assign based on member type
                if (tms.TeamMember.IsDev) tms.CapacityDev = capacity;
                if (tms.TeamMember.IsTest) tms.CapacityTest = capacity;
            }
            else
            {
                tms.CapacityDev = capacity;
                tms.CapacityTest = 0;
            }

            await _teamRepository.SaveChangesAsync();
            return tms;
        }
    }
}
