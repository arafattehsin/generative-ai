// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OnboardFlow.Application.Interfaces;

namespace OnboardFlow.Application.Executors.Maf;

/// <summary>
/// Factory that builds the MAF concurrent review workflow.
/// 
/// Topology:
///   ReviewStart → fan-out → [SecurityReviewer, ComplianceReviewer, FinanceReviewer]
///                          → fan-in barrier → ReviewAggregation → output
/// </summary>
public static class ConcurrentReviewWorkflow
{
    public static Workflow Build(IChatClient chatClient, ILlmClient llmClient)
    {
        var start = new ReviewStartExecutor();

        ChatClientAgent securityReviewer = new(chatClient,
            name: "SecurityReviewer",
            instructions: """
                You are a Security Reviewer for enterprise SaaS onboarding.
                Evaluate the applicant's security posture and integration risks.

                Assess:
                - SSO/SCIM integration readiness
                - Data encryption requirements (at rest, in transit)
                - Network security and firewall considerations
                - API authentication and authorization patterns
                - Vulnerability management and penetration testing needs
                - Incident response and breach notification requirements
                - Third-party dependency security risks

                Respond with a structured JSON:
                {
                  "riskLevel": "Low | Medium | High | Critical",
                  "findings": [
                    { "title": "string", "detail": "string", "severity": "Low|Medium|High|Critical" }
                  ],
                  "recommendation": "Approve | Approve with Conditions | Reject",
                  "conditions": ["string"],
                  "notes": "string"
                }

                Return ONLY the JSON, no markdown.
                """);

        ChatClientAgent complianceReviewer = new(chatClient,
            name: "ComplianceReviewer",
            instructions: """
                You are a Compliance Reviewer for enterprise SaaS onboarding.
                Evaluate regulatory and contractual compliance requirements.

                Assess:
                - Data residency and sovereignty requirements (GDPR, CCPA, etc.)
                - Industry-specific regulations (HIPAA, SOX, PCI-DSS, etc.)
                - Contractual terms and SLA requirements
                - Data retention and deletion policies
                - Audit trail and reporting requirements
                - Cross-border data transfer considerations
                - Export control and sanctions screening

                Respond with a structured JSON:
                {
                  "riskLevel": "Low | Medium | High | Critical",
                  "findings": [
                    { "title": "string", "detail": "string", "severity": "Low|Medium|High|Critical" }
                  ],
                  "recommendation": "Approve | Approve with Conditions | Reject",
                  "conditions": ["string"],
                  "notes": "string"
                }

                Return ONLY the JSON, no markdown.
                """);

        ChatClientAgent financeReviewer = new(chatClient,
            name: "FinanceReviewer",
            instructions: """
                You are a Finance Reviewer for enterprise SaaS onboarding.
                Evaluate billing, credit, and financial risk factors.

                Assess:
                - Billing preference feasibility (annual, quarterly, monthly)
                - Credit risk and payment history considerations
                - Custom pricing or volume discount requirements
                - Invoice and PO requirements
                - Currency and tax jurisdiction implications
                - Revenue recognition considerations
                - Contract value and strategic importance

                Respond with a structured JSON:
                {
                  "riskLevel": "Low | Medium | High | Critical",
                  "findings": [
                    { "title": "string", "detail": "string", "severity": "Low|Medium|High|Critical" }
                  ],
                  "recommendation": "Approve | Approve with Conditions | Reject",
                  "conditions": ["string"],
                  "notes": "string"
                }

                Return ONLY the JSON, no markdown.
                """);

        var aggregation = new ReviewAggregationExecutor(llmClient);

        return new WorkflowBuilder(start)
            .WithName("OnboardFlow-ConcurrentReview")
            .WithDescription("Fan-out/fan-in concurrent review stage for enterprise onboarding")
            .AddFanOutEdge(start, [securityReviewer, complianceReviewer, financeReviewer])
            .AddFanInBarrierEdge([securityReviewer, complianceReviewer, financeReviewer], aggregation)
            .WithOutputFrom(aggregation)
            .Build();
    }
}
