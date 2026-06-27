import { useNavigate } from 'react-router-dom'

function HomePage() {
  const navigate = useNavigate()

  return (
    <div>
      <h1>SafeBeauty</h1>
      <p>Analyse your cosmetic ingredients and find out if they are safe for you.</p>

      <div>
        <button onClick={() => navigate('/scan')}>Scan Barcode</button>
        <button onClick={() => navigate('/manual')}>Enter Ingredients Manually</button>
      </div>
    </div>
  )
}

export default HomePage