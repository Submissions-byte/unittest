using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class Organiser
    {
        [Key]
        public int OrganiserID { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        [Required, MaxLength(255)]
        public string Password { get; set; }
    }
}
