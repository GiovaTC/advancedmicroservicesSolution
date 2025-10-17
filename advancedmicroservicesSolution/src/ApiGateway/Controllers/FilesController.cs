using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdvancedMicroservicesSolution.src.ApiGateway.Services;

namespace AdvancedMicroservicesSolution.src.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService) => _fileService = fileService;

        [HttpPost]
        [Authorize]
        [RequestSizeLimit(10_485_760)] // Limit to 10 MB]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                var saved = await _fileService.SaveFileAsync(file);
                return Ok(new { fileName = saved });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
