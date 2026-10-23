using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Tp3prog.DAO;

public class TeamDAO
{
    private readonly AppDbContext context;

    public TeamDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Team> GetAll() => context.Teams.AsNoTracking()
        .Include(team => team.Players).Include(team => team.Trainers).AsSplitQuery().ToList();

    public Team? GetById(long id) => context.Teams
        .Include(team => team.Players).Include(team => team.Trainers).AsSplitQuery()
        .SingleOrDefault(team => team.Id == id);

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
        existing.Name = team.Name;
        existing.Category = team.Category;
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

    public RelationshipResult AddPlayer(long id, long playerId)
    {
        var team = GetById(id);
        var player = context.Players.Find(playerId);
        if (team is null || player is null) return RelationshipResult.NotFound;
        if (team.Players.All(existing => existing.Id != playerId))
        {
            team.Players.Add(player);
            context.SaveChanges();
        }
        return RelationshipResult.Success;
    }

    public RelationshipResult RemovePlayer(long id, long playerId)
    {
        var team = GetById(id);
        var player = context.Players.Find(playerId);
        if (team is null || player is null) return RelationshipResult.NotFound;
        var assigned = team.Players.SingleOrDefault(existing => existing.Id == playerId);
        if (assigned is not null)
        {
            team.Players.Remove(assigned);
            context.SaveChanges();
        }
        return RelationshipResult.Success;
    }

    public RelationshipResult AddTrainer(long id, long trainerId)
    {
        var team = GetById(id);
        var trainer = context.Trainers.Find(trainerId);
        if (team is null || trainer is null) return RelationshipResult.NotFound;
        if (team.Trainers.All(existing => existing.Id != trainerId))
        {
            team.Trainers.Add(trainer);
            context.SaveChanges();
        }
        return RelationshipResult.Success;
    }

    public RelationshipResult RemoveTrainer(long id, long trainerId)
    {
        var team = GetById(id);
        var trainer = context.Trainers.Find(trainerId);
        if (team is null || trainer is null) return RelationshipResult.NotFound;
        var assigned = team.Trainers.SingleOrDefault(existing => existing.Id == trainerId);
        if (assigned is not null)
        {
            team.Trainers.Remove(assigned);
            context.SaveChanges();
        }
        return RelationshipResult.Success;
    }
}
