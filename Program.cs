using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
//By ANGEL
using OpenHandsVolunteerPlatform.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//By ANGEL
builder.Services.AddScoped<FollowService>();
builder.Services.AddScoped<CertificateService>();
builder.Services.AddScoped<EmailService>();

//on 13.4.2026 in PHASE_4
builder.Services.AddScoped<IEmailSender, IdentityEmailSender>();

//By FY on 3.4.2026 23:19
builder.Services.AddScoped<ICreditScoreService, CreditScoreService>();

//on 15.4.2026 in PHASE_4
builder.Services.AddHostedService<OpportunityReminderService>();

//ADDED on 1.3.2026 18:18 in PHASE_1
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    //By ANGEL
    options.Lockout.AllowedForNewUsers = true;

})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();
//end ADDED

var app = builder.Build();

//ADDED on 7.3.2026 00:03 in PHASE_1
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.
        GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Volunteer", "Organization", "Admin" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Check if admin user exists
    var adminEmail = "admin@mysite.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var user = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
        };

        var result = await userManager.CreateAsync(user, "StrongPassword123!");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}
//end ADDED


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Program.cs - Add area support (YOUR EXISTING ROUTES STAY THE SAME)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//on 13.4.2026 in PHASE_4
app.MapControllers();

app.MapRazorPages();

app.Run();
