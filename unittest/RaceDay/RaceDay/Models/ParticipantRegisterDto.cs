using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class ParticipantRegisterDto
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public int Age { get; set; }
        public string Password { get; set; }
        public string AvatarUrl { get; set; }
    }
}
