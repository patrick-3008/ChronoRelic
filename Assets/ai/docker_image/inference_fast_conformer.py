import requests

def transcribe(audio_path):
    url = "http://localhost:8000/transcribe/"
    with open(audio_path, "rb") as f:
        files = {"file": f}
        response = requests.post(url, files=files)
        response.raise_for_status()
    return response.json()["transcript"]

if __name__ == "__main__":
    audio_path = "sample_data/recorded_audio.wav"
    output_path = "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/gbt/asr_output.txt"

    result = transcribe(audio_path)
    print("Transcription:", result['text'])

    # Save to text file
    with open(output_path, "w", encoding="utf-8") as out_file:
        out_file.write(result['text'] + "/n")

    print(f"Transcription saved to {output_path}")
