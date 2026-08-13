import { describe, it, expect } from 'vitest'
import { getBarcodeValidationError } from './barcodeValidation.js'

describe('getBarcodeValidationError', () => {
    it('accepts a valid EAN-13 barcode', () => {
        expect(getBarcodeValidationError('5901234123457')).toBe(null)
    })

    it('rejects an EAN-13 barcode with an invalid check digit', () => {
        expect(getBarcodeValidationError('5901234123458')).not.toBe(null)
    })

    it('rejects barcodes containing non-digit characters', () => {
        expect(getBarcodeValidationError('590123412345A')).toBe('Barcode must contain digits only.')
    })

    it('rejects barcodes of an unsupported length', () => {
        expect(getBarcodeValidationError('12345')).toBe('Enter an EAN-8, UPC-A, UPC-E, or EAN-13 barcode (8, 12, or 13 digits).')
    })

    it('accepts a valid UPC-E barcode by expanding it to UPC-A', () => {
        expect(getBarcodeValidationError('01234558')).toBe(null)
    })

    it('rejects an 8-digit barcode with an invalid check digit', () => {
        expect(getBarcodeValidationError('01234559')).not.toBe(null)
    })

})
