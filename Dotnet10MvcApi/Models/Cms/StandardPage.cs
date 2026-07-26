using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Models;
using Piranha.Extend.Fields;

namespace Dotnet10MvcApi.Models.Cms
{
    [PageType(Title = "Standard Page")]
    public class StandardPage : Page<StandardPage>
    {
        [Region(Title = "Subtitle")]
        public StringField Subtitle { get; set; } = new();
    }
}
