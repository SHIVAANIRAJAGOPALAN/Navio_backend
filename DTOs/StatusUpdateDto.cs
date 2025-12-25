namespace NavioBackend.DTOs
{
    public class StatusUpdateDto
{
    public string Status { get; set; } = null!;
    public string? Reason { get; set; }   // <-- OPTIONAL
}

}
