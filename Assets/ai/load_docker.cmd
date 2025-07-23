cd "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/docker_image"
docker build -t asr .
docker run -p 8000:8000 asr
