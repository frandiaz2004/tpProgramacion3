using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tp3prog.DAO;
using Tp3prog.DAO.EntityFramework;
using Xunit;

namespace Tp3prog.Tests;

public class AuthFlowTests
{
    [Fact]
    public async Task Register_creates_a_user_and_returns_only_safe_fields()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var response = await client.PostAsJsonAsync("/User", new { email = " User@Example.com ", password = "ValidPassword123!" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(["email", "id"], json.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal("User@Example.com", json.RootElement.GetProperty("email").GetString());
        Assert.True(json.RootElement.GetProperty("id").GetInt64() > 0);
        using var scope = application.Services.CreateScope();
        var stored = scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AsNoTracking().Single();
        Assert.NotEqual("ValidPassword123!", stored.PasswordHash);
        Assert.Equal("USER@EXAMPLE.COM", stored.NormalizedEmail);
        Assert.StartsWith("$2", stored.PasswordHash);
        Assert.True(int.Parse(stored.PasswordHash.Split('$')[2]) >= 12);
        Assert.True(BCrypt.Net.BCrypt.Verify("ValidPassword123!", stored.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("WrongPassword123!", stored.PasswordHash));
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(stored));
    }

    [Fact]
    public async Task Unknown_login_returns_generic_unauthorized()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var response = await client.PostAsJsonAsync("/User/login", new { email = "unknown@example.com", password = "WrongPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_requires_a_bearer_token()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var response = await client.GetAsync("/User/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_a_signed_expiring_token_and_me_returns_only_safe_user_fields()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var id = await Register(client);
        var response = await client.PostAsJsonAsync("/User/login", new { email = " LOGIN@EXAMPLE.COM ", password = "ValidPassword123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(["expiresAt", "token"], json.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
        var token = json.RootElement.GetProperty("token").GetString()!;
        var expiresAt = json.RootElement.GetProperty("expiresAt").GetDateTime();
        Assert.Equal(DateTimeKind.Utc, expiresAt.Kind);
        Assert.InRange(expiresAt, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(id.ToString(), jwt.Subject);
        Assert.Equal(AuthApiFactory.Issuer, jwt.Issuer);
        Assert.Equal(AuthApiFactory.Audience, Assert.Single(jwt.Audiences));
        Assert.Equal(expiresAt, jwt.ValidTo);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type.Contains("password", StringComparison.OrdinalIgnoreCase));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await client.GetAsync("/User/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var user = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal(["email", "id"], user.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(id, user.RootElement.GetProperty("id").GetInt64());
        Assert.Equal("login@example.com", user.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Incorrect_password_and_unknown_email_return_the_same_unauthorized_response()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        await Register(client);
        var wrongPassword = await client.PostAsJsonAsync("/User/login", new { email = "login@example.com", password = "WrongPassword123!" });
        var missingUser = await client.PostAsJsonAsync("/User/login", new { email = "missing@example.com", password = "WrongPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, missingUser.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await missingUser.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Duplicate_email_ignores_case_and_surrounding_spaces_and_is_enforced_by_the_database()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        await Register(client);
        var duplicate = await client.PostAsJsonAsync("/User", new { email = " LOGIN@EXAMPLE.COM ", password = "DifferentPassword123!" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var scope = application.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var original = Assert.Single(context.Users.AsNoTracking());
        var dao = scope.ServiceProvider.GetRequiredService<UserDAO>();
        Assert.Throws<DbUpdateException>(() => dao.Create(" LOGIN@EXAMPLE.COM ", original.PasswordHash));
    }

    [Fact]
    public async Task Password_spaces_are_preserved_during_registration_and_verification()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        await Register(client, "  ValidPassword123!  ");
        var altered = await client.PostAsJsonAsync("/User/login", new { email = "login@example.com", password = "ValidPassword123!" });
        var exact = await client.PostAsJsonAsync("/User/login", new { email = "login@example.com", password = "  ValidPassword123!  " });
        Assert.Equal(HttpStatusCode.Unauthorized, altered.StatusCode);
        Assert.Equal(HttpStatusCode.OK, exact.StatusCode);
    }

    [Fact]
    public async Task A_72_byte_utf8_password_is_accepted_and_verified()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var password = new string('á', 36);
        Assert.Equal(72, Encoding.UTF8.GetByteCount(password));
        await Register(client, password);
        var response = await client.PostAsJsonAsync("/User/login", new { email = "login@example.com", password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_authentication_requests_return_bad_request(string path, string body)
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var response = await client.PostAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = application.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<AppDbContext>().Users);
    }

    public static IEnumerable<object[]> InvalidRequests()
    {
        yield return ["/User", "{}"];
        yield return ["/User", "{\"email\":\"not-an-email\",\"password\":\"ValidPassword123!\"}"];
        yield return ["/User", "{\"email\":\"valid@example.com\",\"password\":\"short\"}"];
        yield return ["/User", "{\"email\":\"valid@example.com\",\"password\":\"ValidPassword123!\",\"passwordHash\":\"client-hash\"}"];
        yield return ["/User", "{\"email\":\"valid@example.com\",\"password\":null}"];
        yield return ["/User", JsonSerializer.Serialize(new { email = "valid@example.com", password = new string('a', 73) })];
        yield return ["/User", JsonSerializer.Serialize(new { email = "valid@example.com", password = new string('á', 37) })];
        yield return ["/User/login", "{\"email\":\"valid@example.com\"}"];
        yield return ["/User/login", "{\"email\":\"invalid\",\"password\":\"ValidPassword123!\"}"];
        yield return ["/User/login", "{\"email\":\"valid@example.com\",\"password\":\"ValidPassword123!\",\"passwordHash\":\"client-hash\"}"];
        yield return ["/User/login", JsonSerializer.Serialize(new { email = "valid@example.com", password = new string('á', 37) })];
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("tampered")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("wrong-key")]
    [InlineData("wrong-algorithm")]
    [InlineData("no-expiry")]
    [InlineData("unsigned")]
    [InlineData("malformed")]
    public async Task Me_rejects_invalid_tokens(string variant)
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var id = await Register(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(id, variant));
        var response = await client.GetAsync("/User/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Players")]
    [InlineData("Students")]
    [InlineData("Trainers")]
    [InlineData("Courses")]
    [InlineData("Teams")]
    [InlineData("Activities")]
    public async Task Existing_crud_remains_public(string resource)
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync('/' + resource)).StatusCode);
    }

    [Theory]
    [InlineData("Jwt:Key", "")]
    [InlineData("Jwt:Key", "short")]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Jwt:Audience", "")]
    [InlineData("Jwt:ExpirationMinutes", "0")]
    [InlineData("Jwt:ExpirationMinutes", "1441")]
    public void Invalid_jwt_configuration_is_rejected_at_startup(string key, string value)
    {
        using var application = new AuthApiFactory(new Dictionary<string, string?> { [key] = value });
        Assert.Throws<OptionsValidationException>(() => application.CreateClient());
    }

    private static async Task<long> Register(HttpClient client, string password = "ValidPassword123!")
    {
        var response = await client.PostAsJsonAsync("/User", new { email = "login@example.com", password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetInt64();
    }

    private static string CreateToken(long id, string variant)
    {
        if (variant == "malformed") return "not-a-token";
        var now = DateTime.UtcNow;
        var key = variant == "wrong-key" ? new string('x', 64) : AuthApiFactory.SigningKey;
        var algorithm = variant == "wrong-algorithm" ? SecurityAlgorithms.HmacSha512 : SecurityAlgorithms.HmacSha256;
        var token = new JwtSecurityToken(
            issuer: variant == "wrong-issuer" ? "another-issuer" : AuthApiFactory.Issuer,
            audience: variant == "wrong-audience" ? "another-audience" : AuthApiFactory.Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, id.ToString())],
            notBefore: now.AddMinutes(-10),
            expires: variant == "no-expiry" ? null : variant == "expired" ? now.AddMinutes(-1) : now.AddMinutes(60),
            signingCredentials: variant == "unsigned" ? null : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), algorithm));
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        if (variant != "tampered") return encoded;
        var parts = encoded.Split('.');
        parts[2] = (parts[2][0] == 'a' ? 'b' : 'a') + parts[2][1..];
        return string.Join('.', parts);
    }
}
