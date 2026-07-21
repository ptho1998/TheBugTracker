using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using TheBugTracker.Data;
using TheBugTracker.Models;

namespace TheBugTracker.Controllers
{
    [Route("uploads")]
    [ApiController]
    public class UploadController(ApplicationDbContext context) : ControllerBase
    {
        /// <summary>
        /// Get Upload
        /// </summary>
        /// <remarks>
        /// Returns an image uploaded by a user as a profile picture
        /// </remarks>
        /// <param name="id">The ID of the image to load</param>
        /// <returns>An image file, or 404 Not Found if it does not exist</returns>
        //[SwaggerIgnore]
        [HttpGet("{id:guid}")]
        [OutputCache(VaryByRouteValueNames = ["id"], Duration = 60 * 60 * 24)]
        public async Task<IActionResult> GetImage(Guid id)
        {
            FileUpload? image = await context.Uploads.FirstOrDefaultAsync(i => i.Id == id);

            if (image is null) return NotFound();

            return File(image.Data!, image.Type!);
        }
    }
}
