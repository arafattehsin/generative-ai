// Copyright (c) Microsoft. All rights reserved.

using PolicyPackBuilder.Application.DTOs;

namespace PolicyPackBuilder.Application.Services;

/// <summary>
/// Service providing sample policy drafts for demo purposes.
/// </summary>
public static class SampleDataService
{
    /// <summary>
    /// Gets all available sample inputs.
    /// </summary>
    public static List<SampleResponse> GetSamples()
    {
        return
        [
            new SampleResponse
            {
                Id = "hr-policy",
                Name = "HR Remote Work Policy",
                Description = "A draft HR policy about remote work arrangements with informal language and missing disclaimers.",
                InputText = """
                    DRAFT - Remote Work Policy v0.1
                    
                    Hey team! We're super excited to roll out our new remote work policy. This is gonna be great for everyone!
                    
                    Who can work remote?
                    Pretty much anyone can work from home whenever they want. Just let your manager know. We trust you guys to get your work done.
                    
                    Equipment
                    We'll give you a laptop and maybe a monitor if you ask nicely. Keep them safe - they're expensive! If something breaks, just email IT at helpdesk@company.com or call 555-123-4567.
                    
                    Work hours
                    You should be online during core hours (10am-3pm) but otherwise its flexible. We don't really track hours as long as the work gets done.
                    
                    Security stuff
                    Use the VPN when working with sensitive data. Don't connect to sketchy wifi. This is 100% safe as long as you follow these simple rules.
                    
                    Benefits
                    Remote work is guaranteed for all employees. There's no risk of losing this benefit once you start. You can work from literally anywhere - coffee shops, beaches, wherever!
                    
                    Questions?
                    Talk to HR or your manager. We're always here to help!
                    
                    - The People Team
                    """
            },
            new SampleResponse
            {
                Id = "product-warranty",
                Name = "Product Warranty Terms",
                Description = "A warranty document with unclear language and potential compliance issues.",
                InputText = """
                    PRODUCT WARRANTY - SMARTWIDGET PRO 3000
                    
                    Congratulations on your purchase! Your SmartWidget Pro 3000 is guaranteed to work perfectly forever. We promise you'll never have issues with this product.
                    
                    COVERAGE
                    This warranty covers absolutely everything that could ever go wrong with your device. There's no risk - we've got you covered unconditionally.
                    
                    DURATION  
                    The warranty is valid for 2 years from purchase date. Keep your receipt (email it to warranty@widgets.com if you lose it).
                    
                    WHAT WE'LL DO
                    If your SmartWidget breaks, we'll fix it or replace it. It always works out in your favor. Contact our support team at 1-800-555-0199 or support@widgets.com.
                    
                    EXCLUSIONS
                    Um... we haven't really figured this part out yet. Probably don't throw it in water or hit it with a hammer? Use common sense!
                    
                    CLAIMS PROCESS
                    1. Contact us
                    2. Describe the problem
                    3. Ship the product (we'll email a label)
                    4. Get your fixed/new widget back!
                    
                    The whole process is risk-free and takes about 5-7 business days.
                    
                    Thanks for choosing SmartWidget!
                    
                    SmartWidget Inc.
                    123 Tech Boulevard
                    Innovation City, TC 12345
                    """
            }
        ];
    }

    /// <summary>
    /// Gets a sample by ID.
    /// </summary>
    public static SampleResponse? GetSampleById(string id)
    {
        return GetSamples().FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
