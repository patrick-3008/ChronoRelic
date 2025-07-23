cd "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/docker_image"
python inference_fast_conformer.py

cd "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/gbt"
python inference_rag_system.py asr_output.txt

cd "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/egtts"
conda activate xtts && python inference_egtts.py
