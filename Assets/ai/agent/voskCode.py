from vosk import Model, KaldiRecognizer
import wave
import json

# Load model
model = Model("vosk-model-small-en-us-0.15")

# Load audio
wf = wave.open("C:/Developer/Unity Projects/ChronoRelic/Assets/ai/docker_image/sample_data/recorded_audio.wav", "rb")
rec = KaldiRecognizer(model, wf.getframerate())

# Store results
results = []

# Recognize
while True:
    data = wf.readframes(4000)
    if len(data) == 0:
        break
    if rec.AcceptWaveform(data):
        result = json.loads(rec.Result())
        results.append(result["text"])

# Append final result
final_result = json.loads(rec.FinalResult())
results.append(final_result["text"])

# Save to file
with open("transcription.txt", "w") as f:
    for line in results:
        f.write(line + "/n")

print("Transcription saved to 'transcription.txt'")