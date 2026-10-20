FROM python:3.12-slim
WORKDIR /app
COPY Tools/Analytics/ /app/analytics/
COPY tmp/AnalyticsWebReview/alpha/play/ /app/web/play/
RUN useradd --create-home collector && mkdir /data && chown collector:collector /data
USER collector
VOLUME ["/data"]
EXPOSE 8765
ENTRYPOINT ["python", "-B", "/app/analytics/collector.py", "serve", "--host", "0.0.0.0", "--port", "8765", "--database", "/data/playtest.sqlite3", "--web-root", "/app/web/play", "--allow-origin", "https://abingigo.github.io"]
