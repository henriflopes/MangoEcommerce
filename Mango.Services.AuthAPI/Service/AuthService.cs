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

		public AuthService(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
		{
			_context = context;
			_userManager = userManager;
			_roleManager = roleManager;
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
				Token = ""
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
