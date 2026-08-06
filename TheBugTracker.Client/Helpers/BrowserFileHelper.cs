using Microsoft.AspNetCore.Components.Forms;

namespace TheBugTracker.Client.Helpers
{
    public static class BrowserFileHelper
    {
        public static readonly int MaxFileSize = 5 * 1024 * 1024; //5MB

        public static async Task<byte[]> GetFileData(IBrowserFile file)
        {
            if (file.Size < MaxFileSize)
            {
                try
                {
                    await using Stream fileStream = file.OpenReadStream(MaxFileSize);
                    await using MemoryStream ms = new MemoryStream();
                    await fileStream.CopyToAsync(ms);

                    return ms.ToArray();
                }
                catch (Exception)
                {
                    throw;
                }
            }

            throw new IOException("File was either invalid or too large");
        }
    }
}
