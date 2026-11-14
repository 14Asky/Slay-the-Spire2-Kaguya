"""Losslessly recompress exported Godot 4.5 textures; never resize or quantize.

Usage: python scripts/OptimizeKaguyaTextures.py build/pack-inventory.json
Requires Pillow with WebP support. Writes a new inventory and an audit receipt.
"""
import concurrent.futures
import hashlib
import io
import json
import struct
import sys
from pathlib import Path

from PIL import Image


def digest(data):
    return hashlib.sha256(data).hexdigest()


def optimize(item):
    key, source, destination = item
    before = Path(source).read_bytes()
    cache = destination.with_suffix('.json')
    if destination.exists() and cache.exists():
        receipt = json.loads(cache.read_text(encoding='utf-8'))
        if receipt['sourceSha256'] == key and digest(destination.read_bytes()) == receipt['resultSha256']:
            return receipt
    # GST2 v1, one lossless WebP image, no mipmaps. Refuse unknown layouts.
    if before[:4] != b'GST2' or struct.unpack_from('<I', before, 4)[0] != 1:
        raise ValueError(f'Unknown texture header: {source}')
    data_format, width, height, mipmaps, image_format, size = struct.unpack_from('<IHHIII', before, 36)
    if data_format != 2 or mipmaps != 0 or image_format not in (4, 5) or size + 56 != len(before):
        raise ValueError(f'Unsupported texture layout: {source}')
    with Image.open(io.BytesIO(before[56:])) as image:
        image.load()
        if image.size != (width, height):
            raise ValueError(f'Texture dimensions disagree: {source}')
        rgba = image.convert('RGBA').tobytes()
        encoded = io.BytesIO()
        image.save(encoded, 'WEBP', lossless=True, quality=100, method=6, exact=True)
    payload = encoded.getvalue()
    with Image.open(io.BytesIO(payload)) as decoded:
        if decoded.size != (width, height) or decoded.convert('RGBA').tobytes() != rgba:
            raise ValueError(f'Pixel verification failed: {source}')
    after = before[:52] + struct.pack('<I', len(payload)) + payload
    if len(after) >= len(before):
        after = before
    destination.write_bytes(after)
    receipt = {
        'sourceSha256': key, 'resultSha256': digest(after),
        'sourceBytes': len(before), 'resultBytes': len(after),
        'width': width, 'height': height, 'rgbaSha256': digest(rgba),
        'pixelsIdentical': True, 'source': str(destination.resolve())
    }
    cache.write_text(json.dumps(receipt, indent=2), encoding='utf-8')
    return receipt


def main():
    input_path = Path(sys.argv[1])
    entries = json.loads(input_path.read_text(encoding='utf-8'))
    output = input_path.parent / 'texture-optimization-v325'
    output.mkdir(exist_ok=True)
    unique = {}
    keys = {}
    for entry in entries:
        if not entry['path'].endswith('.ctex'):
            continue
        key = digest(Path(entry['source']).read_bytes())
        keys[entry['path']] = key
        unique.setdefault(key, (key, entry['source'], output / (key + '.ctex')))
    receipts = {}
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
        futures = {pool.submit(optimize, item): item[0] for item in unique.values()}
        for future in concurrent.futures.as_completed(futures):
            receipt = future.result()
            receipts[receipt['sourceSha256']] = receipt
            if len(receipts) % 50 == 0 or len(receipts) == len(unique):
                saved = sum(r['sourceBytes'] - r['resultBytes'] for r in receipts.values())
                print(json.dumps({'completed':len(receipts),'total':len(unique),'uniqueBytesSaved':saved}), flush=True)
    for entry in entries:
        if entry['path'] in keys:
            entry['source'] = receipts[keys[entry['path']]]['source']
    (input_path.parent/'texture-optimized-inventory.json').write_text(json.dumps(entries, ensure_ascii=False), encoding='utf-8')
    report = {'mode':'lossless-exact-rgba', 'uniqueTextures':len(unique), 'texturePaths':len(keys),
              'paths':keys, 'receipts':list(receipts.values())}
    (input_path.parent/'texture-optimization-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'success':True,'uniqueTextures':len(unique),'texturePaths':len(keys),
                      'uniqueBytesSaved':sum(r['sourceBytes']-r['resultBytes'] for r in receipts.values())}),flush=True)


if __name__ == '__main__':
    main()
