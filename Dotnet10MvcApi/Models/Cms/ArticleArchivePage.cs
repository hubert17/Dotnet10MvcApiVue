using Piranha.AttributeBuilder;
using Piranha.Models;

namespace Dotnet10MvcApi.Models.Cms
{
    [PageType(Title = "Article Archive", IsArchive = true)]
    public class ArticleArchivePage : Page<ArticleArchivePage>
    {
    }
}
