using System.ComponentModel.DataAnnotations;


namespace Identity.Database;

public class Users
{
    [Key]
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Login { get; set; } = default!;
    [Required]
    public string PasswordHash { get; set; } = default!;

}

public record UserDto(int Id, string Login, int AccessLevel);