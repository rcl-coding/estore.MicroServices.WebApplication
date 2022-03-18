using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using RCL.Identity.AAD.Groups;
using RCL.Identity.GraphService;
using RCL.WebHook.DatabaseContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"));

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var previousOptions = options.Events.OnRedirectToIdentityProvider;
    options.Events.OnRedirectToIdentityProvider = async context =>
    {
        await previousOptions(context);
        context.ProtocolMessage.ResponseType = Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectResponseType.IdToken;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.Requirements.Add(new GroupsCheckRequirement(new string[] { "admin" })));

    options.AddPolicy("CheckGroupsRCLWebHook", policy =>
      policy.Requirements.Add(new GroupsCheckRequirement(new string[] { "admin" })));
});

builder.Services.AddScoped<IAuthorizationHandler, B2CGroupsCheckHandler>();
builder.Services.AddTransient<IClaimsTransformation, B2CGroupClaimsTransformation>();
builder.Services.AddB2CGraphClientServices(options => builder.Configuration.Bind("AzureAdB2C", options));

builder.Services.AddDbContext<WebHookDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Database"));
});

builder.Services.AddAzureBlobStorageServices(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Storage");
});

builder.Services.AddRazorPages()
      .AddMicrosoftIdentityUI();

builder.Services.AddProductsFrontEnd(options => builder.Configuration.Bind("Api", options));
builder.Services.AddOrdersFrontEnd(options => builder.Configuration.Bind("Api", options));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.SetupSqlServerWebHookDataInfrastructure();

app.Run();
