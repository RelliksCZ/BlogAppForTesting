using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [StringLength(300)]
        public string? Subtitle { get; set; }

        [Required]
        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("Author")]
        public string? AuthorId { get; set; }

        public IdentityUser? Author { get; set; }

        public string? FileName { get; set; } // název souboru (relativně)
    }
}
