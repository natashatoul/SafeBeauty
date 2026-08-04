import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Html5Qrcode,
  Html5QrcodeScannerState,
  Html5QrcodeSupportedFormats
} from 'html5-qrcode'
import axios from 'axios'
import { getBarcodeValidationError } from '../utils/barcodeValidation'
import { useProfile } from '../context/ProfileContext'
import { saveAnalysis } from '../services/scanHistoryService'


// Base URL for the backend API, kept in one place so it only needs
// to change in a single spot (e.g. when deploying to Azure later).
const API_URL = import.meta.env.VITE_API_URL || '/api'
const EMPTY_CONDITIONS = []
const BARCODE_REVIEW_COVERAGE_THRESHOLD = 80

const BARCODE_FORMATS = [
  Html5QrcodeSupportedFormats.EAN_13,
  Html5QrcodeSupportedFormats.EAN_8,
  Html5QrcodeSupportedFormats.UPC_A,
  Html5QrcodeSupportedFormats.UPC_E
]

const getBarcodeScanBox = (viewfinderWidth, viewfinderHeight) => {
  const width = Math.floor(Math.min(viewfinderWidth * 0.92, 460))
  const height = Math.floor(Math.min(viewfinderHeight * 0.32, 180))

  return {
    width,
    height: Math.max(height, 120)
  }
}

const stopScanner = async (scanner) => {
  const state = scanner.getState()

  if (
    state === Html5QrcodeScannerState.SCANNING ||
    state === Html5QrcodeScannerState.PAUSED
  ) {
    await scanner.stop()
  }

  scanner.clear()
}

const getCameraErrorMessage = (error) => {
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

function ScanPage() {
  const [manualBarcode, setManualBarcode] = useState('')
  const [scannerStatus, setScannerStatus] = useState('starting')
  const [scannerError, setScannerError] = useState('')
  const [barcodeError, setBarcodeError] = useState('')
  const [showScanHelp, setShowScanHelp] = useState(false)
  const [scannerAttempt, setScannerAttempt] = useState(0)
  const [isProcessing, setIsProcessing] = useState(false)
  const scannerRef = useRef(null)
  const isProcessingRef = useRef(false)
  const scanHelpTimeoutRef = useRef(null)

  const navigate = useNavigate()
  const { profile } = useProfile()
  const selectedConditions = profile.conditions ?? EMPTY_CONDITIONS

  const analyseBarcode = useCallback(async (barcode) => {
    try {
      const response = await axios.post(
        `${API_URL}/products/barcode/${encodeURIComponent(barcode)}`,
        {
          userConditions: selectedConditions,
          ageGroup: profile.ageGroup,
          gender: profile.gender
        }
      )

      const results = response.data.analysis
      const sourceIngredients = response.data.sourceIngredients ?? []
      const analysisContext = {
        source: 'Open Beauty Facts',
        productName: response.data.productName,
        barcode: response.data.barcode,
        sourceIngredients,
        selectedConditions,
        ageGroup: profile.ageGroup,
        gender: profile.gender,
        skinType: profile.skinType,
        hairCondition: profile.hairCondition
      }
      const knownCount = results.results?.length ?? 0
      const unknownCount = results.unknownIngredients?.length ?? 0
      const totalCount = knownCount + unknownCount
      const coverage = totalCount > 0 ? Math.round((knownCount / totalCount) * 100) : 0

      if (
        sourceIngredients.length > 0 &&
        totalCount > 0 &&
        coverage < BARCODE_REVIEW_COVERAGE_THRESHOLD
      ) {
        navigate('/manual', {
          state: {
            initialIngredients: sourceIngredients,
            draftSource: 'Open Beauty Facts',
            productName: response.data.productName,
            barcode: response.data.barcode,
            lowCoverageReview: {
              coverage,
              knownCount,
              unknownCount,
              totalCount
            },
            originalBarcodeResult: {
              results,
              analysisContext
            }
          }
        })
        return null
      }

      const savedAnalysis = await saveAnalysis({ results, analysisContext })


      navigate('/results', {
        state: {
          results,
          analysisContext: savedAnalysis?.analysisContext ?? analysisContext
        }
      })
      return null
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        alert('Product not found. Try entering ingredients manually.')
        navigate('/manual')
        return null
      }

      return 'The product service is temporarily unavailable. Please try again.'
    }
  }, [navigate, profile.ageGroup, profile.gender, selectedConditions])


  const processBarcode = useCallback(async (barcode) => {
    const validationError = getBarcodeValidationError(barcode)
    if (validationError) {
      setBarcodeError(validationError)
      return
    }

    if (isProcessingRef.current) return
    isProcessingRef.current = true
    setIsProcessing(true)
    setBarcodeError('')
    setScannerError('')
    setScannerStatus('processing')
    clearTimeout(scanHelpTimeoutRef.current)

    try {
      if (scannerRef.current) {
        await stopScanner(scannerRef.current)
      }

      const requestError = await analyseBarcode(barcode)
      if (requestError) {
        setScannerError(requestError)
        setScannerStatus('error')
      }
    } catch (error) {
      console.error('Failed to process barcode:', error)
      setScannerError('The barcode could not be processed. Please try again.')
      setScannerStatus('error')
    } finally {
      isProcessingRef.current = false
      setIsProcessing(false)
    }
  }, [analyseBarcode])

  const retryScanner = () => {
    if (isProcessingRef.current) return
    setScannerError('')
    setBarcodeError('')
    setShowScanHelp(false)
    setScannerAttempt((attempt) => attempt + 1)
  }

  const handleManualSubmit = async (event) => {
    event.preventDefault()

    const barcode = manualBarcode.trim()
    if (barcode === '') return

    await processBarcode(barcode)
  }

  useEffect(() => {
    let disposed = false
    let startSettled = false
    setScannerStatus('starting')
    setScannerError('')

    const html5Qrcode = new Html5Qrcode('reader', {
      formatsToSupport: BARCODE_FORMATS,
      experimentalFeatures: {
        useBarCodeDetectorIfSupported: true
      }
    })
    scannerRef.current = html5Qrcode

    scanHelpTimeoutRef.current = setTimeout(() => {
      if (!disposed && !isProcessingRef.current) setShowScanHelp(true)
    }, 30000)

    const startPromise = html5Qrcode.start(
      { facingMode: 'environment' },
      {
        fps: 15,
        qrbox: getBarcodeScanBox,
        videoConstraints: {
          facingMode: 'environment',
          width: { ideal: 1920 },
          height: { ideal: 1080 }
        }
      },
      (barcode) => processBarcode(barcode)
    )

    startPromise
      .then(() => {
        startSettled = true
        if (!disposed) setScannerStatus('active')
      })
      .catch((error) => {
        startSettled = true
        if (disposed) return
        console.error('Failed to start scanner:', error)
        setScannerError(getCameraErrorMessage(error))
        setScannerStatus('error')
        setShowScanHelp(true)
      })

    return () => {
      disposed = true
      clearTimeout(scanHelpTimeoutRef.current)
      if (scannerRef.current === html5Qrcode) scannerRef.current = null

      // start() cannot be cancelled while the permission prompt is open.
      // Waiting for it to settle guarantees that a late camera stream is closed.
      const state = html5Qrcode.getState()
      if (
        state === Html5QrcodeScannerState.SCANNING ||
        state === Html5QrcodeScannerState.PAUSED
      ) {
        stopScanner(html5Qrcode).catch(() => { })
      } else {
        try {
          html5Qrcode.clear()
        } catch {
          // The reader may already have been cleared after a successful scan.
        }

        if (!startSettled) {
          startPromise
            .then(() => stopScanner(html5Qrcode))
            .catch(() => { })
        }
      }
    }
  }, [processBarcode, scannerAttempt])

  const scannerStatusText = {
    starting: 'Starting camera…',
    active: 'Scanner active — looking for a barcode',
    processing: 'Barcode detected — checking product…',
    error: 'Scanner needs attention'
  }[scannerStatus]

  return (
    <div className="scan-page">
      <h1>Scan Barcode</h1>
      <p>Point your camera at the product barcode.</p>

      <p className={`scanner-status ${scannerStatus}`} role="status" aria-live="polite">
        {scannerStatusText}
      </p>

      <div id="reader" className="scanner-reader" />

      {scannerError && (
        <div className="scanner-message error" role="alert">
          <p>{scannerError}</p>
          <button type="button" className="primary-button" onClick={retryScanner} disabled={isProcessing}>
            Try camera again
          </button>
        </div>
      )}

      <div className={`scan-guidance${showScanHelp ? ' highlighted' : ''}`}>
        <strong>{showScanHelp ? 'Still not scanning?' : 'For a faster scan'}</strong>
        <ul>
          <li>Hold the barcode horizontally inside the frame.</li>
          <li>Move the camera slowly closer or farther away.</li>
          <li>Avoid glare and keep the full barcode visible.</li>
        </ul>
        {showScanHelp && <p>You can enter the barcode below or use “Enter list” in the menu.</p>}
      </div>

      <form className="barcode-form" onSubmit={handleManualSubmit} noValidate>
        <label htmlFor="manual-barcode">Enter barcode manually</label>
        <input
          id="manual-barcode"
          type="text"
          inputMode="numeric"
          autoComplete="off"
          value={manualBarcode}
          onChange={(event) => {
            setManualBarcode(event.target.value)
            setBarcodeError('')
          }}
          placeholder="e.g. 42345275"
          aria-invalid={barcodeError ? 'true' : 'false'}
          aria-describedby={barcodeError ? 'barcode-error' : undefined}
        />
        {barcodeError && <p id="barcode-error" className="field-error">{barcodeError}</p>}
        <button type="submit" className="primary-button" disabled={isProcessing || manualBarcode.trim() === ''}>
          {isProcessing ? 'Checking…' : 'Analyse Barcode'}
        </button>
      </form>
    </div>
  )
}

export default ScanPage
