using Azure.Core;
using Mango.Services.AuthAPI.Data;
using Mango.Services.AuthAPI.Models;
using Mango.Services.AuthAPI.Models.Dto;
using Mango.Services.AuthAPI.Service.IService;
using Microsoft.AspNetCore.Identity;

namespace Mango.Services.AuthAPI.Service
{
	public class AuthService : IAuthService
	{
		private readonly AppDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly IJwtTokenGenerator _jwtTokenGenerator;

		public AuthService(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IJwtTokenGenerator jwtTokenGenerator)
		{
			_context = context;
			_userManager = userManager;
			_roleManager = roleManager;
			_jwtTokenGenerator = jwtTokenGenerator;
		}

		public async Task<bool> AssignRole(string email, string roleName)
		{
			var user = _context.ApplicationUsers.FirstOrDefault(q => q.Email.ToLower() == email.ToLower());

			if (user != null) 
			{
				if (!_roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
				{
					//create role if it does not exist
					_roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
				}

				await _userManager.AddToRoleAsync(user, roleName);
				return true;
			}
			return false;
		}

		public async Task<LoginResponseDto> Login(LoginRequestDto request)
		{
			var user = _context.ApplicationUsers.FirstOrDefault(q => q.UserName.ToLower() == request.UserName.ToLower());

			bool isValid = await _userManager.CheckPasswordAsync(user, request.Password);

			if (user == null || !isValid)
			{
				return new LoginResponseDto()
				{
					User = null,
					Token = string.Empty
				};
			}

			//if user was found, Generate JWT Token
			var roles = await _userManager.GetRolesAsync(user);
			var token = _jwtTokenGenerator.GenerateToken(user, roles);

			UserDto userDto = new() 
			{ 
				Email = user.Email,
				Id = user.Id,
				Name = user.Name,
				PhoneNumber = user.PhoneNumber
			};

			LoginResponseDto loginResponseDto = new LoginResponseDto() 
			{
				User = userDto,
				Token = token
			};

			return loginResponseDto;

		}

		public async Task<string> Register(RegistrationRequestDto request)
		{
			ApplicationUser user = new()
			{
				UserName = request.Email,
				Email = request.Email,
				NormalizedEmail = request.Email.ToUpper(),
				Name = request.Name,
				PhoneNumber = request.PhoneNumber
			};

			try
			{
				var result = await _userManager.CreateAsync(user, request.Password);
				if (result.Succeeded)
				{
					await _userManager.AddToRoleAsync(user, request.Role);
					return string.Empty;
				}
				else
				{
					return result.Errors.FirstOrDefault().Description;
				}
			}
			catch (Exception ex)
			{

			}

			return "Error Encountered";

		}
	}
}
