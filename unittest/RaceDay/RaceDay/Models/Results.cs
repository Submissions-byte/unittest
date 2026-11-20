using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    public class Results
    {
        [Key]
        public int ResultID {  get; set; }
        [Required]
        public TimeOnly FinishingTime {  get; set; }
        [Required]
        public int FinishingPosition { get; set; }
        [Required]
        public int EnrolmentID { get; set; }
        [ForeignKey(nameof(EnrolmentID))]
        public Enrolment? enrolment { get; set; }
    }
}
