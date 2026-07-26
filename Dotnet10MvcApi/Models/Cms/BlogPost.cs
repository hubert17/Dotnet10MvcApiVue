using System;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Models;
using Piranha.Extend.Fields;

namespace Dotnet10MvcApi.Models.Cms
{
    [PostType(Title = "Blog Post")]
    public class BlogPost : Post<BlogPost>
    {
        [Region(Title = "Subtitle")]
        public StringField Subtitle { get; set; } = new();

        [Region(Title = "Author Name")]
        public StringField AuthorName { get; set; } = new();

        [Region(Title = "Comment Moderation", Description = "Check this box to hold visitor comments in the moderation queue (/manager/comments) prior to publishing. Uncheck to allow comments to be auto-approved and displayed immediately upon submission.")]
        public CheckBoxField RequireModeration { get; set; } = new() { Value = true };
    }
}
