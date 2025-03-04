using System.ComponentModel.DataAnnotations;

namespace ApiLib.Models
{
    public class BaseModel
    {
        public int ID { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [Required]
        public bool Deleted { get; set; }
    }
}
