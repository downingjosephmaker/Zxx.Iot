using Microsoft.Net.Http.Headers;

namespace IotWebApi
{
    public class FileHelper
    {
        public bool IsMultipartContentType(string contentType)
        {
            return
                !string.IsNullOrEmpty(contentType) &&
                contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public string GetBoundary(HttpContext context)
        {
            var mediaTypeHeaderContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);

            return HeaderUtilities.RemoveQuotes(mediaTypeHeaderContentType.Boundary).Value;
        }

        public string GetFileName(string contentDisposition)
        {
            return contentDisposition
                .Split(';')
                .SingleOrDefault(part => part.Contains("filename"))
                .Split('=')
                .Last()
                .Trim('"');
        }

    }
}
