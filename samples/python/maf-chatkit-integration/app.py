# Copyright (c) Microsoft. All rights reserved.

"""SwiftRover - Your AI Travel Assistant.

This demo showcases integration of Microsoft Agent Framework with OpenAI ChatKit,
featuring real-time flight tracking via AviationStack API, GPT-4o vision-based
parking sign analysis, and o3 reasoning model for expense analysis.
"""

import asyncio
import base64
import json
import logging
import os
from collections.abc import AsyncIterator, Callable
from datetime import datetime
from typing import Annotated, Any

import httpx
import uvicorn

# Agent Framework imports
from agent_framework import AgentRunResponseUpdate, ChatAgent, ChatMessage, FunctionCallContent, FunctionResultContent, Role, TextContent
from agent_framework.azure import AzureOpenAIChatClient

# Agent Framework ChatKit integration
from agent_framework_chatkit import ThreadItemConverter, stream_agent_response

# ChatKit imports
from chatkit.actions import Action
from chatkit.server import ChatKitServer
from chatkit.store import StoreItemType, default_generate_id
from chatkit.types import (
    AssistantMessageContent,
    AssistantMessageItem,
    CustomSummary,
    DurationSummary,
    ProgressUpdateEvent,
    TaskItem,
    ThreadItem,
    ThreadItemAddedEvent,
    ThreadItemDoneEvent,
    ThreadItemUpdatedEvent,
    ThreadStreamEvent,
    ThreadMetadata,
    ThoughtTask,
    UserMessageItem,
    WidgetItem,
    Workflow,
    WorkflowItem,
    WorkflowTaskAdded,
    WorkflowTaskUpdated,
)
from chatkit.widgets import WidgetRoot
from openai import OpenAI
from fastapi import FastAPI, Request, UploadFile, File
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse, JSONResponse, Response, StreamingResponse
from pydantic import Field

from attachment_store import FileBasedAttachmentStore
from flight_widget import (
    AirportInfo,
    FlightStatusData,
    LiveFlightData,
    airport_selector_copy_text,
    flight_widget_copy_text,
    render_airport_selector_widget,
    render_error_widget,
    render_flight_widget,
    render_route_selector_widget,
)
from parking_widget import (
    ParkingAnalysisData,
    ParkingRestriction,
    parking_widget_copy_text,
    render_analysing_widget,
    render_parking_upload_prompt,
    render_parking_widget,
)
from store import SQLiteStore

# ============================================================================
# Logging Setup
# ============================================================================

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)
logger = logging.getLogger(__name__)

# ============================================================================
# Configuration
# ============================================================================

SERVER_HOST = "127.0.0.1"
SERVER_PORT = 8001
DB_PATH = "data/chatkit_demo.db"
UPLOADS_DIR = "data/uploads"


# =============================================================================
# Response wrapper classes for widget detection
# =============================================================================


class FlightResponse(str):
    """String subclass carrying flight data for widget rendering."""

    def __new__(cls, text: str, data: FlightStatusData):
        instance = super().__new__(cls, text)
        instance.data = data  # type: ignore
        return instance


class ShowAirportSelector(str):
    """Marker string for airport selector widget."""
    pass


class ShowRouteSelector(str):
    """Marker string for route selector widget."""
    pass


class ShowParkingPrompt(str):
    """Marker string for parking upload prompt."""
    pass


class ExpenseAnalysisResponse(str):
    """String subclass carrying expense analysis data for reasoning display."""

    def __new__(cls, text: str, reasoning_time: float, reasoning_content: str | None = None):
        instance = super().__new__(cls, text)
        instance.reasoning_time = reasoning_time  # type: ignore
        instance.reasoning_content = reasoning_content  # type: ignore
        return instance


# =============================================================================
# Sample Expense Data (for reasoning demo)
# =============================================================================

SAMPLE_EXPENSES = {
    "Q4_2024": {
        "period": "Q4 2024 (Oct-Dec)",
        "department": "Engineering",
        "budget": 150000,
        "expenses": [
            {"date": "2024-10-05", "employee": "Alice Chen", "category": "Travel", "vendor": "Qantas", "amount": 2450.00, "description": "Flight to Sydney client meeting", "approved_by": "Bob Smith"},
            {"date": "2024-10-12", "employee": "Alice Chen", "category": "Meals", "vendor": "The Grounds", "amount": 342.50, "description": "Client dinner - 4 people", "approved_by": "Bob Smith"},
            {"date": "2024-10-15", "employee": "Bob Smith", "category": "Software", "vendor": "JetBrains", "amount": 649.00, "description": "IDE licence renewal", "approved_by": "Carol Davis"},
            {"date": "2024-10-20", "employee": "David Lee", "category": "Travel", "vendor": "Uber", "amount": 89.50, "description": "Airport transfers", "approved_by": "Bob Smith"},
            {"date": "2024-11-01", "employee": "Emma Wilson", "category": "Equipment", "vendor": "Apple Store", "amount": 4299.00, "description": "MacBook Pro 16-inch", "approved_by": "Carol Davis"},
            {"date": "2024-11-03", "employee": "David Lee", "category": "Meals", "vendor": "Various cafes", "amount": 523.00, "description": "Working lunches - November", "approved_by": "Bob Smith"},
            {"date": "2024-11-08", "employee": "Alice Chen", "category": "Conference", "vendor": "PyCon AU", "amount": 1200.00, "description": "Conference registration + travel", "approved_by": "Carol Davis"},
            {"date": "2024-11-15", "employee": "Bob Smith", "category": "Travel", "vendor": "Virgin Australia", "amount": 1850.00, "description": "Flight to Melbourne - partner meeting", "approved_by": "Carol Davis"},
            {"date": "2024-11-18", "employee": "Bob Smith", "category": "Accommodation", "vendor": "Crown Towers", "amount": 890.00, "description": "2 nights Melbourne", "approved_by": "Carol Davis"},
            {"date": "2024-11-22", "employee": "Emma Wilson", "category": "Training", "vendor": "Pluralsight", "amount": 499.00, "description": "Annual subscription", "approved_by": "Bob Smith"},
            {"date": "2024-12-01", "employee": "David Lee", "category": "Software", "vendor": "GitHub", "amount": 252.00, "description": "GitHub Enterprise - 12 seats", "approved_by": "Carol Davis"},
            {"date": "2024-12-05", "employee": "Alice Chen", "category": "Meals", "vendor": "Rockpool", "amount": 1250.00, "description": "Team end-of-year dinner - 8 people", "approved_by": "Carol Davis"},
            {"date": "2024-12-10", "employee": "Frank Zhang", "category": "Travel", "vendor": "Emirates", "amount": 8500.00, "description": "Business class - Sydney to London", "approved_by": "Bob Smith"},
            {"date": "2024-12-12", "employee": "Frank Zhang", "category": "Accommodation", "vendor": "The Savoy", "amount": 2800.00, "description": "4 nights London", "approved_by": "Bob Smith"},
            {"date": "2024-12-15", "employee": "Emma Wilson", "category": "Office Supplies", "vendor": "Officeworks", "amount": 156.00, "description": "Stationery and supplies", "approved_by": "Bob Smith"},
            {"date": "2024-12-18", "employee": "David Lee", "category": "Meals", "vendor": "UberEats", "amount": 890.00, "description": "Late night working meals - December", "approved_by": "Bob Smith"},
        ],
        "company_policy": {
            "meal_limit_per_person": 80.00,
            "domestic_flight_class": "Economy",
            "international_flight_class": "Business (Director+) / Economy",
            "hotel_nightly_limit": 350.00,
            "equipment_approval_threshold": 2000.00,
            "receipt_required_above": 50.00,
        }
    },
}


# =============================================================================
# Helper function to stream widgets
# =============================================================================


async def stream_widget(
    thread_id: str,
    widget: WidgetRoot,
    copy_text: str | None = None,
    generate_id: Callable[[StoreItemType], str] = default_generate_id,
) -> AsyncIterator[ThreadStreamEvent]:
    """Stream a ChatKit widget as a ThreadStreamEvent.

    Args:
        thread_id: The ChatKit thread ID for the conversation.
        widget: The ChatKit widget to display.
        copy_text: Optional text representation of the widget for copy/paste.
        generate_id: Optional function to generate IDs for ChatKit items.

    Yields:
        ThreadStreamEvent: ChatKit event containing the widget.
    """
    item_id = generate_id("message")

    widget_item = WidgetItem(
        id=item_id,
        thread_id=thread_id,
        created_at=datetime.now(),
        widget=widget,
        copy_text=copy_text,
    )

    yield ThreadItemDoneEvent(type="thread.item.done", item=widget_item)


# =============================================================================
# AviationStack API Integration
# =============================================================================

# Known multi-leg flights - maps flight code to first leg departure airport
# This ensures we return the correct segment when user doesn't specify departure
MULTI_LEG_FLIGHTS: dict[str, str] = {
    "QF1": "SYD",   # Sydney → Singapore → London (Kangaroo Route)
    "QF2": "LHR",   # London → Singapore → Sydney
    "SQ21": "SIN",  # Singapore → Newark (world's longest flight)
    "SQ22": "EWR",  # Newark → Singapore
    "EK448": "DXB", # Dubai → Auckland via Sydney
    "BA15": "LHR",  # London → Singapore → Sydney
    "BA16": "SYD",  # Sydney → Singapore → London
}


async def fetch_flight_status(
    flight_iata: str | None = None,
    dep_iata: str | None = None,
    arr_iata: str | None = None,
) -> FlightStatusData | str:
    """Fetch flight status from AviationStack API."""
    api_key = os.environ.get("AVIATIONSTACK_KEY")
    if not api_key:
        return "AVIATIONSTACK_KEY environment variable is not configured. Please set it to use flight tracking."

    params: dict[str, str] = {"access_key": api_key}
    
    # Build query params - combine flight_iata with dep_iata/arr_iata when both provided
    if flight_iata:
        flight_code = flight_iata.upper()
        params["flight_iata"] = flight_code
        
        # For known multi-leg flights, auto-add departure if not specified
        if not dep_iata and flight_code in MULTI_LEG_FLIGHTS:
            dep_iata = MULTI_LEG_FLIGHTS[flight_code]
            logger.info(f"Auto-adding dep_iata={dep_iata} for multi-leg flight {flight_code}")
        
        # IMPORTANT: Also add dep_iata if provided - this ensures correct leg for multi-segment flights
        if dep_iata:
            params["dep_iata"] = dep_iata.upper()
        if arr_iata:
            params["arr_iata"] = arr_iata.upper()
    elif dep_iata and arr_iata:
        params["dep_iata"] = dep_iata.upper()
        params["arr_iata"] = arr_iata.upper()
    elif dep_iata:
        params["dep_iata"] = dep_iata.upper()
    else:
        return "Please provide a flight number (e.g., QF1) or departure/arrival airports."
    
    logger.info(f"AviationStack API params: {params}")  # Log the actual params sent

    try:
        async with httpx.AsyncClient(timeout=30.0) as client:
            response = await client.get(
                "http://api.aviationstack.com/v1/flights",
                params=params,
            )

            if response.status_code != 200:
                return f"AviationStack API error: {response.status_code}"

            data = response.json()

            if "error" in data:
                error_info = data["error"]
                return f"AviationStack error: {error_info.get('message', 'Unknown error')}"

            flights = data.get("data", [])
            if not flights:
                if flight_iata:
                    return f"No flight found with code {flight_iata.upper()}. Please verify the flight number."
                return "No flights found for the specified route."

            flight = flights[0]

            # Parse departure info
            dep = flight.get("departure", {})
            departure = AirportInfo(
                airport=dep.get("airport", ""),
                iata=dep.get("iata", ""),
                icao=dep.get("icao", ""),
                terminal=dep.get("terminal"),
                gate=dep.get("gate"),
                delay=dep.get("delay"),
                scheduled=dep.get("scheduled"),
                estimated=dep.get("estimated"),
                actual=dep.get("actual"),
                timezone=dep.get("timezone"),
            )

            # Parse arrival info
            arr = flight.get("arrival", {})
            arrival = AirportInfo(
                airport=arr.get("airport", ""),
                iata=arr.get("iata", ""),
                icao=arr.get("icao", ""),
                terminal=arr.get("terminal"),
                gate=arr.get("gate"),
                baggage=arr.get("baggage"),
                delay=arr.get("delay"),
                scheduled=arr.get("scheduled"),
                estimated=arr.get("estimated"),
                actual=arr.get("actual"),
                timezone=arr.get("timezone"),
            )

            # Parse live data if available
            live_data = None
            live = flight.get("live")
            if live:
                live_data = LiveFlightData(
                    updated=live.get("updated"),
                    latitude=live.get("latitude"),
                    longitude=live.get("longitude"),
                    altitude=live.get("altitude"),
                    direction=live.get("direction"),
                    speed_horizontal=live.get("speed_horizontal"),
                    speed_vertical=live.get("speed_vertical"),
                    is_ground=live.get("is_ground", False),
                )

            flight_info = flight.get("flight", {})
            airline = flight.get("airline", {})

            # Determine actual flight status - if we have live data with altitude > 0, it's in flight
            raw_status = flight.get("flight_status", "scheduled")
            if live_data and live_data.altitude and live_data.altitude > 0 and not live_data.is_ground:
                actual_status = "active"  # In flight
            elif live_data and live_data.is_ground and departure.actual:
                actual_status = "landed" if arrival.actual else "active"  # Taxiing or landed
            else:
                actual_status = raw_status

            return FlightStatusData(
                flight_date=flight.get("flight_date", ""),
                flight_status=actual_status,
                flight_iata=flight_info.get("iata", ""),
                flight_number=flight_info.get("number", ""),
                airline_name=airline.get("name", ""),
                airline_iata=airline.get("iata", ""),
                departure=departure,
                arrival=arrival,
                live=live_data,
            )

    except httpx.TimeoutException:
        return "Flight API request timed out. Please try again."
    except Exception as e:
        return f"Error fetching flight data: {str(e)}"


# =============================================================================
# Parking Sign Analysis (GPT-4o Vision)
# =============================================================================


async def analyse_parking_sign(
    image_data: bytes,
    image_content_type: str,
    current_time: str | None = None,
) -> ParkingAnalysisData | str:
    """Analyse a parking sign image using GPT-5.1 vision."""
    # Use the same credentials as the rest of the app
    endpoint = os.environ.get("AOI_ENDPOINT_SWDN")
    api_key = os.environ.get("AOI_KEY_SWDN")
    deployment = "gpt-5.1"  # Hardcoded for vision tasks

    if not endpoint or not api_key:
        return "Azure OpenAI is not configured. Please set AOI_ENDPOINT_SWDN and AOI_KEY_SWDN."

    try:
        api_version = os.environ.get("AZURE_OPENAI_API_VERSION", "2024-06-01")

        image_base64 = base64.b64encode(image_data).decode("utf-8")
        image_url = f"data:{image_content_type};base64,{image_base64}"

        time_context = f"\nCurrent time context: {current_time}" if current_time else ""
        prompt = f"""Analyse this parking sign image and determine if parking is allowed.

Provide your analysis in the following JSON format:
{{
    "can_park": true/false,
    "verdict": "Short one-sentence verdict",
    "confidence": "high/medium/low",
    "restrictions": [
        {{
            "type": "Type of restriction",
            "hours": "Operating hours if applicable",
            "days": "Days if applicable",
            "duration": "Time limit if applicable",
            "notes": "Any additional notes"
        }}
    ],
    "time_limit": "Maximum parking duration if any",
    "detailed_analysis": "Detailed explanation of what the sign says",
    "advice": "Practical advice for the driver",
    "sign_description": "Description of signs visible in the image"
}}{time_context}

Be thorough but practical. Focus on giving a clear yes/no answer."""

        # Ensure endpoint doesn't have trailing slash
        endpoint = endpoint.rstrip("/")
        
        async with httpx.AsyncClient(timeout=60.0) as client:
            response = await client.post(
                f"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={api_version}",
                headers={
                    "api-key": api_key,
                    "Content-Type": "application/json",
                },
                json={
                    "messages": [
                        {
                            "role": "user",
                            "content": [
                                {"type": "text", "text": prompt},
                                {"type": "image_url", "image_url": {"url": image_url, "detail": "high"}},
                            ],
                        }
                    ],
                    "max_completion_tokens": 1500,
                    "temperature": 0.1,
                },
            )

            if response.status_code != 200:
                return f"Azure OpenAI API error: {response.status_code} - {response.text}"

            result = response.json()
            content = result["choices"][0]["message"]["content"]

            # Extract JSON from possible markdown
            if "```json" in content:
                content = content.split("```json")[1].split("```")[0]
            elif "```" in content:
                content = content.split("```")[1].split("```")[0]

            analysis = json.loads(content.strip())

            restrictions = []
            for r in analysis.get("restrictions", []):
                restrictions.append(
                    ParkingRestriction(
                        type=r.get("type", ""),
                        hours=r.get("hours"),
                        days=r.get("days"),
                        duration=r.get("duration"),
                        notes=r.get("notes"),
                    )
                )

            return ParkingAnalysisData(
                can_park=analysis.get("can_park", False),
                verdict=analysis.get("verdict", ""),
                confidence=analysis.get("confidence", "medium"),
                restrictions=restrictions,
                time_limit=analysis.get("time_limit"),
                detailed_analysis=analysis.get("detailed_analysis", ""),
                advice=analysis.get("advice", ""),
                current_time_context=current_time,
                sign_description=analysis.get("sign_description", ""),
            )

    except json.JSONDecodeError as e:
        return f"Failed to parse AI response: {str(e)}"
    except Exception as e:
        return f"Error analysing parking sign: {str(e)}"


# =============================================================================
# Expense Analysis with o3 Reasoning
# =============================================================================

# Initialise o3 reasoning client
_reasoning_client: OpenAI | None = None


def get_reasoning_client() -> OpenAI | None:
    """Get or create the OpenAI client for reasoning model."""
    global _reasoning_client
    if _reasoning_client is None:
        endpoint = os.environ.get("AOI_ENDPOINT_SWDN")
        api_key = os.environ.get("AOI_KEY_SWDN")
        if endpoint and api_key:
            _reasoning_client = OpenAI(
                api_key=api_key,
                base_url=f"{endpoint.rstrip('/')}/openai/v1/",
                default_headers={"api-key": api_key},
            )
    return _reasoning_client


# Intent classification for routing queries
class QueryIntent:
    """Possible intents for user queries."""
    FLIGHT = "flight"           # Flight tracking, status, routes
    PARKING = "parking"         # Parking sign analysis (vision)
    EXPENSE = "expense"         # Expense analysis (reasoning model)
    GENERAL = "general"         # General chat / other queries


async def classify_intent(user_message: str, has_image: bool = False) -> str:
    """Classify user intent using gpt-4.1-mini for fast routing.
    
    Args:
        user_message: The user's message text
        has_image: Whether the message includes an image attachment
        
    Returns:
        QueryIntent value indicating the detected intent
    """
    # If there's an image, it's likely parking sign analysis
    if has_image:
        logger.info("Image detected - routing to parking/vision analysis")
        return QueryIntent.PARKING
    
    # Use gpt-4.1-mini for fast intent classification
    client = get_reasoning_client()
    if not client:
        # Fallback to keyword matching if no client
        logger.warning("No reasoning client - falling back to keyword matching")
        return _keyword_intent_fallback(user_message)
    
    try:
        response = client.responses.create(
            model="gpt-5.1",  # Fast model for classification
            input=[
                {
                    "role": "system",
                    "content": """You are an intent classifier. Classify the user's message into ONE of these categories:

- flight: Questions about flight status, tracking, routes, airports, airlines, departures, arrivals
- parking: Questions about parking signs, parking rules, where to park, parking restrictions
- expense: Questions about expenses, spending, budgets, cost analysis, Q1/Q2/Q3/Q4 reviews, financial reports
- general: Any other questions or general chat

Respond with ONLY the category name, nothing else."""
                },
                {
                    "role": "user", 
                    "content": user_message
                }
            ],
            max_output_tokens=20,  # Minimum is 16, use 20 for safety
        )
        
        intent = response.output_text.strip().lower()
        logger.info(f"Intent classified as: {intent}")
        
        # Map response to QueryIntent
        if intent in ["flight", "flights"]:
            return QueryIntent.FLIGHT
        elif intent in ["parking", "park"]:
            return QueryIntent.PARKING
        elif intent in ["expense", "expenses", "budget"]:
            return QueryIntent.EXPENSE
        else:
            return QueryIntent.GENERAL
            
    except Exception as e:
        logger.error(f"Intent classification failed: {e}")
        return _keyword_intent_fallback(user_message)


def _keyword_intent_fallback(user_message: str) -> str:
    """Fallback keyword-based intent detection."""
    text = user_message.lower()
    
    expense_keywords = ["expense", "expenses", "spending", "budget", "cost", "q1", "q2", "q3", "q4", "financial"]
    flight_keywords = ["flight", "flights", "airport", "airline", "departure", "arrival", "flying", "qf", "va", "jq"]
    parking_keywords = ["parking", "park", "sign", "restriction"]
    
    if any(kw in text for kw in expense_keywords):
        return QueryIntent.EXPENSE
    elif any(kw in text for kw in flight_keywords):
        return QueryIntent.FLIGHT
    elif any(kw in text for kw in parking_keywords):
        return QueryIntent.PARKING
    else:
        return QueryIntent.GENERAL


async def analyse_expenses_with_reasoning_streaming(
    period: str = "Q4_2024",
):
    """Async generator that streams expense analysis with reasoning summaries.
    
    Yields tuples of (event_type, data) where:
    - ("reasoning_delta", text) - Incremental reasoning summary text
    - ("reasoning_done", None) - Reasoning complete
    - ("output_delta", text) - Incremental output text  
    - ("complete", ExpenseAnalysisResponse) - Final result
    - ("error", str) - Error message
    """
    import time
    import asyncio
    from concurrent.futures import ThreadPoolExecutor
    import queue
    import threading
    
    client = get_reasoning_client()
    if not client:
        yield ("error", "Azure OpenAI credentials not configured for reasoning model.")
        return
    
    deployment = "o3"
    
    expense_data = SAMPLE_EXPENSES.get(period)
    if not expense_data:
        yield ("error", f"No expense data found for period: {period}")
        return
    
    expenses_text = json.dumps(expense_data["expenses"], indent=2)
    policy_text = json.dumps(expense_data["company_policy"], indent=2)
    
    prompt = f"""Analyse the following expense report for the {expense_data['department']} department ({expense_data['period']}).

EXPENSE DATA:
{expenses_text}

COMPANY POLICY:
{policy_text}

BUDGET: ${expense_data['budget']:,.2f}

Please provide a thorough analysis including:
1. Total spending vs budget
2. Any policy violations or concerns
3. Spending patterns by category and employee
4. Specific items that need review
5. Recommendations for cost optimisation

Be specific and cite actual expense items when identifying issues."""

    # Use a queue to pass events from the sync stream to the async generator
    event_queue: queue.Queue = queue.Queue()
    
    def stream_in_thread():
        """Run the streaming API call in a separate thread."""
        try:
            start_time = time.time()
            
            stream = client.responses.create(
                model=deployment,
                input=[{"role": "user", "content": prompt}],
                reasoning={
                    "effort": "low",
                    "summary": "auto",
                },
                stream=True,
            )
            
            reasoning_summary = ""
            output_text = ""
            
            for event in stream:
                event_type = getattr(event, "type", None)
                
                # Log all events for debugging
                logger.debug(f"Streaming event: {event_type}")
                
                # Handle reasoning summary delta events (correct event name)
                if event_type == "response.reasoning_summary_text.delta":
                    delta = getattr(event, "delta", "")
                    if delta:
                        reasoning_summary += delta
                        event_queue.put(("reasoning_delta", delta))
                        logger.info(f"Reasoning delta: {delta[:50]}...")
                
                # Handle reasoning summary done (correct event name)
                elif event_type == "response.reasoning_summary_text.done":
                    event_queue.put(("reasoning_done", None))
                    logger.info("Reasoning summary complete")
                
                # Handle output text delta events
                elif event_type == "response.output_text.delta":
                    delta = getattr(event, "delta", "")
                    if delta:
                        output_text += delta
                        event_queue.put(("output_delta", delta))
                
                # Handle completion
                elif event_type == "response.completed":
                    logger.info("Response completed event received")
                
                # Log other events we're not handling
                elif event_type and not event_type.startswith("response.created") and not event_type.startswith("response.in_progress"):
                    logger.info(f"Unhandled event type: {event_type}")
            
            end_time = time.time()
            reasoning_time = end_time - start_time
            
            if not output_text:
                output_text = "No analysis generated."
            
            logger.info(f"Expense analysis completed in {reasoning_time:.1f}s")
            
            result = ExpenseAnalysisResponse(
                output_text, 
                reasoning_time, 
                reasoning_summary.strip() if reasoning_summary else None
            )
            event_queue.put(("complete", result))
            
        except Exception as e:
            logger.error(f"Error in expense analysis: {e}")
            event_queue.put(("error", f"Error analysing expenses: {str(e)}"))
        finally:
            event_queue.put(None)  # Signal end of stream
    
    # Start the streaming in a background thread
    thread = threading.Thread(target=stream_in_thread, daemon=True)
    thread.start()
    
    # Yield events as they come in
    try:
        while True:
            # Check for events with a small timeout to not block forever
            try:
                event = await asyncio.to_thread(event_queue.get, timeout=0.1)
                if event is None:
                    break  # End of stream
                yield event
            except queue.Empty:
                # No event yet, continue waiting
                await asyncio.sleep(0.01)
    finally:
        thread.join(timeout=1.0)  # Clean up thread


async def analyse_expenses_with_reasoning(
    department: str = "Engineering",
    period: str = "Q4_2024",
) -> ExpenseAnalysisResponse | str:
    """Analyse expense report using o3 reasoning model with Responses API.
    
    This function calls the o3 model using the Responses API with 
    reasoning_summary enabled to get chain-of-thought summaries.
    """
    import time
    import asyncio
    
    client = get_reasoning_client()
    if not client:
        return "Azure OpenAI credentials not configured for reasoning model."
    
    # Hardcoded o3 deployment for reasoning tasks
    deployment = "o3"
    
    # Get expense data
    expense_data = SAMPLE_EXPENSES.get(period)
    if not expense_data:
        return f"No expense data found for period: {period}"
    
    # Build the analysis prompt
    expenses_text = json.dumps(expense_data["expenses"], indent=2)
    policy_text = json.dumps(expense_data["company_policy"], indent=2)
    
    prompt = f"""Analyse the following expense report for the {expense_data['department']} department ({expense_data['period']}).

EXPENSE DATA:
{expenses_text}

COMPANY POLICY:
{policy_text}

BUDGET: ${expense_data['budget']:,.2f}

Please provide a thorough analysis including:
1. Total spending vs budget
2. Any policy violations or concerns
3. Spending patterns by category and employee
4. Specific items that need review
5. Recommendations for cost optimisation

Be specific and cite actual expense items when identifying issues."""

    try:
        start_time = time.time()
        
        # Call o3 using Responses API with reasoning_summary
        def call_reasoning():
            return client.responses.create(
                model=deployment,
                input=[{"role": "user", "content": prompt}],
                reasoning={
                    "effort": "low",
                    "summary": "auto",  # Enable reasoning summary
                },
            )
        
        response = await asyncio.to_thread(call_reasoning)
        
        end_time = time.time()
        reasoning_time = end_time - start_time
        
        # Extract response text and reasoning summary
        analysis = ""
        reasoning_summary = ""
        
        for item in response.output:
            # Get reasoning summary from ReasoningResponseItem
            if item.type == "reasoning" and hasattr(item, "summary") and item.summary:
                for summary_part in item.summary:
                    if hasattr(summary_part, "text"):
                        reasoning_summary += summary_part.text + "\n"
            # Get assistant's output text
            elif item.type == "message" and hasattr(item, "content"):
                for content_part in item.content:
                    if hasattr(content_part, "text"):
                        analysis += content_part.text
        
        if not analysis:
            analysis = "No analysis generated."
        
        # Use reasoning summary as the reasoning_content
        reasoning_content = reasoning_summary.strip() if reasoning_summary else None
        
        logger.info(f"Expense analysis completed in {reasoning_time:.1f}s")
        if reasoning_content:
            logger.info(f"Reasoning summary: {reasoning_content[:200]}...")
        
        return ExpenseAnalysisResponse(analysis, reasoning_time, reasoning_content)
        
    except Exception as e:
        logger.error(f"Error in expense analysis: {e}")
        return f"Error analysing expenses: {str(e)}"


# =============================================================================
# Agent Tools
# =============================================================================


async def get_flight_status(
    flight_iata: Annotated[str | None, Field(description="Flight IATA code - MUST be uppercase airline code + flight number (e.g., 'QF1' for Qantas 1, 'AA100' for American Airlines 100). Convert any natural language flight references to proper IATA format.")] = None,
    dep_iata: Annotated[str | None, Field(description="Departure airport IATA code - MUST be a 3-letter uppercase airport code. Convert city names to IATA codes: Sydney=SYD, Melbourne=MEL, London Heathrow=LHR, Singapore=SIN, Los Angeles=LAX, New York JFK=JFK, etc. CRITICAL: Always provide this when user mentions a departure city/airport to ensure correct flight segment.")] = None,
    arr_iata: Annotated[str | None, Field(description="Arrival airport IATA code - MUST be a 3-letter uppercase airport code. Convert city names to IATA codes: Sydney=SYD, Melbourne=MEL, London Heathrow=LHR, Singapore=SIN, Los Angeles=LAX, New York JFK=JFK, etc.")] = None,
) -> str:
    """Get real-time flight status information.

    IMPORTANT: For multi-leg flights (e.g., QF1 Sydney→Singapore→London), ALWAYS specify dep_iata 
    to get the correct segment. Without dep_iata, the API may return the wrong leg.
    
    Use this tool to look up flight information. You can search by:
    - Flight number only (flight_iata): Use when user doesn't mention departure airport
    - Flight + departure (flight_iata + dep_iata): Use when user says 'from Sydney' or similar - PREFERRED
    - Route (dep_iata + arr_iata): Search all flights between two airports
    - Departures only (dep_iata): List all departures from an airport
    
    Always convert natural language to IATA codes before calling:
    - Cities → 3-letter airport codes (Sydney → SYD, London → LHR)
    - Airline names → 2-letter codes + number (Qantas 1 → QF1)
    """
    result = await fetch_flight_status(flight_iata, dep_iata, arr_iata)

    if isinstance(result, str):
        return result

    summary = (
        f"Flight {result.flight_iata} ({result.airline_name}): "
        f"{result.departure.iata} → {result.arrival.iata}, "
        f"Status: {result.flight_status.title()}"
    )
    return FlightResponse(summary, result)


def show_airport_selector() -> str:
    """Show an interactive airport selector widget.

    Use this when the user wants to browse available airports or
    select an airport to search flights from.
    """
    return ShowAirportSelector("__SHOW_AIRPORT_SELECTOR__")


def show_route_selector() -> str:
    """Show popular flight routes to search.

    Use this when the user wants to see popular flight routes.
    """
    return ShowRouteSelector("__SHOW_ROUTE_SELECTOR__")


def show_parking_analysis_prompt() -> str:
    """Show instructions for uploading a parking sign image.

    Use this when the user wants to analyse a parking sign.
    """
    return ShowParkingPrompt("__SHOW_PARKING_PROMPT__")


async def analyse_expense_report(
    department: Annotated[str, Field(description="Department name to analyse (e.g., 'Engineering')")] = "Engineering",
) -> str:
    """Analyse expense reports using advanced reasoning.

    Use this tool when the user wants to:
    - Review expense reports for policy violations
    - Identify unusual spending patterns
    - Get budget analysis and recommendations
    - Check for compliance issues

    This uses the o3 reasoning model which will think through the analysis
    step by step, showing 'Thought for X seconds' in the response.
    """
    result = await analyse_expenses_with_reasoning(department=department, period="Q4_2024")
    
    if isinstance(result, str):
        return result
    
    return result  # ExpenseAnalysisResponse


# =============================================================================
# ChatKit Server Implementation
# =============================================================================


class SwiftRoverChatKitServer(ChatKitServer[dict[str, Any]]):
    """ChatKit server for SwiftRover - your AI travel assistant."""

    def __init__(self, data_store: SQLiteStore, attachment_store: FileBasedAttachmentStore):
        super().__init__(data_store, attachment_store)

        logger.info("Initialising SwiftRover server")
        
        # Get Azure OpenAI credentials from environment
        endpoint = os.environ.get("AOI_ENDPOINT_SWDN", "")
        api_key = os.environ.get("AOI_KEY_SWDN", "")
        
        if not endpoint or not api_key:
            raise ValueError("AOI_ENDPOINT_SWDN and AOI_KEY_SWDN environment variables must be set")

        try:
            self.agent = ChatAgent(
                chat_client=AzureOpenAIChatClient(
                    endpoint=endpoint,
                    api_key=api_key,
                    deployment_name="gpt-5.1",  # Hardcoded for chat/vision tasks
                ),
                instructions=(
                    "You are SwiftRover, a smart and helpful AI travel assistant. You help travellers with:\n\n"
                    "1. **Flight Tracking**: Look up real-time flight status using flight numbers "
                    "and airport codes.\n\n"
                    "   CRITICAL: You MUST convert all natural language to IATA codes:\n"
                    "   - City names → 3-letter airport IATA codes (Sydney=SYD, Melbourne=MEL, "
                    "London Heathrow=LHR, Singapore Changi=SIN, Los Angeles=LAX, JFK=JFK, Dubai=DXB)\n"
                    "   - Flight references → IATA format (Qantas 1=QF1, American 100=AA100)\n\n"
                    "   IMPORTANT for multi-leg flights: When user says 'QF1 from Sydney', always provide "
                    "BOTH flight_iata='QF1' AND dep_iata='SYD' to get the correct segment. Without dep_iata, "
                    "multi-leg flights may return the wrong leg.\n\n"
                    "2. **Parking Sign Analysis**: When users upload a photo of a parking sign, "
                    "analyse it to determine if parking is allowed.\n\n"
                    "3. **Expense Analysis** (Reasoning Demo): Analyse expense reports for policy violations "
                    "and spending patterns. This uses the o3 reasoning model which thinks through problems "
                    "step-by-step, showing 'Thought for X seconds' in the UI.\n\n"
                    "Available tools:\n"
                    "- get_flight_status: Get real-time flight information\n"
                    "- show_airport_selector: Show popular airports to choose from\n"
                    "- show_route_selector: Show popular flight routes\n"
                    "- show_parking_analysis_prompt: Show parking sign upload instructions\n"
                    "- analyse_expense_report: Analyse expense reports with advanced reasoning\n\n"
                    "Be concise and helpful. For parking questions, give clear yes/no answers."
                ),
                tools=[get_flight_status, show_airport_selector, show_route_selector, show_parking_analysis_prompt, analyse_expense_report],
            )
            logger.info("Agent initialized successfully")
        except Exception as e:
            logger.error(f"Failed to initialize agent: {e}")
            raise

        self.converter = ThreadItemConverter(
            attachment_data_fetcher=self._fetch_attachment_data,
        )

    async def _fetch_attachment_data(self, attachment_id: str) -> bytes:
        """Fetch attachment binary data for the converter."""
        return await attachment_store.read_attachment_bytes(attachment_id)

    async def respond(
        self,
        thread: ThreadMetadata,
        input_user_message: UserMessageItem | None,
        context: dict[str, Any],
    ) -> AsyncIterator[ThreadStreamEvent]:
        """Handle incoming user messages and generate responses."""

        if input_user_message is None:
            return

        logger.info(f"Processing message for thread: {thread.id}")

        try:
            # Check for image attachments (parking sign analysis)
            parking_image: bytes | None = None
            parking_content_type: str | None = None

            # Debug: log the user message content
            logger.info(f"User message content: {input_user_message.content}")
            logger.info(f"User message attachments: {getattr(input_user_message, 'attachments', None)}")

            # Check attachments field directly (ChatKit style)
            if hasattr(input_user_message, "attachments") and input_user_message.attachments:
                for attachment in input_user_message.attachments:
                    logger.info(f"Found attachment: {attachment}")
                    attachment_id = getattr(attachment, "id", None)
                    attachment_type = getattr(attachment, "type", None)
                    mime_type = getattr(attachment, "mime_type", "")
                    
                    if attachment_type == "image" or (mime_type and mime_type.startswith("image/")):
                        if attachment_id:
                            logger.info(f"Loading image attachment: {attachment_id}")
                            parking_image = await self._fetch_attachment_data(attachment_id)
                            parking_content_type = mime_type or "image/jpeg"
                            break

            # Also check content for inline images
            if not parking_image and input_user_message.content:
                for content_part in input_user_message.content:
                    logger.info(f"Content part: {content_part}, type={getattr(content_part, 'type', None)}")
                    if hasattr(content_part, "type") and content_part.type == "image":
                        # Get attachment ID from the image
                        attachment_id = getattr(content_part, "attachment_id", None)
                        if attachment_id:
                            parking_image = await self._fetch_attachment_data(attachment_id)
                            parking_content_type = "image/jpeg"  # Default
                            break

            # If image was uploaded, analyse parking sign directly
            if parking_image and parking_content_type:
                logger.info("Analysing parking sign from uploaded image")

                current_time = datetime.now().strftime("%A, %I:%M %p")
                result = await analyse_parking_sign(parking_image, parking_content_type, current_time)

                if isinstance(result, str):
                    # Error - show error widget
                    widget = render_error_widget("Analysis Failed", result)
                    async for event in stream_widget(thread_id=thread.id, widget=widget):
                        yield event
                else:
                    # Success - show parking widget
                    widget = render_parking_widget(result)
                    copy_text = parking_widget_copy_text(result)
                    async for event in stream_widget(thread_id=thread.id, widget=widget, copy_text=copy_text):
                        yield event
                return

            # Track widget flags
            flight_data: FlightStatusData | None = None
            show_airport_sel = False
            show_route_sel = False
            show_parking_prompt = False

            # Load thread history
            thread_items_page = await self.store.load_thread_items(
                thread_id=thread.id,
                after=None,
                limit=1000,
                order="asc",
                context=context,
            )
            thread_items = thread_items_page.data

            # Convert to agent messages
            agent_messages = await self.converter.to_agent_input(thread_items)

            if not agent_messages:
                logger.warning("No messages after conversion")
                return

            logger.info(f"Running agent with {len(agent_messages)} message(s)")

            # Extract user text and check for image attachments
            user_text = ""
            has_image = False
            if input_user_message.content:
                if isinstance(input_user_message.content, str):
                    user_text = input_user_message.content
                elif isinstance(input_user_message.content, list):
                    for part in input_user_message.content:
                        if hasattr(part, "text"):
                            user_text += part.text + " "
                        if hasattr(part, "type") and part.type in ["image", "image_url"]:
                            has_image = True
            
            # Check for image attachments
            if input_user_message.attachments:
                has_image = True

            # Use intent classification to route the query
            intent = await classify_intent(user_text, has_image)
            logger.info(f"Query intent: {intent}")

            # Handle expense queries with o3 reasoning model
            if intent == QueryIntent.EXPENSE:
                logger.info("Expense intent detected - using o3 reasoning model")
                
                import time
                start_time = time.time()
                
                # Create workflow item ID upfront
                workflow_item_id = default_generate_id("workflow")
                
                # Step 1: Emit initial workflow item with EMPTY tasks
                initial_workflow_item = WorkflowItem(
                    id=workflow_item_id,
                    thread_id=thread.id,
                    created_at=datetime.now(),
                    workflow=Workflow(
                        type="reasoning",
                        tasks=[],  # Start empty - we'll add tasks via updates
                        expanded=True,  # Show expanded during thinking
                    ),
                )
                yield ThreadItemAddedEvent(type="thread.item.added", item=initial_workflow_item)
                
                # Step 2: Add the initial "Thinking..." task via WorkflowTaskAdded
                thought_task = ThoughtTask(
                    type="thought",
                    title="Thinking...",
                    content="",  # Start empty, will stream content
                )
                # Also add to our local workflow so we can update it
                initial_workflow_item.workflow.tasks.append(thought_task)
                
                yield ThreadItemUpdatedEvent(
                    type="thread.item.updated",
                    item_id=workflow_item_id,
                    update=WorkflowTaskAdded(task=thought_task, task_index=0),
                )
                
                # Step 3: Stream reasoning from o3 and emit updates in real-time
                reasoning_content = ""
                analysis_text = ""
                expense_result = None
                update_counter = 0
                
                async for event_type, data in analyse_expenses_with_reasoning_streaming(period="Q4_2024"):
                    if event_type == "reasoning_delta":
                        # Accumulate reasoning content
                        reasoning_content += data
                        # Update the task content
                        thought_task.content = reasoning_content
                        
                        # Emit WorkflowTaskUpdated for each delta (or batch them)
                        update_counter += 1
                        # Emit every delta to show real-time streaming
                        yield ThreadItemUpdatedEvent(
                            type="thread.item.updated",
                            item_id=workflow_item_id,
                            update=WorkflowTaskUpdated(task=thought_task, task_index=0),
                        )
                        
                        # Log progress periodically
                        if update_counter % 20 == 0:
                            elapsed = int(time.time() - start_time)
                            logger.info(f"Reasoning streaming: {len(reasoning_content)} chars, {update_counter} updates ({elapsed}s)")
                    
                    elif event_type == "reasoning_done":
                        logger.info(f"Reasoning streaming complete: {len(reasoning_content)} chars total")
                    
                    elif event_type == "output_delta":
                        analysis_text += data
                    
                    elif event_type == "complete":
                        expense_result = data  # ExpenseAnalysisResponse
                
                # Step 4: Calculate final timing
                end_time = time.time()
                reasoning_seconds = int(end_time - start_time)
                logger.info(f"Expense analysis completed in {reasoning_seconds}s with {update_counter} streaming updates")
                
                # Emit final workflow item with reasoning summary (collapsed)
                final_workflow_item = WorkflowItem(
                    id=workflow_item_id,
                    thread_id=thread.id,
                    created_at=datetime.now(),
                    workflow=Workflow(
                        type="reasoning",
                        tasks=[
                            ThoughtTask(
                                type="thought",
                                title=f"Thought for {reasoning_seconds}s",
                                content=reasoning_content.strip() if reasoning_content else "Analysed expense data",
                            )
                        ],
                        summary=CustomSummary(title=f"Thought for {reasoning_seconds}s", icon="sparkle"),
                        expanded=False,  # Collapse when done
                    ),
                )
                yield ThreadItemDoneEvent(type="thread.item.done", item=final_workflow_item)
                
                # Step 4: Emit the analysis as an assistant message
                final_text = analysis_text if analysis_text else (str(expense_result) if expense_result else "Analysis complete.")
                assistant_message_id = default_generate_id("message")
                assistant_message = AssistantMessageItem(
                    id=assistant_message_id,
                    thread_id=thread.id,
                    created_at=datetime.now(),
                    content=[
                        AssistantMessageContent(
                            type="output_text",
                            text=final_text,
                            annotations=[],
                        )
                    ],
                )
                yield ThreadItemDoneEvent(type="thread.item.done", item=assistant_message)
                
                logger.info(f"Completed expense analysis for thread: {thread.id}")
                return  # Done with expense query - skip agent

            # For non-expense queries, use the agent as normal
            agent_stream = self.agent.run_stream(agent_messages)

            # Simple intercept stream for non-expense queries
            async def intercept_stream() -> AsyncIterator[AgentRunResponseUpdate]:
                nonlocal flight_data, show_airport_sel, show_route_sel, show_parking_prompt
                async for update in agent_stream:
                    if update.contents:
                        for content in update.contents:
                            if isinstance(content, FunctionResultContent):
                                result = content.result

                                if isinstance(result, FlightResponse):
                                    flight_data = result.data
                                    logger.info(f"Flight data extracted: {flight_data.flight_iata}")
                                elif isinstance(result, ShowAirportSelector):
                                    show_airport_sel = True
                                elif isinstance(result, ShowRouteSelector):
                                    show_route_sel = True
                                elif isinstance(result, ShowParkingPrompt):
                                    show_parking_prompt = True
                    yield update

            async for event in stream_agent_response(intercept_stream(), thread_id=thread.id):
                yield event

            # Render widgets based on captured data
            if flight_data:
                logger.info(f"Creating flight widget for: {flight_data.flight_iata}")
                widget = render_flight_widget(flight_data)
                copy_text = flight_widget_copy_text(flight_data)
                async for event in stream_widget(thread_id=thread.id, widget=widget, copy_text=copy_text):
                    yield event

            if show_airport_sel:
                logger.info("Creating airport selector widget")
                widget = render_airport_selector_widget()
                copy_text = airport_selector_copy_text()
                async for event in stream_widget(thread_id=thread.id, widget=widget, copy_text=copy_text):
                    yield event

            if show_route_sel:
                logger.info("Creating route selector widget")
                widget = render_route_selector_widget()
                async for event in stream_widget(thread_id=thread.id, widget=widget):
                    yield event

            if show_parking_prompt:
                logger.info("Creating parking prompt widget")
                widget = render_parking_upload_prompt()
                async for event in stream_widget(thread_id=thread.id, widget=widget):
                    yield event

            logger.info(f"Completed processing for thread: {thread.id}")

        except Exception as e:
            logger.error(f"Error processing message: {e}", exc_info=True)

    async def action(
        self,
        thread: ThreadMetadata,
        action: Action[str, Any],
        sender: WidgetItem | None,
        context: dict[str, Any],
    ) -> AsyncIterator[ThreadStreamEvent]:
        """Handle widget actions from the frontend."""

        logger.info(f"Received action: {action.type} for thread: {thread.id}")

        if action.type == "airport_selected":
            iata = action.payload.get("iata", "")
            name = action.payload.get("name", "")

            logger.info(f"Airport selected: {name} ({iata})")

            result = await fetch_flight_status(dep_iata=iata)

            if isinstance(result, str):
                widget = render_error_widget("Search Failed", result)
                async for event in stream_widget(thread_id=thread.id, widget=widget):
                    yield event
            else:
                widget = render_flight_widget(result)
                copy_text = flight_widget_copy_text(result)
                async for event in stream_widget(thread_id=thread.id, widget=widget, copy_text=copy_text):
                    yield event

        elif action.type == "route_selected":
            dep_iata = action.payload.get("dep_iata", "")
            arr_iata = action.payload.get("arr_iata", "")
            label = action.payload.get("label", "")

            logger.info(f"Route selected: {label}")

            result = await fetch_flight_status(dep_iata=dep_iata, arr_iata=arr_iata)

            if isinstance(result, str):
                widget = render_error_widget("Search Failed", result)
                async for event in stream_widget(thread_id=thread.id, widget=widget):
                    yield event
            else:
                widget = render_flight_widget(result)
                copy_text = flight_widget_copy_text(result)
                async for event in stream_widget(thread_id=thread.id, widget=widget, copy_text=copy_text):
                    yield event

        else:
            logger.warning(f"Unknown action type: {action.type}")


# =============================================================================
# FastAPI Application
# =============================================================================

# Initialize stores
data_store = SQLiteStore(DB_PATH)
attachment_store = FileBasedAttachmentStore(
    uploads_dir=UPLOADS_DIR,
    base_url=f"http://{SERVER_HOST}:{SERVER_PORT}",
    data_store=data_store,
)

# Create ChatKit server
chatkit_server = SwiftRoverChatKitServer(data_store, attachment_store)

# Create FastAPI app
app = FastAPI(title="SwiftRover - AI Travel Assistant")

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173", "http://127.0.0.1:5173"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ChatKit endpoint
@app.post("/chatkit")
async def chatkit_endpoint(request: Request):
    """Main ChatKit endpoint that handles all ChatKit requests."""
    logger.debug(f"Received ChatKit request from {request.client}")
    request_body = await request.body()
    context = {"request": request}

    try:
        result = await chatkit_server.process(request_body, context)
        if hasattr(result, "__aiter__"):  # StreamingResult
            return StreamingResponse(result, media_type="text/event-stream")
        return Response(content=result.json, media_type="application/json")
    except Exception as e:
        logger.error(f"Error processing ChatKit request: {e}", exc_info=True)
        raise


@app.post("/upload/{attachment_id}")
async def upload_attachment(attachment_id: str, request: Request) -> JSONResponse:
    """Handle file upload (phase 2 of two-phase upload)."""
    content_type = request.headers.get("content-type", "")
    
    # Check if it's multipart form data
    if "multipart/form-data" in content_type:
        # Parse as form data
        form = await request.form()
        # Get the first file from the form
        for key, value in form.items():
            if hasattr(value, "read"):
                # It's an UploadFile
                body = await value.read()
                break
        else:
            # No file found in form, try raw body
            body = await request.body()
    else:
        # Raw binary upload
        body = await request.body()
    
    logger.info(f"Upload {attachment_id}: received {len(body)} bytes, content-type: {content_type}")
    
    success = await attachment_store.store_attachment(attachment_id, body)
    if success:
        return JSONResponse({"status": "ok", "id": attachment_id})
    return JSONResponse({"status": "error", "message": "Failed to store attachment"}, status_code=500)


@app.get("/preview/{attachment_id}")
async def preview_attachment(attachment_id: str) -> Response:
    """Serve attachment for preview."""
    data = await attachment_store.read_attachment_bytes(attachment_id)
    if data:
        content_type = attachment_store.get_content_type(attachment_id)
        return Response(content=data, media_type=content_type)
    return Response(status_code=404)


# =============================================================================
# Main Entry Point
# =============================================================================

if __name__ == "__main__":
    print("\n" + "=" * 60)
    print("🚀 SwiftRover - AI Travel Assistant")
    print("=" * 60)
    print("\nFeatures:")
    print("  - ✈️  Flight Tracking (AviationStack API)")
    print("  - 🅿️  Parking Sign Analysis (gpt-5.1 Vision)")
    print("  - 💰 Expense Analysis (o3 Reasoning Model)")
    print("\nRequired environment variables:")
    print("  - AOI_ENDPOINT_SWDN: Azure OpenAI endpoint URL")
    print("  - AOI_KEY_SWDN: Azure OpenAI API key")
    print("  - AVIATIONSTACK_KEY: AviationStack API key (for flight tracking)")
    print("\nDeployments (hardcoded):")
    print("  - gpt-5.1: Chat and vision tasks")
    print("  - o3: Reasoning tasks (expense analysis)")
    print("\n" + "=" * 60 + "\n")

    uvicorn.run(app, host=SERVER_HOST, port=SERVER_PORT)
