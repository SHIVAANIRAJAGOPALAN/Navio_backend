namespace NavioBackend.DTOs
{
    public class CargoTypeCreateDto
    {
        public string Name { get; set; }
        public string Risk { get; set; } = "Low";
        public string Color { get; set; } = "green";
        public bool Active { get; set; } = true;
        public int? Count { get; set; }
    }
}

