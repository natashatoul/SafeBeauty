const SUPPORTED_BARCODE_LENGTHS = new Set([8, 12, 13])

const hasValidGtinCheckDigit = (barcode) => {
  const checkDigit = Number(barcode.at(-1))
  const payload = barcode.slice(0, -1)
  let sum = 0

  for (let index = payload.length - 1, position = 0; index >= 0; index--, position++) {
    sum += Number(payload[index]) * (position % 2 === 0 ? 3 : 1)
  }

  return (10 - (sum % 10)) % 10 === checkDigit
}

const expandUpcE = (barcode) => {
  const numberSystem = barcode[0]
  if (numberSystem !== '0' && numberSystem !== '1') return null

  const data = barcode.slice(1, 7)
  const checkDigit = barcode[7]
  const lastDataDigit = data[5]
  let upcAPayload

  if (['0', '1', '2'].includes(lastDataDigit)) {
    upcAPayload = `${numberSystem}${data.slice(0, 2)}${lastDataDigit}0000${data.slice(2, 5)}`
  } else if (lastDataDigit === '3') {
    upcAPayload = `${numberSystem}${data.slice(0, 3)}00000${data.slice(3, 5)}`
  } else if (lastDataDigit === '4') {
    upcAPayload = `${numberSystem}${data.slice(0, 4)}00000${data[4]}`
  } else {
    upcAPayload = `${numberSystem}${data.slice(0, 5)}0000${lastDataDigit}`
  }

  return `${upcAPayload}${checkDigit}`
}

export const getBarcodeValidationError = (barcode) => {
  if (!/^\d+$/.test(barcode)) {
    return 'Barcode must contain digits only.'
  }

  if (!SUPPORTED_BARCODE_LENGTHS.has(barcode.length)) {
    return 'Enter an EAN-8, UPC-A, UPC-E, or EAN-13 barcode (8, 12, or 13 digits).'
  }

  if (barcode.length === 8) {
    const expandedUpcE = expandUpcE(barcode)
    const isValidEightDigitCode = hasValidGtinCheckDigit(barcode)
      || (expandedUpcE !== null && hasValidGtinCheckDigit(expandedUpcE))

    return isValidEightDigitCode ? null : 'The barcode check digit is invalid.'
  }

  return hasValidGtinCheckDigit(barcode) ? null : 'The barcode check digit is invalid.'
}
