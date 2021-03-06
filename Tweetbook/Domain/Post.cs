using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tweetbook.Domain
{
    public class Post
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public ICollection<Tag> Tags { get; set; }
    }
}
