import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";
import { NextRequest } from "next/server";

// Use empty adapter since we're using a single AG-UI agent
const serviceAdapter = new ExperimentalEmptyAdapter();

// Create the CopilotRuntime with HttpAgent pointing to the AG-UI backend
const runtime = new CopilotRuntime({
  agents: {
    // Connect to TechMart AgentHost AG-UI endpoint
    "operations-coordinator": new HttpAgent({ 
      url: "http://localhost:5100/agui" 
    }),
  },
});

// Handle CopilotKit runtime requests
export const POST = async (req: NextRequest) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter,
    endpoint: "/api/copilotkit",
  });

  return handleRequest(req);
};
