using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tp3prog.DAO.EntityFramework;
using Tp3prog.DAO;
using Tp3prog.Entities;
using Xunit;

namespace Tp3prog.Tests;

public class UserAccountTests
{
    [Fact]
    public async Task Owner_can_read_the_resource_advertised_by_registration_without_exposing_hashes()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var account = await Register(client);
        Authenticate(client, account.Token);

        var response = await client.GetAsync("/User/" + account.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/User/" + account.Id, new Uri(account.Location).AbsolutePath);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(["email", "id"], json.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(account.Id, json.RootElement.GetProperty("id").GetInt64());
        Assert.Equal(account.Email, json.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Owner_can_update_email_and_persist_it_without_changing_the_password_hash()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var account = await Register(client);
        var original = ReadStoredUser(application, account.Id);
        Authenticate(client, account.Token);
        var newEmail = "Changed@Example.com";
        var response = await client.PutAsJsonAsync("/User/" + account.Id,
            new { email = " " + newEmail + " ", currentPassword = account.Password });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var stored = ReadStoredUser(application, account.Id);
        Assert.Equal(account.Id, stored.Id);
        Assert.Equal(newEmail, stored.Email);
        Assert.Equal(newEmail.ToUpperInvariant(), stored.NormalizedEmail);
        Assert.Equal(original.PasswordHash, stored.PasswordHash);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, account.Email, account.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, " changed@example.com ", account.Password)).StatusCode);
        var me = await client.GetFromJsonAsync<JsonElement>("/User/me");
        Assert.Equal(newEmail, me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Owner_can_delete_after_reauthentication_and_deleted_tokens_cannot_read_or_mutate_the_account()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var account = await Register(client);
        Authenticate(client, account.Token);
        var response = await Delete(client, account.Id, new { currentPassword = account.Password });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = application.Services.CreateScope())
            Assert.Empty(scope.ServiceProvider.GetRequiredService<AppDbContext>().Users);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/User/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, account.Email, account.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/User/" + account.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/User/" + account.Id,
            new { email = "changed@example.com", currentPassword = account.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Delete(client, account.Id, new { currentPassword = account.Password })).StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Account_endpoints_require_authentication(string method)
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var response = await SendAccountRequest(client, method, 1,
            new { email = "valid@example.com", currentPassword = "Password123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task A_user_cannot_read_or_mutate_another_account(string method)
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var owner = await Register(client);
        var other = await Register(client);
        var original = ReadStoredUser(application, other.Id);
        Authenticate(client, owner.Token);
        var body = method == "DELETE"
            ? (object)new { currentPassword = other.Password }
            : new { email = "changed@example.com", currentPassword = other.Password, newPassword = "AnotherPassword123!" };
        var response = await SendAccountRequest(client, method, other.Id, body);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var stored = ReadStoredUser(application, other.Id);
        Assert.Equal(original.Email, stored.Email);
        Assert.Equal(original.PasswordHash, stored.PasswordHash);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Wrong_current_password_returns_generic_unauthorized_without_mutation(string method)
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var account = await Register(client);
        var original = ReadStoredUser(application, account.Id);
        Authenticate(client, account.Token);
        var body = method == "DELETE"
            ? (object)new { currentPassword = "IncorrectPassword123!" }
            : new { email = "changed@example.com", currentPassword = "IncorrectPassword123!", newPassword = "AnotherPassword123!" };
        var response = await SendAccountRequest(client, method, account.Id, body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var failedLogin = await Login(client, account.Email, "IncorrectPassword123!");
        Assert.Equal(await failedLogin.Content.ReadAsStringAsync(), await response.Content.ReadAsStringAsync());
        var stored = ReadStoredUser(application, account.Id);
        Assert.Equal(original.Email, stored.Email);
        Assert.Equal(original.PasswordHash, stored.PasswordHash);
    }

    [Fact]
    public async Task Explicit_null_new_password_preserves_the_exact_existing_hash()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var account = await Register(client);
        var original = ReadStoredUser(application, account.Id);
        Authenticate(client, account.Token);
        var response = await client.PutAsJsonAsync("/User/" + account.Id,
            new { email = account.Email, currentPassword = account.Password, newPassword = (string?)null });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(original.PasswordHash, ReadStoredUser(application, account.Id).PasswordHash);
    }

    [Fact]
    public async Task Password_update_hashes_the_exact_new_password_and_changes_login_credentials()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var account = await Register(client);
        var original = ReadStoredUser(application, account.Id);
        Authenticate(client, account.Token);
        const string newPassword = "  NewPassword123!  ";
        var response = await client.PutAsJsonAsync("/User/" + account.Id,
            new { email = account.Email, currentPassword = account.Password, newPassword });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = ReadStoredUser(application, account.Id);
        Assert.NotEqual(original.PasswordHash, stored.PasswordHash);
        Assert.NotEqual(newPassword, stored.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, stored.PasswordHash));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, account.Email, account.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, account.Email, newPassword.Trim())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, account.Email, newPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/User/me")).StatusCode);
    }

    [Fact]
    public async Task An_email_used_by_another_account_returns_conflict_without_changing_email_or_hash()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var owner = await Register(client);
        var other = await Register(client);
        var original = ReadStoredUser(application, owner.Id);
        Authenticate(client, owner.Token);
        var response = await client.PutAsJsonAsync("/User/" + owner.Id,
            new { email = " " + other.Email.ToUpperInvariant() + " ", currentPassword = owner.Password, newPassword = "AnotherPassword123!" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var stored = ReadStoredUser(application, owner.Id);
        Assert.Equal(original.Email, stored.Email);
        Assert.Equal(original.PasswordHash, stored.PasswordHash);
    }

    [Theory]
    [MemberData(nameof(InvalidMutations))]
    public async Task Invalid_mutation_bodies_do_not_change_the_account(string method, string invalidField)
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var account = await Register(client);
        var original = ReadStoredUser(application, account.Id);
        Authenticate(client, account.Token);
        var body = new Dictionary<string, object?> { ["currentPassword"] = account.Password };
        if (method == "PUT") body["email"] = account.Email;
        switch (invalidField)
        {
            case "missing-email": body.Remove("email"); break;
            case "invalid-email": body["email"] = "invalid-email"; break;
            case "missing-current": body.Remove("currentPassword"); break;
            case "blank-current": body["currentPassword"] = "        "; break;
            case "long-current": body["currentPassword"] = new string('á', 37); break;
            case "empty-new": body["newPassword"] = ""; break;
            case "blank-new": body["newPassword"] = "        "; break;
            case "short-new": body["newPassword"] = "short"; break;
            case "long-new": body["newPassword"] = new string('á', 37); break;
            case "hash": body["passwordHash"] = "client-hash"; break;
            case "id": body["id"] = account.Id + 1; break;
            case "normalized-email": body["normalizedEmail"] = "other@example.com"; break;
        }
        var response = await SendAccountRequest(client, method, account.Id, body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = ReadStoredUser(application, account.Id);
        Assert.Equal(original.Email, stored.Email);
        Assert.Equal(original.PasswordHash, stored.PasswordHash);
    }

    public static IEnumerable<object[]> InvalidMutations()
    {
        foreach (var field in new[] { "missing-email", "invalid-email", "missing-current", "blank-current", "long-current",
            "empty-new", "blank-new", "short-new", "long-new", "hash", "id", "normalized-email" })
            yield return ["PUT", field];
        foreach (var field in new[] { "missing-current", "blank-current", "long-current", "hash", "id", "normalized-email" })
            yield return ["DELETE", field];
    }

    [Fact]
    public async Task A_new_password_of_exactly_72_utf8_bytes_can_be_used_to_login()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var account = await Register(client);
        Authenticate(client, account.Token);
        var newPassword = new string('á', 36);
        var response = await client.PutAsJsonAsync("/User/" + account.Id,
            new { email = account.Email, currentPassword = account.Password, newPassword });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, account.Email, newPassword)).StatusCode);
    }

    [Fact]
    public async Task A_deleted_accounts_token_cannot_update_or_delete_another_account()
    {
        using var application = new AuthApiFactory();
        using var client = application.CreateClient();
        var owner = await Register(client);
        var other = await Register(client);
        Authenticate(client, owner.Token);
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(client, owner.Id, new { currentPassword = owner.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/User/" + other.Id,
            new { email = "changed@example.com", currentPassword = other.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Delete(client, other.Id, new { currentPassword = other.Password })).StatusCode);
        Assert.Equal(other.Email, ReadStoredUser(application, other.Id).Email);
    }

    [Fact]
    public async Task Swagger_keeps_registration_and_login_public_and_marks_account_operations_as_protected()
    {
        using var application = new AuthApiFactory();
        using var development = application.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = development.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = json.RootElement.GetProperty("paths");
        Assert.False(paths.GetProperty("/User").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/User/login").GetProperty("post").TryGetProperty("security", out _));
        foreach (var method in new[] { "get", "put", "delete" })
            Assert.True(paths.GetProperty("/User/{id}").GetProperty(method).GetProperty("security")[0].TryGetProperty("Bearer", out _));
    }

    [Theory]
    [InlineData("update")]
    [InlineData("delete")]
    public void A_stale_verified_password_cannot_mutate_a_concurrently_changed_account(string operation)
    {
        using var database = new TestDatabase();
        var originalHash = BCrypt.Net.BCrypt.HashPassword("OriginalPassword123!", 12);
        var changedHash = BCrypt.Net.BCrypt.HashPassword("ConcurrentPassword123!", 12);
        long id;
        using (var setup = database.CreateContext())
            id = new UserDAO(setup).Create("original@example.com", originalHash).Id;

        using var verifiedContext = database.CreateContext();
        var verifiedDAO = new UserDAO(verifiedContext);
        var verified = verifiedDAO.GetById(id)!;
        Assert.True(BCrypt.Net.BCrypt.Verify("OriginalPassword123!", verified.PasswordHash));
        using (var concurrentContext = database.CreateContext())
            Assert.True(new UserDAO(concurrentContext).Update(id, "concurrent@example.com", originalHash, changedHash));

        var changed = operation == "update"
            ? verifiedDAO.Update(id, "stale@example.com", verified.PasswordHash, null)
            : verifiedDAO.Delete(id, verified.PasswordHash);
        Assert.False(changed);
        using var verify = database.CreateContext();
        var stored = new UserDAO(verify).GetById(id)!;
        Assert.Equal("concurrent@example.com", stored.Email);
        Assert.Equal(changedHash, stored.PasswordHash);
    }

    private static async Task<Account> Register(HttpClient client)
    {
        var email = "account." + Guid.NewGuid().ToString("N") + "@example.com";
        const string password = "OriginalPassword123!";
        var response = await client.PostAsJsonAsync("/User", new { email, password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var login = await Login(client, email, password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var session = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return new Account(user.RootElement.GetProperty("id").GetInt64(), email, password,
            session.RootElement.GetProperty("token").GetString()!, response.Headers.Location!.ToString());
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/User/login", new { email, password });

    private static Task<HttpResponseMessage> Delete(HttpClient client, long id, object body) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/User/" + id) { Content = JsonContent.Create(body) });

    private static Task<HttpResponseMessage> SendAccountRequest(HttpClient client, string method, long id, object body) =>
        method switch
        {
            "GET" => client.GetAsync("/User/" + id),
            "PUT" => client.PutAsJsonAsync("/User/" + id, body),
            _ => Delete(client, id, body)
        };

    private static void Authenticate(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static User ReadStoredUser(AuthApiFactory application, long id)
    {
        using var scope = application.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AsNoTracking().Single(user => user.Id == id);
    }

    private record Account(long Id, string Email, string Password, string Token, string Location);
}
