# ====== STAGE 1: Builder (Install Dependencies) ======
FROM python:3.10-slim AS builder

# Install build dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        git \
        python3-dev \
        build-essential && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create virtual environment
RUN python -m venv /venv
ENV PATH="/venv/bin:$PATH"

# Upgrade pip using the virtual environment's pip
RUN pip install --upgrade pip setuptools wheel

# Copy dependency files first for cache optimization
COPY requirements.txt /tmp/requirements.txt

# Install Python dependencies with pip cache
RUN --mount=type=cache,target=/root/.cache/pip \
    pip install -r /tmp/requirements.txt

# ====== STAGE 2: Final Runtime Image ======
FROM python:3.10-slim AS final

# Install runtime-only system dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        sox && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy virtual environment from builder
COPY --from=builder /venv /venv
ENV PATH="/venv/bin:$PATH"

# Copy application code
WORKDIR /workspace
COPY . /workspace

RUN pip install uvicorn fastapi python-multipart

# Expose the port FastAPI will use
EXPOSE 8000

# Run uvicorn using Python -m (recommended in Docker)
CMD ["python", "-m", "uvicorn", "load_fast_conformer:app", "--host", "0.0.0.0", "--port", "8000"]