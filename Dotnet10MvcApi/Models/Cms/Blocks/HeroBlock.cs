using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Extend.Fields;

namespace Dotnet10MvcApi.Models.Cms.Blocks
{
    [BlockType(Name = "Hero Section", Category = "Content", Icon = "fas fa-star")]
    public class HeroBlock : Block
    {
        [Field(Title = "Badge / Eyebrow")]
        public StringField Badge { get; set; } = new();

        [Field(Title = "Hero Title")]
        public StringField Title { get; set; } = new();

        [Field(Title = "Subtitle / Ingress")]
        public TextField Subtitle { get; set; } = new();

        [Field(Title = "Button Text")]
        public StringField ButtonText { get; set; } = new();

        [Field(Title = "Button URL")]
        public StringField ButtonUrl { get; set; } = new();
    }
}
