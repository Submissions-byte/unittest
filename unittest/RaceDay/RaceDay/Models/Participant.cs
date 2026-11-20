using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class Participant
    {
        [Key]
        public int ParticipantID { get; set; }
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
        [Required, MaxLength(10), MinLength(10)]
        public string PhoneNumber { get; set; }
        [Required]
        public int Age { get; set; }
        [Required, MaxLength(255)]
        public string Password { get; set; }
        [MaxLength(500)]
        public string AvatarUrl { get; set; }
    }
}
