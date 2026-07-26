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

        [Region(Title = "Author / Byline")]
        public StringField AuthorName { get; set; } = new();

        [Region(Title = "Footnotes & Citations", Description = "Optional numbered references or source citations displayed at the bottom of the article.")]
        public HtmlField Citations { get; set; } = new();

        [Region(Title = "Comment Moderation", Description = "Check this box to hold visitor comments in the moderation queue (/manager/comments) prior to publishing. Uncheck to allow comments to be auto-approved and displayed immediately upon submission.")]
        public CheckBoxField RequireModeration { get; set; } = new() { Value = true };
    }
}
