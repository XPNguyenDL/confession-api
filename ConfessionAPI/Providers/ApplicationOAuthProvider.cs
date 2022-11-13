using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using ConfessionAPI.Models;

namespace ConfessionAPI.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;

        public ApplicationOAuthProvider(string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            _publicClientId = publicClientId;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {

            // login: grant_type=password&username=Admin&password=Admin#123
            var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();

            Account user = await userManager.FindAsync(context.UserName, context.Password);

            if (user == null)
            {
                if (context.UserName == "admin" && context.Password == "dluconfession@2022!")
                {
                    var adminUser = new Account()
                    {
                        UserName = "admin",
                        Email = "contact.email.dluconfession@gmail.com",
                        UserProfile = new UserProfile
                        {
                            FirstName = "Super",
                            LastName = "Admin",
                            Description = "Admin DLU Confession",
                            Gender = Gender.Other,
                            Major = "admin",
                            Birthday = DateTime.Now,
                            NickName = "Admin",
                            Avatar = "Default/Avatar_default.png"
                        }
                    };
                    const string adminRole = "Admin",
                        managerRole = "Manager",
                        userRole = "User";
                    var result = userManager.Create(adminUser, "dluconfession@2022!");
                    if (result.Succeeded)
                    {
                        userManager.AddToRole(adminUser.Id, adminRole);
                        userManager.AddToRole(adminUser.Id, managerRole);
                        userManager.AddToRole(adminUser.Id, userRole);
                    }
                }
                context.SetError("invalid_grant", "Sai tên đăng nhập hoặc mật khẩu.");
                return;
            }

            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager,
               OAuthDefaults.AuthenticationType);
            ClaimsIdentity cookiesIdentity = await user.GenerateUserIdentityAsync(userManager,
                CookieAuthenticationDefaults.AuthenticationType);

            AuthenticationProperties properties = CreateProperties(user);
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
            context.Request.Context.Authentication.SignIn(cookiesIdentity);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            // Resource owner password credentials does not provide a client ID.
            if (context.ClientId == null)
            {
                context.Validated();
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                {
                    context.Validated();
                }
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(Account user)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "userName", user.UserName ?? "" },
                { "email", user.Email},
                { "phoneNumber", user.PhoneNumber ?? ""},
                { "userProfile.nickName", user.UserProfile?.NickName ?? ""},
                { "userProfile.avatar", user.UserProfile?.Avatar ?? ""},
                { "userProfile.major", user.UserProfile?.Major ?? ""},
                { "userProfile.firstName", user.UserProfile?.FirstName ?? ""},
                { "userProfile.lastName", user.UserProfile?.LastName ?? ""},
                { "userProfile.fullName", user.UserProfile?.FullName ?? ""},
                { "userProfile.description", user.UserProfile?.Description ?? ""},
                { "lockoutEndDateUtc", user.LockoutEndDateUtc.ToString() ?? ""}
            };
            return new AuthenticationProperties(data);
        }
    }
}