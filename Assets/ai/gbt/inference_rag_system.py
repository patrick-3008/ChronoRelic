# asr_inference.py
import requests
import sys
import json
import time
import os
from datetime import datetime
from pathlib import Path
import mss
from PIL import Image
import numpy as np

BASE_URL = "http://127.0.0.1:8001"
ASR_OUTPUT_FILE = "asr_output.txt"  # Default ASR output file
LOG_FILE = "hemdan_asr_log.txt"

def check_service_status():
    """Check if the loader service is running and model is loaded"""
    try:
        res = requests.get(f"{BASE_URL}/status", timeout=5)
        if res.status_code == 200:
            return res.json()
        else:
            return None
    except requests.exceptions.ConnectionError:
        return None
    except Exception as e:
        print(f"❌ Error checking status: {e}")
        return None

def crop_image_in_memory(img: Image.Image, brightness_threshold: int = 35) -> Image.Image:
    """Crop image to remove dark areas (from visionplore.py)"""
    width, height = img.size
    mid_x, mid_y = width // 2, height // 2
    quadrant_boxes = {
        "top_left": (0, 0, mid_x, mid_y), 
        "top_right": (mid_x, 0, width, mid_y),
        "bottom_left": (0, mid_y, mid_x, height), 
        "bottom_right": (mid_x, mid_y, width, height)
    }
    
    def get_avg_brightness(box): 
        return np.mean(np.array(img.crop(box).convert('L')))
    
    quadrant_brightness = {name: get_avg_brightness(box) for name, box in quadrant_boxes.items()}
    content_quadrants = [name for name, brightness in quadrant_brightness.items() if brightness > brightness_threshold]
    
    if not content_quadrants or len(content_quadrants) == 4: 
        return img
    
    min_x = min(quadrant_boxes[name][0] for name in content_quadrants)
    min_y = min(quadrant_boxes[name][1] for name in content_quadrants)
    max_x = max(quadrant_boxes[name][2] for name in content_quadrants)
    max_y = max(quadrant_boxes[name][3] for name in content_quadrants)
    
    return img.crop((min_x, min_y, max_x, max_y))

def take_screenshot() -> str:
    """Take a screenshot and save it (from visionplore.py)"""
    print("📸 Taking screenshot...")
    output_dir = "screenshots"
    os.makedirs(output_dir, exist_ok=True)
    
    try:
        with mss.mss() as sct:
            sct_img = sct.grab(sct.monitors[1])
            pil_img = Image.frombytes("RGB", sct_img.size, sct_img.bgra, "raw", "BGRX")
            final_img = crop_image_in_memory(pil_img)
            filepath = os.path.join(output_dir, f"screenshot_{datetime.now().strftime('%Y%m%d_%H%M%S')}.png")
            final_img.save(filepath)
            print(f"📸 Screenshot saved: {filepath}")
            return filepath
    except Exception as e:
        print(f"❌ Error taking screenshot: {e}")
        return None

def debug_places_database():
    """Debug the places database"""
    try:
        res = requests.get(f"{BASE_URL}/debug_places")
        if res.status_code == 200:
            result = res.json()
            print(f"🐛 DEBUG: Places database info:")
            print(f"   - Total places: {result.get('places_count', 0)}")
            print(f"   - Sample buildings: {result.get('sample_buildings', [])}")
            if result.get('sample_metadata'):
                print(f"   - Sample metadata: {result['sample_metadata']}")
            return result.get('places_count', 0) > 0
        else:
            print(f"❌ Debug failed: {res.text}")
            return False
    except Exception as e:
        print(f"❌ Debug error: {e}")
        return False

def debug_hemdan_object():
    """Debug the hemdan object to see what methods are available"""
    try:
        res = requests.get(f"{BASE_URL}/debug_hemdan")
        if res.status_code == 200:
            result = res.json()
            print(f"🔍 DEBUG: Hemdan object analysis:")
            print(f"   - Type: {result.get('hemdan_type')}")
            print(f"   - Has identify_building: {result.get('has_identify_building')}")
            print(f"   - Has resnet_ef: {result.get('has_resnet_ef')}")
            print(f"   - Has places_collection: {result.get('has_places_collection')}")
            print(f"   - Places count: {result.get('places_count')}")
            
            if not result.get('has_identify_building'):
                print("❌ CRITICAL: identify_building method is missing!")
            if not result.get('has_resnet_ef'):
                print("❌ CRITICAL: resnet_ef (ResNet50) is missing!")
                
            return result.get('has_identify_building', False) and result.get('has_resnet_ef', False)
        else:
            print(f"❌ Hemdan debug failed: {res.text}")
            return False
    except Exception as e:
        print(f"❌ Hemdan debug error: {e}")
        return False

def force_test_identification(image_path, threshold=0.5):
    """Force test image identification with detailed debugging"""
    try:
        res = requests.post(f"{BASE_URL}/force_identify", params={"image_path": image_path, "threshold": threshold})
        if res.status_code == 200:
            result = res.json()
            print(f"🔥 FORCE DEBUG: Detailed identification test:")
            print(f"   - Image: {result.get('image_path')}")
            print(f"   - Threshold: {result.get('threshold')}")
            print(f"   - Has identify method: {result.get('has_identify_method')}")
            print(f"   - Has ResNet: {result.get('has_resnet')}")
            print(f"   - Embedding generated: {result.get('embedding_generated')}")
            print(f"   - Places count: {result.get('places_count')}")
            print(f"   - Result: {result.get('identification_result')}")
            
            if result.get('error'):
                print(f"❌ ERROR: {result['error']}")
                return False
            return result.get('identification_result') is not None
        else:
            print(f"❌ Force test failed: {res.text}")
            return False
    except Exception as e:
        print(f"❌ Force test error: {e}")
        return False

def should_take_screenshot(message: str) -> bool:
    """Determine if the message requires a screenshot for place identification"""
    place_keywords = [
        # Arabic place-related keywords
        "فين", "مكان", "هنا", "ده", "دي", "المبنى", "البناية", "المعبد", "القصر",
        "الهرم", "الضريح", "المقبرة", "الحديقة", "الساحة", "الشارع", "المنطقة",
        "احنا", "موجودين", "واقفين", "قدام", "جنب", "حوالين",
        # English equivalents
        "where", "place", "here", "building", "temple", "palace", "pyramid",
        "what", "this", "that", "location", "area", "structure"
    ]
    
    question_indicators = [
        "ايه", "إيه", "what", "which", "where", "اين", "أين"
    ]
    
    message_lower = message.lower()
    
    # Check if it's a question about a place
    has_place_keyword = any(keyword in message_lower for keyword in place_keywords)
    has_question = any(indicator in message_lower for indicator in question_indicators)
    
    return has_place_keyword and has_question

def wait_for_service():
    """Wait for the loader service to be ready"""
    print("🔍 Checking if Hemdan loader service is ready...")
    
    max_retries = 30
    for attempt in range(max_retries):
        status = check_service_status()
        if status is None:
            if attempt == 0:
                print("❌ Cannot connect to Hemdan loader service.")
                print("🚀 Please start the loader service first: python model_loader.py")
            print(f"⏳ Waiting for service... (attempt {attempt + 1}/{max_retries})")
            time.sleep(2)
            continue
        
        if status.get("model_loaded", False):
            print("✅ Hemdan loader service is ready!")
            print(f"   - Session ID: {status.get('session_id', 'N/A')}")
            print(f"   - Lore chunks: {status.get('lore_count', 'N/A')}")
            print(f"   - Places: {status.get('places_count', 'N/A')}")
            return True
        else:
            print(f"⏳ Service is running but model not loaded yet... (attempt {attempt + 1}/{max_retries})")
            time.sleep(2)
    
    print("❌ Service did not become ready within the timeout period.")
    return False

def send_message_to_hemdan(message, image_path=None):
    """Send message to the loaded Hemdan model"""
    payload = {"message": message}
    if image_path:
        payload["image_path"] = image_path
        print(f"🔗 Sending screenshot to Hemdan for comparison with Game Screenshots database")
    
    try:
        res = requests.post(f"{BASE_URL}/chat", json=payload, timeout=30)
        if res.status_code == 200:
            result = res.json()
            return {
                "success": True,
                "response": result.get("hemdan_response", "No response"),
                "chunks": result.get("retrieved_chunks", []),
                "session_id": result.get("session_id")
            }
        else:
            error_detail = res.json().get('detail', 'Unknown error')
            return {
                "success": False,
                "error": error_detail
            }
    except requests.exceptions.Timeout:
        return {
            "success": False,
            "error": "Request timed out. The model might be processing a complex query."
        }
    except requests.exceptions.ConnectionError:
        return {
            "success": False,
            "error": "Lost connection to Hemdan service."
        }
    except Exception as e:
        return {
            "success": False,
            "error": f"Unexpected error: {str(e)}"
        }

def read_asr_output(file_path):
    """Read ASR output from file"""
    try:
        if not os.path.exists(file_path):
            return None, f"ASR output file not found: {file_path}"
        
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read().strip()
        
        if not content:
            return None, "ASR output file is empty"
        
        return content, None
    except Exception as e:
        return None, f"Error reading ASR file: {str(e)}"

def log_conversation(text):
    """Log conversation to hemdan_asr_log.txt with timestamp"""
    try:
        with open(LOG_FILE, "a", encoding="utf-8") as f:
            f.write(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] {text}\n")
    except Exception as e:
        print(f"Warning: Could not write to log file: {e}")

def print_sources(chunks):
    """Print retrieved source chunks in a formatted way"""
    if chunks:
        print("\n--- [ مصادر همدان (القطع المستخدمة) ] ---")
        for i, chunk in enumerate(chunks):
            print(f"  [{i+1}] المصدر: {chunk.get('source', 'Unknown')}")
            if chunk.get('type') == 'lore':
                content = chunk.get('content', '')
                content_preview = (content[:120] + '...') if len(content) > 120 else content
                print(f"      المحتوى: \"{content_preview.replace(chr(10), ' ')}\"")
            elif chunk.get('type') == 'place_identification':
                place_info = chunk.get('content', {})
                confidence = place_info.get('confidence', 0) * 100
                print(f"      المبنى: {place_info.get('name', 'N/A')} (بثقة {confidence:.1f}%)")
                print(f"      الوصف: {place_info.get('description', 'N/A')}")
        print("------------------------------------------")

def write_gbt_output(response_text):
    """Write only the pure model response to gbt_output.txt, overwriting each time"""
    try:
        # Clean the response - remove any extra whitespace and ensure it's just the text
        clean_response = response_text.strip()
        
        with open("gbt_output.txt", "w", encoding="utf-8") as f:
            f.write(clean_response)
        print("✅ Response written to gbt_output.txt")
    except Exception as e:
        print(f"❌ Error writing to gbt_output.txt: {e}")

def process_once_and_exit(file_path, image_path=None):
    """Process ASR file once and exit"""
    print(f"📁 Reading ASR file: {file_path}")
    
    content, error = read_asr_output(file_path)
    
    if error:
        print(f"❌ {error}")
        return False
    
    print(f"📢 ASR Input: {content}")
    
    # Debug: Check places database first
    print("🐛 DEBUG: Checking places database...")
    places_ok = debug_places_database()
    if not places_ok:
        print("⚠️ WARNING: Places database appears to be empty or inaccessible!")
    
    # Debug: Check hemdan object structure
    print("🔍 DEBUG: Checking Hemdan object structure...")
    hemdan_ok = debug_hemdan_object()
    if not hemdan_ok:
        print("⚠️ WARNING: Hemdan object is missing critical methods!")
    
    # Auto-detect if we need a screenshot for place identification
    if image_path:
        print(f"🖼️  Using provided image: {image_path}")
        final_image_path = image_path
    elif should_take_screenshot(content):
        print("📸 Detected place-related question. Taking screenshot...")
        auto_screenshot = take_screenshot()
        if auto_screenshot:
            final_image_path = auto_screenshot
            print(f"🔍 Screenshot will be compared against Game Screenshots database using ResNet50 embeddings")
            
            # Debug: Force test identification with detailed output
            print("🔥 DEBUG: Force testing image identification...")
            force_result = force_test_identification(final_image_path, threshold=0.3)
            print(f"🔥 DEBUG: Force identification {'SUCCESS' if force_result else 'FAILED'}")
        else:
            print("❌ Failed to take screenshot, proceeding without image")
            final_image_path = None
    else:
        print("💬 Text-only query detected")
        final_image_path = None
    
    print("🧠 Processing with Hemdan...")
    if final_image_path:
        print(f"🎯 Hemdan will use identify_building() to compare screenshot with known places...")
    
    result = send_message_to_hemdan(content, final_image_path)
    
    if result["success"]:
        response = result['response']
        print(f"📤 Hemdan Response: {response}")
        
        # Show detailed place identification results
        if final_image_path and result['chunks']:
            print(f"🔍 DEBUG: Received {len(result['chunks'])} chunks from Hemdan")
            for chunk in result['chunks']:
                print(f"🔍 DEBUG: Chunk type: {chunk.get('type')}")
                if chunk.get('type') == 'place_identification':
                    place_info = chunk.get('content', {})
                    confidence = place_info.get('confidence', 0) * 100
                    print(f"🎯 Place identified from screenshot: {place_info.get('name', 'Unknown')} (confidence: {confidence:.1f}%)")
                    print(f"📋 Description: {place_info.get('description', 'N/A')}")
        elif final_image_path:
            print("⚠️ WARNING: Screenshot was sent but no place identification chunks were returned!")
            print("🐛 This suggests the identify_building() method is not working properly")
        
        # Show sources for debugging
        print_sources(result['chunks'])
        
        # Write ONLY the pure response to gbt_output.txt (overwrite)
        write_gbt_output(response)
        
        # Log the conversation to the separate log file
        log_conversation(f"ASR Input: {content}")
        if final_image_path:
            log_conversation(f"Screenshot analyzed: {final_image_path}")
            # Log place identification results
            for chunk in result['chunks']:
                if chunk.get('type') == 'place_identification':
                    place_info = chunk.get('content', {})
                    confidence = place_info.get('confidence', 0) * 100
                    log_conversation(f"Place identified: {place_info.get('name', 'Unknown')} ({confidence:.1f}% confidence)")
        log_conversation(f"Hemdan Response: {response}")
        
        print("✅ Processing complete. Exiting.")
        return True
    else:
        print(f"❌ Error: {result['error']}")
        log_conversation(f"Error: {result['error']}")
        return False

def main():
    """Main function"""
    print("🎤 Hemdan ASR Inference Client")
    print("📸 Screenshots will be compared against Game Screenshots database")
    print("🧠 Using ResNet50 embeddings for place identification")
    print("🐛 Debug mode enabled for troubleshooting")
    print("=" * 50)
    
    # Wait for service to be ready
    if not wait_for_service():
        sys.exit(1)
    
    # Parse command line arguments
    if len(sys.argv) == 1:
        # Default: process default ASR file once and exit
        asr_file = ASR_OUTPUT_FILE
        success = process_once_and_exit(asr_file)
        sys.exit(0 if success else 1)
        
    elif len(sys.argv) == 2:
        # Single file mode
        asr_file = sys.argv[1]
        success = process_once_and_exit(asr_file)
        sys.exit(0 if success else 1)
        
    elif len(sys.argv) == 3:
        # File + image mode
        asr_file = sys.argv[1]
        image_path = sys.argv[2]
        success = process_once_and_exit(asr_file, image_path)
        sys.exit(0 if success else 1)
        
    else:
        print("Usage:")
        print("  python asr_inference.py                    # Process default ASR file once")
        print("  python asr_inference.py <asr_file>         # Process single ASR file once")
        print("  python asr_inference.py <asr_file> <image> # Process ASR file with image once")
        sys.exit(1)

if __name__ == "__main__":
    main()