import json, os, uuid, re, sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
A=ROOT+'/Assets'
S=os.path.dirname(os.path.abspath(__file__))

def read_guid(path):
    m=path+'.meta'
    if os.path.exists(m):
        g=re.search(r'guid: ([0-9a-f]{32})',open(m).read())
        if g: return g.group(1)
    return None

def new_guid(): return uuid.uuid4().hex

def write_meta(path, kind):
    if os.path.exists(path+'.meta'): return read_guid(path)
    g=new_guid()
    if kind=='folder':
        body=f"fileFormatVersion: 2\nguid: {g}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif kind=='script':
        body=f"fileFormatVersion: 2\nguid: {g}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif kind=='asset':
        body=f"fileFormatVersion: 2\nguid: {g}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    else:
        body=f"fileFormatVersion: 2\nguid: {g}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    open(path+'.meta','w').write(body)
    return g

# 1) metas for scripts & folders
def meta_all():
    for d,dirs,files in os.walk(A):
        for dn in dirs: write_meta(os.path.join(d,dn),'folder')
        for fn in files:
            if fn.endswith('.meta'): continue
            p=os.path.join(d,fn)
            k='script' if fn.endswith('.cs') else 'asset' if fn.endswith('.asset') else 'default'
            write_meta(p,k)
meta_all()

SC=A+'/Riftbound/Scripts'
g_card=read_guid(SC+'/Config/CardDefinition.cs')
g_cfg=read_guid(SC+'/Config/GameConfig.cs')
g_boot=read_guid(SC+'/Core/GameBootstrap.cs')

def fmt(v):
    if isinstance(v,bool): return '1' if v else '0'
    if isinstance(v,(int,)): return str(v)
    if isinstance(v,float):
        s=repr(v)
        if s.endswith('.0'): s=s[:-2]
        return s
    if isinstance(v,str):
        return '"'+v.replace('\\','\\\\').replace('"','\\"')+'"'
    raise Exception(v)

cards=json.load(open(sys.argv[1] if len(sys.argv) > 1 else os.path.join(S, 'cards.json')))
cards_dir=A+'/Riftbound/Config/Cards'
os.makedirs(cards_dir,exist_ok=True)
def title(i): return i[0]+i[1:].lower()
guid_of={}
# first pass: create metas to know guids
for c in cards:
    d=dict((k,v) for k,v in c)
    fn=cards_dir+'/'+title(d['cardId'])+'.asset'
    if not os.path.exists(fn): open(fn,'w').write('')
    guid_of[d['cardId']]=write_meta(fn,'asset')

def yaml_fields(pairs, indent):
    out=''
    for k,v in pairs:
        if k=='m_Name': continue
        if isinstance(v,list):
            out+=' '*indent+k+':\n'+yaml_fields(v,indent+2)
        elif isinstance(v,dict) and '$ref' in v:
            out+=' '*indent+f"{k}: {{fileID: 11400000, guid: {guid_of[v['$ref']]}, type: 2}}\n"
        elif v is None:
            out+=' '*indent+f"{k}: {{fileID: 0}}\n"
        else:
            out+=' '*indent+f"{k}: {fmt(v)}\n"
    return out

HEAD="%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
for c in cards:
    d=dict((k,v) for k,v in c)
    name=title(d['cardId'])
    body=HEAD+f"  m_Script: {{fileID: 11500000, guid: {g_card}, type: 3}}\n  m_Name: {name}\n  m_EditorClassIdentifier: \n"+yaml_fields(c,2)
    open(cards_dir+'/'+name+'.asset','w').write(body)

deck=['MAW','BLINK','LEECH','ANCHOR','HUNTER','SWARM','PULSE','PARASITE']
res=A+'/Riftbound/Resources'; os.makedirs(res,exist_ok=True)
cfgp=res+'/RiftboundConfig.asset'
if not os.path.exists(cfgp): open(cfgp,'w').write('')
g_cfgasset=write_meta(cfgp,'asset')
body=HEAD+f"  m_Script: {{fileID: 11500000, guid: {g_cfg}, type: 3}}\n  m_Name: RiftboundConfig\n  m_EditorClassIdentifier: \n  deck:\n"
for id in deck: body+=f"  - {{fileID: 11400000, guid: {guid_of[id]}, type: 2}}\n"
open(cfgp,'w').write(body)

# scene
scene=A+'/Riftbound/Scenes/Main.unity'
open(scene,'w').write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 100001}}
  - component: {{fileID: 100002}}
  m_Layer: 0
  m_Name: Riftbound
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &100002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {g_boot}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  config: {{fileID: 11400000, guid: {g_cfgasset}, type: 2}}
  startInDebugMode: 0
  seed: 0
""")
g_scene=write_meta(scene,'default')

PS=ROOT+'/ProjectSettings'; os.makedirs(PS,exist_ok=True)
open(PS+'/EditorBuildSettings.asset','w').write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: Assets/Riftbound/Scenes/Main.unity
    guid: {g_scene}
  m_configObjects: {{}}
""")
print('ok', g_card, g_cfg, g_boot, g_cfgasset, g_scene)
