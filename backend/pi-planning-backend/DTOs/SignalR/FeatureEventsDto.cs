namespace PiPlanningBackend.DTOs.SignalR
{
    public class FeatureImportedDto
    {
        public int BoardId { get; set; }
        public FeatureDto Feature { get; set; } = new();
        public DateTime TimestampUtc { get; set; }
    }

    public class FeatureRefreshedDto
    {
        public int BoardId { get; set; }
        public FeatureDto Feature { get; set; } = new();
        public DateTime TimestampUtc { get; set; }
    }

    public class FeaturesReorderedDto
    {
        public int BoardId { get; set; }
        public List<ReorderFeatureItemDto> Features { get; set; } = [];
        public DateTime TimestampUtc { get; set; }
    }

    public class FeatureDeletedDto
    {
        public int BoardId { get; set; }
        public int FeatureId { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}