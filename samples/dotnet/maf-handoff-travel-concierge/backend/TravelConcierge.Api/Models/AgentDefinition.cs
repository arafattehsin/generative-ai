namespace TravelConcierge.Api.Models;

public sealed record AgentDefinition(
    string Id,
    string Name,
    string Role,
    string Accent,
    string Icon,
    string Summary);

public static class TravelAgents
{
    public static readonly AgentDefinition Triage = new(
        "triage",
        "Journey Triage",
        "Owns intake, priority, and the next best specialist",
        "#0f766e",
        "route",
        "Keeps the traveller oriented and decides who owns the next turn.");

    public static readonly AgentDefinition Flight = new(
        "flight",
        "Flight Recovery",
        "Finds safe reroute options and protects time-critical constraints",
        "#0f766e",
        "plane",
        "Handles cancelled, delayed, missed, and rebooking-heavy flight problems.");

    public static readonly AgentDefinition Stay = new(
        "stay",
        "Stay & Ground",
        "Coordinates hotel, airport transfer, and overnight disruption logistics",
        "#0f766e",
        "bed",
        "Handles where the traveller sleeps, how they move, and what receipts matter.");

    public static readonly AgentDefinition Insurance = new(
        "insurance",
        "Travel Cover",
        "Explains claim evidence, coverage questions, and reimbursement sequence",
        "#0f766e",
        "shield",
        "Turns disruption actions into a clean evidence trail for claims.");

    public static readonly AgentDefinition Loyalty = new(
        "loyalty",
        "Loyalty Desk",
        "Applies status, fare, and preference context to the service path",
        "#0f766e",
        "star",
        "Uses membership status and traveller preferences to shape the recovery plan.");

    public static readonly AgentDefinition Human = new(
        "human",
        "Recovery specialist",
        "Connects policy, safety, and exception decisions to a person",
        "#0f766e",
        "user",
        "A real operations employee takes ownership of the conversation here.");

    public static IReadOnlyList<AgentDefinition> All { get; } =
    [
        Triage,
        Flight,
        Stay,
        Insurance,
        Loyalty,
        Human,
    ];

    public static AgentDefinition Resolve(string? nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId))
        {
            return Triage;
        }

        return All.FirstOrDefault(agent =>
            string.Equals(agent.Id, nameOrId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(agent.Name, nameOrId, StringComparison.OrdinalIgnoreCase)) ?? Triage;
    }
}
