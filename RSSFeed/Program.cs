

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

void deleteCookies(HttpContext context)
{
    var cookieOptions = new CookieOptions
    {
        Expires = DateTime.UtcNow.AddDays(-1),
        Path = "/"
    };
    context.Response.Cookies.Append("UserId", "", cookieOptions);
}

app.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    string html = $"""<input name = "{token.FormFieldName}" type = "hidden" value = "{token.RequestToken}"/>""";
    return Results.Content(html, "text/html");
});


app.MapPost("/login", async (HttpContext context, IAntiforgery antiforgery, IDbConnection db, [FromForm] string email, [FromForm] string password) =>
{
    await antiforgery.ValidateRequestAsync(context);

    string sql = "SELECT userId FROM users WHERE email = @Email AND password = @Password";
    var userId = await db.QueryFirstOrDefaultAsync<int>(sql, new { Email = email, Password = password });

    if (userId > 0)
    {
        context.Response.Cookies.Append("UserId", userId.ToString(), cookieOptions);
        context.Response.Cookies.Append("Email", email, cookieOptions);
        return Results.Content("""<div id="alert-box" class="green">User logged in successfully</div> """);
    }

    if(email == null || email == "")
    {
        return Results.Content("""<div id="alert-box" class="red">Please Provide an Email</div> """);
    }
    else if(password == null || password == "")
    {
        return Results.Content("""<div id="alert-box" class="red">Please Provide a Password</div> """);
    }
    else if (password.Length < 8)
    {
        return Results.Content("""<div id="alert-box" class="red">Password is Too Short</div> """);
    }
    else if(password.Contains(" "))
    {
        return Results.Content("""<div id="alert-box" class="red">Password Can't contain spaces</div> """);
    }
    return Results.Content("""<div id="alert-box" class="red">Invalid Credentials</div> """);
});

app.MapPost("/register", async (HttpContext context, IAntiforgery antiforgery, IDbConnection db, [FromForm] string email, [FromForm] string password, [FromForm] string password2) =>
{
    await antiforgery.ValidateRequestAsync(context);

    if (email == null || email == "")
    {
        return Results.Content("""<div id="alert-box" class="red">Please Provide an Email</div> """);
    }
    else if (password == null || password == "")
    {
        return Results.Content("""<div id="alert-box" class="red">Please Provide a Password</div> """);
    }
    else if (password2 == null || password2 == "")
    {
        return Results.Content("""<div id="alert-box" class="red">Please Confirm Your Password</div> """);
    }
    else if (password.Contains(" ") || password2.Contains(" "))
    {
        return Results.Content("""<div id="alert-box" class="red">Password Can't contain spaces</div> """);
    }
    else if(password != password2)
    {
        return Results.Content("""<div id="alert-box" class="red">Passwords Do not Match</div> """);
    }
    else if (password.Length < 8)
    {
        return Results.Content("""<div id="alert-box" class="red">Password is Too Short</div> """);
    }

    string sql = "INSERT INTO users(email, password) VALUES(@email, @password)";
    int rows = await db.ExecuteAsync(sql, new { email = email, password = password });
    if(rows > 0)
    {
        return Results.Content("""<div id="alert-box" class="green">User Created successfully</div> """);
    }

    return Results.Content("""<div id="alert-box" class="red">Invalid Credentials</div> """);

});

app.MapPost("/logout", (HttpContext context) =>
{
    if (context.Request.Cookies.TryGetValue("UserId", out var userId))
    {
        deleteCookies(context);
        Results.Ok("User Logged Out");
    }

    Results.BadRequest("No user was logged in");
});

app.MapGet("/register-page", () =>{
    var htmlContent = """
        <header class="header">
            <div class="navbar d-flex align-items-center">
                <button class="menu"><img class="menu-img" src="images/menu-svgrepo-com.svg" alt="menu button"></button>
                <h3 class="logo text-white">Feedy</h3>
            </div>
        </header>

        <main class="login-container">
            <form hx-post="/register" hx-target=".alert-container" class="login" id="register-form">
                <h1 style="text-align: center; margin-bottom: 24px;">Register</h1>
                <div hx-get="/token" hx-trigger="load" hx-target="this" hx-swap="outerHTML"></div>
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

                <div class="form-group">
                    <label for="password2">Confirm Password</label>
                    <input class="form-control" type="password" id="password2" name="password2" placeholder="Re-enter Password" />
                    <p style="margin-top: 0px; margin-bottom: 15px;" class="pass-val text-danger"></p>
                </div>

                <div style="margin-bottom: 8px;">already registered? <button class="to-login-btn">Login</button></div>

                <button class="btn btn-primary" type="submit">Register</button>
                <div class="alert-container">
                    <div id="alert-box">
                        invalid email or password
                    </div>
                </div>
            </form>
    </main>
    <script>
        const regEmailInput = document.getElementById("email");
        const regPasswordInput = document.getElementById("password");
        const regPasswordInput2 = document.getElementById("password2");
    
        function removeAlertClass() {
            const alertingBox = document.getElementById("alert-box");
            if (alertingBox.classList.contains("red")) {
                alertingBox.classList.remove("red");
            } else if(alertingBox.classList.contains("green")){
                alertingBox.classList.remove("green");
            }
        }
    
        regEmailInput.addEventListener("click", removeAlertClass);
        regPasswordInput.addEventListener("click", removeAlertClass);
        regPasswordInput2.addEventListener("click", removeAlertClass);
    </script>
    """;

    return Results.Content(htmlContent, "text/html");
});

app.MapGet("/login-page", (HttpContext context) => {
    deleteCookies(context);
    var htmlContent = """
         
            <header class="header">
                <div class="navbar d-flex align-items-center">
                    <button class="menu"><img class="menu-img" src="images/menu-svgrepo-com.svg" alt="menu button"></button>
                    <h3 class="logo text-white">Feedy</h3>
                </div>
            </header>

            <main class="login-container">
                <form hx-post="/login" hx-target=".alert-container" class="login" id="login-form">

                    <h1 style="text-align: center; margin-bottom: 24px;">Login</h1>
                    <div hx-get="/token" hx-trigger="load" hx-target="this" hx-swap="outerHTML"></div>
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

                    <div style="margin-bottom: 8px;">Don't have an account? <button hx-get="/register-page" hx-target=".replace" hx-trigger="click" class="to-register-btn">Register</button></div>

                    <button class="btn btn-primary" type="submit">Login</button>
                    <div class="alert-container">
                        <div id="alert-box">
                            invalid email or password
                        </div>
                    </div>
                </form>
            </main>
            <script>
                const emailInput2 = document.getElementById("email");
                const passwordInput2 = document.getElementById("password");
        
                function removeAlertClass() {
                    const alertingBox = document.getElementById("alert-box");
                    if (alertingBox.classList.contains("red")) {
                        alertingBox.classList.remove("red");
                    } else if(alertingBox.classList.contains("green")){
                        alertingBox.classList.remove("green");
                    }
                }
        
                emailInput2.addEventListener("click", removeAlertClass);
                passwordInput2.addEventListener("click", removeAlertClass);
            </script>
        """;

    return Results.Content(htmlContent, "text/html");
});


app.MapGet("/shortcuts", async (HttpContext context, IDbConnection db) =>
{
    var query = context.Request.Query;
    var email = query["feed"];
    if (context.Request.Cookies.TryGetValue("UserId", out var userId) || !string.IsNullOrEmpty(email))
    {
        var sql = "SELECT name FROM feeds WHERE userId=@userId";

        string id;
        if (!string.IsNullOrEmpty(email))
        {
            sql = "SELECT userId FROM users WHERE email = @Email";
            id = await db.QueryFirstOrDefaultAsync<string>(sql, new { Email = email });
        }
        else
        {
            id = userId;
        }
        sql = "SELECT name FROM feeds WHERE userId=@userId";
        var names = await db.QueryAsync(sql, new {userId = id});

        StringBuilder shortcuts = new StringBuilder();
        foreach (var name in names)
        {
            shortcuts.Append(@$"<a href=""#{name.name}"" class=""shortcut"">{name.name}</a>");
        }
        return Results.Content(shortcuts.ToString());
    }
    return Results.NoContent();
});

app.MapGet("/select-options", async (HttpContext context, IDbConnection db) =>
{
    if (context.Request.Cookies.TryGetValue("UserId", out var userId))
    {
        var sql = "SELECT name, url FROM feeds WHERE userId=@userId";
        var names = await db.QueryAsync(sql, new { userId = userId });

        StringBuilder shortcuts = new StringBuilder();
        shortcuts.Append(@$"<option selected>Select a feed to delete</option>");
        foreach (var name in names)
        {
            shortcuts.Append(@$"<option value={name.url}>{name.name}</option>");
        }
        return Results.Content(shortcuts.ToString());
    }
    return Results.NoContent();
});

app.MapGet("/feeds", async (IDbConnection db, HttpContext context) =>
{
    try
    {
            var query = context.Request.Query;
            var email = query["feed"];
            if (context.Request.Cookies.TryGetValue("UserId", out var userId) || !string.IsNullOrEmpty(email))
            {
            var sql = "SELECT name, url FROM feeds WHERE userId=@userId";
            string id;
            if (!string.IsNullOrEmpty(email))
            {
                sql = "SELECT userId FROM users WHERE email = @Email";
                id = await db.QueryFirstOrDefaultAsync<string>(sql, new { Email = email});
            }
            else
            {
                id=userId;
            }
            sql = "SELECT name, url FROM feeds WHERE userId=@userId";
            var links = await db.QueryAsync(sql, new { userId = id });

            if(!links.Any())
            {
                return Results.Content("""
                    <div style="width: 100%; height: 100%;" class="d-flex justify-content-center align-items-center">
                        <h1>Looks Empty Here</h1>
                    </div>
                    """, "text/html");
            }

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
                    var description = item.Summary?.Text ?? "No Description Available";
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
                count++;
            }
            return Results.Content(feedBuilder.ToString());
            }
        return Results.NoContent();
    }
    catch (Exception ex) 
    {
        return Results.Content("""
            
                <h3 id="nothing" class="feed-title">No rss file was found using this link</h3>
           
            """);
    }
    
});

app.MapGet("/", (HttpContext context) =>
{
    var query = context.Request.Query;
    var email = query["feed"];
    if (!string.IsNullOrEmpty(email))
    {
        var htmlContent = $"""
                <!DOCTYPE html>
                <html>

                <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width">
                    <title></title>
                    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet"
                        integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">
                    <link rel="stylesheet" href="styles.css">

                    <link rel="preconnect" href="https://fonts.googleapis.com">
                    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
                    <link href="https://fonts.googleapis.com/css2?family=Fuzzy+Bubbles:wght@400;700&display=swap" rel="stylesheet">

                    <script src="https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js"
                        integrity="sha384-IQsoLXl5PILFhosVNubq5LC7Qb9DXgDA9i+tQ8Zj3iwWAwPtgFTxbJ8NT4GN1R8p"
                        crossorigin="anonymous"></script>
                    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/js/bootstrap.min.js"
                        integrity="sha384-cVKIPhGWiC2Al4u+LWgxfKTRIcfu0JTxR+EQDz/bgldoEyl4H0zUF0QKbrJ0EcQF"
                        crossorigin="anonymous"></script>
                    <script src="https://unpkg.com/htmx.org@1.9.12"></script>
                    <script src="script.js" defer></script>

                </head>

                <body>
                    <div class="replace">
                    <header class="header">
                        <div class="navbar d-flex align-items-center">
                            <button class="menu"><img class="menu-img" src="images/menu-svgrepo-com.svg" alt="menu button"></button>
                            <h3 class="logo text-white">Feedy</h3>
                            <button hx-get="/login-page" hx-trigger="click" hx-target=".replace" class="login-btn">login</button>
                            <div class="d-none logout"></div>
                        </div>
                    </header>

                    <main class="main-container">                    

                        <div id="sidebar" class="sidebar-container">
                            <div class="sidebar">

                                <h5>Feeds</h5>
                                <div class="shortcuts" hx-get="/shortcuts?feed={email}" hx-trigger="load" hx-swap="innerHTML">
                                </div>
                            </div>
                        </div>

                        <div hx-get="/feeds?feed={email}" hx-trigger="load" hx-swap="innerHTML" class="feed-container">

                        </div>
                    </main>
                    <script>
                        const menu = document.querySelector('.menu');
                        const sidebar = document.querySelector('.sidebar-container');
                        const main = document.querySelector('.main-container');

        """ + " menu.addEventListener('click', () => { sidebar.classList.toggle('active'); main.classList.toggle('no-grid')});</script></div>" + """
            <footer style="width: 100vw; z-index: 100;">
            <div class="text-center p-3" style="background-color: rgb(59, 115, 246);">
                Copyright 2024 @Abdullah Haytham Hedeya
            </div>
        </footer>
                </body>
        </html>
        """;
        return Results.Content(htmlContent, "text/html");
    }
    else
    {
        var htmlContent = """
                <!DOCTYPE html>
        <html>

        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width">
            <title></title>
            <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet"
                integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">
            <link rel="stylesheet" href="styles.css">

            <link rel="preconnect" href="https://fonts.googleapis.com">
            <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
            <link href="https://fonts.googleapis.com/css2?family=Fuzzy+Bubbles:wght@400;700&display=swap" rel="stylesheet">

            <script src="https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js"
                integrity="sha384-IQsoLXl5PILFhosVNubq5LC7Qb9DXgDA9i+tQ8Zj3iwWAwPtgFTxbJ8NT4GN1R8p"
                crossorigin="anonymous"></script>
            <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/js/bootstrap.min.js"
                integrity="sha384-cVKIPhGWiC2Al4u+LWgxfKTRIcfu0JTxR+EQDz/bgldoEyl4H0zUF0QKbrJ0EcQF"
                crossorigin="anonymous"></script>
            <script src="https://unpkg.com/htmx.org@1.9.12"></script>
            <script src="script.js" defer></script>

        </head>

        <body>
            <div class="replace" hx-get="check-page" hx-trigger="load" hx-swap="innerHTML">
            </div>

            <footer style="width: 100vw; z-index: 100;">
                <div class="text-center p-3" style="background-color: rgb(59, 115, 246);">
                    Copyright 2024 @Abdullah Haytham Hedeya
                </div>
            </footer>
        </body>
        </html>
        """;
        return Results.Content(htmlContent, "text/html");
    }
});

app.MapGet("/check-page", async (HttpContext context, IDbConnection db) =>
{
    if (context.Request.Cookies.TryGetValue("UserId", out var userId))
    {
        string email;
        context.Request.Cookies.TryGetValue("Email", out email);
        var htmlContent = """
                    <header class="header">
                        <div class="navbar d-flex align-items-center">
                            <button class="menu"><img class="menu-img" src="images/menu-svgrepo-com.svg" alt="menu button"></button>
                            <h3 class="logo text-white">Feedy</h3>
                            <button hx-post="logout" hx-trigger="click" hx-target=".logout" class="logout-btn">logout</button>
                            <div class="d-none logout"></div>
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
                                        <form hx-post="add-feed" hx-target=".add-message" class="modal-form">
                                            <div class="add-message d-none"></div>
                                            <div hx-get="/token" hx-trigger="load" hx-swap="innerHTML" hx-target="this" ></div>
                                            <div class="form-group">
                                                <label for="name">Name</label>
                                                <input class="form-control" type="text" id="name" name="name" placeholder="Name" />
                                            </div>

                                            <div class="form-group">
                                                <label for="url">Url</label>
                                                <input class="form-control" type="text" id="url" name="url" placeholder="Url" />
                                            </div>

                                            <button class="btn btn-primary" data-bs-dismiss="modal" type="submit">Add Feed</button>
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
                                        <form hx-delete="delete-feed" hx-target=".delete-message" class="modal-form">
                                            <div hx-get="/token" hx-trigger="load" hx-target="this" hx-swap="innerHTML"></div>
                                            <div class="delete-message d-none"></div>
                                            <div class="form-group mb-3">
                                                <label for="select">Feed Name</label>
                                                <select id="delete-select" hx-get="/select-options" hx-trigger="load" hx-swap="innerHTML" hx-target="this" class="form-select" name="feed" aria-label="Default select example">

                                                </select>
                                            </div>

                                            <button id="delete-btn" class="btn btn-danger" data-bs-dismiss="modal" type="submit">Delete Feed</button>
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
                                    <a data-bs-toggle="modal" data-bs-target="#delete-modal" class="side-action"><img style="width: 20px;" src="images/close-svgrepo-com.svg" alt="delete icon"> <p class="action-text">Delete Feed</p></a>
                                    <a class="side-action" onclick="shareFeed()"><img style="width: 20px;" src="images/share-svgrepo-com.svg" alt="share icon"> <p class="action-text">Share Feed</p></a>
                                </div>

                                <h5>Feeds</h5>
                                <div class="shortcuts" hx-get="/shortcuts" hx-trigger="load" hx-swap="innerHTML">
                                </div>
                            </div>
                        </div>

                        <div hx-get="/feeds" hx-trigger="load" hx-swap="innerHTML" class="feed-container">

                        </div>
                    </main>
                    <script>
                        const deleteSelect = document.getElementById("delete-select")
                        const deleteBtn = document.getElementById("delete-btn")
                        if(deleteSelect.value === "Select a feed to delete" || deleteSelect.value === ""){
                            deleteBtn.disabled = true
                        }
                        deleteSelect.addEventListener("change", (e)=>{
                            if(deleteSelect.value === "Select a feed to delete"){
                                deleteBtn.disabled = true
                            } else {deleteBtn.disabled = false}
                        })

                        const menu = document.querySelector('.menu');
                        const sidebar = document.querySelector('.sidebar-container');
                        const main = document.querySelector('.main-container');

                        menu.addEventListener('click', () => {
                            sidebar.classList.toggle('active');
                            main.classList.toggle('no-grid')
                        });
                    </script>
                    <div hx-get="/share" hx-swap="outerHTML" hx-trigger="load"></div>

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
                </div>
            </header>

            <main class="login-container">
                <form hx-post="/login" hx-target=".alert-container" class="login" id="login-form">

                    <h1 style="text-align: center; margin-bottom: 24px;">Login</h1>
                    <div hx-get="/token" hx-trigger="load" hx-target="this" hx-swap="outerHTML"></div>
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

                    <div style="margin-bottom: 8px;">Don't have an account? <button hx-get="/register-page" hx-target=".replace" hx-trigger="click" class="to-register-btn">Register</button></div>

                    <button class="btn btn-primary" type="submit">Login</button>
                    <div class="alert-container">
                        <div id="alert-box">
                            invalid email or password
                        </div>
                    </div>
                </form>
            </main>
            <script>
                const emailInput = document.getElementById("email");
                const passwordInput = document.getElementById("password");
        
                function removeAlertClass() {
                    const alertingBox = document.getElementById("alert-box");
                    if (alertingBox.classList.contains("red")) {
                        alertingBox.classList.remove("red");
                    } else if(alertingBox.classList.contains("green")){
                        alertingBox.classList.remove("green");
                    }
                }
        
                emailInput.addEventListener("click", removeAlertClass);
                passwordInput.addEventListener("click", removeAlertClass);
            </script>
        """;
        return Results.Content(htmlContent, "text/html");
    }

    
});

app.MapPost("/add-feed", async (HttpContext context, IAntiforgery antiforgery, IDbConnection db, [FromForm] string name, [FromForm] string url) =>
{
    await antiforgery.ValidateRequestAsync(context);

    if (context.Request.Cookies.TryGetValue("UserId", out var userId))
    {
        string sql = "INSERT INTO feeds(name, url, userId) VALUES(@name, @url, @userId)";
        int rows = await db.ExecuteAsync(sql, new { name = name, url = url, userId = userId });

        if (rows > 0)
        {
            return Results.Content(""" <div id="add-message">Feed Added Successfully</div>""", "text/html");
        }
    }
    return Results.Content(""" <div id="add-message">Feed not Added</div>""", "text/html");
});

app.MapDelete("/delete-feed", async (HttpContext context, IAntiforgery antiforgery, IDbConnection db, [FromForm] string feed) =>
{
    await antiforgery.ValidateRequestAsync(context);

    if (context.Request.Cookies.TryGetValue("UserId", out var userId))
    {
        string sql = "DELETE FROM feeds WHERE userId=@userId AND url=@feed";
        int rows = await db.ExecuteAsync(sql, new { userId = userId, feed = feed });

        if (rows > 0)
        {
            return Results.Content(""" <div id="delete-message">Feed Deleted Successfully</div>""", "text/html");
        }
    }
    return Results.Content(""" <div id="delete-message">Feed not Deleted</div>""", "text/html");
});

app.MapGet("/share", (HttpContext context) =>
{
    if (context.Request.Cookies.TryGetValue("Email", out var email))
    {
        return Results.Content("<script>function shareFeed(){" + $"navigator.clipboard.writeText(window.location.origin + '/?feed={email}'); alert('Link Copied Successfully');}}</script>");
    }
    return Results.NoContent();
});


app.Run();
