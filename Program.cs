using System.Text;
using FileSystem_Honeywell.Model;
using FileSystem_Honeywell.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddDbContext<FSDBContext>(options =>
//{
//   var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//    options.UseSqlServer(connectionString);
//});


var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")  
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<FileSettings>(
    builder.Configuration.GetSection("FileSettings"));

builder.Services.AddSingleton<IFileStorageService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var uploadsRoot = Path.Combine(env.ContentRootPath, "uploads");
    Directory.CreateDirectory(uploadsRoot);
    return new FileStorageService(uploadsRoot);
});

builder.Services.AddSingleton<IFileMetadataStore>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var metadataPath = Path.Combine(env.ContentRootPath, "fileMetadata.json");
    return new JsonFileMetadataStore(metadataPath);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
