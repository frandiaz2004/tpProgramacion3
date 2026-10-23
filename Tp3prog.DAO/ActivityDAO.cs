using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;

namespace Tp3prog.DAO;

public class ActivityDAO
{
    private readonly AppDbContext context;

    public ActivityDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Activity> GetAll() => context.Activities.ToList();
    public Activity? GetById(long id) => context.Activities.Find(id);

    public Activity Create(Activity activity)
    {
        context.Activities.Add(activity);
        context.SaveChanges();
        return activity;
    }

    public bool Update(long id, Activity activity)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        existing.Title = activity.Title;
        existing.Description = activity.Description;
        existing.Date = activity.Date;
        existing.Type = activity.Type;
        context.SaveChanges();
        return true;
    }

    public bool Delete(long id)
    {
        var activity = GetById(id);
        if (activity is null) return false;
        context.Activities.Remove(activity);
        context.SaveChanges();
        return true;
    }
}
