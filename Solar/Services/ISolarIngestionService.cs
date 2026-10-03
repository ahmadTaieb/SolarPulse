using System.Threading;
using System.Threading.Tasks;
using Solar.Models;

namespace Solar.Services;

/// <summary>
/// Orchestration contract for synchronizing telemetry from the cloud platform, computing metrics, and persisting to the database.
/// </summary>
public interface ISolarIngestionService
{
    /// <summary>
    /// Executes synchronization by fetching recent telemetry, deduplicating against stored records, computing daily metrics, and persisting to SQL Server.
    /// </summary>
    Task<SyncResultDto> IngestDataAsync(CancellationToken cancellationToken = default);
}
