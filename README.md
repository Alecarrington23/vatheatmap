# VATSIM HeatTracker

Track your VATSIM flights and visualize them as a heatmap.

## Features
- **Sign in with VATSIM** (OAuth2 via VATSIM Connect)
- **Live flight tracking** — polls VATSIM data feed every 60 seconds
- **Route heatmap** — every position point you fly contributes to a persistent heatmap
- **Flight history** — callsign, route, aircraft per flight
- **Statistics** — total tracked points, flights, and top routes

## Project Structure

```
vatsim-heatmap/
├── backend/
│   ├── app.py              # Flask app — OAuth, API routes, DB
│   ├── requirements.txt
│   ├── flights.db          # Created automatically on first run
│   └── .env.example        # Copy to .env and fill in
└── frontend/
    ├── index.html          # Landing / login page
    └── dashboard.html      # Main dashboard with map
```

## Setup

### 1. Register on VATSIM Connect

1. Go to https://auth.vatsim.net/
2. Create an OAuth application
3. Set the **Redirect URI** to: `http://localhost:5000/auth/callback`
4. Note your **Client ID** and **Client Secret**

### 2. Configure Environment

```bash
cd backend
cp .env.example .env
# Edit .env with your credentials
```

### 3. Install & Run Backend

```bash
cd backend
python -m venv venv
source venv/bin/activate   # Windows: venv\Scripts\activate
pip install -r requirements.txt
python app.py
```

The server runs on **http://localhost:5000**

### 4. Open the App

Visit http://localhost:5000 in your browser and click **Sign in with VATSIM**.

## How It Works

| Step | Detail |
|------|--------|
| Login | VATSIM OAuth2 → access token stored in server session |
| Live Poll | Every 60 seconds, the dashboard calls `/api/live` |
| `/api/live` | Fetches `data.vatsim.net/v3/vatsim-data.json`, finds your CID, saves lat/lng to `flight_points` table |
| `/api/heatmap` | Aggregates all your `flight_points` (rounded to 0.01°) and returns weighted points for Leaflet.heat |
| Heatmap | `leaflet.heat` renders a gradient overlay on OpenStreetMap tiles |

## API Endpoints

| Route | Description |
|-------|-------------|
| `GET /auth/login` | Redirect to VATSIM OAuth |
| `GET /auth/callback` | OAuth callback, creates session |
| `GET /auth/logout` | Clear session |
| `GET /api/me` | Current user info |
| `GET /api/live` | Current flight + records position |
| `GET /api/heatmap` | All recorded positions |
| `GET /api/flights` | Recent 20 flights |
| `GET /api/stats` | Stats + top routes |

## Database Schema

- **users** — VATSIM CID, name, email
- **flight_points** — lat/lng/altitude/groundspeed per poll interval
- **flights** — flight session with callsign, dep, arr, aircraft

## Deployment Notes

For production:
- Use `gunicorn` instead of Flask dev server
- Set a strong `SECRET_KEY` in `.env`
- Update `REDIRECT_URI` to your public domain
- Consider adding `flask-limiter` for rate limiting
- Add HTTPS (required by VATSIM Connect in production)
