import contextlib
import datetime
import logging
import os
import random
from typing import Annotated

import fastapi
import fastapi.responses
import fastapi.security
import fastapi.staticfiles
import httpx
import opentelemetry.instrumentation.fastapi as otel_fastapi
import telemetry
from jose import jwt, JWTError


@contextlib.asynccontextmanager
async def lifespan(app):
    telemetry.configure_opentelemetry()
    yield


app = fastapi.FastAPI(lifespan=lifespan)
otel_fastapi.FastAPIInstrumentor.instrument_app(app, exclude_spans=["send"])

logger = logging.getLogger(__name__)

# Keycloak configuration from environment variables injected by Aspire.
KEYCLOAK_URL = os.getenv("services__keycloak__https__0", "")
KEYCLOAK_REALM = os.getenv("KEYCLOAK_REALM", "aspirekeycloaksample")
KEYCLOAK_AUDIENCE = os.getenv("KEYCLOAK_AUDIENCE", "keycloak.api.weather")

oauth2_scheme = fastapi.security.HTTPBearer()

_jwks_cache: dict | None = None


async def _get_jwks() -> dict:
    """Fetch and cache the Keycloak JWKS (JSON Web Key Set)."""
    global _jwks_cache
    if _jwks_cache is not None:
        return _jwks_cache

    jwks_url = f"{KEYCLOAK_URL}/realms/{KEYCLOAK_REALM}/protocol/openid-connect/certs"
    async with httpx.AsyncClient(verify=False) as client:
        response = await client.get(jwks_url)
        response.raise_for_status()
        _jwks_cache = response.json()
        return _jwks_cache


async def verify_token(
    credentials: Annotated[fastapi.security.HTTPAuthorizationCredentials, fastapi.Depends(oauth2_scheme)],
) -> dict:
    """Validate the JWT Bearer token from Keycloak."""
    token = credentials.credentials

    try:
        jwks = await _get_jwks()
        # Decode the token header to find the key ID
        unverified_header = jwt.get_unverified_header(token)
        kid = unverified_header.get("kid")

        # Find the matching key
        rsa_key = None
        for key in jwks.get("keys", []):
            if key.get("kid") == kid:
                rsa_key = key
                break

        if rsa_key is None:
            raise fastapi.HTTPException(status_code=401, detail="Unable to find signing key")

        issuer = f"{KEYCLOAK_URL}/realms/{KEYCLOAK_REALM}"
        payload = jwt.decode(
            token,
            rsa_key,
            algorithms=["RS256"],
            audience=KEYCLOAK_AUDIENCE,
            issuer=issuer,
        )
        return payload

    except JWTError as e:
        raise fastapi.HTTPException(status_code=401, detail=f"Token validation failed: {e}")
    except httpx.HTTPError as e:
        raise fastapi.HTTPException(status_code=503, detail=f"Unable to validate token: {e}")


if not os.path.exists("static"):
    @app.get("/", response_class=fastapi.responses.HTMLResponse)
    async def root():
        """Root endpoint."""
        return "Weather API is running. Navigate to <a href='/api/weatherforecast'>/api/weatherforecast</a> (requires auth)."


@app.get("/api/weatherforecast")
async def weather_forecast(token: Annotated[dict, fastapi.Depends(verify_token)]):
    """Weather forecast endpoint (requires JWT Bearer authentication)."""
    summaries = [
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching",
    ]

    forecast = []
    for index in range(1, 6):
        temp_c = random.randint(-20, 55)
        forecast_date = datetime.datetime.now() + datetime.timedelta(days=index)
        forecast_item = {
            "date": forecast_date.isoformat(),
            "temperatureC": temp_c,
            "temperatureF": int(temp_c * 9 / 5) + 32,
            "summary": random.choice(summaries),
        }
        forecast.append(forecast_item)

    return forecast


@app.get("/health", response_class=fastapi.responses.PlainTextResponse)
async def health_check():
    """Health check endpoint."""
    return "Healthy"


# Serve static files directly from root, if the "static" directory exists
if os.path.exists("static"):
    app.mount(
        "/",
        fastapi.staticfiles.StaticFiles(directory="static", html=True),
        name="static"
    )
