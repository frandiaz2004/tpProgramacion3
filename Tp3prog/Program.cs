using Tp3prog.DAO;
using Tp3prog.DAO.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Tp3prog.Filters;
using Tp3prog.Authentication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<PersistenceExceptionFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    });
    options.OperationFilter<BearerSecurityOperationFilter>();
});
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se encontró la cadena DefaultConnection.");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
builder.Services.AddScoped<PlayerDAO>();
builder.Services.AddScoped<TeamDAO>();
builder.Services.AddScoped<TrainerDAO>();
builder.Services.AddScoped<StudentDAO>();
builder.Services.AddScoped<CourseDAO>();
builder.Services.AddScoped<ActivityDAO>();
builder.Services.AddScoped<UserDAO>();
builder.Services.AddUserAuthentication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
