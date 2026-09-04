using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Entities;

namespace Tp3prog.Controllers;

[ApiController]
[Route("[controller]")]
public class CoursesController : ControllerBase
{
    private readonly CourseDAO courseDAO;

    public CoursesController(CourseDAO courseDAO)
    {
        this.courseDAO = courseDAO;
    }

    [HttpGet]
    public ActionResult<List<Course>> GetAll() => Ok(courseDAO.GetAll());

    [HttpGet("{id:long}")]
    public ActionResult<Course> GetById(long id) => courseDAO.GetById(id) is { } course ? Ok(course) : NotFound();

    [HttpPost]
    public ActionResult<Course> Create(Course course)
    {
        var created = courseDAO.Create(course);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, Course course) => courseDAO.Update(id, course) ? Ok(course) : NotFound();

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => courseDAO.Delete(id) ? NoContent() : NotFound();
}