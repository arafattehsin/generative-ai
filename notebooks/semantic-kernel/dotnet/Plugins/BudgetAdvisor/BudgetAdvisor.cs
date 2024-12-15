using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

public class BudgetAdvisor
{
    [KernelFunction, Description("Evaluate the total cost of the itinerary")]
    [return: Description("over or under budget")]
    public async Task<string> EvaluateTotalCostAsync(
        [Description("Total user's budget")] int userBudget,
        [Description("Estimated budget")] int estimatedBudget
    )
    {
        // If an estimated budget is over the user's budget, return that it is over budget
        if (estimatedBudget > userBudget)
        {
            return "over budget";
        }
        else
        {
            return "under budget";
        }
    }
}