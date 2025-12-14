# Copyright (c) Microsoft. All rights reserved.

"""Flight status widget rendering for ChatKit integration sample."""

import base64
from dataclasses import dataclass, field
from datetime import datetime

from chatkit.actions import ActionConfig
from chatkit.widgets import Box, Button, Card, Col, Image, Row, Text, Title, WidgetRoot

# Flight widget colors
FLIGHT_ICON_COLOR = "#0369A1"  # Sky blue
FLIGHT_ICON_ACCENT = "#E0F2FE"  # Light sky blue
STATUS_COLORS = {
    "scheduled": {"bg": "#FEF3C7", "text": "#92400E"},  # Amber
    "active": {"bg": "#DBEAFE", "text": "#1E40AF"},  # Blue (in-flight)
    "landed": {"bg": "#D1FAE5", "text": "#065F46"},  # Green
    "cancelled": {"bg": "#FEE2E2", "text": "#991B1B"},  # Red
    "incident": {"bg": "#FEE2E2", "text": "#991B1B"},  # Red
    "diverted": {"bg": "#FED7AA", "text": "#9A3412"},  # Orange
}

# Popular airports for selector
POPULAR_AIRPORTS = [
    {"iata": "SYD", "name": "Sydney", "country": "Australia"},
    {"iata": "MEL", "name": "Melbourne", "country": "Australia"},
    {"iata": "LAX", "name": "Los Angeles", "country": "USA"},
    {"iata": "JFK", "name": "New York JFK", "country": "USA"},
    {"iata": "LHR", "name": "London Heathrow", "country": "UK"},
    {"iata": "DXB", "name": "Dubai", "country": "UAE"},
    {"iata": "SIN", "name": "Singapore", "country": "Singapore"},
    {"iata": "HKG", "name": "Hong Kong", "country": "China"},
]

# Popular routes for quick selection
POPULAR_ROUTES = [
    {"dep": "SYD", "arr": "MEL", "label": "Sydney → Melbourne"},
    {"dep": "SYD", "arr": "LAX", "label": "Sydney → Los Angeles"},
    {"dep": "LHR", "arr": "JFK", "label": "London → New York"},
    {"dep": "DXB", "arr": "LHR", "label": "Dubai → London"},
    {"dep": "SIN", "arr": "SYD", "label": "Singapore → Sydney"},
]


@dataclass
class LiveFlightData:
    """Live tracking data for in-flight aircraft."""

    updated: str | None = None
    latitude: float | None = None
    longitude: float | None = None
    altitude: float | None = None  # meters
    direction: float | None = None  # degrees
    speed_horizontal: float | None = None  # km/h
    speed_vertical: float | None = None  # km/h
    is_ground: bool = False


@dataclass
class AirportInfo:
    """Airport information."""

    airport: str = ""
    iata: str = ""
    icao: str = ""
    terminal: str | None = None
    gate: str | None = None
    baggage: str | None = None
    delay: int | None = None  # minutes
    scheduled: str | None = None
    estimated: str | None = None
    actual: str | None = None
    timezone: str | None = None


@dataclass
class FlightStatusData:
    """Flight status data container matching AviationStack API response."""

    flight_date: str = ""
    flight_status: str = "scheduled"  # scheduled, active, landed, cancelled, incident, diverted
    flight_iata: str = ""
    flight_number: str = ""
    airline_name: str = ""
    airline_iata: str = ""
    departure: AirportInfo = field(default_factory=AirportInfo)
    arrival: AirportInfo = field(default_factory=AirportInfo)
    live: LiveFlightData | None = None


def _airplane_svg() -> str:
    """Generate SVG for airplane icon."""
    color = FLIGHT_ICON_COLOR
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">'
        f'<path d="M32 8L28 20H16L12 28H24L20 44L8 48V52L20 48L24 56H28L32 44L36 56H40L44 48L56 52V48L44 44L40 28H52L48 20H36L32 8Z" '
        f'fill="{FLIGHT_ICON_ACCENT}" stroke="{color}" stroke-width="2" stroke-linejoin="round"/>'
        "</svg>"
    )


def _airplane_takeoff_svg() -> str:
    """Generate SVG for takeoff icon."""
    color = FLIGHT_ICON_COLOR
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">'
        f'<path d="M8 52H56" stroke="{color}" stroke-width="3" stroke-linecap="round"/>'
        f'<path d="M12 40L24 32L32 28L48 20L52 22L44 32L36 36L20 44L12 40Z" '
        f'fill="{FLIGHT_ICON_ACCENT}" stroke="{color}" stroke-width="2" stroke-linejoin="round"/>'
        "</svg>"
    )


def _airplane_landing_svg() -> str:
    """Generate SVG for landing icon."""
    color = FLIGHT_ICON_COLOR
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">'
        f'<path d="M8 52H56" stroke="{color}" stroke-width="3" stroke-linecap="round"/>'
        f'<path d="M48 20L40 28L32 32L16 40L12 38L20 28L28 24L44 16L48 20Z" '
        f'fill="{FLIGHT_ICON_ACCENT}" stroke="{color}" stroke-width="2" stroke-linejoin="round"/>'
        "</svg>"
    )


def _clock_svg() -> str:
    """Generate SVG for clock/schedule icon."""
    color = FLIGHT_ICON_COLOR
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">'
        f'<circle cx="32" cy="32" r="24" fill="{FLIGHT_ICON_ACCENT}" stroke="{color}" stroke-width="3"/>'
        f'<path d="M32 16V32L44 40" stroke="{color}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>'
        "</svg>"
    )


def _location_svg() -> str:
    """Generate SVG for location/airport icon."""
    color = FLIGHT_ICON_COLOR
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">'
        f'<path d="M32 8c-8.837 0-16 7.163-16 16 0 12 16 32 16 32s16-20 16-32c0-8.837-7.163-16-16-16z" '
        f'fill="{FLIGHT_ICON_ACCENT}" stroke="{color}" stroke-width="3" stroke-linejoin="round"/>'
        f'<circle cx="32" cy="24" r="6" fill="{color}"/>'
        "</svg>"
    )


def _encode_svg(svg: str) -> str:
    """Encode SVG as base64 data URI."""
    encoded = base64.b64encode(svg.encode("utf-8")).decode("ascii")
    return f"data:image/svg+xml;base64,{encoded}"


# Pre-encoded icons
AIRPLANE_ICON = _encode_svg(_airplane_svg())
TAKEOFF_ICON = _encode_svg(_airplane_takeoff_svg())
LANDING_ICON = _encode_svg(_airplane_landing_svg())
CLOCK_ICON = _encode_svg(_clock_svg())
LOCATION_ICON = _encode_svg(_location_svg())


def _get_status_icon(status: str, is_ground: bool | None = None) -> str:
    """Get appropriate icon based on flight status."""
    if status == "scheduled":
        return CLOCK_ICON
    elif status == "active":
        if is_ground:
            return TAKEOFF_ICON
        return AIRPLANE_ICON
    elif status == "landed":
        return LANDING_ICON
    return AIRPLANE_ICON


def _format_time(time_str: str | None) -> str:
    """Format ISO time string to readable format."""
    if not time_str:
        return "--:--"
    try:
        dt = datetime.fromisoformat(time_str.replace("Z", "+00:00"))
        return dt.strftime("%H:%M")
    except (ValueError, AttributeError):
        return time_str[:5] if time_str and len(time_str) >= 5 else "--:--"


def _get_delay_color(delay: int | None) -> str:
    """Get color based on delay minutes."""
    if delay is None or delay <= 0:
        return "green-600"
    elif delay <= 15:
        return "yellow-600"
    elif delay <= 30:
        return "orange-600"
    return "red-600"


def _detail_chip(label: str, value: str, icon: str | None = None) -> Box:
    """Create a detail chip widget component."""
    children = [
        Text(value=label, size="xs", weight="medium", color="tertiary"),
        Text(value=value, weight="semibold", size="sm"),
    ]

    return Box(
        padding=3,
        radius="lg",
        background="surface-tertiary",
        minWidth=100,
        children=[
            Col(
                align="start",
                gap=1,
                children=children,
            )
        ],
    )


def render_flight_widget(data: FlightStatusData) -> WidgetRoot:
    """Render a flight status widget with expandable details.

    Args:
        data: FlightStatusData containing flight information

    Returns:
        A ChatKit WidgetRoot (Card) displaying the flight status
    """
    status = data.flight_status.lower()
    status_style = STATUS_COLORS.get(status, STATUS_COLORS["scheduled"])
    is_ground = data.live.is_ground if data.live else None

    # Get appropriate icon
    status_icon = _get_status_icon(status, is_ground)

    # Format status display
    status_display = {
        "scheduled": "Scheduled",
        "active": "In Flight" if not is_ground else "Departing",
        "landed": "Landed",
        "cancelled": "Cancelled",
        "incident": "Incident",
        "diverted": "Diverted",
    }.get(status, status.title())

    # Build header section
    header = Box(
        padding=5,
        background="surface-tertiary",
        children=[
            Row(
                justify="between",
                align="center",
                children=[
                    Row(
                        gap=3,
                        align="center",
                        children=[
                            Box(
                                padding=2,
                                radius="full",
                                background="blue-100",
                                children=[
                                    Image(
                                        src=status_icon,
                                        alt="Flight",
                                        size=32,
                                        fit="contain",
                                    )
                                ],
                            ),
                            Col(
                                align="start",
                                gap=1,
                                children=[
                                    Title(
                                        value=data.flight_iata or f"{data.airline_iata}{data.flight_number}",
                                        size="md",
                                        weight="bold",
                                    ),
                                    Text(
                                        value=data.airline_name,
                                        color="tertiary",
                                        size="xs",
                                    ),
                                ],
                            ),
                        ],
                    ),
                    Box(
                        padding=2,
                        paddingX=4,
                        radius="full",
                        background=status_style["bg"],
                        children=[
                            Text(
                                value=status_display,
                                size="sm",
                                weight="semibold",
                                color=status_style["text"],
                            )
                        ],
                    ),
                ],
            ),
        ],
    )

    # Route display section
    route_section = Box(
        padding=5,
        children=[
            Row(
                justify="between",
                align="center",
                gap=4,
                children=[
                    # Departure
                    Col(
                        align="start",
                        gap=1,
                        children=[
                            Text(value="FROM", size="xs", color="tertiary", weight="medium"),
                            Title(value=data.departure.iata, size="lg", weight="bold"),
                            Text(value=data.departure.airport, size="xs", color="secondary"),
                            Text(
                                value=_format_time(data.departure.scheduled),
                                size="sm",
                                weight="semibold",
                            ),
                        ],
                    ),
                    # Arrow/plane indicator
                    Col(
                        align="center",
                        children=[
                            Text(value="✈️", size="lg"),
                            Text(value="→", size="sm", color="tertiary"),
                        ],
                    ),
                    # Arrival
                    Col(
                        align="end",
                        gap=1,
                        children=[
                            Text(value="TO", size="xs", color="tertiary", weight="medium"),
                            Title(value=data.arrival.iata, size="lg", weight="bold"),
                            Text(value=data.arrival.airport, size="xs", color="secondary"),
                            Text(
                                value=_format_time(data.arrival.scheduled),
                                size="sm",
                                weight="semibold",
                            ),
                        ],
                    ),
                ],
            ),
        ],
    )

    # Build details chips
    detail_chips = []

    # Departure details
    if data.departure.terminal:
        detail_chips.append(_detail_chip("Dep. Terminal", data.departure.terminal))
    if data.departure.gate:
        detail_chips.append(_detail_chip("Dep. Gate", data.departure.gate))
    if data.departure.delay and data.departure.delay > 0:
        detail_chips.append(_detail_chip("Dep. Delay", f"{data.departure.delay} min"))

    # Arrival details
    if data.arrival.terminal:
        detail_chips.append(_detail_chip("Arr. Terminal", data.arrival.terminal))
    if data.arrival.gate:
        detail_chips.append(_detail_chip("Arr. Gate", data.arrival.gate))
    if data.arrival.baggage:
        detail_chips.append(_detail_chip("Baggage", data.arrival.baggage))
    if data.arrival.delay and data.arrival.delay > 0:
        detail_chips.append(_detail_chip("Arr. Delay", f"{data.arrival.delay} min"))

    # Live data if available and in flight
    if data.live and status == "active" and not data.live.is_ground:
        if data.live.altitude:
            detail_chips.append(_detail_chip("Altitude", f"{int(data.live.altitude):,}m"))
        if data.live.speed_horizontal:
            detail_chips.append(_detail_chip("Speed", f"{int(data.live.speed_horizontal)} km/h"))

    # Details section (expandable content)
    details_section = Box(
        padding=5,
        gap=3,
        background="surface-secondary",
        children=[
            Text(value="Flight Details", weight="semibold", size="sm"),
            Row(
                gap=3,
                wrap="wrap",
                children=detail_chips if detail_chips else [
                    Text(value="No additional details available", size="xs", color="tertiary")
                ],
            ),
        ],
    )

    # Progress indicator for flight stages
    stages = ["Scheduled", "Departing", "In Flight", "Landed"]
    current_stage = 0
    if status == "scheduled":
        current_stage = 0
    elif status == "active":
        current_stage = 1 if is_ground else 2
    elif status == "landed":
        current_stage = 3

    stage_indicators = []
    for i, stage in enumerate(stages):
        is_active = i <= current_stage
        stage_indicators.append(
            Col(
                align="center",
                gap=1,
                children=[
                    Box(
                        padding=2,
                        radius="full",
                        background="blue-500" if is_active else "gray-200",
                        children=[
                            Text(value="✓" if is_active else str(i + 1), size="xs", color="white" if is_active else "tertiary")
                        ],
                    ),
                    Text(value=stage, size="xs", color="secondary" if is_active else "tertiary"),
                ],
            )
        )

    progress_section = Box(
        padding=4,
        children=[
            Row(
                justify="between",
                align="start",
                children=stage_indicators,
            ),
        ],
    )

    return Card(
        key="flight_status",
        padding=0,
        children=[header, route_section, progress_section, details_section],
    )


def flight_widget_copy_text(data: FlightStatusData) -> str:
    """Generate plain text representation of flight status.

    Args:
        data: FlightStatusData containing flight information

    Returns:
        Plain text description for copy/paste functionality
    """
    lines = [
        f"Flight: {data.flight_iata or data.flight_number}",
        f"Airline: {data.airline_name}",
        f"Status: {data.flight_status.title()}",
        f"",
        f"From: {data.departure.airport} ({data.departure.iata})",
        f"  Scheduled: {_format_time(data.departure.scheduled)}",
    ]

    if data.departure.terminal:
        lines.append(f"  Terminal: {data.departure.terminal}")
    if data.departure.gate:
        lines.append(f"  Gate: {data.departure.gate}")
    if data.departure.delay:
        lines.append(f"  Delay: {data.departure.delay} min")

    lines.extend([
        f"",
        f"To: {data.arrival.airport} ({data.arrival.iata})",
        f"  Scheduled: {_format_time(data.arrival.scheduled)}",
    ])

    if data.arrival.terminal:
        lines.append(f"  Terminal: {data.arrival.terminal}")
    if data.arrival.gate:
        lines.append(f"  Gate: {data.arrival.gate}")

    return "\n".join(lines)


def render_airport_selector_widget() -> WidgetRoot:
    """Render an interactive airport selector widget.

    Returns:
        A ChatKit WidgetRoot (Card) with airport selection buttons
    """
    # Header section
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
                                src=LOCATION_ICON,
                                alt="Airport",
                                size=28,
                                fit="contain",
                            )
                        ],
                    ),
                    Col(
                        align="start",
                        gap=1,
                        children=[
                            Title(
                                value="Select Airport",
                                size="md",
                                weight="semibold",
                            ),
                            Text(
                                value="Choose a departure airport to search flights",
                                color="tertiary",
                                size="xs",
                            ),
                        ],
                    ),
                ],
            ),
        ],
    )

    # Create airport buttons
    airport_buttons: list[Button] = []
    for airport in POPULAR_AIRPORTS:
        button = Button(
            label=f"{airport['iata']} - {airport['name']}",
            variant="outline",
            size="md",
            onClickAction=ActionConfig(
                type="airport_selected",
                payload={
                    "iata": airport["iata"],
                    "name": airport["name"],
                    "country": airport["country"],
                },
                handler="server",
            ),
        )
        airport_buttons.append(button)

    # Arrange in rows of 2
    button_rows: list[Row] = []
    for i in range(0, len(airport_buttons), 2):
        row_buttons = airport_buttons[i : i + 2]
        button_rows.append(
            Row(
                gap=3,
                wrap="wrap",
                justify="start",
                children=list(row_buttons),
            )
        )

    # Airports section
    airports_section = Box(
        padding=5,
        gap=3,
        children=[
            *button_rows,
            Box(
                padding=3,
                radius="md",
                background="blue-50",
                children=[
                    Text(
                        value="💡 Select an airport or ask about any flight by its number (e.g., QF1, AA100)",
                        size="xs",
                        color="secondary",
                    ),
                ],
            ),
        ],
    )

    return Card(
        key="airport_selector",
        padding=0,
        children=[header, airports_section],
    )


def render_route_selector_widget() -> WidgetRoot:
    """Render an interactive route selector widget.

    Returns:
        A ChatKit WidgetRoot (Card) with popular route buttons
    """
    # Header section
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
                                src=AIRPLANE_ICON,
                                alt="Routes",
                                size=28,
                                fit="contain",
                            )
                        ],
                    ),
                    Col(
                        align="start",
                        gap=1,
                        children=[
                            Title(
                                value="Popular Routes",
                                size="md",
                                weight="semibold",
                            ),
                            Text(
                                value="Quick search for popular flight routes",
                                color="tertiary",
                                size="xs",
                            ),
                        ],
                    ),
                ],
            ),
        ],
    )

    # Create route buttons
    route_buttons: list[Button] = []
    for route in POPULAR_ROUTES:
        button = Button(
            label=route["label"],
            variant="outline",
            size="md",
            onClickAction=ActionConfig(
                type="route_selected",
                payload={
                    "dep_iata": route["dep"],
                    "arr_iata": route["arr"],
                    "label": route["label"],
                },
                handler="server",
            ),
        )
        route_buttons.append(button)

    # Routes section
    routes_section = Box(
        padding=5,
        gap=3,
        children=[
            Row(
                gap=3,
                wrap="wrap",
                justify="start",
                children=list(route_buttons),
            ),
        ],
    )

    return Card(
        key="route_selector",
        padding=0,
        children=[header, routes_section],
    )


def airport_selector_copy_text() -> str:
    """Generate plain text representation of airport selector."""
    airports_list = "\n".join([f"• {a['iata']} - {a['name']}, {a['country']}" for a in POPULAR_AIRPORTS])
    return f"Available airports:\n{airports_list}"


def render_error_widget(title: str, message: str) -> WidgetRoot:
    """Render an error widget.

    Args:
        title: Error title
        message: Error message

    Returns:
        A ChatKit WidgetRoot (Card) displaying the error
    """
    return Card(
        key="error",
        padding=0,
        children=[
            Box(
                padding=5,
                background="red-50",
                children=[
                    Col(
                        gap=2,
                        children=[
                            Row(
                                gap=2,
                                align="center",
                                children=[
                                    Text(value="⚠️", size="lg"),
                                    Title(value=title, size="md", weight="semibold"),
                                ],
                            ),
                            Text(value=message, size="sm", color="secondary"),
                        ],
                    ),
                ],
            ),
        ],
    )
