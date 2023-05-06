using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using editor.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace editor.Provider
{
  public class CustomAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
  {
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILogger _logger;
    private readonly HMACSHA256 _hmac;

    private readonly AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthenticationStateProvider(
      ILoggerFactory loggerFactory,
      ISessionStorageService sessionStorage,
      IConfiguration configuration) : base(loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<CustomAuthenticationStateProvider>();
      _sessionStorage = sessionStorage;
      var sha256 = new SHA256Managed();
      var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
      var computedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(botToken));
      _hmac = new HMACSHA256(computedHash);
    }

    private bool IsAuthDataValid(User user)
    {
      if (user == null)
      {
        return false;
      }
      var builder = new StringBuilder();
      builder.AppendFormat("auth_date={0}\n", user.AuthDate);
      builder.AppendFormat("first_name={0}\n", user.FirstName);
      builder.AppendFormat("id={0}\n", user.Id);
      builder.AppendFormat("photo_url={0}\n", user.PhotoUrl);
      builder.AppendFormat("username={0}", user.Username);

      var result = _hmac.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
      var hex = BitConverter.ToString(result).Replace("-", string.Empty).ToLower();
      // TODO: obviously change delta check to < 1 day or something like that
      return hex == user.Hash &&
             (DateTimeOffset.Now - DateTimeOffset.FromUnixTimeSeconds(user.AuthDate)).TotalSeconds > 0;
    }

    public async Task<AuthenticationState> AuthenticateUser(User user)
    {
      if (!IsAuthDataValid(user))
      {
        return _anonymous;
      }

      var identity = new ClaimsIdentity(new[]
      {
        new Claim(ClaimTypes.Sid, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FirstName),
        new Claim("Username", user.Username),
        new Claim("Avatar", user.PhotoUrl),
        new Claim("AuthDate", user.AuthDate.ToString()),
      }, "Telegram");
      var principal = new ClaimsPrincipal(identity);
      var authState = new AuthenticationState(principal);
      base.SetAuthenticationState(Task.FromResult(authState));
      await _sessionStorage.SetItemAsync("user", user);
      return authState;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
      var state = await base.GetAuthenticationStateAsync();
      if (state.User.Identity is { IsAuthenticated: true })
      {
        return state;
      }
      
      try
      {
        var user = await _sessionStorage.GetItemAsync<User>("user");
        return await AuthenticateUser(user);
      }
      // this happens on pre-render
      catch (InvalidOperationException)
      {
        return _anonymous;
      }
    }

    public void Logout()
    {
      _sessionStorage.RemoveItemAsync("user");
      base.SetAuthenticationState(Task.FromResult(_anonymous));
    }

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState,
      CancellationToken cancellationToken)
    {
      try
      {
        var user = await _sessionStorage.GetItemAsync<User>("user");
        return user != null && IsAuthDataValid(user);
      }
      // this shouldn't happen, but just in case
      catch (InvalidOperationException)
      {
        return false;
      }
    }

    protected override TimeSpan RevalidationInterval { get; } = TimeSpan.FromHours(1);
  }
}