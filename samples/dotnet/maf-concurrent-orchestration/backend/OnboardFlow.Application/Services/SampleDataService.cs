// Copyright (c) Microsoft. All rights reserved.

namespace OnboardFlow.Application.Services;

/// <summary>
/// Provides sample onboarding request texts for demo purposes.
/// </summary>
public static class SampleDataService
{
    public static readonly List<SampleRequest> Samples =
    [
        new SampleRequest
        {
            Title = "EU SaaS Onboarding with SSO",
            Text = """
                Customer: Contoso GmbH (EU - Germany)
                Contact: Hans Mueller, hans.mueller@contoso.de, +49 30 12345678
                
                Requirements:
                - SSO via Azure AD with SCIM provisioning
                - Data residency must stay within EU (GDPR requirement)
                - Integration with Salesforce CRM for ticket sync
                - Support ticket ingestion from Zendesk
                - SOC 2 Type II compliance required
                - Invoicing via Purchase Order (PO #2025-4832)
                - Estimated 500 seats, scaling to 2000 in 12 months
                - Need dedicated sandbox environment for testing
                - Custom branding on customer portal required
                """
        },
        new SampleRequest
        {
            Title = "US Enterprise with Compliance",
            Text = """
                Customer: Woodgrove Bank (US - New York)
                Contact: Sarah Chen, sarah.chen@woodgrovebank.com, (212) 555-0199
                
                Requirements:
                - SAML-based SSO integration with existing IdP (Okta)
                - FedRAMP Moderate authorization required
                - PCI DSS compliance for payment data handling
                - API integration with internal risk scoring system
                - Data must not leave US boundaries
                - Net-60 payment terms, invoicing via Ariba
                - 2000 seats initial deployment
                - Disaster recovery SLA: 4-hour RPO
                - Audit logging with 7-year retention
                """
        },
        new SampleRequest
        {
            Title = "APAC Startup Quick Onboarding",
            Text = """
                Customer: TechNova Pte Ltd (Singapore)
                Contact: Wei Lin, wei@technova.sg, +65 9123 4567
                
                Requirements:
                - Google Workspace SSO
                - Slack integration for notifications
                - GitHub integration for DevOps workflow
                - Credit card billing (monthly)
                - 25 seats, startup plan
                - No specific compliance requirements yet
                - Fast onboarding preferred (target: 1 week)
                """
        }
    ];
}

public sealed class SampleRequest
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
