#!/usr/bin/env python3
"""Builds the ComfyUI sprite-concept workflow: SDXL base + pixel-art-xl LoRA.

Writes two files next to this script:
  sprite_concept.workflow.json  open in the ComfyUI UI (Workflow > Open, or drag it in)
  sprite_concept.api.json       API format, for scripted runs (POST /prompt)

Usage: python3 tools/comfy/make_workflow.py [--install ~/ai/ComfyUI]
--install also copies the UI workflow into ComfyUI's saved-workflows folder.
"""
import argparse
import json
import shutil
from pathlib import Path

CKPT = "sd_xl_base_1.0.safetensors"
LORA = "pixel-art-xl.safetensors"
LORA_STRENGTH = 1.2  # recommended on the model card

POSITIVE = (
    "pixel art, side view, full body, game sprite, 1980s arcade beat-em-up style, "
    "limited palette, hard edges, plain background, "
    "bald monk in a tattered orange robe, wrapped fists, dry unimpressed expression"
)
NEGATIVE = "blurry, smooth gradients, anti-aliasing, 3d render, photo, text, watermark, multiple characters, cropped"

# (id, type, widgets, link inputs {name: (src_id, src_slot)}, output slot types)
NODES = [
    (1, "CheckpointLoaderSimple", [CKPT], {}, ["MODEL", "CLIP", "VAE"]),
    (2, "LoraLoader", [LORA, LORA_STRENGTH, LORA_STRENGTH], {"model": (1, 0), "clip": (1, 1)}, ["MODEL", "CLIP"]),
    (3, "CLIPTextEncode", [POSITIVE], {"clip": (2, 1)}, ["CONDITIONING"]),
    (4, "CLIPTextEncode", [NEGATIVE], {"clip": (2, 1)}, ["CONDITIONING"]),
    (5, "EmptyLatentImage", [1024, 1024, 1], {}, ["LATENT"]),
    (6, "KSampler", [123456789, "randomize", 28, 6.0, "euler", "normal", 1.0],
     {"model": (2, 0), "positive": (3, 0), "negative": (4, 0), "latent_image": (5, 0)}, ["LATENT"]),
    (7, "VAEDecode", [], {"samples": (6, 0), "vae": (1, 2)}, ["IMAGE"]),
    (8, "SaveImage", ["sprite_concept"], {"images": (7, 0)}, []),
]
TITLES = {3: "Positive prompt", 4: "Negative prompt"}
POS = {1: (0, 0), 2: (420, 0), 3: (840, -120), 4: (840, 140), 5: (840, 380),
       6: (1300, 0), 7: (1700, 0), 8: (1950, 0)}

API_WIDGET_NAMES = {
    "CheckpointLoaderSimple": ["ckpt_name"],
    "LoraLoader": ["lora_name", "strength_model", "strength_clip"],
    "CLIPTextEncode": ["text"],
    "EmptyLatentImage": ["width", "height", "batch_size"],
    "KSampler": ["seed", "control_after_generate", "steps", "cfg", "sampler_name", "scheduler", "denoise"],
    "VAEDecode": [],
    "SaveImage": ["filename_prefix"],
}
LINK_TYPES = {"model": "MODEL", "clip": "CLIP", "vae": "VAE", "positive": "CONDITIONING",
              "negative": "CONDITIONING", "latent_image": "LATENT", "samples": "LATENT", "images": "IMAGE"}


def build_ui():
    links, link_id, out_links = [], 0, {}
    ids_by_target = {}
    for nid, _t, _w, ins, _o in NODES:
        for name, (src, slot) in ins.items():
            link_id += 1
            links.append([link_id, src, slot, nid, list(ins).index(name), LINK_TYPES[name]])
            ids_by_target[(nid, name)] = link_id
            out_links.setdefault((src, slot), []).append(link_id)
    nodes = []
    for order, (nid, ntype, widgets, ins, outs) in enumerate(NODES):
        node = {
            "id": nid, "type": ntype, "pos": list(POS[nid]), "size": [380, 160], "flags": {},
            "order": order, "mode": 0,
            "inputs": [{"name": n, "type": LINK_TYPES[n], "link": ids_by_target[(nid, n)]} for n in ins],
            "outputs": [{"name": t, "type": t, "links": out_links.get((nid, i)), "slot_index": i}
                        for i, t in enumerate(outs)],
            "properties": {"Node name for S&R": ntype},
            "widgets_values": widgets,
        }
        if nid in TITLES:
            node["title"] = TITLES[nid]
            node["size"] = [420, 200]
        nodes.append(node)
    return {"last_node_id": len(NODES), "last_link_id": link_id, "nodes": nodes, "links": links,
            "groups": [], "config": {}, "extra": {}, "version": 0.4}


def build_api():
    prompt = {}
    for nid, ntype, widgets, ins, _o in NODES:
        names = API_WIDGET_NAMES[ntype]
        inputs = {n: v for n, v in zip(names, widgets) if n != "control_after_generate"}
        for name, (src, slot) in ins.items():
            inputs[name] = [str(src), slot]
        prompt[str(nid)] = {"class_type": ntype, "inputs": inputs}
    return prompt


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--install", metavar="COMFY_DIR", help="also copy the UI workflow into ComfyUI")
    args = ap.parse_args()
    here = Path(__file__).parent
    ui = here / "sprite_concept.workflow.json"
    ui.write_text(json.dumps(build_ui(), indent=2) + "\n")
    (here / "sprite_concept.api.json").write_text(json.dumps(build_api(), indent=2) + "\n")
    print("wrote", ui, "and the .api.json")
    if args.install:
        dest = Path(args.install).expanduser() / "user" / "default" / "workflows"
        dest.mkdir(parents=True, exist_ok=True)
        shutil.copy(ui, dest / ui.name)
        print("installed into", dest)


if __name__ == "__main__":
    main()
