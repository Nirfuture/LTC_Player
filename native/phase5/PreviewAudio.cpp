// SPDX-License-Identifier: MIT
#define NOMINMAX
#include <windows.h>
#include <mmdeviceapi.h>
#include <audioclient.h>
#include <wrl/client.h>
#include <memory>
#include <algorithm>
#include <cmath>
#include <atomic>
using Microsoft::WRL::ComPtr;
static std::atomic<int> activeAudio{0};
struct PreviewAudio {
 bool com=false,started=false,counted=false;UINT32 capacity=0;UINT64 written=0;float peak=0;
 ComPtr<IMMDeviceEnumerator> enumerator;ComPtr<IMMDevice> endpoint;
 ComPtr<IAudioClient> client;ComPtr<IAudioRenderClient> render;
 ~PreviewAudio(){if(client){client->Stop();client->Reset();}render.Reset();client.Reset();endpoint.Reset();enumerator.Reset();if(counted)--activeAudio;if(com)CoUninitialize();}
};
static void AudioCheck(HRESULT hr){if(FAILED(hr))throw hr;}
extern "C" __declspec(dllexport) int __cdecl pa_open(void** result) {
 if(!result)return E_INVALIDARG;*result=nullptr;
 try {
  auto a=std::make_unique<PreviewAudio>();AudioCheck(CoInitializeEx(nullptr,COINIT_MULTITHREADED));a->com=true;
  AudioCheck(CoCreateInstance(__uuidof(MMDeviceEnumerator),nullptr,CLSCTX_ALL,IID_PPV_ARGS(&a->enumerator)));
  AudioCheck(a->enumerator->GetDefaultAudioEndpoint(eRender,eMultimedia,&a->endpoint));
  AudioCheck(a->endpoint->Activate(__uuidof(IAudioClient),CLSCTX_ALL,nullptr,(void**)&a->client));
  WAVEFORMATEX f={WAVE_FORMAT_IEEE_FLOAT,2,48000,48000*8,8,32,0};GUID session;AudioCheck(CoCreateGuid(&session));
  AudioCheck(a->client->Initialize(AUDCLNT_SHAREMODE_SHARED,AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM|AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,
   1000000,0,&f,&session));
  AudioCheck(a->client->GetBufferSize(&a->capacity));AudioCheck(a->client->GetService(IID_PPV_ARGS(&a->render)));
  a->counted=true;++activeAudio;*result=a.release();return S_OK;
 }catch(HRESULT hr){return hr;}catch(...){return E_OUTOFMEMORY;}
}
extern "C" __declspec(dllexport) int __cdecl pa_write(void* handle,const float* samples,int offsetFrames,int frames,float gain,int* written) {
 auto a=(PreviewAudio*)handle;if(!a||!samples||!written||offsetFrames<0||frames<0||!std::isfinite(gain)||gain<0||gain>1)return E_INVALIDARG;*written=0;
 try {
  UINT32 padding;AudioCheck(a->client->GetCurrentPadding(&padding));UINT32 count=std::min((UINT32)frames,a->capacity-padding);
  if(!count)return S_OK;BYTE* data;AudioCheck(a->render->GetBuffer(count,&data));float peak=0;
  for(UINT32 i=0;i<count*2;++i){float v=samples[offsetFrames*2+i];v=std::isfinite(v)?std::clamp(v*gain,-1.f,1.f):0.f;((float*)data)[i]=v;peak=std::max(peak,std::abs(v));}
  AudioCheck(a->render->ReleaseBuffer(count,0));a->written+=count;a->peak=peak;*written=(int)count;return S_OK;
 }catch(HRESULT hr){return hr;}
}
extern "C" __declspec(dllexport) int __cdecl pa_run(void* handle,int running) {
 auto a=(PreviewAudio*)handle;if(!a)return E_INVALIDARG;
 HRESULT hr=S_OK;if(running&&!a->started){hr=a->client->Start();if(SUCCEEDED(hr))a->started=true;}
 else if(!running&&a->started){hr=a->client->Stop();if(SUCCEEDED(hr))a->started=false;}
 return FAILED(hr)?hr:S_OK;
}
extern "C" __declspec(dllexport) int __cdecl pa_stats(void* handle,UINT64* written,UINT64* consumed,float* peak) {
 auto a=(PreviewAudio*)handle;if(!a||!written||!consumed||!peak)return E_INVALIDARG;
 UINT32 padding;HRESULT hr=a->client->GetCurrentPadding(&padding);if(FAILED(hr))return hr;
 *written=a->written;*consumed=a->written>=padding?a->written-padding:0;*peak=a->peak;return S_OK;
}
extern "C" __declspec(dllexport) void __cdecl pa_close(void* handle){delete (PreviewAudio*)handle;}
extern "C" __declspec(dllexport) int __cdecl pa_active(){return activeAudio.load();}

