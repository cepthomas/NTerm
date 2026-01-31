import sys
import os
import random
import traceback
import time

# A script for hacking on, that's all.

lines = []
with open('ross_1.txt') as f:
    lines = f.read().splitlines()

start_ms = time.time() * 1000.0

for i in range(5):
    r =  random.randrange(0, len(lines))
    now_ms = time.time() * 1000.0
    print(f'[{i} {now_ms - start_ms}]{lines[r]}')
    time.sleep(0.5)

sys.exit(999)
