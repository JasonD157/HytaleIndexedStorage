# Place this file in the Assets folder and run it

# Places we need to search to obtain (most) textures:
#	- ./Server/Item/Items/Trap
#	- ./Server/Item/Items/Soil
#	- ./Server/Item/Items/Rubble
#	- ./Server/Item/Items/Rock
#	- ./Server/Item/Items/Rail
#	- ./Server/Item/Items/Portal
#	- ./Server/Item/Items/Plant
#	- ./Server/Item/Items/Ore
#	- ./Server/Item/Items/Furniture
#	- ./Server/Item/Items/Electrum
#	- ./Server/Item/Items/Deco
#	- ./Server/Item/Items/Coops
#	- ./Server/Item/Items/Container
#	- ./Server/Item/Items/Cloth
#	- ./Server/Item/Items/Bone
#	- ./Server/Item/Items/Bench

# ./Server/Item/Block/Migrations contains .json's of aliases for block names.
# These need to be integrated too

# Find all JSON files in the folder and its subfolders
# Strip .json to obtain BlockName
# Load JSON, navigate to ./Common/ + BlockType.CustomModelTexture.Texture for the .png texture
# Get the average color of the .png to obtain AverageColor
# Return a JSON doc with the BlockName : AverageColor format for every block.

import os
import json
from PIL import Image

# --------------------------------------------------
# Configuration
# --------------------------------------------------
MIGRATION_FOLDER = "./Server/Item/Block/Migrations"
SEARCH_FOLDERS = [
	"./Server/Item/Items/Trap",
	"./Server/Item/Items/Soil",
	"./Server/Item/Items/Rubble",
	"./Server/Item/Items/Rock",
	"./Server/Item/Items/Rail",
	"./Server/Item/Items/Portal",
	"./Server/Item/Items/Plant",
	"./Server/Item/Items/Ore",
	"./Server/Item/Items/Furniture",
	"./Server/Item/Items/Electrum",
	"./Server/Item/Items/Deco",
	"./Server/Item/Items/Coops",
	"./Server/Item/Items/Container",
	"./Server/Item/Items/Cloth",
	"./Server/Item/Items/Bone",
	"./Server/Item/Items/Bench",
	MIGRATION_FOLDER # Gets special handling in process()
]


OUTPUT_FILE = "BlockAverageColors.json"


# --------------------------------------------------
# Utility: Average color of image
# --------------------------------------------------

def get_average_color(image_path):
	try:
		with Image.open(image_path) as img:
			img = img.convert("RGBA")
			pixels = list(img.getdata())

			total_r = total_g = total_b = total_a = 0
			count = 0

			for r, g, b, a in pixels:
				if a == 0:  # Ignore fully transparent pixels
					continue
				total_r += r
				total_g += g
				total_b += b
				total_a += a
				count += 1

			if count == 0:
				return "#000000"

			avg_r = total_r // count
			avg_g = total_g // count
			avg_b = total_b // count

			return "#{:02x}{:02x}{:02x}".format(avg_r, avg_g, avg_b)

	except Exception as e:
		print(f"Failed to process image: {image_path} ({e})")
		return "#000000"


# --------------------------------------------------
# Main Processing
# --------------------------------------------------

def process_migration_file(results, json_path):
	try:
		with open(json_path, "r", encoding="utf-8") as f:
			data = json.load(f)
			migrations = data.get("DirectMigrations")
			for alias, migrate_to in migrations.items():
				results[alias] = results[migrate_to]
	except Exception as e:
		print(f"Failed to process JSON: {json_path} ({e})")


def process():
	results = {}

	for folder in SEARCH_FOLDERS:
		if not os.path.exists(folder):
			print(f"Folder not found: {folder}")
			continue

		for root, _, files in os.walk(folder):
			for file in files:
				if not file.lower().endswith(".json"):
					continue

				json_path = os.path.join(root, file)
				
				if folder == MIGRATION_FOLDER:
					process_migration_file(results, json_path)
					continue

				block_name = os.path.splitext(file)[0]

				try:
					with open(json_path, "r", encoding="utf-8") as f:
						data = json.load(f)

					texture_path = (
						data.get("Common", {})
							.get("BlockType.CustomModelTexture.Texture")
					)

					if not texture_path:
						continue

					# Normalize texture path
					texture_path = texture_path.replace("\\", "/")

					# If relative, resolve from Assets root
					if not os.path.isabs(texture_path):
						texture_path = os.path.join(".", texture_path)

					if not os.path.exists(texture_path):
						print(f"Texture not found: {texture_path}")
						continue

					average_color = get_average_color(texture_path)
					results[block_name] = average_color

				except Exception as e:
					print(f"Failed to process JSON: {json_path} ({e})")


	with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
		json.dump(results, f, indent=4)

	print(f"\nFinished. Output written to {OUTPUT_FILE}")


# --------------------------------------------------

if __name__ == "__main__":
	process()