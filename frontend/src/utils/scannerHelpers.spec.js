import { describe, it, expect } from 'vitest'
import { getBarcodeScanBox, getCameraErrorMessage } from './scannerHelpers.js'

describe('getBarcodeScanBox', () => {
  it('caps the box size for a large viewfinder', () => {
    expect(getBarcodeScanBox(1000, 1000)).toEqual({ width: 460, height: 180 })
  })

  it('keeps a minimum height for a small viewfinder', () => {
    expect(getBarcodeScanBox(200, 100)).toEqual({ width: 184, height: 120 })
  })
})

describe('getCameraErrorMessage', () => {
  it('explains a permission error', () => {
    expect(getCameraErrorMessage(new Error('NotAllowedError'))).toBe(
      'Camera access was denied. Allow camera access in your browser settings or enter the barcode manually.'
    )
  })

  it('falls back to a generic message for an unknown error', () => {
    expect(getCameraErrorMessage(new Error('something odd'))).toBe(
      'The scanner could not start. Check your camera and browser permissions, then try again.'
    )
  })
})
