using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Entities;

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
    public ActionResult<List<Player>> GetAll() => Ok(playerDAO.GetAll());

    [HttpGet("{id:long}")]
    public ActionResult<Player> GetById(long id) => playerDAO.GetById(id) is { } player ? Ok(player) : NotFound();

    [HttpPost]
    public ActionResult<Player> Create(Player player)
    {
        var created = playerDAO.Create(player);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, Player player) => playerDAO.Update(id, player) ? Ok(player) : NotFound();

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => playerDAO.Delete(id) ? NoContent() : NotFound();
}