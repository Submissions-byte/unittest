using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        [Required, MaxLength(50)]
        public string AgeRange { get; set; }
        [Required, MaxLength(50)]
        public string Distance {  get; set; }
        [Required]
        public int EventID { get; set; }
        [ForeignKey(nameof(EventID))]
        public Event? Event { get; set; }
    }
}
