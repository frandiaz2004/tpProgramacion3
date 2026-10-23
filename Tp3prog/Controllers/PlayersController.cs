using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Contracts;

namespace Tp3prog.Controllers;

[ApiController]
[Route("[controller]")]
public class PlayersController : ControllerBase
{
    private readonly PlayerDAO playerDAO;

    public PlayersController(PlayerDAO playerDAO)
    {
        this.playerDAO = playerDAO;
    }

    [HttpGet]
    public ActionResult<List<PlayerResponse>> GetAll() => Ok(playerDAO.GetAll().Select(player => player.ToResponse()).ToList());

    [HttpGet("{id:long}")]
    public ActionResult<PlayerResponse> GetById(long id) => playerDAO.GetById(id) is { } player ? Ok(player.ToResponse()) : NotFound();

    [HttpPost]
    public ActionResult<PlayerResponse> Create(PlayerRequest request)
    {
        if (request.Id != 0) return BadRequest("El Id se genera al crear el jugador; omitilo o enviá 0.");
        var created = playerDAO.Create(request.ToEntity());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, PlayerRequest request)
    {
        if (request.Id != 0 && request.Id != id) return BadRequest("El Id del cuerpo debe coincidir con el de la URL.");
        return playerDAO.Update(id, request.ToEntity()) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => playerDAO.Delete(id) ? NoContent() : NotFound();
}
