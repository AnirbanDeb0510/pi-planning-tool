namespace PiPlanningBackend.DTOs
{
    public class FeatureDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? AzureId { get; set; }
        public IEnumerable<UserStoryDto>? Children { get; set; }
    }
}
