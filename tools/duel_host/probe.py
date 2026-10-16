import subprocess, json, time, os, sys
cmd=[r'D:\MDProMod\tools\dotnet\dotnet.exe',r'D:\MDProMod\tools\duel_host\bin\Release\duel_host.dll','--root',r'D:\MDProMod\tools\ygo_tcp_probe\staging']
p=subprocess.Popen(cmd,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
p.stdin.write(json.dumps({'cmd':'start','deck0':'starter.ydk','deck1':'starter.ydk'})+'\n')
p.stdin.flush()
time.sleep(1)
p.stdin.write(json.dumps({'cmd':'response','value':7})+'\n')
p.stdin.flush()
time.sleep(1)
p.stdin.write(json.dumps({'cmd':'stop'})+'\n')
p.stdin.flush(); p.stdin.close()
out,err=p.communicate(timeout=10)
print('OUT',repr(out)); print('ERR',repr(err)); print('RC',p.returncode)
