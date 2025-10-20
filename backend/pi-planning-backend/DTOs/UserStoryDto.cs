namespace PiPlanningBackend.DTOs
{
    public class UserStoryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? AzureId { get; set; }
        public double? StoryPoints { get; set; }
        public double? DevStoryPoints { get; set; }
        public double? TestStoryPoints { get; set; }
        public int? CurrentSprintId { get; set; }
        public bool IsMoved { get; set; }
    }
}
