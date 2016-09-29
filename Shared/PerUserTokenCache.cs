using System;
using System.ComponentModel.DataAnnotations;

namespace CogsMinimizer.Shared
{
    public class PerUserTokenCache
    {
        [Key]
        public int Id { get; set; }
        public string webUserUniqueId { get; set; }
        public byte[] cacheBits { get; set; }
        public DateTime LastWrite { get; set; }
    }
}