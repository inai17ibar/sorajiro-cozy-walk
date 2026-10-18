#!/usr/bin/env python3
"""Encode existing Unity frames with their measured timestamps, without overwriting."""
import argparse
import csv
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('capture', type=Path)
    args = parser.parse_args()
    folder = args.capture.resolve()
    folder.relative_to((ROOT / 'Artifacts/Visual/motion').resolve())
    assert not args.capture.is_symlink() and (folder / 'complete.txt').is_file()
    rows = list(csv.DictReader((folder / 'frames.csv').open()))
    assert len(rows) > 1
    timestamps = [float(row['time_s']) for row in rows]
    assert all(b > a for a, b in zip(timestamps, timestamps[1:]))
    video = folder / 'twilight-motion.mp4'
    concat = folder / 'frames.ffconcat'
    assert not video.exists() and not concat.exists(), 'Existing media preserved.'
    lines = ['ffconcat version 1.0']
    for index, row in enumerate(rows):
        name = f"{int(row['frame']):05d}.jpg"
        assert (folder / name).is_file()
        lines.append(f"file '{name}'")
        if index + 1 < len(rows):
            lines.append(f'duration {timestamps[index+1]-timestamps[index]:.6f}')
    concat.write_text('\n'.join(lines) + '\n')
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-n', '-safe', '0',
                    '-f', 'concat', '-i', str(concat), '-fps_mode', 'vfr', '-c:v', 'libx264',
                    '-crf', '18', '-pix_fmt', 'yuv420p', str(video)], check=True)
    print(f'Encoded {len(rows)} actual frames; variable timestamps; not game FPS: {video}')


if __name__ == '__main__':
    main()
