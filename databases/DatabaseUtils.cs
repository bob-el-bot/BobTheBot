using System;
using DotNetEnv;
using Npgsql;

namespace Bob.Database;

public static class DatabaseUtils
{
    public static string GetNpgsqlConnectionString()
    {
        Env.Load();
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var databaseUri = new Uri(databaseUrl);
        var userInfo = databaseUri.UserInfo.Split(':');
        return new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = databaseUri.AbsolutePath.TrimStart('/')
        }.ToString();
    }
}