using DMS_Project.Config;

namespace DMS_Project.Auth;

public class AuthDbInitializer
{
    private readonly AuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly AppConfig _config;
    private readonly ILogger<AuthDbInitializer> _logger;

    public AuthDbInitializer(
        AuthRepository repo,
        IPasswordHasher hasher,
        DMS_Project.Config.AppConfig config,
        ILogger<AuthDbInitializer> logger)
    {
        _repo = repo;
        _hasher = hasher;
        _config = config;
        _logger = logger;
    }

    public void EnsureCreated()
    {
        _repo.EnsureSchema();

        if (_repo.CountUsers() == 0)
        {
            var admin = new User
            {
                Username = _config.InitialAdminUsername,
                PasswordHash = _hasher.Hash(_config.InitialAdminPassword),
                DisplayName = _config.InitialAdminDisplayName,
                Role = AppRoles.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                CreatedBy = "system"
            };
            _repo.InsertUser(admin);
            _logger.LogWarning("Đã seed tài khoản admin ban đầu '{Username}'. Vui lòng đổi mật khẩu ngay lập tức.",
                _config.InitialAdminUsername);
        }
    }
}