using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    public class Location
    {
        [Key]
        public int LocationID { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        [Required, MaxLength(100)]
        public string GPS_coordinates { get; set; }
        [Required] [MaxLength(20)]
        public string StartingLineLocation { get; set; }
        [Required, MaxLength(20)]
        public string FinishLineLocation { get; set; }
        [Required]
        [Precision(10, 2)]
        public decimal RouteLength { get; set; }
        //[ForeignKey(nameof(OrganiserID))]
        [Required]
        public int OrganiserID { get; set; }
        [ForeignKey(nameof(OrganiserID))]
        public Organiser? Organiser { get; set; }

    }
}
