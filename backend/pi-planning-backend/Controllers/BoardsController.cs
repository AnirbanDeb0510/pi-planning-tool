using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Models;
using PiPlanningBackend.DTOs;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/boards")]
    public class BoardsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public BoardsController(AppDbContext db) { _db = db; }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var board = await _db.Boards
                .Include(b => b.Sprints)
                .Include(b => b.Features)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null) return NotFound();

            var dto = new BoardDto
            {
                Id = board.Id,
                Name = board.Name,
                Organization = board.Organization,
                Project = board.Project,
                DevTestToggle = board.DevTestToggle
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BoardDto input)
        {
            var b = new Board
            {
                Name = input.Name,
                Organization = input.Organization,
                Project = input.Project,
                DevTestToggle = input.DevTestToggle
            };
            _db.Boards.Add(b);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = b.Id }, b);
        }
    }
}
