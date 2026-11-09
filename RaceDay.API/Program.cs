
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Connect to SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- AZURE BLOB STORAGE ---
var blobConnectionString =
    builder.Configuration["AzureStorage:ConnectionString"];

if (!string.IsNullOrWhiteSpace(blobConnectionString))
{
    builder.Services.AddSingleton(
        new BlobServiceClient(blobConnectionString));
}

// Register the Blob Storage service
builder.Services.AddScoped<BlobStorageService>();

// --- SESSION MANAGEMENT ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- ENABLE SESSION ---
app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();
