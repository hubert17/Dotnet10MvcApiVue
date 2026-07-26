using System;
using System.Collections.Generic;
using System.Linq;
using Piranha.Models;

namespace Dotnet10MvcApi.Models.Cms
{
    public class CommentSectionViewModel
    {
        public Guid PostId { get; set; }
        public bool EnableComments { get; set; } = true;
        public IEnumerable<Comment> Comments { get; set; } = Enumerable.Empty<Comment>();
        public string FormAction { get; set; } = string.Empty;
    }
}
