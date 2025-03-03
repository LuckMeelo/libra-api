using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using ApiLib.Models;

namespace LibrApi.Data.Models
{
    public class Book : BaseModel
    {
        [MaxLength(150)]
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Summary { get; set; } = string.Empty;

        [Required]
        public string Author { get; set; } = string.Empty;

        [MaxLength(150)]
        [Required]
        public string Genre { get; set; } = string.Empty;

        [Required]
        public int Rating { get; set; }

        [Required]
        public DateTime PublishedDate { get; set; }
    }
}