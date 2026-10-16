from pathlib import Path
import sys, collections
sys.path.insert(0, str(Path(r'D:/MDProMod/tools/unitypy')))
import UnityPy

ROOT=Path(r'D:/MyCardLibrary/mdpro3/MDPro3_Data/StandaloneWindows64/MDPro3')
for f in list(ROOT.glob('sound_assets_se_*.bundle'))[:4]:
    if f.is_file():
        try:
            e=UnityPy.load(str(f)); c=collections.Counter(x.type.name for x in e.objects)
            print(f.name, f.stat().st_size, len(e.objects), dict(c))
            for obj in e.objects:
                if obj.type.name == 'AudioClip':
                    a=obj.read()
                    print('CLIP', a.m_Name, a.m_Length, a.m_Channels, a.m_Frequency, a.m_CompressionFormat)
                    for n, raw in a.samples.items():
                        print('SAMPLE', n,len(raw),raw[:12])
        except Exception as ex:
            print('ERR', f, type(ex).__name__, ex)
