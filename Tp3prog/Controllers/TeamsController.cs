using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Entities;

namespace Tp3prog.Controllers;

[ApiController]
[Route("[controller]")]
public class TeamsController : ControllerBase
{
    private readonly TeamDAO teamDAO;

    public TeamsController(TeamDAO teamDAO)
    {
        this.teamDAO = teamDAO;
    }

    [HttpGet]
    public ActionResult<List<Team>> GetAll() => Ok(teamDAO.GetAll());

    [HttpGet("{id:long}")]
    public ActionResult<Team> GetById(long id) => teamDAO.GetById(id) is { } team ? Ok(team) : NotFound();

    [HttpPost]
    public ActionResult<Team> Create(Team team)
    {
        var created = teamDAO.Create(team);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, Team team) => teamDAO.Update(id, team) ? Ok(team) : NotFound();

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => teamDAO.Delete(id) ? NoContent() : NotFound();
}