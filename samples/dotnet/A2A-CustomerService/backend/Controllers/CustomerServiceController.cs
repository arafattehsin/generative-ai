using A2ACustomerService.Models;
using A2ACustomerService.Services;
using Microsoft.AspNetCore.Mvc;

namespace A2ACustomerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerServiceController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<CustomerServiceController> _logger;

    public CustomerServiceController(
        ITicketService ticketService,
        IConfigurationService configurationService,
        ILogger<CustomerServiceController> logger)
    {
        _ticketService = ticketService;
        _configurationService = configurationService;
        _logger = logger;
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<CustomerTicket>> SubmitTicket([FromBody] SubmitTicketRequest request)
    {
        try
        {
            _logger.LogInformation("Submitting new ticket for customer: {CustomerName}", request.CustomerName);

            var ticket = await _ticketService.SubmitTicketAsync(request);

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
    public ActionResult<ImplementationStatus> GetStatus()
    {
        return Ok(_configurationService.GetStatus());
    }

    [HttpPost("toggle-implementation")]
    public ActionResult<ImplementationStatus> ToggleImplementation([FromBody] ToggleImplementationRequest request)
    {
        try
        {
            _logger.LogInformation("Toggling implementation to: {UseReal}", request.UseReal);

            if (request.UseReal && !_configurationService.HasValidAzureOpenAIConfig)
            {
                return BadRequest(new
                {
                    error = "Cannot enable real implementation: Azure OpenAI configuration is missing",
                    missingConfig = "AOI_ENDPOINT_SWDN and AOI_KEY_SWDN environment variables are required"
                });
            }

            _configurationService.SetImplementation(request.UseReal);
            var status = _configurationService.GetStatus();

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling implementation");
            return StatusCode(500, new { error = "Failed to toggle implementation" });
        }
    }
}
