namespace PiPlanningBackend.DTOs.SignalR
{
    /// <summary>
    /// Unified DTO for board lock state changes via SignalR
    /// </summary>
    public class BoardLockStateChangedDto
    {
        public int BoardId { get; set; }
        public bool IsLocked { get; set; }  // true = locked, false = unlocked
        public DateTime TimestampUtc { get; set; }
    }
}
