using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Services;

public interface ITipsService
{
    Task<TipsResponseDto> GetTipsAsync(Guid userId);
}
