using A2A;
using A2A.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace A2ACustomerService
{
    public static class TestExtensions
    {
        public static void TestAvailableMethods()
        {
            // This will help us see what extension methods are available
            WebApplication app = null!;
            ITaskManager taskManager = null!;

            // This should work
            app.MapA2A(taskManager, "/test");

            // Let's try different method names that might exist
            // app.MapWellKnownAgentCard(taskManager, "/test");
            // app.MapAgentCard(taskManager, "/test");
            // app.MapAgent(taskManager, "/test");
        }
    }
}
