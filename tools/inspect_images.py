from PIL import Image
from pathlib import Path
root=Path(r'D:\MyCardLibrary\mdpro3\Picture\Art')
for p in list(root.glob('*.jpg'))[:5]+list(root.glob('*.png'))[:5]:
    try:
        im=Image.open(p)
        print(p.name, im.size, im.mode)
    except Exception as e: print('ERR',p,e)
