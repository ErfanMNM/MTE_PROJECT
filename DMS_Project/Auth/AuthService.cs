using DMS_Project.Audit;

namespace DMS_Project.Auth;

public interface IAuthService
{
    LoginResponse Login(LoginRequest req);
    User? GetCurrentUser(int userId);
    List<UserDto> ListUsers();
    UserDto? CreateUser(CreateUserRequest req, int actorUserId);
    UserDto? UpdateUser(int id, UpdateUserRequest req);
    bool ChangePassword(int targetUserId, string newPassword, bool isAdminReset);
    bool ChangeOwnPassword(int userId, string oldPassword, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly AuthRepository _repo;
    private readonly JwtTokenService _jwt;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthRepository repo,
        JwtTokenService jwt,
        IPasswordHasher hasher,
        IAuditService audit,
        ILogger<AuthService> logger)
    {
        _repo = repo;
        _jwt = jwt;
        _hasher = hasher;
        _audit = audit;
        _logger = logger;
    }

    public LoginResponse Login(LoginRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            return new LoginResponse { Success = false, Message = "Username và password là bắt buộc" };
        }

        var user = _repo.FindByUsername(req.Username.Trim());
        if (user == null || !_hasher.Verify(req.Password, user.PasswordHash))
        {
            _audit.RecordFailureAsync("Auth.LoginFailed", AuditEntityTypes.User, req.Username,
                "Invalid username or password").GetAwaiter().GetResult();
            return new LoginResponse { Success = false, Message = "Sai tài khoản hoặc mật khẩu" };
        }

        if (!user.IsActive)
        {
            _audit.RecordFailureAsync("Auth.LoginFailed", AuditEntityTypes.User, user.Username,
                "User is disabled").GetAwaiter().GetResult();
            return new LoginResponse { Success = false, Message = "Tài khoản đã bị vô hiệu hóa" };
        }

        var (token, expires) = _jwt.Issue(user);
        var ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        _repo.UpdateLastLogin(user.Id, ts);

        _audit.RecordSuccessAsync("Auth.Login", AuditEntityTypes.User, user.Username,
            before: null, after: ToDto(user), changedFieldsJson: null).GetAwaiter().GetResult();

        return new LoginResponse
        {
            Success = true,
            Token = token,
            ExpiresAt = expires,
            Message = "OK",
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = ts
            }
        };
    }

    public User? GetCurrentUser(int userId) => _repo.FindById(userId);

    public List<UserDto> ListUsers() => _repo.ListAll().Select(ToDto).ToList();

    public UserDto? CreateUser(CreateUserRequest req, int actorUserId)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return null;

        if (_repo.FindByUsername(req.Username.Trim()) != null)
            return null;

        if (req.Role != AppRoles.Admin && req.Role != AppRoles.Operator && req.Role != AppRoles.Viewer)
            return null;

        var newUser = new User
        {
            Username = req.Username.Trim(),
            PasswordHash = _hasher.Hash(req.Password),
            DisplayName = req.DisplayName?.Trim() ?? req.Username.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            Role = req.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CreatedBy = "user#" + actorUserId
        };
        _repo.InsertUser(newUser);

        _audit.RecordSuccessAsync("Auth.UserCreated", AuditEntityTypes.User, newUser.Username,
            before: null, after: ToDto(newUser), changedFieldsJson: null).GetAwaiter().GetResult();

        return ToDto(newUser);
    }

    public UserDto? UpdateUser(int id, UpdateUserRequest req)
    {
        var existing = _repo.FindById(id);
        if (existing == null) return null;

        var before = ToDto(existing);

        var displayName = req?.DisplayName;
        var email = req?.Email;
        var role = req?.Role;
        var isActive = req?.IsActive;

        if (role != null && role != AppRoles.Admin && role != AppRoles.Operator && role != AppRoles.Viewer)
            return null;

        _repo.UpdateUser(id, displayName, email, role, isActive);

        var updated = _repo.FindById(id);
        if (updated == null) return null;

        var after = ToDto(updated);
        var changed = new List<string>();
        if (displayName != null && before.DisplayName != after.DisplayName) changed.Add(nameof(UserDto.DisplayName));
        if (email != null && before.Email != after.Email) changed.Add(nameof(UserDto.Email));
        if (role != null && before.Role != after.Role) changed.Add(nameof(UserDto.Role));
        if (isActive.HasValue && before.IsActive != after.IsActive) changed.Add(nameof(UserDto.IsActive));

        _audit.RecordSuccessAsync("Auth.UserUpdated", AuditEntityTypes.User, updated.Username,
            before: before, after: after, changedFieldsJson: changed.Count == 0 ? null : string.Join(",", changed))
            .GetAwaiter().GetResult();

        return after;
    }

    public bool ChangePassword(int targetUserId, string newPassword, bool isAdminReset)
    {
        var existing = _repo.FindById(targetUserId);
        if (existing == null) return false;

        _repo.UpdatePassword(targetUserId, _hasher.Hash(newPassword));

        _audit.RecordSuccessAsync("Auth.PasswordChanged", AuditEntityTypes.User, existing.Username,
            before: null, after: new { existing.Username, ResetByAdmin = isAdminReset },
            changedFieldsJson: null).GetAwaiter().GetResult();
        return true;
    }

    public bool ChangeOwnPassword(int userId, string oldPassword, string newPassword)
    {
        var existing = _repo.FindById(userId);
        if (existing == null) return false;
        if (!_hasher.Verify(oldPassword, existing.PasswordHash)) return false;

        _repo.UpdatePassword(userId, _hasher.Hash(newPassword));

        _audit.RecordSuccessAsync("Auth.PasswordChanged", AuditEntityTypes.User, existing.Username,
            before: null, after: new { existing.Username, SelfService = true },
            changedFieldsJson: null).GetAwaiter().GetResult();
        return true;
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        DisplayName = u.DisplayName,
        Email = u.Email,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        LastLoginAt = u.LastLoginAt
    };
}