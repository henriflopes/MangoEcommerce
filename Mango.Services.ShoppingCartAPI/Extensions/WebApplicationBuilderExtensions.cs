using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Mango.Services.ShoppingCartAPI.Extensions
{
	public static class WebApplicationBuilderExtensions
	{
		public static WebApplicationBuilder AddAppAuthentication(this WebApplicationBuilder builder) 
		{
			var settingsSection = builder.Configuration.GetSection("ApiSettings");

			var secret = settingsSection.GetValue<string>("Secret");
			var issuer = settingsSection.GetValue<string>("Issuer");
			var audience = settingsSection.GetValue<string>("Audience");

			var key = Encoding.ASCII.GetBytes(secret);

			builder.Services.AddAuthentication(q =>
			{
				q.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				q.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			}).AddJwtBearer(q =>
			{
				q.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = true,
					ValidIssuer = issuer,
					ValidateAudience = true,
					ValidAudience = audience
				};
			});

			return builder;
		}
	}
}
