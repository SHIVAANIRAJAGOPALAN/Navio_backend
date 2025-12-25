namespace NavioBackend.DTOs
{
    public class BulkRestrictionRequest
    {
        public List<long> RoadIds { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public DateTime DateTime { get; set; }
    }
}
