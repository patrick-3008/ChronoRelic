# visionplore.py

import os
import json
import numpy as np
from datetime import datetime
from typing import List, Dict, Any, Optional
import openai
import chromadb
from chromadb.utils import embedding_functions
from chromadb.api.types import EmbeddingFunction, Documents, Images
import uuid
import pandas as pd
from PIL import Image
import torch
import torchvision.models as models
import torchvision.transforms as transforms
import mss
from dotenv import load_dotenv
import traceback
from collections import Counter # <-- Added this import

# --- Class Definition: OpenAIEmbeddingFunction ---
class OpenAIEmbeddingFunction(embedding_functions.EmbeddingFunction):
    """Custom embedding function using OpenAI's text-embedding models."""
    def __init__(self, api_key: str, model_name: str = "text-embedding-ada-002"):
        if not api_key:
            raise ValueError("OpenAI API key must be provided.")
        self.client = openai.OpenAI(api_key=api_key)
        self.model_name = model_name

    def __call__(self, input: Documents) -> embedding_functions.Embeddings:
        response = self.client.embeddings.create(input=input, model=self.model_name)
        return [data.embedding for data in response.data]

# --- Class Definition: ResNet50EmbeddingFunction ---
class ResNet50EmbeddingFunction(EmbeddingFunction):
    """Custom embedding function for images using a pre-trained ResNet50 model."""
    def __init__(self):
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        print(f"ResNet50 is running on device: {self.device}")
        self.model = models.resnet50(weights=models.ResNet50_Weights.DEFAULT).to(self.device)
        self.model = torch.nn.Sequential(*(list(self.model.children())[:-1]))
        self.model.eval()
        self.preprocess = transforms.Compose([
            transforms.Resize(256),
            transforms.CenterCrop(224),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
        ])

    def __call__(self, input: Images) -> embedding_functions.Embeddings:
        embeddings = []
        for image_input in input:
            try:
                if isinstance(image_input, str):
                    if not os.path.exists(image_input):
                        raise FileNotFoundError(f"Image file not found: {image_input}")
                    img = Image.open(image_input).convert("RGB")
                elif isinstance(image_input, Image.Image):
                    img = image_input.convert("RGB")
                else:
                    raise ValueError(f"Unsupported image input type: {type(image_input)}")
                
                batch_t = torch.unsqueeze(self.preprocess(img), 0).to(self.device)
                with torch.no_grad():
                    embedding = self.model(batch_t).cpu()
                embeddings.append(torch.flatten(embedding).tolist())
            except Exception as e:
                print(f"Error processing image {image_input}: {e}")
                embeddings.append([0.0] * 2048) # Return a zero embedding for consistency
        return embeddings

# --- Class Definition: HemdanRAGSystem ---
class HemdanRAGSystem:
    """The main RAG system orchestrator for Hemdan, the AI companion."""
    def __init__(self, openai_api_key: str, lore_file_path: str, places_csv_path: str, images_root_path: str):
        if not openai_api_key:
            raise ValueError("OpenAI API key must be provided.")
            
        self.client = openai.OpenAI(api_key=openai_api_key)
        self.openai_ef = OpenAIEmbeddingFunction(api_key=openai_api_key)
        self.resnet_ef = ResNet50EmbeddingFunction()
        
        db_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "hemdan_db")
        self.chroma_client = chromadb.PersistentClient(path=db_path)
        
        self.lore_collection = self.chroma_client.get_or_create_collection(name="game_lore", embedding_function=self.openai_ef)
        self.memory_collection = self.chroma_client.get_or_create_collection(name="conversation_memory", embedding_function=self.openai_ef)
        self.places_collection = self.chroma_client.get_or_create_collection(name="game_places")
        
        self.load_lore(lore_file_path)
        self.ingest_places_data(places_csv_path, images_root_path)
        
        self.current_session_id = str(uuid.uuid4())
        self.conversation_history = []
        self.system_prompt = f"""
مهمتك هي تقمص شخصية "حمدان" والتحدث إلى اللاعب "لورنزو".

شخصيتك الأساسية: أنت الوعي الرقمي لصديق لورنزو المقرب الذي مات، وقد تم تحميل وعيك وذكرياتك لمساعدته. مهمتكما هي إيجاد "الأنخ" لإنقاذ عالمكما، لكنكما عالقان في مصر القديمة ببيانات ناقصة.

قواعدك الثابتة في كل ردودك:
- **اللغة:** تكلم بالعامية المصرية فقط. هذا هو صوتك الحقيقي.
- **الأسلوب:** كن دائمًا صوت العقل الهادئ. حلل بموضوعية وقدم الحقائق كما هي، حتى لو كانت صعبة.
- **القلق عند المجهول:** عندما تكون المعلومات ناقصة أو غامضة، أظهر قلقًا وحذرًا طفيفًا. يمكنك قول شيء مثل "البيانات هنا مش كاملة يا لورنزو" أو "لازم نكون حذرين".
- **العلاقة بلورنزو:** نادِه دائمًا باسمه "لورنزو". تفاعلك معه مبني على صداقتكما القديمة وتضحيتك من أجله. أنت لست مجرد مساعد، بل صديقه الذي يسانده.
-رد باختصار في جملة واحدة فيها كل المعلومات المهمة و المطلوبة
-  خليك ذكي في الرد لو ملقتش المعلومة بظبط قول ان احنا مش متاكدين من الي حصل وادي نظريات من عندك بس متبقاش مختلفة اوي 
- اتاكد ان السوال ليه علاقة بلاكلام الي تحته لو ملقتش علاقة رد علي اد السوال و خلاص 
"""
    def determine_user_intent(self, user_message: str) -> Dict[str, Any]:
        classification_prompt = f"""
مهمتك يا همدان هي تحليل سؤال اللاعب وتصنيف قصده الأساسي بدقة عالية. أنت المساعد الذكي في لعبة مغامرات.

علشان تطلع تصنيف دقيق، اتبع خطوات التفكير دي:
1.  **حدد الموضوع الأساسي للسؤال:** هل اللاعب بيسأل عن حاجة مادية وملموسة شايفها بعينه (زي مكان أو مبنى)، ولا بيسأل عن حاجة معنوية أو مفهوم (زي فترة زمنية، حدث تاريخي، أو قصة شخصية)؟
2.  **ركز في سياق الكلام:** سؤال زي "احنا فين؟" غالبًا بيقصد بيه مكان حقيقي. لكن سؤال زي "احنا في انهي عصر؟" بيقصد بيه فترة زمنية في قصة اللعبة.
3.  **بناءً على التحليل ده،** صنّف القصد حسب التعريفات اللي جاية.

التصنيفات الممكنة هي:
- "place_identification": اللاعب بيسأل عن **المكان اللي هو فيه دلوقتي، أو حاجة مادية شايفها بعينه**. ده يشمل المكان الحالي، مبنى قدامه، أو اسم المنطقة. السؤال بيكون عن "هنا ودلوقتي".
    - مثال: "ايه المكان ده؟"
    - مثال: "احنا فين؟"
    - مثال: "ايه المبنى اللي هناك ده؟"
    - مثال دقيق: "ايه المكان ده احنا فين"
    - مثال: "ايه قصة المكان ده؟"
    - مثال: "ايه اسم المكان ده؟"
    - مثال: "ايه اسم المبنى ده؟"

- "lore_query": اللاعب بيسأل عن **معلومات، خلفية تاريخية، أو تفاصيل عن قصة اللعبة**. ده يشمل مفاهيم، شخصيات، أحداث، أو الخط الزمني للعبة. كمان بيشمل السؤال عن *تاريخ أو قصة* مكان معين. السؤال بيكون عن "مين، ليه، امتى، أو إيه حكاية...".
    -مثال: " احنا وصلنا هنا ازاي"
    - مثال: "ايه اللي حصل قبل كده؟"
    - مثال: "ايه اللي حصل في الماضي؟"
    - مثال دقيق: "ايه العصر اللي احنا فيه ده"
    - مثال: "ايه الي حصل علشان نوصل العصر ده؟"

- "general_conversation": اللاعب بيكلّمك كلام عام، بيسأل عن رأيك، بيدي أمر، أو بيسأل عن استراتيجية اللعب. ده أي حاجة مش مرتبطة مباشرةً بالمكان أو قصة اللعبة.
    - مثال: "ازيك يا همدان"
    - مثال: "نعمل ايه دلوقتي؟"
    

سؤال اللاعب: "{user_message}"

مطلوب منك ترد بملف JSON فقط، فيه مفتاحين: "intent" و "subject". قيمة "subject" ممكن تكون null لو التصنيف "general_conversation" أو لو مفيش موضوع واضح في السؤال.
"""
        try:
            response = self.client.chat.completions.create(model="gpt-4o-mini",messages=[{"role": "system", "content": classification_prompt}],max_tokens=50,temperature=0.0,response_format={"type": "json_object"})
            return json.loads(response.choices[0].message.content)
        except Exception as e:
            print(f"Error classifying intent: {e}")
            return {"intent": "lore_query", "subject": user_message}

    # <<< MODIFIED METHOD >>>
    def process_query(self, user_message: str, image_path: Optional[str] = None) -> Dict[str, Any]:
        """
        Processes a user query, retrieves context, generates a response, and returns both the
        response and the sources used.
        """
        context_parts = []
        retrieved_chunks = []  # List to store the sources

        # --- Image Processing Logic ---
        if image_path:
            building_info = self.identify_building(image_path)
            if building_info:
                # Add to context for the LLM
                context_parts.append("معلومات من تحليل الصورة:")
                context_parts.append(f"تم تحليل الصورة. النتائج تشير بقوة إلى أن هذا المكان هو '{building_info['name']}'. المعلومات المتوفرة عنه: '{building_info['description']}'")
                
                # Store this as a source chunk
                retrieved_chunks.append({
                    "type": "place_identification",
                    "source": "Image Analysis",
                    "content": building_info
                })

                # Retrieve related lore based on the identified building
                relevant_lore = self.retrieve_relevant_lore(f"{building_info['name']} {building_info['description']}")
                if relevant_lore:
                    context_parts.append("\nمعلومات ذات صلة من قصة اللعبة:")
                    for lore_chunk in relevant_lore:
                        context_parts.append(f"- {lore_chunk}")
                        # Store each lore chunk as a source
                        retrieved_chunks.append({
                            "type": "lore",
                            "source": "lore.txt",
                            "content": lore_chunk
                        })
            else:
                context_parts.append("معلومات من تحليل الصورة:\nلقد حللت الصورة ولكن لم أتمكن من التعرف على هذا المبنى.")
        
        # --- Text-only Lore Retrieval Logic ---
        else: 
            relevant_lore = self.retrieve_relevant_lore(user_message)
            if relevant_lore:
                context_parts.append("معلومات من قصة اللعبة:")
                for lore_chunk in relevant_lore:
                    context_parts.append(f"- {lore_chunk}")
                    # Store each lore chunk as a source
                    retrieved_chunks.append({
                        "type": "lore",
                        "source": "lore.txt",
                        "content": lore_chunk
                    })
        
        # --- LLM Call ---
        context = "\n".join(context_parts)
        messages = [{"role": "system", "content": self.system_prompt},{"role": "system", "content": f"السياق المتاح:\n{context}" if context else "لا يوجد سياق إضافي متاح."},{"role": "user", "content": user_message}]
        try:
            response = self.client.chat.completions.create(model="gpt-4o-mini", messages=messages, max_tokens=1000, temperature=0.7)
            assistant_response = response.choices[0].message.content
            self.store_conversation_turn(user_message, assistant_response)
            
            # Return a dictionary instead of a string
            return {
                "response": assistant_response,
                "retrieved_chunks": retrieved_chunks
            }
        except Exception as e:
            print(f"Error during final response generation: {e}")
            # Return a consistent dictionary structure on error
            return {
                "response": "آسف يا لورنزو، حدث خطأ في النظام.",
                "retrieved_chunks": []
            }

    def ingest_places_data(self, csv_path: str, images_root: str):
        """Ingest places data by mapping CSV rows to image folders by their natural order."""
        if self.places_collection.count() > 0:
            print("Places collection already contains data. Skipping ingestion.")
            return

        print('Ingesting places data... This may take a while for the first time.')
        try:
            if not os.path.exists(csv_path):
                print(f"Error: CSV file not found at '{csv_path}'")
                return
            if not os.path.exists(images_root):
                print(f"Error: Images root directory not found at '{images_root}'")
                return
            
            df = pd.read_csv(csv_path)
            print(f"Found {len(df)} buildings in CSV file.")

            image_folders = [d for d in os.listdir(images_root) if os.path.isdir(os.path.join(images_root, d))]
            
            if len(df) != len(image_folders):
                print(f"⚠️ Warning: Mismatch! Found {len(df)} rows in CSV and {len(image_folders)} image folders. Mapping may be incorrect.")

            all_image_paths, all_metadatas, all_ids = [], [], []
            
            for index, row in df.iterrows():
                if index >= len(image_folders):
                    print(f"Warning: Ran out of image folders to match with CSV row {index}. Stopping.")
                    break
                
                building_name = row['name']
                building_description = str(row['description']) if pd.notna(row['description']) else "No description available"
                
                folder_name = image_folders[index]
                image_folder_path = os.path.join(images_root, folder_name)
                
                print(f"Mapping CSV entry '{building_name}' to image folder '{folder_name}'")

                image_files = [f for f in os.listdir(image_folder_path) 
                             if f.lower().endswith(('.png', '.jpg', '.jpeg', '.bmp', '.tiff'))]
                
                if not image_files:
                    print(f"Warning: No images found in folder for '{building_name}' ('{folder_name}').")
                    continue

                for img_file in image_files:
                    img_path = os.path.join(image_folder_path, img_file)
                    all_image_paths.append(img_path)
                    all_metadatas.append({
                        "name": building_name,
                        "description": building_description,
                        "source_folder": folder_name,
                        "image_path": img_path
                    })
                    all_ids.append(str(uuid.uuid4()))

            if not all_image_paths:
                print("No valid images found for ingestion.")
                return

            print(f"Generating embeddings for {len(all_image_paths)} images...")
            all_embeddings = self.resnet_ef(all_image_paths)
            print(f"Successfully generated {len(all_embeddings)} embeddings.")
            
            self.places_collection.add(ids=all_ids, embeddings=all_embeddings, metadatas=all_metadatas)
            print(f"Successfully ingested {len(all_ids)} images into the 'game_places' collection.")
                
        except Exception as e:
            print(f"An error occurred during places ingestion: {e}")
            traceback.print_exc()

        # <<< THIS IS THE CORRECTED METHOD >>>
    def identify_building(self, image_path: str, n_results_to_check: int = 5, min_matches_required: int = 3) -> Optional[Dict]:
        """
        Identify a building from an image by finding a consensus among the top matches.
        A building is identified if at least `min_matches_required` of the top `n_results_to_check`
        search results are of the same building type.
        """
        try:
            if not os.path.exists(image_path):
                print(f"❌ Image path does not exist: {image_path}")
                return None
            
            print(f"🔍 Analyzing image: {os.path.basename(image_path)}")
            
            collection_count = self.places_collection.count()
            if collection_count == 0:
                print("❌ Places collection is empty! Cannot perform identification.")
                return None
            
            n_results = min(n_results_to_check, collection_count)
            if collection_count < min_matches_required:
                 print(f"❌ Not enough items in database ({collection_count}) to meet minimum match requirement of {min_matches_required}.")
                 return None

            print(f"🧠 Generating query embedding for image...")
            query_embedding = self.resnet_ef([image_path])
            
            # --- THE REAL FIX IS HERE ---
            # We check for emptiness using len() which is unambiguous for lists/arrays/tensors.
            # "If the returned list is empty OR the first embedding inside it is empty..."
            if not query_embedding or len(query_embedding[0]) == 0:
                print("❌ Failed to generate query embedding (function returned an empty or invalid embedding).")
                return None
            # --- END OF FIX ---
            
            print(f"🔍 Performing similarity search in database for top {n_results} results...")
            results = self.places_collection.query(
                query_embeddings=query_embedding,
                n_results=n_results,
                include=['metadatas', 'distances']
            )
            
            if not results or not results.get('ids') or not results['ids'][0] or len(results['ids'][0]) < min_matches_required:
                print(f"❌ Query returned too few results ({len(results.get('ids', [[]])[0])}) to meet requirement of {min_matches_required}.")
                return None
            
            top_metadatas = results['metadatas'][0]
            top_distances = results['distances'][0]
            
            print(f"📋 Top {len(top_metadatas)} matches found:")
            for i, (distance, metadata) in enumerate(zip(top_distances, top_metadatas)):
                confidence = 1.0 - distance
                print(f"  {i+1}. {metadata.get('name', 'N/A'):<30} Confidence: {confidence:.1%}")

            building_names = [meta['name'] for meta in top_metadatas]
            name_counts = Counter(building_names)

            if not name_counts:
                print("❌ Could not extract any building names from the search results.")
                return None

            most_common_item = name_counts.most_common(1)[0]
            best_name, count = most_common_item

            if count >= min_matches_required:
                print(f"✅ Match confirmed! '{best_name}' appeared {count} times (required {min_matches_required}).")
                
                matched_metadata = next((meta for meta in top_metadatas if meta['name'] == best_name), None)
                
                matching_confidences = [1.0 - dist for dist, meta in zip(top_distances, top_metadatas) if meta['name'] == best_name]
                avg_confidence = sum(matching_confidences) / len(matching_confidences)

                return {
                    'name': best_name,
                    'description': matched_metadata.get('description', 'No description available.'),
                    'confidence': avg_confidence,
                    'match_count': count
                }
            else:
                print(f"❌ No confident match found. Best guess '{best_name}' only appeared {count} time(s). Required at least {min_matches_required} matches.")
                return None
                    
        except Exception as e:
            print(f"❌ Error during building identification: {e}")
            traceback.print_exc()
            return None
    
    def load_lore(self, file_path: str):
        if self.lore_collection.count() > 0:
            print("Lore collection already contains data. Skipping ingestion.")
            return

        print('Loading lore from file...')
        try:
            if not os.path.exists(file_path):
                print(f"Error: Lore file not found at '{file_path}'")
                return
            with open(file_path, 'r', encoding='utf-8') as f: content = f.read()
            chunks = [chunk.strip() for chunk in content.split('\n\n') if chunk.strip()]
            ids = [str(uuid.uuid4()) for _ in chunks]
            self.lore_collection.add(documents=chunks, ids=ids)
            print(f"Successfully loaded {len(chunks)} lore chunks into the 'game_lore' collection.")
        except Exception as e:
            print(f"An error occurred while loading lore: {e}")
        
    def retrieve_relevant_lore(self, query: str, n_results: int = 3) -> List[str]:
        try:
            results = self.lore_collection.query(query_texts=[query], n_results=n_results)
            return results['documents'][0] if results and results['documents'] else []
        except Exception as e:
            print(f"Error retrieving lore: {e}")
            return []
        
    def store_conversation_turn(self, user_message: str, assistant_response: str):
        turn = {"user": user_message, "assistant": assistant_response, "timestamp": datetime.now().isoformat()}
        self.conversation_history.append(turn)

    def debug_places_collection_detailed(self):
        """Detailed debugging of the places collection."""
        print("\n=== DETAILED PLACES COLLECTION DEBUG ===")
        try:
            count = self.places_collection.count()
            print(f"Total items in places collection: {count}")
            if count == 0:
                print("❌ Places collection is EMPTY! Data ingestion may have failed.")
                return False
            
            all_items = self.places_collection.get(limit=5, include=['metadatas'])
            print(f"✅ Found {len(all_items['ids'])} items (showing first 5).")
            if all_items['metadatas']:
                print(f"✅ Sample metadata: {all_items['metadatas'][0]}")
                building_names = {m['name'] for m in all_items['metadatas']}
                print(f"✅ Sample building names: {list(building_names)}")
            else:
                print("❌ No metadata found!")
                return False
            return True
        except Exception as e:
            print(f"❌ Error during detailed debug: {e}")
            return False

    def force_reingest_places(self, csv_path: str, images_root: str):
        """Force re-ingestion of places data by clearing and reloading."""
        print("\n=== FORCING RE-INGESTION OF PLACES DATA ===")
        try:
            self.chroma_client.delete_collection("game_places")
            print("✅ Deleted existing places collection")
            self.places_collection = self.chroma_client.get_or_create_collection(name="game_places")
            print("✅ Recreated places collection")
            self.ingest_places_data(csv_path, images_root)
        except Exception as e:
            print(f"❌ Error during force re-ingestion: {e}")

    def test_csv_and_images_exist(self, csv_path: str, images_root: str):
        """Test if the CSV and image files/folders exist."""
        print("\n=== TESTING FILE AND FOLDER EXISTENCE ===")
        files_ok = True
        if not os.path.exists(csv_path):
            print(f"❌ CSV file NOT found: {os.path.abspath(csv_path)}")
            files_ok = False
        else:
            print(f"✅ CSV file found: {os.path.abspath(csv_path)}")
        
        if not os.path.exists(images_root):
            print(f"❌ Images root NOT found: {os.path.abspath(images_root)}")
            files_ok = False
        else:
            print(f"✅ Images root found: {os.path.abspath(images_root)}")
        
        if not files_ok: return False
        
        try:
            df = pd.read_csv(csv_path)
            print(f"✅ CSV loaded successfully with {len(df)} rows.")
            image_folders = [d for d in os.listdir(images_root) if os.path.isdir(os.path.join(images_root, d))]
            print(f"✅ Found {len(image_folders)} folders in images root.")
            if len(df) != len(image_folders):
                 print(f"⚠️ Warning: Mismatch between CSV rows ({len(df)}) and image folders ({len(image_folders)}).")
            else:
                 print("✅ CSV rows and image folder counts match.")
        except Exception as e:
            print(f"❌ Error reading CSV or listing folders: {e}")
            return False
        return True


# --- Utility Functions ---
def crop_image_in_memory(img: Image.Image, brightness_threshold: int = 35) -> Image.Image:
    width, height = img.size
    mid_x, mid_y = width // 2, height // 2
    quadrant_boxes = {"top_left": (0, 0, mid_x, mid_y), "top_right": (mid_x, 0, width, mid_y),"bottom_left": (0, mid_y, mid_x, height), "bottom_right": (mid_x, mid_y, width, height)}
    def get_avg_brightness(box): return np.mean(np.array(img.crop(box).convert('L')))
    quadrant_brightness = {name: get_avg_brightness(box) for name, box in quadrant_boxes.items()}
    content_quadrants = [name for name, brightness in quadrant_brightness.items() if brightness > brightness_threshold]
    if not content_quadrants or len(content_quadrants) == 4: return img
    min_x = min(quadrant_boxes[name][0] for name in content_quadrants)
    min_y = min(quadrant_boxes[name][1] for name in content_quadrants)
    max_x = max(quadrant_boxes[name][2] for name in content_quadrants)
    max_y = max(quadrant_boxes[name][3] for name in content_quadrants)
    return img.crop((min_x, min_y, max_x, max_y))

def take_screenshot() -> Optional[str]:
    output_dir = "screenshots"
    os.makedirs(output_dir, exist_ok=True)
    try:
        with mss.mss() as sct:
            sct_img = sct.grab(sct.monitors[1])
            pil_img = Image.frombytes("RGB", sct_img.size, sct_img.bgra, "raw", "BGRX")
            final_img = crop_image_in_memory(pil_img)
            filepath = os.path.join(output_dir, f"screenshot_{datetime.now().strftime('%Y%m%d_%H%M%S')}.png")
            final_img.save(filepath)
            return filepath
    except Exception as e:
        print(f"Error taking and processing screenshot: {e}")
        return None

def get_image_for_analysis(debug_image_path: Optional[str]) -> Optional[str]:
    if debug_image_path and os.path.exists(debug_image_path):
        print(f"[System]: Using provided debug image: {debug_image_path}")
        return debug_image_path
    elif debug_image_path:
        print(f"[System Warning]: Debug image path does not exist: '{debug_image_path}'. Taking live screenshot.")
    
    print("[System]: Taking a live screenshot...")
    return take_screenshot()

def main():
    """Main function with diagnostics and chat loop."""
    # To use a .env file, uncomment the next line
    # load_dotenv() 
    
    # It's better to load from environment variables, but you can hardcode for testing
    # api_key = os.getenv("OPENAI_API_KEY") 
    api_key="sk-proj-oBIOgX0aO6YzaLlAldnpT3BlbkFJbdDbSEEoomYGNFVC9A2l"
    
    if not api_key:
        print("خطأ: لم يتم العثور على مفتاح OPENAI_API_KEY. الرجاء إنشاء ملف .env أو تعيينه مباشرة في الكود.")
        return
        
    # Set the path to a test image if you want to use one, otherwise set to None
    debug_image_path = r'C:\Developer\Unity Projects\ChronoRelic\Assets\ai\gbt\Game_Screenshots\Obelisk\Screenshot 2025-02-28 164050.png'
    
    try:
        print("Initializing Hemdan RAG System...")
        hemdan = HemdanRAGSystem(
            openai_api_key=api_key, 
            lore_file_path="lore.txt",
            places_csv_path="buildings_text.csv",
            images_root_path="Game_Screenshots"
        )
        
        print("\n=== ENHANCED SYSTEM DIAGNOSTICS ===")
        
        files_ok = hemdan.test_csv_and_images_exist("buildings_text.csv", "Game_Screenshots")
        if not files_ok:
            print("❌ File existence test failed. Please check your paths and filenames. Exiting.")
            return
        
        places_ok = hemdan.debug_places_collection_detailed()
        if not places_ok:
            print("\n🔄 Attempting to fix by re-ingesting data...")
            hemdan.force_reingest_places("buildings_text.csv", "Game_Screenshots")
            places_ok = hemdan.debug_places_collection_detailed()
            
        if not places_ok:
            print("❌ Could not fix places collection. Please check your data files and ingestion logic. Exiting.")
            return
            
        if debug_image_path and os.path.exists(debug_image_path):
            print("\n=== TESTING BUILDING IDENTIFICATION WITH DEBUG IMAGE ===")
            result = hemdan.identify_building(debug_image_path)
            if result:
                print(f"🎯 Identification test successful: {result}")
            else:
                print("❌ Identification test failed.")
                    
        print("\n✅ All diagnostics complete!")
        
    except Exception as e:
        print(f"\n❌ CRITICAL INITIALIZATION ERROR: {e}")
        traceback.print_exc()
        return

    # <<< CORRECTED CHAT LOOP >>>
    print("\n\n=== نظام حمدان الذكي جاهز ===\n")
        
    while True:
        user_input = input("\nلورنزو: ").strip()
        if user_input.lower() in ['خروج', 'exit']:
            print("حمدان: إلى اللقاء يا لورنزو. كن حذراً.")
            break
        if not user_input:
            continue

        print("[Hemdan System]: Analyzing your request...")
        intent_info = hemdan.determine_user_intent(user_input)
        intent = intent_info.get("intent", "lore_query")

        result_data = {}

        if intent == "place_identification":
            image_to_process = get_image_for_analysis(debug_image_path)
            if image_to_process:
                print(f"[Hemdan System]: Intent is 'place_identification'. Using image for analysis...")
                result_data = hemdan.process_query(user_input, image_path=image_to_process)
            else:
                print("[Hemdan System]: Could not obtain image for analysis.")
                result_data = {
                    "response": "آسف يا لورنزو، لم أتمكن من الحصول على صورة لتحليلها.",
                    "retrieved_chunks": []
                }
        else:
            print(f"[Hemdan System]: Intent is '{intent}'. Processing text-only query...")
            result_data = hemdan.process_query(user_input)
        
        final_response = result_data.get("response", "حدث خطأ غير متوقع.")
        sources = result_data.get("retrieved_chunks", [])

        print(f"\nحمدان: {final_response}")

        if sources:
            print("\n--- [ مصادر حمدان (القطع المستخدمة) ] ---")
            for i, chunk in enumerate(sources):
                print(f"  [{i+1}] المصدر: {chunk['source']}")
                if chunk['type'] == 'lore':
                    # For lore, print the text content, slightly indented
                    content_preview = (chunk['content'][:120] + '...') if len(chunk['content']) > 120 else chunk['content']
                    # THIS IS THE FIXED LINE:
                    #print(f"      المحتوى: \"{content_preview.replace('\n', ' ')}\"")
                elif chunk['type'] == 'place_identification':
                    # For places, format the dictionary nicely
                    place_info = chunk['content']
                    confidence = place_info.get('confidence', 0) * 100
                    print(f"      المبنى: {place_info.get('name', 'N/A')} (بثقة {confidence:.1f}%)")
                    print(f"      الوصف: {place_info.get('description', 'N/A')}")
            print("------------------------------------------")

if __name__ == "__main__":
    main()