"use client";

import { CopilotSidebar } from "@copilotkit/react-ui";
import { useCopilotAction, useCopilotReadable } from "@copilotkit/react-core";
import { useState, useEffect } from "react";

// Agent configuration
const agents = [
  {
    id: "operations-coordinator",
    name: "Operations Coordinator",
    description: "Main orchestrator for enterprise retail operations",
    icon: "🎯",
    color: "from-techmart-500 to-techmart-700",
  },
  {
    id: "order-specialist",
    name: "Order Specialist",
    description: "Complex order operations and fulfillment",
    icon: "📦",
    color: "from-amber-500 to-orange-600",
  },
  {
    id: "account-manager",
    name: "Account Manager",
    description: "B2B customer relationships and accounts",
    icon: "🤝",
    color: "from-emerald-500 to-teal-600",
  },
];

// Sample dashboard data
const dashboardStats = [
  { label: "Active Orders", value: "1,247", change: "+12%", trend: "up" },
  { label: "Pending Approvals", value: "23", change: "-5%", trend: "down" },
  { label: "Inventory Alerts", value: "8", change: "+2", trend: "up" },
  { label: "Customer Inquiries", value: "156", change: "-18%", trend: "down" },
];

const recentOrders = [
  { id: "ORD-2024-001", customer: "Contoso Corp", status: "Processing", amount: "$3,799.98" },
  { id: "ORD-2024-002", customer: "Fabrikam Inc", status: "Shipped", amount: "$27,499.50" },
  { id: "ORD-2024-003", customer: "Northwind Traders", status: "Pending Approval", amount: "$15,999.00" },
  { id: "ORD-2024-004", customer: "Adventure Works", status: "Delivered", amount: "$4,999.00" },
];

export default function Home() {
  const [selectedAgent, setSelectedAgent] = useState(agents[0]);
  const [isDarkMode, setIsDarkMode] = useState(false);
  const [highlightedOrderId, setHighlightedOrderId] = useState<string | null>(null);
  const [notification, setNotification] = useState<{ message: string; type: 'success' | 'warning' | 'error' | 'info' } | null>(null);
  const [dashboardData, setDashboardData] = useState(dashboardStats);

  // Apply dark mode to document
  useEffect(() => {
    if (isDarkMode) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }, [isDarkMode]);

  // Auto-hide notification after 5 seconds
  useEffect(() => {
    if (notification) {
      const timer = setTimeout(() => setNotification(null), 5000);
      return () => clearTimeout(timer);
    }
  }, [notification]);

  // ═══════════════════════════════════════════════════════════════════
  // AG-UI Frontend Actions - These can be triggered by the AI agent!
  // ═══════════════════════════════════════════════════════════════════

  // Action: Toggle dark/light mode
  useCopilotAction({
    name: "toggleTheme",
    description: "Switch between dark and light mode. Use this when the user asks to change the theme, switch to dark mode, or make the UI lighter/darker.",
    available: "remote",  // Makes action available to the backend AG-UI agent
    parameters: [
      {
        name: "mode",
        type: "string",
        description: "The theme mode to set: 'dark', 'light', or 'toggle'",
        required: true,
      },
    ],
    handler: async ({ mode }) => {
      if (mode === 'toggle') {
        setIsDarkMode(prev => !prev);
        return `Theme toggled to ${!isDarkMode ? 'dark' : 'light'} mode`;
      } else if (mode === 'dark') {
        setIsDarkMode(true);
        return "Switched to dark mode 🌙";
      } else {
        setIsDarkMode(false);
        return "Switched to light mode ☀️";
      }
    },
  });

  // Action: Highlight an order in the dashboard
  useCopilotAction({
    name: "highlightOrder",
    description: "Highlight a specific order in the dashboard to draw attention to it. Use when discussing a specific order.",
    available: "remote",  // Makes action available to the backend AG-UI agent
    parameters: [
      {
        name: "orderId",
        type: "string",
        description: "The order ID to highlight (e.g., ORD-2024-001)",
        required: true,
      },
    ],
    handler: async ({ orderId }) => {
      setHighlightedOrderId(orderId);
      // Auto-remove highlight after 10 seconds
      setTimeout(() => setHighlightedOrderId(null), 10000);
      return `Order ${orderId} is now highlighted in the dashboard`;
    },
  });

  // Action: Show a notification toast
  useCopilotAction({
    name: "showNotification",
    description: "Display a notification message to the user. Use for alerts, confirmations, or important updates.",
    available: "remote",  // Makes action available to the backend AG-UI agent
    parameters: [
      {
        name: "message",
        type: "string",
        description: "The notification message to display",
        required: true,
      },
      {
        name: "type",
        type: "string",
        description: "The notification type: 'success', 'warning', or 'info'",
        required: false,
      },
    ],
    handler: async ({ message, type = 'info' }) => {
      setNotification({ message, type: type as 'success' | 'warning' | 'info' });
      return `Notification displayed: ${message}`;
    },
  });

  // Action: Update dashboard statistics
  useCopilotAction({
    name: "updateDashboardStat",
    description: "Update a specific statistic on the dashboard. Use when reporting new data or changes.",
    available: "remote",  // Makes action available to the backend AG-UI agent
    parameters: [
      {
        name: "statLabel",
        type: "string",
        description: "The stat to update: 'Active Orders', 'Pending Approvals', 'Inventory Alerts', or 'Customer Inquiries'",
        required: true,
      },
      {
        name: "newValue",
        type: "string",
        description: "The new value to display",
        required: true,
      },
      {
        name: "change",
        type: "string",
        description: "The change indicator (e.g., '+5%', '-10')",
        required: false,
      },
    ],
    handler: async ({ statLabel, newValue, change }) => {
      setDashboardData(prev => prev.map(stat => 
        stat.label === statLabel 
          ? { ...stat, value: newValue, change: change || stat.change }
          : stat
      ));
      return `Updated ${statLabel} to ${newValue}`;
    },
  });

  // Provide readable context to the AI about current UI state
  useCopilotReadable({
    description: "Current UI state including theme and highlighted elements",
    value: {
      currentTheme: isDarkMode ? 'dark' : 'light',
      highlightedOrder: highlightedOrderId,
      activeNotification: notification?.message,
      selectedAgent: selectedAgent.name,
    },
  });

  return (
    <div className={`min-h-screen transition-colors duration-500 ${isDarkMode ? 'dark bg-gradient-to-br from-slate-900 via-slate-800 to-techmart-950' : 'bg-gradient-to-br from-slate-50 via-white to-techmart-50'}`}>
      {/* Notification Toast - AG-UI triggered */}
      {notification && (
        <div className={`fixed top-4 right-4 z-50 max-w-sm p-4 rounded-xl shadow-2xl border animate-slide-in ${
          notification.type === 'success' ? 'bg-emerald-50 dark:bg-emerald-900/90 border-emerald-200 dark:border-emerald-700' :
          notification.type === 'warning' ? 'bg-amber-50 dark:bg-amber-900/90 border-amber-200 dark:border-amber-700' :
          notification.type === 'error' ? 'bg-rose-50 dark:bg-rose-900/90 border-rose-200 dark:border-rose-700' :
          'bg-blue-50 dark:bg-blue-900/90 border-blue-200 dark:border-blue-700'
        }`}>
          <div className="flex items-start gap-3">
            <span className="text-xl">
              {notification.type === 'success' ? '✅' : 
               notification.type === 'warning' ? '⚠️' : 
               notification.type === 'error' ? '❌' : '💡'}
            </span>
            <div className="flex-1">
              <p className={`font-medium text-sm ${
                notification.type === 'success' ? 'text-emerald-800 dark:text-emerald-200' :
                notification.type === 'warning' ? 'text-amber-800 dark:text-amber-200' :
                notification.type === 'error' ? 'text-rose-800 dark:text-rose-200' :
                'text-blue-800 dark:text-blue-200'
              }`}>
                {notification.message}
              </p>
            </div>
            <button 
              onClick={() => setNotification(null)}
              className="text-slate-400 hover:text-slate-600 dark:text-slate-500 dark:hover:text-slate-300"
            >
              ✕
            </button>
          </div>
        </div>
      )}

      {/* Header */}
      <header className="sticky top-0 z-40 border-b border-slate-200/60 dark:border-slate-700/60 bg-white/80 dark:bg-slate-900/80 backdrop-blur-xl">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            {/* Logo */}
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-techmart-500 to-techmart-700 flex items-center justify-center shadow-lg shadow-techmart-500/25">
                <svg className="w-6 h-6 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
              </div>
              <div>
                <h1 className="text-xl font-bold bg-gradient-to-r from-techmart-700 to-techmart-500 bg-clip-text text-transparent">
                  TechMart
                </h1>
                <p className="text-xs text-slate-500 dark:text-slate-400 -mt-0.5">Enterprise Portal</p>
              </div>
            </div>

            {/* Agent Selector & Controls */}
            <div className="flex items-center gap-4">
              {/* Theme Toggle - AG-UI can trigger this */}
              <button
                onClick={() => setIsDarkMode(!isDarkMode)}
                className="p-2 rounded-full bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
                title={isDarkMode ? 'Switch to Light Mode' : 'Switch to Dark Mode'}
              >
                {isDarkMode ? '☀️' : '🌙'}
              </button>

              <div className="flex items-center gap-2 px-4 py-2 rounded-full bg-slate-100 dark:bg-slate-800">
                <span className="text-xl">{selectedAgent.icon}</span>
                <label htmlFor="agent-selector" className="sr-only">Select Agent</label>
                <select
                  id="agent-selector"
                  value={selectedAgent.id}
                  onChange={(e) => setSelectedAgent(agents.find(a => a.id === e.target.value) || agents[0])}
                  className="bg-transparent text-sm font-medium text-slate-700 dark:text-slate-200 focus:outline-none cursor-pointer"
                >
                  {agents.map((agent) => (
                    <option key={agent.id} value={agent.id}>
                      {agent.name}
                    </option>
                  ))}
                </select>
              </div>
              
              {/* Status indicator */}
              <div className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-emerald-50 dark:bg-emerald-900/30 border border-emerald-200 dark:border-emerald-800">
                <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                <span className="text-xs font-medium text-emerald-700 dark:text-emerald-400">AI Online</span>
              </div>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        {/* Welcome Section */}
        <div className="mb-8">
          <h2 className="text-3xl font-bold text-slate-900 dark:text-white mb-2">
            Good afternoon! 👋
          </h2>
          <p className="text-slate-600 dark:text-slate-400">
            Your AI assistant is ready to help with enterprise operations. Try asking about orders, inventory, or customer accounts.
          </p>
        </div>

        {/* Stats Grid - Uses AG-UI controlled dashboardData */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          {dashboardData.map((stat, index) => (
            <div
              key={index}
              className="group relative overflow-hidden rounded-2xl bg-white dark:bg-slate-800/50 border border-slate-200/60 dark:border-slate-700/60 p-6 hover:shadow-xl hover:shadow-slate-200/40 dark:hover:shadow-slate-900/40 transition-all duration-300"
            >
              <div className="absolute inset-0 bg-gradient-to-br from-techmart-500/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />
              <div className="relative">
                <p className="text-sm font-medium text-slate-500 dark:text-slate-400 mb-1">{stat.label}</p>
                <p className="text-3xl font-bold text-slate-900 dark:text-white mb-2">{stat.value}</p>
                <div className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${
                  stat.trend === "up" 
                    ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400" 
                    : "bg-rose-50 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400"
                }`}>
                  {stat.trend === "up" ? "↑" : "↓"} {stat.change}
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Content Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Recent Orders */}
          <div className="lg:col-span-2 rounded-2xl bg-white dark:bg-slate-800/50 border border-slate-200/60 dark:border-slate-700/60 overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200/60 dark:border-slate-700/60">
              <h3 className="font-semibold text-slate-900 dark:text-white">Recent Orders</h3>
            </div>
            <div className="divide-y divide-slate-100 dark:divide-slate-700/50">
              {recentOrders.map((order) => (
                <div 
                  key={order.id} 
                  className={`px-6 py-4 flex items-center justify-between transition-all duration-500 ${
                    highlightedOrderId === order.id 
                      ? 'bg-techmart-100 dark:bg-techmart-900/50 ring-2 ring-techmart-500 ring-inset animate-pulse' 
                      : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'
                  }`}
                >
                  <div className="flex items-center gap-4">
                    <div className={`w-10 h-10 rounded-xl flex items-center justify-center transition-colors ${
                      highlightedOrderId === order.id 
                        ? 'bg-techmart-500 text-white' 
                        : 'bg-techmart-100 dark:bg-techmart-900/30'
                    }`}>
                      <span className={`font-mono text-xs ${highlightedOrderId === order.id ? 'text-white' : 'text-techmart-600 dark:text-techmart-400'}`}>{order.id.slice(-3)}</span>
                    </div>
                    <div>
                      <p className="font-medium text-slate-900 dark:text-white">{order.customer}</p>
                      <p className="text-sm text-slate-500 dark:text-slate-400">{order.id}</p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold text-slate-900 dark:text-white">{order.amount}</p>
                    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${
                      order.status === "Delivered" ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400" :
                      order.status === "Shipped" ? "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400" :
                      order.status === "Processing" ? "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400" :
                      "bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300"
                    }`}>
                      {order.status}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Quick Actions */}
          <div className="rounded-2xl bg-white dark:bg-slate-800/50 border border-slate-200/60 dark:border-slate-700/60 p-6">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-4">Quick Actions</h3>
            <div className="space-y-3">
              <button className="w-full flex items-center gap-3 px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-700/50 hover:bg-techmart-50 dark:hover:bg-techmart-900/20 border border-slate-200/60 dark:border-slate-600/60 transition-colors group">
                <span className="text-xl">📋</span>
                <span className="text-sm font-medium text-slate-700 dark:text-slate-200 group-hover:text-techmart-700 dark:group-hover:text-techmart-400">Check Pending Orders</span>
              </button>
              <button className="w-full flex items-center gap-3 px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-700/50 hover:bg-techmart-50 dark:hover:bg-techmart-900/20 border border-slate-200/60 dark:border-slate-600/60 transition-colors group">
                <span className="text-xl">📦</span>
                <span className="text-sm font-medium text-slate-700 dark:text-slate-200 group-hover:text-techmart-700 dark:group-hover:text-techmart-400">Inventory Status</span>
              </button>
              <button className="w-full flex items-center gap-3 px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-700/50 hover:bg-techmart-50 dark:hover:bg-techmart-900/20 border border-slate-200/60 dark:border-slate-600/60 transition-colors group">
                <span className="text-xl">👥</span>
                <span className="text-sm font-medium text-slate-700 dark:text-slate-200 group-hover:text-techmart-700 dark:group-hover:text-techmart-400">Customer Accounts</span>
              </button>
              <button className="w-full flex items-center gap-3 px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-700/50 hover:bg-techmart-50 dark:hover:bg-techmart-900/20 border border-slate-200/60 dark:border-slate-600/60 transition-colors group">
                <span className="text-xl">🔍</span>
                <span className="text-sm font-medium text-slate-700 dark:text-slate-200 group-hover:text-techmart-700 dark:group-hover:text-techmart-400">Web Search</span>
              </button>
            </div>

            {/* Agent Info Card */}
            <div className={`mt-6 p-4 rounded-xl bg-gradient-to-br ${selectedAgent.color} text-white`}>
              <div className="flex items-center gap-3 mb-2">
                <span className="text-2xl">{selectedAgent.icon}</span>
                <h4 className="font-semibold">{selectedAgent.name}</h4>
              </div>
              <p className="text-sm text-white/80">{selectedAgent.description}</p>
            </div>
          </div>
        </div>

        {/* AI Suggestions - AG-UI Feature Showcase */}
        <div className="mt-8 rounded-2xl bg-gradient-to-r from-techmart-600 to-techmart-800 p-6 text-white">
          <div className="flex items-start gap-4">
            <div className="w-12 h-12 rounded-xl bg-white/10 flex items-center justify-center flex-shrink-0">
              <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold text-lg mb-1">🎯 AG-UI Powered AI Assistant</h3>
              <p className="text-white/80 text-sm mb-4">
                This demo showcases AG-UI (Agent User Interaction Protocol) - the AI can control the UI! 
                Try these commands to see the agent manipulate the frontend in real-time:
              </p>
              <div className="flex flex-wrap gap-2">
                <span className="px-3 py-1 rounded-full bg-white/20 text-xs font-medium border border-white/30">
                  🌙 &quot;Switch to dark mode&quot;
                </span>
                <span className="px-3 py-1 rounded-full bg-white/20 text-xs font-medium border border-white/30">
                  ☀️ &quot;Turn on light theme&quot;
                </span>
                <span className="px-3 py-1 rounded-full bg-white/20 text-xs font-medium border border-white/30">
                  🔦 &quot;Highlight order ORD-2024-001&quot;
                </span>
                <span className="px-3 py-1 rounded-full bg-white/20 text-xs font-medium border border-white/30">
                  📊 &quot;Update total orders to 500&quot;
                </span>
                <span className="px-3 py-1 rounded-full bg-white/20 text-xs font-medium border border-white/30">
                  🔔 &quot;Show a success notification&quot;
                </span>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* CopilotKit Sidebar */}
      <CopilotSidebar
        labels={{
          title: "TechMart AI",
          initial: `Hi! I'm your ${selectedAgent.name}. How can I help you today?`,
          placeholder: "Ask about orders, inventory, or accounts...",
        }}
        defaultOpen={false}
        clickOutsideToClose={true}
      />
    </div>
  );
}
