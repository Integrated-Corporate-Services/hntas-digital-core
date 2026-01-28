
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace HNTAS.Core.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HNDataImportExportController : ControllerBase
    {
        private readonly IHNDataImportExportService _service;

        public HNDataImportExportController(IHNDataImportExportService service)
        {
            _service = service;
        }

        [HttpGet("hn-users-orgs")]
        public async Task<IActionResult> GetJson()
        {
            var rows = await _service.GetAllHeatNetworkRowsAsync();
            return Ok(rows);
        }


        [HttpGet("hn-users-orgs.csv")]
        public async Task<IActionResult> GetCsv([FromQuery] int? take = 100)
        {
            // Get the flattened rows (already filtered to ResponsiblePerson per your service)
            var rows = await _service.GetAllHeatNetworkRowsAsync();

            // Apply optional take
            if (take.HasValue && take.Value > 0)
            {
                rows = rows.Take(take.Value).ToList();
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("UserEmailId,OneloginId,OrganisationName,OrganisationId,OrgStreetAddress,OrgTown,OrgPostcode,PhoneNumber,CompaniesHouseNo,DateOfOrgRegistration,HnId,HnName,DateOfHnRegistration,RegistrationSource,ECStreetAddress,ECTown,ECPostcode,ECLatitude,ECLongitude");

            foreach (var r in rows)
            {
                // Null-safe values
                var userEmailId = r.UserEmailId;
                var oneLoginId = r.OneloginId;
                var organisationName = r.OrganisationName;
                var organisationId = r.OrganisationId;
                var orgStreetAddress = r.OrgStreetAddress;
                var orgTown = r.OrgTown;
                var orgPostcode = r.OrgPostcode;
                var phoneNumber = r.PhoneNumber;
                var companiesHouseNo = r.CompaniesHouseNo;
                var dateOfOrgRegistration = r.DateOfOrgRegistration;
                var hnId = r.HnId;
                var hnName = r.HnName;
                var dateOfHnRegistration = r.DateOfHnRegistration;
                var registrationSource = r.RegistrationSource;
                var ecStreetAddress = r.ECStreetAddress;
                var ecTown = r.ECTown;
                var ecPostcode = r.ECPostcode;
                var ecLatitude = r.ECLatitude;
                var ecLongitude = r.ECLongitude;

                // CSV-escape each field: wrap in double quotes and escape inner quotes by doubling them
                string Esc(string s)
                {
                    // Also normalize line breaks to avoid breaking rows
                    s = s.Replace("\r\n", "\n").Replace("\r", "\n");
                    return $"\"{s.Replace("\"", "\"\"")}\"";
                }

                sb.Append(Esc(userEmailId)).Append(',').Append(Esc(oneLoginId)).Append(',')
                  .Append(Esc(organisationName)).Append(',')
                  .Append(Esc(organisationId)).Append(',')
                  .Append(Esc(orgStreetAddress)).Append(',')
                  .Append(Esc(orgTown)).Append(',')
                  .Append(Esc(orgPostcode)).Append(',')
                  .Append(Esc(phoneNumber)).Append(',')
                  .Append(Esc(companiesHouseNo)).Append(',')
                  .Append(Esc(dateOfOrgRegistration)).Append(',')
                  .Append(Esc(hnId)).Append(',')
                  .Append(Esc(hnName)).Append(',')
                  .Append(Esc(dateOfHnRegistration)).Append(',')
                  .Append(Esc(registrationSource)).Append(',')
                  .Append(Esc(ecStreetAddress)).Append(',')
                  .Append(Esc(ecTown)).Append(',')
                  .Append(Esc(ecPostcode)).Append(',')
                  .Append(Esc(ecLatitude)).Append(',')
                  .Append(Esc(ecLongitude)).AppendLine();                
            }

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

            // Suggest a filename with timestamp; browsers will show a Save dialog
            var fileName = $"hn-users-orgs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            // Return as a file result with text/csv content type and UTF-8 encoding
            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
