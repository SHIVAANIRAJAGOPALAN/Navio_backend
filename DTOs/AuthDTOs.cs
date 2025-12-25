namespace NavioBackend.DTOs
{
    public class LoginRequest
    {
        public string Identifier { get; set; }   // email or driver/admin ID
        public string Password { get; set; }
        public string Role { get; set; }         // "Admin" | "FleetManager" | "Driver"
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
    }
}

