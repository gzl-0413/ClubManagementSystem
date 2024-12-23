#nullable disable warnings

namespace ClubManagementSystem.Models;

public class AnnouncementViewModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Photo { get; set; }
    public DateTime DateTime { get; set; }
    public string Status { get; set; }
    public string LikeUsers { get; set; }
    public int LikeCount { get; set; }  // Number of likes
    public string AdminEmail { get; set; }
    public string AdminName { get; set; }
    public string AdminProfilePicture { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}

public class ToggleStatusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}