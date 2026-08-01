const SUPPORTED_BARCODE_LENGTHS = new Set([8, 12, 13])

// if barcode is correct by GTIN standart (last number is a control number)
// 3600551195377 (10 - (93 % 10)) % 10 93 % 10 // 3 10 - 3 // 7 7 % 10 // 7
const hasValidGtinCheckDigit = (barcode) => {
  const checkDigit = Number(barcode.at(-1))
  const payload = barcode.slice(0, -1)
  let sum = 0

  for (let index = payload.length - 1, position = 0; index >= 0; index--, position++) {
    sum += Number(payload[index]) * (position % 2 === 0 ? 3 : 1)
    // const digit = Number(payload[index])
    //   let multiplier

    //   if (position % 2 === 0) {
    //     multiplier = 3
    //   } else {
    //     multiplier = 1
    //   }
    //sum = sum + digit * multiplier
    // условие ? значениеЕслиДа : значениеЕслиНет - тернарный оператор.

    
  }

  return (10 - (sum % 10)) % 10 === checkDigit

  // const isAdult = (age) => {
    //  const result = age >= 18
    //  return result
    // }
}

// this function make from american UPC-E standart (8 digits) UPC-A standart (12 digits)
// '0' hide incide UPC-E: 04210005 -> UPC-A: 042000001005
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


// export -> use this function in the different files of a project
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
