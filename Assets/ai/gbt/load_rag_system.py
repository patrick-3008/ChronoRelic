# model_loader.py
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import os
import uuid
import json
from datetime import datetime
from visionplore import HemdanRAGSystem  # Import your HemdanRAGSystem class

# === Define FastAPI App ===
app = FastAPI(title="Hemdan RAG Model Loader API")

# === Configuration ===
API_KEY = "sk-proj-oBIOgX0aO6YzaLlAldnpT3BlbkFJbdDbSEEoomYGNFVC9A2l"
LORE_PATH = "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/gbt/lore.txt"
PLACES_CSV_PATH = "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/gbt/buildings_text.csv"
IMAGES_ROOT_PATH = "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/gbt/Game_Screenshots"

# === Global variables ===
hemdan = None
current_session_id = None

# === Request Schemas ===
class UserMessage(BaseModel):
    message: str
    image_path: str = None

class SessionResponse(BaseModel):
    session_id: str
    message: str

# === Initialize Model on Startup ===
def initialize_hemdan_system():
    """Initialize the Hemdan RAG system on startup"""
    global hemdan, current_session_id
    
    try:
        print("🔄 Initializing Hemdan RAG System on startup...")
        hemdan = HemdanRAGSystem(
            openai_api_key=API_KEY,
            lore_file_path=LORE_PATH,
            places_csv_path=PLACES_CSV_PATH,
            images_root_path=IMAGES_ROOT_PATH
        )
        
        print("✅ Running diagnostics...")
        # Run diagnostics
        files_ok = hemdan.test_csv_and_images_exist(PLACES_CSV_PATH, IMAGES_ROOT_PATH)
        if not files_ok:
            raise Exception("File existence test failed. Check your paths.")
        
        places_ok = hemdan.debug_places_collection_detailed()
        if not places_ok:
            print("🔄 Re-ingesting places data...")
            hemdan.force_reingest_places(PLACES_CSV_PATH, IMAGES_ROOT_PATH)
            places_ok = hemdan.debug_places_collection_detailed()
        
        if not places_ok:
            raise Exception("Could not initialize places collection properly.")
        
        # Start a new session
        current_session_id = str(uuid.uuid4())
        hemdan.current_session_id = current_session_id
        hemdan.conversation_history = []
        
        print(f"✅ Hemdan RAG System loaded successfully!")
        print(f"📝 Session started with ID: {current_session_id}")
        return True
        
    except Exception as e:
        print(f"❌ Failed to initialize Hemdan RAG system: {e}")
        hemdan = None
        current_session_id = None
        return False

# === Startup Event ===
@app.on_event("startup")
async def startup_event():
    """Load model automatically when the service starts"""
    success = initialize_hemdan_system()
    if not success:
        print("⚠️ Warning: Failed to load model on startup. Service will still run but chat will fail.")

# === Endpoints ===
@app.get("/status")
def get_status():
    """Check if the model is loaded and ready"""
    if hemdan is None:
        return {
            "model_loaded": False,
            "session_active": False,
            "message": "Model not loaded. Restart the service to retry loading."
        }
    else:
        return {
            "model_loaded": True,
            "session_active": current_session_id is not None,
            "session_id": current_session_id,
            "message": "Hemdan RAG System is loaded and ready",
            "lore_count": hemdan.lore_collection.count(),
            "places_count": hemdan.places_collection.count()
        }

@app.post("/chat")
def chat(user_input: UserMessage):
    """Chat with Hemdan using the loaded model"""
    if hemdan is None:
        raise HTTPException(status_code=500, detail="Model not loaded. Please restart the service.")
    
    if current_session_id is None:
        raise HTTPException(status_code=500, detail="No active session. Please restart the service.")
    
    try:
        # Debug: Check if image_path is provided
        if user_input.image_path:
            print(f"🔍 DEBUG: Received image path: {user_input.image_path}")
            print(f"🔍 DEBUG: Image exists: {os.path.exists(user_input.image_path)}")
            
            # Force place identification intent when image is provided
            print("🎯 DEBUG: Image provided - forcing place identification analysis")
            
            # Test building identification directly
            print("🧠 DEBUG: Testing identify_building method...")
            building_result = hemdan.identify_building(user_input.image_path)
            print(f"🔍 DEBUG: Building identification result: {building_result}")
            
        result = hemdan.process_query(user_input.message, image_path=user_input.image_path)
        
        # Debug: Check what was returned
        print(f"📋 DEBUG: Process query returned: {len(result.get('retrieved_chunks', []))} chunks")
        for chunk in result.get('retrieved_chunks', []):
            print(f"📋 DEBUG: Chunk type: {chunk.get('type')}, source: {chunk.get('source')}")
            
        return {
            "session_id": current_session_id,
            "hemdan_response": result["response"],
            "retrieved_chunks": result["retrieved_chunks"]
        }
    except Exception as e:
        print(f"❌ ERROR in chat endpoint: {str(e)}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Error generating response: {str(e)}")

@app.post("/new_session", response_model=SessionResponse)
def new_session():
    """Start a new conversation session"""
    global current_session_id
    
    if hemdan is None:
        raise HTTPException(status_code=500, detail="Model not loaded. Please restart the service.")
    
    current_session_id = str(uuid.uuid4())
    hemdan.current_session_id = current_session_id
    hemdan.conversation_history = []
    
    return SessionResponse(
        session_id=current_session_id,
        message="تم بدء جلسة جديدة مع همدان."
    )

@app.get("/summary")
def session_summary():
    """Get summary of current session"""
    if hemdan is None:
        raise HTTPException(status_code=500, detail="Model not loaded. Please restart the service.")
    
    if current_session_id is None:
        raise HTTPException(status_code=500, detail="No active session.")
    
    if not hemdan.conversation_history:
        return {
            "session_id": current_session_id,
            "summary": "لا توجد محادثات في هذه الجلسة بعد."
        }
    
    # Simple summary - last 3 exchanges
    recent_history = hemdan.conversation_history[-3:]
    summary = "ملخص آخر المحادثات:\n"
    for i, turn in enumerate(recent_history, 1):
        summary += f"{i}. لورنزو: {turn['user']}\n   همدان: {turn['assistant']}\n"
    
    return {
        "session_id": current_session_id,
        "summary": summary
    }

@app.post("/reload_model")
def reload_model():
    """Manually reload the model (in case of failure)"""
    success = initialize_hemdan_system()
    if success:
        return {
            "success": True,
            "message": "Model reloaded successfully",
            "session_id": current_session_id
        }
    else:
        raise HTTPException(status_code=500, detail="Failed to reload model")

@app.get("/debug_places")
def debug_places():
    """Debug endpoint to check places collection"""
    if hemdan is None:
        raise HTTPException(status_code=500, detail="Model not loaded.")
    
    try:
        count = hemdan.places_collection.count()
        if count > 0:
            # Get sample data
            sample = hemdan.places_collection.get(limit=3, include=['metadatas'])
            building_names = [m.get('name', 'Unknown') for m in sample['metadatas']]
            return {
                "places_count": count,
                "sample_buildings": building_names,
                "sample_metadata": sample['metadatas'][0] if sample['metadatas'] else None
            }
        else:
            return {"places_count": 0, "error": "Places collection is empty"}
    except Exception as e:
        return {"error": f"Error checking places: {str(e)}"}

@app.post("/test_identify")
def test_identify(image_path: str):
    """Test building identification with a specific image"""
    if hemdan is None:
        raise HTTPException(status_code=500, detail="Model not loaded.")
    
    try:
        print(f"🧪 Testing identification for: {image_path}")
        print(f"🧪 Image exists: {os.path.exists(image_path)}")
        print(f"🧪 Places collection count: {hemdan.places_collection.count()}")
        
        # Test with lower threshold for debugging
        result = hemdan.identify_building(image_path, threshold=0.8)  # Try higher threshold first
        if not result:
            print("🧪 No result with 0.8 threshold, trying 0.5...")
            result = hemdan.identify_building(image_path, threshold=0.5)
        if not result:
            print("🧪 No result with 0.5 threshold, trying 0.3...")
            result = hemdan.identify_building(image_path, threshold=0.3)
        
        return {
            "image_path": image_path,
            "image_exists": os.path.exists(image_path),
            "places_count": hemdan.places_collection.count(),
            "identification_result": result,
            "threshold_tested": "0.8, 0.5, 0.3"
        }
    except Exception as e:
        print(f"❌ Error in test_identify: {str(e)}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Error testing identification: {str(e)}")

@app.post("/force_identify")
def force_identify(image_path: str, threshold: float = 0.5):
    """Force building identification with custom threshold"""
    if hemdan is None:
        raise HTTPException(status_code=500, detail="Model not loaded.")
    
    try:
        print(f"🔥 FORCE testing identification for: {image_path}")
        print(f"🔥 Using threshold: {threshold}")
        
        # Check if the image analysis method exists
        if not hasattr(hemdan, 'identify_building'):
            return {"error": "identify_building method not found in hemdan object"}
        
        if not hasattr(hemdan, 'resnet_ef'):
            return {"error": "resnet_ef (ResNet50EmbeddingFunction) not found in hemdan object"}
        
        # Test ResNet50 embedding generation
        print("🔥 Testing ResNet50 embedding generation...")
        try:
            embeddings = hemdan.resnet_ef([image_path])
            print(f"🔥 Generated embeddings length: {len(embeddings) if embeddings else 0}")
        except Exception as embed_error:
            print(f"🔥 Embedding generation failed: {embed_error}")
            return {"error": f"ResNet50 embedding failed: {embed_error}"}
        
        # Now test identification
        result = hemdan.identify_building(image_path, threshold=threshold)
        
        return {
            "image_path": image_path,
            "threshold": threshold,
            "has_identify_method": hasattr(hemdan, 'identify_building'),
            "has_resnet": hasattr(hemdan, 'resnet_ef'),
            "embedding_generated": len(embeddings) if embeddings else 0,
            "places_count": hemdan.places_collection.count(),
            "identification_result": result
        }
    except Exception as e:
        print(f"❌ Error in force_identify: {str(e)}")
        import traceback
        traceback.print_exc()
        return {"error": f"Force identification failed: {str(e)}"}

@app.get("/debug_hemdan")
def debug_hemdan():
    """Debug the hemdan object to see what methods are available"""
    if hemdan is None:
        raise HTTPException(status_code=500, detail="Model not loaded.")
    
    try:
        hemdan_methods = [method for method in dir(hemdan) if not method.startswith('_')]
        has_identify = hasattr(hemdan, 'identify_building')
        has_resnet = hasattr(hemdan, 'resnet_ef')
        has_places = hasattr(hemdan, 'places_collection')
        
        places_count = 0
        if has_places:
            places_count = hemdan.places_collection.count()
        
        return {
            "hemdan_type": str(type(hemdan)),
            "available_methods": hemdan_methods,
            "has_identify_building": has_identify,
            "has_resnet_ef": has_resnet,
            "has_places_collection": has_places,
            "places_count": places_count
        }
    except Exception as e:
        return {"error": f"Debug failed: {str(e)}"}

@app.get("/health")
def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "hemdan_model_loader",
        "model_loaded": hemdan is not None,
        "session_active": current_session_id is not None
    }

if __name__ == "__main__":
    import uvicorn
    print("🚀 Starting Hemdan RAG Model Loader Service...")
    print("📍 The model will be loaded automatically on startup")
    print("🔗 Service will be available at: http://localhost:8001")
    print("🐛 Debug endpoints available:")
    print("   - GET /debug_places - Check places database")
    print("   - POST /test_identify - Test image identification")
    uvicorn.run(app, host="0.0.0.0", port=8001)