using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon.S3;
using SerbleGames.Backend.Auth;
using SerbleGames.Backend.Database;
using SerbleGames.Backend.Schemas.Config;
using SerbleGames.Backend.Serble;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<SerbleApiSettings>()
    .Bind(builder.Configuration.GetSection("SerbleApi"));
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"));
builder.Services.AddOptions<S3Settings>()
    .Bind(builder.Configuration.GetSection("S3"));

JwtSettings jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? throw new Exception("JWT settings not found");
S3Settings s3Settings = builder.Configuration.GetSection("S3").Get<S3Settings>() ?? throw new Exception("S3 settings not found");

builder.Services.AddSingleton<IAmazonS3>(_ => {
    AmazonS3Config config = new() {
        ServiceURL = s3Settings.ServiceUrl
    };
    return new AmazonS3Client(s3Settings.AccessKey, s3Settings.SecretKey, config);
});

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<GamesDatabaseContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("MySql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySql"))));

builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IAdminRepo, AdminRepo>();
builder.Services.AddScoped<IGameRepo, GameRepo>();
builder.Services.AddScoped<IPackageRepo, PackageRepo>();
builder.Services.AddScoped<IJwtManager, JwtManager>();
builder.Services.AddHttpClient<ISerbleApiClient, SerbleApiClient>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.MapControllers();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.Run();
Console.WriteLine("Bye!");
