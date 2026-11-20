using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    // public enum Type { Run, Swim, Cycle }
    [Index(nameof(ParticipantID), nameof(EventID), IsUnique = true)]
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }
        [Required]
        public int ParticipantID { get; set; }
        [ForeignKey(nameof(ParticipantID))]
        public Participant? participant { get; set; }
        [Required]
        public int EventID { get; set; }
        [ForeignKey(nameof(EventID))]
        public Event? @event { get; set; }
        [Required]
        public int CategoryID { get; set; }
        [ForeignKey(nameof(CategoryID))]
        public Category? category { get; set; }
        [Required]
        public DateOnly EnrolmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
