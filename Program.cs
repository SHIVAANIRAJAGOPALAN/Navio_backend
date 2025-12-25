// using System.Text;
// using Microsoft.IdentityModel.Tokens;
// using NavioBackend.Interfaces;
// using NavioBackend.Repository;
// using NavioBackend.Services;
// using MongoDB.Driver;

// var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
// // -----------------------------
// // CORS
// // -----------------------------
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowFrontend", policy =>
//     {
//         policy.WithOrigins("http://localhost:3001")
//               .AllowAnyHeader()
//               .AllowAnyMethod()
//               .AllowCredentials();
//     });
// });

// // -----------------------------
// // Mongo Settings
// // -----------------------------
// builder.Services.Configure<DatabaseSettings>(
//     builder.Configuration.GetSection("DatabaseSettings")
// );

// var dbSettings = builder.Configuration
//     .GetSection("DatabaseSettings")
//     .Get<DatabaseSettings>();

// builder.Services.AddSingleton(dbSettings);

// builder.Services.AddSingleton<IMongoClient>(sp =>
// {
//     var conn = builder.Configuration.GetConnectionString("MongoDB");
//     return new MongoClient(conn);
// });

// builder.Services.AddSingleton<IMongoDatabase>(sp =>
// {
//     var client = sp.GetRequiredService<IMongoClient>();
//     return client.GetDatabase(dbSettings.DatabaseName);
// });

// // -----------------------------
// // Repositories
// // -----------------------------
// builder.Services.AddScoped<IUserRepository, UserRepository>();
// builder.Services.AddScoped<ITruckRepository, TruckRepository>();
// builder.Services.AddScoped<ICargoTypeRepository, CargoTypeRepository>();
// builder.Services.AddScoped<ITripRepository, TripRepository>();

// //builder.Services.AddSingleton<ITripRepository, TripRepository>();


// // -----------------------------
// // Authentication
// // -----------------------------
// var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
// builder.Services.AddAuthentication("Bearer")
//     .AddJwtBearer(options =>
//     {
        
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateIssuerSigningKey = true,
//             ValidateLifetime = true,
//             ValidIssuer = builder.Configuration["Jwt:Issuer"],
//             ValidAudience = builder.Configuration["Jwt:Audience"],
//             IssuerSigningKey = new SymmetricSecurityKey(key)
//         };
//     });

// builder.Services.AddAuthorization();
// builder.Services.AddControllers();

// var app = builder.Build();
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// // -----------------------------
// // Middleware order (IMPORTANT)
// // -----------------------------
// app.UseCors("AllowFrontend");
// app.UseAuthentication();
// app.UseAuthorization();

// app.MapControllers();

// // Optional if needed:
// app.Urls.Add("http://0.0.0.0:5000");

// app.Run();

using System.Text;
using Microsoft.IdentityModel.Tokens;
using NavioBackend.Interfaces;
using NavioBackend.Repository;
using NavioBackend.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------------------------------------
// CORS
// --------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3001",
            "http://navio-frontend.s3-website.ap-south-2.amazonaws.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// --------------------------------------------------
// Load Database Settings (NO INTERFACE NEEDED)
// --------------------------------------------------
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings"));

var dbSettings = builder.Configuration
    .GetSection("DatabaseSettings")
    .Get<DatabaseSettings>();

builder.Services.AddSingleton(dbSettings);

// --------------------------------------------------
// MongoDB Client + Database
// --------------------------------------------------
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    return new MongoClient(dbSettings.ConnectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(dbSettings.DatabaseName);
});

// --------------------------------------------------
// Repositories
// --------------------------------------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITruckRepository, TruckRepository>();
builder.Services.AddScoped<ICargoTypeRepository, CargoTypeRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IDriverNotesRepository, DriverNotesRepository>();
builder.Services.AddScoped<IRoadRestrictionsRepository, RoadRestrictionsRepository>();
builder.Services.AddScoped<IActivityLogsRepository, ActivityLogsRepository>();

// --------------------------------------------------
// JWT Authentication
// --------------------------------------------------
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// --------------------------------------------------
// Swagger
// --------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --------------------------------------------------
// Middleware Order (Must be exact)
// --------------------------------------------------
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --------------------------------------------------
// Open API on localhost:5000
// --------------------------------------------------
app.Urls.Add("http://0.0.0.0:5000");

app.Run();
