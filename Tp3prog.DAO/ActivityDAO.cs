using Tp3prog.Entities;

namespace Tp3prog.DAO;

public class ActivityDAO
{
    public List<Activity> GetAll() => MockDatabase.Activities;
    public Activity? GetById(long id) => MockDatabase.Activities.FirstOrDefault(activity => activity.Id == id);

    public Activity Create(Activity activity)
    {
        activity.Id = MockDatabase.Activities.Count == 0 ? 1 : MockDatabase.Activities.Max(item => item.Id) + 1;
        MockDatabase.Activities.Add(activity);
        return activity;
    }

    public bool Update(long id, Activity activity)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        activity.Id = id;
        MockDatabase.Activities[MockDatabase.Activities.IndexOf(existing)] = activity;
        return true;
    }

    public bool Delete(long id)
    {
        var activity = GetById(id);
        return activity is not null && MockDatabase.Activities.Remove(activity);
    }
}