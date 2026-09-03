"""
run_evaluation.py
-----------------------------------
Evaluation for the SafeBeauty zero-shot ingredient classifier.

Reads test_set.csv -> classifies each ingredient via facebook/bart-large-mnli
(same endpoint, labels and wording as the app) -> writes predictions.csv.

This version is resilient to the free-tier HF API being unstable:
  - shorter per-request timeout (30s) so it never hangs for minutes
  - up to 3 retries per ingredient, with a short wait between them
  - a warm-up loop that keeps trying until the model responds

Key is read from environment variable HF_API_KEY (never written to file).

Run (PowerShell):
    $env:HF_API_KEY="hf_your_key"; 
    python run_evaluation.py
"""

import csv
import json
import os
import time
import urllib.request
import urllib.error

ENDPOINT = "https://router.huggingface.co/hf-inference/models/facebook/bart-large-mnli"
LABELS = [
    "safe cosmetic ingredient",
    "skin irritant",
    "potentially harmful",
    "allergen",
]

INPUT_FILE = "test_set.csv"
OUTPUT_FILE = "predictions.csv"
PAUSE_SECONDS = 1.0     # gap between ingredients
TIMEOUT = 30            # per request - short, so we never hang for minutes
MAX_RETRIES = 3         # attempts per ingredient before giving up as Unknown
RETRY_WAIT = 8          # seconds to wait between retries (lets a sleepy model wake)


def build_input(name):
    return f"{name} is a cosmetic ingredient"


def classify_once(name, api_key):
    """One attempt. Returns (label, score) or raises on failure."""
    body = json.dumps({
        "inputs": build_input(name),
        "parameters": {"candidate_labels": LABELS},
    }).encode("utf-8")

    request = urllib.request.Request(
        ENDPOINT, data=body,
        headers={"Authorization": f"Bearer {api_key}",
                 "Content-Type": "application/json"},
        method="POST")

    with urllib.request.urlopen(request, timeout=TIMEOUT) as response:
        raw = response.read().decode("utf-8")

    payload = json.loads(raw)
    # Router returns a LIST of {label, score}, best-first.
    # Some deployments wrap it once more: [ [ {...} ] ].
    if isinstance(payload, list) and payload and isinstance(payload[0], list):
        payload = payload[0]
    top = payload[0]
    return top["label"], round(float(top["score"]), 2)


def classify(name, api_key):
    """
    Try up to MAX_RETRIES times. Returns (label, score).
    Falls back to ("Unknown", 0.0) only after all retries fail - the same
    graceful degradation the app uses, but we retry first so a transient
    timeout does not wrongly become a real 'Unknown'.
    """
    for attempt in range(1, MAX_RETRIES + 1):
        try:
            return classify_once(name, api_key)
        except Exception as e:
            reason = type(e).__name__
            if attempt < MAX_RETRIES:
                print(f"      (retry {attempt}/{MAX_RETRIES-1} for '{name}': {reason}, waiting {RETRY_WAIT}s)")
                time.sleep(RETRY_WAIT)
            else:
                print(f"      (gave up on '{name}' after {MAX_RETRIES} tries: {reason})")
    return "Unknown", 0.0


def main():
    api_key = os.environ.get("HF_API_KEY", "").strip()
    if not api_key:
        print("ERROR: HF_API_KEY is not set in this terminal.")
        print('Set it:  $env:HF_API_KEY="hf_xxxx"; python run_evaluation.py')
        return

    with open(INPUT_FILE, newline="", encoding="utf-8") as f:
        rows = list(csv.DictReader(f))
    print(f"Loaded {len(rows)} ingredients from {INPUT_FILE}\n")

    # Warm-up loop: keep trying until the model answers once, so cold start
    # does not contaminate the first few real results.
    print("Warming up the model...")
    for w in range(1, 7):
        try:
            classify_once("Glycerin", api_key)
            print("  Model is awake.\n")
            break
        except Exception as e:
            print(f"  warm-up {w}/6 not ready ({type(e).__name__}), waiting 10s...")
            time.sleep(10)
    else:
        print("  Warning: model still slow, continuing anyway.\n")

    results = []
    correct = 0
    for i, row in enumerate(rows, 1):
        name = row["ingredient_name"].strip()
        true_label = row["true_label"].strip()

        predicted, confidence = classify(name, api_key)
        is_correct = (predicted == true_label)
        if is_correct:
            correct += 1

        mark = "OK " if is_correct else "XX "
        print(f"  [{i:2}/{len(rows)}] {mark} {name:32} true={true_label:24} -> {predicted} ({confidence})")

        results.append({
            "ingredient_name": name,
            "true_label": true_label,
            "predicted_label": predicted,
            "confidence": confidence,
        })
        time.sleep(PAUSE_SECONDS)

    with open(OUTPUT_FILE, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(
            f, fieldnames=["ingredient_name", "true_label", "predicted_label", "confidence"])
        writer.writeheader()
        writer.writerows(results)

    unknown = sum(1 for r in results if r["predicted_label"] == "Unknown")
    accuracy = correct / len(rows) if rows else 0
    print(f"\nDone. Wrote {len(results)} rows to {OUTPUT_FILE}")
    print(f"Quick accuracy: {correct}/{len(rows)} = {accuracy:.1%}")
    if unknown:
        print(f"Note: {unknown} ingredient(s) ended as 'Unknown' (API did not respond after retries).")
        print("You can re-run to retry just those, or increase RETRY_WAIT.")


if __name__ == "__main__":
    main()