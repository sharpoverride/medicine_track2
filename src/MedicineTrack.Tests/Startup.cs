using Microsoft.Extensions.DependencyInjection;

namespace MedicineTrack.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // This is intentionally left empty.
        // We are creating validators manually in the tests,
        // so we don't need to register any services here.
        // This class is only here to satisfy the test runner's
        // requirement for a startup class.
    }
}