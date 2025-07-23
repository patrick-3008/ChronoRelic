import requests

# FastAPI server URL
FASTAPI_URL = "http://127.0.0.1:8002"  # Or the IP address where your server is running

# Path to the text file
TEXT_FILE_PATH = "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/gbt/gbt_output.txt"
AUDIO_OUTPUT_PATH = "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/egtts/egtts_output.wav"

def perform_inference(text_content: str):
    """
    Sends the text content to the FastAPI server for inference and saves the audio.
    """
    headers = {"Content-Type": "application/json"}
    payload = {"text": text_content}

    print(f"Sending text for inference to {FASTAPI_URL}/infer...")
    try:
        response = requests.post(f"{FASTAPI_URL}/infer", headers=headers, json=payload)
        response.raise_for_status()  # Raise an HTTPError for bad responses (4xx or 5xx)

        with open(AUDIO_OUTPUT_PATH, "wb") as f:
            f.write(response.content)
        print(f"Audio successfully saved to {AUDIO_OUTPUT_PATH}")

    except requests.exceptions.ConnectionError:
        print(f"Error: Could not connect to the FastAPI server at {FASTAPI_URL}. Is it running?")
    except requests.exceptions.RequestException as e:
        print(f"Error during inference request: {e}")
        if response is not None:
            print(f"Server response: {response.text}")
    except Exception as e:
        print(f"An unexpected error occurred: {e}")

if __name__ == "__main__":
    try:
        with open(TEXT_FILE_PATH, "r", encoding="utf-8") as f:
            text_to_infer = f.read().strip()
        print(f"Text loaded from {TEXT_FILE_PATH}")
    except FileNotFoundError:
        print(f"Error: Text file not found at {TEXT_FILE_PATH}")
        text_to_infer = ""
    except Exception as e:
        print(f"Error reading text file: {e}")
        text_to_infer = ""

    if text_to_infer:
        perform_inference(text_to_infer)
    else:
        print("No text to infer. Please check the text file.")