using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Entities;

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
    public ActionResult<List<Activity>> GetAll() => Ok(activityDAO.GetAll());

    [HttpGet("{id:long}")]
    public ActionResult<Activity> GetById(long id) => activityDAO.GetById(id) is { } activity ? Ok(activity) : NotFound();

    [HttpPost]
    public ActionResult<Activity> Create(Activity activity)
    {
        var created = activityDAO.Create(activity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, Activity activity) => activityDAO.Update(id, activity) ? Ok(activity) : NotFound();

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => activityDAO.Delete(id) ? NoContent() : NotFound();
}