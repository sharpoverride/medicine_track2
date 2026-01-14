using MedicineTrack.RavenDB.Ingestion.Models;
using Raven.Client.Documents.Indexes;

namespace MedicineTrack.RavenDB.Ingestion.Indexes;

/// <summary>
/// Index for querying traces by service name, timestamp, and severity level
/// Enables efficient time-range queries and filtering by service and severity
/// </summary>
public class Traces_ByServiceAndTime : AbstractIndexCreationTask<TraceDocument>
{
    public Traces_ByServiceAndTime()
    {
        Map = traces => from trace in traces
                       select new
                       {
                           trace.CloudRoleName,
                           trace.Timestamp,
                           trace.SeverityLevel,
                           trace.OperationId
                       };
    }
}
