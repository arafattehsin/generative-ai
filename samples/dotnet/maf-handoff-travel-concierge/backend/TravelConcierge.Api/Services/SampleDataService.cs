using TravelConcierge.Api.Models;

namespace TravelConcierge.Api.Services;

public sealed class SampleDataService
{
    public IReadOnlyList<SampleScenario> GetSamples() =>
    [
        new(
            "cancelled-last-flight",
            "Cancelled Last Flight Home",
            "Maya Chen",
            "SYD-LAX-4817",
            "Critical",
            """
            My Qantas flight QF11 from Sydney to Los Angeles was cancelled after two delays. It is 9 PM and I am still at Sydney Airport with one checked bag and no rebooking. I have a medical appointment in Los Angeles tomorrow at 2 PM. I need the earliest confirmed way home.
            """,
            ["cancelled flight", "baggage"]),
        new(
            "missed-connection-family",
            "Missed Connection With Kids",
            "Omar Haddad",
            "MEL-SIN-LHR-2240",
            "High",
            """
            We missed our Singapore connection because the first leg left Melbourne late. I am travelling with two children, one bag is still tagged to London, and the desk says the next seats may be tomorrow. Can you help me work out who should handle flights, hotel, and baggage?
            """,
            ["missed connection", "family", "baggage"]),
        new(
            "storm-diversion-loyalty",
            "Storm Diversion With Status",
            "Priya Raman",
            "AKL-SFO-9072",
            "High",
            """
            Our San Francisco flight diverted to Honolulu due to weather. I have Platinum status, a flexible fare, and an onward meeting in Seattle. I need the fastest practical reroute and a clear note on what the airline versus insurance should cover.
            """,
            ["diversion", "loyalty", "reroute"]),
    ];
}
