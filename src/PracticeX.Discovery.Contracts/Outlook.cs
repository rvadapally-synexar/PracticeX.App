namespace PracticeX.Discovery.Contracts;

public sealed record OutlookOAuthStartResponse(string AuthorizeUrl, string State);

public sealed record OutlookScanRequest(int? Top, DateTimeOffset? Since);
