

using Dapper;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Data;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery();
builder.Services.AddSingleton<IDbConnection>(_ => new SqliteConnection("Data Source=wwwroot/linksdb.db"));

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapFallbackToFile("/index.html");

var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Expires = DateTime.UtcNow.AddDays(1),
    SameSite = SameSiteMode.Strict,
    Path = "/"
};

app.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    string html = $"""<input name = "{token.FormFieldName}" type = "hidden" value = "{token.RequestToken}"/>""";
    return Results.Content(html, "text/html");
});

app.MapPost("/login", async (HttpContext context, IAntiforgery antiforgery, IDbConnection db, [FromForm] string email, [FromForm] string password) =>
{
    await antiforgery.ValidateRequestAsync(context);

    string sql = "SELECT COUNT(*) AS UserCount FROM users WHERE email = @Email AND password = @Password";
    var result = await db.QueryFirstOrDefaultAsync<int>(sql, new { Email = email, Password = password });

    if(result == 1)
    {
        sql = "SELECT userId FROM users WHERE email = @Email AND password = @Password";
        var userId = await db.QueryFirstOrDefaultAsync<int>(sql, new { Email = email, Password = password });
        context.Response.Cookies.Append("UserId", userId.ToString(), cookieOptions);
        return Results.Ok("User Logged in successfully");
    }

    return Results.Content("Ay 7aga for now", "text/html");
});

app.MapGet("/shortcuts", async (IDbConnection db) =>
{
    var sql = "SELECT name FROM feeds WHERE userId=1"; //user id will need to be dynamic (use cookies 8aleban)
    var names = await db.QueryAsync(sql);

    StringBuilder shortcuts = new StringBuilder();
    foreach (var name in names)
    {
        shortcuts.Append(@$"<a href=""#{name.name}"" class=""shortcut"">{name.name}</a>");
    }
    return Results.Content(shortcuts.ToString());
});

app.MapGet("/select-options", async (IDbConnection db) =>
{
    var sql = "SELECT name FROM feeds WHERE userId=1"; //user id will need to be dynamic (use cookies 8aleban)
    var names = await db.QueryAsync(sql);

    StringBuilder shortcuts = new StringBuilder();
    shortcuts.Append(@$"<option selected>Select a feed to delete</option>");
    foreach (var name in names)
    {
        shortcuts.Append(@$"<option value={name.name}>{name.name}</option>");
    }
    return Results.Content(shortcuts.ToString());
});

app.MapGet("/feeds", async (IDbConnection db, HttpContext context) =>
{
    var sql = "SELECT name, url FROM feeds WHERE userID=1"; //user id will need to be dynamic (use cookies 8aleban)
    var links = await db.QueryAsync(sql);

    StringBuilder feedBuilder = new StringBuilder();
    int count = 1;
    foreach (var link in links)
    {
        XmlReader reader = XmlReader.Create(link.url);
        SyndicationFeed feedItems = SyndicationFeed.Load(reader);
        reader.Close();

        feedBuilder.Append($"""
            <div class="feed mb-4">
                <h3 id="{link.name}" class="feed-title">{count}-{link.name}</h3>
            """); //add closing </div> for this in the end
        var isFirst = true;
        foreach (var item in feedItems.Items)
        {
            var description = item.Summary.Text;
            var itemLink = item.Links.Count > 0 ? item.Links[0]?.Uri?.AbsoluteUri ?? "#" : "#";
            var publishedDate = item.PublishDate.DateTime;
            var line = "";

            if (isFirst)
            {
                line = "";
                isFirst = false;
            }
            else
            {
                line = "<hr/>";
            }

            feedBuilder.Append($"""
                {line}
                <div class="feed-item">
                    <p dir="auto" class="description">
                        {description}
                    </p>
                    <a href="{itemLink}" class="btn btn-outline-primary mb-2">Read More<a>
                    <p>Publish Date: {publishedDate}</p>
                </div>
                """);
        }
        feedBuilder.Append("</div>");    
    }
    return Results.Content(feedBuilder.ToString());
});

app.MapGet("/check-page", async (HttpContext context, IDbConnection db) =>
{
    if (context.Request.Cookies.TryGetValue("UserId", out var userId))
    {
        var htmlContent = """
                    <header class="header">
                        <div class="navbar d-flex align-items-center">
                            <button class="menu"><img class="menu-img" src="images/menu-svgrepo-com.svg" alt="menu button"></button>
                            <h3 class="logo text-white">Feedy</h3>
                            <button class="logout-btn">logout</button>
                        </div>
                    </header>

                    <main class="main-container">

                        <div class="modal fade" id="add-modal" tabindex="-1" aria-labelledby="modal-title" aria-hidden="true">
                            <div class="modal-dialog">
                                <div class="modal-content">
                                    <div class="modal-header">

                                    <h5 class="modal-title" id="modal-title">Add Feed</h5>
                                    <button class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button> 
                                    </div>

                                    <div class="modal-body">
                                        <form action="/login" method="post" class="modal-form">
                                            <div hx-get="/token" hx-trigger="load" hx-swap="innerHTML"></div>
                                            <div class="form-group">
                                                <label for="name">Name</label>
                                                <input class="form-control" type="text" id="name" name="name" placeholder="Name" />
                                            </div>

                                            <div class="form-group">
                                                <label for="url">Url</label>
                                                <input class="form-control" type="text" id="url" name="url" placeholder="Url" />
                                            </div>

                                            <button class="btn btn-primary" type="submit">Add Feed</button>
                                        </form>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="modal fade" id="delete-modal" tabindex="-1" aria-labelledby="modal-title2" aria-hidden="true">
                            <div class="modal-dialog">
                                <div class="modal-content">
                                    <div class="modal-header">

                                    <h5 class="modal-title" id="modal-title2">Delete Feed</h5>
                                    <button class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button> 
                                    </div>

                                    <div class="modal-body">
                                        <form action="/login" method="post" class="modal-form">
                                            <div hx-get="/token" hx-trigger="load" hx-swap="innerHTML"></div>
                                            <div class="form-group mb-3">
                                                <label for="select">Feed Name</label>
                                                <select hx-get="/select-options" hx-trigger="load" hx-swap="innerHTML" class="form-select" aria-label="Default select example">

                                                </select>
                                            </div>

                                            <button class="btn btn-danger" type="submit">Delete Feed</button>
                                        </form>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div id="sidebar" class="sidebar-container">
                            <div class="sidebar">

                                <div class="actions-container">
                                    <h5>Actions</h5>
                                    <a data-bs-toggle="modal" data-bs-target="#add-modal" class="side-action"><img src="images/plus.svg" alt="add icon"> <p class="action-text">Add Feed</p></a>
                                    <a data-bs-toggle="modal" data-bs-target="#delete-modal" class="side-action"><img style="width: 20px;" src="images/close-svgrepo-com.svg" alt="add icon"> <p class="action-text">Delete Feed</p></a>
                                </div>

                                <h5>Feeds</h5>
                                <div class="shortcuts" hx-get="/shortcuts" hx-trigger="load" hx-swap="innerHTML">
                                </div>
                            </div>
                        </div>

                        <div hx-get="/feeds" hx-trigger="load" hx-swap="innerHTML" class="feed-container">

                        </div>
                    </main>

                    <footer style="width: 100%;">
                        <div class="text-center p-3" style="background-color: rgb(59, 115, 246);">
                            Copyright 2024 ŠAbdullah Haytham Hedeya
                        </div>
                    </footer>
        """;
        return Results.Content(htmlContent, "text/html");
    }
    else
    {
        var htmlContent = """
         
            <header class="header">
                <div class="navbar d-flex align-items-center">
                    <button class="menu"><img class="menu-img" src="images/menu-svgrepo-com.svg" alt="menu button"></button>
                    <h3 class="logo text-white">Feedy</h3>
                    <button class="logout-btn">logout</button>
                </div>
            </header>

            <main class="login-container replace">
                <form action="/login" method="post" class="login" id="login-form">

                    <h1 style="text-align: center; margin-bottom: 24px;">Login</h1>
                    <div hx-get="/token" hx-trigger="load" hx-swap="innerHTML"></div>
                    <div class="form-group">
                        <label for="email">Email</label>
                        <input class="form-control" type="email" id="email" name="email" placeholder="Email" />
                        <p style="margin-top: 0px; margin-bottom: 15px;" class="email-val text-danger"></p>
                    </div>

                    <div class="form-group">
                        <label for="password">Password</label>
                        <input class="form-control" type="password" id="password" name="password" placeholder="Password" />
                        <p style="margin-top: 0px; margin-bottom: 15px;" class="pass-val text-danger"></p>
                    </div>

                    <div style="margin-bottom: 8px;">Don't have an account? <button class="to-register-btn">Register</button></div>

                    <button class="btn btn-primary" type="submit">Login</button>
                    <div class="alert-container">
                        <div id="alert-box">
                            invalid email or password
                        </div>
                    </div>
                </form>
            </main>

            <footer style="width: 100%;">
                <div class="text-center p-3" style="background-color: rgb(59, 115, 246);">
                    Copyright 2024 ŠAbdullah Haytham Hedeya
                </div>
            </footer>
        """;
        return Results.Content(htmlContent, "text/html");
    }

    
});


app.Run();
