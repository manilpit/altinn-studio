using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Altinn.Platform.Authentication.Configuration;
using Altinn.Platform.Authentication.Model;
using AltinnCore.Authentication.Constants;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authentication.Controllers
{
    /// <summary>
    /// Handles the authentication of requests to platform
    /// </summary>
    [Route("authentication/api/v1/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly GeneralSettings _generalSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationController"/> class
        /// </summary>
        /// <param name="logger">the logger</param>
        /// <param name="generalSettings">the general settings</param>
        public AuthenticationController(ILogger<AuthenticationController> logger, IOptions<GeneralSettings> generalSettings)
        {
            _logger = logger;
            _generalSettings = generalSettings.Value;
        }

        /// <summary>
        /// Request that handles the form authentication cookie from SBL
        /// </summary>
        /// <param name="goToUrl">The url to redirect to if everything validates ok</param>
        /// <returns>redirect to correct url based on the validation of the form authentication sbl cookie</returns>
        [HttpGet]
        public async Task<ActionResult> Get(string goToUrl)
        {
            string encodedGoToUrl = HttpUtility.UrlEncode($"{_generalSettings.GetPlatformEndpoint}authentication/api/v1/authentication?goto={goToUrl}");
            if (Request.Cookies[_generalSettings.GetSBLCookieName] == null)
            {
                return Redirect($"{_generalSettings.GetSBLRedirectEndpoint}?goTo={encodedGoToUrl}");
            }
            else
            {
                UserAuthenticationModel userAuthentication = null;
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(UserAuthenticationModel));
                Uri endpointUrl = new Uri($"{_generalSettings.GetBridgeApiEndpoint}/tickets");
                using (HttpClient client = new HttpClient())
                {
                    UserAuthenticationModel postUserValue = new UserAuthenticationModel() { EncryptedTicket = Request.Cookies[_generalSettings.GetSBLCookieName] };
                    HttpResponseMessage response = await client.PostAsync(endpointUrl, new StringContent(postUserValue.ToString(), Encoding.UTF8, "application/json"));
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream stream = await response.Content.ReadAsStreamAsync();
                        userAuthentication = serializer.ReadObject(stream) as UserAuthenticationModel;
                        if (userAuthentication.IsAuthenticated)
                        {
                            List<Claim> claims = new List<Claim>();
                            string issuer = _generalSettings.GetPlatformEndpoint;
                            claims.Add(new Claim(AltinnCoreClaimTypes.UserId, userAuthentication.UserID.ToString(), ClaimValueTypes.String, issuer));
                            claims.Add(new Claim(AltinnCoreClaimTypes.UserName, userAuthentication.Username, ClaimValueTypes.String, issuer));
                            claims.Add(new Claim(AltinnCoreClaimTypes.PartyID, userAuthentication.PartyID.ToString(), ClaimValueTypes.Integer32, issuer));
                            claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, userAuthentication.AuthenticationMethod.ToString(), ClaimValueTypes.String, issuer));
                            claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, userAuthentication.AuthenticationLevel.ToString(), ClaimValueTypes.Integer32, issuer));
                            if (userAuthentication.SSN != null)
                            {
                                claims.Add(new Claim(AltinnCoreClaimTypes.SSN, userAuthentication.SSN, ClaimValueTypes.String, issuer));
                            }

                            ClaimsIdentity identity = new ClaimsIdentity(_generalSettings.GetClaimsIdentity);
                            identity.AddClaims(claims);
                            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                            await HttpContext.SignInAsync(
                                JwtCookieDefaults.AuthenticationScheme,
                                principal,
                                new AuthenticationProperties
                                {
                                    ExpiresUtc = DateTime.UtcNow.AddMinutes(int.Parse(_generalSettings.GetJwtCookieValidityTime)),
                                    IsPersistent = false,
                                    AllowRefresh = false,
                                });

                            if (userAuthentication.TicketUpdated)
                            {
                                Response.Cookies.Append(_generalSettings.GetSBLCookieName, userAuthentication.EncryptedTicket);
                            }

                            return Redirect(goToUrl);
                        }
                        else
                        {
                            // If user is not authenticated redirect to login
                            _logger.LogError($"Getting the authenticated user failed with statuscode {response.StatusCode}");
                            return Redirect($"{_generalSettings.GetSBLRedirectEndpoint}?goTo={encodedGoToUrl}");
                        }
                    }
                    else
                    {
                        return Redirect($"{_generalSettings.GetSBLRedirectEndpoint}?goTo={encodedGoToUrl}");
                    }
                }
            }
        }
    }
}