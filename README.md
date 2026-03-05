# Hytale Regionfile Analyzer

A C# (.NET 10) utility for **analyzing Hytale IndexedStorage `region.bin` files** and identifying **corrupted chunks and indexes**.

This tool is read-only by design (for now) — it detects corruption but **does not attempt to repair or modify files**.
As such this tool is currently **useless for repairing** your worlds!
It is as of now only intended to be a resource others can use for their own tools.

The code is quite bad and unoptimized, which I hope to tackle in the future.

I would also like to credit the following two users for providing documentation on the Hytale file format and its contents. Without them, this project wouldn't have been made :)
- [nickt128](https://gist.github.com/nickt128/bbf223d849fced931c9ecbc3a988a83c)
- [TheMcSebi](https://github.com/TheMcSebi/hytale-region-parser)

---

## ✨ Features

* Parses Hytale `IndexedStorage` region files (`region.bin`)
* Detects:

  * Corrupted segment headers
  * Corrupted blob_index entries
* Simple CLI / entry-point execution

---

## 🚧 Current Status

⚠️ **Analysis only**

This project is currently limited to:

* Identifying corrupted chunks
* Identifying corrupted index entries

It **does not**:

* Repair corruption
* Rewrite region files
* Recover lost chunks

Repair tooling may be added sometime in the future.
No promises.

---

## 🛠 Requirements

* **.NET 10 SDK**  
#### NuGet packages:
* Newtonsoft.Json        13.0.4
* Newtonsoft.Json.Bson   1.0.3
* Zstandard.Net          1.1.7

---

## 📂 Project Structure

```
/target
  ├─ region_0_0.bin
  ├─ region_0_1.bin
  └─ ...
/src
  ├─ Main.cs
  └─ ...
```

* `/target` — directory containing the region files to analyze (when not using the CLI)
* `/src/Main.cs` — entry point

---

## ▶️ Usage

### Option 1: Run via source

1. Place all `region.bin` files you want to analyze into the `/target` directory
2. Run the project:

```bash
dotnet run
```

The tool will automatically scan all region files found in `/target`.

---

### Option 2: Build and run the executable

1. Build the project:

```bash
dotnet build -c Release
```

2. Run the generated executable and supply a path:

```bash
HytaleIndexedStorageAnalyzer.exe /path/to/region/files
```

---

## 📤 Output

* Corruption findings are printed to the console.

  <img width="552" height="106" alt="image" src="https://github.com/user-attachments/assets/6fbcee27-c92c-47f2-927a-df644f349979" />
* A map of each region.bin file, with mapped segments.

  <img width="466" height="344" alt="image" src="https://github.com/user-attachments/assets/aaa8da54-a493-4bfc-ad6d-d18f3b0e0559" />
* Combined statistics of every analyzed regionfile: empty/corrupted/normal chunks.

  <img width="237" height="68" alt="image" src="https://github.com/user-attachments/assets/15014412-2779-44c5-a9a0-e1618c46f615" />





## 📜 License

* GNU GPLv3
