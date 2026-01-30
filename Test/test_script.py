import sys
import os
import random
import traceback
import time

# A script for hacking on, that's all.

lines = []
with open('ross_1.txt') as f:
    lines = f.read().splitlines()
lenl = len(lines)

# with open("heroes.txt", "r") as heroes:    
# hlist = heroes.read().splitlines()


start_ms = time.time() * 1000.0

for i in range(5):
    r =  random.randrange(0, lenl)
    now_ms = time.time() * 1000.0
    # readlines() includes NL so remove it.
    print(f'[{i} {now_ms - start_ms}]{lines[r]}') # {lines[r].rstrip()}
    # In order for the Term host to get the lines in realtime - flush now.
    sys.stdout.flush()
    time.sleep(1.0)

sys.exit(999)
