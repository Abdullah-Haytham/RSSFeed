

using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery();
builder.Services.AddSingleton<IDbConnection>(_ => new SqliteConnection("Data Source=wwwroot/linksdb.db"));

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapFallbackToFile("/index.html");


app.MapGet("/shortcuts", async (IDbConnection db) =>
{
    var sql = "SELECT name FROM feeds WHERE userId=1"; //user id will need to be dynamic (use cookies 8aleban)
    var names = await db.QueryAsync(sql);

    StringBuilder shortcuts = new StringBuilder();
    foreach (var name in names)
    {
        shortcuts.Append(@$"<a class=""shortcut"">{name.name}</a>");
    }
    return Results.Content(shortcuts.ToString());
});

app.Run();
