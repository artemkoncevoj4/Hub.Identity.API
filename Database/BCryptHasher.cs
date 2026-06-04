namespace Identity.Database;
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public class BCryptHasher : IPasswordHasher
{
    public string Hash(string password) => 
        BCrypt.Net.BCrypt.HashPassword(password, 11);

    public bool Verify(string password, string passwordHash) => 
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}