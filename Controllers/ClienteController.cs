using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ApiEjemplo;

namespace ApiEjemplo.Controllers;

[ApiController]
[Route("[controller]")] 
public class ClienteController(NpgsqlDataSource db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Cliente>>> Get()
    {
        const string consulta = "SELECT id_cliente, nombre, correo, ciudad, fecha_registro FROM cliente";
        await using var cmd = db.CreateCommand(consulta);
        await using var lector = await cmd.ExecuteReaderAsync(); 
        var lista = new List<Cliente>();
        
        while (await lector.ReadAsync())
        {
            lista.Add(new Cliente
            {
                Id = lector.GetInt32(0),
                Nombre = lector.GetString(1),
                Correo = lector.GetString(2),
                Ciudad = lector.IsDBNull(3) ? null : lector.GetString(3),
                FechaRegistro = lector.GetDateTime(4)
            });
        }
        return Ok(lista);
    } 

    [HttpGet("{id:int}")] 
    public async Task<ActionResult<Cliente>> ObtenerPorId(int id)
    {
        const string consulta = "SELECT id_cliente, nombre, correo, ciudad, fecha_registro FROM cliente WHERE id_cliente = @ID";
        await using var cmd = db.CreateCommand(consulta);
        cmd.Parameters.AddWithValue("ID", id);
        await using var lector = await cmd.ExecuteReaderAsync();
        
        if (!await lector.ReadAsync())
        {
            return NotFound(new {error = "El cliente no existe"});
        }
        
        return Ok(new Cliente 
        {
            Id = lector.GetInt32(0),
            Nombre = lector.GetString(1),
            Correo = lector.GetString(2),
            Ciudad = lector.IsDBNull(3) ? null : lector.GetString(3),
            FechaRegistro = lector.GetDateTime(4)
        });
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> Post(Cliente nuevoCliente)
    {
        if (string.IsNullOrWhiteSpace(nuevoCliente.Nombre) || string.IsNullOrWhiteSpace(nuevoCliente.Correo))
        {
            return BadRequest(new { error = "Nombre o correo incorrectos" });
        }

        const string consulta = "INSERT INTO cliente (nombre, correo, ciudad, fecha_registro) VALUES (@nombre, @correo, @ciudad, @fecha_registro) RETURNING id_cliente";
        
        try
        {
            await using var cmd = db.CreateCommand(consulta);
            cmd.Parameters.AddWithValue("nombre", nuevoCliente.Nombre.Trim());
            cmd.Parameters.AddWithValue("correo", nuevoCliente.Correo.Trim());
            cmd.Parameters.AddWithValue("ciudad", string.IsNullOrWhiteSpace(nuevoCliente.Ciudad) ? DBNull.Value : nuevoCliente.Ciudad.Trim());
            
            var fecha = nuevoCliente.FechaRegistro == default ? DateTime.UtcNow.Date : nuevoCliente.FechaRegistro;
            cmd.Parameters.AddWithValue("fecha_registro", fecha);
            
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            nuevoCliente.Id = id;
            nuevoCliente.FechaRegistro = fecha;

            return CreatedAtAction(nameof(ObtenerPorId), new { id }, nuevoCliente);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { error = "Ya existe un cliente con ese correo"});
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Cliente>> Actualizar(int id, Cliente clienteActualizado)
    {
        if (string.IsNullOrWhiteSpace(clienteActualizado.Nombre) || string.IsNullOrWhiteSpace(clienteActualizado.Correo))
        {
            return BadRequest(new { error = "Nombre o correo incorrectos"});
        }

        const string consulta = "UPDATE cliente SET nombre = @nombre, correo = @correo, ciudad = @ciudad WHERE id_cliente = @id";
        
        try
        {
            await using var cmd = db.CreateCommand(consulta);
            cmd.Parameters.AddWithValue("nombre", clienteActualizado.Nombre.Trim());
            cmd.Parameters.AddWithValue("correo", clienteActualizado.Correo.Trim());
            cmd.Parameters.AddWithValue("ciudad", string.IsNullOrWhiteSpace(clienteActualizado.Ciudad) ? DBNull.Value : clienteActualizado.Ciudad.Trim());
            cmd.Parameters.AddWithValue("id", id);
            
            var filasAfectadas = await cmd.ExecuteNonQueryAsync();
            if (filasAfectadas == 0) return NotFound(new { error = "Cliente no encontrado" });

            return Ok(new { mensaje = "Cliente actualizado correctamente" });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { error = "Ya existe otro cliente con ese correo"});
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        const string consulta = "DELETE FROM cliente WHERE id_cliente = @id_cliente";
        await using var cmd = db.CreateCommand(consulta); 
        cmd.Parameters.AddWithValue("id_cliente", id);
        
        var filasAfectadas = await cmd.ExecuteNonQueryAsync();

        if (filasAfectadas == 0)
        {
            return NotFound(new { error = "Cliente no encontrado" });
        }

        return NoContent(); 
    }
}