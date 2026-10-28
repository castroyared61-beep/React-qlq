using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ApiEjemplo.Controllers;

[ApiController]
[Route("[controller]")] 
public class CategoriaController(NpgsqlDataSource db) : ControllerBase
{
    [HttpGet(Name = "GetCategoria")]
    public async Task<ActionResult<List<Categoria>>> Get()
    {
        const string consulta = "SELECT * FROM categoria";
        await using var cmd = db.CreateCommand(consulta);
        await using var lector = await cmd.ExecuteReaderAsync(); 
        var lista = new List<Categoria>();
        
        while (await lector.ReadAsync())
        {
            var categoria = new Categoria
            {
                Id = lector.GetInt32(0),
                Nombre = lector.GetString(1)
            };
            lista.Add(categoria);
        }
        return Ok(lista);
    } 

    [HttpGet("{id:int}")] 
    public async Task<ActionResult<Categoria>> ObtenerPorId(int id)
    {
        const string consulta = "SELECT * FROM categoria where id_categoria = @ID";
        await using var cmd = db.CreateCommand(consulta);
        cmd.Parameters.AddWithValue("ID", id);
        await using var lector = await cmd.ExecuteReaderAsync();
        
        if (!await lector.ReadAsync())
        {
            return NotFound(new {error = "La categoria no existe"});
        }
        
        return Ok(new Categoria 
        {
            Id = lector.GetInt32(0),
            Nombre = lector.GetString(1)
        });
    }

    [HttpPost]
    public async Task<ActionResult<Categoria>> Post(Categoria nuevaCategoria)
    {
        if (string.IsNullOrWhiteSpace(nuevaCategoria.Nombre))
        {
            return BadRequest( new { error = "Nombre incorrecto para la categoria"} );
        }

        const string consulta = "INSERT INTO categoria (nombre) VALUES (@nombre) RETURNING id_categoria";
        var nombre = nuevaCategoria.Nombre.Trim();
        try
        {
            await using var cmd = db.CreateCommand(consulta);
            cmd.Parameters.AddWithValue("nombre", nombre);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, new Categoria{ Id=id, Nombre = nombre});
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { error = "Ya existe una categoria con ese nombre"});
        }
    }
 
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        const string consulta = "DELETE FROM categoria WHERE id_categoria = @id_categoria";
        await using var cmd = db.CreateCommand(consulta); 
        cmd.Parameters.AddWithValue("id_categoria", id);
        var filasAfectadas = await cmd.ExecuteNonQueryAsync();

        if (filasAfectadas == 0)
        {
            return NotFound(new { error = "Categoría no encontrada" });
        }

        return NoContent(); 
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Categoria>> Actualizar(int id, Categoria nuevaCategoria)
    {
        if (string.IsNullOrWhiteSpace(nuevaCategoria.Nombre))
        {
            return BadRequest( new { error = "Nombre incorrecto para la categoria"});
        }

        const string consulta = "UPDATE categoria SET nombre = @nombre WHERE id_categoria=@id";
        var nombre = nuevaCategoria.Nombre.Trim();
        try
        {
            await using var cmd = db.CreateCommand(consulta);
            cmd.Parameters.AddWithValue("nombre",nombre);
            cmd.Parameters.AddWithValue("id",id);
            await cmd.ExecuteNonQueryAsync();
            return CreatedAtAction(nameof(ObtenerPorId), new { id }, new Categoria{ Id=id, Nombre = nombre});
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { error = "Ya existe una categoria con ese nombre"});
        }
    }
}