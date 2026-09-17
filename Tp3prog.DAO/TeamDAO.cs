using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;

namespace Tp3prog.DAO;

public class TeamDAO
{
    private readonly AppDbContext context;

    public TeamDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Team> GetAll() => context.Teams.ToList();
    public Team? GetById(long id) => context.Teams.Find(id);

    public Team Create(Team team)
    {
        context.Teams.Add(team);
        context.SaveChanges();
        return team;
    }

    public bool Update(long id, Team team)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        context.Entry(existing).CurrentValues.SetValues(team);
        context.SaveChanges();
        return true;
    }

    public bool Delete(long id)
    {
        var team = GetById(id);
        if (team is null) return false;
        context.Teams.Remove(team);
        context.SaveChanges();
        return true;
    }
}