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

        [Region(Title = "Require Comment Moderation")]
        public CheckBoxField RequireModeration { get; set; } = new() { Value = true };
    }
}
