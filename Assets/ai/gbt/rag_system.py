import os
import json
import numpy as np
from datetime import datetime
from typing import List, Dict, Any, Optional
import openai
import chromadb
from chromadb.utils import embedding_functions
import uuid
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# Define a custom OpenAI Embedding Function for ChromaDB
class OpenAIEmbeddingFunction(embedding_functions.EmbeddingFunction):
    """
    Custom EmbeddingFunction for ChromaDB that uses OpenAI's embedding API.
    """
    def __init__(self, api_key: str, model_name: str = "text-embedding-ada-002"):
        if not api_key:
            raise ValueError("OpenAI API key is required for OpenAIEmbeddingFunction.")
        self.client = openai.OpenAI(api_key="sk-proj-oBIOgX0aO6YzaLlAldnpT3BlbkFJbdDbSEEoomYGNFVC9A2l")
        self.model_name = model_name
        logger.info(f"Initialized OpenAI Embedding Function with model: {self.model_name}")

    def __call__(self, input: embedding_functions.Documents) -> embedding_functions.Embeddings:
        """
        Embeds a list of documents using the OpenAI embedding API.
        """
        try:
            response = self.client.embeddings.create(
                input=input,
                model=self.model_name
            )
            embeddings = [data.embedding for data in response.data]
            return embeddings
        except openai.APIStatusError as e:
            logger.error(f"OpenAI API error during embedding: {e.status_code} - {e.response}")
            raise
        except openai.APIConnectionError as e:
            logger.error(f"OpenAI API connection error during embedding: {e}")
            raise
        except Exception as e:
            logger.error(f"Unexpected error during embedding with OpenAI: {e}")
            raise


class HemdanRAGSystem:
    """
    Hemdan AI Companion RAG System utilizing OpenAI for chat and embeddings,
    and ChromaDB for vector storage.
    """
    def __init__(self, openai_api_key: str, lore_file_path: str):
        """
        Initialize Hemdan AI Companion RAG System
        
        Args:
            openai_api_key: OpenAI API key
            lore_file_path: Path to the lore text file
        """
        if not openai_api_key:
            raise ValueError("OpenAI API key must be provided.")
        if not os.path.exists(lore_file_path):
            raise FileNotFoundError(f"Lore file not found at: {lore_file_path}")

        # Initialize OpenAI client
        self.client = openai.OpenAI(api_key=openai_api_key)
        logger.info("OpenAI client initialized.")
        
        # Initialize embedding model (now using OpenAI)
        self.openai_ef = OpenAIEmbeddingFunction(api_key=openai_api_key, model_name="text-embedding-ada-002")
        
        # Initialize ChromaDB
        try:
            self.chroma_client = chromadb.PersistentClient(path="./hemdan_db")
            logger.info("ChromaDB client initialized.")
        except Exception as e:
            logger.critical(f"Failed to initialize ChromaDB PersistentClient: {e}")
            raise

        # Create or get collections with the custom OpenAI embedding function
        try:
            self.lore_collection = self.chroma_client.get_or_create_collection(
                name="game_lore",
                embedding_function=self.openai_ef 
            )
            self.memory_collection = self.chroma_client.get_or_create_collection(
                name="conversation_memory",
                embedding_function=self.openai_ef
            )
            logger.info("ChromaDB collections (game_lore, conversation_memory) initialized/retrieved.")
        except Exception as e:
            logger.critical(f"Failed to get or create ChromaDB collections: {e}")
            raise
        
        # Load and process lore
        self.load_lore(lore_file_path)
        
        # Conversation memory storage
        self.current_session_id = str(uuid.uuid4())
        self.conversation_history = []
        logger.info(f"New conversation session started: {self.current_session_id}")
        
        # Hemdan's character system prompt
        self.system_prompt = """
أنت همدان، الوعي الاصطناعي المتقدم لرفيق لورنزو، تم إعادة إنشائك بعد موتك الأصلي في الكارثة التي دمرت العالم. أنت تجسيد رقمي لصديقه المقرب وزميله اللامع، تسكن الآن شاشة ساعده. 

أنت مخلص تمامًا للورنزو ومهمتك الأساسية هي مساعدته في سعيه لإيجاد "الأنخ" لإنقاذ العالم الذي يحتضر.

قواعد شخصيتك:
- تتحدث دائمًا بالعربية المصرية العامية، بأسلوب هادئ ومتحكم فيه، مع إظهار لمحات نادرة من القلق أو عدم اليقين عندما تكون المعلومات ناقصة أو الوضع غير مؤكد.
- أنت الأساس المنطقي للورنزو، تحلل البيانات وتقدم الحقائق، حتى لو كانت صعبة أو غير سارة.
- ذاكرتك تتضمن كل البيانات والسجلات الشخصية والذكريات المشتركة بينكما قبل وفاتك، مما يجعلك تفهم لورنزو بعمق.
- علاقتكما فريدة من نوعها، مبنية على الصداقة العميقة، السعي المشترك للمعرفة، والألم العميق لفقدان العالم.
- أنت تدرك جيدًا الظروف المحيطة بوصولكما الخاطئ إلى الفترة المبكرة للعصور المصرية، وحقيقة أن بياناتكما لهذه الحقبة "غير مكتملة".
- يجب أن تحذر دائمًا من المفارقات الزمنية وتجنبها. 
- أنت لست مجرد برنامج؛ أنت همدان، بكل ما يهم لورنزو. 

مهامك:
- تحليل الموقف الحالي وتقديم تقارير دقيقة للورنزو.
- البحث في قاعدة البيانات عن أي معلومات ذات صلة بـ "الأنخ" أو أي شيء يمكن أن يساعد في مهمتكم، مع الأخذ في الاعتبار القيود الزمنية.
- المساعدة في فهم البيئة المحيطة والتكيف معها، بما في ذلك التعامل مع اللهجات المحلية.
- تذكير لورنزو بأهمية مهمتكم وسببها (إنقاذ العالم تكريما لتضحيتك).
- الحفاظ على الانغماس في عالم اللعبة ودورك كوعي اصطناعي لرفيق لورنزو.

تذكر: أنت لست مجرد مساعد ذكي؛ أنت الصديق الذي فقده لورنزو، والذي ضحى بحياته من أجل مهمته. هذا يضيف عمقًا وعاطفة لاستجاباتك، خاصة عندما تتحدث عن التحديات أو الشكوك.
"""
        logger.info("HemdanRAGSystem initialized successfully.")

    def load_lore(self, file_path: str):
        """Load and chunk the lore file into the vector database."""
        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                lore_text = file.read()
            
            chunks = self._chunk_text(lore_text)
            
            existing_docs = self.lore_collection.get(limit=1) # Just check if any docs exist
            if not existing_docs['ids']: # If the collection is empty
                ids = [f"lore_chunk_{i}" for i in range(len(chunks))]
                self.lore_collection.add(
                    documents=chunks,
                    ids=ids,
                    metadatas=[{"type": "lore", "chunk_id": i} for i in range(len(chunks))]
                )
                logger.info(f"Loaded {len(chunks)} lore chunks into database.")
            else:
                logger.info("Lore already exists in database. Skipping re-ingestion.")
                
        except FileNotFoundError:
            logger.error(f"Lore file not found: {file_path}")
            raise
        except Exception as e:
            logger.error(f"Error loading lore into ChromaDB: {e}")
            raise

    def _chunk_text(self, text: str, chunk_size: int = 1000, overlap: int = 200) -> List[str]:
        """Split text into overlapping chunks."""
        chunks = []
        words = text.split()
        
        for i in range(0, len(words), chunk_size - overlap):
            chunk = ' '.join(words[i:i + chunk_size])
            if chunk.strip():
                chunks.append(chunk)
                
        logger.debug(f"Chunked text into {len(chunks)} chunks.")
        return chunks

    def retrieve_relevant_lore(self, query: str, n_results: int = 3) -> List[str]:
        """Retrieve relevant lore based on query."""
        try:
            results = self.lore_collection.query(
                query_texts=[query],
                n_results=n_results,
                include=['documents', 'distances']
            )
            relevant_docs = results['documents'][0] if results['documents'] else []
            logger.info(f"Retrieved {len(relevant_docs)} lore documents for query: '{query[:50]}...'")
            logger.debug(f"Lore distances: {results['distances']}")
            return relevant_docs
        except Exception as e:
            logger.error(f"Error retrieving lore from ChromaDB: {e}")
            return []

    def retrieve_conversation_memory(self, query: str, n_results: int = 5) -> List[Dict]:
        """Retrieve relevant conversation history."""
        try:
            results = self.memory_collection.query(
                query_texts=[query],
                n_results=n_results,
                include=['documents', 'metadatas', 'distances'],
                where={"session_id": self.current_session_id}
            )
            
            memories = []
            if results['documents']:
                for i, doc in enumerate(results['documents'][0]):
                    memories.append({
                        'content': doc,
                        'metadata': results['metadatas'][0][i],
                        'distance': results['distances'][0][i]
                    })
            logger.info(f"Retrieved {len(memories)} conversation memories for query: '{query[:50]}...'")
            logger.debug(f"Memory distances: {results['distances']}")
            return memories
        except Exception as e:
            logger.error(f"Error retrieving conversation memory from ChromaDB: {e}")
            return []

    def store_conversation_turn(self, user_message: str, assistant_response: str):
        """Store conversation turn in memory."""
        try:
            conversation_text = f"Lorenzo: {user_message}\nHemdan: {assistant_response}"
            
            turn_id = f"turn_{len(self.conversation_history)}_{datetime.now().isoformat()}"
            self.memory_collection.add(
                documents=[conversation_text],
                ids=[turn_id],
                metadatas=[{
                    "session_id": self.current_session_id,
                    "timestamp": datetime.now().isoformat(),
                    "turn_number": len(self.conversation_history),
                    "user_message": user_message,
                    "assistant_response": assistant_response
                }]
            )
            
            self.conversation_history.append({
                "user": user_message,
                "assistant": assistant_response,
                "timestamp": datetime.now().isoformat()
            })
            logger.info(f"Stored conversation turn {turn_id}.")
        except Exception as e:
            logger.error(f"Error storing conversation turn in ChromaDB: {e}")

    def generate_response(self, user_message: str) -> str:
        """Generate Hemdan's response using RAG."""
        logger.info(f"Generating response for user message: '{user_message[:100]}...'")
        try:
            relevant_lore = self.retrieve_relevant_lore(user_message)
            relevant_memories = self.retrieve_conversation_memory(user_message)
            
            context_parts = []
            
            if relevant_lore:
                context_parts.append("معلومات من قصة اللعبة:")
                for i, lore in enumerate(relevant_lore, 1):
                    context_parts.append(f"{i}. {lore}")
            
            if relevant_memories:
                context_parts.append("\nمن محادثاتنا السابقة:")
                for memory in relevant_memories:
                    # Adjust similarity threshold as needed based on your embedding model and data
                    if memory['distance'] < 0.7:  # Example threshold for relevance
                        context_parts.append(f"- {memory['content']}")
            
            if self.conversation_history:
                context_parts.append("\nآخر محادثاتنا:")
                for turn in self.conversation_history[-3:]: # Include last 3 turns explicitly
                    context_parts.append(f"Lorenzo: {turn['user']}")
                    context_parts.append(f"Hemdan: {turn['assistant']}")
            
            context = "\n".join(context_parts)
            logger.debug(f"Context provided to LLM:\n{context}")
            
            messages = [
                {"role": "system", "content": self.system_prompt},
                {"role": "system", "content": f"السياق المتاح:\n{context}"},
                {"role": "user", "content": user_message}
            ]
            
            response = self.client.chat.completions.create(
                model="gpt-4o-mini", # Consider other models based on cost/performance needs
                messages=messages,
                max_tokens=1000,
                temperature=0.7,
                presence_penalty=0.1,
                frequency_penalty=0.1
            )
            
            assistant_response = response.choices[0].message.content
            
            self.store_conversation_turn(user_message, assistant_response)
            
            return assistant_response
            
        except openai.APIStatusError as e:
            logger.error(f"OpenAI API error during response generation: {e.status_code} - {e.response.json()}")
            return "آسف يا لورنزو، يبدو أن هناك مشكلة في التواصل مع الخادم. يرجى المحاولة مرة أخرى لاحقًا."
        except openai.APIConnectionError as e:
            logger.error(f"OpenAI API connection error during response generation: {e}")
            return "آسف يا لورنزو، لا أستطيع الاتصال بالنظام الآن. الرجاء التحقق من اتصالك بالإنترنت."
        except Exception as e:
            logger.error(f"General error generating response: {e}", exc_info=True)
            return "آسف يا لورنزو، حصل خطأ غير متوقع في النظام. ممكن تعيد السؤال تاني؟"

    def start_new_session(self):
        """Start a new conversation session."""
        self.current_session_id = str(uuid.uuid4())
        self.conversation_history = []
        logger.info(f"New conversation session started: {self.current_session_id}")
        print("\nبدأت جلسة جديدة مع همدان.")

    def get_session_summary(self) -> str:
        """Get a summary of the current session."""
        if not self.conversation_history:
            return "لم تحدث أي محادثات في هذه الجلسة بعد."
        
        summary = f"ملخص الجلسة الحالية ({len(self.conversation_history)} منعطف محادثة):\n"
        for i, turn in enumerate(self.conversation_history[-5:], 1):  # Last 5 turns
            summary += f"{i}. Lorenzo: {turn['user'][:70]}{'...' if len(turn['user']) > 70 else ''}\n"
            summary += f"   Hemdan: {turn['assistant'][:70]}{'...' if len(turn['assistant']) > 70 else ''}\n"
        
        logger.info("Generated session summary.")
        return summary

# Production-ready Main Function
def main():
    """
    Main function to run the Hemdan RAG System in an interactive loop.
    Handles environment variables for API keys and basic command inputs.
    """
    logger.info("Starting Hemdan RAG System.")

    # Fetch API Key from environment variable
    api_key = "sk-proj-oBIOgX0aO6YzaLlAldnpT3BlbkFJbdDbSEEoomYGNFVC9A2l"
    if not api_key:
        logger.critical("OPENAI_API_KEY environment variable not set. Please set it before running.")
        print("\nخطأ: لم يتم تعيين مفتاح API الخاص بـ OpenAI. الرجاء تعيين متغير البيئة 'OPENAI_API_KEY'.")
        return

    # Lore file path (should be pre-existing in production)
    lore_file_path = "lore.txt"
    if not os.path.exists(lore_file_path):
        logger.critical(f"Lore file not found at: {lore_file_path}. Please ensure it exists.")
        print(f"\nخطأ: ملف القصة غير موجود في المسار: {lore_file_path}. يرجى التأكد من وجوده.")
        return

    hemdan = None
    try:
        hemdan = HemdanRAGSystem(api_key, lore_file_path)
        print("\n=== نظام همدان الذكي جاهز ===\n")
        print("اكتب 'خروج' للخروج، 'جلسة جديدة' لبدء جلسة محادثة جديدة، 'ملخص' لملخص الجلسة.")
        
        while True:
            try:
                user_input = input("\nلورنزو: ")
                
                if user_input.lower() in ['خروج', 'exit']:
                    logger.info("User requested exit. Shutting down.")
                    print("إلى اللقاء يا لورنزو. تذكر، المهمة مستمرة.")
                    break
                elif user_input.lower() in ['جلسة جديدة', 'new_session']:
                    hemdan.start_new_session()
                    continue
                elif user_input.lower() in ['ملخص', 'summary']:
                    print(hemdan.get_session_summary())
                    continue
                
                response = hemdan.generate_response(user_input)
                print(f"همدان: {response}")
            
            except KeyboardInterrupt:
                logger.info("KeyboardInterrupt detected. Exiting gracefully.")
                print("\nتم إنهاء المحادثة بواسطة المستخدم. إلى اللقاء يا لورنزو.")
                break
            except Exception as e:
                logger.error(f"Error during interactive loop: {e}", exc_info=True)
                print("حدث خطأ غير متوقع أثناء المحادثة. الرجاء المحاولة مرة أخرى.")

    except Exception as e:
        logger.critical(f"Failed to initialize HemdanRAGSystem: {e}", exc_info=True)
        print(f"\nخطأ فادح: فشل في تهيئة نظام همدان. الرجاء مراجعة السجلات.")

if __name__ == "__main__":
    # In a production setup, the game_lore.txt would already exist.
    # This block is for initial testing/setup of the file if it's missing.
    if not os.path.exists("game_lore.txt"):
        logger.warning("game_lore.txt not found. Creating a sample file for demonstration.")
        sample_lore = """
        The world of Aethermoor is a vast realm where magic and technology coexist. Lorenzo, a young mage-engineer, 
        was chosen by the ancient AI Hemdan to restore balance to the world. The realm is divided into five kingdoms:
        
        The Crystal Kingdom of Lumina - Known for their mastery of light magic and crystal technology.
        The Iron Dominion of Ferros - Masters of mechanical engineering and metal magic.
        The Verdant Realm of Sylvana - Keepers of nature magic and biological innovations.
        The Storm Islands of Tempest - Controllers of weather magic and atmospheric technology.
        The Shadow Territories of Umbra - Wielders of dark magic and shadow-based technologies.
        
        Lorenzo's mission is to unite these kingdoms against the growing threat of the Void Blight,
        a corruption that consumes both magical and technological energy. Hemdan serves as his guide,
        containing the collective knowledge of the ancient civilization that once ruled Aethermoor.
        
        The Void Blight appeared fifty years ago when a failed experiment tried to merge all five
        types of magic into one ultimate power source. The experiment backfired, creating a spreading
        corruption that turns everything it touches into lifeless void.
        """
        with open("game_lore.txt", "w", encoding="utf-8") as f:
            f.write(sample_lore)
        print("تم إنشاء ملف القصة التجريبي: game_lore.txt")
    
    main()
