using DotLearn.Auth.Models.DTOs;
using DotLearn.Auth.Models.Entities;
using DotLearn.Auth.Repositories;
using DotLearn.Auth.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DotLearn.Auth.Tests;

[TestClass]
public class AuthServiceTests
{
    private Mock<IUserRepository> _repoMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private IAuthService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repoMock = new Mock<IUserRepository>();
        _configMock = new Mock<IConfiguration>();
        // Provide a long enough secret for HS256
        _configMock.Setup(c => c["Jwt:Secret"])
            .Returns("test-secret-key-that-is-at-least-32-characters-long!");
        _service = new AuthService(_repoMock.Object, _configMock.Object);
    }

    [TestMethod]
    public async Task RegisterAsync_Success_ReturnsTokens()
    {
        _repoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(new RegisterRequestDto(
            "Test User", "test@example.com", "Password@123"));

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.AccessToken);
        Assert.IsNotNull(result.RefreshToken);
    }

    [TestMethod]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("existing@example.com"))
            .ReturnsAsync(true);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            _service.RegisterAsync(new RegisterRequestDto(
                "Test", "existing@example.com", "Password@123")));
    }

    [TestMethod]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Role = "Student",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
            });

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginRequestDto("test@example.com", "WrongPassword")));
    }

    [TestMethod]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized()
    {
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginRequestDto("unknown@example.com", "Password@123")));
    }

    [TestMethod]
    public async Task RefreshTokenAsync_ExpiredToken_ThrowsUnauthorized()
    {
        _repoMock.Setup(r => r.GetByRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            _service.RefreshTokenAsync("expired-token"));
    }
}
