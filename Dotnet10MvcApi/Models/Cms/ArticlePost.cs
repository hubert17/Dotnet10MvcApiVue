using System;
using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Models;
using Piranha.Extend.Fields;

namespace Dotnet10MvcApi.Models.Cms
{
    [PostType(Title = "Article Post")]
    public class ArticlePost : Post<ArticlePost>
    {
        [Region(Title = "Subtitle")]
        public StringField Subtitle { get; set; } = new();

        [Region(Title = "Reading Time")]
        public StringField ReadingTime { get; set; } = new();
    }
}
