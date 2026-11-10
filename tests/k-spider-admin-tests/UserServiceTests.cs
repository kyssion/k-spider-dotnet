using KSpider.Admin.Auth;
using KSpider.Admin.Repository.Models;
using KSpider.Admin.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class UserServiceTests
{
    [TestMethod]
    public async Task CreateRejectsInvalidUsernameAndPassword()
    {
        var harness = NewHarness();

        Assert.AreEqual(
            UserMutationOutcome.ValidationError,
            await harness.Service.CreateAsync(new UserCreateRequest("a", "long-enough", null), 1, default));
        Assert.AreEqual(
            UserMutationOutcome.ValidationError,
            await harness.Service.CreateAsync(new UserCreateRequest("valid_user", "short", null), 1, default));
    }

    [TestMethod]
    public async Task CreateHashesPasswordAndStoresUser()
    {
        var harness = NewHarness();

        var outcome = await harness.Service.CreateAsync(
            new UserCreateRequest("new_user", "long-enough", "New User"),
            1,
            CancellationToken.None);

        Assert.AreEqual(UserMutationOutcome.Success, outcome);
        var user = harness.Repository.Users.Single(item => item.Username == "new_user");
        Assert.AreNotEqual("long-enough", user.PasswordHash);
        Assert.AreEqual("New User", user.DisplayName);
        Assert.AreEqual("active", user.Status);
        Assert.AreEqual(
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success,
            new PasswordService().Verify(user, "long-enough"));
    }

    [TestMethod]
    public async Task CannotDisableSelf()
    {
        var harness = NewHarness();

        var outcome = await harness.Service.PatchAsync(
            1,
            new UserPatchRequest(null, "disabled"),
            1,
            CancellationToken.None);

        Assert.AreEqual(UserMutationOutcome.Forbidden, outcome);
        Assert.AreEqual("active", harness.Repository.Users.Single(user => user.Id == 1).Status);
    }

    [TestMethod]
    public async Task CannotDisableLastActiveAccount()
    {
        var harness = NewHarness();
        harness.Repository.Seed(User(2, "other", "disabled"));

        var outcome = await harness.Service.PatchAsync(
            1,
            new UserPatchRequest(null, "disabled"),
            999,
            CancellationToken.None);

        Assert.AreEqual(UserMutationOutcome.Conflict, outcome);
        Assert.AreEqual("active", harness.Repository.Users.Single(user => user.Id == 1).Status);
    }

    [TestMethod]
    public async Task CannotResetOwnPasswordThroughUserEndpoint()
    {
        var harness = NewHarness();

        var outcome = await harness.Service.ResetPasswordAsync(
            1,
            "new-password",
            1,
            CancellationToken.None);

        Assert.AreEqual(UserMutationOutcome.Forbidden, outcome);
    }

    private static Harness NewHarness()
    {
        var repository = new FakeUserRepository();
        var passwordService = new PasswordService();
        var admin = User(1, "admin", "active");
        repository.Seed(admin with { PasswordHash = passwordService.Hash(admin, "admin") });
        var service = new UserService(
            repository,
            passwordService,
            NullLogger<UserService>.Instance);
        return new Harness(service, repository);
    }

    private static SysUserRecord User(long id, string username, string status)
    {
        var now = DateTime.UtcNow;
        return new SysUserRecord(id, username, "hash", null, status, now, now);
    }

    private sealed record Harness(UserService Service, FakeUserRepository Repository);
}
