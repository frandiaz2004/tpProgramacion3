using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;

namespace Tp3prog.DAO;

public class CourseDAO
{
    private readonly AppDbContext context;

    public CourseDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Course> GetAll() => context.Courses.ToList();
    public Course? GetById(long id) => context.Courses.Find(id);

    public Course Create(Course course)
    {
        context.Courses.Add(course);
        context.SaveChanges();
        return course;
    }

    public bool Update(long id, Course course)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        context.Entry(existing).CurrentValues.SetValues(course);
        context.SaveChanges();
        return true;
    }

    public bool Delete(long id)
    {
        var course = GetById(id);
        if (course is null) return false;
        context.Courses.Remove(course);
        context.SaveChanges();
        return true;
    }
}