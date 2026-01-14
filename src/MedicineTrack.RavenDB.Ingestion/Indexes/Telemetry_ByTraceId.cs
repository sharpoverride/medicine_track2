using MedicineTrack.RavenDB.Ingestion.Models;
using Raven.Client.Documents.Indexes;

namespace MedicineTrack.RavenDB.Ingestion.Indexes;

/// <summary>
/// Multi-map index for querying all telemetry types by OperationId (TraceId)
/// Enables distributed tracing by correlating traces, requests, and dependencies across services
/// </summary>
public class Telemetry_ByTraceId : AbstractMultiMapIndexCreationTask<Telemetry_ByTraceId.Result>
{
    public class Result
    {
        public string? OperationId { get; set; }
        public string? CloudRoleName { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ItemType { get; set; }
    }

    public Telemetry_ByTraceId()
    {
        AddMap<TraceDocument>(traces =>
            from trace in traces
            select new Result
            {
                OperationId = trace.OperationId,
                CloudRoleName = trace.CloudRoleName,
                Timestamp = trace.Timestamp,
                ItemType = trace.ItemType
            });

        AddMap<RequestDocument>(requests =>
            from request in requests
            select new Result
            {
                OperationId = request.OperationId,
                CloudRoleName = request.CloudRoleName,
                Timestamp = request.Timestamp,
                ItemType = request.ItemType
            });

        AddMap<DependencyDocument>(dependencies =>
            from dependency in dependencies
            select new Result
            {
                OperationId = dependency.OperationId,
                CloudRoleName = dependency.CloudRoleName,
                Timestamp = dependency.Timestamp,
                ItemType = dependency.ItemType
            });
    }
}
