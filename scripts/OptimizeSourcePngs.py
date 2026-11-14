"""Recompress PNG IDAT streams without changing pixels, filters or metadata."""
import concurrent.futures
import hashlib
import json
import struct
import sys
import zlib
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
BACKUP = ROOT/'build/asset-source-backup'


def optimize(name):
    source = (ROOT/name).resolve()
    source.relative_to(ROOT)
    before = source.read_bytes()
    if not before.startswith(b'\x89PNG\r\n\x1a\n'):
        raise ValueError(f'Invalid PNG: {name}')
    chunks, offset = [], 8
    while offset < len(before):
        length = struct.unpack_from('>I', before, offset)[0]
        raw = before[offset:offset+length+12]
        kind, data = raw[4:8], raw[8:-4]
        if len(raw) != length+12 or zlib.crc32(raw[4:-4]) != struct.unpack('>I', raw[-4:])[0]:
            raise ValueError(f'Invalid PNG chunk: {name}')
        chunks.append((kind, data, raw))
        offset += len(raw)
    compressed = b''.join(data for kind, data, _ in chunks if kind == b'IDAT')
    filtered = zlib.decompress(compressed)
    replacement = zlib.compress(filtered, 9)
    after = before
    if len(replacement) < len(compressed):
        encoded = b'IDAT' + replacement
        idat = struct.pack('>I', len(replacement)) + encoded + struct.pack('>I', zlib.crc32(encoded))
        parts, wrote = [before[:8]], False
        for kind, _, raw in chunks:
            if kind == b'IDAT':
                if not wrote:
                    parts.append(idat)
                    wrote = True
            else:
                parts.append(raw)
        after = b''.join(parts)
        assert zlib.decompress(replacement) == filtered
        backup = BACKUP/name
        backup.parent.mkdir(parents=True, exist_ok=True)
        if not backup.exists():
            backup.write_bytes(before)
        source.write_bytes(after)
    return {'path':name, 'before':len(before), 'after':len(after),
            'sourceSha256':hashlib.sha256(before).hexdigest(),
            'resultSha256':hashlib.sha256(after).hexdigest(),
            'filteredPixelsSha256':hashlib.sha256(filtered).hexdigest(),
            'pixelsAndMetadataIdentical':True}


if __name__ == '__main__':
    audit = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
    names = [a['path'] for a in audit['assets'] if a['path'].lower().endswith('.png') and (ROOT/a['path']).exists()]
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        results = list(pool.map(optimize, names))
    report = {'files':results,'checked':len(results),'changed':sum(r['after']<r['before'] for r in results),
              'bytesSaved':sum(r['before']-r['after'] for r in results)}
    (ROOT/'build/source-png-optimization-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k != 'files'}))
