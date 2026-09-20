using Microsoft.AspNetCore.Identity;

namespace Core.Domain.Identity.AspNetIdentity
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CompanyName { get; set; }
        public string? ProfilePictureName { get; set; }
        public bool? IsBlocked { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? CreatedById { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedById { get; set; }

        //-----------------------//
        // Propriétés de navigation (Relations)

        // Un utilisateur peut être un CabinetComptabilite
        // public AccountingCompanyProfil? CabinetComptabiliteProfile { get; set; }

        // Un utilisateur peut être un ComptableIndividuel
        // public IndividualAccountantProfile? IndividuelComptabiliteProfile { get; set; }
        // Un utilisateur peut être un Client
        // public ClientProfile? ClientProfile { get; set; }
        // Un utilisateur peut être un Collaborateur
        // public CollaboratorProfile? CoIlaboratorProfile { get; set; }

        //-----------------------//
        public ApplicationUser()
        {
        }

        public ApplicationUser(
            string email,
            string firstName,
            string lastName,
            string companyName = "",
            string createdById = ""
        )
        {
            EmailConfirmed = true;
            IsBlocked = false;
            IsDeleted = false;
            CreatedAt = DateTime.UtcNow;
            Email = email.Trim();
            UserName = Email;
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            CompanyName = companyName.Trim();
            CreatedById = createdById.Trim();
        }
    }
}