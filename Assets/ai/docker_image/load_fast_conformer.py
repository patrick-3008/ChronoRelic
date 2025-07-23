from fastapi import FastAPI, UploadFile, File
import torch
import nemo.collections.asr as nemo_asr
from ruamel.yaml import YAML
from omegaconf import OmegaConf
import librosa
import numpy as np
import tempfile

app = FastAPI()

class ASRModel:
    def __init__(self, ckpt_path, device):
        config_path = 'ASR_for_egyptian_dialect/configs/FC-transducer-inference.yaml'
        yaml = YAML(typ='safe')
        with open(config_path) as f:
            params = yaml.load(f)
        params['model'].pop('test_ds', None)
        conf = OmegaConf.create(params)

        self.model = nemo_asr.models.EncDecRNNTBPEModel(cfg=conf['model']).to(device)
        self.model.load_state_dict(torch.load(ckpt_path, map_location=device, weights_only=False)['state_dict'])
        self.model.eval()

    def infer(self, audio):
        return self.model.transcribe([audio])[0]

device = "cuda" if torch.cuda.is_available() else "cpu"
model = ASRModel("ASR_for_egyptian_dialect/Models/asr_model.ckpt", device)

@app.post("/transcribe/")
async def transcribe(file: UploadFile = File(...)):
    # Save to temp file and load with librosa
    with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as tmp:
        tmp.write(await file.read())
        tmp_path = tmp.name

    audio, sr = librosa.load(tmp_path, sr=16000)
    with torch.no_grad():
        transcript = model.infer(audio)

    return {"transcript": transcript}

