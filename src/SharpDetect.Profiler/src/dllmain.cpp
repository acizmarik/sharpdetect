// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "ClassFactory.h"
#include "EasyLogging.h"

INITIALIZE_EASYLOGGINGPP

const IID IID_IUnknown = { 0x00000000, 0x0000, 0x0000, { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };
const IID IID_IClassFactory = { 0x00000001, 0x0000, 0x0000, { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };

BOOL STDMETHODCALLTYPE DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	return TRUE;
}

extern "C" HRESULT STDMETHODCALLTYPE DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
	// {79d2f672-5206-11eb-ae93-0242ac130002}
	const GUID CLSID_CorProfiler = { 0x79d2f672, 0x5206, 0x11eb, { 0xae, 0x93, 0x02, 0x42, 0xac, 0x13, 0x00, 0x02 } };

	if (ppv == nullptr || rclsid != CLSID_CorProfiler)
		return E_FAIL;

	auto factory = new ClassFactory();
	if (factory == nullptr)
		return E_FAIL;

	return factory->QueryInterface(riid, ppv);
}

extern "C" HRESULT STDMETHODCALLTYPE DllCanUnloadNow()
{
	return S_OK;
}
