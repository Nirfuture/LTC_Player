// A canvas owns decoding/composition resources independently of presentation windows.
extern "C" __declspec(dllexport) int __cdecl canvas_create(int width,int height,void** result){
 if(!result||width<4||height<4||width>8192||height>8192)return E_INVALIDARG;
 *result=nullptr;
 try{
  auto o=std::make_unique<Output>();o->width=width;o->height=height;o->offscreen=true;o->ownsWindow=false;
  Check(D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,D3D11_CREATE_DEVICE_BGRA_SUPPORT,nullptr,0,D3D11_SDK_VERSION,&o->device,nullptr,&o->context));
  D3D11_TEXTURE2D_DESC td={};td.Width=width;td.Height=height;td.MipLevels=td.ArraySize=1;td.Format=DXGI_FORMAT_B8G8R8A8_UNORM;td.SampleDesc.Count=1;td.BindFlags=D3D11_BIND_SHADER_RESOURCE;
  Check(o->device->CreateTexture2D(&td,nullptr,&o->upload));*result=o.release();return S_OK;
 }catch(HRESULT hr){return hr;}catch(...){return E_OUTOFMEMORY;}
}
extern "C" __declspec(dllexport) int __cdecl canvas_detach(void* handle){
 auto o=(Output*)handle;if(!o||!o->offscreen)return E_INVALIDARG;
 o->swap.Reset();if(o->window&&IsWindow(o->window)){SetWindowLongPtrW(o->window,GWLP_USERDATA,0);DestroyWindow(o->window);}
 o->window=nullptr;o->stopRequested=false;o->externalError=0;return S_OK;
}
extern "C" __declspec(dllexport) int __cdecl canvas_attach(void* handle,const Display* selected,int compatibility,int probe){
 auto o=(Output*)handle;if(!o||!o->offscreen||!selected||o->swap)return E_INVALIDARG;
 if(selected->width!=o->width||selected->height!=o->height)return E_INVALIDARG;
 bool found=false;for(auto& d:Displays())if(!wcscmp(d.name,selected->name)&&d.x==selected->x&&d.y==selected->y&&d.width==selected->width&&d.height==selected->height)found=true;
 if(!found)return HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_CONNECTED);
 HWND window=nullptr;
 try{
  WNDCLASSW cls={};cls.lpfnWndProc=OutputProc;cls.hInstance=GetModuleHandleW(nullptr);cls.lpszClassName=L"LtcPlayerOutput";cls.hbrBackground=(HBRUSH)GetStockObject(BLACK_BRUSH);
  if(!RegisterClassW(&cls)&&GetLastError()!=ERROR_CLASS_ALREADY_EXISTS)return HRESULT_FROM_WIN32(GetLastError());
  window=CreateWindowExW(WS_EX_NOACTIVATE,cls.lpszClassName,L"LTC Player Output",WS_POPUP,selected->x,selected->y,probe?640:o->width,probe?360:o->height,nullptr,nullptr,GetModuleHandleW(nullptr),nullptr);
  if(!window)return HRESULT_FROM_WIN32(GetLastError());
  ComPtr<IDXGIDevice> dxgi;Check(o->device.As(&dxgi));ComPtr<IDXGIAdapter> adapter;Check(dxgi->GetAdapter(&adapter));ComPtr<IDXGIFactory> factory;Check(adapter->GetParent(IID_PPV_ARGS(&factory)));
  DXGI_SWAP_CHAIN_DESC sd={};sd.BufferDesc.Width=o->width;sd.BufferDesc.Height=o->height;sd.BufferDesc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;sd.SampleDesc.Count=1;sd.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;sd.BufferCount=2;sd.OutputWindow=window;sd.Windowed=TRUE;sd.SwapEffect=compatibility?DXGI_SWAP_EFFECT_DISCARD:DXGI_SWAP_EFFECT_FLIP_DISCARD;
  ComPtr<IDXGISwapChain> swap;Check(factory->CreateSwapChain(o->device.Get(),&sd,&swap));Check(factory->MakeWindowAssociation(window,DXGI_MWA_NO_ALT_ENTER));
  if(o->last){ComPtr<ID3D11Texture2D> back;Check(swap->GetBuffer(0,IID_PPV_ARGS(&back)));o->context->CopyResource(back.Get(),o->last.Get());Check(swap->Present(0,0));}
  o->externalError=0;o->window=window;o->swap=swap;o->confirmClose=true;o->stopRequested=false;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)o);
  ShowWindow(window,SW_SHOWNOACTIVATE);SetWindowPos(window,probe?HWND_NOTOPMOST:HWND_TOPMOST,selected->x,selected->y,probe?640:o->width,probe?360:o->height,SWP_NOACTIVATE|SWP_SHOWWINDOW);return S_OK;
 }catch(HRESULT hr){if(window)DestroyWindow(window);return hr;}catch(...){if(window)DestroyWindow(window);return E_OUTOFMEMORY;}
}
// Re-present the existing composition; attaching/detaching never re-composes or changes it.
extern "C" __declspec(dllexport) int __cdecl canvas_output_refresh(void* handle){
 auto o=(Output*)handle;if(!o||!o->offscreen)return E_INVALIDARG;if(!o->swap||!o->last)return S_FALSE;
 try{if(!IsWindow(o->window))return HRESULT_FROM_WIN32(ERROR_INVALID_WINDOW_HANDLE);ComPtr<ID3D11Texture2D> back;Check(o->swap->GetBuffer(0,IID_PPV_ARGS(&back)));o->context->CopyResource(back.Get(),o->last.Get());return o->swap->Present(0,DXGI_PRESENT_DO_NOT_WAIT);}catch(HRESULT hr){return hr;}
}

extern "C" __declspec(dllexport) int __cdecl canvas_output_error(void* handle){auto o=(Output*)handle;return o?o->externalError:E_INVALIDARG;}

extern "C" __declspec(dllexport) int __cdecl canvas_device_error(void* handle){auto o=(Output*)handle;return o&&o->device?o->device->GetDeviceRemovedReason():E_INVALIDARG;}
