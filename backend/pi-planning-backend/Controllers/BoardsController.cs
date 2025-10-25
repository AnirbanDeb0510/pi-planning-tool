using Microsoft.AspNetCore.Mvc;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BoardController : ControllerBase
    {
        private readonly IBoardService _boardService;

        public BoardController(IBoardService boardService)
        {
            _boardService = boardService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBoard(BoardCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var board = await _boardService.CreateBoardAsync(dto);
            return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, board);
        }

        [HttpGet("{id}")]
        public IActionResult GetBoard(int id)
        {
            return Ok($"Placeholder for board {id}");
        }
    }
}
