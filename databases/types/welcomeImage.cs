using System.ComponentModel.DataAnnotations;

namespace Database.Types
{
    public class WelcomeImage
    {
        [Key]
        public ulong Id { get; set; }

        [Required]
        public byte[] Image { get; set; }
    }
}