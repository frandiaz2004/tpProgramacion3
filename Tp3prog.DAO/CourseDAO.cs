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

    public RelationshipResult Create(Course course, IReadOnlyCollection<long>? studentIds)
    {
        if (studentIds is null || studentIds.Count == 0) return RelationshipResult.MinimumRequired;
        var ids = studentIds.Distinct().ToArray();
        using var transaction = MembershipLocks.Begin(context);
        MembershipLocks.People(context, ids);
        var students = context.Students.Where(student => ids.Contains(student.Id)).ToList();
        if (students.Count != ids.Length) return RelationshipResult.NotFound;
        course.Students = students;
        context.Courses.Add(course);
        context.SaveChanges();
        transaction.Commit();
        return RelationshipResult.Success;
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
        using var transaction = MembershipLocks.Begin(context);
        MembershipLocks.Courses(context, [id]);
        var course = GetById(id);
        if (course is null) return false;
        context.Courses.Remove(course);
        context.SaveChanges();
        transaction.Commit();
        return true;
    }

    public RelationshipResult AddStudent(long id, long studentId)
    {
        using var transaction = MembershipLocks.Begin(context);
        MembershipLocks.People(context, [studentId]);
        MembershipLocks.Courses(context, [id]);
        var course = GetById(id);
        var student = context.Students.Find(studentId);
        if (course is null || student is null) return RelationshipResult.NotFound;
        if (!context.Courses.Any(existing => existing.Id == id && existing.Students.Any(member => member.Id == studentId)))
        {
            course.Students.Add(student);
            context.SaveChanges();
        }
        transaction.Commit();
        return RelationshipResult.Success;
    }

    public RelationshipResult RemoveStudent(long id, long studentId)
    {
        using var transaction = MembershipLocks.Begin(context);
        MembershipLocks.People(context, [studentId]);
        MembershipLocks.Courses(context, [id]);
        if (!context.Courses.Any(course => course.Id == id) || !context.Students.Any(student => student.Id == studentId))
            return RelationshipResult.NotFound;
        var count = context.Courses.Where(course => course.Id == id && course.Students.Any(student => student.Id == studentId))
            .Select(course => (int?)course.Students.Count).SingleOrDefault();
        if (count is null) return RelationshipResult.Success;
        if (count <= 1) return RelationshipResult.MinimumRequired;
        var course = GetById(id);
        var enrolled = course!.Students.Single(existing => existing.Id == studentId);
        course.Students.Remove(enrolled);
        context.SaveChanges();
        transaction.Commit();
        return RelationshipResult.Success;
    }

    public RelationshipResult AddActivity(long id, long activityId)
    {
        var course = GetById(id);
        var activity = context.Activities.Find(activityId);
        if (course is null || activity is null) return RelationshipResult.NotFound;

        // CourseId es una FK sombra: una actividad tiene un solo curso.
        var courseId = context.Entry(activity).Property<long?>("CourseId").CurrentValue;
        if (courseId.HasValue && courseId != id) return RelationshipResult.Conflict;
        if (courseId == id) return RelationshipResult.Success;

        // La actualización condicional evita pisar una asignación concurrente.
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
