using Duende.IdentityModel.Client;
using ECM.ReservationSystem.Application;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Infrastructure;
using ECM.ReservationSystem.InfrastructureExtensions;
using ECM.ReservationSystem.Services;
using ECM.ReservationSystem.Services.Implementations;
using ECM.ReservationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;
using System.Security.Claims;
using static ECM.ReservationSystem.OpenIdSettings.Constants;

var builder = WebApplication.CreateBuilder(args);


AsposeLicenseHelper.SetLicense(builder.Configuration);
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});

var allowInsecureHttp = builder.Configuration.GetValue<bool>("Settings:AllowHttp", false); // true if your public URL is http://...




// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Register Services
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IUnitService, UnitService>();


builder.Services.AddHostedService<ExpiredHoldsCleanupService>();


#region Identity External Server
var identitySection = builder.Configuration.GetSection("Settings:Identity");
var identityServerUrl = identitySection["ServerURL"];
var identityClientId = identitySection["ClientId"];
var identityClientSecret = identitySection["ClientSecret"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie("Cookies", options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.AccessDeniedPath = "/Home/ErrorAccessDenied";
    if (allowInsecureHttp)
    {
        options.Cookie.SameSite = SameSiteMode.Lax;                 // works with ResponseMode=query
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.None;                // cross-site OIDC on HTTPS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = context =>
        {
            if (context.Principal.Identity.IsAuthenticated)
            {
                var tokens = context.Properties.GetTokens();
                var refreshToken = tokens.FirstOrDefault(t => t.Name == "refresh_token");
                var accessToken = tokens.FirstOrDefault(t => t.Name == "access_token");
                var exp = tokens.FirstOrDefault(t => t.Name == "expires_at");

                if (exp != null && DateTime.TryParse(exp.Value, out var expires) && expires < DateTime.UtcNow)
                {
                    var client = new HttpClient();
                    var disco = client.GetDiscoveryDocumentAsync(identityServerUrl).Result;

                    if (disco.IsError)
                    {
                        context.RejectPrincipal();
                        return Task.CompletedTask;
                    }

                    var response = client.RequestRefreshTokenAsync(new RefreshTokenRequest
                    {
                        Address = disco.TokenEndpoint,
                        ClientId = identityClientId,
                        ClientSecret = identityClientSecret,
                        RefreshToken = refreshToken?.Value
                    }).Result;

                    if (response.IsError)
                    {
                        context.RejectPrincipal();
                        return Task.CompletedTask;
                    }

                    // update tokens
                    refreshToken.Value = response.RefreshToken;
                    accessToken.Value = response.AccessToken;
                    var newExpires = DateTime.UtcNow.AddSeconds(response.ExpiresIn);
                    exp.Value = newExpires.ToString("o", CultureInfo.InvariantCulture);

                    context.Properties.StoreTokens(tokens);
                    context.ShouldRenew = true;
                }
            }
            return Task.CompletedTask;
        }
    };
    options.Cookie.Name = "Intalio.LA.MHIS";
    options.ForwardChallenge = "oidc";
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = identityServerUrl;
    options.RequireHttpsMetadata = !allowInsecureHttp;
    options.ClientId = identityClientId;
    options.ClientSecret = identityClientSecret;
    options.ResponseType = "code";
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("IdentityServerApi");
    options.Scope.Add("offline_access");
    options.GetClaimsFromUserInfoEndpoint = false;
    options.SaveTokens = true;
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";

    if (allowInsecureHttp)
    {
        options.ResponseMode = "query"; // top-level GET so Lax cookies are sent
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        options.ResponseMode = "form_post";
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    }

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = ctx =>
        {
            var req = ctx.Request;
            var host = req.Headers["X-Forwarded-Host"].FirstOrDefault() ?? req.Host.Value;
            var proto = req.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? req.Scheme;
            var prefix = req.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? req.PathBase.Value;

            ctx.ProtocolMessage.RedirectUri = $"{proto}://{host}{prefix}{ctx.Options.CallbackPath}";
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProviderForSignOut = ctx =>
        {
            var req = ctx.Request;
            var host = req.Headers["X-Forwarded-Host"].FirstOrDefault() ?? req.Host.Value;
            var proto = req.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? req.Scheme;
            var prefix = req.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? req.PathBase.Value;

            ctx.ProtocolMessage.PostLogoutRedirectUri = $"{proto}://{host}{prefix}/";
            return Task.CompletedTask;
        },
        OnTicketReceived = e =>
        {
            var identity = e.Principal.Identities.FirstOrDefault();
            if (identity != null)
            {
                var structureIdsString = identity.Claims.First(t => t.Type == Claims.StructureIds).Value.Replace('/', ',');
                var groupIdsString = identity.Claims.First(t => t.Type == Claims.GroupIds).Value.Replace('/', ',');

                var defaultStructureId = structureIdsString.Split(',').FirstOrDefault() ?? "";
                var defaultGroupId = groupIdsString.Split(',').FirstOrDefault() ?? "";

                identity.AddClaim(new Claim(Claims.UserId, identity.Claims.First(t => t.Type == "Id").Value));
                identity.AddClaim(new Claim(Claims.RoleId, identity.Claims.First(t => t.Type == "ApplicationRoleId").Value));
                identity.AddClaim(new Claim(Claims.StructureIds, structureIdsString));
                identity.AddClaim(new Claim(Claims.DefaultStructureId, defaultStructureId));
                identity.AddClaim(new Claim(Claims.GroupIds, groupIdsString));
                identity.AddClaim(new Claim(Claims.DefaultGroupId, defaultGroupId));
                identity.AddClaim(new Claim(Claims.Email, identity.Claims.First(t => t.Type == "Email").Value));
                identity.AddClaim(new Claim(Claims.DisplayName, identity.Claims.First(t => t.Type == "DisplayName").Value));
            }
            return Task.CompletedTask;
        },
        OnRemoteFailure = e =>
        {
            e.HandleResponse();
            e.Response.Redirect("Home/ErrorAccessDenied");
            return Task.CompletedTask;
        }
    };
})
.AddJwtBearer("Bearer", options =>
{
    options.Authority = identityServerUrl;
    options.RequireHttpsMetadata = false;
    options.Audience = "IdentityServerApi";
});
#endregion



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}");



app.Run();
