using Mando.Services.RewardAPI.Message;
using Mando.Services.RewardAPI.Models.Dto;
using Mango.Services.RewardAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Mango.Services.RewardAPI.Services
{
	public class RewardService : IRewardService
	{
		private DbContextOptions<AppDbContext> _dbOptions;

		public RewardService(DbContextOptions<AppDbContext> dbOptions)
		{
			_dbOptions = dbOptions;
		}

		public async Task UpdateRewards(RewardsMessage rewardsMessage)
		{
			try
			{
				Rewards rewards = new()
				{
					OrderId = rewardsMessage.OrderId,
					RewardsActivity = rewardsMessage.RewardsActivity,
					UserId = rewardsMessage.UserId,
					RewardsDate = DateTime.Now
				};

				await using var _context = new AppDbContext(_dbOptions);
				await _context.Rewards.AddAsync(rewards);
				await _context.SaveChangesAsync();
			}
			catch
			{
				
			}
		}
	}
}
