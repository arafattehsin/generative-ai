---
name: incident-triage
description: Classify supply-chain disruption severity, identify likely root causes, and generate prioritized mitigation actions for operations teams.
metadata:
  author: operations-intelligence
  version: "1.0"
---

# Incident Triage Skill

Use this skill when the operator needs severity classification, risk framing, immediate actions, and escalation guidance for shipment disruptions.

## Triage Workflow

1. Assess urgency using SLA risk, at-risk orders, and business impact.
2. Map active disruption signals to probable causes.
3. Prioritize actions in the first 15 minutes, first 2 hours, and same-day horizon.
4. Prepare a concise executive brief using the supplied templates.

## Severity Rules

- `critical`: severe SLA breach probability with high-volume or high-value customer impact.
- `high`: measurable disruption with likely customer impact if not addressed quickly.
- `medium`: manageable disruption with clear containment path.
- `low`: limited impact and no immediate escalation required.

## Required Outputs

- Severity decision with reasoning.
- Top 3 probable causes.
- Action plan ordered by urgency.
- At-risk order clusters.
- Recommendation on escalation tier.

## Resources

- [references/SLA_POLICY.md](references/SLA_POLICY.md)
- [references/ESCALATION_MATRIX.md](references/ESCALATION_MATRIX.md)
- [references/VENDOR_CONSTRAINTS.md](references/VENDOR_CONSTRAINTS.md)
- [assets/triage-report-template.md](assets/triage-report-template.md)
- [assets/executive-brief-template.md](assets/executive-brief-template.md)
