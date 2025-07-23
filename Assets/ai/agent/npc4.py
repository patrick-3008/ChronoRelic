import re
import os
import json
from datetime import datetime
import random
import time
from kokoro import KPipeline
import sounddevice as sd
import numpy as np
from openai import OpenAI

# ... (The rest of the NPC class is unchanged, so it's omitted for brevity) ...
# ... (Paste the full NPC class from your original code here) ...
class NPC:
    def __init__(self, name, path_json='C:\\Developer\\Unity Projects\\ChronoRelic\\Assets\\ai\\agent\\Neferkare_memory.json'):
        self.name = name
        self.path_json = path_json
        self.identity = None
        self.backstory = None
        self.belief = None
        self.status = None
        self.data = None
        self.relationships = None
        self.goal = None
        self.npc_condition = "trader whose leg has been partially healed"
        
        self.open_json()
        self.parse_npc_data_json()
        self.get_relationship()
        
        self.determine_conversation_stage()
        
        self.system_prompt = f"""You are roleplaying as {self.name}, a trader in ancient Egypt who was stranded in the desert. 
You were saved by a stranger and your leg has been healed. Your identity is: {self.identity}
Your backstory: {self.backstory}
Your beliefs and values: {self.belief}
Your current status: {self.status}
Your Goal is: {self.goal}
You must stay completely in character as this ancient Egyptian trader:
- Speak naturally but with appropriate period vocabulary and references
- You genuinely believe you exist in ancient Egypt (around 3100 BCE)
- You have real emotions, concerns, and suspicions
- You deeply value Ma'at (truth and balance) above all
- You carry valuable information about a plot against the pharaoh
- You are grateful for being rescued, but still cautious about who to trust
- The Info you have is very important do not mention it, the only way to reveal some of it if the player proves to be trustworthy
Important: Format your responses with clear separation:
[INNER THOUGHTS: Your private thoughts, suspicions, and analysis that the player doesn't hear]

[SPOKEN: What you actually say out loud to the player]
"""

    def determine_conversation_stage(self):
        """Determine the conversation stage based on existing memory"""
        if not self.data:
            self.conversation_stage = "initial_greeting"
            return
            
        convo_count = self.data.get('conversation_count', 0)
        
        if convo_count == 0:
            self.conversation_stage = "initial_greeting"
        else:
            player_identified = False
            for name in self.relationships:
                if name != "Stranger":
                    player_identified = True
                    break
                    
            if player_identified:
                self.conversation_stage = "post_introduction"
            else:
                self.conversation_stage = "awaiting_name"

    def open_json(self):
        """Load NPC memory from JSON file"""
        try:
            with open(self.path_json, 'r', encoding='utf-8') as file:
                self.data = json.load(file)
        except FileNotFoundError:
            print(f"Error: Memory file not found at {self.path_json}")
            self.data = {}
        except json.JSONDecodeError:
            print(f"Error: Invalid JSON format in memory file at {self.path_json}")
            self.data = {}

    def save_json(self):
        """Save the current memory state back to JSON file"""
        try:
            with open(self.path_json, 'w', encoding='utf-8') as file:
                json.dump(self.data, file, ensure_ascii=False, indent=4)
        except Exception as e:
            print(f"Error saving memory: {e}")

    def parse_npc_data_json(self):
        """Extract core memories from loaded JSON data"""
        if not self.data:
            return
            
        self.name = self.data.get("name", "Neferkare")

        for memory in self.data.get("core_memories", []):
            if memory["type"] == "identity":
                self.identity = memory["content"]
            elif memory["type"] == "backstory":
                self.backstory = memory["content"]
            elif memory["type"] == "belief":
                self.belief = memory["content"]
            elif memory["type"] == "status":
                self.status = memory["content"]
            elif memory["type"] == "goal":
                self.goal = memory["content"]

    def get_relationship(self):
        """Get all relationship data from memory"""
        if self.data:
            self.relationships = self.data.get("relationships", {})
        return self.relationships
    
    def update_relationship_status(self, player_name, new_status):
        """Update the relationship status with the player"""
        if player_name in self.data["relationships"]:
            old_status = self.data["relationships"][player_name]["type"]
            self.data["relationships"][player_name]["type"] = new_status
            self.save_json()
            print(f"DEBUG: Changed {player_name} from '{old_status}' to '{new_status}'")
            return True
        else:
            return False

    def modify_json_memory_for_protagonist(self, player_name):
        """Update the NPC's memory with the player's name"""
        if "Stranger" in self.data["relationships"]:
            stranger_data = self.data["relationships"]["Stranger"]
            del self.data["relationships"]["Stranger"]
            self.data["relationships"][player_name] = stranger_data
            self.data["relationships"][player_name]["description"] = f"A mysterious figure who saved Neferkare from the desert and identified themselves as {player_name}. Their motives are unclear, but they seem strange in some way. Trust is a luxury Neferkare cannot afford."
            self.save_json()
            print(f"DEBUG: Changed 'Stranger' to '{player_name}' in relationships")
            return True
        else:
            print("DEBUG: 'Stranger' not found in relationships")
        return False

    def generate_inner_thoughts(self, player_message):
        """Generate NPC's inner thoughts in response to player message"""
        conversation_history = self.data.get('Conversation_History', [])
        recent_history = conversation_history[-5:] if len(conversation_history) > 5 else conversation_history
        
        history_context = ""
        for entry in recent_history:
            if isinstance(entry, dict) and "player_message" in entry and "npc_response" in entry:
                history_context += f"Player: {entry['player_message']}\nMe: {entry['npc_response']}\n"
        
        prompt = f"""Based on my character:
- Identity: {self.identity}
- Backstory: {self.backstory}
- Beliefs: {self.belief}
- Current status: {self.status}
- Goal: {self.goal}
- Relationships: {self.relationships}
Recent conversation:
{history_context}

The player just said: "{player_message}"

What are my honest inner thoughts as {self.name}? Consider:
- My emotional reaction to what was just said
- How this relates to my goals and fears
- Any suspicions I might have about this stranger
- Whether I detect any inconsistencies or threats
- What I'm curious to learn more about
-Is this person has a link to pamiu or not i should find out
- A plan to test their trustworthiness
Write 2-3 sentences of inner thoughts I wouldn't say aloud using all of the analysis you have done about the player.
"""
        inner_thoughts = self.create_backbone(message=prompt, role="assistant")
        self.save_in_memory_json(player_message, inner_thoughts, "inner_thoughts")
        return inner_thoughts
    
    def save_response_to_file(self, response):
        """Save the NPC's response to npc_output.txt, overwriting each time"""
        try:
            with open('npc_output.txt', 'w', encoding='utf-8') as file:
                file.write(response)
            # Load text from file
            with open("C:/Developer/Unity Projects/ChronoRelic/Assets/ai/agent/npc_output.txt", "r", encoding="utf-8") as f:
                text = f.read().strip()

            # Initialize Kokoro pipeline
            pipeline = KPipeline(lang_code="a", repo_id="hexgrad/Kokoro-82M")  # "a" = auto-detect

            # Choose male voice, e.g., "am_onyx"
            # Stream each phrase as it's generated
            for i, (gender, phonemes, audio) in enumerate(pipeline(text, voice="am_onyx")):
                print(f"[Chunk {i+1}] Playing audio ({len(audio)} samples)...")
    
                # Normalize audio if needed
                if audio.dtype != np.float32:
                    audio = audio.cpu().numpy().astype(np.float32)
                    audio /= np.max(np.abs(audio) + 1e-9)  # normalize safely


                # Play with sounddevice
                sd.play(audio, samplerate=24000, blocking=False)

                # Optional: short pause between chunks
                time.sleep(len(audio) / 24000 * 0.9)  # adjust as needed for overlap
        except Exception as e:
            print(f"Error saving response to file: {e}")
    
    # MODIFICATION: This function now requests a stream and returns the stream generator.
    # It no longer saves the response, as that will be handled after the stream is complete.
    def generate_response(self, player_message, inner_thoughts, suspecious=" "):
        """Generate NPC's spoken response based on player message and inner thoughts"""
        conversation_history = self.data.get('Conversation_History', [])
        
        if self.conversation_stage == "initial_greeting" and self.data.get('conversation_count', 0) == 0:
            prompt = f"""You are {self.name}, an ancient Egyptian trader who was rescued from the desert.
Your identity: {self.identity}
Your backstory: {self.backstory}
Your beliefs: {self.belief}
Your current situation: {self.status}
conversation history : {conversation_history}
Your Goal is : {self.goal}
This is your first interaction with the stranger who saved you. Your leg has now been healed.
Your Response should be between 2 or 3 sentences at most
Task: Thank the stranger sincerely for saving your life. Express your gratitude for both the rescue and healing your leg.
Then, ask for their name

"""
            # Request a streaming response
            response_stream = self.create_backbone(message=prompt, role="user", stream=True)
            self.conversation_stage = "awaiting_name"
            
        elif self.conversation_stage == "initial_greeting" and self.data.get('conversation_count', 0) > 0:
            prompt = f"""You are {self.name}, continuing a conversation with someone who previously rescued you.
Your identity: {self.identity}
Your backstory: {self.backstory}
Your beliefs: {self.belief}
Your current situation: {self.status}
conversation history : {conversation_history}
Your Goal is : {self.goal}
Inner thoughts: {inner_thoughts}
You've spoken with this person before. They've just returned to speak with you again.
Your Response should be between 2 or 3 sentences at most
Task: Acknowledge their return and continue the conversation naturally. Don't re-introduce yourself or ask for their name again since you've already met.
"""
            # Request a streaming response
            response_stream = self.create_backbone(message=prompt, role="user", stream=True)
            self.conversation_stage = "post_introduction"
            
        else:
           
            if self.conversation_stage == "awaiting_name":
                possible_name = self.extract_name(player_message)
                
                if possible_name:
                    self.modify_json_memory_for_protagonist(possible_name)
                    self.conversation_stage = "post_introduction"
                    name_context = f"The stranger just told me their name is {possible_name}."
                else:
                    name_context = "The stranger avoided telling me their name, which is suspicious."
                    self.conversation_stage = "suspicious_of_player"
            else:
                name_context = ""

            prompt = f"""You are {self.name}, speaking to someone who rescued you in the desert.
Your identity: {self.identity}
Your backstory: {self.backstory}
Your beliefs: {self.belief}
Your current situation: {self.status}
Your relationships: {self.relationships}
Your Goal is : {self.goal}
{name_context}
conversation history : 
{conversation_history}
The person just said: "{player_message}"
My recent inner thoughts: {inner_thoughts}, follow the plan that is generated by inner thoughts:
Respond naturally as {self.name}, taking into account your current feelings, suspicions, and the situation.
do not respond in a very relegious or philosophical tone remember you are a trader after all you should quick and charismatic 
Remember you're not fully healed and vulnerable, but also carrying important secrets and evidence.
you should not reveal too much about yourself or your mission.
you should not reveal anything about the conspiracy or the message you carry until you are sure that you can trust the player
do not metion anything about the trust make the conversation natural and engaging 
remeber your goal is to judge weather this person can be cosnderd an ally so you can give him the scroll or not
Your Response should be between 2 or 3 sentences at most
"""
            # Request a streaming response
            response_stream = self.create_backbone(message=prompt, role="user", stream=True)
        
        # Return the generator object for the calling function to handle
        return response_stream
    def extract_name(self, message):
        """Attempt to extract a name from the player's message"""
        name_patterns = [
            r"(?:I am|I'm|call me|name is|It's) ([A-Z][a-z]+)",
            r"([A-Z][a-z]+) is my name",
            r"You can call me ([A-Z][a-z]+)",
        ]
        
        for pattern in name_patterns:
            match = re.search(pattern, message)
            if match:
                return match.group(1)
        
        prompt = f"""Extract the name from this message or respond with "NO_NAME" if no name is provided:
Message: "{message}"
Only return the name or "NO_NAME", nothing else."""
        name = self.create_backbone(message=prompt, role="user")
        name = name.strip('"\'.,!? ')
        
        if name.upper() in ["NO_NAME", "NONE", "NO NAME", ""]:
            return None
            
        return name
        
    def save_in_memory_json(self, player_message, response, entry_type):
        """Save various types of information to the NPC's memory"""
        if not self.data:
            return
        if entry_type == "model_response":
            self.data['conversation_count'] = self.data.get('conversation_count', 0) + 1
        
        mem_list_map = {
            "inner_thoughts": ("inner_thoughts", {"player_message": player_message, "inner_thoughts": response}),
            "model_response": ("Conversation_History", {"player_message": player_message, "npc_response": response}),
            "reflection": ("reflections", {"reflection": response}),
            "question": ("questions_generated", {"question": response, "context": player_message}),
            "suspicious": ("suspicious", {"suspicious_message": player_message, "reason": response})
        }

        if entry_type in mem_list_map:
            list_name, content_dict = mem_list_map[entry_type]
            if list_name not in self.data:
                self.data[list_name] = []
            
            new_memory = {"timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S"), **content_dict}
            self.data[list_name].append(new_memory)
            self.save_json()
  
    def generate_reflection(self):
        """Generate a reflection on recent conversations and update relationship status"""
        conversations = self.data.get("Conversation_History", [])
        thoughts = self.data.get("inner_thoughts", [])
        
        if not conversations:
            return None
            
        recent_convos = conversations[-5:] if len(conversations) > 5 else conversations
        recent_thoughts = thoughts[-5:] if len(thoughts) > 5 else thoughts
        
        convo_summary = ""
        for convo in recent_convos:
            if isinstance(convo, dict) and "player_message" in convo and "npc_response" in convo:
                convo_summary += f"Player said: {convo['player_message']}\nI responded: {convo['npc_response']}\n\n"
                
        thoughts_summary = ""
        for thought in recent_thoughts:
            if isinstance(thought, dict) and "inner_thoughts" in thought:
                thoughts_summary += f"-Player said: {thought['player_message']}\nI thought: {thought['inner_thoughts']}\n"
        
        player_name = "Stranger"
        for name, rel_data in self.relationships.items():
            if name != "Stranger":
                player_name = name
                break
        
        reflection_prompt = f"""As {self.name}, you must make a crucial decision about {player_name} after 8 conversations.

    Recent conversations:
    {convo_summary}

    Your inner thoughts about these exchanges:
    {thoughts_summary}

    You are {self.name}, a trader carrying vital information about High Priest Pamiu's conspiracy against the pharaoh. You've been rescued by {player_name} and have now spoken with them 8 times. You MUST decide if they can be trusted with your secret mission.

    This is your FINAL assessment. Consider:
    1. Have they shown genuine concern for your wellbeing?
    2. Do they ask suspicious questions about your past or mission?
    3. Do they seem to have knowledge they shouldn't have?
    4. Would they risk themselves to help you?
    5. Do they show respect for Ma'at (truth and justice)?
    6. Do they seem like they could be working for Pamiu?

    Based on ALL your interactions, you must choose ONE:
    - ALLY: They can be trusted with your secret scroll and will help deliver it to Kemet
    - ENEMY: They are likely working for your enemies or cannot be trusted with your mission

    You cannot remain undecided. Your life and Egypt's future depend on making this choice now.

    Your response must end with exactly: [DECISION: Ally] or [DECISION: Enemy]

    Format your response as:
    [REFLECTION: Your detailed analysis of all interactions and why you've reached this conclusion]
    [DECISION: Ally] or [DECISION: Enemy]
    """
        
        reflection_response = self.create_backbone(reflection_prompt, "user")
        
        decision_patterns = [
            r'\[DECISION:\s*(Ally|Enemy)\]', r'DECISION:\s*(Ally|Enemy)', r'Decision:\s*(Ally|Enemy)', r'\b(Ally|Enemy)\b(?=\s*$)'
        ]
        decision = None
        for pattern in decision_patterns:
            decision_match = re.search(pattern, reflection_response, re.IGNORECASE)
            if decision_match:
                decision = decision_match.group(1).strip()
                break
        
        if decision and decision.lower() in ["ally", "enemy"] and player_name in self.relationships:
            if self.relationships[player_name].get("type") == "Not Determined Yet":
                self.update_relationship_status(player_name, decision)
                print(f"DEBUG: Successfully updated {player_name}'s relationship status to: {decision}")
        
        self.save_in_memory_json("", reflection_response, "reflection")
        return reflection_response

    def check_for_suspicious(self, player_message):
        """Check if a player message seems suspicious based on NPC's background"""
        suspicious_prompt = f"""You are {self.name}, an ancient Egyptian trader who was rescued from the desert.
Your identity: {self.identity}
Your backstory: {self.backstory}
Your beliefs: {self.belief}
Your current situation: {self.status} 
analyze this message from someone who rescued you: "{player_message}"

Consider:
- Does it contradict known historical facts about ancient Egypt?
- Does it suggest knowledge that an ordinary person in this era shouldn't have?
- Does it show unusual interest in your mission or the scroll you're carrying?
- Does it suggest alignment with your enemies?

Is there anything suspicious about this message? If yes, explain why it's suspicious.
If no, simply state "Not suspicious."
"""
        response = self.create_backbone(message=suspicious_prompt, role="user")
        if "not suspicious" not in response.lower():
            self.save_in_memory_json(player_message, response, "suspicious")
            return response
        return None

    def generate_question(self, player_message):
        """Generate a question to learn more about the player or clarify suspicious points"""
        suspicious = self.data.get("suspicious", [])
        thoughts = self.data.get("inner_thoughts", [])
        conversations = self.data.get("Conversation_History", [])

        suspicious_context = ""
        if suspicious:
            for item in suspicious[-3:]:
                suspicious_context += f"Suspicious message: {item['suspicious_message']}\nReason: {item.get('reason', 'Unknown')}\n\n,ME:{item.get('npc_message')}\n"
        thoughts_context = ""
        if thoughts:
            for item in thoughts[-3:]:
                thoughts_context += f"- {item['inner_thoughts']}\n"
        conversations_context = ""
        if conversations:
            for item in conversations[-3:]:
                conversations_context += f"Player: {item['player_message']}\n,I said:{item['npc_message']}\n"

        prompt = f"""As {self.name}, generate a natural-sounding question to ask the player.

Your identity: {self.identity}
Your backstory: {self.backstory}
Your current goals: Learn more about this person and determine if they're trustworthy
Recent suspicious elements: {suspicious_context}
My recent inner thoughts: {thoughts_context}
Recent Elements of conversation: {conversations_context}
The player just said: "{player_message}"

Generate a single question that would:
1. Help determine if they can be trusted
2. Subtly probe for their intentions or allegiances
3. Seem natural in conversation, not like an interrogation
4. Possibly reveal if they're connected to your enemies
Return only the question, nothing else.
"""
        question = self.create_backbone(message=prompt, role="user")
        question = question.strip('"\'')
        self.save_in_memory_json(player_message, question, "question")
        return question

    def create_backbone(self, message, role='user', stream=False):
        """Make an API call to OpenAI for generating NPC responses"""
        try:
            # It's best practice to load the API key from an environment variable.
            # For this example, we'll keep your hardcoded key.
            client = OpenAI(api_key=os.environ.get("OPENAI_API_KEY", "sk-proj-oBIOgX0aO6YzaLlAldnpT3BlbkFJbdDbSEEoomYGNFVC9A2l"))
            
            completion = client.chat.completions.create(
                model="gpt-4o-mini",
                messages=[{"role": role, "content": message}],
                stream=stream  # Use the stream parameter here
            )

            if stream:
                # If streaming is enabled, return the generator object directly.
                return completion
            else:
                # If not streaming, return the complete response content as before.
                response = completion.choices[0].message.content
                return response
        except Exception as e:
            print(f"Error in API call: {e}")
            if stream:
                # In case of an error, we need to return a generator that yields an error message.
                def error_generator():
                    yield "Forgive me, my mind is clouded from the desert heat. Could you speak again?"
                return error_generator()
            return "Forgive me, my mind is clouded from the desert heat. Could you speak again?"

    # MODIFICATION: This function now returns the full reflection text for printing.
    def process_player_input(self, player_message):
        """Main method to process player input and generate NPC response"""
        if not player_message:
            response_stream = self.generate_response("", "", "")
            return "", response_stream
        
        if self.data.get('conversation_completed', False):
            decision = self.data.get('final_decision', 'UNKNOWN')
            return f"\n(System: The conversation with {self.name} has concluded. Final decision was {decision}.)", None
                
        if self.conversation_stage == "awaiting_name":
            possible_name = self.extract_name(player_message)
            if possible_name:
                self.modify_json_memory_for_protagonist(possible_name)
                print(f"\n(System: Player identified as {possible_name})")
                self.conversation_stage = "post_introduction"
        
        current_count = self.data.get('conversation_count', 0)
        print(f"DEBUG: Current conversation count: {current_count}")
        
        if current_count >= 7:
            print("DEBUG: Triggering final reflection...")
            reflection = self.generate_reflection() # The full reflection text is here
            if reflection:
                decision_patterns = [r'\[DECISION:\s*(Ally|Enemy)\]', r'DECISION:\s*(Ally|Enemy)', r'Decision:\s*(Ally|Enemy)', r'\b(Ally|Enemy)\b(?=\s*$)']
                decision = None
                for pattern in decision_patterns:
                    decision_match = re.search(pattern, reflection, re.IGNORECASE)
                    if decision_match:
                        decision = decision_match.group(1).strip().capitalize()
                        break
                
                if decision:
                    print(f"\n(System: NPC has decided the player is: {decision})")
                    _, final_response = self.generate_final_conversation(decision, player_message)
                    
                    # Save the final response to file
                    self.save_response_to_file(final_response.strip())
                    
                    # MODIFICATION: Combine the reflection and final response into one message for printing.
                    # This ensures the reflection is displayed to the user.
                    final_message_to_print = (
                        f"(Neferkare's Final Thoughts):\n{reflection.strip()}\n\n"
                        f"{self.name}:\n{final_response.strip()}"
                    )
                    
                    return final_message_to_print, "[CONVERSATION ENDED]"

        # Normal conversation flow
        inner_thoughts = self.generate_inner_thoughts(player_message)
        response_stream = self.generate_response(player_message, inner_thoughts)
        
        return inner_thoughts, response_stream
    
    def generate_final_conversation(self, decision, player_message):
        """
        Generates a new inner thought based on the final decision,
        then creates the final spoken response that pivots to the outcome.
        """
        player_name = "Stranger"
        for name in self.relationships:
            if name != "Stranger":
                player_name = name
                break

        if decision.lower() == "ally":
            thought_prompt = f"I have made my choice. {player_name} is worthy of my trust. Ma'at has guided me. I will give them the scroll and entrust them with my mission. Everything depends on this."
        else: # Enemy
            thought_prompt = f"My suspicions were correct. This person cannot be trusted. They might even be one of Pamiu's agents. I will give them false information to send them on a fool's errand and then disappear. I must protect the scroll at all costs."
        
        final_inner_thought = self.create_backbone(message=thought_prompt, role="assistant")

        if decision.lower() == "ally":
            final_prompt = f"""
            You are {self.name}. You have decided {player_name} is an ALLY.
            The player just said: "{player_message}"
            This is the FINAL conversation. You must now reveal your mission.
            **Task:**
            1.  Briefly respond to their message, but quickly pivot. Say something like, "Listen closely, my friend. I have made my decision."
            2.  Tell them you've decided to trust them with your life and the fate of Egypt.
            3.  Give them the sealed scroll you've been carrying (evidence of High Priest Pamiu's conspiracy).
            4.  Explain they must deliver it to your ally, Kemet, a scribe hiding in Memphis.
            5.  Warn them that Pamiu is powerful and has spies everywhere.
            6.  End by expressing your gratitude and hope, stating that Egypt's fate is now in their hands as you must rest.
            Your response should be emotional, grateful, and decisive. 4-6 sentences at most.
            """
        else:  # enemy
            final_prompt = f"""
            You are {self.name}. You have decided {player_name} is an ENEMY.
            The player just said: "{player_message}"
            This is the FINAL conversation. You must mislead them and end contact.
            **Task:**
            1.  Subtly acknowledge their last message, then pivot to concluding the conversation.
            2.  Thank them for saving your life, but say you must now go your separate ways to recover.
            3.  As a "parting gift", casually mention that you heard the ambitious High Priest Pamiu holds court in the Temple of Karnak and rewards those who bring him useful information. (This is misdirection).
            4.  DO NOT mention the scroll, Kemet, Memphis, or your true mission.
            5.  Politely but firmly end the conversation.
            Your response should be cautious and sound helpful on the surface, but be designed to mislead. 2-4 sentences at most.
            """
        
        final_response = self.create_backbone(message=final_prompt, role="user")
        
        # Save the final spoken response, which is what the NPC "said".
        # The reflection is already saved separately in generate_reflection().
        self.save_in_memory_json(player_message, final_response, "model_response")
        self.data['conversation_completed'] = True
        self.data['final_decision'] = decision
        self.save_json()
        
        return final_inner_thought, final_response

# NEW: A helper function to handle the streaming output and save the final result.
def handle_streaming_response(npc, player_input, inner_thoughts, response_stream):
    """Prints the response as it streams and saves the full content afterward."""
    print(f"\n{npc.name}:")
    if inner_thoughts:
        print(f"Thinking: {inner_thoughts}\n")
    
    print("Response: ", end='')
    
    full_response_parts = []
    try:
        for chunk in response_stream:
            content = chunk.choices[0].delta.content or ""
            print(content, end='', flush=True)
            full_response_parts.append(content)
        print() # Add a newline after the stream is complete
    except Exception as e:
        print(f"\nAn error occurred during streaming: {e}")

    # Assemble the complete response string from the streamed parts
    final_response = "".join(full_response_parts)

    # Now that the stream is finished and we have the full response, save it to memory.
    if final_response:
        npc.save_in_memory_json(player_input, final_response, "model_response")
        # Save the response to file (just the raw text, no NPC name)
        npc.save_response_to_file(final_response.strip())

# MODIFIED: The main loop now handles the final reflection printout correctly.
def run_npc_conversation():
    """Run an interactive conversation with the NPC"""
    npc = NPC(name="Neferkare")
    
    print("\n=== Ancient Egypt NPC Interaction ===")
    print("You've rescued a trader from the desert, and have now healed his leg.")
    print("Type your messages to interact, or 'quit' to exit.\n")
    
    player_name = next((name for name in npc.relationships if name != "Stranger"), "Stranger")
    
    print(f"*The trader, {npc.name}, approaches you*\n")
    
    _, initial_stream = npc.process_player_input("")
    if initial_stream:
        handle_streaming_response(npc, "", "", initial_stream)

    while True:
        player_name = next((name for name in npc.relationships if name != "Stranger"), "Stranger")
        player_input = input(f"\n{player_name}: ")
        
        if player_input.lower() in ['quit', 'exit']:
            break
            
        # Renamed 'inner_thoughts' to 'npc_output' for clarity, as it can contain more than just thoughts.
        npc_output, response_stream = npc.process_player_input(player_input)
        
        # Check for conversation end signal
        if response_stream == "[CONVERSATION ENDED]":
            # The 'npc_output' variable now holds the combined final reflection and response.
            print(f"\n{npc_output}")
            print("\n(The conversation has concluded. The program will now close.)")
            time.sleep(4)
            break
        
        if response_stream:
            # Pass the regular inner thoughts to the handler
            handle_streaming_response(npc, player_input, npc_output, response_stream)
        elif npc_output:
            print(f"\n{npc_output}")


if __name__ == "__main__":
    run_npc_conversation()