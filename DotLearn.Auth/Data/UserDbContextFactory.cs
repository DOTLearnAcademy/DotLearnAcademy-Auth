using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotLearn.Auth.Data;

public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        var connStr = "Server=dotlearn-db.c7ge68ueyfep.ap-southeast-2.rds.amazonaws.com,1433;Database=AuthDb;User Id=admin;Password=DOTLearn@123;TrustServerCertificate=True";
        optionsBuilder.UseSqlServer(connStr);
        return new UserDbContext(optionsBuilder.Options);
    }
}
