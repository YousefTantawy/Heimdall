namespace HeimdallBackend.DTOs
{
    public class AgentPayloadDtos
    {
        public required string AgentId { get; set; }
        public required List<string> Flows { get; set; }
    }
}
