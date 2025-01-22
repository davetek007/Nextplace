using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Nextplace.Api.Db;
using Nextplace.Api.Helpers;
using Nextplace.Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Nextplace.Api.Controllers;

[Tags("User APIs")]
[ApiController]
[Route("Users")]
public class UserController(AppDbContext context, IConfiguration configuration, IMemoryCache cache) : ControllerBase
{
  [HttpPost("Register", Name = "RegisterUser")]
  [SwaggerOperation("Register a new user")]
  public async Task<ActionResult> RegisterUser(RegisterUserRequest request)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "RegisterUser", out var offendingIpAddress))
      {
        await context.SaveLogEntry("RegisterUser", $"Rate limit exceeded by {offendingIpAddress}",
          "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("RegisterUser", "Started", "Information", executionInstanceId, clientIp);

      if (string.IsNullOrWhiteSpace(request.EmailAddress) || !IsValidEmailAddress(request.EmailAddress))
      {
        await context.SaveLogEntry("RegisterUser", "Invalid email", "Warning", executionInstanceId, clientIp);
        return BadRequest("Email Address is not valid");
      }

      if (string.IsNullOrWhiteSpace(request.Password))
      {
        await context.SaveLogEntry("RegisterUser", "Invalid password", "Warning", executionInstanceId, clientIp);
        return BadRequest("Password is not valid");
      }

      if (context.User.Any(u => u.EmailAddress == request.EmailAddress && u.Active))
      {
        await context.SaveLogEntry("RegisterUser", $"Email address {request.EmailAddress} already exists", "Warning", executionInstanceId, clientIp);
        return Conflict();
      }

      var validationKey = Guid.NewGuid().ToString();
      var salt = GenerateSalt();
      var hashedPassword = HashPasswordWithSalt(request.Password, salt);
      var hashedValidationKey = HashPasswordWithSalt(validationKey, salt);

      var user = new Db.User
      {
        EmailAddress = request.EmailAddress,
        Password = hashedPassword,
        Salt = salt,
        Active = true,
        ValidationKey = hashedValidationKey,
        CreateDate = DateTime.UtcNow,
        LastUpdateDate = DateTime.UtcNow,
        Status = "Pending Validation",
        UserType = "Standard"
      };

      context.User.Add(user);
      await context.SaveChangesAsync();

      await context.SaveLogEntry("RegisterUser", $"User {user.Id} created", "Information", executionInstanceId, clientIp);

      await SendRegisterUserEmail(validationKey, request.EmailAddress);
      await context.SaveLogEntry("RegisterUser", $"Email sent to {request.EmailAddress}", "Information", executionInstanceId, clientIp);

      await context.SaveLogEntry("RegisterUser", "Completed", "Information", executionInstanceId, clientIp);

      return CreatedAtAction(nameof(RegisterUser), null, null);
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("RegisterUser", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpPatch("Register/Revalidate", Name = "RegisterUserRevalidate")]
  [SwaggerOperation("Resend validation email for a registering user")]
  public async Task<ActionResult> RegisterUserRevalidate([SwaggerParameter("Email Address", Required = true)][FromBody] string emailAddress)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "RegisterUserRevalidate", out var offendingIpAddress))
      {
        await context.SaveLogEntry("RegisterUserRevalidate", $"Rate limit exceeded by {offendingIpAddress}",
          "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("RegisterUserRevalidate", "Started", "Information", executionInstanceId, clientIp);

      if (string.IsNullOrWhiteSpace(emailAddress) || !IsValidEmailAddress(emailAddress))
      {
        await context.SaveLogEntry("RegisterUserRevalidate", "Invalid email", "Warning", executionInstanceId, clientIp);
        return BadRequest("Email Address is not valid");
      }

      var user = await context.User.FirstOrDefaultAsync(u =>
        u.EmailAddress == emailAddress && u.Active && u.Status == "Pending Validation");

      if (user == null)
      {
        await context.SaveLogEntry("RegisterUserRevalidate", $"Email address {emailAddress} does not exist in the database or is not in the correct state", "Warning", executionInstanceId, clientIp);
        return Conflict();
      }

      var validationKey = Guid.NewGuid().ToString();
      var salt = user.Salt;
      var hashedValidationKey = HashPasswordWithSalt(validationKey, salt);

      user.ValidationKey = hashedValidationKey;
      user.LastUpdateDate = DateTime.UtcNow;
      await context.SaveChangesAsync();

      await SendRegisterUserEmail(validationKey, emailAddress);
      await context.SaveLogEntry("RegisterUserRevalidate", $"Email sent to {emailAddress}", "Information", executionInstanceId, clientIp);

      await context.SaveLogEntry("RegisterUserRevalidate", "Completed", "Information", executionInstanceId, clientIp);

      return Ok();
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("RegisterUserRevalidate", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpPost("Validate", Name = "ValidateUser")]
  [SwaggerOperation("Validate a user")]
  public async Task<ActionResult<Models.User>> ValidateUser(ValidateUserRequest request)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "ValidateUser", out var offendingIpAddress))
      {
        await context.SaveLogEntry("ValidateUser", $"Rate limit exceeded by {offendingIpAddress}",
          "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("ValidateUser", "Started", "Information", executionInstanceId, clientIp);

      if (string.IsNullOrWhiteSpace(request.EmailAddress) || !IsValidEmailAddress(request.EmailAddress))
      {
        await context.SaveLogEntry("ValidateUser", "Invalid email", "Warning", executionInstanceId, clientIp);
        return BadRequest("Email Address is not valid");
      }

      if (string.IsNullOrWhiteSpace(request.ValidationKey))
      {
        await context.SaveLogEntry("ValidateUser", "Invalid validation key", "Warning", executionInstanceId, clientIp);
        return BadRequest("Validation Key is not valid");
      }

      var user = await context.User.Include(u => u.UserFavorites).Include(u => u.UserSettings)
        .FirstOrDefaultAsync(
          u => u.EmailAddress == request.EmailAddress && u.Active && u.Status == "Pending Validation");
      
      if (user == null)
      {
        await context.SaveLogEntry("ValidateUser", $"Email address {request.EmailAddress} does not exist in the database or is not in the correct state", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedValidationKey = HashPasswordWithSalt(request.ValidationKey, salt);

      if (user.ValidationKey == hashedValidationKey)
      {
        user.Status = "Validated";
        user.ValidationKey = null;
        var sessionToken = Guid.NewGuid().ToString();
        var hashedSessionToken = HashPasswordWithSalt(sessionToken, salt);
        user.SessionToken = hashedSessionToken;
        user.LastUpdateDate = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await context.SaveLogEntry("ValidateUser", $"User {user.Id} validated", "Information", executionInstanceId, clientIp);

        Response.Headers.Append("Nextplace-Session-Token", sessionToken);
      }
      else
      {
        await context.SaveLogEntry("ValidateUser", $"Validation key does not match for user ID {user.Id}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      await context.SaveLogEntry("ValidateUser", "Completed", "Information", executionInstanceId, clientIp);

      return Ok(await ConvertDbUserToModel(user));
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("ValidateUser", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpPost("ForgotPassword/{emailAddress}", Name = "ForgotPassword")]
  [SwaggerOperation("Forgot password")]
  public async Task<ActionResult> ForgotPassword([SwaggerParameter("Email Address", Required = true)][FromRoute] string emailAddress)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "ForgotPassword", out var offendingIpAddress))
      {
        await context.SaveLogEntry("ForgotPassword", $"Rate limit exceeded by {offendingIpAddress}",
          "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("ForgotPassword", "Started", "Information", executionInstanceId, clientIp);

      if (string.IsNullOrWhiteSpace(emailAddress) || !IsValidEmailAddress(emailAddress))
      {
        await context.SaveLogEntry("ForgotPassword", "Invalid email", "Warning", executionInstanceId, clientIp);
        return BadRequest("Email Address is not valid");
      }

      var user = await context.User.FirstOrDefaultAsync(u => u.EmailAddress == emailAddress && u.Active && u.Status == "Validated");
      if (user == null)
      {
        await context.SaveLogEntry("ForgotPassword", $"Email address {emailAddress} does not exist in the database or is not in the correct state", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var validationKey = Guid.NewGuid().ToString();
      var salt = user.Salt;
      var hashedValidationKey = HashPasswordWithSalt(validationKey, salt);

      user.ValidationKey = hashedValidationKey;
      user.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();

      await SendForgotPasswordEmail(validationKey, emailAddress);
      await context.SaveLogEntry("ForgotPassword", $"Email sent to {emailAddress}", "Information", executionInstanceId, clientIp);

      await context.SaveLogEntry("ForgotPassword", "Completed", "Information", executionInstanceId, clientIp);

      return Ok();
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("ForgotPassword", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpPost("ResetPassword", Name = "ResetPassword")]
  [SwaggerOperation("Reset user password")]
  public async Task<ActionResult<Models.User>> ResetPassword(ResetPasswordRequest request)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "ResetPassword", out var offendingIpAddress))
      {
        await context.SaveLogEntry("ResetPassword", $"Rate limit exceeded by {offendingIpAddress}",
          "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("ResetPassword", "Started", "Information", executionInstanceId, clientIp);

      if (string.IsNullOrWhiteSpace(request.EmailAddress) || !IsValidEmailAddress(request.EmailAddress))
      {
        await context.SaveLogEntry("ResetPassword", "Invalid email", "Warning", executionInstanceId, clientIp);
        return BadRequest("Email Address is not valid");
      }

      if (string.IsNullOrWhiteSpace(request.ValidationKey))
      {
        await context.SaveLogEntry("ResetPassword", "Invalid validation key", "Warning", executionInstanceId, clientIp);
        return BadRequest("Validation Key is not valid");
      }

      if (string.IsNullOrWhiteSpace(request.Password))
      {
        await context.SaveLogEntry("ResetPassword", "Invalid password", "Warning", executionInstanceId, clientIp);
        return BadRequest("Password is not valid");
      }

      var user = await context.User.Include(u => u.UserFavorites).Include(u => u.UserSettings)
        .FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress && u.Active && u.Status == "Validated");
      if (user == null)
      {
        await context.SaveLogEntry("ResetPassword", $"Email address {request.EmailAddress} does not exist in the database or is not in the correct state", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedValidationKey = HashPasswordWithSalt(request.ValidationKey, salt);

      if (user.ValidationKey == hashedValidationKey)
      {
        salt = GenerateSalt();
        var hashedPassword = HashPasswordWithSalt(request.Password, salt);

        user.Password = hashedPassword;
        user.Salt = salt;
        user.ValidationKey = null;
        var sessionToken = Guid.NewGuid().ToString();
        var hashedSessionToken = HashPasswordWithSalt(sessionToken, salt);
        user.SessionToken = hashedSessionToken;
        user.LastUpdateDate = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await context.SaveLogEntry("ResetPassword", $"User {user.Id} validated", "Information", executionInstanceId, clientIp);

        Response.Headers.Append("Nextplace-Session-Token", sessionToken);
      }
      else
      {
        await context.SaveLogEntry("ResetPassword", $"Validation key does not match for user ID {user.Id}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      await context.SaveLogEntry("ResetPassword", "Completed", "Information", executionInstanceId, clientIp);

      return Ok(await  ConvertDbUserToModel(user));
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("ResetPassword", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpDelete("", Name = "DeleteUser")]
  [SwaggerOperation("Delete user")]
  public async Task<ActionResult> DeleteUser(DeleteUserRequest request)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "DeleteUser", out var offendingIpAddress))
      {
        await context.SaveLogEntry("DeleteUser", $"Rate limit exceeded by {offendingIpAddress}",
          "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("DeleteUser", "Started", "Information", executionInstanceId, clientIp);

      if (string.IsNullOrWhiteSpace(request.EmailAddress) || !IsValidEmailAddress(request.EmailAddress))
      {
        await context.SaveLogEntry("DeleteUser", "Invalid email", "Warning", executionInstanceId, clientIp);
        return BadRequest("Email Address is not valid");
      }

      if (string.IsNullOrWhiteSpace(request.Password))
      {
        await context.SaveLogEntry("DeleteUser", "Invalid password", "Warning", executionInstanceId, clientIp);
        return BadRequest("Password is not valid");
      }

      var user = await context.User.FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress && u.Active);
      if (user == null)
      {
        await context.SaveLogEntry("DeleteUser", $"Email address {request.EmailAddress} does not exist", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedPassword = HashPasswordWithSalt(request.Password, salt);

      if (user.Password != hashedPassword)
      {
        await context.SaveLogEntry("DeleteUser", $"Password does not match for email address {request.EmailAddress}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      user.Active = false;
      user.LastUpdateDate = DateTime.UtcNow;
      await context.SaveChangesAsync();

      await context.SaveLogEntry("DeleteUser", "Completed", "Information", executionInstanceId, clientIp);

      return Ok();
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("DeleteUser", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpPost("Logon", Name = "LogonUser")]
  [SwaggerOperation("User logon")]
  public async Task<ActionResult<Models.User>> LogonUser(LogonUserRequest request)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "LogonUser", out var offendingIpAddress))
      {
        await context.SaveLogEntry("LogonUser", $"Rate limit exceeded by {offendingIpAddress}",
          "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("LogonUser", "Started", "Information", executionInstanceId, clientIp);

      if (string.IsNullOrWhiteSpace(request.EmailAddress) || !IsValidEmailAddress(request.EmailAddress))
      {
        await context.SaveLogEntry("LogonUser", "Invalid email", "Warning", executionInstanceId, clientIp);
        return BadRequest("Email Address is not valid");
      }

      if (string.IsNullOrWhiteSpace(request.Password))
      {
        await context.SaveLogEntry("LogonUser", "Invalid password", "Warning", executionInstanceId, clientIp);
        return BadRequest("Password is not valid");
      }

      var user = context.User
        .Include(u => u.UserSettings!)
        .Include(u => u.UserFavorites!)
        .FirstOrDefault(u => u.EmailAddress == request.EmailAddress && u.Active && u.Status == "Validated");

      if (user == null)
      {
        await context.SaveLogEntry("LogonUser", $"Email address {request.EmailAddress} does not exist", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedPassword = HashPasswordWithSalt(request.Password, salt);

      if (user.Password != hashedPassword)
      {
        await context.SaveLogEntry("LogonUser", $"Password does not match for email address {request.EmailAddress}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var sessionToken = Guid.NewGuid().ToString();
      var hashedSessionToken = HashPasswordWithSalt(sessionToken, salt);
      user.SessionToken = hashedSessionToken;
      user.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();

      await context.SaveLogEntry("LogonUser", "Completed", "Information", executionInstanceId, clientIp);

      Response.Headers.Append("Nextplace-Session-Token", sessionToken);

      return Ok(await ConvertDbUserToModel(user));
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("LogonUser", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpGet("{id}", Name = "GetUser")]
  [SwaggerOperation("Get user")]
  public async Task<ActionResult<Models.User>> GetUser(
    [SwaggerParameter("User ID", Required = true)][FromRoute] long id,
    [SwaggerParameter("Session token", Required = true)][FromHeader] string sessionToken)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "GetUser", out var offendingIpAddress))
      {
        await context.SaveLogEntry("GetUser", $"Rate limit exceeded by {offendingIpAddress}",
          "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      Response.AppendCorsHeaders();

      await context.SaveLogEntry("GetUser", "Started", "Information", executionInstanceId, clientIp);
      
      var user = context.User
        .Include(u => u.UserSettings!)
        .Include(u => u.UserFavorites!)
        .FirstOrDefault(u => u.Id == id && u.Active && u.Status == "Validated");

      if (user == null)
      {
        await context.SaveLogEntry("GetUser", $"User ID {id} does not exist", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedSessionToken = HashPasswordWithSalt(sessionToken, salt);

      if (user.SessionToken != hashedSessionToken)
      {
        await context.SaveLogEntry("GetUser", $"Session token does not match for User ID {id}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      user.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();

      await context.SaveLogEntry("GetUser", "Completed", "Information", executionInstanceId, clientIp);

      Response.Headers.Append("Nextplace-Session-Token", sessionToken);

      return Ok(await ConvertDbUserToModel(user));
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("GetUser", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpPost("{id}/Favorites", Name = "AddUserFavorite")]
  [SwaggerOperation("Add a favorite for a user")]
  public async Task<ActionResult> AddUserFavorite(
      [SwaggerParameter("User ID", Required = true)][FromRoute] long id,
      [SwaggerParameter("Favorite details", Required = true)][FromBody] AddUserFavoriteRequest request,
      [SwaggerParameter("Session token", Required = true)][FromHeader] string sessionToken)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "AddUserFavorite", out var offendingIpAddress))
      {
        await context.SaveLogEntry("AddUserFavorite", $"Rate limit exceeded by {offendingIpAddress}",
            "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("AddUserFavorite", "Started", "Information", executionInstanceId, clientIp);

      var user = context.User.Include(u => u.UserFavorites!).FirstOrDefault(u =>
          u.Id == id && u.Active && u.Status == "Validated");

      if (user == null)
      {
        await context.SaveLogEntry("AddUserFavorite", $"User ID {id} does not exist", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedSessionToken = HashPasswordWithSalt(sessionToken, salt);

      if (user.SessionToken != hashedSessionToken)
      {
        await context.SaveLogEntry("AddUserFavorite", $"Session token does not match for User ID {id}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }
      user.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();
      
      var userFavorite = user.UserFavorites!.FirstOrDefault(um => um.Active && um.NextplaceId == request.NextplaceId);
      if (userFavorite != null)
      {
        await context.SaveLogEntry("AddUserFavorite", $"Favorite {request.NextplaceId} already exists for user {user.Id}", "Warning", executionInstanceId, clientIp);
        Response.Headers.Append("Nextplace-Session-Token", sessionToken);
        return Conflict();
      }

      user.UserFavorites!.Add(new Db.UserFavorite
      {
        Active = true,
        CreateDate = DateTime.UtcNow,
        LastUpdateDate = DateTime.UtcNow,
        NextplaceId = request.NextplaceId,
        UserId = user.Id
      });

      await context.SaveChangesAsync();

      await context.SaveLogEntry("AddUserFavorite", "Completed", "Information", executionInstanceId, clientIp);

      Response.Headers.Append("Nextplace-Session-Token", sessionToken);

      return Ok();
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("AddUserFavorite", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }
  
  [HttpDelete("{id}/Favorites", Name = "DeleteUserFavorite")]
  [SwaggerOperation("Delete a favorite for a user")]
  public async Task<ActionResult> DeleteUserFavorite(
      [SwaggerParameter("User ID", Required = true)][FromRoute] long id,
      [SwaggerParameter("Favorite details", Required = true)][FromBody] DeleteUserFavoriteRequest request,
      [SwaggerParameter("Session token", Required = true)][FromHeader] string sessionToken)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "DeleteUserFavorite", out var offendingIpAddress))
      {
        await context.SaveLogEntry("DeleteUserFavorite", $"Rate limit exceeded by {offendingIpAddress}",
            "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("DeleteUserFavorite", "Started", "Information", executionInstanceId, clientIp);

      var user = context.User.Include(u => u.UserFavorites!).FirstOrDefault(u =>
          u.Id == id && u.Active && u.Status == "Validated");

      if (user == null)
      {
        await context.SaveLogEntry("DeleteUserFavorite", $"User ID {id} does not exist", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedSessionToken = HashPasswordWithSalt(sessionToken, salt);

      if (user.SessionToken != hashedSessionToken)
      {
        await context.SaveLogEntry("DeleteUserFavorite", $"Session token does not match for User ID {id}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      user.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();

      var userFavorite = user.UserFavorites!.FirstOrDefault(um => um.Active && um.NextplaceId == request.NextplaceId);
      if (userFavorite == null)
      {
        await context.SaveLogEntry("DeleteUserFavorite", $"Favorite {request.NextplaceId} does not exist for user {user.Id}", "Warning", executionInstanceId, clientIp);
        Response.Headers.Append("Nextplace-Session-Token", sessionToken);
        return NotFound();
      }

      userFavorite.Active = false;
      userFavorite.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();

      await context.SaveLogEntry("DeleteUserFavorite", "Completed", "Information", executionInstanceId, clientIp);

      Response.Headers.Append("Nextplace-Session-Token", sessionToken);

      return Ok();
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("DeleteUserFavorite", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }
  
  [HttpPost("{id}/Settings", Name = "AddUserSetting")]
  [SwaggerOperation("Add a setting for a user")]
  public async Task<ActionResult> AddUserSetting(
      [SwaggerParameter("User ID", Required = true)][FromRoute] long id,
      [SwaggerParameter("Setting details", Required = true)][FromBody] AddUserSettingRequest request,
      [SwaggerParameter("Session token", Required = true)][FromHeader] string sessionToken)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "AddUserSetting", out var offendingIpAddress))
      {
        await context.SaveLogEntry("AddUserSetting", $"Rate limit exceeded by {offendingIpAddress}",
            "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("AddUserSetting", "Started", "Information", executionInstanceId, clientIp);

      var user = context.User.Include(u => u.UserSettings!).FirstOrDefault(u =>
          u.Id == id && u.Active && u.Status == "Validated");

      if (user == null)
      {
        await context.SaveLogEntry("AddUserSetting", $"User ID {id} does not exist", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedSessionToken = HashPasswordWithSalt(sessionToken, salt);

      if (user.SessionToken != hashedSessionToken)
      {
        await context.SaveLogEntry("AddUserSetting", $"Session token does not match for User ID {id}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }
      user.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();

      var userSetting = user.UserSettings!.FirstOrDefault(um => um.Active && um.SettingName == request.SettingName);
      if (userSetting != null)
      {
        await context.SaveLogEntry("AddUserSetting", $"Setting {request.SettingName} already exists for user {user.Id}", "Warning", executionInstanceId, clientIp);
        Response.Headers.Append("Nextplace-Session-Token", sessionToken);
        return Conflict();
      }

      user.UserSettings!.Add(new Db.UserSetting
      {
        Active = true,
        CreateDate = DateTime.UtcNow,
        LastUpdateDate = DateTime.UtcNow,
        SettingName = request.SettingName,
        SettingValue = request.SettingValue,
        UserId = user.Id
      });

      await context.SaveChangesAsync();

      await context.SaveLogEntry("AddUserSetting", "Completed", "Information", executionInstanceId, clientIp);

      Response.Headers.Append("Nextplace-Session-Token", sessionToken);

      return Ok();
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("AddUserSetting", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  [HttpDelete("{id}/Settings", Name = "DeleteUserSetting")]
  [SwaggerOperation("Delete a setting for a user")]
  public async Task<ActionResult> DeleteUserSetting(
      [SwaggerParameter("User ID", Required = true)][FromRoute] long id,
      [SwaggerParameter("Setting details", Required = true)][FromBody] DeleteUserSettingRequest request,
      [SwaggerParameter("Session token", Required = true)][FromHeader] string sessionToken)
  {
    HttpContext.GetIpAddressesFromHeader(out _, out var clientIp);
    var executionInstanceId = Guid.NewGuid().ToString();
    try
    {
      if (!HttpContext.CheckRateLimit(cache, configuration, "DeleteUserSetting", out var offendingIpAddress))
      {
        await context.SaveLogEntry("DeleteUserSetting", $"Rate limit exceeded by {offendingIpAddress}",
            "Warning", executionInstanceId, clientIp);
        return StatusCode(429);
      }

      await context.SaveLogEntry("DeleteUserSetting", "Started", "Information", executionInstanceId, clientIp);

      var user = context.User.Include(u => u.UserSettings!).FirstOrDefault(u =>
          u.Id == id && u.Active && u.Status == "Validated");

      if (user == null)
      {
        await context.SaveLogEntry("DeleteUserSetting", $"User ID {id} does not exist", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      var salt = user.Salt;
      var hashedSessionToken = HashPasswordWithSalt(sessionToken, salt);

      if (user.SessionToken != hashedSessionToken)
      {
        await context.SaveLogEntry("DeleteUserSetting", $"Session token does not match for User ID {id}", "Warning", executionInstanceId, clientIp);
        return Unauthorized();
      }

      user.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();

      var userSetting = user.UserSettings!.FirstOrDefault(um => um.Active && um.SettingName == request.SettingName);
      if (userSetting == null)
      {
        await context.SaveLogEntry("DeleteUserSetting", $"Setting {request.SettingName} does not exist for user {user.Id}", "Warning", executionInstanceId, clientIp);
        Response.Headers.Append("Nextplace-Session-Token", sessionToken);
        return NotFound();
      }

      userSetting.Active = false;
      userSetting.LastUpdateDate = DateTime.UtcNow;

      await context.SaveChangesAsync();

      await context.SaveLogEntry("DeleteUserSetting", "Completed", "Information", executionInstanceId, clientIp);

      Response.Headers.Append("Nextplace-Session-Token", sessionToken);

      return Ok();
    }
    catch (Exception ex)
    {
      await context.SaveLogEntry("DeleteUserSetting", ex, executionInstanceId, clientIp);
      return StatusCode(500);
    }
  }

  private async Task SendForgotPasswordEmail(string validationKey, string emailAddress)
  {
    var forgotPasswordValidationPage = configuration["UserSettings:ForgotPasswordValidationPage"]!;

    var akv = new AkvHelper(configuration);

    var emailTenantId = await akv.GetSecretAsync("EmailTenantId");
    var emailClientSecret = await akv.GetSecretAsync("EmailClientSecret");
    var emailClientId = await akv.GetSecretAsync("EmailClientId");

    var clientSecretCredential = new ClientSecretCredential(emailTenantId, emailClientId, emailClientSecret);

    var graphClient = new GraphServiceClient(clientSecretCredential);

    var message = new SendMailPostRequestBody
    {
      Message = new Message
      {
        Subject = "Reset your Nextplace password",
        Body = new ItemBody
        {
          ContentType = BodyType.Html,
          Content = EmailContent.ForgotPasswordEmailValidation(forgotPasswordValidationPage, validationKey, emailAddress)
        },
        ToRecipients = new List<Recipient>
        {
          new()
          {
            EmailAddress = new EmailAddress
            {
              Address = emailAddress
            }
          }
        }
      }
    };

    EmailContent.AddHeaderImage(message.Message);

    await graphClient.Users["admin@nextplace.ai"].SendMail.PostAsync(message);
  }

  private async Task SendRegisterUserEmail(string validationKey, string emailAddress)
  {
    var newUserValidationPage = configuration["UserSettings:NewUserValidationPage"]!;

    var akv = new AkvHelper(configuration);

    var emailTenantId = await akv.GetSecretAsync("EmailTenantId");
    var emailClientSecret = await akv.GetSecretAsync("EmailClientSecret");
    var emailClientId = await akv.GetSecretAsync("EmailClientId");

    var clientSecretCredential = new ClientSecretCredential(emailTenantId, emailClientId, emailClientSecret);

    var graphClient = new GraphServiceClient(clientSecretCredential);

    var message = new SendMailPostRequestBody
    {
      Message = new Message
      {
        Subject = "Confirm your email address",
        Body = new ItemBody
        {
          ContentType = BodyType.Html,
          Content = EmailContent.NewUserEmailValidation(newUserValidationPage, validationKey, emailAddress)
        },
        ToRecipients = new List<Recipient>
        {
          new()
          {
            EmailAddress = new EmailAddress
            {
              Address = emailAddress
            }
          }
        }
      }
    };

    EmailContent.AddHeaderImage(message.Message);

    await graphClient.Users["admin@nextplace.ai"].SendMail.PostAsync(message);
  }

  private static bool IsValidEmailAddress(string emailAddress)
  {
    try
    {
      var addr = new System.Net.Mail.MailAddress(emailAddress);
      return addr.Address == emailAddress;
    }
    catch
    {
      return false;
    }
  }

  private static string GenerateSalt()
  {
    var bytes = new byte[128 / 8];
    using var keyGenerator = RandomNumberGenerator.Create();
    keyGenerator.GetBytes(bytes);
    return BitConverter.ToString(bytes).Replace("-", "").ToLower();
  }

  internal static string HashPasswordWithSalt(string password, string salt)
  {
    var passwordBytes = Encoding.UTF8.GetBytes(password);
    var saltBytes = Encoding.UTF8.GetBytes(salt);
    var combinedBytes = new byte[passwordBytes.Length + saltBytes.Length];
    Array.Copy(passwordBytes, 0, combinedBytes, 0, passwordBytes.Length);
    Array.Copy(saltBytes, 0, combinedBytes, passwordBytes.Length, saltBytes.Length);
    using var sha256Hash = SHA256.Create();
    var hashBytes = sha256Hash.ComputeHash(combinedBytes);
    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
  }

  private async Task<Models.User> ConvertDbUserToModel(Db.User user)
  {
    var userSettings = (from userSetting in user.UserSettings!
      where userSetting.Active
      select new Models.UserSetting(userSetting.SettingName, userSetting.SettingValue)).ToList();

    var userFavorites = (from userFavorite in user.UserFavorites!
      where userFavorite.Active
      select new Models.UserFavorite(userFavorite.NextplaceId)).ToList();

    var u = new Models.User(user.Id, user.EmailAddress, userFavorites, userSettings);

    if (userFavorites.Count != 0)
    {
      var nextplaceIds = userFavorites.Select(u1 => u1.NextplaceId).ToList();
      var query = context.Property
        .Include(tg => tg.Predictions)!.ThenInclude(p => p.Miner)
        .Include(tg => tg.EstimateStats)
        .AsQueryable();
      
      query = query.Where(p => nextplaceIds.Contains(p.NextplaceId));
      var properties = await PropertyController.GetProperties(query, new PropertyFilter());

      
      foreach (var userFavorite in userFavorites)
      {
        userFavorite.Property = properties.FirstOrDefault(p => p.NextplaceId == userFavorite.NextplaceId);
      }
    } 
    
    return u;
  }
}