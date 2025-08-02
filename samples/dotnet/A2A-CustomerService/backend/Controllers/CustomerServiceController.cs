using A2ACustomerService.Models;
using A2ACustomerService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace A2ACustomerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerServiceController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IHubContext<CustomerServiceHub> _hubContext;
    private readonly ILogger<CustomerServiceController> _logger;

    public CustomerServiceController(
        ITicketService ticketService,
        IHubContext<CustomerServiceHub> hubContext,
        ILogger<CustomerServiceController> logger)
    {
        _ticketService = ticketService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<CustomerTicket>> SubmitTicket([FromBody] SubmitTicketRequest request)
    {
        try
        {
            _logger.LogInformation("Submitting new ticket for customer: {CustomerName}", request.CustomerName);

            var ticket = await _ticketService.SubmitTicketAsync(request);

            // Send real-time update to clients
            await _hubContext.Clients.All.SendAsync("TicketCreated", ticket);

            return Ok(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting ticket for customer: {CustomerName}", request.CustomerName);
            return StatusCode(500, new { error = "Failed to submit ticket" });
        }
    }

    [HttpGet("tickets/{ticketId}")]
    public async Task<ActionResult<CustomerTicket>> GetTicket(string ticketId)
    {
        try
        {
            var ticket = await _ticketService.GetTicketAsync(ticketId);

            if (ticket == null)
            {
                return NotFound(new { error = "Ticket not found" });
            }

            return Ok(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticket: {TicketId}", ticketId);
            return StatusCode(500, new { error = "Failed to retrieve ticket" });
        }
    }

    [HttpGet("tickets")]
    public async Task<ActionResult<List<CustomerTicket>>> GetAllTickets()
    {
        try
        {
            var tickets = await _ticketService.GetAllTicketsAsync();
            return Ok(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tickets");
            return StatusCode(500, new { error = "Failed to retrieve tickets" });
        }
    }

    [HttpGet("agents")]
    public async Task<ActionResult<List<AgentInfo>>> GetAgents()
    {
        try
        {
            var agents = await _ticketService.GetAgentsAsync();
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agents");
            return StatusCode(500, new { error = "Failed to retrieve agents" });
        }
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        var isReal = _ticketService is A2ATicketService;
        return Ok(new
        {
            implementation = isReal ? "real" : "mock",
            timestamp = DateTime.UtcNow
        });
    }
}
