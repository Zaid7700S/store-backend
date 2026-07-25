namespace store.Dto
{
    public class TokenApiDto
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }
}
