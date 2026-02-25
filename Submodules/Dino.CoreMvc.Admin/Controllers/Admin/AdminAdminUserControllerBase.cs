using Microsoft.AspNetCore.Mvc;
using Dino.Core.AdminBL.Models;
using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin.Entities;
using Dino.Common.Security;
using Dino.CoreMvc.Common.Helpers;
using System.Net.Mail;
using Dino.Core.AdminBL.Settings;
using Dino.Infra.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.CoreMvc.Admin.ModelsSettings;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dino.Common.Helpers;
using Dino.CoreMvc.Admin.Attributes.Permissions;

namespace Dino.CoreMvc.Admin.Controllers.Admin
{
    [AdminPermission((short)RoleType.DinoAdmin)]
    public class AdminAdminUserControllerBase<TAdminUserModel, TAdminUser, TAdminRole> : DinoAdminBaseEntityController<TAdminUserModel, TAdminUser, int>
        where TAdminUserModel : AdminAdminUserModelBase, new()
        where TAdminUser : AdminUserBase<TAdminUser, TAdminRole>, new()
        where TAdminRole : AdminRoleBase<TAdminRole, TAdminUser>, new()
    {
        protected IOptions<AdminConfig> AdminConfig => HttpContext?.RequestServices.GetService(typeof(IOptions<AdminConfig>)) as IOptions<AdminConfig>;
        
        private readonly Random _random = new Random();
        private int OTP_EXPIRY_MINUTES => AdminConfig?.Value?.LoginSecurityConfig.OtpExpiryMinutes ?? 15;
        
        private class PasswordChangeEmailGenerator : IEmailGenerator
        {
            private readonly string _subject;
            private readonly string _message;
            
            public PasswordChangeEmailGenerator(string subject, string message)
            {
                _subject = subject;
                _message = message;
            }
            
            public string BuildMessage() => _message;
            public string BuildSubject() => _subject;
        }

        public AdminAdminUserControllerBase() : base("admin_users")
        {
        }

        protected override async Task<AdminSegment> CreateAdminSegment()
        {
            return new AdminSegment
            {
                General = new AdminSegmentGeneral
                {
                    Name = "מנהלים",
                    Priority = 2
                },
                UI = new AdminSegmentUI
                {
                    Icon = "user",
                    IconType = IconType.PrimeIcons,
                    ShowInMenu = true,
                },
                Navigation = new AdminSegmentNavigation
                {
                    CustomPath = null,
                },
            };
        }

        protected override async Task<ListDef> CreateListDef(string refId = null)
        {
            return new ListDef
            {
                Title = "Users List",
                AllowReOrdering = false,
                AllowAdd = true,
                AllowEdit = true,
                AllowDelete = true,
                ShowArchive = true,
                ShowDeleteConfirmation = true,
            };
        }

        protected override async Task<TAdminUser> GetEntityById(string id)
        {
            var entity = await base.GetEntityById(id);
            if (GetCurrentAdminUserRoleType() != (short)RoleType.DinoAdmin)
            {
                entity.RoleId = (int)RoleType.RegularAdmin;
            }
            return entity;
        }

        protected override IQueryable<TAdminUser> GetFilteredData(string refId, bool? showArchive, bool? showDeleted, ListRetrieveParams listParams)
        {
            // Return all users - role masking will be handled in the List method override
            var data = base.GetFilteredData(refId, showArchive, showDeleted, listParams);

            // If the user is not dino admin, replace the role with regular admin.
            if (GetCurrentAdminUserRoleType() != (short)RoleType.DinoAdmin)
            {
                foreach (var item in data)
                {
                    item.RoleId = (int)RoleType.RegularAdmin;
                }
            }

            return data;
        }
        

        // Method called by AdminFieldSelect with SelectSourceType.Function to filter available roles
        public Dictionary<int, string> GetAvailableRoles()
        {
            var currentUserRoleType = GetCurrentAdminUserRoleType();
            var query = DbContext.Set<TAdminRole>().AsQueryable();

            // If user is not DinoAdmin, filter out DinoAdmin roles from dropdown
            if (currentUserRoleType != (short)RoleType.DinoAdmin)
            {
                query = query.Where(r => r.RoleType != (short)RoleType.DinoAdmin);
            }

            return query.ToDictionary(r => r.Id, r => r.Name);
        }

        protected override async Task RunCustomBeforeSave(string id, TAdminUserModel model, TAdminUser efModel)
        {
            if (model.Password.IsNotNullOrEmpty())
            {
                var salt = PasswordTools.GenerateRandomSalt();
                var hash = PasswordTools.GenerateSaltedHashSHA256(model.Password, salt);

                efModel.PasswordSalt = salt;
                efModel.PasswordHash = hash;
            }

            var currentUserRoleType = GetCurrentAdminUserRoleType();

            // Get the role being assigned
            var assignedRole = await DbContext.Set<TAdminRole>().FindAsync(model.RoleId);
            if (assignedRole != null)
            {
                // Prevent assigning higher-level roles than current user has
                if (assignedRole.RoleType < currentUserRoleType) // Lower number = higher privilege
                {
                    // But only if that's not the existing role.
                    var user = DbContext.Set<TAdminUser>().AsNoTracking().FirstOrDefault(u => u.Id == int.Parse(id));
                    if (user.RoleId != assignedRole.Id)
                    {
                        throw new InvalidOperationException(
                            "You cannot assign a role with higher privileges than your own role.");
                    }
                }
            }

            await base.RunCustomBeforeSave(id, model, efModel);
        }

        protected override async Task RunCustomBeforeMapping(string id, TAdminUserModel model)
        {
            await base.RunCustomBeforeMapping(id, model);

            // Check if the user is not a DinoAdmin.
            if (GetCurrentAdminUserRoleType() != (short)RoleType.DinoAdmin)
            {
                // And if we're not editing...
                if (id.IsNotNullOrEmpty())
                {
                    // Check if the current user, that is NOT a DinoAdmin, is trying to edit a DinoAdmin user.
                    // Note we are not using the regular GetEntityById method, because it will return the user with the role of the current user.
                    var user = DbContext.Set<TAdminUser>().AsNoTracking().FirstOrDefault(u => u.Id == int.Parse(id));
                    if (user.RoleId == (int)RoleType.DinoAdmin)
                    {
                        // Don't change the role - keep the user as DinoAdmin
                        // Return success but don't actually modify the role
                        model.RoleId = user.RoleId; // Reset to original role
                    }
                }
            }
        }

        #region Login

        protected async Task PerformLoginAsync(TAdminUser user)
        {
            var additionalClaims = new Dictionary<string, string>
            {
                { "UserRoleIdentifier", user.Role.Id.ToString() },
                { "UserRoleType", user.Role.RoleType.ToString() }
            };
            await AccountHelpers.LoginToRole(HttpContext, user.Role.Name, "Id", user.Id.ToString(), true, true, additionalClaims);
        }

        [HttpPost]
        public async Task<JsonResult> login([FromBody] LoginModel login, string refId = null)
        {
            bool res = false;
            string error = null;

            try
            {
                TAdminUser? user = await RetrieveAdminUserByEmail(login.email);
                if (user == null)
                {
                    error = "user is not exist";
                    return CreateJsonResponse(false, null, error);
                }

                var userHashPassword = user.PasswordHash;
                var useralt = user.PasswordSalt;
                var isPasswordCorrect = PasswordTools.ComparePasswordsSHA256(login.password, userHashPassword, useralt);

                if (!isPasswordCorrect)
                {
                    error = "password is incorrect";
                    return CreateJsonResponse(false, null, error);
                }
                var settings = AppSettings.Get<DinoMasterSettingsBase>();

                // Check if OTP is required for login
                if (settings?.RequireOtpOnLogin == true)     // NULL value is false, so we can log in the first time.
                {
                    // Generate and store OTP
                    var (result, otp) = await CreateAndStoreOTP(login.email);
                    if (result && otp != null)
                    {
                        // Send OTP email
                        await SendLoginOtpEmail(login.email, otp);
                        
                        // Return special response indicating OTP is required
                        return CreateJsonResponse(true, new { requireOtp = true }, null);
                    }
                    else
                    {
                        error = "Failed to generate OTP";
                        return CreateJsonResponse(false, null, error);
                    }
                }
                else
                {
                    // No OTP required, proceed with normal login
                    await PerformLoginAsync(user);
                    res = true;
                }
            }
            catch (Exception ex)
            {
                res = false;
                error = $"Something went wrong in server while login: {ex.Message}";
            }

            return CreateJsonResponse(res, null, error);
        }

        [HttpPost]
        public async Task<JsonResult> verifyLoginOtp([FromBody] VerifyLoginOtpModel model)
        {
            try
            {
                // Find user by email
                TAdminUser? user = await RetrieveAdminUserByEmail(model.email);
                if (user == null)
                {
                    return CreateJsonResponse(false, null, "User not found");
                }

                // Validate OTP
                var validationResult = await ValidateOTP(user, model.otp);
                if (!validationResult.isValid)
                {
                    return CreateJsonResponse(false, null, validationResult.error);
                }

                // OTP is valid, proceed with login
                await PerformLoginAsync(user);
                
                return CreateJsonResponse(true, null, null);
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, null, $"Error verifying OTP: {ex.Message}");
            }
        }

        [HttpGet]
        public JsonResult validateAuth()
        {
            try
            {
                // Check if the user is authenticated
                bool isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
                
                return CreateJsonResponse(true, isAuthenticated, null);
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, false, ex.Message);
            }
        }
        
        [HttpPost]
        public async Task<JsonResult> forgotPassword([FromBody] ForgotPasswordModel model)
        {
            try
            {
                var(result,otp) = await CreateAndStoreOTP(model.email);

                if (!result)
                {
                    // Don't reveal that the user doesn't exist for security reasons
                    return CreateJsonResponse(true, null, null);
                }

                // Send email with OTP
                await SendOtpEmail(model.email, otp);
                return CreateJsonResponse(true, null, null);
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, null, $"Error processing request: {ex.Message}");
            }
        }
        
        [HttpPost]
        public async Task<JsonResult> VerifyOtp([FromBody] VerifyOtpModel model)
        {
            TAdminUser? entity = await RetrieveAdminUserByEmail(model.email);
            if (entity == null)
            {
                return CreateJsonResponse(false, null, "user not found");
            }
            var res = await ValidateOTP(entity, model.otp);
            return CreateJsonResponse(res.isValid, null, res.error);
        }

        protected async Task<TAdminUser?> RetrieveAdminUserByEmail(string email)
        {
            var entity = await DbContext.Set<TAdminUser>().FirstOrDefaultAsync(au => au.Email == email);
            return entity;
        }

        protected async Task<(bool isValid, string? error)> ValidateOTP(TAdminUser user, string otp)
        {
            (bool, string?) res = (false, null);

            try
            {
                var isExpired = user.VerificationCodeDate < DateTime.UtcNow.AddMinutes(-OTP_EXPIRY_MINUTES);
                var isOtpEquals = user.EmailVerificationCode == otp;
                if (!isOtpEquals || isExpired)
                {
                    res = (false, "Invalid or expired OTP");
                }
                else
                {
                    res = (true, null);
                }

                user.EmailVerificationCode = null;
                user.VerificationCodeDate = null;
                await DbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                res = (false, $"Error processing request: {ex.Message}");
            }

            return res;
        }

        protected async Task<(bool result, string? otp)> CreateAndStoreOTP(string email)
        {
            (bool, string?) res = (false, null);

            try
            {
                var entity = await RetrieveAdminUserByEmail(email);

                if (entity == null)
                {
                    res = (false, null);
                    return res;
                }

                string otp = GenerateOtp();
                entity.EmailVerificationCode = otp;
                entity.VerificationCodeDate = DateTime.UtcNow;
                await DbContext.SaveChangesAsync();

                res = (true, otp);
            }
            catch (Exception ex)
            {
                res = (false, null);
            }

            return res;
        }


        [HttpGet]
        public async Task<JsonResult> logout()
        {
            try
            {
                // Clear authentication cookies
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return CreateJsonResponse(true, null, null);
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, null, $"Error during logout: {ex.Message}");
            }
        }

        protected string GenerateOtp()
        {
            // Generate a 6-digit OTP
            return _random.Next(100000, 999999).ToString();
        }

        protected async Task SendOtpEmail(string email, string otp)
        {
            try
            {
                var subject = "Your Password Reset OTP";
                var message = $@"
                <html>
                <body>
                    <h2>Password Reset Request</h2>
                    <p>We received a request to reset your password. Please use the following OTP to reset your password:</p>
                    <h3 style='background-color: #f2f2f2; padding: 10px; text-align: center; font-size: 24px;'>{otp}</h3>
                    <p>This OTP will expire in {OTP_EXPIRY_MINUTES} minutes.</p>
                    <p>If you did not request a password reset, please ignore this email or contact support.</p>
                    <p>Thank you,<br/>Admin Team</p>
                </body>
                </html>";

                await ConfigAndSendEmail(email, subject, message);
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.LogError(ex, $"Error sending OTP email: {ex.Message}");
                // Continue execution - don't throw exception to client
            }
        }

        #endregion

        #region Password Change and Recovery

        [HttpPost]
        public async Task<JsonResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            try
            {
                if (!HttpContext.User.Identity.IsAuthenticated)
                {
                    return CreateJsonResponse(false, null, "User not authenticated");
                }
                
                // Get the current user ID from the claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "Id");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return CreateJsonResponse(false, null, "Unable to identify the current user");
                }
                
                // Find the user in the database
                var user = await DbContext.Set<TAdminUser>().FindAsync(userId);
                if (user == null)
                {
                    return CreateJsonResponse(false, null, "User not found");
                }
                
                // Verify current password
                var isCurrentPasswordValid = PasswordTools.ComparePasswordsSHA256(
                    model.currentPassword, user.PasswordHash, user.PasswordSalt);
                    
                if (!isCurrentPasswordValid)
                {
                    return CreateJsonResponse(false, null, "Current password is incorrect");
                }
                
                // Update to new password
                var updatePasswordSuccess = await UpdatePassword(user, model.newPassword);
                if (!updatePasswordSuccess)
                {
                    return CreateJsonResponse(false, null, "could not save the new password");
                }
                
                // Send confirmation email
                await SendPasswordChangedEmail(user.Email);
                
                return CreateJsonResponse(true, null, null);
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, null, $"Error changing password: {ex.Message}");
            }
        }



        [HttpPost]
        public async Task<JsonResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            try
            {
                TAdminUser? entity = await RetrieveAdminUserByEmail(model.email);
                if (entity == null)
                {
                    return CreateJsonResponse(false, null, "user not found");
                }

                var res = await ValidateOTP(entity, model.otp);

                if (!res.isValid)
                {
                    return CreateJsonResponse(false, null, res.error);
                }

                var updatePasswordSuccsses = await UpdatePassword(entity, model.newPassword);

                if (!updatePasswordSuccsses)
                {
                    return CreateJsonResponse(false, null, "could not save the new password");
                }

                // Send confirmation email
                await SendPasswordChangedEmail(model.email);

                return CreateJsonResponse(true, null, null);
            }
            catch (Exception ex)
            {
                return CreateJsonResponse(false, null, $"Error resetting password: {ex.Message}");
            }
        }

        protected async Task<bool> UpdatePassword(TAdminUser user, string newPassword)
        {
            try
            {
                // Update password
                var salt = PasswordTools.GenerateRandomSalt();
                var hash = PasswordTools.GenerateSaltedHashSHA256(newPassword, salt);

                user.PasswordSalt = salt;
                user.PasswordHash = hash;
                user.UpdateDate = DateTime.UtcNow;
                await DbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                return false;

            }

            return true;
        }

        #endregion

        #region Emails

        protected async Task SendPasswordChangedEmail(string email)
        {
            try
            {
                var subject = "Your Password Has Been Changed";
                var message = $@"
                <html>
                <body>
                    <h2>Password Change Confirmation</h2>
                    <p>Your password has been successfully changed.</p>
                    <p>If you did not make this change, please contact our support team immediately.</p>
                    <p>Thank you,<br/>Admin Team</p>
                </body>
                </html>";

                await ConfigAndSendEmail(email, subject, message);
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.LogError(ex, $"Error sending password changed email: {ex.Message}");
                // Continue execution - don't throw exception to client
            }
        }

        protected async Task SendLoginOtpEmail(string email, string otp)
        {
            try
            {
                var subject = "Your Login Verification Code";
                var message = $@"
                <html>
                <body>
                    <h2>Login Verification Code</h2>
                    <p>You are attempting to log in. Please use the following code to verify your identity:</p>
                    <h3 style='background-color: #f2f2f2; padding: 10px; text-align: center; font-size: 24px;'>{otp}</h3>
                    <p>This code will expire in {OTP_EXPIRY_MINUTES} minutes.</p>
                    <p>If you did not attempt to log in, please ignore this email or contact support.</p>
                    <p>Thank you,<br/>Admin Team</p>
                </body>
                </html>";

                await ConfigAndSendEmail(email, subject, message);
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.LogError(ex, $"Error sending login OTP email: {ex.Message}");
                // Continue execution - don't throw exception to client
            }
        }

        protected async Task ConfigAndSendEmail(string email, string subject, string message)
        {
            var emailGenerator = new PasswordChangeEmailGenerator(subject, message);

            var settings = AppSettings.Get<DinoMasterSettingsBase>();
            var emailSettings = new EmailClientSettings
            {
                SmtpHost = settings.EmailSettings.SmtpHost,
                SmtpPort = settings.EmailSettings.SmtpPort,
                SmtpUser = settings.EmailSettings.SmtpUser,
                SmtpPassword = settings.EmailSettings.SmtpPassword,
                DefaultFromAddress = new MailAddress(
                    settings.EmailSettings.FromEmail,
                    settings.EmailSettings.FromName),
                EnableSsl = settings.EmailSettings.EnableSsl
            };

            var emailClient = new EmailClient(emailSettings);
            await emailClient.SendEmailAsync(
                new[] { email },
                null,
                null,
                emailGenerator,
                null,
                true);
        }

        #endregion
    }

    #region Models

    // Model classes for API requests
    public class LoginModel
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class VerifyLoginOtpModel
    {
        public string email { get; set; }
        public string otp { get; set; }
    }

    public class ForgotPasswordModel
    {
        public string email { get; set; }
    }

    public class VerifyOtpModel
    {
        public string email { get; set; }
        public string otp { get; set; }
    }

    public class ResetPasswordModel
    {
        public string email { get; set; }
        public string otp { get; set; }
        public string newPassword { get; set; }
    }

    public class ChangePasswordModel
    {
        public string currentPassword { get; set; }
        public string newPassword { get; set; }
    }

    #endregion
} 