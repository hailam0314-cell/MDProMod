from pathlib import Path
import sys,io
sys.path.insert(0,r'D:/MDProMod/tools/unitypy')
sys.path.insert(0,r'D:/MDProMod/tools/audio_pipeline/deps')
import UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data
import fmod_toolkit.fmod as fmod
import soundfile as sf

path=Path(sys.argv[1])
e=UnityPy.load(str(path))
a=[o.read() for o in e.objects if o.type.name=='AudioClip'][0]
r=a.m_Resource
raw=get_resource_data(r.m_Source,a.object_reader.assets_file,r.m_Offset,r.m_Size)
print('RAW',a.m_Name,len(raw),raw[:16],flush=True)
system,lock=fmod.get_pyfmodex_system_instance(a.m_Channels,fmod.pyfmodex.flags.INIT_FLAGS.NORMAL)
print('SYSTEM',flush=True)
sound=system.create_sound(bytes(raw), fmod.pyfmodex.flags.MODE.OPENMEMORY,
    exinfo=fmod.pyfmodex.structure_declarations.CREATESOUNDEXINFO(length=len(raw),numchannels=a.m_Channels,defaultfrequency=a.m_Frequency))
print('SOUND',sound.num_subsounds,flush=True)
sub=sound.get_subsound(0)
print('SUB',sub.format,sub.get_length(fmod.pyfmodex.enums.TIMEUNIT.PCMBYTES),flush=True)
wav=fmod.subsound_to_wav(sub)
print('WAV',len(wav),flush=True)
pcm,rate=sf.read(io.BytesIO(wav),dtype='float32',always_2d=True)
print('PCM',pcm.shape,rate,flush=True)
sf.write(Path(__file__).with_name('probe.ogg'),pcm.mean(axis=1),rate,format='OGG',subtype='VORBIS',compression_level=0.7)
print('OGG',flush=True)
