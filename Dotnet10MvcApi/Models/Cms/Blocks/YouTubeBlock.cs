using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;

namespace Dotnet10MvcApi.Models.Cms.Blocks
{
    [BlockType(Name = "YouTube Video", Category = "Media", Icon = "fab fa-youtube")]
    public class YouTubeBlock : Block
    {
        [Field(Title = "YouTube Video URL or ID")]
        public StringField VideoUrl { get; set; } = new();

        [Field(Title = "Optional Caption / Title")]
        public StringField Caption { get; set; } = new();

        /// <summary>
        /// Helper to extract clean YouTube embed URL from standard watch/share URLs
        /// </summary>
        public string GetEmbedUrl()
        {
            if (string.IsNullOrWhiteSpace(VideoUrl?.Value))
                return string.Empty;

            var val = VideoUrl.Value.Trim();

            // Handles https://www.youtube.com/watch?v=VIDEO_ID
            if (val.Contains("youtube.com/watch?v="))
            {
                var parts = val.Split("v=");
                if (parts.Length > 1)
                {
                    var id = parts[1].Split('&')[0];
                    return $"https://www.youtube.com/embed/{id}";
                }
            }
            // Handles https://youtu.be/VIDEO_ID
            if (val.Contains("youtu.be/"))
            {
                var parts = val.Split("youtu.be/");
                if (parts.Length > 1)
                {
                    var id = parts[1].Split('?')[0];
                    return $"https://www.youtube.com/embed/{id}";
                }
            }
            // Handles direct embed URL or VIDEO_ID
            if (val.StartsWith("http"))
                return val;

            return $"https://www.youtube.com/embed/{val}";
        }
    }
}
