using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Tp3prog.DAO;

public class PlayerDAO
{
    private readonly AppDbContext context;

    public PlayerDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Player> GetAll() => context.Players.AsNoTracking().Include(player => player.Teams).ToList();

    public Player? GetById(long id) => context.Players.Include(player => player.Teams)
        .SingleOrDefault(player => player.Id == id);

    public Player Create(Player player)
    {
        context.Players.Add(player);
        context.SaveChanges();
        return player;
    }

    public bool Update(long id, Player player)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        existing.Name = player.Name;
        existing.Age = player.Age;
        existing.Dni = player.Dni;
        existing.Number = player.Number;
        context.SaveChanges();
        return true;
    }

    public bool Delete(long id)
    {
        var player = GetById(id);
        if (player is null) return false;
        context.Players.Remove(player);
        context.SaveChanges();
        return true;
    }
}
