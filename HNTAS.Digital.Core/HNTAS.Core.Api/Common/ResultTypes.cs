using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Common;

public record ValidationGateResult(
  bool IsValid,
    string? Message = null,
    string? Detail = null,
    int StatusCode = 200,
    List<KpiSubmissionApiError>? Errors = null
);

