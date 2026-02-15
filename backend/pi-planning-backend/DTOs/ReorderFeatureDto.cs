namespace PiPlanningBackend.DTOs
{
    public class ReorderFeatureDto
    {
        public List<ReorderFeatureItemDto> Features { get; set; } = new();
    }

    public class ReorderFeatureItemDto
    {
        public int FeatureId { get; set; }
        public int NewPriority { get; set; }
    }
}
