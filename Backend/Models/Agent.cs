using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeimdallBackend.Models
{
    public class Agent
    {
        public int UserId { get; set; }

        public int AgentId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string AgentName { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}