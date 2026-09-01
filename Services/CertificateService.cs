using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using System.Text;
using System.Text.RegularExpressions;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Services
{
    public class CertificateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CertificateService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<string> GenerateCertificateHtml(Volunteer volunteer, Opportunity opportunity, Organization organization)
        {
            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Certificate of Appreciation</title>
                    <style>
                        body {{ font-family: 'Georgia', serif; margin: 0; padding: 40px; background: #f5f7ff; }}
                        .certificate {{ max-width: 800px; margin: 0 auto; background: white; padding: 50px; 
                                      border: 20px solid #8b92d1; border-radius: 20px; text-align: center; }}
                        h1 {{ color: #3f4079; font-size: 48px; margin-bottom: 30px; }}
                        .volunteer-name {{ font-size: 32px; font-weight: bold; margin: 30px 0; color: #2d3e4f; }}
                        .event-name {{ font-size: 24px; color: #8b92d1; margin: 20px 0; }}
                        .date {{ font-size: 18px; color: #8e94a7; margin: 20px 0; }}
                        .org-name {{ margin-top: 40px; font-size: 16px; }}
                        .signature {{ margin-top: 50px; border-top: 1px solid #ccc; padding-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='certificate'>
                        <h1>CERTIFICATE OF APPRECIATION</h1>
                        <p>This certificate is proudly presented to</p>
                        <div class='volunteer-name'>{volunteer.Name}</div>
                        <p>in recognition of their valuable contribution to</p>
                        <div class='event-name'>{opportunity.Title}</div>
                        <p>organized by</p>
                        <div class='org-name'>{organization.OrganizationName}</div>
                        <div class='date'>Date: {opportunity.Date.ToString("MMMM d, yyyy")}</div>
                        <div class='signature'>
                            <p>{organization.OrganizationName}</p>
                            <p><small>Issued on {DateTime.Now.ToString("MMMM d, yyyy")}</small></p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return html;
        }

        public async Task SaveCertificateRecord(string volunteerId, string organizationId, string opportunityId, string pdfPath)
        {
            _context.Certificates.Add(new Certificate
            {
                CertificateId = Guid.NewGuid().ToString(),
                VolunteerId = volunteerId,
                OrganizationId = organizationId,
                OpportunityId = opportunityId,
                IssuedAt = DateTime.Now,
                PdfPath = pdfPath
            });
            await _context.SaveChangesAsync();
        }
    }
}