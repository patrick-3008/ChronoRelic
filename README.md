# Chrono Relic 🎮⏳

**AN AI­-POWERED INTERACTIVE 3D RPG VIDEO GAME**

## 🧩 Overview

**Chrono Relic** is a 3D role-playing game (RPG) and graduation project developed using Unity, set in the rich and mysterious world of **Ancient Egypt**. Players embark on a time-traveling adventure to uncover hidden historical secrets, interact with dynamic characters, and engage in strategic combat and puzzle-solving challenges.

This project serves as the basis for an academic thesis, exploring the integration of **real-time Speech-to-Speech (S2S) AI companions**, **cognitive NPCs**, and immersive gameplay mechanics in a historically inspired setting with **full Egyptian Arabic voice interaction**.

---

## 📖 Thesis Abstract

> *Chrono Relic* combines immersive storytelling with strategic gameplay, blending traditional RPG mechanics with cutting-edge AI technology. It introduces cognitive NPC modeling, dynamic personality systems, and a fully Arabic-speaking AI companion to create a unique, engaging player experience. From artifact discovery to time-travel dilemmas, players must navigate ancient Egypt both physically and psychologically.

---

## 🎥 Demo Videos

* 📹 **Gameplay Overview Demo**
  
    link ->  https://youtu.be/orHdX0ebkpU
  

* 🎙️ **Egyptian Arabic AI Companion Demo**

https://github.com/user-attachments/assets/4e2ae722-4cb1-4fde-ba7e-8620e4e32ec8

---

## 🌟 Key Features

- 🏛️ **Historically Inspired World**  
  Explore highly detailed ancient Egyptian environments with atmospheric audio, dynamic lighting, and authentic 3D models.
  
- 🧠 **AI-Driven Companion (Arabic-speaking)**  
  A Retrieval-Augmented Generation (RAG) system powers an intelligent companion who provides real-time help, historical context, and puzzle hints—**entirely in Egyptian Arabic**.
  
- 🗣️ **Cognitive NPCs with Evolving Personalities**  
  Characters that remember player actions and build trust or suspicion over time, offering socially complex interactions.
  
- 🕹️ **Strategic Combat & Puzzle-Solving**  
  Real-time battles, exploration-based missions, and environmental puzzles to challenge both the player’s reflexes and reasoning.
  
- 📈 **RPG Systems**  
  Character progression, inventory management, mission-based storytelling, and boss fights.
  
- 💻 **Robust Technical Foundation**  
  Built in Unity using C# with state machines, procedural generation, custom animation pipelines, and a modular architecture for scalability.

## 🕹️ Immersion Design
**Chrono Relic** prioritizes immersion, the psychological state where players feel deeply absorbed in the game world, achieved through:
- **Suspension of Disbelief:** Coherent world design avoids the "uncanny valley" by prioritizing believability over hyper-realism.
- **Presence:** High-quality 3D visuals, spatial audio, and responsive controls create a vivid "being there" experience.
- **Immersion Types:**
  - **Sensory:** Vivid graphics, spatial audio, and dynamic lighting envelop players in the world.
  - **Narrative & Psychological:** Branching narratives and emotionally resonant characters, like the AI companion Hemdan, foster deep investment and a sense of embodying the protagonist, Lorenzo.
  - **Tactical & Strategic:** Fluid combat and complex puzzles engage players’ reflexes and problem-solving skills.
  - **Spatial:** Logical, reactive environments enhance the sense of a living game world.

## 🧠 AI-Driven Narrative Systems
The game’s AI systems enhance immersion and narrative depth:
- **Cognitive NPC System (Neferkare):** Uses a dual-process cognitive model (internal monologue and behavioral response) to simulate human-like NPCs. As a narrative gatekeeper, Neferkare drives psychological immersion through trust-based, choice-driven dialogue that shapes the story.
- **Companion System (Hemdan):** Built on a RAG pipeline with a ChromaDB vector database, Hemdan delivers context-aware, factual responses in Egyptian Arabic. As Lorenzo’s narrative anchor, it provides reliable information and emotional resonance, reflecting the hero’s loss.
- **Narrative Synergy:** Neferkare’s unpredictable behavior contrasts with Hemdan’s reliability, creating dynamic tension that enriches the quest for the Ankh.

## 🎙️ Speech-to-Speech Pipeline Architecture
The AI companion’s real-time Egyptian Arabic interaction is powered by:
1. **Speech-to-Text (STT) - Fast Conformer:**
   - **Architecture:** Hybrid CNN-Transformer with 32M parameters.
   - **Features:** Real-time streaming, optimized for Egyptian Arabic.
   - **Dataset:** Trained on 100+ hours of Egyptian Arabic speech.
   - **Credits:** Based on [Egyptian Arabic ASR](https://github.com/yousefkotp/Egyptian-Arabic-ASR-and-Diarization.git).
   - 
2. **Large Language Model (LLM):**
   - **Model:** GPT-4o Mini via OpenAI API.
   - **Features:** Context-aware dialogue with emotional intelligence and native Arabic support.
   - **Integration:** Cloud-based with local fallback.
   - 
3. **Text-to-Speech (TTS) - EGTTS:**
   - **Model:** Fine-tuned XTTS-v2 with 482M parameters.
   - **Dataset:** Fine-tuned on 20 hours of our custom Egyptian Arabic speech dataset.
   - **Performance:** 2-3 seconds inference time per sentence, delivering high-fidelity speech.
   - **Credits:** Based on [Egyptian Text-To-Speech](https://github.com/joejoe03/Egyptian-Text-To-Speech.git).
4. **Integration Flow:**

Player Speech → Fast Conformer (STT) → GPT-4o Mini (LLM) → EGTTS (TTS) → Audio Response
    ↓                    ↓                     ↓                ↓
 Unity Input    →    FastAPI Server   →   OpenAI API   →   Local TTS Server
 
---

## 🔧 Tech Stack
- **Engine:** Unity 3D
- **Languages:** C#, Python
- **AI Framework:** Custom Speech-to-Speech pipeline
- **RAG System:** Retrieval-Augmented Generation with ChromaDB vector database
- **STT Model:** Fast Conformer (Fine-tuned for Egyptian Arabic)
- **LLM:** GPT-4o Mini via OpenAI API
- **TTS Model:** EGTTS (Fine-tuned with 20 hours of our custom dataset)
- **Deployment:** Docker containers with FastAPI microservices
- **Tools:** Blender (3D modeling), FMOD (Audio), Git (Version Control),Adobe Suite (UI/UX)
  

## 📁 Project Structure (sample)

```
/Assets
  /Scripts
  /Scenes
  /Models
  /Audio
  /AI
  /UI
/Thesis
  ChronoRelic_Thesis.pdf
  Abstract.txt
/Videos
  Gameplay_Demo.mp4
  Arabic_Companion_Demo.mp4
```

---

## 🧑‍🎓 About the Project

This game is developed as the **graduation project** for the Bachelor’s degree in Computer Science, combining narrative design, technical implementation, and academic research into AI-enhanced gameplay.

---

## 📜 License

This project is licensed under the MIT License – see the [LICENSE](LICENSE) file for details.

---

## 🙌 Acknowledgements

- **STT Model:** Built upon [Egyptian Arabic ASR and Diarization](https://github.com/yousefkotp/Egyptian-Arabic-ASR-and-Diarization.git)
- **TTS Model:** Enhanced version of [Egyptian Text-To-Speech](https://github.com/joejoe03/Egyptian-Text-To-Speech.git)
- **Dataset Contributors:** 50 volunteer students from Arab Academy for Science, Technology & Maritime Transport
- **Academic Supervision:** Dr. Ahmed El-Kabbany
- **Cultural Heritage:** Inspired by the rich history of Ancient Egypt
---
## 📚 Citations

If you use this work in your research, please cite:
```bibtex
@misc{chrono_relic_2024,
  title={CHRONO RELIC: AN AI­POWERED INTERACTIVE 3D RPG VIDEO GAME},
  author={Ahmed Kamal, Alaa Imam, Ismail Fakhr, Patrick Nashaat, Reem Sameh},
  year={July 2025},
  institution={Arab Academy for Science, Technology & Maritime Transport}
}
