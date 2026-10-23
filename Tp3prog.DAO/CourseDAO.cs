using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Tp3prog.DAO;

public class CourseDAO
{
    private readonly AppDbContext context;

    public CourseDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Course> GetAll() => context.Courses.AsNoTracking()
        .Include(course => course.Students).Include(course => course.Activities).AsSplitQuery().ToList();

    public Course? GetById(long id) => context.Courses
        .Include(course => course.Students).Include(course => course.Activities).AsSplitQuery()
        .SingleOrDefault(course => course.Id == id);

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
        existing.Name = course.Name;
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

    public RelationshipResult AddStudent(long id, long studentId)
    {
        var course = GetById(id);
        var student = context.Students.Find(studentId);
        if (course is null || student is null) return RelationshipResult.NotFound;
        if (course.Students.All(existing => existing.Id != studentId))
        {
            course.Students.Add(student);
            context.SaveChanges();
        }
        return RelationshipResult.Success;
    }

    public RelationshipResult RemoveStudent(long id, long studentId)
    {
        var course = GetById(id);
        var student = context.Students.Find(studentId);
        if (course is null || student is null) return RelationshipResult.NotFound;
        var enrolled = course.Students.SingleOrDefault(existing => existing.Id == studentId);
        if (enrolled is not null)
        {
            course.Students.Remove(enrolled);
            context.SaveChanges();
        }
        return RelationshipResult.Success;
    }

    public RelationshipResult AddActivity(long id, long activityId)
    {
        var course = GetById(id);
        var activity = context.Activities.Find(activityId);
        if (course is null || activity is null) return RelationshipResult.NotFound;

        // La migración existente permite que una actividad pertenezca a un solo curso.
        var courseId = context.Entry(activity).Property<long?>("CourseId").CurrentValue;
        if (courseId.HasValue && courseId != id) return RelationshipResult.Conflict;
        if (courseId == id) return RelationshipResult.Success;

        // La condición se comprueba en la base para no sobrescribir otra asignación concurrente.
        var changed = context.Activities.Where(existing => existing.Id == activityId &&
            EF.Property<long?>(existing, "CourseId") == null)
            .ExecuteUpdate(setters => setters.SetProperty(existing => EF.Property<long?>(existing, "CourseId"), (long?)id));
        context.Entry(activity).Reload();
        if (changed == 0)
        {
            if (context.Entry(activity).State == EntityState.Detached) return RelationshipResult.NotFound;
            return context.Entry(activity).Property<long?>("CourseId").CurrentValue == id
                ? RelationshipResult.Success : RelationshipResult.Conflict;
        }
        return RelationshipResult.Success;
    }

    public RelationshipResult RemoveActivity(long id, long activityId)
    {
        var course = GetById(id);
        var activity = context.Activities.Find(activityId);
        if (course is null || activity is null) return RelationshipResult.NotFound;
        context.Activities.Where(existing => existing.Id == activityId &&
            EF.Property<long?>(existing, "CourseId") == id)
            .ExecuteUpdate(setters => setters.SetProperty(existing => EF.Property<long?>(existing, "CourseId"), (long?)null));
        context.Entry(activity).Reload();
        return RelationshipResult.Success;
    }
}
