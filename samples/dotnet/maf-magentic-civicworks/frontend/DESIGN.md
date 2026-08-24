---
name: CivicWorks
description: A civic routebook for evidence-bound council investigation and officer review.
colors:
  civic-blue: "#123d56"
  civic-blue-deep: "#0b2a3c"
  works-yellow: "#f4c542"
  works-yellow-light: "#fff2b6"
  eucalyptus: "#2f755f"
  routebook-paper: "#f5f2e9"
  routebook-paper-deep: "#e9e5d9"
  enamel-white: "#fffefa"
  charcoal: "#17242a"
  muted-ink: "#526169"
  boundary: "#c9c7bb"
  boundary-dark: "#859198"
  conflict: "#b63e32"
  conflict-light: "#f7ddd7"
  focus-blue: "#145ed6"
typography:
  display:
    fontFamily: "Barlow Condensed, sans-serif"
    fontSize: "clamp(2.4rem, 3.5vw, 4.15rem)"
    fontWeight: 800
    lineHeight: 0.93
    letterSpacing: "-0.02em"
  title:
    fontFamily: "Barlow Condensed, sans-serif"
    fontSize: "1.45rem"
    fontWeight: 800
    lineHeight: 1.05
  body:
    fontFamily: "Atkinson Hyperlegible Next Variable, Arial, sans-serif"
    fontSize: "0.87rem"
    fontWeight: 400
    lineHeight: 1.55
  label:
    fontFamily: "Atkinson Hyperlegible Next Variable, Arial, sans-serif"
    fontSize: "0.66rem"
    fontWeight: 800
    lineHeight: 1.2
    letterSpacing: "0.08em"
rounded:
  sign: "2px"
  panel: "3px"
  round: "50%"
components:
  primary-action:
    backgroundColor: "{colors.works-yellow}"
    textColor: "{colors.charcoal}"
    rounded: "{rounded.sign}"
    padding: "0 14px"
    height: "46px"
  secondary-action:
    backgroundColor: "transparent"
    textColor: "{colors.enamel-white}"
    rounded: "{rounded.sign}"
    height: "46px"
  enamel-panel:
    backgroundColor: "{colors.enamel-white}"
    textColor: "{colors.charcoal}"
    rounded: "{rounded.panel}"
  route-station:
    backgroundColor: "{colors.enamel-white}"
    textColor: "{colors.charcoal}"
    rounded: "{rounded.sign}"
    padding: "8px"
    width: "128px"
---

# Design System: CivicWorks

## Overview

**Creative North Star: "Civic Routebook"**

Civic Routebook turns an evidence-bound council investigation into a piece of physical wayfinding. Deep civic blues and off-white enamel surfaces carry authority; works yellow traces change, eucalyptus marks approval, and conflict red interrupts the route when claims disagree. The result is operational and information-dense without reading as a generic AI-monitoring dashboard.

The system borrows the grammar of NSW public-works notices and transit maps: condensed destination type, bounded sign panels, route tape, station markers, striped works markings, and explicit junctions. It favors visible causality over decorative data display. Human checkpoints remain structurally prominent, while evidence lineage and bounded execution use compact, highly legible supporting type.

**Key Characteristics:**

- Investigation state is expressed as a continuous route with visible branches, stops, and checkpoints.
- Enamel-white panels and routebook-paper fields are bounded by fine civic-grey rules.
- Condensed wayfinding headlines pair with hyperlegible operational copy.
- Rectilinear signs, circular stations, route tape, and works hatching provide the signature geometry.
- Human approval and evidence conflicts are stated in words and symbols as well as color.

## Colors

The palette combines civic authority with worksite visibility and warm, paper-like neutrality.

### Primary

- **Deep Civic Blue:** The stable structural color for the sign bar, plan infrastructure, route outlines, and institutional labels.
- **Checkpoint Blue:** The deeper companion used for the docked officer checkpoint and high-contrast operational zones.
- **Focus Blue:** A dedicated, unmistakable keyboard-focus outline rather than a decorative accent.

### Secondary

- **Works Yellow:** The active route, officer checkpoint symbol, execution progress, and primary action. It identifies what has changed or what requires action.
- **Pale Works Yellow:** Supporting text on dark civic panels where the works accent needs a quieter expression.

### Tertiary

- **Eucalyptus Green:** Verified evidence, approved routes, and positive recommendation language.
- **Conflict Red:** Material evidence conflicts and challenged assumptions.
- **Conflict Wash:** The background paired with conflict red so alerts remain readable without becoming visually punitive.

### Neutral

- **Routebook Paper:** The warm application field that keeps the workspace civic and physical rather than software-grey.
- **Routebook Paper Deep:** The recessed case-context rail.
- **Enamel White:** Interactive route panels, evidence records, and option sheets.
- **Charcoal:** Primary reading text.
- **Muted Ink:** Explanations, metadata, secondary labels, and inactive route information.
- **Boundary Grey / Boundary Grey Dark:** Dividers, panel edges, superseded paths, and dashed infrastructure.

### Named Rules

**The Route Carries State Rule.** Civic blue is stable plan infrastructure; works yellow is the current or revised path and primary action; eucalyptus is reserved for verified or approved states; conflict red is paired with an alert icon, copy, or border so status never depends on color alone.

**The Paper and Enamel Rule.** Large work areas stay routebook paper; interactive records and plan surfaces are enamel white with visible civic-grey boundaries.

## Typography

**Display Font:** Barlow Condensed (with sans-serif fallback)
**Body Font:** Atkinson Hyperlegible Next Variable (with Arial and sans-serif fallbacks)

**Character:** Barlow Condensed supplies the compressed authority of civic wayfinding and public-works labels. Atkinson Hyperlegible Next keeps dense evidence, constraints, budgets, and controls readable at compact sizes.

### Hierarchy

- **Display** (800, `clamp(2.4rem, 3.5vw, 4.15rem)`, 0.93): Route-change thesis and first-viewport decision framing; balances to about 16 characters per line.
- **Headline** (800, 1.85rem, 1): Officer-checkpoint decisions and approval state.
- **Title** (800, 1.45rem, 1.05): Evidence records, brief titles, and section-level destinations.
- **Body** (400, 0.87rem, 1.55): Evidence explanation and operational context, generally limited to 68 characters per line.
- **Label** (700–800, 0.64–0.78rem, 0.06–0.10em tracking): References, statuses, budgets, and sign legends; uppercase is reserved for wayfinding and administrative metadata.

### Named Rules

**The Wayfinding Pair Rule.** Use Barlow Condensed for destinations, station names, case numerals, and checkpoint headings; use Atkinson Hyperlegible Next for explanations, evidence, controls, and metadata.

**The Compressed, Not Cramped Rule.** Condensed type may be tight and loud, but operational body copy keeps generous line height and a bounded measure.

## Layout

The desktop workspace is a civic control strip rather than a card dashboard: a 282px case rail, a fluid route field with a 620px minimum, and a 342px officer checkpoint dock. The 76px sign bar spans all three zones, with a 6px works-yellow rule separating identity from the investigation.

Spacing is compact and clustered rather than governed by a formal token scale. Repeated control insets sit near 8–14px, panel insets near 18–24px, and workspace edges near 26–32px. Fine 1px boundaries divide dense information; larger gaps are reserved for changes in task level.

At 1260px, the rails tighten to 240px and 310px. At 1040px, the officer checkpoint docks below the case and route columns. At 760px, the whole workspace stacks, nonessential case detail collapses, the horizontal SVG route becomes a vertical numbered route, evidence columns collapse, and option columns become a single reading sequence.

**The Route Owns the Centre Rule.** The plan route and its conflict junction receive the largest fluid region; supporting case facts and officer authority remain docked at its edges until the viewport requires stacking.

## Elevation & Depth

Depth is a restrained hybrid of tonal layering, borders, and low ambient shadows. Routebook paper, deeper paper, enamel white, and deep-blue docks establish most hierarchy; shadows lift only the active route surface, evidence sheet, options brief, and station signs.

### Shadow Vocabulary

- **Route Field Lift** (`0 18px 34px rgba(42, 51, 55, 0.1)`): The main plan surface above routebook paper.
- **Record Sheet Lift** (`0 14px 28px rgba(42, 51, 55, 0.08)`): Evidence and options sheets.
- **Station Sign Lift** (`4px 6px 14px rgba(23, 36, 42, 0.13)`): Route nodes; hover increases to `5px 8px 18px rgba(23, 36, 42, 0.18)`.

### Named Rules

**The Bounded Before Lifted Rule.** Establish hierarchy with tone and a visible border first; use shadow only when a surface must read as a sign or sheet above the routebook.

## Shapes

The form language is rectilinear and infrastructural. Controls and route nodes use 2px sign corners; major enamel panels use 3px corners. Circles are reserved for stations, checkpoint markers, owner seals, evidence pips, and route-brand geometry. Thick route lines, dashed superseded branches, and diagonal works hatching create the physical network vocabulary.

**The Signs and Stations Rule.** Use nearly square rectangles for information and action; use circles only for people, stops, checkpoints, and compact state markers.

## Components

### Buttons

- **Shape:** Compact public-sign controls with 2px corners and a 46px minimum action height.
- **Primary:** Works-yellow fill and border with dark text, 14px horizontal padding, strong label weight, and an arrow that separates label from direction.
- **Hover / Focus:** Hover brightens the yellow; keyboard focus always uses a 3px focus-blue outline offset by 3px.
- **Secondary:** Transparent on the deep-blue checkpoint with a translucent white border; hover adds a restrained white wash.

### Chips

- **Style:** Evidence statuses are small uppercase labels with a 1px current-color border and no pill rounding.
- **State:** Verified uses eucalyptus, conflict uses conflict red, and queued uses civic blue. Nearby words and pips repeat the state.

### Cards / Containers

- **Corner Style:** Enamel-sign corners (3px), never soft dashboard rounding.
- **Background:** Enamel white over routebook paper; checkpoint containers invert to deep civic blue.
- **Shadow Strategy:** Main plan and record sheets use the restrained shadow vocabulary; rails and checkpoint docks rely on tonal separation.
- **Border:** 1px boundary grey, darkened for the route surface and evidence sheets.
- **Internal Padding:** Dense records use 18–24px; large route space is reserved inside the map rather than created through oversized panel padding.

### Navigation

- **Style:** The plan display is a two-part rectangular switch inside a 1px boundary. The active view is civic blue with enamel-white text; the inactive view stays transparent. On mobile both choices share the full available width.

### Route Network

The signature route component combines a thick civic-blue base, an 8px works-yellow revised path, circular stations, compact enamel signs, and a visibly dashed superseded branch. The revised route draws once over 1.1 seconds with a decisive ease-out curve (`cubic-bezier(0.16, 1, 0.3, 1)`); reduced-motion preference removes the animation.

Selected stations receive a 3px works-yellow outline offset by 2px. Conflict stations combine red edge and icon treatments with a pale conflict surface. Queued stations fill the station dot yellow; checkpoints change the station ring to eucalyptus.

### Officer Checkpoint

The checkpoint is a deep-blue dock, not a modal. A circular HOLD or approval marker anchors the decision, the human owner is explicit, constraints use native checkboxes with works-yellow accents, and the primary approval action remains within the same visual authority zone.

## Do's and Don'ts

### Do:

- **Do** make changed plans visible as route geometry, with the causal evidence fixed at the junction.
- **Do** use works yellow for the active route, checkpoint emphasis, progress, and primary officer action.
- **Do** pair every conflict, queued, verified, or approved color with words, geometry, or an icon.
- **Do** preserve condensed wayfinding headings alongside hyperlegible operational copy.
- **Do** convert the route to a vertical stop sequence at narrow widths and honor reduced-motion preference.

### Don't:

- **Don't** flatten the investigation into generic KPI cards, agent chat, or an AI-monitoring dashboard.
- **Don't** round sign panels or controls beyond the established 3px language; reserve circles for stations, people, checkpoints, and state pips.
- **Don't** use works yellow as body text or as a broad background field; its scarcity makes route revision and action legible.
- **Don't** hide human ownership, evidence lineage, or the difference between a current and superseded plan.
- **Don't** add ambient decorative motion; movement belongs to meaningful route revision and must have a reduced-motion fallback.
