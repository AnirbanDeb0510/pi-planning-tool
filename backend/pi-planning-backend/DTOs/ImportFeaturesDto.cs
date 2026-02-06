namespace PiPlanningBackend.DTOs
{
    public class ImportFeaturesDto
    {
        public int BoardId { get; set; }
        public List<int> AzureFeatureIds { get; set; } = new();
        // optional: PAT could be supplied here or via a every-request query param
        public string? Pat { get; set; }
    }
}
