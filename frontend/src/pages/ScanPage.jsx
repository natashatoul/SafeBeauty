import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Html5Qrcode, Html5QrcodeScannerState } from 'html5-qrcode'
import axios from 'axios'

// Base URL for the backend API, kept in one place so it only needs
// to change in a single spot (e.g. when deploying to Azure later).
const API_URL = import.meta.env.VITE_API_URL || '/api'

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

function ScanPage() {
  const [manualBarcode, setManualBarcode] = useState('')

  // HOOK: useRef(null)
  // Unlike useState, changing a ref's value does NOT trigger a re-render.
  // Think of useState as a whiteboard everyone in the room can see —
  // changing it updates what's displayed. useRef is more like a personal
  // notebook in a drawer: you can read and write to it, but nothing on
  // screen reacts when it changes.
  // Here it's used to "remember" the scanner object across renders,
  // without needing the component to redraw when it's created.
  const scannerRef = useRef(null)

  // A second ref, used purely as a flag: "has the scanner already
  // been started?" Starts as false. This exists to guard against
  // accidentally starting the camera twice.
  const isInitialized = useRef(false)

  const navigate = useNavigate()

  const analyseBarcode = useCallback(async (barcode) => {
    try {
      // Send a GET request to the backend with the scanned or typed barcode.
      // axios.get() gives back the data directly in response.data
      // (fetch would need an extra response.json() step for the same thing).
      const response = await axios.get(`${API_URL}/products/barcode/${barcode}`)

      // Same pattern as ManualInputPage: navigate to /results,
      // carrying the analysis data via route state.
      navigate('/results', { state: { results: response.data.analysis } })
    } catch {
      // If the product isn't found (or the request fails for any
      // other reason), fall back to manual ingredient entry.
      alert('Product not found. Try entering ingredients manually.')
      navigate('/manual')
    }
  }, [navigate])

  const handleManualSubmit = async (event) => {
    event.preventDefault()

    const barcode = manualBarcode.trim()
    if (barcode === '') return

    await analyseBarcode(barcode)
  }

  // HOOK: useEffect(...)
  // Runs right after the component first renders.
  // This is where the camera gets started — a side effect outside the
  // normal "get data, show data" flow of rendering.
  useEffect(() => {
    // Guard clause: if the scanner has already been started, stop here
    // and do nothing. This matters because React's StrictMode (used in
    // development only) deliberately runs every effect, cleans it up,
    // then runs it again — as a way of catching effects that don't
    // clean up properly. Without this guard, the camera could
    // theoretically be asked to start twice in quick succession.
    if (isInitialized.current) return
    isInitialized.current = true

    // Creates a new scanner instance. 'reader' is the id of the <div>
    // below — the library will inject the camera video feed directly
    // into that element, bypassing normal React rendering.
    const html5Qrcode = new Html5Qrcode('reader')

    // .start() begins the camera and barcode scanning. It takes 3 arguments:
    html5Qrcode.start(
      // 1) Camera config: use the back camera (better for scanning
      //    a barcode on a physical product than the front camera)
      { facingMode: 'user' },
      // 2) Scan config: 10 frames per second, and a wide scan box on
      //    screen showing where to line up the barcode
      { fps: 10, qrbox: { width: 300, height: 120 } },
      // 3) Callback function — runs automatically once a barcode is
      //    successfully detected. `barcode` is the decoded value (a string).
      //    Marked `async` because it needs to `await` two things below:
      //    stopping the camera, and the server request.
      async (barcode) => {
        // Stop the camera as soon as a barcode is found.
        // `await` ensures we don't move on until it's actually stopped.
        await stopScanner(html5Qrcode)

        await analyseBarcode(barcode)
      }
    ).catch((error) => {
      // If starting the camera itself fails (e.g. permission denied,
      // or the camera is already in use by another app), this catches
      // that failure. Without this, the page would just sit there
      // blank with no explanation of what went wrong.
      console.error('Failed to start scanner:', error)
    })

    // Store the scanner instance in the ref, so it can be accessed
    // later from outside this function (e.g. in the cleanup below).
    scannerRef.current = html5Qrcode

    // CLEANUP FUNCTION: the function returned from useEffect.
    // React automatically calls this when the component is about to
    // disappear from the screen (e.g. the user navigates away from
    // this page), or — in development, under StrictMode — right after
    // the first run, as part of its double-invoke check.
    return () => {
      // Stop the camera. .catch(() => {}) quietly ignores any error
      // (e.g. if it was already stopped), so a minor cleanup issue
      // doesn't crash the app.
      stopScanner(html5Qrcode).catch(() => {})

      // Reset the flag when this effect is cleaned up. This is the
      // important fix: without resetting it here, isInitialized would
      // stay `true` forever after StrictMode's first cleanup, and the
      // SECOND, real run of the effect would be blocked by the guard
      // clause above — leaving the camera never actually started.
      isInitialized.current = false
    }
  }, [analyseBarcode])

  return (
    <div>
      <h2>Scan Barcode</h2>
      <p>Point your camera at the product barcode.</p>
      {/* This div looks empty, but html5-qrcode injects the camera feed
          directly into it, matched by this id="reader" */}
      <div id="reader" style={{ width: '300px', minHeight: '300px' }} />

      <form onSubmit={handleManualSubmit}>
        <label htmlFor="manual-barcode">Enter barcode manually</label>
        <br />
        <input
          id="manual-barcode"
          type="text"
          value={manualBarcode}
          onChange={(event) => setManualBarcode(event.target.value)}
          placeholder="e.g. 42345275"
        />
        <button type="submit" disabled={manualBarcode.trim() === ''}>
          Analyse Barcode
        </button>
      </form>
    </div>
  )
}

export default ScanPage
