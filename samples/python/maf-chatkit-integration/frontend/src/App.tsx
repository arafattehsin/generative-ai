// Copyright (c) Microsoft. All rights reserved.

import { ChatKit, useChatKit } from "@openai/chatkit-react";

/**
 * SwiftRover - AI Travel Assistant
 * 
 * This demo showcases:
 * - Real-time flight tracking using AviationStack API
 * - Parking sign analysis using GPT-4o vision
 * - Expense analysis using o3 reasoning model
 * - Interactive widgets with ChatKit
 * - Image upload for multimodal analysis
 */

const CHATKIT_API_URL = "/chatkit";
const CHATKIT_API_DOMAIN_KEY = 
  import.meta.env.VITE_CHATKIT_API_DOMAIN_KEY ?? "domain_pk_localhost_dev";

export default function App() {
  const chatkit = useChatKit({
    api: {
      url: CHATKIT_API_URL,
      domainKey: CHATKIT_API_DOMAIN_KEY,
      uploadStrategy: { type: "two_phase" },
    },
    startScreen: {
      greeting: "Hey there! I'm SwiftRover, your AI travel assistant. I can track flights in real-time, analyse parking signs from photos, or dive deep into your expense reports with reasoning. What can I help you with?",
      prompts: [
        { label: "✈️ Check Flight QF1", prompt: "What's the status of flight QF1?" },
        { label: "🛫 Sydney to Melbourne", prompt: "Show me flights from Sydney to Melbourne" },
        { label: "🅿️ Analyse Parking Sign", prompt: "I'll upload a parking sign photo for you to analyse" },
        { label: "💰 Analyse Q4 Expenses", prompt: "Analyse my Q4 expenses" },
      ],
    },
    composer: {
      placeholder: "Ask about a flight, upload a parking sign, or analyse expenses...",
      attachments: {
        enabled: true,
        accept: { "image/*": [".png", ".jpg", ".jpeg", ".gif", ".webp"] },
      },
    },
  });

  return <ChatKit control={chatkit.control} style={{ height: "100%" }} />;
}
