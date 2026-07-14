using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using Confluent.Kafka;
Env.Load();


var builder = WebApplication.CreateBuilder();
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");                
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT_KEY is not set in environment variables!");
}
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "FileVaultApi";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "FileVaultFront";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var kafkaBootstrap = Environment.GetEnvironmentVariable("Kafka__BootstrapServers") ?? "kafka:9092";

builder.Services.AddControllers();
builder.Services.AddDbContext<Identity.Database.AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("jwtToken"))
                {
                    context.Token = context.Request.Cookies["jwtToken"];
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<Identity.Database.IPasswordHasher, Identity.Database.BCryptHasher>();
builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
{
    var config = new ProducerConfig { BootstrapServers = kafkaBootstrap };
    return new ProducerBuilder<Null, string>(config).Build();
});



var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<Identity.Database.AppDbContext>();
    var hasher = services.GetRequiredService<Identity.Database.IPasswordHasher>();

    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();
app.MapControllers();
app.Run();