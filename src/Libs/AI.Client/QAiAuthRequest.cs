using Api.AspNetCore.Models.Secure;

namespace AI.Client
{
    public class QAiAuthRequest : JwtTokenRequest
    {
        public string? PinCode { get; set; }
    }
}
