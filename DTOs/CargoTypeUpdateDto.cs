namespace NavioBackend.DTOs
{
    public class CargoTypeUpdateDto
    {
        public string? Name { get; set; }
        public string? Risk { get; set; }
        public string? Color { get; set; }
        public bool? Active { get; set; }
        public int? Count { get; set; }
    }
}
