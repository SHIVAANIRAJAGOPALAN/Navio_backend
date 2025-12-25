namespace NavioBackend.DTOs
{
    public class TruckCreateDto
    {
        public string Number { get; set; }
        public double Length { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public int Capacity { get; set; }
        public string? CapacityUnit { get; set; } = "lbs";
        public string Status { get; set; }
         public string? BodyType { get; set; }
        public string? DutyClass { get; set; }
    }
}

