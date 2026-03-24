// OpenGLES Catalyst Compatibility Shim
// Provides EAGLContext, CAEAGLLayer, and EAGLRenderingAPI types
// that are missing from the Microsoft.MacCatalyst bindings.
// These are thin wrappers around the native OpenGLES framework
// which IS available on Mac Catalyst at runtime.

#if MACCATALYST
using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using CoreAnimation;
using CoreGraphics;

namespace OpenGLES
{
    public enum EAGLRenderingAPI : ulong
    {
        OpenGLES1 = 1,
        OpenGLES2 = 2,
        OpenGLES3 = 3,
    }

    /// <summary>
    /// Minimal EAGLContext wrapper for Mac Catalyst.
    /// Wraps the native EAGLContext class via ObjC runtime.
    /// </summary>
    public class EAGLContext : NSObject
    {
        private static readonly IntPtr classHandle = Class.GetHandle("EAGLContext");

        public EAGLContext(EAGLRenderingAPI api)
            : base(CreateWithAPI(api))
        {
        }

        public EAGLContext(EAGLRenderingAPI api, EAGLSharegroup shareGroup)
            : base(CreateWithAPIAndShareGroup(api, shareGroup))
        {
        }

        internal EAGLContext(IntPtr handle) : base(handle)
        {
        }

        private static IntPtr CreateWithAPI(EAGLRenderingAPI api)
        {
            var alloc = Messaging.IntPtr_objc_msgSend(classHandle, Selector.GetHandle("alloc"));
            var init = Messaging.IntPtr_objc_msgSend_UInt64(alloc, Selector.GetHandle("initWithAPI:"), (ulong)api);
            return init;
        }

        private static IntPtr CreateWithAPIAndShareGroup(EAGLRenderingAPI api, EAGLSharegroup shareGroup)
        {
            var alloc = Messaging.IntPtr_objc_msgSend(classHandle, Selector.GetHandle("alloc"));
            var init = Messaging.IntPtr_objc_msgSend_UInt64_IntPtr(alloc,
                Selector.GetHandle("initWithAPI:sharegroup:"), (ulong)api, shareGroup.Handle);
            return init;
        }

        public EAGLRenderingAPI API
        {
            get
            {
                return (EAGLRenderingAPI)Messaging.UInt64_objc_msgSend(Handle, Selector.GetHandle("API"));
            }
        }

        public EAGLSharegroup ShareGroup
        {
            get
            {
                var handle = Messaging.IntPtr_objc_msgSend(Handle, Selector.GetHandle("sharegroup"));
                return Runtime.GetNSObject<EAGLSharegroup>(handle);
            }
        }

        public bool RenderBufferStorage(ulong target, CAEAGLLayer drawable)
        {
            return Messaging.bool_objc_msgSend_UInt64_IntPtr(Handle,
                Selector.GetHandle("renderbufferStorage:fromDrawable:"), target, drawable.Handle);
        }

        public bool PresentRenderBuffer(ulong target)
        {
            return Messaging.bool_objc_msgSend_UInt64(Handle,
                Selector.GetHandle("presentRenderbuffer:"), target);
        }

        public static EAGLContext CurrentContext
        {
            get
            {
                var handle = Messaging.IntPtr_objc_msgSend(classHandle, Selector.GetHandle("currentContext"));
                if (handle == IntPtr.Zero) return null;
                return Runtime.GetNSObject<EAGLContext>(handle);
            }
        }

        public static bool SetCurrentContext(EAGLContext context)
        {
            return Messaging.bool_objc_msgSend_IntPtr(classHandle,
                Selector.GetHandle("setCurrentContext:"), context?.Handle ?? IntPtr.Zero);
        }
    }

    /// <summary>
    /// Minimal EAGLSharegroup wrapper.
    /// </summary>
    public class EAGLSharegroup : NSObject
    {
        public EAGLSharegroup(IntPtr handle) : base(handle) { }
    }

    /// <summary>
    /// ObjC messaging helpers for types not in the Catalyst bindings.
    /// </summary>
    internal static class Messaging
    {
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern IntPtr IntPtr_objc_msgSend_UInt64(IntPtr receiver, IntPtr selector, ulong arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern IntPtr IntPtr_objc_msgSend_UInt64_IntPtr(IntPtr receiver, IntPtr selector, ulong arg1, IntPtr arg2);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern ulong UInt64_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern bool bool_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern bool bool_objc_msgSend_UInt64(IntPtr receiver, IntPtr selector, ulong arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern bool bool_objc_msgSend_UInt64_IntPtr(IntPtr receiver, IntPtr selector, ulong arg1, IntPtr arg2);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);
    }
}

namespace CoreAnimation
{
    /// <summary>
    /// Thin wrapper around the native CAEAGLLayer.
    /// Does NOT subclass — just wraps the native handle to provide typed access.
    /// The iOSGameView.LayerClass returns the real native CAEAGLLayer class,
    /// and UIKit creates the actual native instance. We just wrap it here.
    /// </summary>
    public class CAEAGLLayer
    {
        public IntPtr Handle { get; private set; }

        public CAEAGLLayer(IntPtr handle) { Handle = handle; }

        public NSDictionary DrawableProperties
        {
            get
            {
                var handle = OpenGLES.Messaging.IntPtr_objc_msgSend(Handle, Selector.GetHandle("drawableProperties"));
                return Runtime.GetNSObject<NSDictionary>(handle);
            }
            set
            {
                OpenGLES.Messaging.void_objc_msgSend_IntPtr(Handle, Selector.GetHandle("setDrawableProperties:"), value?.Handle ?? IntPtr.Zero);
            }
        }

        /// Access Bounds/Frame/ContentsScale via the underlying CALayer wrapper.
        private CALayer AsCALayer => Runtime.GetNSObject<CALayer>(Handle);

        public CGRect Bounds => AsCALayer.Bounds;
        public CGRect Frame
        {
            get => AsCALayer.Frame;
            set => AsCALayer.Frame = value;
        }
        public nfloat ContentsScale
        {
            get => AsCALayer.ContentsScale;
            set => AsCALayer.ContentsScale = value;
        }
    }
}

namespace OpenGLES
{
    /// <summary>
    /// Loads extern NSString* constants from the native OpenGLES framework.
    /// </summary>
    internal static class EAGLNativeSymbols
    {
        private static IntPtr _lib;
        internal static IntPtr Lib
        {
            get
            {
                if (_lib == IntPtr.Zero)
                    _lib = NativeLibrary.Load("/System/Library/Frameworks/OpenGLES.framework/OpenGLES");
                return _lib;
            }
        }

        internal static NSString LoadNSString(string symbol)
        {
            var ptr = NativeLibrary.GetExport(Lib, symbol);
            var strHandle = Marshal.ReadIntPtr(ptr);
            return Runtime.GetNSObject<NSString>(strHandle);
        }
    }

    /// <summary>
    /// EAGL drawable property keys.
    /// </summary>
    public static class EAGLDrawableProperty
    {
        public static NSString RetainedBacking => EAGLNativeSymbols.LoadNSString("kEAGLDrawablePropertyRetainedBacking");
        public static NSString ColorFormat => EAGLNativeSymbols.LoadNSString("kEAGLDrawablePropertyColorFormat");
    }

    /// <summary>
    /// EAGL color format constants.
    /// </summary>
    public static class EAGLColorFormat
    {
        public static NSString RGBA8 => EAGLNativeSymbols.LoadNSString("kEAGLColorFormatRGBA8");
        public static NSString RGB565 => EAGLNativeSymbols.LoadNSString("kEAGLColorFormatRGB565");
    }

}
#endif
