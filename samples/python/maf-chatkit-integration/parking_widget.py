# Copyright (c) Microsoft. All rights reserved.

"""Parking sign analysis widget rendering for ChatKit integration sample."""

import base64
from dataclasses import dataclass, field

from chatkit.widgets import Box, Card, Col, Image, Row, Text, Title, WidgetRoot

# Parking widget colors
CAN_PARK_COLOR = "#059669"  # Green-600
CANNOT_PARK_COLOR = "#DC2626"  # Red-600
PARKING_BG_COLOR = "#F0FDF4"  # Green-50
NO_PARKING_BG_COLOR = "#FEF2F2"  # Red-50


@dataclass
class ParkingRestriction:
    """Individual parking restriction from sign analysis."""

    type: str = ""  # e.g., "No Parking", "Time Limited", "Permit Required"
    hours: str | None = None  # e.g., "8am-6pm"
    days: str | None = None  # e.g., "Mon-Fri"
    duration: str | None = None  # e.g., "2 hours"
    notes: str | None = None


@dataclass
class ParkingAnalysisData:
    """Parking sign analysis data container."""

    can_park: bool = True
    verdict: str = ""  # Short verdict e.g., "Yes, you can park here" or "No, parking is prohibited"
    confidence: str = "high"  # high, medium, low
    restrictions: list[ParkingRestriction] = field(default_factory=list)
    time_limit: str | None = None  # e.g., "2 hours max"
    detailed_analysis: str = ""  # Detailed explanation from GPT-4o
    advice: str = ""  # Actionable advice
    current_time_context: str | None = None  # Context about current time relevance
    sign_description: str = ""  # Description of what signs were detected


def _checkmark_svg() -> str:
    """Generate SVG for checkmark (can park)."""
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">'
        f'<circle cx="32" cy="32" r="28" fill="{PARKING_BG_COLOR}" stroke="{CAN_PARK_COLOR}" stroke-width="3"/>'
        f'<path d="M20 32L28 40L44 24" stroke="{CAN_PARK_COLOR}" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>'
        "</svg>"
    )


def _cross_svg() -> str:
    """Generate SVG for cross (cannot park)."""
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">'
        f'<circle cx="32" cy="32" r="28" fill="{NO_PARKING_BG_COLOR}" stroke="{CANNOT_PARK_COLOR}" stroke-width="3"/>'
        f'<path d="M22 22L42 42M42 22L22 42" stroke="{CANNOT_PARK_COLOR}" stroke-width="4" stroke-linecap="round"/>'
        "</svg>"
    )


def _parking_sign_svg() -> str:
    """Generate SVG for parking sign icon."""
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">'
        '<rect x="12" y="8" width="40" height="48" rx="4" fill="#3B82F6" stroke="#1E40AF" stroke-width="2"/>'
        '<text x="32" y="42" font-family="Arial, sans-serif" font-size="28" font-weight="bold" fill="white" '
        'text-anchor="middle">P</text>'
        "</svg>"
    )


def _encode_svg(svg: str) -> str:
    """Encode SVG as base64 data URI."""
    encoded = base64.b64encode(svg.encode("utf-8")).decode("ascii")
    return f"data:image/svg+xml;base64,{encoded}"


# Pre-encoded icons
CHECKMARK_ICON = _encode_svg(_checkmark_svg())
CROSS_ICON = _encode_svg(_cross_svg())
PARKING_SIGN_ICON = _encode_svg(_parking_sign_svg())


def _confidence_badge(confidence: str) -> Box:
    """Create confidence level badge."""
    colors = {
        "high": {"bg": "green-100", "text": "green-700"},
        "medium": {"bg": "yellow-100", "text": "yellow-700"},
        "low": {"bg": "red-100", "text": "red-700"},
    }
    style = colors.get(confidence.lower(), colors["medium"])

    return Box(
        padding=2,
        paddingX=3,
        radius="full",
        background=style["bg"],
        children=[
            Text(
                value=f"{confidence.title()} confidence",
                size="xs",
                weight="medium",
                color=style["text"],
            )
        ],
    )


def _restriction_chip(restriction: ParkingRestriction) -> Box:
    """Create a restriction detail chip."""
    details = []
    if restriction.hours:
        details.append(f"⏰ {restriction.hours}")
    if restriction.days:
        details.append(f"📅 {restriction.days}")
    if restriction.duration:
        details.append(f"⏱️ {restriction.duration}")
    if restriction.notes:
        details.append(f"📝 {restriction.notes}")

    return Box(
        padding=3,
        radius="lg",
        background="surface-tertiary",
        children=[
            Col(
                gap=2,
                children=[
                    Text(value=restriction.type, weight="semibold", size="sm"),
                    *[Text(value=detail, size="xs", color="secondary") for detail in details],
                ],
            )
        ],
    )


def render_parking_widget(data: ParkingAnalysisData) -> WidgetRoot:
    """Render a parking analysis widget with expandable details.

    Args:
        data: ParkingAnalysisData containing parking sign analysis

    Returns:
        A ChatKit WidgetRoot (Card) displaying the parking analysis
    """
    # Determine verdict styling
    if data.can_park:
        verdict_bg = PARKING_BG_COLOR
        verdict_icon = CHECKMARK_ICON
        verdict_emoji = "✅"
        header_color = "green-50"
    else:
        verdict_bg = NO_PARKING_BG_COLOR
        verdict_icon = CROSS_ICON
        verdict_emoji = "❌"
        header_color = "red-50"

    # Header with large verdict
    header = Box(
        padding=5,
        background=header_color,
        children=[
            Row(
                justify="between",
                align="center",
                children=[
                    Row(
                        gap=4,
                        align="center",
                        children=[
                            Image(
                                src=verdict_icon,
                                alt="Verdict",
                                size=48,
                                fit="contain",
                            ),
                            Col(
                                gap=1,
                                children=[
                                    Title(
                                        value=f"{verdict_emoji} {'Yes!' if data.can_park else 'No!'}",
                                        size="lg",
                                        weight="bold",
                                    ),
                                    Text(
                                        value=data.verdict,
                                        size="sm",
                                        color="secondary",
                                        weight="medium",
                                    ),
                                ],
                            ),
                        ],
                    ),
                    _confidence_badge(data.confidence),
                ],
            ),
        ],
    )

    # Quick info section
    quick_info_items = []

    if data.time_limit:
        quick_info_items.append(
            Box(
                padding=3,
                radius="md",
                background="surface-tertiary",
                children=[
                    Col(
                        gap=1,
                        children=[
                            Text(value="⏱️ Time Limit", size="xs", color="tertiary", weight="medium"),
                            Text(value=data.time_limit, weight="semibold", size="sm"),
                        ],
                    )
                ],
            )
        )

    if data.current_time_context:
        quick_info_items.append(
            Box(
                padding=3,
                radius="md",
                background="blue-50",
                children=[
                    Col(
                        gap=1,
                        children=[
                            Text(value="🕐 Current Time", size="xs", color="tertiary", weight="medium"),
                            Text(value=data.current_time_context, size="xs", color="secondary"),
                        ],
                    )
                ],
            )
        )

    quick_info_section = (
        Box(
            padding=5,
            gap=3,
            children=[
                Row(gap=3, wrap="wrap", children=quick_info_items),
            ],
        )
        if quick_info_items
        else None
    )

    # Restrictions section
    restrictions_section = None
    if data.restrictions:
        restriction_chips = [_restriction_chip(r) for r in data.restrictions]
        restrictions_section = Box(
            padding=5,
            gap=3,
            background="surface-secondary",
            children=[
                Text(value="📋 Restrictions", weight="semibold", size="sm"),
                Row(gap=3, wrap="wrap", children=restriction_chips),
            ],
        )

    # Detailed analysis section (expandable)
    analysis_section = Box(
        padding=5,
        gap=3,
        background="surface-tertiary",
        children=[
            Text(value="🔍 Detailed Analysis", weight="semibold", size="sm"),
            Text(value=data.detailed_analysis, size="sm", color="secondary"),
        ],
    )

    # Advice section
    advice_section = None
    if data.advice:
        advice_section = Box(
            padding=4,
            radius="lg",
            background="blue-50",
            children=[
                Row(
                    gap=2,
                    children=[
                        Text(value="💡", size="md"),
                        Text(value=data.advice, size="sm", color="secondary"),
                    ],
                )
            ],
        )

    # Build card children
    children = [header]
    if quick_info_section:
        children.append(quick_info_section)
    if restrictions_section:
        children.append(restrictions_section)
    children.append(analysis_section)
    if advice_section:
        children.append(Box(padding=5, children=[advice_section]))

    return Card(
        key="parking_analysis",
        padding=0,
        children=children,
    )


def parking_widget_copy_text(data: ParkingAnalysisData) -> str:
    """Generate plain text representation of parking analysis.

    Args:
        data: ParkingAnalysisData containing parking sign analysis

    Returns:
        Plain text description for copy/paste functionality
    """
    lines = [
        f"Parking Analysis Result",
        f"=======================",
        f"Verdict: {'✅ YES - ' if data.can_park else '❌ NO - '}{data.verdict}",
        f"Confidence: {data.confidence.title()}",
    ]

    if data.time_limit:
        lines.append(f"Time Limit: {data.time_limit}")

    if data.restrictions:
        lines.append(f"\nRestrictions:")
        for r in data.restrictions:
            lines.append(f"  • {r.type}")
            if r.hours:
                lines.append(f"    Hours: {r.hours}")
            if r.days:
                lines.append(f"    Days: {r.days}")
            if r.duration:
                lines.append(f"    Duration: {r.duration}")

    lines.extend([
        f"\nDetailed Analysis:",
        data.detailed_analysis,
    ])

    if data.advice:
        lines.extend([f"\nAdvice:", data.advice])

    return "\n".join(lines)


def render_parking_upload_prompt() -> WidgetRoot:
    """Render a widget prompting user to upload a parking sign image.

    Returns:
        A ChatKit WidgetRoot (Card) with upload instructions
    """
    header = Box(
        padding=5,
        background="surface-tertiary",
        children=[
            Row(
                gap=3,
                align="center",
                children=[
                    Box(
                        padding=3,
                        radius="full",
                        background="blue-100",
                        children=[
                            Image(
                                src=PARKING_SIGN_ICON,
                                alt="Parking",
                                size=32,
                                fit="contain",
                            )
                        ],
                    ),
                    Col(
                        gap=1,
                        children=[
                            Title(
                                value="Parking Sign Analysis",
                                size="md",
                                weight="semibold",
                            ),
                            Text(
                                value="Upload a photo of a parking sign for AI analysis",
                                color="tertiary",
                                size="xs",
                            ),
                        ],
                    ),
                ],
            ),
        ],
    )

    instructions = Box(
        padding=5,
        gap=4,
        children=[
            Col(
                gap=3,
                children=[
                    Row(
                        gap=2,
                        align="center",
                        children=[
                            Text(value="📸", size="md"),
                            Text(value="Take a clear photo of the parking sign", size="sm"),
                        ],
                    ),
                    Row(
                        gap=2,
                        align="center",
                        children=[
                            Text(value="📤", size="md"),
                            Text(value="Upload using the attachment button below", size="sm"),
                        ],
                    ),
                    Row(
                        gap=2,
                        align="center",
                        children=[
                            Text(value="🤖", size="md"),
                            Text(value="AI will analyse and tell you if you can park", size="sm"),
                        ],
                    ),
                ],
            ),
            Box(
                padding=3,
                radius="md",
                background="amber-50",
                children=[
                    Text(
                        value="⚠️ This is for informational purposes only. Always verify with actual signage.",
                        size="xs",
                        color="secondary",
                    ),
                ],
            ),
        ],
    )

    return Card(
        key="parking_upload_prompt",
        padding=0,
        children=[header, instructions],
    )


def render_analysing_widget() -> WidgetRoot:
    """Render a widget showing analysis in progress.

    Returns:
        A ChatKit WidgetRoot (Card) with analysing animation
    """
    return Card(
        key="analysing",
        padding=0,
        children=[
            Box(
                padding=5,
                background="surface-tertiary",
                children=[
                    Row(
                        gap=3,
                        align="center",
                        justify="center",
                        children=[
                            Text(value="🔍", size="lg"),
                            Col(
                                gap=1,
                                children=[
                                    Text(value="Analysing parking sign...", weight="semibold"),
                                    Text(value="Using AI vision to read the sign", size="xs", color="tertiary"),
                                ],
                            ),
                        ],
                    ),
                ],
            ),
        ],
    )
