import qoi
from PIL import Image
import numpy
import os
import sys

input = sys.argv[1]

def convert(path):
    with Image.open(path) as im:
        qoi.write(path.removesuffix('png') + 'qoi', numpy.array(im))


if os.path.isdir(input):
    for p in os.listdir(input):
        if p.endswith('png'):
            convert(p)
else:
    convert(input)
