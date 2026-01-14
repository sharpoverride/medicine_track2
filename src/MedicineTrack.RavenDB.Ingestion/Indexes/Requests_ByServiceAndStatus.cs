using MedicineTrack.RavenDB.Ingestion.Models;
using Raven.Client.Documents.Indexes;

namespace MedicineTrack.RavenDB.Ingestion.Indexes;

/// <summary>
/// Index for querying requests by service name, success status, and response code
/// Enables efficient queries for error rates, request volumes, and performance analysis
/// </summary>
public class Requests_ByServiceAndStatus : AbstractIndexCreationTask<RequestDocument>
{
    public Requests_ByServiceAndStatus()
    {
        Map = requests => from request in requests
                         select new
                         {
                             request.CloudRoleName,
                             request.Success,
                             request.ResultCode,
                             request.Timestamp,
                             request.DurationMs
                         };
    }
}
