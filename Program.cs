using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var cadena = builder.Configuration.GetConnectionString("Tienda") ?? throw new InvalidOperationException("La conexion se fue a rd");

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(cadena));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();