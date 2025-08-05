using A2ACustomerService.Models;

namespace A2ACustomerService.Services;

public interface ITicketService
{
    Task<CustomerTicket> SubmitTicketAsync(SubmitTicketRequest request);
    Task<CustomerTicket?> GetTicketAsync(string ticketId);
    Task<List<CustomerTicket>> GetAllTicketsAsync();
    Task<List<AgentInfo>> GetAgentsAsync();
}
