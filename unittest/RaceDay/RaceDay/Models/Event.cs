using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    public enum Type { Run, Swim, Cycle }
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        [Required, MaxLength(50)]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]      
        public DateOnly EventDate { get; set; }
        [Required]
        public TimeOnly StartTime { get; set; }
        [Required]
        public TimeOnly EndTime { get; set; }
        [Required]
        public Type EventType { get; set; } //alter db context to use this.
        [MaxLength(500)]
        public string BannerUrl { get; set; }
        [Required]
        public int LocationID { get; set; }
        [ForeignKey(nameof(LocationID))]
        public Location? Location { get; set; }
        [Required]
        public int OrganiserID { get; set; }
        [ForeignKey(nameof(OrganiserID))]
        public Organiser? Organiser { get; set; }
    }
}
