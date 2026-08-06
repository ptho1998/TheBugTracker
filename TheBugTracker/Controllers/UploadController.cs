using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Data;
using TheBugTracker.Models;

namespace TheBugTracker.Controllers
{
    [Route("api/uploads")]
    [ApiController]
    [Authorize]
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
        [SwaggerIgnore]
        [HttpGet("{id:guid}")]
        [OutputCache(VaryByRouteValueNames = ["id"], Duration = 60 * 60 * 24)]
        public async Task<IActionResult> GetImage(Guid id)
        {
            FileUpload? image = await context.Uploads.FirstOrDefaultAsync(i => i.Id == id);

            if (image is null || await context.Attachments.AnyAsync(a => a.UploadId == id))
            {
                return NotFound();
            }

            return File(image.Data!, image.Type!);
        }

        [HttpGet("/api/attachments/{uploadId:guid}")]
        [Authorize]
        public async Task<IActionResult> DownloadAttachment([FromRoute] Guid uploadId, [FromQuery] bool download = false)
        {
            UserInfo userInfo = UserInfoHelper.GetUserInfo(User)!;

            TicketAttachment? attachment = await context.Attachments
                .Include(a => a.Upload)
                .FirstOrDefaultAsync(a => a.UploadId == uploadId
                                          && a.Ticket!.Project!.CompanyId == userInfo.CompanyId);

            if (attachment is null)
            {
                return NotFound();
            }

            //Response.Headers.TryAdd("Content-Disposition", $"attachment; filename={attachment.FileName}");
            //return File(attachment.Upload!.Data!, attachment.Upload!.Type!, attachment.FileName);

            //Response.Headers.TryAdd("Content-Disposition", $"inline; filename=\"{attachment.FileName}\"");
            //return File(attachment.Upload!.Data!, attachment.Upload!.Type!);

            string disposition = download ? "attachment" : "inline";
            Response.Headers.TryAdd("Content-Disposition", $"{disposition}; filename={attachment.FileName}");
            return File(attachment.Upload!.Data!, attachment.Upload!.Type!);
        }
    }
}
