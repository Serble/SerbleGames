using System.ComponentModel.DataAnnotations;

namespace SerbleGames.Backend.Schemas.Db;

public class UserAchievement {
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = null!;
    public string AchievementId { get; set; } = null!;
    public DateTime DateEarned { get; set; } = DateTime.UtcNow;
}
