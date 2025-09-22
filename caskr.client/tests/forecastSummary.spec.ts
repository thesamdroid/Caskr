import { test, expect } from '@playwright/test'
import { formatForecastSummary } from '../src/utils/forecastSummary'

test('formats forecast summary with age information', () => {
  const message = formatForecastSummary('2025-06-01', 5, 12)
  expect(message).toContain('Barrels aged 5 years available')
  expect(message).toContain('12')
})

test('omits age phrase when no age is provided', () => {
  const message = formatForecastSummary('2025-06-01', 0, 4)
  const formattedDate = new Date('2025-06-01').toLocaleDateString()
  expect(message).toBe(`Barrels available on ${formattedDate}: 4`)
})
