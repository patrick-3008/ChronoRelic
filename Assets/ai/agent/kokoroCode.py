from kokoro import KPipeline
import soundfile as sf

# Load text from file
with open("C:/Developer/Unity Projects/ChronoRelic/Assets/ai/agent/npc_output.txt", "r", encoding="utf-8") as f:
    text = f.read().strip()

# Initialize Kokoro pipeline
pipeline = KPipeline(lang_code="a")  # "a" = auto-detect (or use "en" for English)

# Choose male voice, e.g., "am_adam"
for i, (g, p, audio) in enumerate(pipeline(text, voice="am_onyx")):
    sf.write("output.wav", audio, 24000)
    break  # write only the first result