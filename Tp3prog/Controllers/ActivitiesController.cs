using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Contracts;

namespace Tp3prog.Controllers;

[ApiController]
[Route("[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly ActivityDAO activityDAO;

    public ActivitiesController(ActivityDAO activityDAO)
    {
        this.activityDAO = activityDAO;
    }

    [HttpGet]
    public ActionResult<List<ActivityResponse>> GetAll() => Ok(activityDAO.GetAll().Select(activity => activity.ToResponse()).ToList());

    [HttpGet("{id:long}")]
    public ActionResult<ActivityResponse> GetById(long id) => activityDAO.GetById(id) is { } activity ? Ok(activity.ToResponse()) : NotFound();

    [HttpPost]
    public ActionResult<ActivityResponse> Create(ActivityRequest request)
    {
        if (request.Id != 0) return BadRequest("El Id se genera al crear la actividad; omitilo o enviá 0.");
        var created = activityDAO.Create(request.ToEntity());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, ActivityRequest request)
    {
        if (request.Id != 0 && request.Id != id) return BadRequest("El Id del cuerpo debe coincidir con el de la URL.");
        return activityDAO.Update(id, request.ToEntity()) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => activityDAO.Delete(id) ? NoContent() : NotFound();
}
