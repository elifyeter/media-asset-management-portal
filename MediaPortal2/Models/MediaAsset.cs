using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediaPortal2.Models
{
    public class MediaAsset
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }   

        [Required]
        public string FilePath { get; set; }  

        [Required]
        public string MediaType { get; set; }  

        public string? Title { get; set; }     
        public string? Tags { get; set; }      

        public DateTime UploadDate { get; set; } = DateTime.Now;
     
        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
    }
}