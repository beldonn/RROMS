using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using RROMS.Data;
using RROMS.Models;
using RROMS.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Database ----------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in appsettings.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------------- Identity ----------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// ---------------- Razor Pages + role-based folder authorization ----------------
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdministratorRole");
    options.Conventions.AuthorizeFolder("/Staff", "RequireStaffRole");
    options.Conventions.AuthorizeFolder("/Reservations");
    options.Conventions.AuthorizeFolder("/Orders");
    options.Conventions.AuthorizeFolder("/Feedback");
    options.Conventions.AuthorizeFolder("/Notifications");
    options.Conventions.AuthorizePage("/Account/Profile");
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole(Roles.Administrator));
    options.AddPolicy("RequireStaffRole", policy => policy.RequireRole(Roles.Staff, Roles.Administrator));
});

// ---------------- Session (used for the shopping cart) ----------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(3);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// ---------------- Application services ----------------
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

var app = builder.Build();

// ---------------- Middleware pipeline ----------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// ---------------- Chatbot minimal API ----------------
// A tiny anonymous endpoint used by the floating chat widget in Pages/Shared/_Layout.cshtml.
app.MapPost("/api/chatbot/ask", async (ChatRequest request, IChatbotService chatbotService, IHttpContextAccessor accessor) =>
{
    var userId = accessor.HttpContext?.User?.Identity?.IsAuthenticated == true
        ? accessor.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        : null;
    var answer = await chatbotService.GetAnswerAsync(request.Message ?? string.Empty, userId);
    return Results.Ok(new { answer });
}).AllowAnonymous();

// ---------------- Apply migrations + seed reference/demo data ----------------
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();

record ChatRequest(string? Message);
