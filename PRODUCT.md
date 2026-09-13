# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

The primary user is a fictional NSW council officer reviewing a neighbourhood access and drainage investigation. The interface is optimized for a polished five-to-seven-minute demonstration while preserving the language, constraints, and decision boundaries of a credible officer workflow.

## Product Purpose

CivicWorks helps an officer investigate a precinct-level access and drainage problem through a bounded multi-agent workflow. It makes the manager's proposed plan, evidence collection, conflicts, replanning, limits, and human checkpoints visible, then produces a structured preliminary options brief for officer review.

Success means the demonstration clearly shows that the initial plan is a runtime hypothesis, conflicting sandstone-drain evidence triggers a meaningful revision, material claims retain evidence references, and the workflow ends with either a reviewable options brief or an honest bounded-incomplete result.

## Positioning

CivicWorks demonstrates observable, evidence-bound agent orchestration rather than an autonomous council decision. Its defining mechanism is a Magentic manager that can revise an investigation route when evidence conflicts, while keeping council-officer approval, evidence lineage, and execution limits explicit.

## Operating Context

The principal case is `CW-2047 · Marrin Precinct`, covering recurring ponding, cracked and uneven footpaths, an incomplete accessible route between civic destinations, street-tree and business-disruption constraints, and a conflict between the asset register and a site observation suggesting a possible sandstone drain.

The walkthrough moves through case opening, initial plan review, specialist evidence gathering, conflict discovery, visible replanning, revised-route review, verification, and a three-option preliminary brief. The expected recommendation is `Option C: Investigate first`.

Specialist roles are Community & Access Analyst, Civil Assets Analyst, Place & Constraints Advisor, Cost & Delivery Analyst, Evidence Verifier, and Magentic Manager.

## Capabilities and Constraints

- Show the precinct, current plan, plan revision, evidence state, remaining execution budget, human owner, agent operational status, plan history, and checkpoints.
- Translate live orchestration events into safe UI-specific status updates without exposing chain-of-thought.
- Use only fictional, synthetic council evidence exposed through read-only tools.
- Produce a structured `PreliminaryWorksOptionsBrief` or an explicit bounded-incomplete result.
- Make round, stall, reset, elapsed-time, and tool-use limits observable.
- Require officer review for the initial and revised plans and for the final recommendation.
- Never approve expenditure or works, contact residents or businesses, create work orders, or present fictional estimates as real council estimates.
- Use React, TypeScript, and Vite for the officer interface.

## Evidence on Hand

The scenario, workflow, roles, constraints, acceptance criteria, and proposed technical architecture are documented in `C:\Users\AT\source\repos\arafattehsin-website\docs\agent-orchestration-part-6-civicworks-plan.md`.

No real resident information, council records, testimonials, deployed-customer claims, production metrics, or approved CivicWorks brand assets are available and none should be fabricated.

## Product Principles

1. Make the replan legible: the evidence conflict, changed route, and consequences should be unmistakable.
2. Keep humans visibly accountable: every consequential output remains decision support owned by a council officer.
3. Show evidence, not private reasoning: operational state and source lineage replace transcript theatre.
4. Stop honestly: bounded-incomplete is a valid outcome when verification or budget limits prevent a recommendation.
5. Optimize for narrative clarity without weakening credible council workflow constraints.

## Accessibility & Inclusion

The principal desktop walkthrough must be keyboard-accessible, readable, and operable without relying on color alone. The interface should treat accessible-route impacts as first-class evidence rather than a secondary annotation.
