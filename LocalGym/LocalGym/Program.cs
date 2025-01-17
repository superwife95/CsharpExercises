using LocalGym;
using LocalGym.GymDbContext;
using LocalGym.Models;
using LocalGym.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});
builder.Services.AddDbContext<GymInfo>(DbContextOptions
    => DbContextOptions.UseSqlServer("Server=WSAMZN-GDQ29NA1;" +
    "database=GymInfo;Trusted_Connection =" +
    " True; TrustServerCertificate = True;"));
builder.Services.AddScoped<IGymInfoRepository, GymRepository>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddAuthentication("Beare").AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Authentication:Issuer"],
        ValidAudience = builder.Configuration["Authentication:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(builder.Configuration["Authentication:SecretForKey"])),
    };
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseExceptionHandler(apperror =>
{
    apperror.Run(async Context =>
    {
        Context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        Context.Response.ContentType = "application/json";
        var contextFeature = Context.Features.Get<IExceptionHandlerFeature>();
        if (contextFeature != null)
        {
            if (contextFeature.Error is IException e)
            {
                if (e.statusCode.HasValue)
                {
                    Context.Response.StatusCode = (int)e.statusCode.Value;
                    await Context.Response.WriteAsync(new ExceptionModel().toJson());
                }

                
            }
            await Context.Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                StatusCode = Context.Response.StatusCode,
                Message = "something went wrong",
            }));

        }
    });
});
app.UseHttpsRedirection();
app.UseMiddleware<LocalGym.ExceptionHandlerMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
