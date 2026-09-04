using Tp3prog.Entities;

namespace Tp3prog.DAO;

public class TrainerDAO
{
    public List<Trainer> GetAll() => MockDatabase.Trainers;
    public Trainer? GetById(long id) => MockDatabase.Trainers.FirstOrDefault(trainer => trainer.Id == id);

    public Trainer Create(Trainer trainer)
    {
        trainer.Id = MockDatabase.Trainers.Count == 0 ? 1 : MockDatabase.Trainers.Max(item => item.Id) + 1;
        MockDatabase.Trainers.Add(trainer);
        return trainer;
    }

    public bool Update(long id, Trainer trainer)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        trainer.Id = id;
        MockDatabase.Trainers[MockDatabase.Trainers.IndexOf(existing)] = trainer;
        return true;
    }

    public bool Delete(long id)
    {
        var trainer = GetById(id);
        return trainer is not null && MockDatabase.Trainers.Remove(trainer);
    }
}