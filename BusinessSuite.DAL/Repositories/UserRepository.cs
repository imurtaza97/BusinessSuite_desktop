using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.DAL.Repositories;

public class UserRepository
{
    private readonly AppDbContext _dbFactory;

    public UserRepository(AppDbContext db)
    {
        _dbFactory = db;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        string normalizedUsername = username.ToLower().Trim();
        var user = await _dbFactory.Users.FirstOrDefaultAsync(u => u.UserName == normalizedUsername);
        if (user == null) return null;

        // In a real app, use proper password hashing comparison
        // For this fix, we'll assume BCrypt is used if the hash starts with $2
        bool isValid = false;
        if (user.PasswordHash.StartsWith("$2"))
        {
            isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        else
        {
            isValid = user.PasswordHash == password;
        }

        return isValid ? user : null;
    }
}
