# import os
# import torch
# import torchaudio
# from TTS.tts.configs.xtts_config import XttsConfig
# from TTS.tts.models.xtts import Xtts
# from IPython.display import Audio, display

# CONFIG_FILE_PATH = 'C:/Users/Patrickn/Jupyter_notebooks/Graduation/AI_Companion/TTS/egtts/EGTTS-V0.1/config.json'
# VOCAB_FILE_PATH = 'C:/Users/Patrickn/Jupyter_notebooks/Graduation/AI_Companion/TTS/egtts/EGTTS-V0.1/vocab.json'
# MODEL_PATH = 'C:/Users/Patrickn/Jupyter_notebooks/Graduation/AI_Companion/TTS/egtts/EGTTS-V0.1'
# SPEAKER_AUDIO_PATH = 'C:/Users/Patrickn/Jupyter_notebooks/Graduation/AI_Companion/TTS/egtts/EGTTS-V0.1/speaker_reference.wav'

# print("Loading model...")
# config = XttsConfig()
# config.load_json(CONFIG_FILE_PATH)
# model = Xtts.init_from_config(config)
# model.load_checkpoint(config, checkpoint_dir=MODEL_PATH, use_deepspeed=False, vocab_path=VOCAB_FILE_PATH)
# model.to(torch.device("cuda"))

# print("Computing speaker latents...")
# gpt_cond_latent, speaker_embedding = model.get_conditioning_latents(audio_path=[SPEAKER_AUDIO_PATH])

# with open("C:/Developer/Unity Projects/grad/Assets/ai/gbt/gbt_output.txt", "r", encoding="utf-8") as f:
#     text = f.read().strip()


# # text = "انا اسمي باتريك و المشروع ان شاء اللة هيبقي زي الفل"
# print("Inference...")

# out = model.inference(
#     text,
#     "ar",
#     gpt_cond_latent,
#     speaker_embedding,
#     temperature=0.75,
# )

# # AUDIO_OUTPUT_PATH = "/content/sample_data/output_audio.wav"
# torchaudio.save("xtts_audio.wav", torch.tensor(out["wav"]).unsqueeze(0), 24000)
# display(Audio(out["wav"], rate=24000, autoplay=True))


# api_server.py
import os
import torch
import torchaudio
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from TTS.tts.configs.xtts_config import XttsConfig
from TTS.tts.models.xtts import Xtts
import uvicorn

# Configuration paths (make sure these are accessible from where you run the server)
CONFIG_FILE_PATH = 'C:/Developer/Unity Projects/ChronoRelic/Assets/ai/egtts/EGTTS-V0.1/config.json'
VOCAB_FILE_PATH = 'C:/Developer/Unity Projects/ChronoRelic/Assets/ai/egtts/EGTTS-V0.1/vocab.json'
MODEL_PATH = 'C:/Developer/Unity Projects/ChronoRelic/Assets/ai/egtts/EGTTS-V0.1'
SPEAKER_AUDIO_PATH = 'C:/Developer/Unity Projects/ChronoRelic/Assets/ai/egtts/EGTTS-V0.1/speaker_reference.wav'

app = FastAPI()

# Global variables to hold the model and latents
model = None
gpt_cond_latent = None
speaker_embedding = None

class InferenceRequest(BaseModel):
    text: str

@app.on_event("startup")
async def load_model_on_startup():
    """
    Loads the XTTS model and computes speaker latents when the FastAPI application starts.
    """
    global model, gpt_cond_latent, speaker_embedding, device

    print("Loading model...")

    try:
        # Choose the device
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        print(f"Using device: {device}")

        # Load and move model
        config = XttsConfig()
        config.load_json(CONFIG_FILE_PATH)

        model = Xtts.init_from_config(config)
        model.load_checkpoint(
            config,
            checkpoint_dir=MODEL_PATH,
            use_deepspeed=False,
            vocab_path=VOCAB_FILE_PATH
        )
        model.to(device)
        print(f"Model loaded on device: {next(model.parameters()).device}")

        # Compute speaker latents and move them to the same device
        print("Computing speaker latents...")
        gpt_cond_latent, speaker_embedding = model.get_conditioning_latents(audio_path=[SPEAKER_AUDIO_PATH])
        gpt_cond_latent = gpt_cond_latent.to(device)
        speaker_embedding = speaker_embedding.to(device)
        print(f"Speaker latents moved to device: {device}")

        print("Model and latents successfully loaded and ready.")

    except Exception as e:
        print(f"Error loading model or computing latents: {e}")
        raise RuntimeError(f"Failed to load model or compute latents: {e}")


@app.post("/infer")
async def infer_text(request: InferenceRequest):
    """
    Performs inference on the provided text using the loaded XTTS model.
    Returns the audio as a WAV file.
    """
    if model is None or gpt_cond_latent is None or speaker_embedding is None:
        raise HTTPException(status_code=503, detail="Model not loaded. Please wait for startup.")

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    print(f"Received text for inference: {request.text[:50]}...")

    try:
        # Move embeddings to the correct device
        gpt_cond_latent_device = gpt_cond_latent.to(device)
        speaker_embedding_device = speaker_embedding.to(device)

        out = model.inference(
            request.text,
            "ar",
            gpt_cond_latent_device,
            speaker_embedding_device,
            temperature=0.75,
        )

        # Convert to tensor and ensure it's on CPU for saving
        audio_tensor = torch.tensor(out["wav"]).unsqueeze(0).cpu()

        # Save to temp file
        temp_audio_path = "egtts_output.wav"
        torchaudio.save(temp_audio_path, audio_tensor, 24000)

        with open(temp_audio_path, "rb") as audio_file:
            audio_bytes = audio_file.read()

        os.remove(temp_audio_path)

        from starlette.responses import StreamingResponse
        import io
        return StreamingResponse(io.BytesIO(audio_bytes), media_type="audio/wav")

    except Exception as e:
        print(f"Error during inference: {e}")
        raise HTTPException(status_code=500, detail=f"Inference failed: {e}")

if __name__ == "__main__":
    # To run this server, use: uvicorn api_server:app --host 0.0.0.0 --port 8002
    uvicorn.run(app, host="0.0.0.0", port=8002)