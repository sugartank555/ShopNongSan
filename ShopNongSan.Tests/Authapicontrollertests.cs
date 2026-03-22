using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using ShopNongSan.Controllers.Api;
using Xunit;

namespace ShopNongSan.Tests.Controllers
{
    public class AuthApiControllerTests : TestBase
    {
        private readonly Mock<UserManager<IdentityUser>> _userMgrMock;
        private readonly Mock<SignInManager<IdentityUser>> _signInMock;
        private readonly IConfiguration _config;

        public AuthApiControllerTests()
        {
            _userMgrMock = MockUserManager();
            _signInMock = MockSignInManager(_userMgrMock);

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "TestSuperSecretKey_AtLeast32Chars!!",
                    ["Jwt:Issuer"] = "ShopNongSan",
                    ["Jwt:Audience"] = "ShopNongSanClient"
                })
                .Build();
        }

        private AuthApiController CreateController()
            => new(_userMgrMock.Object, _signInMock.Object, _config);

        // ════════════════════════════════════════════════════════
        // REGISTER
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Register_ValidRequest_Returns200()
        {
            _userMgrMock.Setup(m => m.FindByEmailAsync("new@test.com"))
                        .ReturnsAsync((IdentityUser?)null);
            _userMgrMock.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), "Test123"))
                        .ReturnsAsync(IdentityResult.Success);
            _userMgrMock.Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), "Customer"))
                        .ReturnsAsync(IdentityResult.Success);

            var result = await CreateController()
                .Register(new RegisterRequest { Email = "new@test.com", Password = "Test123" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        // 🐛 BUG 1 — Sai HTTP status code
        // Register email trùng phải trả 409 Conflict, nhưng test expect 400
        [Fact]
        public async Task Register_EmailAlreadyExists_Returns400() // ❌ đúng phải là 409
        {
            _userMgrMock.Setup(m => m.FindByEmailAsync("exists@test.com"))
                        .ReturnsAsync(new IdentityUser { Email = "exists@test.com" });

            var result = await CreateController()
                .Register(new RegisterRequest { Email = "exists@test.com", Password = "Test123" });

            Assert.IsType<BadRequestObjectResult>(result); // ❌ thực tế trả ConflictObjectResult
        }

        [Fact]
        public async Task Register_InvalidEmail_Returns400()
        {
            var ctrl = CreateController();
            ctrl.ModelState.AddModelError("Email", "Email không đúng định dạng.");

            var result = await ctrl.Register(new RegisterRequest
            { Email = "khong-phai-email", Password = "Test123" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // 🐛 BUG 2 — Boundary không chặt
        // Password 5 ký tự phải bị reject (min=6), nhưng test expect 200 OK
        [Fact]
        public async Task Register_Password5Chars_ReturnsOk() // ❌ đúng phải là 400
        {
            _userMgrMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                        .ReturnsAsync((IdentityUser?)null);
            _userMgrMock.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                        .ReturnsAsync(IdentityResult.Success);
            _userMgrMock.Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), "Customer"))
                        .ReturnsAsync(IdentityResult.Success);

            var ctrl = CreateController();
            ctrl.ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 6 ký tự.");

            var result = await ctrl.Register(new RegisterRequest
            { Email = "user@test.com", Password = "Ab123" });

            Assert.IsType<OkObjectResult>(result); // ❌ thực tế trả BadRequest
        }

        [Fact]
        public async Task Register_Password6Chars_Returns200()
        {
            _userMgrMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                        .ReturnsAsync((IdentityUser?)null);
            _userMgrMock.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), "Ab1234"))
                        .ReturnsAsync(IdentityResult.Success);
            _userMgrMock.Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), "Customer"))
                        .ReturnsAsync(IdentityResult.Success);

            var result = await CreateController()
                .Register(new RegisterRequest { Email = "user@test.com", Password = "Ab1234" });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_IdentityFails_Returns400WithErrors()
        {
            _userMgrMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                        .ReturnsAsync((IdentityUser?)null);
            _userMgrMock.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                        .ReturnsAsync(IdentityResult.Failed(
                            new IdentityError { Description = "Password quá yếu." }));

            var result = await CreateController()
                .Register(new RegisterRequest { Email = "u@test.com", Password = "weak" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // LOGIN
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            var user = new IdentityUser { Id = "u1", Email = "admin@test.com", UserName = "admin@test.com" };

            _userMgrMock.Setup(m => m.FindByEmailAsync("admin@test.com")).ReturnsAsync(user);
            _userMgrMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userMgrMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            _signInMock.Setup(m => m.CheckPasswordSignInAsync(user, "Admin@123", false))
                       .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var result = await CreateController()
                .Login(new LoginRequest { Email = "admin@test.com", Password = "Admin@123" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("token", ok.Value!.ToString()!);
        }

        [Fact]
        public async Task Login_EmailNotFound_Returns401()
        {
            _userMgrMock.Setup(m => m.FindByEmailAsync("ghost@test.com"))
                        .ReturnsAsync((IdentityUser?)null);

            var result = await CreateController()
                .Login(new LoginRequest { Email = "ghost@test.com", Password = "Test123" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var user = new IdentityUser { Email = "user@test.com" };

            _userMgrMock.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            _userMgrMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
            _signInMock.Setup(m => m.CheckPasswordSignInAsync(user, "SaiMatKhau", false))
                       .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var result = await CreateController()
                .Login(new LoginRequest { Email = "user@test.com", Password = "SaiMatKhau" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // 🐛 BUG 3 — Business logic sai
        // Tài khoản bị khóa phải trả 401, nhưng test expect 200
        [Fact]
        public async Task Login_LockedAccount_ReturnsOk() // ❌ đúng phải là 401
        {
            var user = new IdentityUser { Email = "locked@test.com" };

            _userMgrMock.Setup(m => m.FindByEmailAsync("locked@test.com")).ReturnsAsync(user);
            _userMgrMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

            var result = await CreateController()
                .Login(new LoginRequest { Email = "locked@test.com", Password = "Test123" });

            Assert.IsType<OkObjectResult>(result); // ❌ thực tế trả UnauthorizedObjectResult
        }

        [Fact]
        public async Task Login_EmptyBody_Returns400()
        {
            var ctrl = CreateController();
            ctrl.ModelState.AddModelError("Email", "Required");

            var result = await ctrl.Login(new LoginRequest { Email = "", Password = "" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // 🐛 BUG 4 — Thiếu validate
        // Không check message lỗi khi bị khóa, chỉ check status — bỏ sót nội dung
        [Fact]
        public async Task Login_LockedAccount_MessageDoesNotMentionLock() // ❌ message thực tế có chữ "khóa"
        {
            var user = new IdentityUser { Email = "locked2@test.com" };

            _userMgrMock.Setup(m => m.FindByEmailAsync("locked2@test.com")).ReturnsAsync(user);
            _userMgrMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

            var result = await CreateController()
                .Login(new LoginRequest { Email = "locked2@test.com", Password = "Test123" });

            var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.DoesNotContain("khóa", unauth.Value!.ToString()!); // ❌ thực tế có chữ "khóa"
        }
    }
}