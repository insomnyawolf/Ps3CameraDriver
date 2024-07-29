﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using DirectN;
using VCamNetSampleSource.Utilities;
using VirtualCameraCommon;

namespace VCamNetSampleSource
{
    public class MediaStream : MFAttributes, IMFMediaStream2, IKsControl
    {
        
        private uint Width => _generator.Width;
        private uint Height => _generator.Height;

        public const int NUM_ALLOCATOR_SAMPLES = 10;

        private readonly object _lock = new();
        private readonly MediaSource _source;
        private IComObject<IMFMediaEventQueue>? _queue;
        private IComObject<IMFStreamDescriptor>? _descriptor;
        private IComObject<IMFVideoSampleAllocatorEx>? _allocator;
        private _MF_STREAM_STATE _state;
        private Guid _format;

        public readonly FrameConfiguration FrameConfiguration;

        private Ps3CamFrameSource _generator = new();

        public MediaStream(MediaSource source, uint index)
        {

            try
            {
                ArgumentNullException.ThrowIfNull(source);
                _source = source;

                SetGUID(MFConstants.MF_DEVICESTREAM_STREAM_CATEGORY, KSMedia.PINNAME_VIDEO_CAPTURE).ThrowOnError();
                SetUINT32(MFConstants.MF_DEVICESTREAM_STREAM_ID, index).ThrowOnError();
                SetUINT32(MFConstants.MF_DEVICESTREAM_FRAMESERVER_SHARED, 1).ThrowOnError();
                SetUINT32(MFConstants.MF_DEVICESTREAM_ATTRIBUTE_FRAMESOURCE_TYPES, (uint)_MFFrameSourceTypes.MFFrameSourceTypes_Color).ThrowOnError();

                Functions.MFCreateEventQueue(out var queue).ThrowOnError();
                _queue = new ComObject<IMFMediaEventQueue>(queue);

                // set 1 here to force RGB32 only
                var mediaTypes = new IMFMediaType[2];

                // RGB
                Functions.MFCreateMediaType(out var rgbType).ThrowOnError();
                rgbType.SetGUID(MFConstants.MF_MT_MAJOR_TYPE, MFConstants.MFMediaType_Video).ThrowOnError();
                rgbType.SetGUID(MFConstants.MF_MT_SUBTYPE, MFConstants.MFVideoFormat_RGB32).ThrowOnError();
                rgbType.SetSize(MFConstants.MF_MT_FRAME_SIZE, Width, Height);
                rgbType.SetUINT32(MFConstants.MF_MT_DEFAULT_STRIDE, Width * 4).ThrowOnError();
                rgbType.SetUINT32(MFConstants.MF_MT_INTERLACE_MODE, (uint)_MFVideoInterlaceMode.MFVideoInterlace_Progressive).ThrowOnError();
                rgbType.SetUINT32(MFConstants.MF_MT_ALL_SAMPLES_INDEPENDENT, 1).ThrowOnError();
                rgbType.SetRatio(MFConstants.MF_MT_FRAME_RATE, 30, 1);
                var bitrate = Width * 4 * Height * 8 * 30;
                rgbType.SetUINT32(MFConstants.MF_MT_AVG_BITRATE, (uint)bitrate).ThrowOnError();
                rgbType.SetRatio(MFConstants.MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
                mediaTypes[0] = rgbType;

                // NV12
                if (mediaTypes.Length > 1)
                {
                    Functions.MFCreateMediaType(out var nv12Type).ThrowOnError();
                    nv12Type.SetGUID(MFConstants.MF_MT_MAJOR_TYPE, MFConstants.MFMediaType_Video).ThrowOnError();
                    nv12Type.SetGUID(MFConstants.MF_MT_SUBTYPE, MFConstants.MFVideoFormat_NV12).ThrowOnError();
                    nv12Type.SetSize(MFConstants.MF_MT_FRAME_SIZE, Width, Height);
                    nv12Type.SetUINT32(MFConstants.MF_MT_DEFAULT_STRIDE, Width * 3 / 2).ThrowOnError();
                    nv12Type.SetUINT32(MFConstants.MF_MT_INTERLACE_MODE, (uint)_MFVideoInterlaceMode.MFVideoInterlace_Progressive).ThrowOnError();
                    nv12Type.SetUINT32(MFConstants.MF_MT_ALL_SAMPLES_INDEPENDENT, 1).ThrowOnError();
                    nv12Type.SetRatio(MFConstants.MF_MT_FRAME_RATE, 30, 1);
                    bitrate = Width * 3 * Height * 8 * 30 / 2;
                    nv12Type.SetUINT32(MFConstants.MF_MT_AVG_BITRATE, (uint)bitrate).ThrowOnError();
                    nv12Type.SetRatio(MFConstants.MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
                    mediaTypes[1] = nv12Type;
                }

                Functions.MFCreateStreamDescriptor(index, mediaTypes.Length, mediaTypes, out var descriptor).ThrowOnError();
                descriptor.GetMediaTypeHandler(out var handler).ThrowOnError();
                handler.SetCurrentMediaType(mediaTypes[0]).ThrowOnError();
                _descriptor = new ComObject<IMFStreamDescriptor>(descriptor);
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT Start(IMFMediaType? type)
        {
            var queue = _queue;
            var allocator = _allocator;
            if (queue == null || allocator == null)
            {
                EventProvider.LogInfo($"MF_E_SHUTDOWN");
                return HRESULTS.MF_E_SHUTDOWN;
            }

            if (type != null)
            {
                allocator.Object.InitializeSampleAllocator(NUM_ALLOCATOR_SAMPLES, type).ThrowOnError();

                type.GetGUID(MFConstants.MF_MT_SUBTYPE, out _format).ThrowOnError();
                EventProvider.LogInfo("Format: " + _format.GetMFName());
            }

            // at this point, set D3D manager may have not been called
            // so we want to create a D2D1 renter target anyway
            _generator.EnsureRenderTarget().ThrowOnError();

            queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEStreamStarted, Guid.Empty, HRESULTS.S_OK, null).ThrowOnError();
            _state = _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING;
            EventProvider.LogInfo("Started");
            return HRESULTS.S_OK;
        }

        public HRESULT Stop()
        {
            var queue = _queue;
            var allocator = _allocator;
            if (queue == null || allocator == null)
            {
                EventProvider.LogInfo($"MF_E_SHUTDOWN");
                return HRESULTS.MF_E_SHUTDOWN;
            }

            allocator.Object.UninitializeSampleAllocator();
            queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEStreamStopped, Guid.Empty, HRESULTS.S_OK, null).ThrowOnError();
            _state = _MF_STREAM_STATE.MF_STREAM_STATE_STOPPED;
            return HRESULTS.S_OK;
        }

        public MFSampleAllocatorUsage GetAllocatorUsage() => MFSampleAllocatorUsage.MFSampleAllocatorUsage_UsesProvidedAllocator;
        public HRESULT SetAllocator(object allocator)
        {
            if (allocator == null)
            {
                EventProvider.LogInfo($"E_POINTER");
                return HRESULTS.E_POINTER;
            }

            if (allocator is not IMFVideoSampleAllocatorEx aex)
            {
                EventProvider.LogInfo($"E_NOINTERFACE");
                return HRESULTS.E_NOINTERFACE;
            }

            _allocator = new ComObject<IMFVideoSampleAllocatorEx>(aex);
            return HRESULTS.S_OK;
        }

        public HRESULT Set3DManager(object manager)
        {
            var allocator = _allocator;
            if (allocator == null)
            {
                EventProvider.LogInfo($"E_POINTER");
                return HRESULTS.E_POINTER;
            }

            allocator.Object.SetDirectXManager(manager).ThrowOnError();
            var hr = _generator.SetD3DManager(manager);
            return hr;
        }

        public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
        {
            EventProvider.LogInfo($"iid{iid:B}");
            ppv = 0;
            return CustomQueryInterfaceResult.NotHandled;
        }

        public HRESULT GetEvent(uint flags, out IMFMediaEvent evt)
        {
            EventProvider.LogInfo($"flags:{flags}");
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    if (queue == null)
                    {
                        evt = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = queue.Object.GetEvent(flags, out evt);
                    EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT BeginGetEvent(IMFAsyncCallback callback, object state)
        {
            //EventProvider.LogInfo($"callback:{callback} state:{state}");
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    if (queue == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = queue.Object.BeginGetEvent(callback, state);
                    //EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT EndGetEvent(IMFAsyncResult result, out IMFMediaEvent evt)
        {
            //EventProvider.LogInfo($"result:{result}");
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    if (queue == null)
                    {
                        evt = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = queue.Object.EndGetEvent(result, out evt);
                    //EventProvider.LogInfo($" evt {evt}=> {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT QueueEvent(uint type, Guid extendedType, HRESULT hrStatus, PROPVARIANT value)
        {
            EventProvider.LogInfo($"type:{type} value:{value}");
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    if (queue == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = queue.Object.QueueEventParamVar(type, extendedType, hrStatus, value);
                    EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT GetMediaSource(out IMFMediaSource ppMediaSource)
        {
            EventProvider.LogInfo();
            lock (_lock)
            {
                var source = _source;
                if (source == null)
                {
                    ppMediaSource = null!;
                    return HRESULTS.MF_E_SHUTDOWN;
                }

                ppMediaSource = source;
                var hr = HRESULTS.S_OK;
                EventProvider.LogInfo($" => {hr}");
                return hr;
            }
        }

        public HRESULT GetStreamDescriptor(out IMFStreamDescriptor streamDescriptor)
        {
            EventProvider.LogInfo();
            try
            {
                lock (_lock)
                {
                    var descriptor = _descriptor;
                    if (descriptor == null)
                    {
                        streamDescriptor = null!;
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    streamDescriptor = descriptor.Object;
                    var hr = HRESULTS.S_OK;
                    EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT RequestSample(object token)
        {
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    var allocator = _allocator;
                    if (allocator == null || queue == null)
                        return HRESULTS.MF_E_SHUTDOWN;

                    allocator.Object.AllocateSample(out var sample).ThrowOnError();

                    using (var inputSample = new ComObject<IMFSample>(sample))
                    {
                        sample.SetSampleTime(Functions.MFGetSystemTime()).ThrowOnError();
                        sample.SetSampleDuration(333333).ThrowOnError();

                        using var outputSample = _generator.Generate(inputSample, _format);
                        if (token != null)
                        {
                            outputSample.Object.SetUnknown(MFConstants.MFSampleExtension_Token, token).ThrowOnError();
                        }

                        queue.Object.QueueEventParamUnk((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEMediaSample, Guid.Empty, HRESULTS.S_OK, outputSample.Object).ThrowOnError();
                    }

                    // we must do this sometimes, otherwise the allocator gets full too early
                    if (_generator.FrameCount % (NUM_ALLOCATOR_SAMPLES / 2) == 0)
                    {
                        GC.Collect();
                    }
                    return HRESULTS.S_OK;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT SetStreamState(_MF_STREAM_STATE value)
        {
            EventProvider.LogInfo($"value:{value}");
            try
            {
                if (_state != value)
                {
                    switch (value)
                    {
                        case _MF_STREAM_STATE.MF_STREAM_STATE_STOPPED:
                            return Stop();

                        case _MF_STREAM_STATE.MF_STREAM_STATE_PAUSED:
                            if (_state != _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING)
                            {
                                EventProvider.LogInfo($"MF_E_INVALID_STATE_TRANSITION");
                                return HRESULTS.MF_E_INVALID_STATE_TRANSITION;
                            }

                            _state = value;
                            break;

                        case _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING:
                            return Start(null);

                        default:
                            EventProvider.LogInfo($"MF_E_INVALID_STATE_TRANSITION");
                            return HRESULTS.MF_E_INVALID_STATE_TRANSITION;
                    }
                }
                return HRESULTS.S_OK;
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT GetStreamState(out _MF_STREAM_STATE value)
        {
            value = _state;
            EventProvider.LogInfo($"value:{value}");
            return HRESULTS.S_OK;
        }

        public HRESULT KsProperty(ref KSIDENTIFIER roperty, uint propertyLength, nint propertyData, uint dataLength, out uint bytesReturned)
        {
            EventProvider.LogInfo($"Property:{roperty.GetMFName()} PropertyLength:{propertyLength} DataLength:{dataLength}");
            bytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        public HRESULT KsMethod(ref KSIDENTIFIER method, uint methodLength, nint methodData, uint dataLength, out uint bytesReturned)
        {
            EventProvider.LogInfo($"Method:{method.GetMFName()} PropertyLength:{methodLength} DataLength:{dataLength}");
            bytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        public HRESULT KsEvent(ref KSIDENTIFIER evt, uint eventLength, nint eventData, uint dataLength, out uint bytesReturned)
        {
            EventProvider.LogInfo($"Event:{evt.GetMFName()} PropertyLength:{eventLength} DataLength:{dataLength}");
            bytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        protected override void Dispose(bool disposing)
        {
            EventProvider.LogInfo();
            Interlocked.Exchange(ref _descriptor!, null)?.Dispose();
            Interlocked.Exchange(ref _queue!, null)?.Dispose();
            Interlocked.Exchange(ref _allocator!, null)?.Dispose();
            Interlocked.Exchange(ref _generator!, null)?.Dispose();
            base.Dispose(disposing);
        }
    }
}