FROM python:3.12-slim
WORKDIR /app
COPY pyproject.toml ./
COPY shiftjam_pipeline ./shiftjam_pipeline
COPY lore ./lore
COPY schemas ./schemas
RUN pip install --no-cache-dir .
# Output goes to the mounted game content folder; the API key comes from the environment.
ENTRYPOINT ["python", "-m", "shiftjam_pipeline.cli"]
