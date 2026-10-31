using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tp3prog.Authentication;
using Tp3prog.Contracts;
using Tp3prog.DAO;

namespace Tp3prog.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class UserController(UserDAO userDAO, PasswordService passwords, JwtTokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public ActionResult<UserResponse> Create(CreateUserDTO request)
    {
        if (userDAO.GetByEmail(request.Email) is not null) return Conflict("Ya existe un usuario con ese email.");
        var created = userDAO.Create(request.Email, passwords.Hash(request.Password));
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new UserResponse(created.Id, created.Email));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginDTO request)
    {
        var user = userDAO.GetByEmail(request.Email);
        var validPassword = passwords.Verify(request.Password, user?.PasswordHash);
        if (user is null || !validPassword) return Unauthorized("Email o contraseña incorrectos.");
        return Ok(tokens.Create(user));
    }

    [HttpGet("me")]
    public ActionResult<UserResponse> Me()
    {
        if (!long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)) return Unauthorized();
        var user = userDAO.GetById(id);
        return user is null ? Unauthorized() : Ok(new UserResponse(user.Id, user.Email));
    }

    [HttpGet("{id:long}")]
    public ActionResult<UserResponse> GetById(long id)
    {
        if (CheckOwnership(id) is { } denied) return denied;
        var user = userDAO.GetById(id);
        return user is null ? Unauthorized() : Ok(new UserResponse(user.Id, user.Email));
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, UpdateUserDTO request)
    {
        if (CheckOwnership(id) is { } denied) return denied;
        var user = userDAO.GetById(id);
        if (user is null) return Unauthorized();
        if (!passwords.Verify(request.CurrentPassword, user.PasswordHash)) return Unauthorized("Email o contraseña incorrectos.");
        var duplicate = userDAO.GetByEmail(request.Email);
        if (duplicate is not null && duplicate.Id != id) return Conflict("Ya existe un usuario con ese email.");

        var newPasswordHash = request.NewPassword is null ? null : passwords.Hash(request.NewPassword);
        return userDAO.Update(id, request.Email, user.PasswordHash, newPasswordHash)
            ? NoContent() : Conflict("La cuenta cambió durante la operación. Volvé a intentarlo.");
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id, DeleteUserDTO request)
    {
        if (CheckOwnership(id) is { } denied) return denied;
        var user = userDAO.GetById(id);
        if (user is null) return Unauthorized();
        if (!passwords.Verify(request.CurrentPassword, user.PasswordHash)) return Unauthorized("Email o contraseña incorrectos.");
        return userDAO.Delete(id, user.PasswordHash)
            ? NoContent() : Conflict("La cuenta cambió durante la operación. Volvé a intentarlo.");
    }

    private ActionResult? CheckOwnership(long id)
    {
        if (!long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var authenticatedId)) return Unauthorized();
        return authenticatedId == id ? null : Forbid();
    }
}
