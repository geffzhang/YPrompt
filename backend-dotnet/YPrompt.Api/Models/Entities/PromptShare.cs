using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YPrompt.Api.Models.Entities;

[Table("prompt_shares")]
public class PromptShare
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("prompt_id")]
    public int PromptId { get; set; }

    [Required]
    [Column("share_code")]
    [MaxLength(32)]
    public string ShareCode { get; set; } = string.Empty;

    [Column("expire_time")]
    public DateTime? ExpireTime { get; set; }

    [Column("view_count")]
    public int ViewCount { get; set; } = 0;

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PromptId")]
    public virtual Prompt? Prompt { get; set; }
}
