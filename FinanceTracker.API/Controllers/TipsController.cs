using Microsoft.AspNetCore.Mvc;
using FinanceTracker.API.Services;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/v1/tips")]
public class TipsController : ControllerBase
{
    private readonly ITipsService _tipsService;

    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public TipsController(ITipsService tipsService)
    {
        _tipsService = tipsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTips()
    {
        var result = await _tipsService.GetTipsAsync(DefaultUserId);
        return Ok(result);
    }
}
