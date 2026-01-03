namespace PiPlanningBackend.DTOs
{
    public class FeatureDto
    {
        public int? Id { get; set; }
        public string Title { get; set; } = "";
        public string? AzureId { get; set; }
        public int? Priority { get; set; }
        public string? ValueArea { get; set; }
        public List<UserStoryDto>? Children { get; set; }
    }
}
