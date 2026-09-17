using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;

namespace Tp3prog.DAO;

public class TrainerDAO
{
    private readonly AppDbContext context;

    public TrainerDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Trainer> GetAll() => context.Trainers.ToList();
    public Trainer? GetById(long id) => context.Trainers.Find(id);

    public Trainer Create(Trainer trainer)
    {
        context.Trainers.Add(trainer);
        context.SaveChanges();
        return trainer;
    }

    public bool Update(long id, Trainer trainer)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        context.Entry(existing).CurrentValues.SetValues(trainer);
        context.SaveChanges();
        return true;
    }

    public bool Delete(long id)
    {
        var trainer = GetById(id);
        if (trainer is null) return false;
        context.Trainers.Remove(trainer);
        context.SaveChanges();
        return true;
    }
}