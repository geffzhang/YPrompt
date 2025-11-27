using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YPrompt.Api.Models.Entities;

[Table("prompt_tags")]
public class PromptTag
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("tag_name")]
    [MaxLength(50)]
    public string TagName { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("use_count")]
    public int UseCount { get; set; } = 0;

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
