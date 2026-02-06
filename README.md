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
* A map of each region.bin file, with mapped segments.
* Statistics of each regionfile: empty/corrupted/normal chunks.

## 📜 License

* GNU GPLv3
