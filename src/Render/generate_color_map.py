# Place this file in the (unzipped) Hytale/install/release/package/game/latest/Assets folder and run it using:
# > python generate_color_map.py

# A color_map.json file will be output into the same directory



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
# Find all JSON files in the folder and its subfolders
# Strip .json to obtain BlockName
# Load JSON, navigate to ./Common/ + BlockType.CustomModelTexture.Texture for the .png texture
# Get the average color of the .png to obtain AverageColor
# Return a JSON doc with the BlockName : AverageColor format for every block.

import os
import json
import traceback
from PIL import Image, ImageOps

# --------------------------------------------------
# Configuration
# --------------------------------------------------
MIGRATION_FOLDER = "./Server/Item/Block/Migrations"
SEARCH_FOLDERS = [
	"./Server/Item/Items/Wood",
	"./Server/Item/Items/Trap",
	"./Server/Item/Items/Soil",
	"./Server/Item/Items/Rubble",
	"./Server/Item/Items/Rock",
	"./Server/Item/Items/Rail",
	"./Server/Item/Items/Potion",
	"./Server/Item/Items/Portal",
	"./Server/Item/Items/Plant",
	"./Server/Item/Items/Ore",
	"./Server/Item/Items/MISC",
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

OUTPUT_FILE = "color_map.json"


# --------------------------------------------------
# Utility: Average color of image
# --------------------------------------------------

def average(hex_colors):
	if not hex_colors:
		raise ValueError("Color list cannot be empty.")

	total_r = total_g = total_b = 0
	count = 0

	for color in hex_colors:
		color = color.strip().lstrip('#')

		if len(color) != 6:
			raise ValueError(f"Invalid hex color: {color}")

		r = int(color[0:2], 16)
		g = int(color[2:4], 16)
		b = int(color[4:6], 16)

		total_r += r
		total_g += g
		total_b += b
		count += 1

	avg_r = round(total_r / count)
	avg_g = round(total_g / count)
	avg_b = round(total_b / count)

	return f"#{avg_r:02x}{avg_g:02x}{avg_b:02x}"

def get_average_color(image_path, grayscale=False):
	try:
		with Image.open(image_path) as img:
			if grayscale:
				img = img.convert("L")
				img = ImageOps.colorize(img, black="black", white=grayscale)
				img = img.convert("RGBA")
			else:
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
				# Indicates a blockstate internally but we dont care
				if migrate_to[0] == "*":
					migrate_to = migrate_to[1:]

				results[alias] = results[migrate_to]
	except Exception as e:
		print(f"Failed to process Migration JSON: {json_path} ({traceback.format_exception(e)})")

def process():
	results = {}

	for folder in SEARCH_FOLDERS:
		if not os.path.exists(folder):
			print(f"Folder not found: {folder}")
			continue

		for root, _, files in os.walk(folder):
			#print(f"Found {len(files)} files in {root}")
			for file in files:
				if not file.lower().endswith(".json"):
					continue

				json_path = os.path.join(root, file)
				if folder == MIGRATION_FOLDER:
					process_migration_file(results, json_path)
					continue

				block_names = [os.path.splitext(file)[0]]
				tint_color = False

				try:
					with open(json_path, "r", encoding="utf-8") as f:
						data = json.load(f)

						blocktype = data.get("BlockType")
						if blocktype == None:
							# Item is not a block
							continue

						regular = blocktype.get("Textures")
						custom = blocktype.get("CustomModelTexture")
						state = blocktype.get("State")
						blocktint = blocktype.get("Tint")
						if blocktint:
							print(block_names[0], blocktint[0])
							tint_color = blocktint[0]

						png_paths = []
						def findpngs(lists):
							for texturemap in lists:
								up = texturemap.get("Up")
								if up:
									if "_GS" in up and blocktype.get("TintUp"):
										global tint_color
										tint_color = blocktype.get("TintUp")[0]
									png_paths.append(up)
									break

								for name, item in texturemap.items():
									if isinstance(item, str):
										png_paths.append(item)

						# Crop growth stages each have different textures, while rotational blocks only have rotation data in their states
						# As such we only fall back to crops when all else is excluded
						if state and not regular:
							for name, stage in state.get("Definitions").items():
								textures = stage.get("CustomModelTexture")
								#tint_color = stage.get("BiomeTint")
								
								# Some crops such as the tomato eternal dont use textures for some reason
								# TODO: find out why
								if textures: 
									findpngs(textures)
								
								block_names.append(block_names[0] + "_State_Definitions_" + name)

						if custom:
							findpngs(custom)

						if regular: # Models such as skull piles may not have normal textures
							findpngs(regular)

						texture_paths = (
							"Common/" + path for path in png_paths
						)

					if not texture_paths:
						print("Didn't find texture path")
						continue

					# Normalize texture path
					texture_paths = (texture_path.replace("\\", "/") for texture_path in texture_paths)

					averages = []
					for texture_path in texture_paths:
						idx = len("Common/")
						if texture_path[idx] == "#":
							averages.clear()
							averages.append(texture_path[idx+1:])
							break

						# If relative, resolve from Assets root
						if not os.path.isabs(texture_path):
							texture_paths = os.path.join(".", texture_path)

						if not os.path.exists(texture_path):
							print(f"Texture not found: {texture_path}")
							continue
						
						if "_GS" in texture_path or tint_color:
							averages.append(get_average_color(texture_path, tint_color or "#83e03e"))
						else:
							averages.append(get_average_color(texture_path))

					for block_name in block_names:
						# Yes, we mash all states together. Can't be bothered
						if block_name == "Survival_Trap_Grass":
							print(block_name, averages)
						results[block_name] = average(averages)

				except Exception as e:
					print(f"Failed to process JSON: {json_path} ({e})")

	with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
		json.dump(results, f, indent=4)

	print(f"\nFinished. Output written to {OUTPUT_FILE}")


# --------------------------------------------------

if __name__ == "__main__":
	print(os.getcwd())
	process()