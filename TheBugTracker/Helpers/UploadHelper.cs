using System.Text.RegularExpressions;
using TheBugTracker.Client.Helpers;
using TheBugTracker.Models;

namespace TheBugTracker.Helpers
{
    public static partial class UploadHelper
    {
        public static readonly string DefaultProfilePictureUrl = "https://api.dicebear.com/10.x/glass/svg";

        public static async Task<FileUpload> GetFileUploadAsync(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] data = ms.ToArray();

            if (ms.Length > BrowserFileHelper.MaxFileSize)
            {
                throw new Exception("The selected file is too large.");
            }

            FileUpload upload = new()
            {
                Id = Guid.NewGuid(),
                Data = data,
                Type = file.ContentType
            };

            return upload;
        }

        public static FileUpload GetFileUpload(string dataUrl)
        {
            GroupCollection matchGroups = DataUrlRegex().Match(dataUrl).Groups;

            if (matchGroups.ContainsKey("type") && matchGroups.ContainsKey("data"))
            {
                string contentType = matchGroups["type"].Value;
                string base64String = matchGroups["data"].Value;

                byte[] imageData = Convert.FromBase64String(base64String);

                if (imageData.Length <= BrowserFileHelper.MaxFileSize)
                {
                    return new FileUpload()
                    {
                        Id = Guid.NewGuid(),
                        Data = imageData,
                        Type = contentType
                    };
                }
            }

            throw new IOException("Data URL was either invalid or too large");
        }

        [GeneratedRegex(@"data:(?<type>.+?);base64,(?<data>.+)")]
        private static partial Regex DataUrlRegex();
    }
}
