using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime.Remoting.Messaging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using ConfessionAPI.DAL;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using ConfessionAPI.Models;
using ConfessionAPI.Providers;
using ConfessionAPI.Results;
using Google.Authenticator;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;

namespace ConfessionAPI.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        ConfessionDbContext db = new ConfessionDbContext();
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }
        Random random = new Random();


        public string RandomString(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        // GET api/Account/UserInfo
        //[HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        // dlu@2022! pass email hzwappxqikdylebx
        // email noreply.email.dluconfession@gmail.com

        [HttpPost]
        [AllowAnonymous]
        public IHttpActionResult SendEmailOTP()
        {
            try
            {
                
                var otp = RandomString(6);
                var email = HttpContext.Current.Request["email"];
                Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
                Match match = regex.Match(email);
                if (match.Success)
                {
                    using (var context = new ConfessionDbContext())
                    {
                        var user = context.IdentityUsers.FirstOrDefault(s => s.Email == email);
                        if (user != null)
                        {
                            user.Otp = otp;
                            user.OtpCreateDate = DateTime.Now;
                            user.OtpWrongTime = 0;
                            context.Entry(user).State = EntityState.Modified;
                            context.SaveChanges();

                            var ctx = HttpContext.Current;
                            var dir = ctx.Server.MapPath("~/App_Data/LayoutHtml/EmailLayout/");
                            var path = Path.Combine(dir, "EmailOtp.html");
                            var mailBody = File.ReadAllText(path);
                            
                            mailBody = mailBody.Replace("{otp}", user.Otp);
                            mailBody = mailBody.Replace("{username}", user.UserName);
                            
                            using (MailMessage mail = new MailMessage())
                            {
                                mail.From = new MailAddress("noreply.email.dluconfession@gmail.com");
                                mail.To.Add(email);
                                mail.Subject = $"{otp} là mã xác nhận tài khoản DLU-Confession của bạn";
                                mail.Body = mailBody;
                                mail.IsBodyHtml = true;

                                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                                {
                                    smtp.Credentials = new NetworkCredential("noreply.email.dluconfession@gmail.com",
                                        "tfdvpxulcftdytvx");
                                    smtp.EnableSsl = true;
                                    smtp.Send(mail);
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("Error", "Email không tồn tại.");
                            return BadRequest(ModelState);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập đúng định dạng Email.");
                    return BadRequest(ModelState);
                }

                return Ok();
            }

            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return BadRequest(ModelState);
            }
        }

        public bool CheckOtp(Account account)
        {
            var timeOut = DateTime.Now.AddMinutes(-10);
            if (account.OtpCreateDate >= timeOut)
            {
                return true;
            }
            return false;
        }

        [HttpPost]
        [AllowAnonymous]
        public IHttpActionResult ForgotPassword()
        {
            try
            {
                var data = HttpContext.Current.Request["ForgotPassword"];
                var changePassword = JsonConvert.DeserializeObject<ForgotPassword>(data);

                Regex regexPass = new Regex(@"^(?=.*\d)(?=.*[a-z])(?=.*[a-zA-Z]).{6,}$");
                Match matchPass = regexPass.Match(changePassword.NewPassword);

                if (!matchPass.Success)
                {
                    ModelState.AddModelError("Error", $"Mật khẩu phải có ít nhất 01 chữ cái thường, 01 chữ số và tối thiểu 06 ký tự");
                    return BadRequest(ModelState);
                }

                Regex regexEmail = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
                Match matchEmail = regexEmail.Match(changePassword.Email);

                if (matchEmail.Success)
                {
                    using (var db = new ConfessionDbContext())
                    {
                        var account = db.IdentityUsers.FirstOrDefault(x => x.Email == changePassword.Email);
                        if (account != null)
                        {
                            if (account.OtpWrongTime >= 3)
                            {
                                ModelState.AddModelError("Error", "Bạn đã nhập sai quá 3 lần. Vui lòng nhập lại mã xác nhận mới.");
                                return BadRequest(ModelState);
                            }

                            if (!CheckOtp(account))
                            {
                                ModelState.AddModelError("Error", "Mã xác nhận đã quá hạn sử dụng.");
                                return BadRequest(ModelState);
                            }
                            else
                            {
                                if (changePassword.Otp == account.Otp)
                                {
                                    var newPassword = UserManager.PasswordHasher.HashPassword(changePassword.NewPassword);
                                    account.PasswordHash = newPassword;
                                    account.Otp = null;
                                    account.OtpWrongTime = 0;
                                    account.OtpCreateDate = null;
                                    db.Entry(account).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                                else
                                {
                                    account.OtpWrongTime++;
                                    db.Entry(account).State = EntityState.Modified;
                                    db.SaveChanges();

                                    ModelState.AddModelError("Error", "Mã xác nhận không đúng.");
                                    return BadRequest(ModelState);
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("Error", "Email không tồn tại.");
                            return BadRequest(ModelState);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("Error", "Email không đúng định dạng.");
                    return BadRequest(ModelState);
                }
                return Ok();
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }
        

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Regex regex = new Regex(@"^(?=.*\d)(?=.*[a-z])(?=.*[a-zA-Z]).{6,}$");
            Match match = regex.Match(model.NewPassword);

            if (!match.Success)
            {
                ModelState.AddModelError("Error", $"Mật khẩu phải có ít nhất 01 chữ cái thường, 01 chữ số và tối thiểu 06 ký tự");
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                model.NewPassword);


            if (!result.Succeeded)
            {
                ModelState.AddModelError("Error", "Mật Khẩu không đúng");
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/SetPassword
        [Route("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            Account user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                   OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // Post api/Account/GetUserInfo
        [HttpPost]
        [AllowAnonymous]
        public IHttpActionResult GetUserInfo()
        {
            try
            {
                var userId = HttpContext.Current.Request["Id"];
                var userInfo = db.IdentityUsers.FirstOrDefault(s => s.Id == userId);
                if (userInfo == null)
                {
                    ModelState.AddModelError("Error", "Tài khoản không tồn tại");
                    return BadRequest(ModelState);
                }

                var user = new Account()
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    PhoneNumber = userInfo.PhoneNumber,
                    UserProfile = userInfo.UserProfile
                };

                if (user.UserProfile.Avatar == null)
                {
                    user.UserProfile.Avatar = "Default/Avatar_default.png";
                }

                return Json(user);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public IHttpActionResult Register(RegisterBindingModel model)
        {
            var user = new Account();
            try
            {
                if (db.IdentityUsers.SingleOrDefault(x => x.UserName == model.UserName) != null)
                {
                    ModelState.AddModelError("Error", $"Tên đăng nhập đã được sử dụng.");
                    return BadRequest(ModelState);
                }
                if (db.IdentityUsers.SingleOrDefault(x => x.Email == model.Email) != null)
                {
                    ModelState.AddModelError("Error", $"Email đã được sử dụng.");
                    return BadRequest(ModelState);
                }

                Regex regex = new Regex(@"^(?=.*\d)(?=.*[a-z])(?=.*[a-zA-Z]).{6,}$");
                Match match = regex.Match(model.Password);

                if (!match.Success)
                {
                    ModelState.AddModelError("Error", $"Mật khẩu phải có ít nhất 01 chữ cái thường, 01 chữ số và tối thiểu 06 ký tự");
                    return BadRequest(ModelState);
                }

                string userRole = "User";
                user = new Account()
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    UserProfile = new UserProfile
                    {
                        FirstName = model.UserName,
                        LastName = model.UserName,
                        Birthday = DateTime.Now,
                        Gender = Gender.Other,
                        NickName = model.NickName,
                        Avatar = "Default/Avatar_default.png"
                    }
                };

                if (user.UserProfile.NickName.IsNullOrWhiteSpace())
                {
                    var numNickName = Guid.NewGuid().ToString().Split('-')[0];
                    user.UserProfile.NickName = "User@" +  numNickName;
                }

                IdentityResult result = UserManager.Create(user, model.Password);

                if (result.Succeeded)
                {
                    UserManager.AddToRole(user.Id, userRole);
                }

                if (!result.Succeeded)
                {
                    //ModelState.AddModelError("Error", "Mật khẩu phải có ít nhất 01 ký tự đặc biệt, và 01 chữ cái viết hoa.");
                    //ModelState.AddModelError("ErrorResult", result.Errors.ToString());
                    //return BadRequest(ModelState);

                    return GetErrorResult(result);
                }

                return Json(user);
            }
            catch (Exception e)
            {
                return Json(e);
            }
        }

        // POST api/Account/RegisterExternal
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return InternalServerError();
            }

            var user = new Account() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);


            }
            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }
}
