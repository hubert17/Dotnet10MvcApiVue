using Piranha.AttributeBuilder;
using Piranha.Models;

namespace Dotnet10MvcApi.Models.Cms
{
    [PageType(Title = "Blog Archive", IsArchive = true)]
    public class BlogArchivePage : Page<BlogArchivePage>
    {
    }
}
