using Tp3prog.DAO;
using Tp3prog.DAO.EntityFramework;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró la cadena DefaultConnection.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddScoped<PlayerDAO>();
builder.Services.AddScoped<TeamDAO>();
builder.Services.AddScoped<TrainerDAO>();
builder.Services.AddScoped<StudentDAO>();
builder.Services.AddScoped<CourseDAO>();
builder.Services.AddScoped<ActivityDAO>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
