namespace PiPlanningBackend.Models
{
    public class Feature
    {
        public int Id { get; set; }
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        public string? AzureId { get; set; }
        public string Title { get; set; } = "";
        public int? Priority { get; set; }
        public string? ValueArea { get; set; }
        public bool IsFinalized { get; set; }

        public ICollection<UserStory> UserStories { get; set; } = new List<UserStory>();
    }
}
