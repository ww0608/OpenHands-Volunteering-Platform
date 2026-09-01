//By ANGEL
namespace OpenHandsVolunteerPlatform.Models
{
    public class Certificate
    {
        public string CertificateId { get; set; } = Guid.NewGuid().ToString();

        public string VolunteerId { get; set; }
        public Volunteer Volunteer { get; set; }

        public string OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public string OpportunityId { get; set; }
        public Opportunity Opportunity { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.Now;

        public string? PdfPath { get; set; } // path after PDF is generated
    }
}
