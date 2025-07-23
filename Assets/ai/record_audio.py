import pyaudio
import wave
import sys
import os
import threading

# Audio recording parameters
FORMAT = pyaudio.paInt16  # 16-bit format
CHANNELS = 1  # Mono audio
RATE = 44100  # Sampling rate (Hz)
CHUNK = 1024  # Buffer size
OUTPUT_FILENAME = "recorded_audio.wav"

# Flag to control recording
is_recording = True

def record_audio(output_folder):
    global is_recording
    audio = pyaudio.PyAudio()

    # Ensure the output folder exists
    os.makedirs(output_folder, exist_ok=True)

    output_path = os.path.join(output_folder, OUTPUT_FILENAME)

    # Open the microphone stream
    stream = audio.open(format=FORMAT, channels=CHANNELS,
                        rate=RATE, input=True,
                        frames_per_buffer=CHUNK)

    print(f"Recording... Saving to {output_path}")

    frames = []
    while is_recording:
        data = stream.read(CHUNK)
        frames.append(data)

    print("Recording stopped.")

    # Stop and close the stream
    stream.stop_stream()
    stream.close()
    audio.terminate()

    # Save the recorded audio to a WAV file
    with wave.open(output_path, 'wb') as wf:
        wf.setnchannels(CHANNELS)
        wf.setsampwidth(audio.get_sample_size(FORMAT))
        wf.setframerate(RATE)
        wf.writeframes(b''.join(frames))

    print(f"Saved recording as {output_path}")

def stop_recording():
    global is_recording
    is_recording = False

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Error: Output folder argument is missing.")
        sys.exit(1)

    output_folder = sys.argv[1]
    
    recording_thread = threading.Thread(target=record_audio, args=(output_folder,))
    recording_thread.start()
    
    # Wait for Unity to send the stop signal
    input()  # Blocks until Unity sends a signal (simulated as input)
    stop_recording()
    recording_thread.join()
