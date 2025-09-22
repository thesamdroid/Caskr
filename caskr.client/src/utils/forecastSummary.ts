export function formatForecastSummary(
  date: string | null | undefined,
  ageYears: number | null | undefined,
  count: number
): string {
  if (!date) {
    return ''
  }

  const formattedDate = new Date(date).toLocaleDateString()
  const ageText = ageYears && ageYears > 0
    ? `aged ${ageYears} ${ageYears === 1 ? 'year' : 'years'} `
    : ''

  return `Barrels ${ageText}available on ${formattedDate}: ${count}`
}
