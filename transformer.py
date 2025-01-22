from transformers import AutoModelForCausalLM, AutoTokenizer
import torch

# Use Mistral 7B instead of Falcon
model_name = "mistralai/Mistral-7B-Instruct-v0.3"

# Load the tokenizer and model
tokenizer = AutoTokenizer.from_pretrained(model_name)
model = AutoModelForCausalLM.from_pretrained(model_name, torch_dtype=torch.float16, device_map="auto")

# Example conversation
prompt = "Hello, how are you today?"
inputs = tokenizer(prompt, return_tensors="pt").to("cuda" if torch.cuda.is_available() else "cpu")

# Generate response
outputs = model.generate(**inputs, max_length=150, temperature=0.5, top_p=0.9, top_k=50, repetition_penalty=1.2)

# Decode response
response = tokenizer.decode(outputs[0], skip_special_tokens=True)
print("Bot Response:", response)
