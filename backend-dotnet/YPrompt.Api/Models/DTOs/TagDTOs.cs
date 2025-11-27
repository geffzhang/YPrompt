namespace YPrompt.Api.Models.DTOs;

// Tag DTOs

public class TagInfo
{
    public int Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public int UseCount { get; set; }
    public string CreateTime { get; set; } = string.Empty;
}

public class CreateTagRequest
{
    public string TagName { get; set; } = string.Empty;
}
