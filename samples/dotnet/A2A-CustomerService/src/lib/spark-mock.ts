// Mock implementation of spark global
declare global {
  var spark: {
    llmPrompt: (strings: TemplateStringsArray, ...values: any[]) => string
    llm: (prompt: string, model: string) => Promise<string>
  }
}

// Create a simple mock implementation
(window as any).spark = {
  llmPrompt: (strings: TemplateStringsArray, ...values: any[]) => {
    // Simple string interpolation for the template literal
    let result = strings[0]
    for (let i = 0; i < values.length; i++) {
      result += String(values[i]) + strings[i + 1]
    }
    return result
  },
  llm: async (prompt: string, model: string) => {
    // Mock LLM response - in a real implementation, this would call an actual LLM
    console.log('Mock LLM called with prompt:', prompt, 'model:', model)
    
    // Simple rule-based responses for demo purposes
    if (prompt.includes('billing') || prompt.includes('payment') || prompt.includes('charge') || prompt.includes('refund')) {
      if (prompt.includes('technical') || prompt.includes('login') || prompt.includes('bug')) {
        return 'billing,technical'
      }
      return 'billing'
    }
    
    if (prompt.includes('technical') || prompt.includes('login') || prompt.includes('bug') || prompt.includes('error')) {
      return 'technical'
    }
    
    // Default response
    return 'front-desk'
  }
}

export {}
