import { useState, useEffect, useCallback } from 'react';
import { useAuth } from 'react-oidc-context';
import aspireLogo from '/Aspire.png';
import './App.css';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

function App() {
  const auth = useAuth();
  const [weatherData, setWeatherData] = useState<WeatherForecast[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [useCelsius, setUseCelsius] = useState(false);

  const fetchWeatherForecast = useCallback(async () => {
    if (!auth.user?.access_token) return;

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/weatherforecast', {
        headers: {
          'Authorization': `Bearer ${auth.user.access_token}`,
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: WeatherForecast[] = await response.json();
      setWeatherData(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch weather data');
      console.error('Error fetching weather forecast:', err);
    } finally {
      setLoading(false);
    }
  }, [auth.user?.access_token]);

  useEffect(() => {
    if (auth.isAuthenticated) {
      fetchWeatherForecast();
    }
  }, [auth.isAuthenticated, fetchWeatherForecast]);

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });
  };

  // Handle OIDC callback (silent redirect)
  if (auth.isLoading) {
    return (
      <div className="app-container">
        <div className="loading-skeleton" role="status">
          <span className="visually-hidden">Loading authentication...</span>
        </div>
      </div>
    );
  }

  if (auth.error) {
    return (
      <div className="app-container">
        <div className="error-message" role="alert">
          <p>Authentication error: {auth.error.message}</p>
          <button onClick={() => auth.signinRedirect()} type="button" className="refresh-button">
            Try again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="app-container">
      <header className="app-header">
        <a
          href="https://aspire.dev"
          target="_blank"
          rel="noopener noreferrer"
          aria-label="Visit Aspire website (opens in new tab)"
          className="logo-link"
        >
          <img src={aspireLogo} className="logo" alt="Aspire logo" />
        </a>
        <h1 className="app-title">Aspire Keycloak Sample</h1>
        <p className="app-subtitle">TypeScript AppHost + Python API + React Frontend</p>
      </header>

      <main className="main-content">
        {/* Auth section */}
        <section className="auth-section" aria-labelledby="auth-heading">
          <div className="card">
            <h2 id="auth-heading" className="section-title">Authentication</h2>
            {auth.isAuthenticated ? (
              <div className="auth-info">
                <p>
                  Signed in as <strong>{auth.user?.profile.preferred_username || auth.user?.profile.email}</strong>
                </p>
                <button
                  className="refresh-button"
                  onClick={() => auth.signoutRedirect()}
                  type="button"
                >
                  Sign out
                </button>
              </div>
            ) : (
              <div className="auth-info">
                <p>Sign in to access the Weather API.</p>
                <button
                  className="refresh-button"
                  onClick={() => auth.signinRedirect()}
                  type="button"
                >
                  Sign in with Keycloak
                </button>
              </div>
            )}
          </div>
        </section>

        {/* Weather section - only visible when authenticated */}
        {auth.isAuthenticated && (
          <section className="weather-section" aria-labelledby="weather-heading">
            <div className="card">
              <div className="section-header">
                <h2 id="weather-heading" className="section-title">Weather Forecast</h2>
                <div className="header-actions">
                  <fieldset className="toggle-switch" aria-label="Temperature unit selection">
                    <legend className="visually-hidden">Temperature unit</legend>
                    <button
                      className={`toggle-option ${!useCelsius ? 'active' : ''}`}
                      onClick={() => setUseCelsius(false)}
                      aria-pressed={!useCelsius}
                      type="button"
                    >
                      <span aria-hidden="true">°F</span>
                      <span className="visually-hidden">Fahrenheit</span>
                    </button>
                    <button
                      className={`toggle-option ${useCelsius ? 'active' : ''}`}
                      onClick={() => setUseCelsius(true)}
                      aria-pressed={useCelsius}
                      type="button"
                    >
                      <span aria-hidden="true">°C</span>
                      <span className="visually-hidden">Celsius</span>
                    </button>
                  </fieldset>
                  <button
                    className="refresh-button"
                    onClick={fetchWeatherForecast}
                    disabled={loading}
                    aria-label={loading ? 'Loading weather forecast' : 'Refresh weather forecast'}
                    type="button"
                  >
                    <svg
                      className={`refresh-icon ${loading ? 'spinning' : ''}`}
                      width="20"
                      height="20"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      aria-hidden="true"
                      focusable="false"
                    >
                      <path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2"/>
                    </svg>
                    <span>{loading ? 'Loading...' : 'Refresh'}</span>
                  </button>
                </div>
              </div>

              {error && (
                <div className="error-message" role="alert" aria-live="polite">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                  </svg>
                  <span>{error}</span>
                </div>
              )}

              {loading && weatherData.length === 0 && (
                <div className="loading-skeleton" role="status" aria-live="polite" aria-label="Loading weather data">
                  {[...Array(5)].map((_, i) => (
                    <div key={i} className="skeleton-row" aria-hidden="true" />
                  ))}
                  <span className="visually-hidden">Loading weather forecast data...</span>
                </div>
              )}

              {weatherData.length > 0 && (
                <div className="weather-grid">
                  {weatherData.map((forecast, index) => (
                    <article key={index} className="weather-card" aria-label={`Weather for ${formatDate(forecast.date)}`}>
                      <h3 className="weather-date">
                        <time dateTime={forecast.date}>{formatDate(forecast.date)}</time>
                      </h3>
                      <p className="weather-summary">{forecast.summary}</p>
                      <div className="weather-temps" aria-label={`Temperature: ${useCelsius ? forecast.temperatureC : forecast.temperatureF} degrees ${useCelsius ? 'Celsius' : 'Fahrenheit'}`}>
                        <div className="temp-group">
                          <span className="temp-value" aria-hidden="true">
                            {useCelsius ? forecast.temperatureC : forecast.temperatureF}°
                          </span>
                          <span className="temp-unit" aria-hidden="true">{useCelsius ? 'Celsius' : 'Fahrenheit'}</span>
                        </div>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </div>
          </section>
        )}
      </main>

      <footer className="app-footer">
        <nav aria-label="Footer navigation">
          <a href="https://aspire.dev" target="_blank" rel="noopener noreferrer">
            Learn more about Aspire<span className="visually-hidden"> (opens in new tab)</span>
          </a>
          <a
            href="https://github.com/dotnet/aspire"
            target="_blank"
            rel="noopener noreferrer"
            className="github-link"
            aria-label="View Aspire on GitHub (opens in new tab)"
          >
            <img src="/github.svg" alt="" width="24" height="24" aria-hidden="true" />
            <span className="visually-hidden">GitHub</span>
          </a>
        </nav>
      </footer>
    </div>
  );
}

export default App;
