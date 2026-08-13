import { Html5QrcodeScannerState } from 'html5-qrcode'

export const getBarcodeScanBox = (viewfinderWidth, viewfinderHeight) => {
  const width = Math.floor(Math.min(viewfinderWidth * 0.92, 460))
  const height = Math.floor(Math.min(viewfinderHeight * 0.32, 180))

  return {
    width,
    height: Math.max(height, 120)
  }
}

export const stopScanner = async (scanner) => {
  const state = scanner.getState()

  if (
    state === Html5QrcodeScannerState.SCANNING ||
    state === Html5QrcodeScannerState.PAUSED
  ) {
    await scanner.stop()
  }

  scanner.clear()
}

export const getCameraErrorMessage = (error) => {
  const message = String(error).toLowerCase()

  if (message.includes('notallowederror') || message.includes('permission')) {
    return 'Camera access was denied. Allow camera access in your browser settings or enter the barcode manually.'
  }

  if (message.includes('notfounderror') || message.includes('devicesnotfounderror')) {
    return 'No camera was found on this device. Enter the barcode or ingredient list manually.'
  }

  if (message.includes('notreadableerror') || message.includes('could not start video')) {
    return 'The camera is unavailable or already in use by another app. Close the other app and try again.'
  }

  if (message.includes('overconstrainederror')) {
    return 'The camera cannot use the requested settings. Try again or enter the barcode manually.'
  }

  return 'The scanner could not start. Check your camera and browser permissions, then try again.'
}
