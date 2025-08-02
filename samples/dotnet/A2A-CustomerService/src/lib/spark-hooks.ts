// Mock implementation of @github/spark/hooks
import { useState, useCallback } from 'react'

export function useKV<T>(key: string, defaultValue: T) {
  const [value, setValue] = useState<T>(() => {
    try {
      const stored = localStorage.getItem(key)
      return stored ? JSON.parse(stored) : defaultValue
    } catch {
      return defaultValue
    }
  })

  const updateValue = useCallback((newValue: T | ((prev: T) => T)) => {
    setValue(prev => {
      const next = typeof newValue === 'function' ? (newValue as (prev: T) => T)(prev) : newValue
      try {
        localStorage.setItem(key, JSON.stringify(next))
      } catch (error) {
        console.warn('Failed to save to localStorage:', error)
      }
      return next
    })
  }, [key])

  return [value, updateValue] as const
}
