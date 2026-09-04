using Tp3prog.Entities;

namespace Tp3prog.DAO;

public class TeamDAO
{
    public List<Team> GetAll() => MockDatabase.Teams;
    public Team? GetById(long id) => MockDatabase.Teams.FirstOrDefault(team => team.Id == id);

    public Team Create(Team team)
    {
        team.Id = MockDatabase.Teams.Count == 0 ? 1 : MockDatabase.Teams.Max(item => item.Id) + 1;
        MockDatabase.Teams.Add(team);
        return team;
    }

    public bool Update(long id, Team team)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        team.Id = id;
        MockDatabase.Teams[MockDatabase.Teams.IndexOf(existing)] = team;
        return true;
    }

    public bool Delete(long id)
    {
        var team = GetById(id);
        return team is not null && MockDatabase.Teams.Remove(team);
    }
}