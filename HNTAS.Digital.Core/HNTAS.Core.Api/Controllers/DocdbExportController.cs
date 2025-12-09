
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HNTAS.Core.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocdbExportController : ControllerBase
    {
        private readonly IDocdbExportService _service;

        public DocdbExportController(IDocdbExportService service)
        {
            _service = service;
        }

        [HttpGet("hn-users-orgs")]
        public async Task<IActionResult> GetJson([FromQuery] int? take = 100)
        {
            var rows = await _service.GetFlattenedHeatNetworkUserOrgAsync();
            return Ok(rows);
        }


        [HttpGet("hn-users-orgs.csv")]
        public async Task<IActionResult> GetCsv([FromQuery] int? take = 100)
        {
            // Get the flattened rows (already filtered to ResponsiblePerson per your service)
            var rows = await _service.GetFlattenedHeatNetworkUserOrgAsync();

            // Apply optional take
            if (take.HasValue && take.Value > 0)
            {
                rows = rows.Take(take.Value).ToList();
            }

            // Build CSV in-memory
            // Header: HnId, HeatNetworkName, Location, OrgId, EmailId, OrganisationName
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("HnId,HeatNetworkName,Location,OrgId,EmailId,OrganisationName");

            foreach (var r in rows)
            {
                // Null-safe values
                var hnId = r.HnId ?? string.Empty;
                var name = r.HeatNetworkName ?? string.Empty;
                var loc = r.Location ?? string.Empty;
                var orgId = r.OrgId ?? string.Empty;
                var email = r.EmailId ?? string.Empty;
                var orgNm = r.OrganisationName ?? string.Empty;

                // CSV-escape each field: wrap in double quotes and escape inner quotes by doubling them
                string Esc(string s)
                {
                    // Also normalize line breaks to avoid breaking rows
                    s = s.Replace("\r\n", "\n").Replace("\r", "\n");
                    return $"\"{s.Replace("\"", "\"\"")}\"";
                }

                sb.Append(Esc(hnId)).Append(',')
                  .Append(Esc(name)).Append(',')
                  .Append(Esc(loc)).Append(',')
                  .Append(Esc(orgId)).Append(',')
                  .Append(Esc(email)).Append(',')
                  .Append(Esc(orgNm)).AppendLine();
            }

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

            // Suggest a filename with timestamp; browsers will show a Save dialog
            var fileName = $"hn-users-orgs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            // Return as a file result with text/csv content type and UTF-8 encoding
            return File(csvBytes, "text/csv; charset=utf-8", fileName);

        }
    }
}
