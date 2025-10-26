using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models
{
    /// <summary>
    /// User entity with Keycloak integration
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public int UserTypeId { get; set; }

        [Required]
        public int CompanyId { get; set; }

        /// <summary>
        /// Keycloak user ID for SSO integration
        /// </summary>
        [MaxLength(100)]
        public string? KeycloakUserId { get; set; }

        /// <summary>
        /// Whether the user account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When the user was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time the user logged in
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        [ForeignKey("UserTypeId")]
        public virtual UserType? UserType { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public virtual ICollection<Order>? Orders { get; set; }
    }

    /// <summary>
    /// Company entity
    /// </summary>
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Company address line 1
        /// </summary>
        [MaxLength(255)]
        public string? AddressLine1 { get; set; }

        /// <summary>
        /// Company address line 2
        /// </summary>
        [MaxLength(255)]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// City
        /// </summary>
        [MaxLength(100)]
        public string? City { get; set; }

        /// <summary>
        /// State/Province
        /// </summary>
        [MaxLength(100)]
        public string? State { get; set; }

        /// <summary>
        /// Postal/Zip code
        /// </summary>
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        [MaxLength(100)]
        public string? Country { get; set; }

        /// <summary>
        /// Company phone number
        /// </summary>
        [MaxLength(50)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Company website
        /// </summary>
        [MaxLength(255)]
        public string? Website { get; set; }

        /// <summary>
        /// TTB (Alcohol and Tobacco Tax and Trade Bureau) permit number
        /// </summary>
        [MaxLength(50)]
        public string? TtbPermitNumber { get; set; }

        /// <summary>
        /// Whether the company account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When the company was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time company info was updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<User>? Users { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }
    }

    /// <summary>
    /// User type/role entity
    /// </summary>
    public class UserType
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<User>? Users { get; set; }
    }
}
